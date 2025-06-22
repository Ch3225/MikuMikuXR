using UnityEngine;
using MMDVR.Scripts.Events;
using MMDVR.Scripts.Managers;
using MMDVR.Scripts.Components;

namespace MMDVR.Scripts.Managers
{
    /// <summary>
    /// 同步模式
    /// </summary>
    public enum SyncMode
    {
        SyncWithAudio,      // 与音频同步
        SyncWithGame,       // 与游戏帧同步
        ManualControl       // 手动控制
    }    /// <summary>
    /// 播放状态管理器 - 专门负责播放控制、进度管理、同步等
    /// </summary>
    public class PlaybackManager : MonoBehaviour
    {
        public static PlaybackManager Instance { get; private set; }
        
        [Header("播放状态")]
        [Tooltip("是否正在播放")] public bool isPlaying;
        [Tooltip("当前播放进度（秒）")] [Range(0, 9999)] public float playTime;
        [Tooltip("播放时长（秒）")] public float totalDuration;

        [Header("同步设置")]
        [Tooltip("同步模式")] public SyncMode syncMode = SyncMode.SyncWithAudio;
        [Tooltip("播放速度")] [Range(0.1f, 3.0f)] public float playSpeed = 1.0f;
        [Tooltip("是否启用帧率限制")] public bool enableFrameLimit = true;
        [Tooltip("最大帧率(防止抖动)")] public int maxFrameRate = 60;
        
        [Header("延迟补偿")]
        [Tooltip("音频延迟(秒)")] public float audioDelay = 0f;
        [Tooltip("动作延迟(秒)")] public float motionDelay = 0f;
        [Tooltip("摄像机延迟(秒)")] public float cameraDelay = 0f;

        private ResourceManager resourceManager;
        private SceneDisplayManager sceneDisplayManager;
        
