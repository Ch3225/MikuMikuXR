using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using MMDVR.Scripts.Data;
using MMDVR.Scripts.Components;
using MMDVR.Scripts.Managers;
using MMDVR.Scripts.Events;
using MMDVR.Events;
using LibMMD.Unity3D;

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

            // 使用协程进行异步创建，避免卡顿
            string actorId = modelId; // Actor ID 与 Model ID 保持一致
            ResourceManager.Instance.StartGlobalCoroutine(CreateActorCoroutine(modelComponent, actorId));
            
            return actorId;
        }
        
        /// <summary>
        /// 异步创建演员的协程，分帧处理避免卡顿
        /// </summary>
        private System.Collections.IEnumerator CreateActorCoroutine(ModelComponent modelComponent, string actorId)
        {
            Debug.Log($"🎭 开始创建演员: {actorId}");
            
            try
            {
                // 创建MMD游戏对象
                GameObject mmdObj = MmdGameObject.CreateGameObject($"Actor_{actorId}");
                mmdObj.transform.SetParent(actorContainer);
                
                yield return new WaitForEndOfFrame(); // 等待GameObject创建完成

                // 获取MmdGameObject组件并配置
                var mmdGameObject = mmdObj.GetComponent<MmdGameObject>();
                if (mmdGameObject != null)
                {
                    Debug.Log($"⚙️ 配置MMD组件...");
                    
                    // 设置物理模式为无物理，避免物理相关错误
                    mmdGameObject.PhysicsMode = MmdGameObject.PhysicsModeEnum.None;
                    
                    // 配置HDRP材质设置
                    var config = new LibMMD.Unity3D.MmdUnityConfig
                    {
                        EnableDrawSelfShadow = LibMMD.Unity3D.MmdConfigSwitch.AsConfig,
                        EnableCastShadow = LibMMD.Unity3D.MmdConfigSwitch.AsConfig,
                        EnableEdge = LibMMD.Unity3D.MmdConfigSwitch.AsConfig
                    };
                    mmdGameObject.UpdateConfig(config);
                    
                    yield return new WaitForEndOfFrame(); // 等待配置完成
                    
                    Debug.Log($"📁 加载MMD模型: {Path.GetFileName(modelComponent.filePath)}");
                    
                    // 加载模型（可能耗时，分帧处理）
                    mmdGameObject.LoadModel(modelComponent.filePath);
                    
                    yield return new WaitForSeconds(0.1f); // 等待模型加载初始化
                    
                    Debug.Log($"✅ MMD模型加载完成: {mmdGameObject.ModelName}");
                    
                    // 检查mesh是否正确加载
                    yield return StartCoroutine(ValidateModelMesh(mmdObj));
                }
                else
                {
                    Debug.LogError("❌ 无法获取MmdGameObject组件");
                    Destroy(mmdObj);
                    yield break;
                }

                yield return new WaitForEndOfFrame(); // 等待组件添加完成

                Debug.Log($"🎪 添加演员组件...");
                
                // 添加演员组件
                var actorComponent = mmdObj.AddComponent<SceneActorComponent>();
                actorComponent.actorId = actorId;
                actorComponent.modelId = modelComponent.modelId;
                actorComponent.SetModelComponent(modelComponent);
                actorComponent.displayName = Path.GetFileNameWithoutExtension(modelComponent.filePath);

                // 添加到演员列表
                var actorData = new ActorData
                {
                    id = actorId,
                    displayName = Path.GetFileNameWithoutExtension(modelComponent.filePath),
                    filePath = modelComponent.filePath,
                    modelId = modelComponent.modelId,
                    motionId = "",
                    isVisible = true
                };
                actorList.Add(actorData);

                yield return new WaitForEndOfFrame(); // 等待数据添加完成

                Debug.Log($"🎉 演员创建完成: {actorId}");
                
                // 触发事件
                SceneDisplayEvents.TriggerActorSpawned(actorId, modelComponent.modelId);
                SceneDisplayEvents.TriggerActorListChanged();
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ 创建演员时出错: {e.Message}");
                Debug.LogError($"异常类型: {e.GetType().Name}");
                Debug.LogError($"异常堆栈: {e.StackTrace}");
                
                if (e.InnerException != null)
                {
                    Debug.LogError($"内部异常: {e.InnerException.Message}");
                }
            }
        }
        
        /// <summary>
        /// 验证模型网格是否正确加载
        /// </summary>
        private System.Collections.IEnumerator ValidateModelMesh(GameObject mmdObj)
        {
            Debug.Log($"🔍 验证模型网格...");
            
            yield return new WaitForSeconds(0.1f); // 等待网格生成
            
            var meshFilter = mmdObj.GetComponent<MeshFilter>();
            var skinnedMeshRenderer = mmdObj.GetComponent<SkinnedMeshRenderer>();
            
            if (meshFilter && meshFilter.mesh != null)
            {
                Debug.Log($"✅ MeshFilter网格: {meshFilter.mesh.vertexCount} 顶点");
            }
            if (skinnedMeshRenderer && skinnedMeshRenderer.sharedMesh != null)
            {
                Debug.Log($"✅ SkinnedMeshRenderer网格: {skinnedMeshRenderer.sharedMesh.vertexCount} 顶点");
            }
            
            if ((meshFilter == null || meshFilter.mesh == null) && 
                (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null))
            {
                Debug.LogWarning($"⚠️ 模型网格可能未正确加载");
            }
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
            actorData.motionId = motionId;            // 更新场景中的演员组件
            Transform actorObj = actorContainer.Find($"Actor_{actorId}");
            if (actorObj != null)
            {
                var actorComponent = actorObj.GetComponent<SceneActorComponent>();
                if (actorComponent != null)
                {
                    actorComponent.AddMotion(motionId); // 使用新的添加动作方法
                }
            }Debug.Log($"SceneDisplayManager: 关联动作 {motionId} 到演员 {actorId}");
            
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
        }        /// <summary>
        /// 关联动作到演员的内部实现
        /// </summary>
        private void AssignMotionToActorInternal(string actorId, string motionId)
        {
            // 通过遍历actorContainer查找SceneActorComponent.actorId匹配（与原系统保持一致）
            Transform actorTransform = null;
            SceneActorComponent currentActorComponent = null;
            
            for (int i = 0; i < actorContainer.childCount; i++)
            {
                Transform child = actorContainer.GetChild(i);
                var ac = child.GetComponent<SceneActorComponent>();
                if (ac != null && ac.actorId == actorId)
                {
                    actorTransform = child;
                    currentActorComponent = ac;
                    break;
                }
            }
            
            if (actorTransform == null)
            {
                Debug.LogError($"SceneDisplayManager: 找不到演员 {actorId}");
                return;
            }

            // 查找对应的演员数据（用于后续更新）
            var actor = actorList.Find(a => a.id == actorId);
            if (actor != null)
            {
                actor.motionId = motionId; // 更新演员的动作ID
            }

            // 获取动作组件
            var motionComponent = resourceManager.GetMotion(motionId);
            if (motionComponent == null)
            {
                Debug.LogError($"SceneDisplayManager: 找不到动作资源 {motionId}");
                return;
            }

            // 获取MmdGameObject并加载动作
            var mmdGameObject = actorTransform.GetComponent<MmdGameObject>();
            if (mmdGameObject != null && !string.IsNullOrEmpty(motionComponent.filePath))
            {
                try
                {
                    if (File.Exists(motionComponent.filePath))
                    {
                        mmdGameObject.LoadMotion(motionComponent.filePath);
                        
                        // 配置MMD设置（参考原系统）
                        mmdGameObject.UpdateConfig(new LibMMD.Unity3D.MmdUnityConfig
                        {
                            EnableDrawSelfShadow = LibMMD.Unity3D.MmdConfigSwitch.ForceFalse,
                            EnableCastShadow = LibMMD.Unity3D.MmdConfigSwitch.ForceFalse,
                            EnableEdge = LibMMD.Unity3D.MmdConfigSwitch.AsConfig
                        });
                        mmdGameObject.PhysicsMode = MmdGameObject.PhysicsModeEnum.Bullet;
                          // 更新SceneActorComponent
                        if (currentActorComponent != null)
                        {
                            currentActorComponent.AddMotion(motionComponent.motionId);
                        }
                        
                        Debug.Log($"SceneDisplayManager: 成功为演员 {actorId} 加载动作 {motionComponent.filePath}");
                    }
                    else
                    {
                        Debug.LogError($"VMD文件不存在: {motionComponent.filePath}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"SceneDisplayManager: 为演员 {actorId} 加载动作失败: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"SceneDisplayManager: 演员 {actorId} 没有MmdGameObject组件");
            }

            Debug.Log($"SceneDisplayManager: 关联动作 {motionId} 到演员 {actorId}");
            
            // 触发事件
            if (actor != null)
            {
                SceneDisplayEvents.TriggerModelMotionAssociationChanged(actor.modelId, motionId, true);
            }
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
