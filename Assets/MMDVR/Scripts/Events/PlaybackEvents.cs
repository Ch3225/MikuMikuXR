using UnityEngine;
using System;

namespace MMDVR.Scripts.Events
{
    /// <summary>
    /// 播放相关事件定义
    /// </summary>
    public static class PlaybackEvents
    {        // 播放状态事件
        public static event Action<bool> OnPlaybackStateChanged;
        public static event Action<float> OnPlaybackTimeChanged;
        public static event Action<float> OnPlaybackDurationChanged;
        public static event Action OnPlayPauseToggle;
        public static event Action OnStopRequested;
        
        // 资源状态事件
        public static event Action<string> OnMusicActivated;
        public static event Action<string> OnCameraActivated;
        public static event Action<string, string> OnMotionAssigned; // motionId, actorId
        
        // 音量控制事件
        public static event Action<float> OnVolumeChanged;
        public static event Action<bool> OnMuteStateChanged;
        
        // 同步事件
        public static event Action OnSyncRequired;
        
        // 触发事件的方法
        public static void TriggerPlaybackStateChanged(bool isPlaying)
        {
            OnPlaybackStateChanged?.Invoke(isPlaying);
        }
        
        public static void TriggerPlaybackTimeChanged(float time)
        {
            OnPlaybackTimeChanged?.Invoke(time);
        }
        
        public static void TriggerPlaybackDurationChanged(float duration)
        {
            OnPlaybackDurationChanged?.Invoke(duration);
        }
        
        public static void TriggerMusicActivated(string musicId)
        {
            OnMusicActivated?.Invoke(musicId);
        }
        
        public static void TriggerCameraActivated(string cameraId)
        {
            OnCameraActivated?.Invoke(cameraId);
        }
        
        public static void TriggerMotionAssigned(string motionId, string actorId)
        {
            OnMotionAssigned?.Invoke(motionId, actorId);
        }
        
        public static void TriggerVolumeChanged(float volume)
        {
            OnVolumeChanged?.Invoke(volume);
        }
        
        public static void TriggerMuteStateChanged(bool isMuted)
        {
            OnMuteStateChanged?.Invoke(isMuted);
        }
          public static void TriggerSyncRequired()
        {
            OnSyncRequired?.Invoke();
        }
        
        public static void TriggerPlayPauseToggle()
        {
            OnPlayPauseToggle?.Invoke();
        }
        
        public static void TriggerStopRequested()
        {
            OnStopRequested?.Invoke();
        }
          /// <summary>
        /// 清除所有事件订阅（用于场景清理）
        /// </summary>
        public static void ClearAllEvents()
        {
            OnPlaybackStateChanged = null;
            OnPlaybackTimeChanged = null;
            OnPlaybackDurationChanged = null;
            OnPlayPauseToggle = null;
            OnStopRequested = null;
            OnMusicActivated = null;
            OnCameraActivated = null;
            OnMotionAssigned = null;
            OnVolumeChanged = null;
            OnMuteStateChanged = null;
            OnSyncRequired = null;
        }
    }
}
