using UnityEngine;
using System.Collections.Generic;
using MMDVR.Managers;
using MMDVR.Components; // Added for MMDCameraComponent

namespace MMDVR.Scripts.Synchronization
{
    /// <summary>
    /// 播放同步器 - 负责音频、动作、摄像机的精确同步
    /// 基于原始mmd2timeline的ProgressHelper和同步机制
    /// </summary>
    public class PlaybackSynchronizer : MonoBehaviour
    {
        [Header("同步设置")]
        [Tooltip("同步模式")] public SyncMode syncMode = SyncMode.SyncWithAudio;
        [Tooltip("播放速度")] [Range(0.1f, 3.0f)] public float playSpeed = 1.0f;
        [Tooltip("是否启用帧率限制")] public bool enableFrameLimit = true;
        [Tooltip("最大帧率(防止抖动)")] public int maxFrameRate = 60;
        
        [Header("延迟补偿")]
        [Tooltip("音频延迟(秒)")] public float audioDelay = 0f;
        [Tooltip("动作延迟(秒)")] public float motionDelay = 0f;
        [Tooltip("摄像机延迟(秒)")] public float cameraDelay = 0f;
        
        private SceneStatesManager sceneManager;
        private float lastSyncTime = 0f;
        private float frameTimeThreshold = 1f / 60f; // 60fps阈值
        
        public enum SyncMode
        {
            SyncWithAudio,      // 与音频同步
            SyncWithGame,       // 与游戏帧同步
            ManualControl       // 手动控制
        }
        
        private void Awake()
        {
            sceneManager = SceneStatesManager.Instance;
            if (enableFrameLimit)
            {
                frameTimeThreshold = 1f / maxFrameRate;
            }
        }
        
        private void Update()
        {
            if (sceneManager == null || !sceneManager.isPlaying) return;
            
            // 帧率限制，防止更新过于频繁导致抖动
            if (enableFrameLimit && Time.time - lastSyncTime < frameTimeThreshold)
                return;
                
            lastSyncTime = Time.time;
            
            switch (syncMode)
            {
                case SyncMode.SyncWithAudio:
                    SyncWithAudio();
                    break;
                case SyncMode.SyncWithGame:
                    SyncWithGameTime();
                    break;
                case SyncMode.ManualControl:
                    // 手动控制模式不进行自动同步
                    break;
            }
        }
        
        /// <summary>
        /// 与音频同步 - 优先级最高的同步模式
        /// </summary>
        private void SyncWithAudio()
        {
            // Original: if (sceneManager.IsPlayingMusic())
            if (sceneManager.isPlaying && !string.IsNullOrEmpty(sceneManager.currentActiveMusicId))
            {
                // Original: float audioTime = sceneManager.GetCurrentMusicTime();
                float audioTime = 0f;
                Transform musicObj = sceneManager.musicContainer.Find($"Music_{sceneManager.currentActiveMusicId}");
                if (musicObj != null)
                {
                    var audioSource = musicObj.GetComponent<AudioSource>();
                    if (audioSource != null && audioSource.clip != null)
                    {
                        audioTime = audioSource.time;
                    }
                }

                if (Mathf.Abs(audioTime - sceneManager.playTime) > 0.1f) // 如果音频和系统时间差距超过0.1秒
                {
                    // 强制同步到音频时间
                    SyncToTime(audioTime, hardUpdate: true);
                }
                else
                {
                    // 正常同步
                    SyncToTime(audioTime, hardUpdate: false);
                }
            }
            else
            {
                // 没有音频时回退到游戏时间同步
                SyncWithGameTime();
            }
        }
        
        /// <summary>
        /// 与游戏时间同步
        /// </summary>
        private void SyncWithGameTime()
        {
            float deltaTime = Time.deltaTime * playSpeed;
            float newTime = sceneManager.playTime + deltaTime;
            SyncToTime(newTime, hardUpdate: false);
        }
        
        /// <summary>
        /// 同步到指定时间
        /// </summary>
        /// <param name="targetTime">目标时间</param>
        /// <param name="hardUpdate">是否强制更新</param>
        public void SyncToTime(float targetTime, bool hardUpdate = false)
        {
            // 更新系统时间
            sceneManager.playTime = targetTime;
            
            // 同步所有组件
            SyncAudio(targetTime, hardUpdate);
            SyncMotions(targetTime, hardUpdate);
            SyncCamera(targetTime, hardUpdate);
        }
        
