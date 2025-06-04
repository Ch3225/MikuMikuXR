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
        [Header("需要跟随的UI面板（如MainUI）")]
        public GameObject uiPanel;
        [Header("（可选）VR摄像机，不设置则尝试Camera.main")]
        public Camera vrCamera; // User can assign this in Inspector for reliability
        [Header("距离头盔的偏移（米）")]
        public Vector3 offset = new Vector3(0, 0.0f, 3.0f); // Increased Z to 3.0f
        [Header("只在VR模式下激活")]
        public bool onlyInVR = true;
        [Header("可选：同步UI按钮（ToggleUISectionButton）")]
        public ToggleUISectionButton toggleUIButton;

        private List<InputDevice> devices = new List<InputDevice>();
        private Dictionary<uint, bool> lastButtonStates = new Dictionary<uint, bool>();
        // private Camera mainCamera; // This field seems redundant if vrCamera is used and falls back to Camera.main
        private Quaternion fixedUIRotation; 
        private Vector3 worldOffsetFromHmd; // To store the fixed world-space offset vector from HMD

        void Start()
        {
            if (uiPanel != null)
                uiPanel.SetActive(false);
            
            if (vrCamera == null) // If user hasn't assigned it
            {
                vrCamera = Camera.main;
            }

            if (vrCamera == null)
            {
                Debug.LogError("VRDesktopUIAdapter: VR Camera not found or assigned! UI positioning will not work correctly. Please assign the VR Camera in the Inspector or ensure a Camera is tagged 'MainCamera'.");
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
                    if (uiPanel != null && uiPanel.activeSelf && onlyInVR) uiPanel.SetActive(false); 
                    return; 
                }
            }

            if (onlyInVR && !IsVRActive()) 
            {
                if (uiPanel != null && uiPanel.activeSelf) uiPanel.SetActive(false);
                return;
            }

            bool wasActive = (uiPanel != null) && uiPanel.activeSelf; // Capture state before input processing

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
                    if (toggleUIButton != null)
                    {
                        toggleUIButton.OnToggleUIClicked();
                    }
                    else if (uiPanel != null)
                    {
                        uiPanel.SetActive(!uiPanel.activeSelf);
                    }
                }
                lastButtonStates[deviceKey] = pressed;
            }

            bool isActive = (uiPanel != null) && uiPanel.activeSelf; // Capture state after input processing

            if (isActive && vrCamera != null)
            {
                if (!wasActive) // UI Panel was just activated in this frame
                {
                    // Determine and set the fixed world rotation based on HMD's current orientation
                    fixedUIRotation = Quaternion.LookRotation(vrCamera.transform.forward, vrCamera.transform.up);
                    uiPanel.transform.rotation = fixedUIRotation;

                    // Calculate and store the fixed world-space offset from the HMD
                    worldOffsetFromHmd = vrCamera.transform.rotation * offset;
                    uiPanel.transform.position = vrCamera.transform.position + worldOffsetFromHmd;
                }
                else // UI Panel was already active, update position and maintain fixed rotation & world offset
                {
                    uiPanel.transform.position = vrCamera.transform.position + worldOffsetFromHmd;
                    uiPanel.transform.rotation = fixedUIRotation; // Apply the stored fixed rotation
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
