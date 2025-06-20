using UnityEngine;
using System.Collections.Generic;
using MMDVR.Scripts.Data;
using MMDVR.Scripts.Components;
using MMDVR.Scripts.Managers;
using MMDVR.Scripts.Events;
using MMDVR.Events;

namespace MMDVR.Scripts.Managers
{    /// <summary>
    /// 场景展示管理器 - 专门负责资源在场景中的展示和控制
    /// 不涉及资源加载，只管理资源如何在场景中呈现
    /// </summary>
    public class SceneDisplayManager : MonoBehaviour
    {
        public static SceneDisplayManager Instance { get; private set; }

        [Header("场景展示容器")]
        [Tooltip("演员展示容器")] public Transform actorContainer;

        [Header("当前状态")]
        [Tooltip("当前活动摄像机ID")] public string currentActiveCameraId = "BUILTIN_FREE_CAMERA";
        [Tooltip("当前活动音乐ID")] public string currentActiveMusicId;

        [Header("数据列表")]
        [Tooltip("演员数据列表")] public List<ActorData> actorList = new List<ActorData>();

        private ResourceManager resourceManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeDisplayContainers();
            
            // 获取ResourceManager引用
            resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
            {
                Debug.LogError("SceneDisplayManager: 找不到ResourceManager实例");
            }
        }        /// <summary>
        /// 初始化展示容器
        /// </summary>
        private void InitializeDisplayContainers()
        {
            if (actorContainer == null)
                actorContainer = CreateDisplayContainer("Actors");
        }

        /// <summary>
        /// 创建展示容器
        /// </summary>
        private Transform CreateDisplayContainer(string containerName)
        {
            GameObject container = new GameObject(containerName);
            container.transform.SetParent(transform);
            Debug.Log($"SceneDisplayManager: 创建了{containerName}展示容器");
            return container.transform;
        }

        // ==================== 演员展示管理 ====================        /// <summary>
        /// 添加演员到场景（基于已加载的模型资源）
        /// </summary>
        public string AddActor(string modelId)
        {
            if (resourceManager == null || string.IsNullOrEmpty(modelId))
            {
                Debug.LogError("SceneDisplayManager: ResourceManager未初始化或modelId为空");
                return null;
            }

            // 检查模型资源是否存在
            var modelComponent = resourceManager.GetModel(modelId);
            if (modelComponent == null)
            {
                Debug.LogError($"SceneDisplayManager: 找不到模型资源 {modelId}");
                return null;
            }

            // 创建演员对象
            string actorId = System.Guid.NewGuid().ToString("N")[..8];
            GameObject actorObj = new GameObject($"Actor_{actorId}");
            actorObj.transform.SetParent(actorContainer);

            // 添加演员组件
            var actorComponent = actorObj.AddComponent<SceneActorComponent>();
            actorComponent.actorId = actorId;
            actorComponent.associatedModelId = modelId;

            // 添加到演员列表
            var actorData = new ActorData
            {
                id = actorId,
                displayName = $"Actor_{actorId}",
                modelId = modelId,
                motionId = "",
                isVisible = true
            };
            actorList.Add(actorData);

            Debug.Log($"SceneDisplayManager: 添加演员到场景 {actorId} (模型: {modelId})");
            
            // 触发事件
            SceneDisplayEvents.TriggerActorSpawned(actorId, modelId);
            SceneDisplayEvents.TriggerActorListChanged();

            return actorId;
        }

        /// <summary>
        /// 移除演员
        /// </summary>
        public void RemoveActor(string actorId)
        {
            Transform actorObj = actorContainer.Find($"Actor_{actorId}");
            if (actorObj != null)
            {
                // 从列表中移除
                actorList.RemoveAll(a => a.id == actorId);

                Debug.Log($"SceneDisplayManager: 移除演员: {actorId}");
                
                // 触发事件
                SceneDisplayEvents.TriggerActorDestroyed(actorId);
                SceneDisplayEvents.TriggerActorListChanged();
                
                Destroy(actorObj.gameObject);
            }
        }        /// <summary>
        /// 关联动作到演员
        /// </summary>
        public void AssociateMotionWithActor(string motionId, string actorId)
        {
            if (resourceManager == null)
            {
                Debug.LogError("SceneDisplayManager: ResourceManager未初始化");
                return;
            }            // 检查动作资源是否存在
            var motionComponent = resourceManager.GetMotion(motionId);
            if (motionComponent == null)
            {
                Debug.LogError($"SceneDisplayManager: 找不到动作资源 {motionId}");
                return;
            }

            // 查找演员
            var actorData = actorList.Find(a => a.id == actorId);
            if (actorData == null)
            {
                Debug.LogError($"SceneDisplayManager: 找不到演员 {actorId}");
                return;
            }

            // 更新演员动作关联
            actorData.motionId = motionId;

            // 更新场景中的演员组件
            Transform actorObj = actorContainer.Find($"Actor_{actorId}");
            if (actorObj != null)
            {
                var actorComponent = actorObj.GetComponent<SceneActorComponent>();
                if (actorComponent != null)
                {
                    actorComponent.associatedMotionId = motionId;
                }
            }            Debug.Log($"SceneDisplayManager: 关联动作 {motionId} 到演员 {actorId}");
            
            // 触发事件
            SceneDisplayEvents.TriggerModelMotionAssociationChanged(actorData.modelId, motionId, true);
        }        /// <summary>
        /// 切换演员可见性
        /// </summary>
        public void ToggleActorVisibility(string actorId)
        {
            var actorData = actorList.Find(a => a.id == actorId);
            if (actorData != null)
            {
                actorData.isVisible = !actorData.isVisible;

                Transform actorObj = actorContainer.Find($"Actor_{actorId}");
                if (actorObj != null)
                {
                    actorObj.gameObject.SetActive(actorData.isVisible);
                }

                Debug.Log($"SceneDisplayManager: 切换演员可见性 {actorId} -> {actorData.isVisible}");
                SceneDisplayEvents.TriggerActorVisibilityChanged(actorId, actorData.isVisible);
            }
        }

