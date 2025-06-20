using System;

namespace MMDVR.Events
{
    /// <summary>
    /// UI状态事件 - 处理UI显示状态变化、模式切换等被动UI状态通知
    /// 注意：这里的事件主要用于UI状态同步和刷新，而不是用户操作触发
    /// 用户操作应该使用InputEvents，业务逻辑应该使用对应的业务事件（如PlaybackEvents）
    /// </summary>
    public static class UIEvents
    {
        // ==================== UI显示状态事件 ====================
        /// <summary>主UI显示/隐藏状态变化</summary>
        public static Action<bool> OnMainUIVisibilityChanged;
        
        /// <summary>UI面板切换完成</summary>
        public static Action<string> OnUIPanelSwitched;
        
        /// <summary>UI模式变化（VR/桌面模式切换完成）</summary>
        public static Action<bool> OnUIModeChanged;
        
        // ==================== UI状态同步事件 ====================
        /// <summary>UI锁定状态改变</summary>
        public static Action<bool> OnUILockStateChanged;
        
        /// <summary>UI错误信息需要显示</summary>
        public static Action<string> OnUIErrorOccurred;        
        // ==================== 状态通知方法 ====================
        public static void NotifyMainUIVisibilityChanged(bool isVisible)
        {
            OnMainUIVisibilityChanged?.Invoke(isVisible);
        }
        
        public static void NotifyUIPanelSwitched(string panelName)
        {
            OnUIPanelSwitched?.Invoke(panelName);
        }
        
        public static void NotifyUIModeChanged(bool isVRMode)
        {
            OnUIModeChanged?.Invoke(isVRMode);
        }
        
        public static void NotifyUILockStateChanged(bool isLocked)
        {
            OnUILockStateChanged?.Invoke(isLocked);
        }
        
        public static void NotifyUIErrorOccurred(string errorMessage)
        {
            OnUIErrorOccurred?.Invoke(errorMessage);
        }        
        /// <summary>
        /// 清除所有UI事件订阅（用于场景清理）
        /// </summary>
        public static void ClearAllEvents()
        {
            OnMainUIVisibilityChanged = null;
            OnUIPanelSwitched = null;
            OnUIModeChanged = null;
            OnUILockStateChanged = null;
            OnUIErrorOccurred = null;
        }
    }
}
