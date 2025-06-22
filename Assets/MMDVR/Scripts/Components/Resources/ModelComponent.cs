using UnityEngine;
using MMDVR.Scripts.Model; // For IResourceInfo and ResourceType

namespace MMDVR.Scripts.Components
{
    /// <summary>
    /// 模型组件 - 存储模型相关信息，状态与ActorComponent同步
    /// </summary>
    public class ModelComponent : MonoBehaviour, IResourceInfo
    {
        [Header("模型标识")]
        public string id; // 统一使用id字段
        public string displayName;
        public string filePath;

        [Header("模型状态")]
        public bool isLoaded = false;
        
        // 移除了本地状态管理，改为与ActorComponent同步
        private ActorComponent _associatedActor;
          /// <summary>
        /// 关联的ActorComponent
        /// </summary>
        public ActorComponent AssociatedActor        { 
            get => _associatedActor;
            set 
            {
                if (_associatedActor != value)
                {
                    _associatedActor = value;
                    // 建立关联
                    if (value != null)
                    {
                        value.SetModelComponent(this);
                    }
                }
            }
        }/// <summary>
        /// 模型是否可见 - 通过GameObject的activeInHierarchy状态
        /// </summary>
        public bool IsVisible 
        { 
            get => gameObject.activeInHierarchy;
            set 
            {
                gameObject.SetActive(value);
            }
        }

        /// <summary>
        /// 模型是否启用 - 从关联的ActorComponent获取
        /// </summary>
        public bool IsEnabled 
        { 
            get => _associatedActor != null ? _associatedActor.IsEnabled : true;
            set 
            {
                if (_associatedActor != null)
                {
                    _associatedActor.IsEnabled = value;
                }
            }
        }

        /// <summary>
        /// 模型可见性变化事件
        /// </summary>
        public System.Action<ModelComponent, bool> OnVisibilityChanged;
        
        /// <summary>
        /// 模型启用状态变化事件
        /// </summary>
        public System.Action<ModelComponent, bool> OnEnabledStateChanged;        [Header("模型数据")]
        public Renderer[] renderers;        // 向后兼容属性
        public string ID => id;
        public string DisplayName => displayName;

        // IResourceInfo 接口实现
        public string FilePath => filePath;
        public ResourceType Type => ResourceType.Model;

        private void Awake()
        {
            if (string.IsNullOrEmpty(id))
                id = System.Guid.NewGuid().ToString("N")[..8];
            
            if (string.IsNullOrEmpty(displayName))
                displayName = gameObject.name;
        }        /// <summary>
        /// 设置模型可见性（内部方法，不触发事件）
        /// </summary>
        private void ApplyVisibility(bool visible)
        {
            if (renderers != null)
            {
                foreach (var renderer in renderers)
                {
                    if (renderer != null)
                        renderer.enabled = visible;
                }
            }
            else
            {
                // 如果没有缓存渲染器，尝试在当前GameObject中查找
                var localRenderers = GetComponentsInChildren<Renderer>();
                foreach (var renderer in localRenderers)
                {
                    if (renderer != null)
                        renderer.enabled = visible;
                }
            }
        }/// <summary>
        /// 设置模型可见性（公共方法，会触发事件）
        /// </summary>
        public void SetVisibility(bool visible)
        {
            IsVisible = visible; // 使用属性，会同步到ActorComponent
        }        /// <summary>
        /// 缓存渲染器组件
        /// </summary>
        public void CacheRenderers()
        {
            renderers = GetComponentsInChildren<Renderer>();
        }

        /// <summary>
        /// 设置模型对象（用于建立关联和缓存渲染器）
        /// </summary>
        public void SetupModelReferences(GameObject model)
        {
            if (model != null)
            {
                CacheRenderers();
                isLoaded = true;
                Debug.Log($"ModelComponent[{id}]: 建立模型引用并缓存渲染器");
            }
        }
    }
}
