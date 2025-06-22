using System;
using System.Collections.Generic;
using UnityEngine;
using MMDVR.Scripts.UIInteraction; // 添加了正确的命名空间

namespace MMDVR.Scripts.Managers
{
    /// <summary>
    /// 键盘输入管理器 - 集中管理所有键盘快捷键
    /// 提供统一的输入处理和事件分发，支持Inspector配置按键映射
    /// 
    /// 支持功能：
    /// - 可配置的快捷键管理
    /// - 摄像机移动控制（WSADQE + Shift加速）
    /// - 与CameraComponent和ToggleMouseButton集成
    /// </summary>
    public class KeyboardInputManager : MonoBehaviour
    {
        public static KeyboardInputManager Instance { get; private set; }

        [Header("UI控制")]
        [Tooltip("切换UI显示/隐藏")]
        public KeyCode toggleUI = KeyCode.H;
        
        [Tooltip("切换鼠标控制模式")]
        public KeyCode toggleMouseMode = KeyCode.Tab;
        
        [Header("播放控制")]
        [Tooltip("播放/暂停")]
        public KeyCode playPause = KeyCode.Space;
        
        [Tooltip("停止播放")]
        public KeyCode stop = KeyCode.S;
        
        [Tooltip("停止播放的修饰键")]
        public bool stopRequireCtrl = true;
        
        [Header("摄像机控制")]
        [Tooltip("重置摄像机")]
        public KeyCode resetCamera = KeyCode.R;
        
        [Tooltip("向前移动")]
        public KeyCode moveForward = KeyCode.W;
        
        [Tooltip("向后移动")]
        public KeyCode moveBackward = KeyCode.S;
        
        [Tooltip("向左移动")]
        public KeyCode moveLeft = KeyCode.A;
        
        [Tooltip("向右移动")]
        public KeyCode moveRight = KeyCode.D;
        
        [Tooltip("向上移动")]
        public KeyCode moveUp = KeyCode.E;
        
        [Tooltip("向下移动")]
        public KeyCode moveDown = KeyCode.Q;
        
        [Tooltip("快速移动修饰键")]
        public KeyCode fastMovement = KeyCode.LeftShift;
        
        [Header("系统功能")]
        [Tooltip("全屏切换")]
        public KeyCode toggleFullscreen = KeyCode.F11;
        
        [Tooltip("截图")]
        public KeyCode screenshot = KeyCode.F12;
        
        [Tooltip("VR模式切换")]
        public KeyCode toggleVR = KeyCode.V;
        
        [Header("高级设置")]
        [Tooltip("是否启用输入")]
        public bool enableInput = true;
        
        [Tooltip("是否启用摄像机移动控制")]
        public bool enableCameraMovement = true;
        
        [Tooltip("鼠标灵敏度")]
        public float mouseSensitivity = 2.0f;
        
        [Tooltip("移动速度")]
        public float movementSpeed = 10.0f;
        
        [Tooltip("快速移动倍数")]
        public float fastMovementMultiplier = 3.0f;        [Header("组件引用")]
        [SerializeField] private MMDVR.Scripts.Components.SceneItems.CameraComponent freeCameraController;
        [SerializeField] private ToggleMouseButton toggleMouseButton;

        // 公共属性
        public bool IsControllingMovement => enableCameraMovement && enableInput;
        
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
            if (!enableInput) return;
            
            CheckKeyboardInput();
            
            // 处理摄像机移动
            if (enableCameraMovement && freeCameraController != null)
            {
                HandleCameraMovement();
            }
        }

        /// <summary>
        /// 检查键盘输入
        /// </summary>
        private void CheckKeyboardInput()
        {
            // Tab - 切换鼠标控制模式
            if (UnityEngine.Input.GetKeyDown(toggleMouseMode))
            {
                if (toggleMouseButton != null)
                {
                    toggleMouseButton.OnToggleMouseClicked();
                }
            }
            
            // H - 切换UI显示/隐藏
            if (UnityEngine.Input.GetKeyDown(toggleUI))
            {
                OnToggleUI?.Invoke();
            }
            
            // Space - 播放/暂停
            if (UnityEngine.Input.GetKeyDown(playPause))
            {
                OnPlayPause?.Invoke();
            }
            
            // S - 停止 (需要按住Ctrl)
            bool stopKeyPressed = UnityEngine.Input.GetKeyDown(stop);
            bool ctrlPressed = UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl);
            if (stopKeyPressed && (!stopRequireCtrl || ctrlPressed))
            {
                OnStop?.Invoke();
            }
            
            // R - 重置相机
            if (UnityEngine.Input.GetKeyDown(resetCamera))
            {
                OnResetCamera?.Invoke();
                if (freeCameraController != null)
                {
                    freeCameraController.ResetToTransform();
                }
            }
            
            // F12 - 截图
            if (UnityEngine.Input.GetKeyDown(screenshot))
            {
                OnScreenshot?.Invoke();
            }
            
            // F11 - 全屏
            if (UnityEngine.Input.GetKeyDown(toggleFullscreen))
            {
                OnToggleFullscreen?.Invoke();
            }
            
            // V - 切换VR模式
            if (UnityEngine.Input.GetKeyDown(toggleVR))
            {
                OnToggleVR?.Invoke();
            }
        }

        /// <summary>
        /// 处理摄像机移动
        /// </summary>
        private void HandleCameraMovement()
        {
            if (freeCameraController == null || !freeCameraController.IsActive) return;

            var move = Vector3.zero;
            if (UnityEngine.Input.GetKey(moveForward)) move.z += 1;
            if (UnityEngine.Input.GetKey(moveBackward)) move.z -= 1;
            if (UnityEngine.Input.GetKey(moveLeft)) move.x -= 1;
            if (UnityEngine.Input.GetKey(moveRight)) move.x += 1;
            if (UnityEngine.Input.GetKey(moveUp)) move.y += 1;
            if (UnityEngine.Input.GetKey(moveDown)) move.y -= 1;

            float currentSpeed = movementSpeed;
            if (UnityEngine.Input.GetKey(fastMovement))
            {
                currentSpeed *= fastMovementMultiplier;
            }

            freeCameraController.transform.Translate(move.normalized * currentSpeed * Time.deltaTime);
        }
    }
}
