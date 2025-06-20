using UnityEngine;

namespace MMDVR.Scripts.Components
{
    /// <summary>
    /// 模型组件 - 存储模型相关信息和状态
    /// </summary>
    public class ModelComponent : MonoBehaviour
    {
        [Header("模型标识")]
        public string modelId;
        public string displayName;
        public string filePath;

        [Header("模型状态")]
        public bool isLoaded = false;
        public bool isVisible = true;

        [Header("模型数据")]
        public GameObject modelObject;
        public Renderer[] renderers;

        private void Awake()
        {
            if (string.IsNullOrEmpty(modelId))
                modelId = System.Guid.NewGuid().ToString("N")[..8];
            
            if (string.IsNullOrEmpty(displayName))
                displayName = gameObject.name;
        }

        /// <summary>
        /// 设置模型可见性
        /// </summary>
        public void SetVisibility(bool visible)
        {
            isVisible = visible;
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