        // 同步相关私有字段
        private float lastSyncTime = 0f;
        private float frameTimeThreshold = 1f / 60f; // 60fps阈值

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);            // 订阅播放事件
            PlaybackEvents.OnPlayPauseToggle += TogglePlayPause;
            PlaybackEvents.OnStopRequested += Stop;
        }
        
        private void Start()
        {
            // 获取Manager引用
            resourceManager = ResourceManager.Instance;
            sceneDisplayManager = SceneDisplayManager.Instance;
            
            if (resourceManager == null)
            {
                Debug.LogError("PlaybackManager: 找不到ResourceManager实例");
            }
            
            if (sceneDisplayManager == null)
            {
                Debug.LogError("PlaybackManager: 找不到SceneDisplayManager实例");
            }
            
            // 初始化同步设置
            if (enableFrameLimit)            {
                frameTimeThreshold = 1f / maxFrameRate;
            }
        }
        
        private void Update()
        {
            if (!isPlaying) return;
            
            // 帧率限制，防止更新过于频繁导致抖动
            if (enableFrameLimit && Time.time - lastSyncTime < frameTimeThreshold)
                return;
                
            lastSyncTime = Time.time;
            
            // 执行同步逻辑
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
            
            // 检查播放结束
            if (playTime >= totalDuration && totalDuration > 0)
            {
                Pause();
            }

            // 触发时间更新事件
            PlaybackEvents.TriggerPlaybackTimeChanged(playTime);
        }

        // ===== 播放控制方法 =====
        
        public void Play()
        {
            if (sceneDisplayManager == null) return;

            // 修复：如果currentActiveMusicId无效，自动选择第一个可用音乐
            if (string.IsNullOrEmpty(sceneDisplayManager.currentActiveMusicId))
            {
                string firstMusicId = sceneDisplayManager.GetAndActivateFirstAvailableMusic();
                if (string.IsNullOrEmpty(firstMusicId))
                {
                    Debug.LogWarning("没有可用的音乐资源");
                }
            }
            
            var audioSource = sceneDisplayManager.GetActiveMusicAudioSource();
            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.time = playTime;
                audioSource.Play();
                totalDuration = audioSource.clip.length;
                isPlaying = true;
                Debug.Log("播放开始");
            }
            else
            {
                // 没有音乐也可以播放（仅播放动画和摄像机）
                isPlaying = true;
                Debug.Log("无音乐播放开始");
            }
            
            // 同步所有演员动作的播放状态
            UpdateAllActorMotionStates(true);
              // 触发播放状态事件
            PlaybackEvents.TriggerPlaybackStateChanged(isPlaying);
        }
        
        public void Pause()
        {
            if (sceneDisplayManager == null) return;

            var audioSource = sceneDisplayManager.GetActiveMusicAudioSource();
            if (audioSource != null)
            {
                audioSource.Pause();
            }
            
            isPlaying = false;
            Debug.Log("播放暂停");
            
            // 暂停所有演员动作
            PauseAllActorMotions();
              // 触发播放状态事件
            PlaybackEvents.TriggerPlaybackStateChanged(isPlaying);
        }
        
        public void Stop()
        {
            if (sceneDisplayManager == null) return;
            
            // 停止音乐
            var audioSource = sceneDisplayManager.GetActiveMusicAudioSource();
            if (audioSource != null)
            {
                audioSource.Stop();
            }

            isPlaying = false;
            playTime = 0f;
            
            // 重置所有演员动作
            UpdateAllActorMotionStates(false);
            
            Debug.Log("播放停止");            PlaybackEvents.TriggerPlaybackStateChanged(isPlaying);
        }

        public void TogglePlayPause()
        {
            if (isPlaying)
                Pause();
            else
                Play();
        }
        
        public void SeekTo(float time)
        {
            if (sceneDisplayManager == null) return;

            playTime = Mathf.Clamp(time, 0f, totalDuration);
              var audioSource = sceneDisplayManager.GetActiveMusicAudioSource();
            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.time = playTime;
            }
            
            // 同步所有演员动作时间
            UpdateAllActorMotionStates(isPlaying);
            
            Debug.Log($"跳转到时间: {playTime:F2}s");
            PlaybackEvents.TriggerPlaybackTimeChanged(playTime);
        }

        // ===== 音乐控制方法 =====
        
        public float GetMusicDuration()
        {
            if (sceneDisplayManager == null) return 0f;

            var audioSource = sceneDisplayManager.GetActiveMusicAudioSource();
            if (audioSource != null && audioSource.clip != null)
            {
                return audioSource.clip.length;
            }
              return 0f;
        }
        
        public void SetMusicVolume(float volume)
        {
            if (sceneDisplayManager == null) return;

            var audioSource = sceneDisplayManager.GetActiveMusicAudioSource();
            if (audioSource != null)
            {
                audioSource.volume = volume;
            }            PlaybackEvents.TriggerVolumeChanged(volume);
        }

        // ===== 动作同步方法 =====
        
        private void UpdateAllActorMotionStates(bool play)
        {
            if (sceneDisplayManager == null) return;

            var actorList = sceneDisplayManager.GetActorList();
            foreach (var actor in actorList)
            {
                var actorObj = sceneDisplayManager.GetActorGameObject(actor.id);
                if (actorObj != null)
                {
                    var mmdGameObject = actorObj.GetComponent<LibMMD.Unity3D.MmdGameObject>();
                    if (mmdGameObject != null)
                    {
                        mmdGameObject.Playing = play;                        if (play) // If playing, also sync time
                        {
                            mmdGameObject.SetMotionPos(playTime);
                        }
                    }
                }
            }
        }
        
        private void PauseAllActorMotions()
        {
            if (sceneDisplayManager == null) return;

            var actorList = sceneDisplayManager.GetActorList();
            foreach (var actor in actorList)
            {
                var actorObj = sceneDisplayManager.GetActorGameObject(actor.id);
                if (actorObj != null)
                {
                    var mmdGameObject = actorObj.GetComponent<LibMMD.Unity3D.MmdGameObject>();
                    if (mmdGameObject != null)
                    {
                        mmdGameObject.Playing = false;
                    }
                }
            }        }

        // ===== VMD摄像机更新 =====
        
        private void UpdateVMDCamera()
        {
            if (sceneDisplayManager == null || string.IsNullOrEmpty(sceneDisplayManager.currentActiveCameraId)) 
                return;

            if (sceneDisplayManager.currentActiveCameraId == "BUILTIN_FREE_CAMERA")
                return; // Free Camera不需要更新
                
            // 获取VMD摄像机并应用当前时间的状态
            var cameraData = resourceManager?.GetCamera(sceneDisplayManager.currentActiveCameraId);
            if (cameraData != null)
            {
                var cameraComponent = cameraData.GetComponent<MMDCameraComponent>();
                if (cameraComponent != null && cameraComponent.vmdCameraData != null)
                {
                    // 更新VMD摄像机位置和朝向
                    cameraComponent.ApplyAtTime(playTime);
                }
            }
        }

        // ===== 同步功能（集成自PlaybackSynchronizer）=====        /// <summary>
        /// 与音频同步 - 优先级最高的同步模式
        /// </summary>
        private void SyncWithAudio()
        {
            if (sceneDisplayManager == null) return;

            // 检查是否有活动音乐
            if (!string.IsNullOrEmpty(sceneDisplayManager.currentActiveMusicId))
            {
                float audioTime = GetCurrentAudioTime();
                
                if (Mathf.Abs(audioTime - playTime) > 0.1f) // 如果音频和系统时间差距超过0.1秒
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
            float newTime = playTime + deltaTime;
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
            playTime = targetTime;
            
            // 同步所有组件
            SyncAudio(targetTime, hardUpdate);
            SyncMotions(targetTime, hardUpdate);
            SyncCamera(targetTime, hardUpdate);
        }        /// <summary>
        /// 获取当前音频时间
        /// </summary>
        private float GetCurrentAudioTime()
        {
            if (sceneDisplayManager == null || string.IsNullOrEmpty(sceneDisplayManager.currentActiveMusicId))
                return 0f;

            var audioSource = sceneDisplayManager.GetActiveMusicAudioSource();
            if (audioSource != null && audioSource.clip != null)
            {
                return audioSource.time;
            }
              return 0f;
        }

        /// <summary>
        /// 同步音频
        /// </summary>
        private void SyncAudio(float time, bool hardUpdate)
        {
            if (syncMode == SyncMode.SyncWithAudio && !hardUpdate)
                return; // 音频同步模式下不需要调整音频时间

            float adjustedTime = time - audioDelay;
            if (adjustedTime < 0) adjustedTime = 0;            var audioSource = sceneDisplayManager?.GetActiveMusicAudioSource();
            if (audioSource != null && audioSource.clip != null)
            {
                float currentAudioTime = audioSource.time;
                if (hardUpdate || Mathf.Abs(currentAudioTime - adjustedTime) > 0.1f)
                {
                    audioSource.time = adjustedTime;
                }
            }
        }

        /// <summary>
        /// 同步动作
        /// </summary>
        private void SyncMotions(float time, bool hardUpdate)
        {
            if (sceneDisplayManager == null) return;

            float adjustedTime = time - motionDelay;
            if (adjustedTime < 0) adjustedTime = 0;

            // 同步所有演员的动作
            var actorList = sceneDisplayManager.GetActorList();
            foreach (var actor in actorList)
            {
                var actorObj = sceneDisplayManager.GetActorGameObject(actor.id);
                if (actorObj != null)
                {
                    var mmdGameObject = actorObj.GetComponent<LibMMD.Unity3D.MmdGameObject>();
                    if (mmdGameObject != null)
                    {
                        if (hardUpdate) // 只有强制同步时才设置动作进度
                        {
                            mmdGameObject.SetMotionPos(adjustedTime);
                        }
                        mmdGameObject.Playing = isPlaying;
                    }
                }
            }
        }        /// <summary>
        /// 同步摄像机
        /// </summary>
        private void SyncCamera(float time, bool hardUpdate)
        {
            if (sceneDisplayManager == null || resourceManager == null) return;

            float adjustedTime = time - cameraDelay;
            if (adjustedTime < 0) adjustedTime = 0;
            
            // 更新VMD摄像机（如果有的话）
            if (!string.IsNullOrEmpty(sceneDisplayManager.currentActiveCameraId) && 
                sceneDisplayManager.currentActiveCameraId != "BUILTIN_FREE_CAMERA")
            {
                var cameraData = resourceManager.GetCamera(sceneDisplayManager.currentActiveCameraId);
                if (cameraData != null)
                {
                    var mmdCameraComponent = cameraData.GetComponent<MMDCameraComponent>();
                    if (mmdCameraComponent != null && mmdCameraComponent.vmdCameraData != null)
                    {
                        var cameraState = mmdCameraComponent.vmdCameraData.GetCameraStateAtTime(adjustedTime);
                        if (cameraState != null)
                        {
                            sceneDisplayManager.ApplyCameraState(cameraState);
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

        // ==================== 音乐管理方法 ====================
        
        /// <summary>
        /// 设置活动音乐
        /// </summary>
        public void SetActiveMusic(string musicId)
        {
            if (resourceManager == null)
                resourceManager = ResourceManager.Instance;
                
            var musicComponent = resourceManager.GetMusic(musicId);
            if (musicComponent != null)
            {
                Debug.Log($"PlaybackManager: 设置活动音乐 {musicId}");
                // 这里可以添加设置当前播放音乐的逻辑
                // 例如：currentMusicId = musicId;
            }
            else
            {
                Debug.LogError($"PlaybackManager: 音乐组件未找到 {musicId}");
            }
        }

        /// <summary>
        /// 检查指定音乐是否正在播放
        /// </summary>
        public bool IsPlayingMusic(string musicId)
        {
            // 简单实现：检查是否在播放状态且音乐ID匹配
            // 在实际实现中，您可能需要跟踪当前播放的音乐ID
            return isPlaying; // 这里可以添加更具体的逻辑
        }

        /// <summary>
        /// 停止音乐播放
        /// </summary>
        public void StopMusic()
        {
            Stop(); // 使用现有的停止方法
        }

        private void OnDestroy()
        {
            // 取消事件订阅
            PlaybackEvents.OnPlayPauseToggle -= TogglePlayPause;
            PlaybackEvents.OnStopRequested -= Stop;
        }
    }
}
