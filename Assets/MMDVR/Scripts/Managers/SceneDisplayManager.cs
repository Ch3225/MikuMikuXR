using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using MMDVR.Scripts.Model;
using MMDVR.Scripts.Components;
using MMDVR.Scripts.Managers;
using MMDVR.Scripts.Events;
using MMDVR.Events;
using MMDVR.Scripts.UIInteraction.ResourceManagement;
using MMDVR.Scripts.UIInteraction.ResourceManagement.ListController;
using LibMMD.Unity3D;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace MMDVR.Scripts.Managers
{    /// <summary>
    /// 场景展示管理器 - 专门负责资源在场景中的展示和控制
    /// 不涉及资源加载，只管理资源如何在场景中呈现
    /// </summary>
    public class SceneDisplayManager : MonoBehaviour
    {
        public static SceneDisplayManager Instance { get; private set; }        [Header("场景展示容器")]
        [Tooltip("演员展示容器")] public Transform actorContainer;

        [Header("音频播放")]
        [Tooltip("音乐播放源 - 用于播放当前激活的音乐")] public AudioSource musicAudioSource;

        [Header("当前状态")]
        [Tooltip("当前活动摄像机ID")] public string currentActiveCameraId = "BUILTIN_FREE_CAMERA";
        [Tooltip("当前活动音乐ID")] public string currentActiveMusicId;

        [Header("数据列表")]
        [Tooltip("演员数据列表")] public List<ActorData> actorList = new List<ActorData>();
        // 移除 ResourceManager 直接依赖
        // private ResourceManager resourceManager;
        private AssociationManager associationManager;        // 引用各个ListController以获取列表数据
        private ModelListController modelListController;
        private MotionListController motionListController;
        private CameraListController cameraListController;
        private MusicListController musicListController;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
        private void Start()
        {
            InitializeDisplayContainers();
            associationManager = AssociationManager.Instance;
            if (associationManager == null)
            {
                Debug.LogError("SceneDisplayManager: 找不到AssociationManager实例");
            }
            modelListController = ModelListController.Instance;
            motionListController = MotionListController.Instance;
            cameraListController = CameraListController.Instance;
            musicListController = MusicListController.Instance;
            // 监听模型-动作关联变更事件，确保MmdGameObject和ActorComponent都就绪后再加载动作
            MMDVR.Events.SceneDisplayEvents.OnModelMotionAssociationChanged += OnModelMotionAssociationChanged;
        }

        private void OnDestroy()
        {
            // 清理所有展示对象
            if (Instance == this)
            {
                ClearAllDisplayObjects();
            }

            // 取消事件订阅
            MMDVR.Events.SceneDisplayEvents.OnModelMotionAssociationChanged -= OnModelMotionAssociationChanged;
            ResourceEvents.OnModelListChanged -= SyncWithResourceLists;
            ResourceEvents.OnMotionListChanged -= SyncWithResourceLists;
            ResourceEvents.OnCameraListChanged -= SyncActiveCameraWithList;
            ResourceEvents.OnMusicListChanged -= SyncActiveMusicWithList;
        }

        private void OnModelMotionAssociationChanged(string modelId, string motionId, bool isAssociated)
        {
            // 查找所有与modelId关联的Actor GameObject
            foreach (Transform actor in actorContainer)
            {
                var actorComponent = actor.GetComponent<ActorComponent>();
                var mmdGameObject = actor.GetComponent<MmdGameObject>();
                if (actorComponent != null && mmdGameObject != null && actorComponent.actorId == modelId)
                {
                    if (isAssociated)
                    {
                        // 加载动作
                        var motionComponent = ResourceManager.Instance?.GetMotion(motionId);
                        if (motionComponent != null)
                        {
                            mmdGameObject.LoadMotion(motionComponent.filePath);
                        }
                    }
                    else
                    {
                        // 取消动作时可重置为T-Pose或其它逻辑
                        mmdGameObject.ResetToTPose();
                    }
                }
            }
        }

        /// <summary>
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
            if (string.IsNullOrEmpty(modelId))
            {
                Debug.LogError("SceneDisplayManager: modelId为空");
                return null;
            }

            // 检查模型资源是否存在（通过 ResourceManager 获取）
            var modelComponent = ResourceManager.Instance?.GetModel(modelId);
            if (modelComponent == null)
            {
                Debug.LogError($"SceneDisplayManager: 找不到模型资源 {modelId}");
                Debug.LogError($"SceneDisplayManager: 当前modelContainer下模型数量: " + (ResourceManager.Instance?.GetModelList()?.Count ?? -1));
                return null;
            }

            // 检查是否已存在该演员，避免重复
            if (actorList.Any(a => a.modelId == modelId))
            {
                Debug.LogWarning($"SceneDisplayManager: 已存在该模型的演员 {modelId}");
                return modelId;
            }

            Debug.Log($"SceneDisplayManager: AddActor 调用成功，开始异步创建演员 {modelId}，当前modelContainer下模型数量: " + (ResourceManager.Instance?.GetModelList()?.Count ?? -1));
            StartCoroutine(CreateActorCoroutine(modelComponent, modelId));
            return modelId;
        }
          /// <summary>
        /// 异步创建演员的协程，分帧处理避免卡顿
        /// </summary>
        private System.Collections.IEnumerator CreateActorCoroutine(ModelComponent modelComponent, string actorId)
        {
            Debug.Log($"🎭 开始创建演员: {actorId}");
            GameObject mmdObj = null;
            MmdGameObject mmdGameObject = null;
            bool creationSuccess = false;
            // 创建MMD游戏对象
            try
            {
                mmdObj = MmdGameObject.CreateGameObject($"Actor_{actorId}");
                mmdObj.transform.SetParent(actorContainer);
                creationSuccess = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ 创建MMD游戏对象时出错: {e.Message}");
                yield break;
            }
            yield return new WaitForEndOfFrame(); // 等待GameObject创建完成
            // 先添加演员组件
            ActorComponent actorComponent = null;
            try
            {
                Debug.Log($"🎪 添加演员组件...");
                actorComponent = mmdObj.AddComponent<ActorComponent>();
                actorComponent.actorId = actorId;
                actorComponent.displayName = Path.GetFileNameWithoutExtension(modelComponent.filePath);
                // 添加到演员列表
                var actorData = new ActorData
                {
                    id = actorId,
                    displayName = Path.GetFileNameWithoutExtension(modelComponent.filePath),
                    filePath = modelComponent.filePath,
                    modelId = modelComponent.id,
                    motionIds = new List<string>(),
                    isVisible = true
                };
                actorList.Add(actorData);
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ 添加演员组件时出错: {e.Message}");
                if (mmdObj != null) Destroy(mmdObj);
                yield break;
            }
            yield return new WaitForEndOfFrame(); // 等待组件添加完成
            // 获取MmdGameObject组件并配置
            try
            {
                mmdGameObject = mmdObj.GetComponent<MmdGameObject>();
                if (mmdGameObject != null)
                {
                    Debug.Log($"⚙️ 配置MMD组件...");
                    // 配置HDRP材质设置
                    var config = new LibMMD.Unity3D.MmdUnityConfig
                    {
                        EnableDrawSelfShadow = LibMMD.Unity3D.MmdConfigSwitch.AsConfig,
                        EnableCastShadow = LibMMD.Unity3D.MmdConfigSwitch.AsConfig,
                        EnableEdge = LibMMD.Unity3D.MmdConfigSwitch.AsConfig
                    };
                    mmdGameObject.UpdateConfig(config);
                }
                else
                {
                    Debug.LogError("❌ 无法获取MmdGameObject组件");
                    Destroy(mmdObj);
                    yield break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ 配置MMD组件时出错: {e.Message}");
                if (mmdObj != null) Destroy(mmdObj);
                yield break;
            }
            yield return new WaitForEndOfFrame(); // 等待配置完成
            // 加载模型
            try
            {
                Debug.Log($"📁 加载MMD模型: {Path.GetFileName(modelComponent.filePath)}");
                mmdGameObject.LoadModel(modelComponent.filePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ 加载MMD模型时出错: {e.Message}");
                if (mmdObj != null) Destroy(mmdObj);
                yield break;
            }
            yield return new WaitForSeconds(0.1f); // 等待模型加载初始化
            Debug.Log($"✅ MMD模型加载完成: {mmdGameObject.ModelName}");
            // 检查mesh是否正确加载
            yield return StartCoroutine(ValidateModelMesh(mmdObj));
            yield return new WaitForEndOfFrame(); // 等待所有组件添加完成
            // 检查并应用关联的动作
            Debug.Log($"🔍 检查模型 {modelComponent.id} 的关联动作...");
            if (AssociationManager.Instance != null)
            {
                var associatedMotions = AssociationManager.Instance.GetModelAssociatedMotions(modelComponent.id);
                if (associatedMotions.Count > 0)
                {
                    Debug.Log($"🎭 找到 {associatedMotions.Count} 个关联动作，开始应用...");
                    foreach (var motionId in associatedMotions)
                    {
                        yield return new WaitForEndOfFrame(); // 分帧处理
                        try
                        {
                            ApplyMotionToActor(actorId, motionId);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"❌ 应用动作 {motionId} 到演员 {actorId} 时出错: {e.Message}");
                        }
                    }
                }
                else
                {
                    Debug.Log($"ℹ️ 模型 {modelComponent.id} 没有关联的动作");
                }
            }
            Debug.Log($"🎉 演员创建完成: {actorId}");
            // 触发事件
            try
            {
                SceneDisplayEvents.TriggerActorSpawned(actorId, modelComponent.id);
                SceneDisplayEvents.TriggerActorListChanged();
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ 触发事件时出错: {e.Message}");
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
            var motionComponent = ResourceManager.Instance?.GetMotion(motionId);
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
                var actorComponent = actorObj.GetComponent<ActorComponent>();
                if (actorComponent != null)
                {
                    actorComponent.AddMotion(motionId);
                }
            }
            Debug.Log($"SceneDisplayManager: 关联动作 {motionId} 到演员 {actorId}");
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
        public ActorComponent GetActor(string actorId)
        {
            Transform actorObj = actorContainer.Find($"Actor_{actorId}");
            return actorObj?.GetComponent<ActorComponent>();
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
                var actorComponent = child.GetComponent<ActorComponent>();
                if (actorComponent != null && actorComponent.actorId == actorId)
                {
                    return child.gameObject;
                }
            }
            
            return null;
        }        // ==================== 音乐播放控制 ====================
        
        /// <summary>
        /// 激活音乐播放
        /// </summary>
        public void ActivateMusic(string musicId)
        {
            var musicComponent = ResourceManager.Instance?.GetMusic(musicId);
            if (musicComponent == null)
            {
                Debug.LogError($"SceneDisplayManager: 找不到音乐资源 {musicId}");
                return;
            }

            // 停止当前音乐（如果绑定了AudioSource）
            if (musicAudioSource != null && musicAudioSource.isPlaying)
            {
                musicAudioSource.Stop();
            }

            // 激活新音乐
            currentActiveMusicId = musicId;

            // 自动将音频文件加载到绑定的AudioSource中
            if (musicAudioSource != null)
            {
                musicAudioSource.clip = null;
                Debug.Log($"SceneDisplayManager: 音频文件已加载到AudioSource - {musicComponent.displayName}");
            }
            else
            {
                if (musicAudioSource == null)
                    Debug.LogWarning("SceneDisplayManager: musicAudioSource未绑定，请在Inspector中设置");
            }

            Debug.Log($"SceneDisplayManager: 激活音乐 {musicId}");
            SceneDisplayEvents.TriggerMusicActivated(musicId);
        }        /// <summary>
        /// 获取当前激活音乐的AudioSource
        /// </summary>
        public AudioSource GetActiveMusicAudioSource()
        {
            // 直接返回SceneDisplayManager绑定的AudioSource
            return musicAudioSource;
        }
          /// <summary>
        /// 获取第一个可用音乐并激活
        /// </summary>
        public string GetAndActivateFirstAvailableMusic()
        {
            var musicList = ResourceManager.Instance?.musicList;
            if (musicList != null && musicList.Count > 0)
            {
                var firstMusic = musicList[0];
                ActivateMusic(firstMusic.id);
                return firstMusic.id;
            }
            return "";
        }

        /// <summary>
        /// 获取第一个可用摄像机并激活
        /// </summary>
        public string GetAndActivateFirstAvailableCamera()
        {
            var cameraList = ResourceManager.Instance?.cameraList;
            if (cameraList != null && cameraList.Count > 0)
            {
                var firstCamera = cameraList[0];
                ActivateCamera(firstCamera.id);
                return firstCamera.id;
            }
            // 如果没有加载的摄像机，激活内置Free Camera
            ActivateCamera("BUILTIN_FREE_CAMERA");
            return "BUILTIN_FREE_CAMERA";
        }

        /// <summary>
        /// 激活摄像机
        /// </summary>
        public void ActivateCamera(string cameraId)
        {
            // 这里只做ID有效性判断，具体组件获取可根据业务调整
            var cameraData = ResourceManager.Instance?.cameraList?.Find(c => c.id == cameraId);
            if (cameraData == null && cameraId != "BUILTIN_FREE_CAMERA")
            {
                Debug.LogError($"SceneDisplayManager: 找不到摄像机资源 {cameraId}");
                return;
            }
            currentActiveCameraId = cameraId;
            SceneDisplayEvents.TriggerCameraActivated(cameraId);
        }        /// <summary>
        /// 应用摄像机状态
        /// </summary>
        public void ApplyCameraState(MMDVR.Scripts.Components.CameraState cameraState)
        {
            if (currentActiveCameraId == "BUILTIN_FREE_CAMERA")
                return;
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
        /// <summary>
        /// 与ResourceManager的资源列表同步，确保场景状态与资源状态一致
        /// 只处理资源删除的清理，不自动创建新演员
        /// </summary>
        private void SyncWithResourceLists()
        {
            var currentModels = ResourceManager.Instance?.GetModelList();
            var currentMotions = ResourceManager.Instance?.GetMotionList();

            // 移除已被删除的模型对应的演员
            var actorsToRemove = new List<ActorData>();
            foreach (var actor in actorList)
            {
                bool modelExists = currentModels.Any(m => m.id == actor.modelId);
                if (!modelExists)
                {
                    actorsToRemove.Add(actor);
                }
            }

            foreach (var actor in actorsToRemove)
            {
                RemoveActor(actor.id);
                Debug.Log($"SceneDisplayManager: 移除了不存在模型 {actor.modelId} 对应的演员 {actor.id}");
            }            
            // 清理无效的动作关联
            foreach (var actor in actorList)
            {
                if (!string.IsNullOrEmpty(actor.motionId))
                {
                    bool motionExists = currentMotions.Any(m => m.motionId == actor.motionId);
                    if (!motionExists)
                    {
                        actor.motionId = null;
                        Debug.Log($"SceneDisplayManager: 清除了演员 {actor.id} 的无效动作关联");
                    }
                }
            }
        }/// <summary>
        /// 同步当前活动摄像机与CameraListController的ListSortAndActivate DropZone第一项
        /// </summary>
        public void SyncActiveCameraWithList()
        {
            if (cameraListController == null) return;

            // 直接从CameraListController获取第一项ID
            var firstCameraId = cameraListController.GetFirstCameraId();
            if (!string.IsNullOrEmpty(firstCameraId) && firstCameraId != currentActiveCameraId)
            {
                ActivateCamera(firstCameraId);
            }
            else if (string.IsNullOrEmpty(firstCameraId))
            {
                // 没有可用摄像机，强制切回自由相机
                ActivateCamera("BUILTIN_FREE_CAMERA");
            }
        }

        /// <summary>
        /// 同步当前活动音乐与MusicListController的ListSortAndActivate DropZone第一项
        /// </summary>
        public void SyncActiveMusicWithList()
        {
            if (musicListController == null) return;

            // 直接从MusicListController获取第一项ID
            var firstMusicId = musicListController.GetFirstMusicId();
            if (!string.IsNullOrEmpty(firstMusicId) && firstMusicId != currentActiveMusicId)
            {
                ActivateMusic(firstMusicId);
                Debug.Log($"SceneDisplayManager: 根据MusicListController同步活动音乐为 {firstMusicId}");
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
            // 通过遍历actorContainer查找ActorComponent.actorId匹配（与原系统保持一致）
            Transform actorTransform = null;
            ActorComponent currentActorComponent = null;
            
            for (int i = 0; i < actorContainer.childCount; i++)
            {
                Transform child = actorContainer.GetChild(i);
                var ac = child.GetComponent<ActorComponent>();
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
            var motionComponent = ResourceManager.Instance?.GetMotion(motionId);
            if (motionComponent == null)
            {
                Debug.LogError($"SceneDisplayManager: 找不到动作资源 {motionId}");
                return;
            }            // 获取MmdGameObject并加载动作
            var mmdGameObject = actorTransform.GetComponent<MmdGameObject>();
            if (mmdGameObject != null && !string.IsNullOrEmpty(motionComponent.filePath))
            {
                try
                {
                    if (File.Exists(motionComponent.filePath))
                    {
                        Debug.Log($"SceneDisplayManager: 加载VMD动作文件到演员 {actorId}: {motionComponent.filePath}");
                        mmdGameObject.LoadMotion(motionComponent.filePath);
                        
                        // 更新ActorComponent的动作关联
                        if (currentActorComponent != null)
                        {
                            currentActorComponent.AddMotion(motionComponent.motionId);
                        }
                        
                        Debug.Log($"SceneDisplayManager: 成功为演员 {actorId} 加载动作 {motionComponent.filePath}");
                    }
                    else
                    {
                        Debug.LogError($"SceneDisplayManager: 动作文件不存在: {motionComponent.filePath}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"SceneDisplayManager: 加载动作到演员 {actorId} 时出错: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"SceneDisplayManager: 演员 {actorId} 没有MmdGameObject组件或动作文件路径为空");
            }

            Debug.Log($"SceneDisplayManager: 关联动作 {motionId} 到演员 {actorId}");
            
            // 触发事件
            if (actor != null)
            {
                SceneDisplayEvents.TriggerModelMotionAssociationChanged(actor.modelId, motionId, true);
            }
        }

        /// <summary>
        /// 将动作应用到指定的Actor
        /// </summary>
        private void ApplyMotionToActor(string actorId, string motionId)
        {
            Debug.Log($"🎬 应用动作 {motionId} 到演员 {actorId}");
            Transform actorTransform = actorContainer.Find($"Actor_{actorId}");
            if (actorTransform == null)
            {
                Debug.LogError($"❌ 找不到演员: Actor_{actorId}");
                return;
            }
            var mmdGameObject = actorTransform.GetComponent<MmdGameObject>();
            if (mmdGameObject == null)
            {
                Debug.LogError($"❌ 演员 {actorId} 没有MmdGameObject组件");
                return;
            }
            var motionComponent = ResourceManager.Instance?.GetMotion(motionId);
            if (motionComponent == null)
            {
                Debug.LogError($"❌ 找不到动作资源: {motionId}");
                return;
            }
            try
            {
                Debug.Log($"🎭 加载VMD动作文件: {motionComponent.filePath}");
                mmdGameObject.LoadMotion(motionComponent.filePath);
                // 加载动作后再设置物理模式
                mmdGameObject.PhysicsMode = MmdGameObject.PhysicsModeEnum.Bullet;
                // 更新Actor数据中的动作关联
                var actorData = actorList.Find(a => a.id == actorId);
                if (actorData != null)
                {
                    if (!actorData.motionIds.Contains(motionId))
                    {
                        actorData.motionIds.Add(motionId);
                    }
                    Debug.Log($"✅ 动作 {motionId} 成功应用到演员 {actorId}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ 应用动作 {motionId} 到演员 {actorId} 时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 从指定的Actor移除动作
        /// </summary>
        private void RemoveMotionFromActor(string actorId, string motionId)
        {
            Debug.Log($"🎬 从演员 {actorId} 移除动作 {motionId}");
            
            // 查找Actor GameObject
            Transform actorTransform = actorContainer.Find($"Actor_{actorId}");
            if (actorTransform == null)
            {
                Debug.LogError($"❌ 找不到演员: Actor_{actorId}");
                return;
            }
            
            // 获取MmdGameObject组件
            var mmdGameObject = actorTransform.GetComponent<MmdGameObject>();
            if (mmdGameObject == null)
            {
                Debug.LogError($"❌ 演员 {actorId} 没有MmdGameObject组件");
                return;
            }
            
            try
            {
                // 移除动作（这里可能需要调用MmdGameObject的相应方法）
                // 注意：LibMMD可能没有直接的"移除动作"方法，可能需要重新加载或清空
                Debug.Log($"🎭 尝试移除动作 {motionId} 从演员 {actorId}");
                
                // 更新Actor数据中的动作关联
                var actorData = actorList.Find(a => a.id == actorId);
                if (actorData != null)
                {
                    actorData.motionIds.Remove(motionId);
                    Debug.Log($"✅ 动作 {motionId} 已从演员数据 {actorId} 中移除");
                      // 如果没有其他动作了，可能需要重置到默认姿势
                    if (actorData.motionIds.Count == 0)
                    {
                        Debug.Log($"🔄 演员 {actorId} 已无关联动作，重置到默认姿势");
                        // TODO: 这里可能需要调用MmdGameObject的方法来重置姿势
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ 移除动作时出错: {e.Message}");
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
        /// 获取与指定动作关联的模型ID列表
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
            }            return associatedModels;
        }

        /// <summary>
        /// 获取激活的摄像机数据
        /// </summary>
        public CameraData GetActiveCameraData()
        {
            return ResourceManager.Instance?.cameraList.Find(c => c.id == currentActiveCameraId);
        }

        /// <summary>
        /// 切换模型显示状态
        /// </summary>
        public void ToggleModel(string modelId)
        {
            var modelComponent = ResourceManager.Instance?.GetModel(modelId);
            if (modelComponent != null)
            {
                modelComponent.SetVisibility(!modelComponent.IsVisible);
                Debug.Log($"SceneDisplayManager: 切换模型 {modelId} 显示状态为 {modelComponent.IsVisible}");
            }
        }

        /// <summary>
        /// 根据ID获取Actor对象
        /// </summary>
        public GameObject GetActorObjectById(string modelId)
        {
            var modelComponent = ResourceManager.Instance?.GetModel(modelId);
            return modelComponent?.gameObject;
        }

        /// <summary>
        /// 检查模型是否被禁用
        /// </summary>
        public bool IsModelDisabled(string modelId)
        {
            var modelComponent = ResourceManager.Instance?.GetModel(modelId);
            return modelComponent != null && !modelComponent.IsVisible;
        }

        /// <summary>
        /// 设置活动音乐
        /// </summary>
        public void SetActiveMusic(string musicId)
        {
            // 先清除之前激活的音乐状态
            if (!string.IsNullOrEmpty(currentActiveMusicId))
            {
                var previousMusic = ResourceManager.Instance?.GetMusic(currentActiveMusicId);
                if (previousMusic != null)
                {
                    previousMusic.SetActive(false);
                }
            }
            currentActiveMusicId = musicId;
            var musicComponent = ResourceManager.Instance?.GetMusic(musicId);
            if (musicComponent != null)
            {
                musicComponent.SetActive(true);
                // 异步加载音频到唯一AudioSource
                if (musicAudioSource != null)
                {
                    StartCoroutine(LoadAndAssignAudioClip(musicComponent.filePath));
                }
                else
                {
                    Debug.LogWarning("SceneDisplayManager: musicAudioSource未绑定");
                }
                EventManager.OnMusicActivated?.Invoke(musicComponent);
            }
        }
        // 修正：IEnumerator LoadAndAssignAudioClip 不带类型参数
        private System.Collections.IEnumerator LoadAndAssignAudioClip(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) yield break;
            string url = "file://" + filePath.Replace("\\", "/");
            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, GetAudioTypeByExtension(filePath)))
            {
                yield return www.SendWebRequest();
                if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"音频加载失败: {filePath}, {www.error}");
                }
                else
                {
                    var clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                    musicAudioSource.clip = clip;
                    Debug.Log($"SceneDisplayManager: 音频已加载到AudioSource: {filePath}");
                }
            }
        }
        private AudioType GetAudioTypeByExtension(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            if (ext == ".mp3") return AudioType.MPEG;
            if (ext == ".ogg") return AudioType.OGGVORBIS;
            return AudioType.WAV;
        }

        /// <summary>
        /// 添加演员到场景（基于文件路径，会自动管理ModelComponent）
        /// </summary>
        public string AddActorFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("SceneDisplayManager: filePath为空");
                return null;
            }

            string displayName = System.IO.Path.GetFileNameWithoutExtension(filePath);

            // 先查找ResourceManager的Models容器下是否已有对应ModelComponent（通过filePath）
            ModelComponent modelComponent = null;
            // 解耦：通过事件/缓存获取modelContainer，不直接依赖ResourceManager
            var modelContainer = MMDVR.Scripts.Managers.ResourceManager.Instance?.modelContainer;
            
            for (int i = 0; i < modelContainer.childCount; i++)
            {
                var mc = modelContainer.GetChild(i).GetComponent<ModelComponent>();
                if (mc != null && mc.filePath == filePath)
                {
                    modelComponent = mc;
                    break;
                }
            }

            // 如果没有找到，则通过ResourceManager创建一个新的ModelComponent
            if (modelComponent == null)
            {
                // 通过事件驱动模型创建，不直接依赖ResourceManager
                MMDVR.Events.ResourceEvents.TriggerModelLoadRequest(filePath);
                return null;
            }

            // 使用现有的AddActor(modelId)方法创建演员
            return AddActor(modelComponent.id);
        }

        // ==================== 公共状态查询方法 ====================

        /// <summary>
        /// 获取当前活动摄像机ID
        /// </summary>
        public string GetCurrentActiveCameraId()
        {
            return currentActiveCameraId;
        }

        /// <summary>
        /// 获取当前活动音乐ID
        /// </summary>
        public string GetCurrentActiveMusicId()
        {
            return currentActiveMusicId;
        }

        /// <summary>
        /// 获取所有可见演员的列表
        /// </summary>
        public List<ActorData> GetVisibleActors()
        {
            return actorList.Where(a => a.isVisible).ToList();
        }

        /// <summary>
        /// 获取所有演员的数量
        /// </summary>
        public int GetActorCount()
        {
            return actorList.Count;
        }

        /// <summary>
        /// 检查指定模型是否已有对应的演员
        /// </summary>
        public bool HasActorForModel(string modelId)
        {
            return actorList.Any(a => a.modelId == modelId);
        }

        /// <summary>
        /// 获取指定模型对应的演员ID
        /// </summary>
        public string GetActorIdForModel(string modelId)
        {
            var actor = actorList.Find(a => a.modelId == modelId);
            return actor?.id;
        }

        /// <summary>
        /// 获取场景中所有模型的关联状态摘要
        /// </summary>
        public Dictionary<string, List<string>> GetAllModelMotionAssociations()
        {
            var associations = new Dictionary<string, List<string>>();
            
            foreach (var actor in actorList)
            {
                if (!string.IsNullOrEmpty(actor.modelId))
                {
                    if (!associations.ContainsKey(actor.modelId))
                    {
                        associations[actor.modelId] = new List<string>();
                    }
                    
                    if (!string.IsNullOrEmpty(actor.motionId))
                    {
                        associations[actor.modelId].Add(actor.motionId);
                    }
                }
            }
            
            return associations;        }

        /// <summary>
        /// 强制同步所有状态（手动触发同步）
        /// </summary>
        public void ForceSyncAllStates()
        {
            Debug.Log("SceneDisplayManager: 强制同步所有状态");
            SyncWithResourceLists();
            SyncActiveCameraWithList();
            SyncActiveMusicWithList();
        }

        // ==================== 与PlaybackManager的交互接口 ====================

        /// <summary>
        /// 获取当前播放所需的所有演员对象（用于PlaybackManager）
        /// </summary>
        public List<GameObject> GetAllActorGameObjects()
        {
            var actorObjects = new List<GameObject>();
            
            if (actorContainer != null)
            {
                for (int i = 0; i < actorContainer.childCount; i++)
                {
                    Transform child = actorContainer.GetChild(i);
                    var actorComponent = child.GetComponent<ActorComponent>();
                    if (actorComponent != null)
                    {
                        actorObjects.Add(child.gameObject);
                    }
                }
            }
            
            return actorObjects;
        }

        /// <summary>
        /// 获取当前活动的MmdGameObject列表（用于播放控制）
        /// </summary>
        public List<MmdGameObject> GetActiveMmdGameObjects()
        {
            var mmdObjects = new List<MmdGameObject>();
            
            foreach (var actor in actorList)
            {
                if (actor.isVisible)
                {
                    Transform actorObj = actorContainer.Find($"Actor_{actor.id}");
                    if (actorObj != null)
                    {
                        var mmdGameObject = actorObj.GetComponent<MmdGameObject>();
                        if (mmdGameObject != null)
                        {
                            mmdObjects.Add(mmdGameObject);
                        }
                    }
                }
            }
            
            return mmdObjects;
        }

        /// <summary>
        /// 检查场景是否准备好播放（有模型、动作、音乐等）
        /// </summary>
        public bool IsSceneReadyForPlayback()
        {
            bool hasVisibleActors = actorList.Any(a => a.isVisible);
            bool hasActiveMusic = !string.IsNullOrEmpty(currentActiveMusicId);
            bool hasActiveCamera = !string.IsNullOrEmpty(currentActiveCameraId);
            
            return hasVisibleActors && hasActiveMusic && hasActiveCamera;
        }

        /// <summary>
        /// 获取播放状态信息（用于调试和状态显示）
        /// </summary>
        public string GetPlaybackStatusInfo()
        {
            // 示例：返回当前音乐、摄像机、演员数量等信息
            return $"Music: {currentActiveMusicId}, Camera: {currentActiveCameraId}, ActorCount: {actorList.Count}";
        }
        
        /// <summary>
        /// 内部方法：更新Actor的动作关联数据（仅供内部表现层使用）
        /// 注意：这是表现层内部数据同步，不触发数据层事件
        /// </summary>
        private void UpdateActorMotionAssociation(string modelId, string motionId)
        {
            var actor = actorList.Find(a => a.modelId == modelId);
            if (actor != null)
            {
                if (!actor.motionIds.Contains(motionId))
                {
                    actor.motionIds.Add(motionId);
                }
                Debug.Log($"SceneDisplayManager: 更新Actor {actor.id} 的动作关联: {motionId}");
            }
            else
            {
                Debug.LogWarning($"SceneDisplayManager: 找不到模型 {modelId} 对应的Actor，无法更新动作关联");
            }
        }
    }
}
