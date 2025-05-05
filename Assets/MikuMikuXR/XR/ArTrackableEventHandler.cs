using UnityEngine;

namespace MikuMikuXR.XR
{
    // 将原本继承自 Vuforia.DefaultTrackableEventHandler 的类改为继承自 MonoBehaviour
    public class ArTrackableEventHandler : MonoBehaviour
    {
        private const int ArCameraLayer = 8;
        
        private Camera _controlCamera;

        protected virtual void Start()
        {
            _controlCamera = Camera.main;
            if (_controlCamera == null)
            {
                return;
            }
            
            // 注释掉原本使用 AR 背景平面的代码
            // _controlCamera.transform.Find("BackgroundPlane").gameObject.layer = ArCameraLayer;
            // _controlCamera.cullingMask = 1 << ArCameraLayer;   
        }

        // 这些方法在 VR 模式下不再需要，但保留空方法以防其他脚本仍然调用
        protected virtual void OnTrackingFound()
        {
            // VR 模式下不需要特殊处理
        }

        protected virtual void OnTrackingLost()
        {
            // VR 模式下不需要特殊处理
        }
    }
}