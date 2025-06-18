using System;
using UnityEngine;

namespace MMDVR.Managers
{
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

        // path: 模型文件路径
        public static Action<string> OnModelLoadRequest;        // 模型列表变更事件
        public static Action OnActorListChanged;

        // 相机列表变更事件
        public static Action OnCameraListChanged;

        // 动作列表变更事件
        public static Action OnMotionListChanged;

        // 音乐列表变更事件
        public static Action OnMusicListChanged;
        
        // 模型-动作关联变更事件
        public static Action OnModelMotionAssociationChanged;

        // 相机激活事件
        public static Action<MMDVR.Scripts.UIInteraction.CameraData> OnCameraActivated;        // 通用事件触发方法
        public void TriggerEvent(string eventName)
        {
            switch (eventName)
            {
                case "MusicListUpdated":
                    OnMusicListChanged?.Invoke();
                    break;
                case "CameraListUpdated":
                    OnCameraListChanged?.Invoke();
                    break;
                case "ActorListUpdated":
                case "ActorListChanged":
                    OnActorListChanged?.Invoke();
                    break;
                case "MotionListUpdated":
                case "MotionListChanged":
                    OnMotionListChanged?.Invoke();
                    break;
                case "ModelMotionAssociationChanged":
                    OnModelMotionAssociationChanged?.Invoke();
                    break;
                case "MusicActivated":
                case "PlaybackStateChanged":
                case "PlaybackTimeChanged":
                    // 播放控制相关事件
                    break;
            }
        }
    }
}
