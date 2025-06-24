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
        public string filePath;        [Header("动作状态")]
        
        private bool _isPlaying = false;
        /// <summary>
        /// 动作是否正在播放 - 这是行为状态
        /// </summary>
        public bool isPlaying 
        { 
            get => _isPlaying;
            set 
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnPlayStateChanged?.Invoke(this, value);
                }
            }
        }

        [Header("关联信息")]
        private string _assignedActorId;
        /// <summary>
        /// 分配给的Actor ID - 关联状态变化可以被监听
        /// </summary>
        public string assignedActorId 
        { 
            get => _assignedActorId;
            set 
            {
                if (_assignedActorId != value)
                {
                    var oldActorId = _assignedActorId;
                    _assignedActorId = value;
                    OnActorAssignmentChanged?.Invoke(this, oldActorId, value);
                }
            }
        }

        private string _assignedModelId;
        /// <summary>
        /// 分配给的Model ID - 关联状态变化可以被监听
        /// </summary>
        public string assignedModelId 
        { 
            get => _assignedModelId;
            set 
            {
                if (_assignedModelId != value)
                {
                    var oldModelId = _assignedModelId;
                    _assignedModelId = value;
                    OnModelAssignmentChanged?.Invoke(this, oldModelId, value);
                }
            }
        }

        /// <summary>
        /// 播放状态变化事件
        /// </summary>
        public System.Action<MotionComponent, bool> OnPlayStateChanged;
        
        /// <summary>
        /// Actor关联变化事件
        /// </summary>
        public System.Action<MotionComponent, string, string> OnActorAssignmentChanged; // (component, oldActorId, newActorId)
        
        /// <summary>
        /// Model关联变化事件
        /// </summary>
        public System.Action<MotionComponent, string, string> OnModelAssignmentChanged; // (component, oldModelId, newModelId)

        private void Awake()
        {
            if (string.IsNullOrEmpty(motionId))
                motionId = System.Guid.NewGuid().ToString("N")[..8];
            
            if (string.IsNullOrEmpty(displayName))
                displayName = gameObject.name;
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
        }

        /// <summary>
        /// 更新播放状态
        /// </summary>
        public void UpdatePlayback(float deltaTime)
        {
            if (isPlaying)
            {
                
            }
        }
    }
}
