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
        [Tooltip("是否正在播放")] private bool _isPlaying;
        public bool isPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying == value) return;
                // 只允许通过Play/Pause方法切换播放状态，防止递归和状态错乱
                if (value)
                {
                    Play();
                }
                else
                {
                    Pause();
                }
            }
        }
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

        [Header("音频播放")]
        [Tooltip("音频播放源 - 手动绑定用于播放音乐")] public AudioSource musicAudioSource;

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
            
            // 初始化播放状态
            playTime = 0f;
            totalDuration = 0f;
            isPlaying = false;
            
            // 初始化同步设置
            if (enableFrameLimit)            {
                frameTimeThreshold = 1f / maxFrameRate;
            }
            
            Debug.Log("PlaybackManager: 初始化完成");
        }
        
        private void Update()
        {
            // 播放状态驱动链路优化：
            // 1. 若isPlaying为true但音乐未播放，强制调用Play()
            // 2. 保证playTime、audioSource.time、动作三者同步
            if (isPlaying)
            {
                // 帧率限制，防止更新过于频繁导致抖动
                if (enableFrameLimit && Time.time - lastSyncTime < frameTimeThreshold)
                    return;
                lastSyncTime = Time.time;

                var audioSource = sceneDisplayManager?.GetActiveMusicAudioSource();
                if (syncMode == SyncMode.SyncWithAudio && audioSource != null && audioSource.clip != null)
                {
                    // 若未播放则强制播放
                    if (!audioSource.isPlaying)
                    {
                        audioSource.time = playTime;
                        audioSource.Play();
                    }
                    // 保证playTime与audioSource.time同步
                    playTime = audioSource.time;
                    totalDuration = audioSource.clip.length;
                }
                else if (syncMode == SyncMode.SyncWithGame)
                {
                    float deltaTime = Time.deltaTime * playSpeed;
                    playTime += deltaTime;
                }
                // 手动模式不自动递增

                // 检查播放结束
                if (playTime >= totalDuration && totalDuration > 0)
                {
                    Pause();
                }

                // 同步所有组件
                SyncToTime(playTime, hardUpdate: false);

                // 触发时间更新事件
                PlaybackEvents.TriggerPlaybackTimeChanged(playTime);
            }
            else
            {
                // 若isPlaying为false但音乐还在播放，强制暂停
                var audioSource = sceneDisplayManager?.GetActiveMusicAudioSource();
                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSource.Pause();
                }
            }
        }
        // ===== 播放控制方法 =====
        
        public void Play()
        {
            // 防止递归调用
            if (_isPlaying) return;
            _isPlaying = true;
            Debug.Log("PlaybackManager: Play() called");
            
            if (sceneDisplayManager == null)
            {
                Debug.LogError("PlaybackManager: SceneDisplayManager is null, cannot play");
                return;
            }
            
            if (resourceManager == null)
            {
                Debug.LogError("PlaybackManager: ResourceManager is null, cannot play");
                return;
            }
            
            Debug.Log($"PlaybackManager: 当前激活音乐ID: '{sceneDisplayManager.currentActiveMusicId}'");
            
            // 修复：如果currentActiveMusicId无效，自动选择第一个可用音乐
            if (string.IsNullOrEmpty(sceneDisplayManager.currentActiveMusicId))
            {
                string firstMusicId = sceneDisplayManager.GetAndActivateFirstAvailableMusic();
                if (string.IsNullOrEmpty(firstMusicId))
                {
                    Debug.LogWarning("没有可用的音乐资源");
                }
                else
                {
                    Debug.Log($"自动激活音乐: {firstMusicId}");
                }
            }

            // 修复：如果currentActiveCameraId无效，自动选择第一个可用摄像机
            if (string.IsNullOrEmpty(sceneDisplayManager.currentActiveCameraId))
            {
                string firstCameraId = sceneDisplayManager.GetAndActivateFirstAvailableCamera();
                Debug.Log($"自动激活摄像机: {firstCameraId}");
            }            // 尝试播放当前激活的音乐
            if (!string.IsNullOrEmpty(sceneDisplayManager.currentActiveMusicId))
            {
                Debug.Log($"PlaybackManager: 尝试获取音乐组件: {sceneDisplayManager.currentActiveMusicId}");
                var musicComponent = resourceManager.GetMusic(sceneDisplayManager.currentActiveMusicId);
                AudioSource audioSourceToUse = sceneDisplayManager.GetActiveMusicAudioSource();
                if (audioSourceToUse == null)
                {
                    audioSourceToUse = musicAudioSource;
                }
                if (musicComponent != null && audioSourceToUse != null && musicComponent.audioClip != null)
                {
                    if (audioSourceToUse.clip != musicComponent.audioClip)
                        audioSourceToUse.clip = musicComponent.audioClip;
                    audioSourceToUse.time = playTime;
                    if (!audioSourceToUse.isPlaying)
                        audioSourceToUse.Play();
                    totalDuration = musicComponent.audioClip.length;
                }
            }
            else
            {
                // 没有音乐也可以播放（仅播放动画和摄像机）
                isPlaying = true;
                totalDuration = 300f; // 默认5分钟时长
                Debug.Log($"无音乐播放开始 - playTime: {playTime}, totalDuration: {totalDuration}");
            }// 同步所有演员动作的播放状态
            try
            {
                UpdateAllActorMotionStates(true);
                  // 播放时设置所有MMD对象物理模式为Bullet
                foreach (var actorData in sceneDisplayManager.actorList)
                {
                    Transform actorTransform = sceneDisplayManager.actorContainer.Find($"Actor_{actorData.id}");
                    if (actorTransform != null)
                    {
                        var mmdGameObject = actorTransform.GetComponent<LibMMD.Unity3D.MmdGameObject>();
                        if (mmdGameObject != null)
                        {
                            mmdGameObject.PhysicsMode = LibMMD.Unity3D.MmdGameObject.PhysicsModeEnum.Bullet;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"PlaybackManager: Error in UpdateAllActorMotionStates: {ex.Message}");
                Debug.LogError(ex.StackTrace);
            }
              // 触发播放状态事件
            PlaybackEvents.TriggerPlaybackStateChanged(isPlaying);
        }        public void Pause()
        {
            // 防止递归调用
            if (!_isPlaying) return;
            _isPlaying = false;
            if (sceneDisplayManager == null) return;

            // 暂停当前激活的音乐
            if (!string.IsNullOrEmpty(sceneDisplayManager.currentActiveMusicId))
            {
                // 优先使用SceneDisplayManager的AudioSource，如果没有则使用PlaybackManager的
                AudioSource audioSourceToUse = sceneDisplayManager.GetActiveMusicAudioSource();
                if (audioSourceToUse == null)
                {
                    audioSourceToUse = musicAudioSource;
                }
                
                if (audioSourceToUse != null && audioSourceToUse.isPlaying)
                {
                    audioSourceToUse.Pause();
                    Debug.Log($"音乐暂停: {sceneDisplayManager.currentActiveMusicId}");
                }
            }
            
            Debug.Log("播放暂停");
            
            // 暂停所有演员动作
            PauseAllActorMotions();
              // 暂停时设置所有MMD对象物理模式为None
            foreach (var actorData in sceneDisplayManager.actorList)
            {
                Transform actorTransform = sceneDisplayManager.actorContainer.Find($"Actor_{actorData.id}");
                if (actorTransform != null)
                {
                    var mmdGameObject = actorTransform.GetComponent<LibMMD.Unity3D.MmdGameObject>();
                    if (mmdGameObject != null)
                    {
                        mmdGameObject.PhysicsMode = LibMMD.Unity3D.MmdGameObject.PhysicsModeEnum.None;
                    }
                }
            }
              // 触发播放状态事件
            PlaybackEvents.TriggerPlaybackStateChanged(isPlaying);
        }
          public void Stop()
        {
            if (sceneDisplayManager == null) return;
            
            // 停止音乐 - 优先使用SceneDisplayManager的AudioSource，如果没有则使用PlaybackManager的
            AudioSource audioSourceToUse = sceneDisplayManager.GetActiveMusicAudioSource();
            if (audioSourceToUse == null)
            {
                audioSourceToUse = musicAudioSource;
            }
            
            if (audioSourceToUse != null)
            {
                audioSourceToUse.Stop();
            }

            isPlaying = false;
            playTime = 0f;
            
            // 重置所有演员动作
            UpdateAllActorMotionStates(false);
            
            Debug.Log("播放停止");
            PlaybackEvents.TriggerPlaybackStateChanged(isPlaying);
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
            
            // 优先使用SceneDisplayManager的AudioSource，如果没有则使用PlaybackManager的
            AudioSource audioSourceToUse = sceneDisplayManager.GetActiveMusicAudioSource();
            if (audioSourceToUse == null)
            {
                audioSourceToUse = musicAudioSource;
            }
            
            if (audioSourceToUse != null && audioSourceToUse.clip != null)
            {
                audioSourceToUse.time = playTime;
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

            // 优先使用SceneDisplayManager的AudioSource，如果没有则使用PlaybackManager的
            AudioSource audioSourceToUse = sceneDisplayManager.GetActiveMusicAudioSource();
            if (audioSourceToUse == null)
            {
                audioSourceToUse = musicAudioSource;
            }
            
            if (audioSourceToUse != null && audioSourceToUse.clip != null)
            {
                return audioSourceToUse.clip.length;
            }
            
            return 0f;
        }
        
        public void SetMusicVolume(float volume)
        {
            if (sceneDisplayManager == null) return;

            // 优先使用SceneDisplayManager的AudioSource，如果没有则使用PlaybackManager的
            AudioSource audioSourceToUse = sceneDisplayManager.GetActiveMusicAudioSource();
            if (audioSourceToUse == null)
            {
                audioSourceToUse = musicAudioSource;
            }
              if (audioSourceToUse != null)
            {
                audioSourceToUse.volume = volume;
            }
            PlaybackEvents.TriggerVolumeChanged(volume);
        }

        // ===== 动作同步方法 =====
          private void UpdateAllActorMotionStates(bool play)
        {
            if (sceneDisplayManager == null) 
            {
                Debug.LogWarning("PlaybackManager: sceneDisplayManager is null");
                return;
            }

            var actorList = sceneDisplayManager.GetActorList();
            if (actorList == null || actorList.Count == 0)
            {
                Debug.Log("PlaybackManager: No actors to update");
                return;
            }

            foreach (var actor in actorList)
            {
                if (actor == null)
                {
                    Debug.LogWarning("PlaybackManager: Actor is null, skipping");
                    continue;
                }

                var actorObj = sceneDisplayManager.GetActorGameObject(actor.id);
                if (actorObj == null)
                {
                    Debug.LogWarning($"PlaybackManager: Actor GameObject not found for ID: {actor.id}");
                    continue;
                }                var mmdGameObject = actorObj.GetComponent<LibMMD.Unity3D.MmdGameObject>();
                if (mmdGameObject == null)
                {
                    Debug.LogWarning($"PlaybackManager: MmdGameObject component not found on actor: {actor.id}");
                    continue;
                }

                // 额外检查MmdGameObject是否有必需的数据
                if (!IsValidMmdGameObject(mmdGameObject))
                {
                    Debug.LogWarning($"PlaybackManager: MmdGameObject not valid for actor: {actor.id}");
                    continue;
                }try
                {
                    mmdGameObject.Playing = play;
                    
                    if (play) // If playing, also sync time
                    {
                        // 添加额外的安全检查
                        if (!float.IsNaN(playTime) && !float.IsInfinity(playTime) && playTime >= 0)
                        {
                            // 检查MmdGameObject是否已经正确初始化
                            if (mmdGameObject.gameObject != null && mmdGameObject.enabled)
                            {
                                mmdGameObject.SetMotionPos(playTime);
                                Debug.Log($"PlaybackManager: Set motion position {playTime} for actor {actor.id}");
                            }
                            else
                            {
                                Debug.LogWarning($"PlaybackManager: MmdGameObject not properly initialized for actor {actor.id}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"PlaybackManager: Invalid playTime value: {playTime}");
                        }
                    }
                }
                catch (System.NullReferenceException ex)
                {
                    Debug.LogError($"PlaybackManager: NullReferenceException for actor {actor.id}: {ex.Message}");
                    Debug.LogError($"PlaybackManager: Skipping actor {actor.id} due to null reference. This may indicate the MmdGameObject is not properly initialized.");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"PlaybackManager: Error updating motion state for actor {actor.id}: {ex.Message}");
                    Debug.LogError(ex.StackTrace);
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
        }        // ===== 同步功能（集成自PlaybackSynchronizer）=====
        
        /// <summary>
        /// 与音频同步 - 优先级最高的同步模式
        /// </summary>
        private void SyncWithAudio()
        {
            if (sceneDisplayManager == null) return;

            // 检查是否有活动音乐
            if (!string.IsNullOrEmpty(sceneDisplayManager.currentActiveMusicId))
            {
                float audioTime = GetCurrentAudioTime();
                Debug.Log($"SyncWithAudio: audioTime={audioTime}, playTime={playTime}");
                
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
                Debug.Log("SyncWithAudio: 没有活动音乐，回退到游戏时间同步");
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
            SyncAudio(targetTime, hardUpdate);            SyncMotions(targetTime, hardUpdate);
            SyncCamera(targetTime, hardUpdate);
        }
        
        /// <summary>
        /// 获取当前音频时间
        /// </summary>
        private float GetCurrentAudioTime()
        {
            if (sceneDisplayManager == null || string.IsNullOrEmpty(sceneDisplayManager.currentActiveMusicId))
                return 0f;

            var audioSource = sceneDisplayManager.GetActiveMusicAudioSource();
            if (audioSource != null && audioSource.clip != null)
            {
                return audioSource.time;            }
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
            if (adjustedTime < 0) adjustedTime = 0;
            
            var audioSource = sceneDisplayManager?.GetActiveMusicAudioSource();
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
                    }                }
            }
        }
        
        /// <summary>
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
        }        /// <summary>        /// <summary>
        /// 检查指定音乐是否正在播放
        /// </summary>
        public bool IsPlayingMusic(string musicId)
        {
            // 简单实现：检查是否在播放状态且音乐ID匹配
            // 在实际实现中，您可能需要跟踪当前播放的音乐ID
            if (sceneDisplayManager == null || string.IsNullOrEmpty(musicId))
            {
                return false;
            }
            
            // 检查当前激活的音乐ID是否匹配且正在播放
            return isPlaying && sceneDisplayManager.currentActiveMusicId == musicId;
        }
        
        /// <summary>
        /// 停止音乐播放
        /// </summary>
        public void StopMusic()
        {
            Stop(); // 使用现有的停止方法
        }

        /// <summary>
        /// 检查MmdGameObject是否有效且可以播放
        /// </summary>
        /// <param name="mmdGameObject">要检查的MmdGameObject</param>
        /// <returns>如果有效返回true，否则返回false</returns>
        private bool IsValidMmdGameObject(LibMMD.Unity3D.MmdGameObject mmdGameObject)
        {
            if (mmdGameObject == null)
            {
                return false;
            }

            // 检查GameObject是否有效
            if (mmdGameObject.gameObject == null)
            {
                return false;
            }

            // 检查组件是否启用
            if (!mmdGameObject.enabled)
            {
                return false;
            }

            // 检查GameObject是否激活
            if (!mmdGameObject.gameObject.activeInHierarchy)
            {
                return false;
            }

            // 检查是否有模型数据
            try
            {
                // 如果有ModelName属性，检查是否不为空
                if (!string.IsNullOrEmpty(mmdGameObject.ModelName))
                {
                    return true;
                }
                
                // 备用检查：看是否有任何mesh renderer
                var meshRenderers = mmdGameObject.GetComponentsInChildren<MeshRenderer>();
                var skinnedMeshRenderers = mmdGameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                
                return meshRenderers.Length > 0 || skinnedMeshRenderers.Length > 0;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"PlaybackManager: Error checking MmdGameObject validity: {e.Message}");
                return false;
            }
        }

        private void OnDestroy()
        {
            // 取消事件订阅
            PlaybackEvents.OnPlayPauseToggle -= TogglePlayPause;
            PlaybackEvents.OnStopRequested -= Stop;
        }
    }
}
