using System;

namespace MMDVR.Events
{
    /// <summary>
    /// 输入事件管理 - 统一处理桌面和VR输入
    /// </summary>
    public static class InputEvents
    {
        public enum InputSource { Desktop, VR }
        
        // UI切换请求事件
        public static Action<InputSource> OnUIToggleRequested;
        
        // 输入设备状态变化事件
        public static Action<bool> OnVRDeviceStateChanged;
        
        // 自定义按键事件
        public static Action<string> OnCustomKeyPressed; // 通用自定义按键事件
        public static Action<InputSource, string> OnShortcutTriggered; // 快捷键事件
        
        // 输入模式变化事件
        public static Action<InputSource> OnInputModeChanged;
        
        // 触发事件的便捷方法
        public static void TriggerUIToggle(InputSource source)
        {
            OnUIToggleRequested?.Invoke(source);
        }
        
        public static void TriggerVRDeviceStateChanged(bool isActive)
        {
            OnVRDeviceStateChanged?.Invoke(isActive);
        }
        
        public static void TriggerCustomKeyPressed(string keyName)
        {
            OnCustomKeyPressed?.Invoke(keyName);
        }
        
        public static void TriggerShortcut(InputSource source, string shortcutName)
        {
            OnShortcutTriggered?.Invoke(source, shortcutName);
        }
        
        public static void TriggerInputModeChanged(InputSource source)
        {
            OnInputModeChanged?.Invoke(source);
        }        
        /// <summary>
        /// 清除所有输入事件订阅（用于场景清理）
        /// </summary>
        public static void ClearAllEvents()
        {
            OnUIToggleRequested = null;
            OnVRDeviceStateChanged = null;
            OnCustomKeyPressed = null;
            OnShortcutTriggered = null;
            OnInputModeChanged = null;
        }
    }
}