        /// <summary>
        /// 获取演员组件
        /// </summary>
        public SceneActorComponent GetActor(string actorId)
        {
            Transform actorObj = actorContainer.Find($"Actor_{actorId}");
            return actorObj?.GetComponent<SceneActorComponent>();
        }

        /// <summary>
        /// 根据演员ID获取GameObject
        /// </summary>
        public GameObject GetActorGameObject(string actorId)
        {
            if (actorContainer == null) return null;
            
            for (int i = 0; i < actorContainer.childCount; i++)
            {
                Transform child = actorContainer.GetChild(i);
                var actorComponent = child.GetComponent<SceneActorComponent>();
                if (actorComponent != null && actorComponent.actorId == actorId)
                {
                    return child.gameObject;
                }
            }
            
            return null;
        }

        // ==================== 音乐播放控制 ====================        /// <summary>
        /// 激活音乐播放
        /// </summary>
        public void ActivateMusic(string musicId)
        {
            if (resourceManager == null)
            {
                Debug.LogError("SceneDisplayManager: ResourceManager未初始化");
                return;
            }            // 检查音乐资源是否存在
            var musicComponent = resourceManager.GetMusic(musicId);
            if (musicComponent == null)
            {
                Debug.LogError($"SceneDisplayManager: 找不到音乐资源 {musicId}");
                return;
            }

            // 停止当前音乐
            if (!string.IsNullOrEmpty(currentActiveMusicId))
            {
                var currentMusic = resourceManager.GetMusic(currentActiveMusicId);
                if (currentMusic != null)
                {
                    var audioSource = currentMusic.GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        audioSource.Stop();
                    }
                }
            }

            // 激活新音乐
            currentActiveMusicId = musicId;

