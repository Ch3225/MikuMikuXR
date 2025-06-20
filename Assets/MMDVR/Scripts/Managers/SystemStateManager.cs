using UnityEngine;
using MMDVR.Events;
using LibMMD.Unity3D;

namespace MMDVR.Scripts.Managers
{
    /// <summary>
    /// 摄像机模式枚举
    /// </summary>
    public enum CameraMode
    {
        Desktop,  // 桌面模式
        VR        // VR模式
    }

    /// <summary>
    /// 系统状态管理器 - 管理VR/桌面模式切换、设备检测等系统级状态
    /// </summary>
    public class SystemStateManager : MonoBehaviour
    {
        public static SystemStateManager Instance { get; private set; }

        [Header("运行模式")]
        [Tooltip("当前运行模式")] public CameraMode currentCameraMode = CameraMode.Desktop;        [Header("功能摄像机引用")]
        [Tooltip("主摄像机（统一使用）")] public Camera mainCamera;
        [Tooltip("VR Origin GameObject")] public GameObject vrOrigin;

        private static bool mmdSystemInitialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }            Instance = this;
            DontDestroyOnLoad(this.gameObject);

            // 初始化MMD系统
            InitializeMMDSystem();

            // 订阅系统事件
            SystemEvents.OnVRModeDetected += OnVRModeDetected;
        }

        private void Start()
        {
            // 启动VR检测
            SystemEvents.StartVRDetection(this);
        }

        /// <summary>
        /// 获取当前活动摄像机
        /// </summary>
        public Camera GetActiveCamera()
        {
            // 如果为VR模式，尝试从VR Origin获取摄像机
            if (IsVRMode && vrOrigin != null)
            {
                var vrCamera = vrOrigin.GetComponentInChildren<Camera>();
                if (vrCamera != null) return vrCamera;
            }
            
            // 桌面模式或VR找不到时使用主摄像机
            return mainCamera;
        }

        /// <summary>
        /// 处理VR模式检测结果
        /// </summary>
        /// <param name="isVRActive">VR是否激活</param>
        private void OnVRModeDetected(bool isVRActive)
        {
            CameraMode detectedMode = isVRActive ? CameraMode.VR : CameraMode.Desktop;
            SetCameraMode(detectedMode);
            Debug.Log($"VR检测完成，自动切换到 {detectedMode} 模式");
        }

        /// <summary>
        /// 设置摄像机模式 - 可被外部脚本调用
        /// </summary>
        public void SetCameraMode(CameraMode mode)
        {
            currentCameraMode = mode;
            
            switch (mode)
            {
                case CameraMode.Desktop:
                    if (vrOrigin != null) vrOrigin.SetActive(false);
                    break;
                    
                case CameraMode.VR:
                    if (vrOrigin != null) vrOrigin.SetActive(true);
                    break;
            }

            // 触发系统状态变更事件
            SystemEvents.TriggerXRSystemStateChanged(mode == CameraMode.VR);
        }

        /// <summary>
        /// 获取当前是否为VR模式
        /// </summary>
        public bool IsVRMode => currentCameraMode == CameraMode.VR;

        /// <summary>
        /// 手动切换模式
        /// </summary>
        public void ToggleCameraMode()
        {
            CameraMode newMode = currentCameraMode == CameraMode.Desktop ? CameraMode.VR : CameraMode.Desktop;
            SetCameraMode(newMode);
        }        private void OnDestroy()
        {
            // 取消事件订阅
            SystemEvents.OnVRModeDetected -= OnVRModeDetected;
        }

        /// <summary>
        /// 初始化MMD系统（集成自MMDVRInitializer）
        /// </summary>
        private void InitializeMMDSystem()
        {
            if (!mmdSystemInitialized)
            {
                Debug.Log("SystemStateManager: 初始化MMD核心系统...");
                
                // 确保MmdResourceManager已创建
                var existingManager = FindObjectOfType<MmdResourceManager>();
                if (existingManager == null)
                {
                    var managerGo = new GameObject("MmdResourceManager");
                    var manager = managerGo.AddComponent<MmdResourceManager>();
                    DontDestroyOnLoad(managerGo);
                    Debug.Log("SystemStateManager: 创建了MmdResourceManager");
                }
                
                mmdSystemInitialized = true;
            }
        }

        private void OnApplicationQuit()
        {
            Debug.Log("SystemStateManager: 应用退出，执行MMD资源清理...");
            
            // 查找所有可能需要清理的MMD资源
            var mmdObjects = FindObjectsOfType<MmdGameObject>();
            foreach (var mmd in mmdObjects)
            {
                if (mmd != null)
                {
                    try
                    {
                        // 激活OnApplicationQuit清理
                        mmd.SendMessage("OnApplicationQuit", SendMessageOptions.DontRequireReceiver);
                    }
                    catch
                    {
                        // 忽略错误
                    }
                }
            }
        }
    }
}
