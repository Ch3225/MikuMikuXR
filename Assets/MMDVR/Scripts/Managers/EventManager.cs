using System;
using UnityEngine;
using MMDVR.Events;
using MMDVR.Scripts.Events;

namespace MMDVR.Scripts.Managers
{    /// <summary>
    /// 统一事件管理�?- 作为事件系统的中心协调器和向后兼容层
    /// 实际事件管理已分散到专门类中�?
    /// - InputEvents: 输入相关事件
    /// - ResourceEvents: 资源管理事件  
    /// - SceneDisplayEvents: 场景展示事件
    /// - UIEvents: UI交互事件
    /// - SystemEvents: 系统级事�?
    /// - PlaybackEvents: 播放控制事件
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }

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

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SystemEvents.TriggerApplicationPause();
        }

        void OnApplicationQuit()
        {
            SystemEvents.TriggerApplicationQuit();
        }        // ==================== 便捷方法 - 直接调用对应分类管理�?====================
        public static void TriggerUIToggle(InputSource source) => InputEvents.TriggerUIToggle((InputEvents.InputSource)(int)source);
        public static void TriggerActorListChanged() => SceneDisplayEvents.TriggerActorListChanged();
        public static void TriggerMotionListChanged() => ResourceEvents.TriggerMotionListChanged();
        public static void TriggerCameraListChanged() => ResourceEvents.TriggerCameraListChanged();
        public static void TriggerMusicListChanged() => ResourceEvents.TriggerMusicListChanged();
        public static void TriggerModelMotionAssociationChanged() => SceneDisplayEvents.TriggerModelMotionAssociationChanged("", "", false);

        // 兼容旧版本的字符串触发方法（逐步废弃�?
        [System.Obsolete("Use specific event classes (InputEvents, ResourceEvents, SceneDisplayEvents, etc.) instead")]
        public void TriggerEvent(string eventName)
        {
            switch (eventName)
            {
                case "MusicListUpdated":
                    ResourceEvents.TriggerMusicListChanged();
                    break;
                case "CameraListUpdated":
                    ResourceEvents.TriggerCameraListChanged();
                    break;
                case "ActorListUpdated":
                case "ActorListChanged":
                    SceneDisplayEvents.TriggerActorListChanged();
                    break;
                case "MotionListUpdated":
                case "MotionListChanged":
                    ResourceEvents.TriggerMotionListChanged();
                    break;                case "ModelMotionAssociationChanged":
                    // 需要传递参数，这里使用默认值
                    SceneDisplayEvents.TriggerModelMotionAssociationChanged("", "", true);
                    break;
                case "MusicActivated":
                case "PlaybackStateChanged":
                case "PlaybackTimeChanged":
                    // 播放控制相关事件已迁移到PlaybackEvents
                    break;
            }
        }        /// <summary>
        /// 清除所有事件订阅（用于场景清理）
        /// </summary>
        public static void ClearAllEvents()
        {
            InputEvents.ClearAllEvents();
            // ContentEvents已移除 - 功能已分散到ResourceEvents和SceneDisplayEvents
            ResourceEvents.ClearAllEvents();
            SceneDisplayEvents.ClearAllEvents();
            UIEvents.ClearAllEvents();
            SystemEvents.ClearAllEvents();
            PlaybackEvents.ClearAllEvents();
        }

        // ==================== 向后兼容的静态属性 ====================
        public enum InputSource { Desktop, VR }
        
        // 直接访问分类事件管理器的静态字段
        public static Action<string> OnModelLoadRequest
        {
            get => ResourceEvents.OnModelLoadRequest;
            set => ResourceEvents.OnModelLoadRequest = value;
        }
        
        public static Action OnActorListChanged
        {
            get => SceneDisplayEvents.OnActorListChanged;
            set => SceneDisplayEvents.OnActorListChanged = value;
        }
        
        public static Action OnMotionListChanged
        {
            get => ResourceEvents.OnMotionListChanged;
            set => ResourceEvents.OnMotionListChanged = value;
        }
          // 为不同签名的事件提供兼容性包装
        private static Action _onModelMotionAssociationChanged;
        public static Action OnModelMotionAssociationChanged
        {
            get => _onModelMotionAssociationChanged;
            set 
            {
                _onModelMotionAssociationChanged = value;
                // 连接到实际的事件
                if (value != null)
                {
                    SceneDisplayEvents.OnModelMotionAssociationChanged += (modelId, motionId, isAssociated) => value.Invoke();
                }
            }
        }
        
        public static Action OnCameraListChanged
        {
            get => ResourceEvents.OnCameraListChanged;
            set => ResourceEvents.OnCameraListChanged = value;
        }
          public static Action OnMusicListChanged
        {
            get => ResourceEvents.OnMusicListChanged;
            set => ResourceEvents.OnMusicListChanged = value;
        }
          // 相机激活事件 - 兼容不同的事件签名
        public static Action<MMDVR.Scripts.Model.CameraData> OnCameraActivated;
        
        // 音乐激活事件 - 兼容不同的事件签名  
        public static Action<MMDVR.Scripts.Components.MusicComponent> OnMusicActivated;
        
        // 模型动作关联/解除关联事件
        public static Action<string, string> OnModelMotionAssociated;
        public static Action<string, string> OnModelMotionDisassociated;
    }
}
