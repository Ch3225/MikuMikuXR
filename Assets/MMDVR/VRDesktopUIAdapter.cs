using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using MMDVR.UI.Controls;

namespace MMDVR
{
    /// <summary>
    /// VR模式下将UGUI面板跟随到XR控制器前方，并监听菜单键呼出/隐藏UI
    /// </summary>
    public class VRDesktopUIAdapter : MonoBehaviour
    {
        [Header("桌面模式UI (Screen Space Overlay)")]
        public GameObject desktopUIPanel;
        
        [Header("VR模式UI (World Space)")]
        public GameObject vrUIPanel;

        [Header("（可选）VR摄像机，不设置则尝试Camera.main")]
        public Camera vrCamera; // User can assign this in Inspector for reliability
        
        [Header("距离头盔的偏移（米）")]
        public Vector3 offset = new Vector3(0, 0.0f, 3.0f); // Increased Z to 3.0f
        
        [Header("可选：同步UI按钮（ToggleUISectionButton）")]
        public ToggleUISectionButton toggleUIButton;

        // 提供兼容性的uiPanel属性，指向当前激活的UI面板
        public GameObject uiPanel 
        {
            get 
            {
                if (IsVRActive() && vrUIPanel != null && vrUIPanel.activeSelf)
                    return vrUIPanel;
                else if (desktopUIPanel != null && desktopUIPanel.activeSelf)
                    return desktopUIPanel;
                else
                    return IsVRActive() ? vrUIPanel : desktopUIPanel;
            }
        }

        private List<InputDevice> devices = new List<InputDevice>();
        private Dictionary<uint, bool> lastButtonStates = new Dictionary<uint, bool>();
        private Quaternion fixedUIRotation; 
        private Vector3 worldOffsetFromHmd; // To store the fixed world-space offset vector from HMD

        public enum UIShowSource
        {
            Desktop,
            VR
        }

        void Start()
        {
            // 确保两个UI初始时均为隐藏状态
            if (desktopUIPanel != null)
                desktopUIPanel.SetActive(false);
            
            if (vrUIPanel != null)
                vrUIPanel.SetActive(false);
            
            if (vrCamera == null) // If user hasn't assigned it
            {
                vrCamera = Camera.main;
            }

            if (vrCamera == null)
            {
                Debug.LogError("VRDesktopUIAdapter: VR Camera not found or assigned! UI positioning will not work correctly. Please assign the VR Camera in the Inspector or ensure a Camera is tagged 'MainCamera'.");
            }
            
            // 确保桌面UI为Screen Space Overlay模式
            if (desktopUIPanel != null)
            {
                Canvas desktopCanvas = desktopUIPanel.GetComponent<Canvas>();
                if (desktopCanvas != null)
                {
                    desktopCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }
            }
            
            // 确保VR UI为World Space模式
            if (vrUIPanel != null)
            {
                Canvas vrCanvas = vrUIPanel.GetComponent<Canvas>();
                if (vrCanvas != null)
                {
                    vrCanvas.renderMode = RenderMode.WorldSpace;
                    vrUIPanel.transform.localScale = Vector3.one * 0.001f; // 设置适合VR的缩放
                }
            }
        }

        public void ToggleUI(UIShowSource source)
        {
            if (source == UIShowSource.Desktop)
            {
                // 桌面模式：切换桌面UI的显示/隐藏
                if (desktopUIPanel != null)
                {
                    // 关闭VR UI，打开/关闭桌面UI
                    if (vrUIPanel != null)
                        vrUIPanel.SetActive(false);
                    
                    desktopUIPanel.SetActive(!desktopUIPanel.activeSelf);
                }
            }
            else // VR模式
            {
                // VR模式：切换VR UI的显示/隐藏
                if (vrUIPanel != null)
                {
                    // 关闭桌面UI，打开/关闭VR UI
                    if (desktopUIPanel != null)
                        desktopUIPanel.SetActive(false);
                    
                    bool toShow = !vrUIPanel.activeSelf;
                    vrUIPanel.SetActive(toShow);
                    
                    if (toShow)
                    {
                        // 更新VR UI在世界中的位置和旋转
                        if (vrCamera != null)
                        {
                            fixedUIRotation = Quaternion.LookRotation(vrCamera.transform.forward, vrCamera.transform.up);
                            worldOffsetFromHmd = vrCamera.transform.rotation * offset;
                            vrUIPanel.transform.position = vrCamera.transform.position + worldOffsetFromHmd;
                            vrUIPanel.transform.rotation = fixedUIRotation;
                        }
                    }
                }
            }
        }

        void Update()
        {
            if (vrCamera == null) 
            {
                if (Camera.main != null) 
                {
                    vrCamera = Camera.main;
                }
                else
                {
                    return; 
                }
            }

            // 监听VR控制器按钮输入
            devices.Clear();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);
            foreach (var device in devices)
            {
                bool menuPressed = false;
                bool secondaryPressed = false;
                device.TryGetFeatureValue(CommonUsages.menuButton, out menuPressed);
                device.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryPressed);
                bool pressed = menuPressed || secondaryPressed;
                
                uint deviceKey = unchecked((uint)device.GetHashCode());
                bool lastPressed = false;
                lastButtonStates.TryGetValue(deviceKey, out lastPressed);

                if (pressed && !lastPressed)
                {
                    ToggleUI(UIShowSource.VR);
                }
                lastButtonStates[deviceKey] = pressed;
            }

            // 如果VR UI处于激活状态且在World Space模式下，持续更新位置和旋转
            if (vrUIPanel != null && vrUIPanel.activeSelf && vrCamera != null)
            {
                var canvas = vrUIPanel.GetComponent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
                {
                    vrUIPanel.transform.position = vrCamera.transform.position + worldOffsetFromHmd;
                    vrUIPanel.transform.rotation = fixedUIRotation;
                }
            }
        }

        private bool IsVRActive()
        {
            List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var subsystem in subsystems)
            {
                if (subsystem.running)
                    return true;
            }
            // Fallback check for HMD presence if subsystem check is not definitive
            List<InputDevice> hmds = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, hmds);
            return hmds.Count > 0 && hmds[0].isValid;
        }
    }
}
