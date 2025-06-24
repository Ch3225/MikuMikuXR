using UnityEngine;
using System;
using System.Collections.Generic;
using MMDVR.Scripts.Managers;
using LibMMD.Unity3D;
using MMDVR.Scripts.Model; // 引用正确的命名空间

namespace MMDVR.Scripts.Components
{
    /// <summary>
    /// 演员组件 - 统一的演员管理组件
    /// 合并了原来的ActorComponent和SceneActorComponent的功能
    /// 负责管理场景中的演员实例，包括属性监听、数据管理和事件处理
    /// </summary>
    public class ActorComponent : MonoBehaviour, INotifyPropertyChanged
    {        [Header("演员标识")]
        [SerializeField] private string _actorId;
        [SerializeField] private string _displayName;

        [Header("演员数据")]
        public ActorData actorData;

        // 运行时缓存的MmdGameObject引用
        private MmdGameObject _mmdGameObject;
        public MmdGameObject MmdGameObject 
        { 
            get 
            { 
                if (_mmdGameObject == null)
                    _mmdGameObject = GetComponent<MmdGameObject>();
                return _mmdGameObject;
            }
        }

        // 属性变化事件
        public static event Action<ActorComponent, string, object, object> OnPropertyChanged;
        
        // Actor特定事件
        public static event Action<string, string, string> OnActorMotionChanged; // actorId, oldMotionId, newMotionId
        public static event Action<string, bool> OnActorEnabledChanged; // actorId, isEnabled
        public static event Action<string, bool> OnActorVisibilityChanged; // actorId, isVisible

        /// <summary>
        /// Actor ID
        /// </summary>
        public string ActorId 
        { 
            get => _actorId; 
            set 
            {
                if (_actorId != value)
                {
                    SetProperty(ref _actorId, value, nameof(ActorId));
                    if (actorData != null) actorData.id = value;
                }
            }
        }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName 
        { 
            get => _displayName; 
            set 
            {
                if (_displayName != value)
                {
                    SetProperty(ref _displayName, value, nameof(DisplayName));
                    if (actorData != null) actorData.displayName = value;
                }
            }
        }

        /// <summary>
        /// 当前激活的动作ID（由AssociationManager管理）
        /// </summary>
        public string CurrentMotionId
        {
            get
            {
                if (AssociationManager.Instance != null && !string.IsNullOrEmpty(ActorId))
                {
                    var motions = AssociationManager.Instance.GetModelAssociatedMotions(ActorId);
                    return motions.Count > 0 ? motions[0] : null;
                }
                return null;
            }
            set
            {
                if (AssociationManager.Instance != null && !string.IsNullOrEmpty(ActorId))
                {
                    var oldValue = CurrentMotionId;
                    if (oldValue != value)
                    {
                        // 先移除旧的
                        if (!string.IsNullOrEmpty(oldValue))
                            AssociationManager.Instance.DisassociateModelFromMotion(ActorId, oldValue);
                        // 添加新的
                        if (!string.IsNullOrEmpty(value))
                            AssociationManager.Instance.AssociateModelWithMotion(ActorId, value);
                        OnActorMotionChanged?.Invoke(_actorId, oldValue, value);
                        LoadMotionToMMD(value);
                        if (actorData != null)
                            actorData.motionIds = AssociationManager.Instance.GetModelAssociatedMotions(ActorId);
                    }
                }
            }
        }

        /// <summary>
        /// 是否启用 - 通过GameObject的激活状态判断
        /// </summary>
        public bool IsEnabled 
        { 
            get => gameObject.activeInHierarchy; 
            set 
            {
                if (gameObject.activeInHierarchy != value)
                {
                    OnActorEnabledChanged?.Invoke(_actorId, value);
                    gameObject.SetActive(value);
                }
            }
        }// 兼容性属性（小写）- 可读写
        public string actorId 
        { 
            get => ActorId; 
            set => ActorId = value; 
        }
        public string displayName 
        { 
            get => DisplayName; 
            set => DisplayName = value; 
        }

        /// <summary>
        /// 设置属性并触发变化事件的通用方法
        /// </summary>
        private void SetProperty<T>(ref T field, T value, string propertyName)
        {
            var oldValue = field;
            field = value;
            
            OnPropertyChanged?.Invoke(this, propertyName, oldValue, value);
            
            Debug.Log($"ActorComponent [{_actorId}]: {propertyName} changed from {oldValue} to {value}");
        }

        private void Awake()
        {
            // 初始化显示名称
            if (string.IsNullOrEmpty(_displayName))
                _displayName = gameObject.name;

            // 如果ActorId为空，使用GameObject名称作为ID
            if (string.IsNullOrEmpty(_actorId))
                _actorId = gameObject.name;

            // 初始化演员数据
            if (actorData == null)
            {
                actorData = new ActorData
                {
                    id = _actorId,
                    displayName = _displayName,
                    modelId = null, // 由AssociationManager管理
                    motionIds = AssociationManager.Instance != null ? AssociationManager.Instance.GetModelAssociatedMotions(_actorId) : new List<string>(),
                    isVisible = gameObject.activeInHierarchy
                };
            }
        }

