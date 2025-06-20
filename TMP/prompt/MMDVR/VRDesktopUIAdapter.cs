using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;
using MMDVR.UI.Controls;

namespace MMDVR
{
    /// <summary>
    /// VR和桌面模式UI适配器，使用单一UI实例根据呼出方式动态切換Canvas模式
    /// </summary>
    public class VRDesktopUIAdapter : MonoBehaviour
    {
        [Header("主UI面板（单一实例）")]
        public GameObject mainUIPanel;

        [Header("（可选）VR摄像机，不设置则尝试Camera.main")]
        public Camera vrCamera; // User can assign this in Inspector for reliability
        
        [Header("距离头盔的偏移（米）")]
        public Vector3 offset = new Vector3(0, 0.0f, 3.0f); // Increased Z to 3.0f
        
        [Header("可选：同步UI按钮（ToggleUISectionButton）")]
        public ToggleUISectionButton toggleUIButton;

        // 提供兼容性的uiPanel属性，指向主UI面板
        public GameObject uiPanel 
        {
            get 
            {
                return mainUIPanel;
            }
        }

        // 向后兼容的属性
        [System.Obsolete("Use mainUIPanel instead")]
        public GameObject desktopUIPanel 
        {
            get { return mainUIPanel; }
            set { mainUIPanel = value; }
        }
        
        [System.Obsolete("Use mainUIPanel instead")]
        public GameObject vrUIPanel        {
            get { return mainUIPanel; }
            set { mainUIPanel = value; }
        }

        private List<InputDevice> devices = new List<InputDevice>();
        private Dictionary<uint, bool> lastButtonStates = new Dictionary<uint, bool>();
        private Quaternion fixedUIRotation; 
        private Vector3 worldOffsetFromHmd; // To store the fixed world-space offset vector from HMD
        private Canvas mainCanvas; // 缓存主Canvas组件
        private bool isCurrentlyInVRMode = false; // 当前是否处于VR模式显示
        
        // Input Module 管理
        private XRUIInputModule xrInputModule;
        private StandaloneInputModule standaloneInputModule;
        private bool lastVRActiveState = false;

        public enum UIShowSource
        {
            Desktop,
            VR
        }

        void Start()
        {
            // 初始化主UI面板
            if (mainUIPanel != null)
            {
                mainCanvas = mainUIPanel.GetComponent<Canvas>();
                if (mainCanvas == null)
                {
                    Debug.LogError("VRDesktopUIAdapter: MainUIPanel must have a Canvas component!");
                    return;
                }
                
                // 初始时隐藏UI
                mainUIPanel.SetActive(false);
                
                // 根据当前是否在VR模式中设置初始Canvas状态
                if (IsVRActive())
                {
                    SetupForVRMode();
                }
                else
                {
                    SetupForDesktopMode();
                }
            }

            if (vrCamera == null) // If user hasn't assigned it
            {
                vrCamera = Camera.main;
            }

            if (vrCamera == null)
            {
                Debug.LogError("VRDesktopUIAdapter: VR Camera not found or assigned! UI positioning will not work correctly. Please assign the VR Camera in the Inspector or ensure a Camera is tagged 'MainCamera'.");
            }
            
            // 初始化Input Module管理
            InitializeInputModules();
            
            // 根据当前VR状态设置正确的Input Module
            bool vrActive = IsVRActive();
            lastVRActiveState = vrActive;
            UpdateInputModules(vrActive);
        }

        private void SetupForVRMode()
        {
            if (mainCanvas == null) return;
            
            mainCanvas.renderMode = RenderMode.WorldSpace;
            mainUIPanel.transform.localScale = Vector3.one * 0.001f; // 设置适合VR的缩放
            isCurrentlyInVRMode = true;
        }

        private void SetupForDesktopMode()
        {
            if (mainCanvas == null) return;
              mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainUIPanel.transform.localScale = Vector3.one; // 重置为默认缩放
            isCurrentlyInVRMode = false;
        }

