using UnityEngine;
using UnityEngine.XR;

namespace MMDVR.VR
{
    /// <summary>
    /// VR实用工具类
    /// </summary>
    public static class VRHelper
    {
        /// <summary>
        /// 检查VR系统是否可用
        /// </summary>
        public static bool IsVRAvailable()
        {
            return XRSettings.isDeviceActive && XRSettings.enabled;
        }

        /// <summary>
        /// 获取当前VR设备名称
        /// </summary>
        public static string GetVRDeviceName()
        {
            return XRSettings.isDeviceActive ? XRSettings.loadedDeviceName : "没有VR设备";
        }

        /// <summary>
        /// 检查SteamVR是否可用
        /// </summary>
        public static bool IsSteamVRAvailable()
        {
            return SteamVRBridge.Instance != null && SteamVRBridge.Instance.IsSteamVRInitialized();
        }

        /// <summary>
        /// 创建简单的控制器可视化
        /// </summary>
        public static GameObject CreateControllerVisual(Transform parent, Color color)
        {
            GameObject controller = GameObject.CreatePrimitive(PrimitiveType.Cube);
            controller.transform.parent = parent;
            controller.transform.localPosition = Vector3.zero;
            controller.transform.localRotation = Quaternion.identity;
            controller.transform.localScale = new Vector3(0.05f, 0.05f, 0.1f);

            var renderer = controller.GetComponent<MeshRenderer>();
            renderer.material.color = color;

            return controller;
        }

        /// <summary>
        /// 将世界坐标转换为VR相对坐标
        /// </summary>
        public static Vector3 WorldToVRSpace(Vector3 worldPosition)
        {
            if (VRManager.Instance != null && VRManager.Instance.VRCamera != null)
            {
                var vrCamera = VRManager.Instance.VRCamera;
                return vrCamera.transform.InverseTransformPoint(worldPosition);
            }
            return worldPosition;
        }

        /// <summary>
        /// 将VR相对坐标转换为世界坐标
        /// </summary>
        public static Vector3 VRToWorldSpace(Vector3 vrPosition)
        {
            if (VRManager.Instance != null && VRManager.Instance.VRCamera != null)
            {
                var vrCamera = VRManager.Instance.VRCamera;
                return vrCamera.transform.TransformPoint(vrPosition);
            }
            return vrPosition;
        }
        
        /// <summary>
        /// 在头戴式设备上显示消息
        /// </summary>
        public static void ShowMessageInHeadset(string message, float duration = 3.0f)
        {
            // 在未来可以实现为VR环境中的UI消息
            Debug.Log("VR消息: " + message);
        }

        /// <summary>
        /// 计算两点之间距离，考虑VR比例
        /// </summary>
        public static float CalculateVRDistance(Vector3 posA, Vector3 posB, float vrScale = 1.0f)
        {
            return Vector3.Distance(posA, posB) * vrScale;
        }

        /// <summary>
        /// 振动控制器
        /// </summary>
        public static void VibrateController(bool isLeft, float strength, float duration)
        {
            if (SteamVRBridge.Instance != null)
            {
                SteamVRBridge.Instance.HapticPulse(isLeft, duration, Mathf.Clamp01(strength), 1.0f);
            }
            else if (VRInputController.Instance != null)
            {
                if (isLeft)
                {
                    VRInputController.Instance.VibrateLeftController(Mathf.Clamp01(strength), duration);
                }
                else
                {
                    VRInputController.Instance.VibrateRightController(Mathf.Clamp01(strength), duration);
                }
            }
        }
    }
}
