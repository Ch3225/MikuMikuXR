using System;
using System.Collections.Generic;
using UnityEngine;

namespace MMDVR.Scripts.Input
{
    /// <summary>
    /// 简单的键盘输入管理器 - 只处理基本的键盘快捷键
    /// </summary>
    public class KeyboardInputManager : MonoBehaviour
    {
        [Header("键盘快捷键设置")]
        [SerializeField] private bool enableKeyboardInput = true;
        
        // 键盘快捷键事件
        public static event Action OnToggleUI;
        public static event Action OnPlayPause;
        public static event Action OnStop;
        public static event Action OnResetCamera;
        public static event Action OnScreenshot;
        public static event Action OnToggleFullscreen;
        
        void Update()
        {
            if (!enableKeyboardInput) return;
            
            CheckKeyboardInput();
        }
          /// <summary>
        /// 检查键盘输入
        /// </summary>
        private void CheckKeyboardInput()
        {
            // Tab - 切换UI
            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab))
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
        }
        
        /// <summary>
        /// 启用/禁用键盘输入
        /// </summary>
        public void SetKeyboardInputEnabled(bool enabled)
        {
            enableKeyboardInput = enabled;
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
        }
          void Start()
        {
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
        }
    }
}