        public void ToggleUI(UIShowSource source)
        {
            if (mainUIPanel == null || mainCanvas == null) return;

            bool isCurrentlyActive = mainUIPanel.activeSelf;
            
            if (source == UIShowSource.Desktop)
            {
                // 桌面模式：切换UI显示/隐藏，并确保Canvas为ScreenSpaceOverlay模式
                if (!isCurrentlyActive)
                {
                    // 显示UI前先设置为桌面模式
                    SetupForDesktopMode();
                    mainUIPanel.SetActive(true);
                }
                else
                {
                    // 隐藏UI
                    mainUIPanel.SetActive(false);
                }
            }
            else // VR模式
            {
                // VR模式：切换UI显示/隐藏，并确保Canvas为WorldSpace模式
                if (!isCurrentlyActive)
                {
                    // 显示UI前先设置为VR模式
                    SetupForVRMode();
                    mainUIPanel.SetActive(true);
                    
                    // 更新VR UI在世界中的位置和旋转
                    if (vrCamera != null)
                    {
                        fixedUIRotation = Quaternion.LookRotation(vrCamera.transform.forward, vrCamera.transform.up);
                        worldOffsetFromHmd = vrCamera.transform.rotation * offset;
                        mainUIPanel.transform.position = vrCamera.transform.position + worldOffsetFromHmd;
                        mainUIPanel.transform.rotation = fixedUIRotation;
                    }
                }
                else
                {
                    // 隐藏UI
                    mainUIPanel.SetActive(false);                }
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

            // 如果UI处于激活状态且当前为VR模式，持续更新位置和旋转
            if (mainUIPanel != null && mainUIPanel.activeSelf && isCurrentlyInVRMode && vrCamera != null)
            {
                if (mainCanvas != null && mainCanvas.renderMode == RenderMode.WorldSpace)
                {
                    mainUIPanel.transform.position = vrCamera.transform.position + worldOffsetFromHmd;
                    mainUIPanel.transform.rotation = fixedUIRotation;
                }
            }
            
            // 检查VR设备状态变化，自动切换Input Module
            bool currentVRActive = IsVRActive();
            if (currentVRActive != lastVRActiveState)
            {
                UpdateInputModules(currentVRActive);
                lastVRActiveState = currentVRActive;
                
                Debug.Log($"VRDesktopUIAdapter: VR device status changed to {(currentVRActive ? "Active" : "Inactive")}, switched input modules accordingly");
            }

            // 自动管理输入模块
            ManageInputModules();
        }

        private void ManageInputModules()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogError("VRDesktopUIAdapter: No EventSystem found in the scene. Please add an EventSystem to the scene.");
                return;
            }

            // 获取XRUIInputModule和StandaloneInputModule
            xrInputModule = eventSystem.GetComponent<XRUIInputModule>();
            standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();

            if (xrInputModule == null && standaloneInputModule == null)
            {
                Debug.LogError("VRDesktopUIAdapter: No suitable InputModule found on the EventSystem. Please add either XRUIInputModule or StandaloneInputModule.");
                return;
            }

            // 根据当前模式启用/禁用输入模块
            if (isCurrentlyInVRMode)
            {
                if (xrInputModule != null) xrInputModule.enabled = true;
                if (standaloneInputModule != null) standaloneInputModule.enabled = false;
            }
            else
            {
                if (xrInputModule != null) xrInputModule.enabled = false;
                if (standaloneInputModule != null) standaloneInputModule.enabled = true;
            }
        }

        private void InitializeInputModules()
        {
            // 查找现有的Input Modules
            xrInputModule = FindObjectOfType<XRUIInputModule>();
            standaloneInputModule = FindObjectOfType<StandaloneInputModule>();
            
            // 如果没有StandaloneInputModule，创建一个作为备用
            if (standaloneInputModule == null)
            {
                GameObject eventSystemObj = EventSystem.current?.gameObject;
                if (eventSystemObj != null)
                {
                    standaloneInputModule = eventSystemObj.AddComponent<StandaloneInputModule>();
                    Debug.Log("VRDesktopUIAdapter: Created StandaloneInputModule as fallback for desktop mode");
                }
            }
        }

        private void UpdateInputModules(bool useVRModule)
        {
            if (useVRModule)
            {
                // VR模式：启用XRUIInputModule，禁用StandaloneInputModule
                if (xrInputModule != null)
                    xrInputModule.enabled = true;
                if (standaloneInputModule != null)
                    standaloneInputModule.enabled = false;
            }
            else
            {
                // 桌面模式：禁用XRUIInputModule，启用StandaloneInputModule
                if (xrInputModule != null)
                    xrInputModule.enabled = false;
                if (standaloneInputModule != null)
                    standaloneInputModule.enabled = true;
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
