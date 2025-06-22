using UnityEngine;

namespace MMDVR.Scripts.Components
{
    /// <summary>
    /// 音乐组件 - 存储音乐相关信息和AudioSource
    /// </summary>
    public class MusicComponent : MonoBehaviour
    {
        [Header("音乐标识")]
        public string musicId;
        public string displayName;
        public string filePath;        [Header("音乐状态")]
        public bool isLoaded = false;
        
        private bool _isActive = false;
        /// <summary>
        /// 音乐是否处于激活状态（正在播放）
        /// 注意：这里监听的是行为状态，而不是存在性
        /// </summary>
        public bool isActive 
        { 
            get => _isActive;
            private set 
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnActiveStateChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// 音乐激活状态变化事件 - UI可以监听此事件
        /// </summary>
        public System.Action<MusicComponent, bool> OnActiveStateChanged;

        [Header("音频组件")]
        public AudioSource audioSource;
        public AudioClip audioClip;

        private void Awake()
        {
            if (string.IsNullOrEmpty(musicId))
                musicId = System.Guid.NewGuid().ToString("N")[..8];
            
            if (string.IsNullOrEmpty(displayName))
                displayName = gameObject.name;

            // 确保有AudioSource组件
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        /// <summary>
        /// 设置音频剪辑
        /// </summary>
        public void SetAudioClip(AudioClip clip)
        {
            audioClip = clip;
            if (audioSource != null)
            {
                audioSource.clip = clip;
                isLoaded = (clip != null);
            }
        }        /// <summary>
        /// 播放音乐 - 这里管理行为状态
        /// </summary>
        public void Play()
        {
            if (audioSource != null && audioClip != null)
            {
                audioSource.Play();
                isActive = true; // 这会触发OnActiveStateChanged事件
            }
        }

        /// <summary>
        /// 暂停音乐 - 这里管理行为状态
        /// </summary>
        public void Pause()
        {
            if (audioSource != null)
            {
                audioSource.Pause();
                isActive = false; // 这会触发OnActiveStateChanged事件
            }
        }

        /// <summary>
        /// 停止音乐 - 这里管理行为状态
        /// </summary>
        public void Stop()
        {
            if (audioSource != null)
            {
                audioSource.Stop();
                isActive = false; // 这会触发OnActiveStateChanged事件
            }
        }

        /// <summary>
        /// 设置播放时间
        /// </summary>
        public void SetTime(float time)
        {
            if (audioSource != null && audioClip != null)
            {
                audioSource.time = Mathf.Clamp(time, 0f, audioClip.length);
            }
        }

        /// <summary>
        /// 获取播放时间
        /// </summary>
        public float GetTime()
        {
            return audioSource != null ? audioSource.time : 0f;
        }

        /// <summary>
        /// 获取音乐长度
        /// </summary>
        public float GetDuration()
        {
            return audioClip != null ? audioClip.length : 0f;
        }
    }
}
