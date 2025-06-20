using System;

namespace MMDVR.Events
{
    /// <summary>
    /// 场景展示事件 - 处理场景中对象的展示、控制等事件
    /// </summary>
    public static class SceneDisplayEvents
    {
        // ==================== 演员展示事件 ====================
        /// <summary>演员生成事件 (actorId, modelId)</summary>
        public static Action<string, string> OnActorSpawned;
        
        /// <summary>演员销毁事件 (actorId)</summary>
        public static Action<string> OnActorDestroyed;
        
        /// <summary>演员可见性变化事件 (actorId, isVisible)</summary>
        public static Action<string, bool> OnActorVisibilityChanged;
        
        // ==================== 音乐播放事件 ====================
        /// <summary>音乐激活事件 (musicId)</summary>
        public static Action<string> OnMusicActivated;
        
        /// <summary>音乐停止事件 (musicId)</summary>
        public static Action<string> OnMusicStopped;
        
        // ==================== 摄像机控制事件 ====================
        /// <summary>摄像机激活事件 (cameraId)</summary>
        public static Action<string> OnCameraActivated;
        
        /// <summary>摄像机状态应用事件 (cameraId)</summary>
        public static Action<string> OnCameraStateApplied;
        
        // ==================== 关联管理事件 ====================
        /// <summary>模型-动作关联变更事件 (modelId, motionId, isAssociated)</summary>
        public static Action<string, string, bool> OnModelMotionAssociationChanged;
        
        /// <summary>演员列表变更事件</summary>
        public static Action OnActorListChanged;
        
        // ==================== 便捷触发方法 ====================
        public static void TriggerActorSpawned(string actorId, string modelId)
        {
            OnActorSpawned?.Invoke(actorId, modelId);
        }
        
        public static void TriggerActorDestroyed(string actorId)
        {
            OnActorDestroyed?.Invoke(actorId);
        }
        
        public static void TriggerActorVisibilityChanged(string actorId, bool isVisible)
        {
            OnActorVisibilityChanged?.Invoke(actorId, isVisible);
        }
        
        public static void TriggerMusicActivated(string musicId)
        {
            OnMusicActivated?.Invoke(musicId);
        }
        
        public static void TriggerMusicStopped(string musicId)
        {
            OnMusicStopped?.Invoke(musicId);
        }
        
        public static void TriggerCameraActivated(string cameraId)
        {
            OnCameraActivated?.Invoke(cameraId);
        }
        
        public static void TriggerCameraStateApplied(string cameraId)
        {
            OnCameraStateApplied?.Invoke(cameraId);
        }
        
        // ==================== 关联管理事件触发方法 ====================
        public static void TriggerModelMotionAssociationChanged(string modelId, string motionId, bool isAssociated)
        {
            OnModelMotionAssociationChanged?.Invoke(modelId, motionId, isAssociated);
        }
        
        public static void TriggerActorListChanged()
        {
            OnActorListChanged?.Invoke();
        }

        /// <summary>
        /// 清除所有场景展示事件订阅（用于场景清理）
        /// </summary>
        public static void ClearAllEvents()
        {
            OnActorSpawned = null;
            OnActorDestroyed = null;
            OnActorVisibilityChanged = null;
            OnMusicActivated = null;
            OnMusicStopped = null;
            OnCameraActivated = null;
            OnCameraStateApplied = null;
            OnModelMotionAssociationChanged = null;
            OnActorListChanged = null;
        }
    }
}