        /// <summary>
        /// 添加动作ID到列表
        /// </summary>
        public void AddMotion(string motionId)
        {
            if (!string.IsNullOrEmpty(motionId) && AssociationManager.Instance != null && !AssociationManager.Instance.GetModelAssociatedMotions(_actorId).Contains(motionId))
            {
                AssociationManager.Instance.AssociateModelWithMotion(_actorId, motionId);
                if (actorData != null) actorData.motionIds = AssociationManager.Instance.GetModelAssociatedMotions(_actorId);
            }
        }

        /// <summary>
        /// 移除动作ID
        /// </summary>
        public void RemoveMotion(string motionId)
        {
            if (AssociationManager.Instance != null)
            {
                AssociationManager.Instance.DisassociateModelFromMotion(_actorId, motionId);
                if (actorData != null) actorData.motionIds = AssociationManager.Instance.GetModelAssociatedMotions(_actorId);
            }
        }

        /// <summary>
        /// 清空所有动作
        /// </summary>
        public void ClearMotions()
        {
            if (AssociationManager.Instance != null)
            {
                AssociationManager.Instance.ClearModelAssociations(_actorId);
                if (actorData != null) actorData.motionIds = new List<string>();
            }
        }

        /// <summary>
        /// 获取当前所有动作ID列表
        /// </summary>
        public List<string> GetMotionIds()
        {
            if (AssociationManager.Instance != null)
                return AssociationManager.Instance.GetModelAssociatedMotions(_actorId);
            return new List<string>();
        }        /// <summary>
        /// 设置可见性（内部方法）
        /// </summary>
        private void SetVisibilityInternal(bool visible)
        {
            // 查找关联的模型组件
            var modelComponent = GetComponentInChildren<ModelComponent>();
            if (modelComponent != null)
            {
                // 如果有模型组件，控制模型的可见性
                modelComponent.gameObject.SetActive(visible);
            }
            else if (MmdGameObject != null)
            {
                // 直接控制MMD对象的可见性
                MmdGameObject.gameObject.SetActive(visible);
            }
            else
            {
                // fallback: 控制整个GameObject
                gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 设置激活状态
        /// </summary>
        public void SetActive(bool active)
        {
            IsEnabled = active;
        }        /// <summary>
        /// 切换可见性
        /// </summary>
        public void ToggleVisibility()
        {
            bool currentVisibility = gameObject.activeInHierarchy;
            SetVisibilityInternal(!currentVisibility);
        }

        /// <summary>
        /// 实际加载动作到MMD对象
        /// </summary>
        private void LoadMotionToMMD(string motionId)
        {
            if (string.IsNullOrEmpty(motionId))
            {
                // 重置为T姿势
                var mmdGameObject = MmdGameObject;
                if (mmdGameObject != null)
                {
                    mmdGameObject.ResetToTPose();
                    Debug.Log($"ActorComponent [{_actorId}]: Reset to T-Pose");
                }
                return;
            }

            // 从ResourceManager获取动作组件
            if (ResourceManager.Instance != null)
            {
                var motionComponent = ResourceManager.Instance.GetMotionComponentById(motionId);
                if (motionComponent != null && !string.IsNullOrEmpty(motionComponent.filePath))
                {
                    var mmdGameObject = MmdGameObject;
                    if (mmdGameObject != null)
                    {
                        mmdGameObject.LoadMotion(motionComponent.filePath);
                        Debug.Log($"ActorComponent [{_actorId}]: Loaded motion {motionId} from {motionComponent.filePath}");
                    }
                }
            }
        }        /// <summary>
        /// 初始化组件
        /// </summary>
        void Start()
        {
            // 自动设置SkinnedMeshRenderer.UpdateWhenOffscreen = true
            if (MmdGameObject != null)
            {
                var renderers = MmdGameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var r in renderers)
                {
                    r.updateWhenOffscreen = true;
                }
            }
            
            // 初始化时应用当前状态 - 使用GameObject当前状态
            SetVisibilityInternal(gameObject.activeInHierarchy);
            
            // 加载当前动作（如果有）
            if (!string.IsNullOrEmpty(CurrentMotionId))
            {
                LoadMotionToMMD(CurrentMotionId);
            }
        }

        /// <summary>
        /// 外部调用：更新关联的动作（通常由AssociationManager调用）
        /// </summary>
        public void UpdateAssociatedMotion(string motionId)
        {
            CurrentMotionId = motionId; // 这会触发属性变化事件
        }

        /// <summary>
        /// 外部调用：切换启用状态
        /// </summary>
        public void ToggleEnabled()
        {
            IsEnabled = !IsEnabled; // 这会触发属性变化事件
        }

        /// <summary>
        /// 获取演员数据副本
        /// </summary>
        public ActorData GetActorData()
        {
            return new ActorData
            {
                id = actorData.id,
                displayName = actorData.displayName,
                modelId = actorData.modelId,
                motionIds = new List<string>(actorData.motionIds),
                isVisible = actorData.isVisible
            };
        }

        /// <summary>
        /// 兼容性方法：获取关联的动作ID（旧接口）
        /// </summary>
        public string AssociatedMotionId 
        { 
            get => CurrentMotionId; 
            set => CurrentMotionId = value;
        }
    }

    /// <summary>
    /// 通用的属性变化通知接口
    /// </summary>
    public interface INotifyPropertyChanged
    {
        // 标记接口，用于识别支持属性变化通知的组件
    }
}
