using UnityEngine;
using MMDVR.Managers;

namespace MMDVR.VR
{
    /// <summary>
    /// 管理VR模式下的相机
    /// 处理MMD相机与VR相机的交互
    /// </summary>
    public class VRCameraController : MonoBehaviour
    {
        public static VRCameraController Instance { get; private set; }

        [Header("相机引用")]
        public Camera vrCamera;     // VR相机（通常是主相机）
        public Camera mmdCamera;    // MMD相机参考

        [Header("VR相机设置")]
        public float eyeDistanceScale = 1.0f;  // IPD缩放因子
        public float nearClipPlane = 0.01f;
        public float farClipPlane = 1000f;

        private bool _usingMmdCameraTracking = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // 找到或创建VR相机
            InitializeVRCamera();
            
            // 注册为相机管理器
            CameraManager.Instance?.AddCamera(vrCamera.gameObject);
        }

        /// <summary>
        /// 初始化VR相机
        /// </summary>
        public void InitializeVRCamera()
        {
            // 如果未指定VR相机，尝试从VRManager获取
            if (vrCamera == null && VRManager.Instance != null)
            {
                vrCamera = VRManager.Instance.VRCamera;
            }
            
            // 如果仍然为空，使用主相机
            if (vrCamera == null)
            {
                vrCamera = Camera.main;
                if (vrCamera == null)
                {
                    Debug.LogError("无法找到VR相机，VRCameraController可能无法正常工作");
                    return;
                }
            }

            // 确保相机设置正确
            vrCamera.stereoTargetEye = UnityEngine.StereoTargetEyeMask.Both;
            vrCamera.nearClipPlane = nearClipPlane;
            vrCamera.farClipPlane = farClipPlane;
            vrCamera.tag = "MainCamera";
            
            // 确保相机在正确的图层中渲染
            vrCamera.cullingMask = -1; // 渲染所有图层
            
            Debug.Log("VR相机已初始化");
        }

        /// <summary>
        /// 连接到MMD相机，以便在VR中使用MMD相机动画
        /// </summary>
        public void LinkToMMDCamera(Camera mmdCam)
        {
            mmdCamera = mmdCam;
            _usingMmdCameraTracking = mmdCam != null;
            
            if (_usingMmdCameraTracking)
            {
                Debug.Log("已连接到MMD相机");
            }
            else
            {
                Debug.Log("MMD相机连接已断开");
            }
        }

        private void LateUpdate()
        {
            // 如果我们使用MMD相机的轨迹，使VR相机跟随它
            if (_usingMmdCameraTracking && mmdCamera != null)
            {
                // 只跟随位置和旋转，不修改VR相机的立体设置
                vrCamera.transform.position = mmdCamera.transform.position;
                vrCamera.transform.rotation = mmdCamera.transform.rotation;
            }
        }

        /// <summary>
        /// 切换使用MMD相机轨迹
        /// </summary>
        public void ToggleMMDCameraTracking(bool useMMDCamera)
        {
            _usingMmdCameraTracking = useMMDCamera && mmdCamera != null;
        }

        /// <summary>
        /// 调整瞳距（眼间距）
        /// </summary>
        public void SetEyeDistance(float scale)
        {
            eyeDistanceScale = Mathf.Clamp(scale, 0.1f, 2.0f);
            // 注意：在Unity的XR系统中，眼间距通常通过API来控制
            // 具体实现取决于您使用的XR系统
            Debug.Log($"眼间距设置为 {eyeDistanceScale}");
        }

        /// <summary>
        /// 重置VR相机到初始位置
        /// </summary>
        public void ResetVRCameraPosition()
        {
            vrCamera.transform.position = Vector3.zero;
            vrCamera.transform.rotation = Quaternion.identity;
            
            Debug.Log("VR相机位置已重置");
        }
    }
}
