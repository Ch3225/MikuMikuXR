using System;
using System.Collections.Generic;
using UnityEngine;

namespace MMDVR.Scripts.Managers
{    /// <summary>
    /// 键盘输入管理器 - 集中管理所有键盘快捷键
    /// 提供统一的输入处理和事件分发
    /// 
    /// 支持功能：
    /// - 快捷键管理（Tab=鼠标切换、H=UI切换、Space=播放、Ctrl+S=停止、R=重置、F11=全屏、F12=截图、V=VR）
    /// - 摄像机移动控制（WSADQE + Shift加速）
    /// - 与FreeCameraController和ToggleMouseButton集成
    /// </summary>
    public class KeyboardInputManager : MonoBehaviour
    {
        public static KeyboardInputManager Instance { get; private set; }
          [Header("键盘快捷键设置")]
        [SerializeField] private bool enableKeyboardInput = true;
        [SerializeField] private bool controlCameraMovement = true; // 是否控制摄像机移动
        
        [Header("摄像机控制")]
        [SerializeField] private MMDVR.Scripts.Controls.FreeCameraController freeCameraController;
        [SerializeField] private MMDVR.Scripts.Controls.ToggleMouseButton toggleMouseButton;
        
        // 公共属性
        public bool IsControllingMovement => controlCameraMovement && enableKeyboardInput;
        
        // 键盘快捷键事件
        public static event Action OnToggleUI;
        public static event Action OnPlayPause;
        public static event Action OnStop;
        public static event Action OnResetCamera;
        public static event Action OnScreenshot;
        public static event Action OnToggleFullscreen;
        public static event Action OnToggleVR;
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
          void Update()
        {
            if (!enableKeyboardInput) return;
            
            CheckKeyboardInput();
            
            // 处理摄像机移动
            if (controlCameraMovement && freeCameraController != null)
            {
                HandleCameraMovement();
            }
        }        /// <summary>
        /// 检查键盘输入
        /// </summary>
        private void CheckKeyboardInput()
        {
            // Tab - 切换鼠标控制模式（只处理鼠标控制，不触发UI切换）
            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab))
            {
                if (toggleMouseButton != null)
                {
                    toggleMouseButton.OnToggleMouseClicked();
                }
                // 移除UI切换，避免冲突
            }
            
            // H - 切换UI显示/隐藏 (新增，替代Tab的UI功能)
            if (UnityEngine.Input.GetKeyDown(KeyCode.H))
            {
                OnToggleUI?.Invoke();
            }
            
            // Space - 播放/暂停
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                OnPlayPause?.Invoke();
            }
            
            // S - 停止 (需要按住Ctrl)
            if (UnityEngine.Input.GetKey(KeyCode.LeftControl) && UnityEngine.Input.GetKeyDown(KeyCode.S))
            {
                OnStop?.Invoke();
            }
            
            // R - 重置相机
            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                OnResetCamera?.Invoke();
                if (freeCameraController != null)
                {
                    freeCameraController.ResetToTransform();
                }
            }
            
            // F12 - 截图
            if (UnityEngine.Input.GetKeyDown(KeyCode.F12))
            {
                OnScreenshot?.Invoke();
            }
            
            // F11 - 全屏切换
            if (UnityEngine.Input.GetKeyDown(KeyCode.F11))
            {
                OnToggleFullscreen?.Invoke();
            }
            
            // V - 切换VR模式
            if (UnityEngine.Input.GetKeyDown(KeyCode.V))
            {
                OnToggleVR?.Invoke();
            }
        }
        
        /// <summary>
        /// 启用/禁用键盘输入
        /// </summary>
        public void SetKeyboardInputEnabled(bool enabled)
        {
            enableKeyboardInput = enabled;
        }
        
        /// <summary>
        /// 启用/禁用摄像机移动控制
        /// </summary>
        public void SetCameraMovementControlEnabled(bool enabled)
        {
            controlCameraMovement = enabled;
        }
        
        /// <summary>
        /// 截图功能
        /// </summary>
        private void TakeScreenshot()
        {
            string filename = $"Screenshot_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"截图已保存: {path}");
        }        void Start()
        {
            // 自动查找组件（如果没有手动设置）
            if (freeCameraController == null)
            {
                freeCameraController = FindObjectOfType<MMDVR.Scripts.Controls.FreeCameraController>();
            }
            if (toggleMouseButton == null)
            {
                toggleMouseButton = FindObjectOfType<MMDVR.Scripts.Controls.ToggleMouseButton>();
            }
            
            // 绑定内部事件到具体功能
            OnToggleUI += () => {
                MMDVR.Events.InputEvents.TriggerUIToggle(MMDVR.Events.InputEvents.InputSource.Desktop);
            };
            
            OnPlayPause += () => {
                MMDVR.Scripts.Events.PlaybackEvents.TriggerPlayPauseToggle();
            };
            
            OnStop += () => {
                MMDVR.Scripts.Events.PlaybackEvents.TriggerStopRequested();
            };
            
            OnResetCamera += () => {
                MMDVR.Events.SystemEvents.TriggerCameraReset();
            };
            
            OnScreenshot += TakeScreenshot;
            
            OnToggleFullscreen += () => {
                Screen.fullScreen = !Screen.fullScreen;
            };
            
            OnToggleVR += () => {
                // 触发VR切换事件
                var systemStateManager = MMDVR.Scripts.Managers.SystemStateManager.Instance;
                if (systemStateManager != null)
                {
                    systemStateManager.ToggleVRMode();
                }
            };
        }
          void OnDestroy()
        {
            // 清理事件订阅
            OnToggleUI = null;
            OnPlayPause = null;
            OnStop = null;
            OnResetCamera = null;
            OnScreenshot = null;
            OnToggleFullscreen = null;
            OnToggleVR = null;
            
            if (Instance == this)
            {
                Instance = null;
            }
        }
        /// <summary>
        /// 处理摄像机移动输入 - WSADQE + Shift加速
        /// </summary>
        private void HandleCameraMovement()
        {
            // 检查移动输入
            bool forward = UnityEngine.Input.GetKey(KeyCode.W);
            bool back = UnityEngine.Input.GetKey(KeyCode.S);
            bool left = UnityEngine.Input.GetKey(KeyCode.A);
            bool right = UnityEngine.Input.GetKey(KeyCode.D);
            bool up = UnityEngine.Input.GetKey(KeyCode.E);
            bool down = UnityEngine.Input.GetKey(KeyCode.Q);
            bool fastSpeed = UnityEngine.Input.GetKey(KeyCode.LeftShift);
            
            // 获取移动方向
            Vector3 direction = MMDVR.Scripts.Controls.FreeCameraController.GetMovementDirection(
                forward, back, left, right, up, down);
            
            // 应用移动
            if (direction.magnitude > 0.1f)
            {
                freeCameraController.ApplyMovement(direction, fastSpeed);
            }
        }
    }
}
