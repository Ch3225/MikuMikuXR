using UnityEngine;

namespace MMDVR.Scripts.Components
{    /// <summary>
    /// 音乐组件 - 只存储音乐相关数据，不包含播放控制
    /// 播放控制由SceneDisplayManager的AudioSource负责
    /// </summary>
    public class MusicComponent : MonoBehaviour
    {
        [Header("音乐标识")]
        public string musicId;
        public string displayName;
        public string filePath;        private bool _isActive = false;
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
        public System.Action<MusicComponent, bool> OnActiveStateChanged;        private void Awake()
        {
            if (string.IsNullOrEmpty(musicId))
                musicId = System.Guid.NewGuid().ToString("N")[..8];
            
            if (string.IsNullOrEmpty(displayName))
                displayName = gameObject.name;
        }        /// <summary>
        /// 设置激活状态 - 由SceneDisplayManager调用
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
        }

        /// <summary>
        /// 获取音乐长度
        /// </summary>
        public float GetDuration()
        {
            return 0f;
        }
    }
}
