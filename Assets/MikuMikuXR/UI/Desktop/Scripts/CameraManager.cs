using UnityEngine;
using LibMMD.Unity3D;

namespace MikuMikuXR.UI.Desktop
{
    public class CameraManager : MonoBehaviour
    {
        public enum CameraMode
        {
            Free,
            Animation
        }

        [Header("相机控制组件")]
        public FreeCameraController freeCameraController;
        public MmdCameraController mmdCameraController;

        [Header("当前相机模式")]
        public CameraMode currentMode = CameraMode.Free;

        void Awake()
        {
            ApplyCameraMode(currentMode);
        }

        public void SetCameraMode(CameraMode mode)
        {
            if (currentMode == mode) return;
            currentMode = mode;
            ApplyCameraMode(mode);
        }

        private void ApplyCameraMode(CameraMode mode)
        {
            if (freeCameraController != null)
                freeCameraController.enabled = (mode == CameraMode.Free);
            if (mmdCameraController != null)
                mmdCameraController.enabled = (mode == CameraMode.Animation);
        }

        // UI调用：加载相机动画并切换到动画模式
        public bool LoadCameraAnimation(string vmdPath)
        {
            if (mmdCameraController == null) return false;
            bool loaded = mmdCameraController.LoadCameraMotion(vmdPath);
            if (loaded)
            {
                SetCameraMode(CameraMode.Animation);
            }
            return loaded;
        }

        // UI调用：切换到自由相机
        public void SetFreeCamera()
        {
            SetCameraMode(CameraMode.Free);
        }
    }
}
