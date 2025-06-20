using UnityEngine;

namespace MMDVR.Scripts.Components
{
    /// <summary>
    /// 场景演员组件数据 - 用于场景中的演员实例
    /// </summary>
    [System.Serializable]
    public class SceneActorData
    {
        public string id;
        public string displayName;
        public string modelId;
        public string currentMotionId;
        public bool isVisible = true;
        public bool isActive = true;
        public Vector3 position = Vector3.zero;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 scale = Vector3.one;
    }

    /// <summary>
    /// 场景演员组件 - 管理场景中的演员实例
    /// </summary>
    public class SceneActorComponent : MonoBehaviour
    {
        [Header("演员标识")]
        public string actorId;
        public string displayName;        [Header("关联资源")]
        public string modelId;
        public string currentMotionId;
        public string associatedModelId; // 兼容性属性
        public string associatedMotionId; // 兼容性属性

        [Header("演员状态")]
        public bool isVisible = true;
        public bool isActive = true;

        private void Start()
        {
            // 同步兼容性属性
            associatedModelId = modelId;
            associatedMotionId = currentMotionId;
        }

        [Header("演员数据")]
        public SceneActorData actorData;

        [Header("组件引用")]
        public ModelComponent modelComponent;
        public MotionComponent currentMotionComponent;
        public GameObject modelObject;

        private void Awake()
        {
            if (string.IsNullOrEmpty(actorId))
                actorId = System.Guid.NewGuid().ToString("N")[..8];
            
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
                    currentMotionId = currentMotionId,
                    isVisible = isVisible,
                    isActive = isActive,
                    position = transform.position,
                    rotation = transform.rotation,
                    scale = transform.localScale
                };
            }
        }

        /// <summary>
        /// 设置模型组件
        /// </summary>
        public void SetModelComponent(ModelComponent model)
        {
            modelComponent = model;
            if (model != null)
            {
                modelId = model.modelId;
                modelObject = model.modelObject;
                actorData.modelId = modelId;
                
                // 将模型对象设为子对象
                if (model.transform.parent != transform)
                {
                    model.transform.SetParent(transform);
                }
            }
        }

        /// <summary>
        /// 设置动作组件
        /// </summary>
        public void SetMotionComponent(MotionComponent motion)
        {
            currentMotionComponent = motion;
            if (motion != null)
            {
                currentMotionId = motion.motionId;
                actorData.currentMotionId = currentMotionId;
                
                // 关联动作到此演员
                motion.AssignToActor(actorId, modelId);
            }
        }

        /// <summary>
        /// 设置可见性
        /// </summary>
        public void SetVisibility(bool visible)
        {
            isVisible = visible;
            actorData.isVisible = visible;
            
            if (modelComponent != null)
            {
                modelComponent.SetVisibility(visible);
            }
            else if (modelObject != null)
            {
                modelObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 设置激活状态
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
            actorData.isActive = active;
            gameObject.SetActive(active);
        }

        /// <summary>
        /// 更新位置变换
        /// </summary>
        public void UpdateTransform()
        {
            actorData.position = transform.position;
            actorData.rotation = transform.rotation;
            actorData.scale = transform.localScale;
        }

        /// <summary>
        /// 应用位置变换
        /// </summary>
        public void ApplyTransform()
        {
            transform.position = actorData.position;
            transform.rotation = actorData.rotation;
            transform.localScale = actorData.scale;
        }

        /// <summary>
        /// 获取演员数据副本
        /// </summary>
        public SceneActorData GetActorData()
        {
            UpdateTransform();
            return new SceneActorData
            {
                id = actorData.id,
                displayName = actorData.displayName,
                modelId = actorData.modelId,
                currentMotionId = actorData.currentMotionId,
                isVisible = actorData.isVisible,
                isActive = actorData.isActive,
                position = actorData.position,
                rotation = actorData.rotation,
                scale = actorData.scale
            };
        }
    }
}
