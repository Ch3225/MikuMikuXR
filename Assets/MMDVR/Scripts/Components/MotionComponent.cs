using UnityEngine;

namespace MMDVR.Scripts.Components
{
    /// <summary>
    /// 动作组件 - 存储动作相关信息和状态
    /// </summary>
    public class MotionComponent : MonoBehaviour
    {
        [Header("动作标识")]
        public string motionId;
        public string displayName;
        public string filePath;

        [Header("动作状态")]
        public bool isLoaded = false;
        public bool isPlaying = false;

        [Header("关联信息")]
        public string assignedActorId;
        public string assignedModelId;

        [Header("动作数据")]
        public AnimationClip animationClip;
        public float duration = 0f;
        public float currentTime = 0f;

        private void Awake()
        {
            if (string.IsNullOrEmpty(motionId))
                motionId = System.Guid.NewGuid().ToString("N")[..8];
            
            if (string.IsNullOrEmpty(displayName))
                displayName = gameObject.name;
        }

        /// <summary>
        /// 设置动画剪辑
        /// </summary>
        public void SetAnimationClip(AnimationClip clip)
        {
            animationClip = clip;
            if (clip != null)
            {
                duration = clip.length;
                isLoaded = true;
            }
            else
            {
                duration = 0f;
                isLoaded = false;
            }
        }

        /// <summary>
        /// 关联到演员/模型
        /// </summary>
        public void AssignToActor(string actorId, string modelId = null)
        {
            assignedActorId = actorId;
            if (!string.IsNullOrEmpty(modelId))
                assignedModelId = modelId;
        }

        /// <summary>
        /// 取消关联
        /// </summary>
        public void UnassignFromActor()
        {
            assignedActorId = null;
            assignedModelId = null;
        }

        /// <summary>
        /// 开始播放
        /// </summary>
        public void StartPlayback()
        {
            isPlaying = true;
            currentTime = 0f;
        }

        /// <summary>
        /// 暂停播放
        /// </summary>
        public void PausePlayback()
        {
            isPlaying = false;
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        public void StopPlayback()
        {
            isPlaying = false;
            currentTime = 0f;
        }

        /// <summary>
        /// 设置播放时间
        /// </summary>
        public void SetPlaybackTime(float time)
        {
            currentTime = Mathf.Clamp(time, 0f, duration);
        }

        /// <summary>
        /// 更新播放状态
        /// </summary>
        public void UpdatePlayback(float deltaTime)
        {
            if (isPlaying && isLoaded)
            {
                currentTime += deltaTime;
                if (currentTime >= duration)
                {
                    currentTime = duration;
                    isPlaying = false;
                }
            }
        }
    }
}
