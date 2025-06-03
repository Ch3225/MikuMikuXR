using UnityEngine;
using UnityEngine.XR;
#if UNITY_XR_MANAGEMENT
using UnityEngine.XR.Management;
#endif

namespace MMDVR.UI.Controls
{
    public class VRDesktopAutoSwitcher : MonoBehaviour
    {
        [SerializeField] private GameObject desktopCamerasGroup;
        [SerializeField] private GameObject xrRigGroup;

        void Start()
        {
            if (IsVRDevicePresent())
            {
                // 启用VR
                if (xrRigGroup != null) xrRigGroup.SetActive(true);
                if (desktopCamerasGroup != null) desktopCamerasGroup.SetActive(false);
#if UNITY_XR_MANAGEMENT
                var xrManager = XRGeneralSettings.Instance.Manager;
                xrManager.InitializeLoaderSync();
                xrManager.StartSubsystems();
#endif
            }
            else
            {
                // 启用桌面
                if (xrRigGroup != null) xrRigGroup.SetActive(false);
                if (desktopCamerasGroup != null) desktopCamerasGroup.SetActive(true);
#if UNITY_XR_MANAGEMENT
                var xrManager = XRGeneralSettings.Instance.Manager;
                xrManager.StopSubsystems();
                xrManager.DeinitializeLoader();
#endif
            }
        }

        private bool IsVRDevicePresent()
        {
            var hmdDevices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, hmdDevices);
            return hmdDevices.Count > 0;
        }
    }
}
