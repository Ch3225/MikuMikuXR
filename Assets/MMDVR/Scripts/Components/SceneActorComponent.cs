using UnityEngine;
using LibMMD.Unity3D;
using System.Collections.Generic;

namespace MMDVR.Scripts.Components
{
    /// <summary>
    /// 场景演员组件数据 - 简化版本，支持多动作
    /// </summary>
    [System.Serializable]
    public class SceneActorData
    {
        public string id;
        public string displayName;
        public string modelId;
        public List<string> motionIds = new List<string>(); // 支持多个动作
        public bool isVisible = true;
        // 移除位置、姿态、大小字段 - 直接使用Transform
    }

    /// <summary>
    /// 场景演员组件 - 管理场景中的演员实例
    /// 重新设计：支持多动作，简化字段，移除冗余连接
    /// </summary>
    public class SceneActorComponent : MonoBehaviour
    {
        [Header("演员标识")]
        public string actorId;
        public string displayName;
        
        [Header("关联资源")]
        public string modelId;
        public List<string> motionIds = new List<string>(); // 支持多个动作ID
        
        [Header("组件引用")]
        public ModelComponent modelRef; // 模型组件引用
        // 移除mmdGameObject字段 - 直接通过GetComponent获取        [Header("演员数据")]
        public SceneActorData actorData;

        // 运行时缓存的MmdGameObject引用
        private MmdGameObject _mmdGameObject;
        public MmdGameObject MmdGameObject 
        { 
            get 
            { 
                if (_mmdGameObject == null)
                    _mmdGameObject = GetComponent<MmdGameObject>();
                return _mmdGameObject;
            }        }

        private void Awake()
        {
            if (string.IsNullOrEmpty(displayName))
                displayName = gameObject.name;

            // 初始化演员数据
            if (actorData == null)
            {
                actorData = new SceneActorData
                {
                    id = actorId,
                    displayName = displayName,
                    modelId = modelId,
                    motionIds = new List<string>(motionIds), // 复制当前动作列表
                    isVisible = true
                };
            }
        }        /// <summary>
        /// 设置模型组件
        /// </summary>
        public void SetModelComponent(ModelComponent model)
        {
            modelRef = model;
            if (model != null)
            {
                modelId = model.modelId;
                actorData.modelId = modelId;
                
                // 将模型对象设为子对象
                if (model.transform.parent != transform)
                {
                    model.transform.SetParent(transform);
                }
            }
        }

        /// <summary>
        /// 添加动作ID到列表
        /// </summary>
        public void AddMotion(string motionId)
        {
            if (!string.IsNullOrEmpty(motionId) && !motionIds.Contains(motionId))
            {
                motionIds.Add(motionId);
                actorData.motionIds.Add(motionId);
            }
        }

        /// <summary>
        /// 移除动作ID
        /// </summary>
        public void RemoveMotion(string motionId)
        {
            motionIds.Remove(motionId);
            actorData.motionIds.Remove(motionId);
        }

        /// <summary>
        /// 清空所有动作
        /// </summary>
        public void ClearMotions()
        {
            motionIds.Clear();
            actorData.motionIds.Clear();
        }        /// <summary>
        /// 获取当前所有动作ID列表
        /// </summary>
        public List<string> GetMotionIds()
        {
            return new List<string>(motionIds);
        }

        /// <summary>
        /// 设置可见性 - 暂未实现完整功能
        /// </summary>
        public void SetVisibility(bool visible)
        {
            actorData.isVisible = visible;
            
            // TODO: 实现模型可见性控制
            if (modelRef != null)
            {
                // modelRef.SetVisibility(visible);
            }
            else if (MmdGameObject != null)
            {
                MmdGameObject.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 设置激活状态 - 使用GameObject的activeInHierarchy
        /// </summary>
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

        /// <summary>
        /// 获取演员数据副本 - 简化版本，移除位置信息
        /// </summary>
        public SceneActorData GetActorData()
        {
            return new SceneActorData
            {
                id = actorData.id,
                displayName = actorData.displayName,
                modelId = actorData.modelId,
                motionIds = new List<string>(actorData.motionIds),
                isVisible = actorData.isVisible
            };
        }
    }
}