        /// <summary>
        /// 同步音频
        /// </summary>
        private void SyncAudio(float time, bool hardUpdate)
        {
            if (syncMode == SyncMode.SyncWithAudio && !hardUpdate)
                return; // 音频同步模式下不需要调整音频时间

            float adjustedTime = time - audioDelay;
            if (adjustedTime < 0) adjustedTime = 0;

            // Original: if (sceneManager.IsPlayingMusic())
            if (sceneManager.isPlaying && !string.IsNullOrEmpty(sceneManager.currentActiveMusicId))
            {
                // Original: float currentAudioTime = sceneManager.GetCurrentMusicTime();
                // Original: sceneManager.SetMusicTime(adjustedTime);
                Transform musicObj = sceneManager.musicContainer.Find($"Music_{sceneManager.currentActiveMusicId}");
                if (musicObj != null)
                {
                    var audioSource = musicObj.GetComponent<AudioSource>();
                    if (audioSource != null && audioSource.clip != null)
                    {
                        float currentAudioTime = audioSource.time;
                        if (hardUpdate || Mathf.Abs(currentAudioTime - adjustedTime) > 0.1f)
                        {
                            audioSource.time = adjustedTime;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 同步动作
        /// </summary>
        private void SyncMotions(float time, bool hardUpdate)
        {
            float adjustedTime = time - motionDelay;
            if (adjustedTime < 0) adjustedTime = 0;

            // 同步所有演员的动作
            for (int i = 0; i < sceneManager.actorContainer.childCount; i++)
            {
                Transform actorObj = sceneManager.actorContainer.GetChild(i);
                var actorComponent = actorObj.GetComponent<ActorComponent>();
                if (actorComponent != null && !string.IsNullOrEmpty(actorComponent.currentMotionId))
                {
                    var mmdGameObject = actorObj.GetComponent<LibMMD.Unity3D.MmdGameObject>();                    if (mmdGameObject != null)
                    {
                        // 使用LibMMD的精确时间设置，启用物理效果
                        mmdGameObject.SetMotionPos(adjustedTime); // SetMotionPos只接受一个参数
                        mmdGameObject.Playing = sceneManager.isPlaying;
                    }
                }
            }
        }
        
        /// <summary>
        /// 同步摄像机
        /// </summary>
        private void SyncCamera(float time, bool hardUpdate)
        {
            float adjustedTime = time - cameraDelay;
            if (adjustedTime < 0) adjustedTime = 0;
            
            // 更新VMD摄像机（如果有的话）
            if (!string.IsNullOrEmpty(sceneManager.currentActiveCameraId) && 
                sceneManager.currentActiveCameraId != "BUILTIN_FREE_CAMERA")
            {
                Transform mmdCamerasContainer = sceneManager.cameraContainer.Find("MMDCameras");
                if (mmdCamerasContainer != null)
                {
                    Transform cameraObj = mmdCamerasContainer.Find($"VMDCamera_{sceneManager.currentActiveCameraId}");
                    if (cameraObj != null)
                    {
                        var mmdCameraComponent = cameraObj.GetComponent<MMDCameraComponent>();
                        if (mmdCameraComponent != null && mmdCameraComponent.vmdCameraData != null)
                        {                            // 设置VMD摄像机播放位置
                            var cameraState = mmdCameraComponent.vmdCameraData.GetCameraStateAtTime(adjustedTime);
                            if (cameraState != null)
                            {
                                sceneManager.PublicApplyCameraState(cameraState); // 使用公共方法
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 设置播放速度
        /// </summary>
        public void SetPlaySpeed(float speed)
        {
            playSpeed = Mathf.Clamp(speed, 0.1f, 3.0f);
        }
        
        /// <summary>
        /// 设置同步模式
        /// </summary>
        public void SetSyncMode(SyncMode mode)
        {
            syncMode = mode;
        }
        
        /// <summary>
        /// 设置延迟补偿
        /// </summary>
        public void SetDelay(float audio, float motion, float camera)
        {
            audioDelay = audio;
            motionDelay = motion;
            cameraDelay = camera;
        }
    }
}