            Debug.Log($"SceneDisplayManager: 激活音乐 {musicId}");
            SceneDisplayEvents.TriggerMusicActivated(musicId);
        }

        /// <summary>
        /// 获取当前激活音乐的AudioSource
        /// </summary>
        public AudioSource GetActiveMusicAudioSource()
        {
            if (string.IsNullOrEmpty(currentActiveMusicId) || resourceManager == null)
                return null;
                
            var musicComponent = resourceManager.GetMusic(currentActiveMusicId);
            if (musicComponent == null) return null;
            
            return musicComponent.GetComponent<AudioSource>();
        }
          /// <summary>
        /// 获取第一个可用音乐并激活
        /// </summary>
        public string GetAndActivateFirstAvailableMusic()
        {
            if (resourceManager == null) return "";
            
            var musicList = resourceManager.GetMusicList();
            if (musicList.Count > 0)
            {
                var firstMusic = musicList[0];
                ActivateMusic(firstMusic.id);
                return firstMusic.id;
            }
            
            return "";
        }

        // ==================== 摄像机切换控制 ====================

        /// <summary>
        /// 激活摄像机
        /// </summary>
        public void ActivateCamera(string cameraId)
        {
            if (resourceManager == null)
            {
                Debug.LogError("SceneDisplayManager: ResourceManager未初始化");
                return;
            }

            // 如果不是内置摄像机，检查资源是否存在
            if (cameraId != "BUILTIN_FREE_CAMERA")
            {
                var cameraComponent = resourceManager.GetCamera(cameraId);
                if (cameraComponent == null)
                {
                    Debug.LogError($"SceneDisplayManager: 找不到摄像机资源: {cameraId}");
                    return;
                }
            }

            currentActiveCameraId = cameraId;

            Debug.Log($"SceneDisplayManager: 激活摄像机: {cameraId}");
            SceneDisplayEvents.TriggerCameraActivated(cameraId);
        }        /// <summary>
        /// 应用摄像机状态
        /// </summary>
        public void ApplyCameraState(MMDVR.Scripts.Components.CameraState cameraState)
        {
            // 应用VMD摄像机状态到当前激活的摄像机
            Camera activeCamera = SystemStateManager.Instance?.GetActiveCamera();
            if (activeCamera != null)
            {
                activeCamera.transform.position = cameraState.position;
                activeCamera.transform.rotation = cameraState.rotation;
                activeCamera.fieldOfView = cameraState.fieldOfView;
            }
        }        /// <summary>
        /// 获取当前活动摄像机
        /// </summary>
        public Camera GetActiveCamera()
        {
            // 通过SystemStateManager获取主摄像机
            if (SystemStateManager.Instance != null)
            {
                return SystemStateManager.Instance.GetActiveCamera();
            }

            // 备用方案
            return Camera.main;
        }

        // ==================== 通用展示管理 ====================        /// <summary>
        /// 清理所有场景展示对象
        /// </summary>
        public void ClearAllDisplayObjects()
        {
            Debug.Log("SceneDisplayManager: 清理所有场景展示对象");

            // 清理演员
            ClearContainer(actorContainer);
            actorList.Clear();

            // 重置状态
            currentActiveCameraId = "BUILTIN_FREE_CAMERA";
            currentActiveMusicId = "";

            // 触发事件
            SceneDisplayEvents.TriggerActorListChanged();
        }

        /// <summary>
        /// 清理指定容器
        /// </summary>
        private void ClearContainer(Transform container)
        {
            if (container == null) return;

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        private void OnDestroy()
        {
            // 清理所有展示对象
            if (Instance == this)
            {
                ClearAllDisplayObjects();
            }
        }

        // ==================== 演员管理扩展方法 ====================        /// <summary>
        /// 关联动作到演员
        /// </summary>
        public void AssignMotionToActor(string actorId, string motionId)
        {
            // 直接调用现有的内部实现
            AssignMotionToActorInternal(actorId, motionId);
        }

        /// <summary>
        /// 关联动作到演员的内部实现
        /// </summary>
        private void AssignMotionToActorInternal(string actorId, string motionId)
        {
            // 查找对应的演员
            var actor = actorList.Find(a => a.id == actorId);
            if (actor == null)
            {
                Debug.LogError($"SceneDisplayManager: 找不到演员 {actorId}");
                return;
            }

            // 更新演员的动作ID
            actor.motionId = motionId;

            // 查找场景中的演员组件并更新
            Transform actorObj = actorContainer.Find($"Actor_{actorId}");
            if (actorObj != null)
            {
                var actorComponent = actorObj.GetComponent<SceneActorComponent>();
                if (actorComponent != null)
                {
                    // 获取动作组件
                    var motionComponent = resourceManager.GetMotion(motionId);
                    if (motionComponent != null)
                    {
                        actorComponent.SetMotionComponent(motionComponent);
                    }
                }
            }

            Debug.Log($"SceneDisplayManager: 关联动作 {motionId} 到演员 {actorId}");
            
            // 触发事件
            SceneDisplayEvents.TriggerModelMotionAssociationChanged(actor.modelId, motionId, true);
        }

        /// <summary>
        /// 获取演员列表
        /// </summary>
        public List<ActorData> GetActorList()
        {
            return new List<ActorData>(actorList);
        }

        /// <summary>
        /// 获取模型关联的动作
        /// </summary>
        public List<string> GetModelAssociatedMotions(string modelId)
        {
            var associatedMotions = new List<string>();
            foreach (var actor in actorList)
            {
                if (actor.modelId == modelId && !string.IsNullOrEmpty(actor.motionId))
                {
                    associatedMotions.Add(actor.motionId);
                }
            }
            return associatedMotions;
        }

        /// <summary>
        /// 获取关联的动作
        /// </summary>
        public List<string> GetAssociatedMotions(string actorId)
        {
            var actor = actorList.Find(a => a.id == actorId);
            if (actor != null && !string.IsNullOrEmpty(actor.motionId))
            {
                return new List<string> { actor.motionId };
            }
            return new List<string>();
        }

        /// <summary>
        /// 获取关联的模型
        /// </summary>
        public List<string> GetAssociatedModels(string motionId)
        {
            var associatedModels = new List<string>();
            foreach (var actor in actorList)
            {
                if (actor.motionId == motionId && !string.IsNullOrEmpty(actor.modelId))
                {
                    associatedModels.Add(actor.modelId);
                }
            }
            return associatedModels;
        }
    }
}
