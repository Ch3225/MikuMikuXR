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
        [Header("距离头盔的偏移（米）")]
        public Vector3 offset = new Vector3(0, 0.0f, 0.5f); // Adjusted Z for typical HMD distance
        [Header("只在VR模式下激活")]
        public bool onlyInVR = true;
        [Header("可选：同步UI按钮（ToggleUISectionButton）")]
        public ToggleUISectionButton toggleUIButton;

        private List<InputDevice> devices = new List<InputDevice>();
        private Dictionary<uint, bool> lastButtonStates = new Dictionary<uint, bool>();
        private Camera mainCamera;

        void Start()
        {
            if (uiPanel != null)
                uiPanel.SetActive(false);
            
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("VRDesktopUIAdapter: Main Camera not found! UI positioning will not work correctly.");
            }
        }

        void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main; // Try to get camera if it wasn't available at Start
                if (mainCamera == null) return; // Still no camera, can't proceed
            }

            if (onlyInVR && !IsVRActive())
            {
                if (uiPanel != null && uiPanel.activeSelf) uiPanel.SetActive(false);
                return;
            }

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

            // UI Positioning Logic - now relative to HMD (mainCamera)
            if (uiPanel != null && uiPanel.activeSelf && mainCamera != null)
            {
                // Position the panel in front of the camera
                uiPanel.transform.position = mainCamera.transform.position +
                                             mainCamera.transform.right * offset.x +
                                             mainCamera.transform.up * offset.y +
                                             mainCamera.transform.forward * offset.z;

                // Make the panel look at the camera
                // The panel's "forward" should point towards the camera, or its "back" should point along camera's forward
                // This makes the panel's +Z axis point towards the camera.
                // If your panel's content is on its -Z face, you might need to adjust the rotation.
                uiPanel.transform.rotation = Quaternion.LookRotation(uiPanel.transform.position - mainCamera.transform.position, mainCamera.transform.up);
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
