using UnityEngine;
using MMDVR.Events;
using LibMMD.Unity3D;
using TMPro;

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
        public static SystemStateManager Instance { get; private set; }        [Header("运行模式")]
        [Tooltip("当前运行模式")] public CameraMode currentCameraMode = CameraMode.Desktop;

        [Header("摄像机引用")]
        [Tooltip("桌面模式摄像机")] public Camera desktopCamera;
        [Tooltip("VR Origin GameObject")] public GameObject vrOrigin;

        // ==================== VR模式切换功能 ====================
          [Header("VR模式切换")]
        // 不需要额外的desktopCamerasGroup，直接控制摄像机的激活状态
        
        private bool isVRMode = false;

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
        }        private void Start()
        {
            // 启动VR检测
            SystemEvents.StartVRDetection(this);
        }        /// <summary>
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
            
            // 桌面模式使用桌面摄像机
            return desktopCamera;
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
        }        /// <summary>
        /// 切换VR/桌面模式
        /// </summary>
        public void ToggleVRMode()
        {
            isVRMode = !isVRMode;
            
            // 控制摄像机激活状态
            SwitchCameraMode(isVRMode);
            
            Debug.Log($"SystemStateManager: 切换到{(isVRMode ? "VR" : "桌面")}模式");
            
            // 触发VR状态变化事件
            SystemEvents.TriggerXRSystemStateChanged(isVRMode);
        }        /// <summary>
        /// 设置VR模式（不切换，直接设置）
        /// </summary>
        /// <param name="vrMode">是否启用VR模式</param>
        public void SetVRMode(bool vrMode)
        {
            if (isVRMode == vrMode) return; // 如果状态相同，不做任何操作
            
            isVRMode = vrMode;
            
            // 控制摄像机激活状态
            SwitchCameraMode(isVRMode);
            
            Debug.Log($"SystemStateManager: 设置为{(isVRMode ? "VR" : "桌面")}模式");
            
            // 触发VR状态变化事件
            SystemEvents.TriggerXRSystemStateChanged(isVRMode);
        }        /// <summary>
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
                }            }
        }

        /// <summary>
        /// 切换摄像机模式
        /// </summary>
        /// <param name="useVRMode">是否使用VR模式</param>
        private void SwitchCameraMode(bool useVRMode)
        {
            if (useVRMode)
            {
                // VR模式：激活VR Origin，禁用桌面摄像机
                if (vrOrigin != null) vrOrigin.SetActive(true);
                if (desktopCamera != null) desktopCamera.gameObject.SetActive(false);
            }
            else
            {
                // 桌面模式：禁用VR Origin，激活桌面摄像机
                if (vrOrigin != null) vrOrigin.SetActive(false);
                if (desktopCamera != null) desktopCamera.gameObject.SetActive(true);
            }
        }
    }
}
