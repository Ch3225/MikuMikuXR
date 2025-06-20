using System;

namespace MMDVR.Events
{
    /// <summary>
    /// 资源管理事件 - 处理资源加载、卸载等生命周期事件
    /// </summary>
    public static class ResourceEvents
    {
        // ==================== 资源生命周期事件 ====================
        /// <summary>资源加载完成事件 (resourceType, resourceId)</summary>
        public static Action<string, string> OnResourceLoaded;
        
        /// <summary>资源卸载完成事件 (resourceType, resourceId)</summary>
        public static Action<string, string> OnResourceUnloaded;
        
        /// <summary>资源加载失败事件 (resourceType, resourcePath, errorMessage)</summary>
        public static Action<string, string, string> OnResourceLoadFailed;
        
        /// <summary>资源缓存状态变化事件</summary>
        public static Action<int> OnResourceCacheChanged;
        
        // ==================== 资源列表变更事件 ====================
        /// <summary>模型列表变更事件</summary>
        public static Action OnModelListChanged;
        
        /// <summary>动作列表变更事件</summary>
        public static Action OnMotionListChanged;
        
        /// <summary>音乐列表变更事件</summary>
        public static Action OnMusicListChanged;
        
        /// <summary>摄像机列表变更事件</summary>
        public static Action OnCameraListChanged;
        
        /// <summary>模型加载请求事件</summary>
        public static Action<string> OnModelLoadRequest;
        
        /// <summary>动作加载请求事件</summary>
        public static Action<string> OnMotionLoadRequest;
        
        /// <summary>音乐加载请求事件</summary>
        public static Action<string> OnMusicLoadRequest;
        
        /// <summary>摄像机加载请求事件</summary>
        public static Action<string> OnCameraLoadRequest;

        // ==================== 便捷触发方法 ====================
        public static void TriggerResourceLoaded(string resourceType, string resourceId)
        {
            OnResourceLoaded?.Invoke(resourceType, resourceId);
        }
        
        public static void TriggerResourceUnloaded(string resourceType, string resourceId)
        {
            OnResourceUnloaded?.Invoke(resourceType, resourceId);
        }
        
        public static void TriggerResourceLoadFailed(string resourceType, string resourcePath, string errorMessage)
        {
            OnResourceLoadFailed?.Invoke(resourceType, resourcePath, errorMessage);
        }
        
        public static void TriggerResourceCacheChanged(int cacheCount)
        {
            OnResourceCacheChanged?.Invoke(cacheCount);
        }
        
        public static void TriggerModelListChanged()
        {
            OnModelListChanged?.Invoke();
        }
        
        public static void TriggerMotionListChanged()
        {
            OnMotionListChanged?.Invoke();
        }
        
        public static void TriggerMusicListChanged()
        {
            OnMusicListChanged?.Invoke();
        }
        
        public static void TriggerCameraListChanged()
        {
            OnCameraListChanged?.Invoke();
        }
        
        public static void TriggerModelLoadRequest(string modelPath)
        {
            OnModelLoadRequest?.Invoke(modelPath);
        }

        public static void TriggerMotionLoadRequest(string motionPath)
        {
            OnMotionLoadRequest?.Invoke(motionPath);
        }

        public static void TriggerMusicLoadRequest(string musicPath)
        {
            OnMusicLoadRequest?.Invoke(musicPath);
        }

        public static void TriggerCameraLoadRequest(string cameraPath)
        {
            OnCameraLoadRequest?.Invoke(cameraPath);
        }

        /// <summary>
        /// 清除所有资源事件订阅（用于场景清理）
        /// </summary>
        public static void ClearAllEvents()
        {
            OnResourceLoaded = null;
            OnResourceUnloaded = null;
            OnResourceLoadFailed = null;
            OnResourceCacheChanged = null;
            OnModelListChanged = null;
            OnMotionListChanged = null;
            OnMusicListChanged = null;
            OnCameraListChanged = null;
            OnModelLoadRequest = null;
            OnMotionLoadRequest = null;
            OnMusicLoadRequest = null;
            OnCameraLoadRequest = null;
        }
    }
}
