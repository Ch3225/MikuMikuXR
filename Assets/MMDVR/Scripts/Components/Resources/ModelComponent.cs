using UnityEngine;
using MMDVR.Scripts.Model; // For IResourceInfo and ResourceType

namespace MMDVR.Scripts.Components
{
    /// <summary>
    /// 模型组件 - 存储模型相关信息和状态
    /// </summary>
    public class ModelComponent : MonoBehaviour, IResourceInfo
    {        [Header("模型标识")]
        public string id; // 统一使用id字段
        public string displayName;
        public string filePath;[Header("模型状态")]
        public bool isLoaded = false;
        
        private bool _isVisible = true;
        /// <summary>
        /// 模型是否可见 - 这是行为状态，可以被监听
        /// </summary>
        public bool isVisible 
        { 
            get => _isVisible;
            set 
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    ApplyVisibility(value);
                    OnVisibilityChanged?.Invoke(this, value);
                }
            }
        }

        private bool _isEnabled = true;
        /// <summary>
        /// 模型是否启用 - 这是行为状态，可以被监听
        /// </summary>
        public bool isEnabled 
        { 
            get => _isEnabled;
            set 
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    gameObject.SetActive(value);
                    OnEnabledStateChanged?.Invoke(this, value);
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
        public System.Action<ModelComponent, bool> OnEnabledStateChanged;

        [Header("模型数据")]
        public GameObject modelObject;
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
            else if (modelObject != null)
            {
                modelObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 设置模型可见性（公共方法，会触发事件）
        /// </summary>
        public void SetVisibility(bool visible)
        {
            isVisible = visible; // 使用属性，会触发事件
        }

        /// <summary>
        /// 缓存渲染器组件
        /// </summary>
        public void CacheRenderers()
        {
            if (modelObject != null)
            {
                renderers = modelObject.GetComponentsInChildren<Renderer>();
            }
            else
            {
                renderers = GetComponentsInChildren<Renderer>();
            }
        }

        /// <summary>
        /// 设置模型对象
        /// </summary>
        public void SetModelObject(GameObject model)
        {
            modelObject = model;
            if (model != null)
            {
                model.transform.SetParent(transform);
                CacheRenderers();
                isLoaded = true;
            }
        }
    }
}
