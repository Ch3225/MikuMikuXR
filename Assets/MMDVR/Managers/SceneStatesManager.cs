using System.Collections.Generic;
using UnityEngine;
using MMDVR.Scripts.UIInteraction;
using MMDVR.Components;
using System.Collections;
using System.Linq;
using UnityEngine.Networking;
using System.IO;
using LibMMD.Unity3D; // Added for MmdUnityConfig and MmdPhysicsMode

namespace MMDVR.Managers
{
    /// <summary>
    /// 摄像机模式枚举
    /// </summary>
    public enum CameraMode
    {
        Desktop,  // 桌面模式
        VR        // VR模式
    }

    /// <summary>
    /// 统一的场景状态管理器 - 直接管理所有资源容器及其子对象
    /// 完全基于容器架构，不再使用独立的Manager
    /// 资源组织结构: SceneStatesManager/[Musics|Actors|Cameras|Motions]/ResourceObjects
    /// </summary>
    public class SceneStatesManager : MonoBehaviour
    {
        public static SceneStatesManager Instance { get; private set; }

        [Header("播放状态")]
        [Tooltip("是否正在播放")] public bool isPlaying;
        [Tooltip("当前播放进度（秒）")] [Range(0, 9999)] public float playTime;
        [Tooltip("播放时长（秒）")] public float totalDuration;        [Header("功能摄像机引用")]
        [Tooltip("主摄像机（统一使用）")] public Camera mainCamera;
        [Tooltip("VR Origin GameObject")] public GameObject vrOrigin;

        [Header("运行模式")]
        [Tooltip("当前运行模式")] public CameraMode currentCameraMode = CameraMode.Desktop;

        [Header("资源容器引用")]
        [Tooltip("模型资源容器")] public Transform modelContainer;
        [Tooltip("场景演员容器")] public Transform actorContainer;
        [Tooltip("动作资源容器")] public Transform motionContainer;
        [Tooltip("摄像机资源容器")] public Transform cameraContainer;
        [Tooltip("音乐资源容器")] public Transform musicContainer;

        [Header("当前激活状态")]
        public string currentActiveMusicId;
        public string currentActiveCameraId;

        [Header("资源关联")]
        [Tooltip("模型-动作关联映射")] 
        private Dictionary<string, List<string>> modelMotionAssociations = new Dictionary<string, List<string>>();
        
        [Header("资源状态")]
        [Tooltip("禁用的模型列表")]
        private HashSet<string> disabledModelIds = new HashSet<string>();

        [Header("同步控制")]
        [Tooltip("播放同步器")] public MMDVR.Scripts.Synchronization.PlaybackSynchronizer synchronizer;

        // Free Camera默认设置
        private Vector3 freeCameraPosition = Vector3.zero;
        private Quaternion freeCameraRotation = Quaternion.identity;
        private float freeCameraFOV = 60f;        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);            InitializeResourceContainers();
            InitializeDefaultCamera();
        }

        private void InitializeCameraMode()
        {
            // 初始化摄像机模式，设置容器状态
            SetCameraMode(currentCameraMode);
        }

        /// <summary>
        /// 设置摄像机模式 - 可被外部脚本调用
        /// </summary>
        public void SetCameraMode(CameraMode mode)
        {
            currentCameraMode = mode;
            
            switch (mode)
            {
                case CameraMode.Desktop:
                    if (vrOrigin != null) vrOrigin.SetActive(false);
                    break;
                    
                case CameraMode.VR:
                    if (vrOrigin != null) vrOrigin.SetActive(true);
                    break;
            }
            
            Debug.Log($"摄像机模式切换到: {mode}");
        }

        /// <summary>
        /// 获取当前活动摄像机
        /// </summary>
        public Camera GetActiveCamera()
        {
            if (currentCameraMode == CameraMode.VR && vrOrigin != null)
            {
                // VR模式下从XR Origin中获取摄像机
                var vrCamera = vrOrigin.GetComponentInChildren<Camera>();
                if (vrCamera != null) return vrCamera;
            }
            
            // 桌面模式或VR找不到时使用主摄像机
            return mainCamera;
        }        private void InitializeResourceContainers()
        {
            // 如果容器不存在，创建它们
            if (musicContainer == null)
                musicContainer = CreateResourceContainer("Musics");
            if (modelContainer == null)
                modelContainer = CreateResourceContainer("Models");
            if (motionContainer == null)
                motionContainer = CreateResourceContainer("Motions");
            if (cameraContainer == null)
            {
                cameraContainer = CreateResourceContainer("Cameras");
                // 创建相机子容器
                CreateResourceContainer("MMDCameras", cameraContainer);
                CreateResourceContainer("FreeCameras", cameraContainer);
            }
        }        private Transform CreateResourceContainer(string name)
        {
            // 先尝试查找现有容器
            Transform existing = transform.Find(name);
            if (existing != null) return existing;
            
            // 创建新容器
            GameObject container = new GameObject(name);
            container.transform.SetParent(this.transform);
            return container.transform;
        }

        private Transform CreateResourceContainer(string name, Transform parent)
        {
            // 先尝试查找现有容器
            Transform existing = parent.Find(name);
            if (existing != null) return existing;
            
            // 创建新容器
            GameObject container = new GameObject(name);
            container.transform.SetParent(parent);
            return container.transform;
        }        private void InitializeDefaultCamera()
        {
            // 确保始终有一个Free Camera
            if (string.IsNullOrEmpty(currentActiveCameraId))
            {
                currentActiveCameraId = "BUILTIN_FREE_CAMERA";
                
                // 获取FreeCameras容器
                Transform freeCamerasContainer = cameraContainer.Find("FreeCameras");
                
                // 创建Free Camera资源对象（如果不存在）
                Transform freeCamObj = freeCamerasContainer.Find("FreeCamera_Resource");
                if (freeCamObj == null)
                {
                    GameObject freeCameraResource = new GameObject("FreeCamera_Resource");
                    freeCameraResource.transform.SetParent(freeCamerasContainer);
                    
                    // 添加标识组件
                    var freeCamComponent = freeCameraResource.AddComponent<FreeCameraComponent>();
                    freeCamComponent.id = "BUILTIN_FREE_CAMERA";
                    freeCamComponent.displayName = "Free Camera";
                    freeCamComponent.position = freeCameraPosition;
                    freeCamComponent.rotation = freeCameraRotation;
                    freeCamComponent.fieldOfView = freeCameraFOV;
                }
            }
        }

        private void Update()
        {
            if (isPlaying)
            {
                // 更新播放时间
                playTime += Time.deltaTime;
                
                // 更新VMD摄像机
                UpdateVMDCamera();
                
                // 检查播放结束
                if (playTime >= totalDuration && totalDuration > 0)
                {
                    Pause();
                }
            }
        }        private void UpdateVMDCamera()
        {
            if (!string.IsNullOrEmpty(currentActiveCameraId) && currentActiveCameraId != "BUILTIN_FREE_CAMERA")
            {
                Transform mmdCamerasContainer = cameraContainer.Find("MMDCameras");
                Transform cameraObj = mmdCamerasContainer.Find($"VMDCamera_{currentActiveCameraId}");
                if (cameraObj != null)
                {
                    var mmdCameraComponent = cameraObj.GetComponent<MMDCameraComponent>();
                    if (mmdCameraComponent != null && mmdCameraComponent.vmdCameraData != null)
                    {
                        // 更新VMD摄像机状态
                        var cameraState = mmdCameraComponent.vmdCameraData.GetCameraStateAtTime(playTime);
                        if (cameraState != null)
                        {
                            ApplyCameraState(cameraState);
                        }
                    }
                }
            }
        }

        // ===== 音乐资源管理 =====
        
        public void AddMusic(string filePath)
        {
            string id = System.Guid.NewGuid().ToString();
            string displayName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            
            // 创建音乐资源GameObject
            GameObject musicObj = new GameObject($"Music_{id}");
            musicObj.transform.SetParent(musicContainer);
            
            // 添加组件
            var musicComponent = musicObj.AddComponent<MusicComponent>();
            musicComponent.id = id;
            musicComponent.displayName = displayName;
            musicComponent.filePath = filePath;
            
            var audioSource = musicObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            musicComponent.audioSource = audioSource;
            
            // 异步加载音频
            StartCoroutine(LoadAudioClip(filePath, audioSource, id));
            
            // 通知UI更新
            EventManager.Instance?.TriggerEvent("MusicListUpdated");
            
            Debug.Log($"音乐已添加: {displayName} (ID: {id})");
        }

        private IEnumerator LoadAudioClip(string filePath, AudioSource audioSource, string musicId)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.UNKNOWN))
            {
                yield return www.SendWebRequest();
                
                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    audioSource.clip = clip;
                    
                    Debug.Log($"音频加载成功: {System.IO.Path.GetFileName(filePath)}");
                    
                    // 通知UI更新（可能需要显示时长等信息）
                    EventManager.Instance?.TriggerEvent("MusicListUpdated");
                }
                else
                {
                    Debug.LogError($"音频加载失败: {www.error}");
                }
            }
        }        public void RemoveMusic(string musicId)
        {
            Transform musicObj = musicContainer.Find($"Music_{musicId}");
            if (musicObj != null)
            {
                // 如果正在播放这首音乐，先停止
                if (currentActiveMusicId == musicId)
                {
                    Pause();
                    currentActiveMusicId = null;
                }
                
                DestroyImmediate(musicObj.gameObject);
                EventManager.Instance?.TriggerEvent("MusicListUpdated");
                Debug.Log($"音乐已移除: {musicId}");
            }
            else
            {
                Debug.LogWarning($"未找到要删除的音乐: Music_{musicId}");
            }
        }

        public void ActivateMusic(string musicId)
        {
            // 停止当前音乐
            if (!string.IsNullOrEmpty(currentActiveMusicId))
            {
                Transform currentMusicObj = musicContainer.Find($"Music_{currentActiveMusicId}");
                if (currentMusicObj != null)
                {
                    var audioSource = currentMusicObj.GetComponent<AudioSource>();
                    if (audioSource != null) audioSource.Stop();
                }
            }
            
            // 激活新音乐
            currentActiveMusicId = musicId;
            Transform newMusicObj = musicContainer.Find($"Music_{musicId}");
            if (newMusicObj != null)
            {
                var audioSource = newMusicObj.GetComponent<AudioSource>();
                if (audioSource != null && audioSource.clip != null)
                {
                    totalDuration = audioSource.clip.length;
                    playTime = 0f;
                    Debug.Log($"音乐已激活: {musicId}");
                }
            }
            
            EventManager.Instance?.TriggerEvent("MusicActivated");
        }

        public List<MusicData> GetMusicList()
        {
            List<MusicData> musicList = new List<MusicData>();
            
            for (int i = 0; i < musicContainer.childCount; i++)
            {
                Transform child = musicContainer.GetChild(i);
                var musicComponent = child.GetComponent<MusicComponent>();
                if (musicComponent != null)
                {
                    musicList.Add(new MusicData
                    {
                        id = musicComponent.id,
                        title = musicComponent.displayName,
                        filePath = musicComponent.filePath
                    });
                }
            }
            
            return musicList;
        }

        // ===== 摄像机资源管理 =====
          public void AddVMDCamera(string filePath)
        {
            string id = System.Guid.NewGuid().ToString();
            string displayName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            
            // 获取MMDCameras容器
            Transform mmdCamerasContainer = cameraContainer.Find("MMDCameras");
            
            // 创建VMD摄像机资源GameObject
            GameObject cameraObj = new GameObject($"VMDCamera_{id}");
            cameraObj.transform.SetParent(mmdCamerasContainer);
            
            // 添加VMD摄像机组件
            var mmdCameraComponent = cameraObj.AddComponent<MMDCameraComponent>();
            mmdCameraComponent.id = id;
            mmdCameraComponent.displayName = displayName;
            mmdCameraComponent.filePath = filePath;
            
            // 加载VMD数据
            try
            {
                mmdCameraComponent.LoadVMDData(filePath);
                Debug.Log($"VMD摄像机已添加: {displayName} (ID: {id})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"VMD摄像机加载失败: {e.Message}");
                DestroyImmediate(cameraObj);
                return;
            }
            
            // 通知UI更新
            EventManager.Instance?.TriggerEvent("CameraListUpdated");
        }        public void RemoveCamera(string cameraId)
        {
            // 不能删除内置Free Camera
            if (cameraId == "BUILTIN_FREE_CAMERA") return;
            
            // 在MMDCameras容器中查找VMD摄像机
            Transform mmdCamerasContainer = cameraContainer.Find("MMDCameras");
            Transform cameraObj = mmdCamerasContainer?.Find($"VMDCamera_{cameraId}");
            
            if (cameraObj != null)
            {
                // 如果正在使用这个摄像机，切换到Free Camera
                if (currentActiveCameraId == cameraId)
                {
                    ActivateCamera("BUILTIN_FREE_CAMERA");
                }
                
                // 立即销毁，确保UI刷新时已经不存在了
                DestroyImmediate(cameraObj.gameObject);
                EventManager.Instance?.TriggerEvent("CameraListUpdated");
                Debug.Log($"摄像机已移除: {cameraId}");
            }
            else
            {
                Debug.LogError($"未找到要删除的摄像机: VMDCamera_{cameraId}");
            }
        }

        public void ActivateCamera(string cameraId)
        {
            currentActiveCameraId = cameraId;
            
            if (cameraId == "BUILTIN_FREE_CAMERA")
            {
                // 使用Free Camera
                ApplyFreeCameraState();
            }
            else
            {
                // 使用VMD摄像机 - 在Update中实时更新
                Debug.Log($"VMD摄像机已激活: {cameraId}");
            }
            
            EventManager.Instance?.TriggerEvent("CameraActivated");
        }        private void ApplyFreeCameraState()
        {
            Transform freeCamerasContainer = cameraContainer.Find("FreeCameras");
            Transform freeCamObj = freeCamerasContainer.Find("FreeCamera_Resource");
            if (freeCamObj != null)
            {
                var freeCamComponent = freeCamObj.GetComponent<FreeCameraComponent>();
                if (freeCamComponent != null)
                {
                    // 应用Free Camera状态到当前激活的摄像机
                    Camera activeCamera = GetActiveCamera();
                    if (activeCamera != null)
                    {
                        activeCamera.transform.position = freeCamComponent.position;
                        activeCamera.transform.rotation = freeCamComponent.rotation;
                        activeCamera.fieldOfView = freeCamComponent.fieldOfView;
                    }
                }
            }
        }

        private void ApplyCameraState(MMDVR.Components.CameraState cameraState)
        {
            // 应用VMD摄像机状态到当前激活的摄像机
            Camera activeCamera = GetActiveCamera();
            if (activeCamera != null)
            {
                activeCamera.transform.position = cameraState.position;
                activeCamera.transform.rotation = cameraState.rotation;
                activeCamera.fieldOfView = cameraState.fieldOfView;
            }
        }

        // Make ApplyCameraState public
        public void PublicApplyCameraState(MMDVR.Components.CameraState cameraState)
        {
            ApplyCameraState(cameraState);
        }        public List<CameraData> GetCameraList()
        {
            List<CameraData> cameraList = new List<CameraData>();
            
            // 添加内置Free Camera
            cameraList.Add(new CameraData
            {
                id = "BUILTIN_FREE_CAMERA",
                displayName = "Free Camera",
                filePath = "",
                isFreeCamera = true
            });
            
            // 添加VMD摄像机 - 从MMDCameras容器中查找
            Transform mmdCamerasContainer = cameraContainer.Find("MMDCameras");
            if (mmdCamerasContainer != null)
            {
                for (int i = 0; i < mmdCamerasContainer.childCount; i++)
                {
                    Transform child = mmdCamerasContainer.GetChild(i);
                    var mmdCameraComponent = child.GetComponent<MMDCameraComponent>();
                    if (mmdCameraComponent != null)
                    {
                        cameraList.Add(new CameraData
                        {
                            id = mmdCameraComponent.id,
                            displayName = mmdCameraComponent.displayName,
                            filePath = mmdCameraComponent.filePath,
                            isFreeCamera = false
                        });
                    }
                }
            }
            
            return cameraList;
        }

        // ===== 演员资源管理 =====
          public void AddActor(string filePath)
        {
            string displayName = System.IO.Path.GetFileNameWithoutExtension(filePath);

            // 先查找modelContainer下是否已有对应ModelComponent（通过filePath）
            ModelComponent modelComponent = null;
            for (int i = 0; i < modelContainer.childCount; i++)
            {
                var mc = modelContainer.GetChild(i).GetComponent<ModelComponent>();
                if (mc != null && mc.filePath == filePath)
                {
                    modelComponent = mc;
                    break;
                }
            }            // 如果没有找到，则创建一个新的ModelComponent
            if (modelComponent == null)
            {
                string newModelId = System.Guid.NewGuid().ToString(); // 为新模型生成GUID
                GameObject modelObj = new GameObject($"Model_{newModelId}");
                modelObj.transform.SetParent(modelContainer);
                modelComponent = modelObj.AddComponent<ModelComponent>();modelComponent.id = newModelId;
                modelComponent.displayName = displayName;
                modelComponent.filePath = filePath;
                // 触发模型列表更新事件
                EventManager.Instance?.TriggerEvent("ModelListChanged");
            }

            // 创建Actor GameObject
            GameObject actorObj = new GameObject($"Actor_{displayName}");
            actorObj.transform.SetParent(actorContainer);

            // 添加组件
            var actorComponent = actorObj.AddComponent<SceneActorComponent>();
            actorComponent.modelRef = modelComponent;
            actorComponent.actorId = modelComponent.id; // Actor ID 与 Model ID 保持一致
            actorComponent.displayName = modelComponent.displayName;

            // 加载PMX模型
            try
            {
                LoadPMXModel(filePath, actorObj, actorComponent);
                Debug.Log($"演员已添加: {displayName} (ID: {actorComponent.actorId})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"PMX模型加载失败: {e.Message}");
                DestroyImmediate(actorObj);
                return;
            }
            EventManager.Instance?.TriggerEvent("ActorListUpdated");
        }        private void LoadPMXModel(string filePath, GameObject actorObj, SceneActorComponent actorComponent)
        {
            Debug.Log($"=== 开始加载PMX模型 ===");
            Debug.Log($"文件路径: {filePath}");
            Debug.Log($"文件是否存在: {File.Exists(filePath)}");
            
            if (File.Exists(filePath))
            {
                Debug.Log($"文件大小: {new FileInfo(filePath).Length} bytes");
            }
            
            try 
            {
                // 检查必要的组件
                if (!actorObj.GetComponent<MeshFilter>())
                {
                    actorObj.AddComponent<MeshFilter>();
                    Debug.Log("添加了MeshFilter组件");
                }
                
                if (!actorObj.GetComponent<SkinnedMeshRenderer>())
                {
                    actorObj.AddComponent<SkinnedMeshRenderer>();
                    Debug.Log("添加了SkinnedMeshRenderer组件");
                }
                
                // 使用LibMMD加载PMX模型
                var mmdGameObject = actorObj.AddComponent<LibMMD.Unity3D.MmdGameObject>();
                if (mmdGameObject != null)
                {
                    Debug.Log("MmdGameObject组件已添加，开始加载模型...");
                    
                    // 设置物理模式为无物理，避免物理相关错误
                    mmdGameObject.PhysicsMode = LibMMD.Unity3D.MmdGameObject.PhysicsModeEnum.None;
                    
                    bool loadSuccess = mmdGameObject.LoadModel(filePath);
                    Debug.Log($"LoadModel返回结果: {loadSuccess}");
                    
                    if (loadSuccess)
                    {
                        // actorComponent.isLoaded = true;
                        // actorComponent.isPlaceholder = false;
                        Debug.Log($"PMX模型加载成功: {filePath}");
                        Debug.Log($"模型名称: {mmdGameObject.ModelName}");
                        
                        // 检查mesh是否正确加载
                        var meshFilter = actorObj.GetComponent<MeshFilter>();
                        var skinnedMeshRenderer = actorObj.GetComponent<SkinnedMeshRenderer>();
                        
                        if (meshFilter && meshFilter.mesh != null)
                        {
                            Debug.Log($"MeshFilter中的mesh顶点数: {meshFilter.mesh.vertexCount}");
                        }
                        
                        if (skinnedMeshRenderer && skinnedMeshRenderer.sharedMesh != null)
                        {
                            Debug.Log($"SkinnedMeshRenderer中的mesh顶点数: {skinnedMeshRenderer.sharedMesh.vertexCount}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"PMX模型加载失败: {filePath}");
                        Debug.LogError("LoadModel方法返回false，可能的原因：");
                        Debug.LogError("1. PMX文件格式不支持或损坏");
                        Debug.LogError("2. LibMMD内部异常");
                        Debug.LogError("3. 纹理文件缺失");
                        CreatePlaceholderModel(actorObj, actorComponent);
                    }
                }
                else
                {
                    Debug.LogError("无法添加MmdGameObject组件");
                    CreatePlaceholderModel(actorObj, actorComponent);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"PMX模型加载异常: {e.Message}");
                Debug.LogError($"异常类型: {e.GetType().Name}");
                Debug.LogError($"异常堆栈: {e.StackTrace}");
                
                // 检查内部异常
                if (e.InnerException != null)
                {
                    Debug.LogError($"内部异常: {e.InnerException.Message}");
                    Debug.LogError($"内部异常堆栈: {e.InnerException.StackTrace}");
                }
                
                CreatePlaceholderModel(actorObj, actorComponent);
            }
            
            Debug.Log($"=== PMX模型加载完成 ===");
        }

        private void CreatePlaceholderModel(GameObject actorObj, SceneActorComponent actorComponent)
        {
            // 创建一个占位的GameObject
            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            placeholder.name = "PMX_Placeholder";
            placeholder.transform.SetParent(actorObj.transform);
            placeholder.transform.localPosition = Vector3.zero;
            
            // 设置占位对象的外观
            var renderer = placeholder.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.7f, 0.7f, 1f, 0.8f); // 浅蓝色半透明
                renderer.material = material;
            }
            
            // 添加标识，表明这是占位对象
            // actorComponent.isPlaceholder = true;
        }

        public void RemoveActor(string actorId)
        {
            Transform actorObj = actorContainer.Find($"Actor_{actorId}");
            if (actorObj != null)
            {
                DestroyImmediate(actorObj.gameObject);
                EventManager.Instance?.TriggerEvent("ActorListUpdated");
                Debug.Log($"演员已移除: {actorId}");
            }
        }

        public List<ModelData> GetModelList()
        {
            List<ModelData> modelList = new List<ModelData>();
            
            for (int i = 0; i < modelContainer.childCount; i++)
            {
                Transform child = modelContainer.GetChild(i);
                var modelComponent = child.GetComponent<ModelComponent>();
                if (modelComponent != null)
                {
                    modelList.Add(new ModelData
                    {
                        id = modelComponent.id,
                        displayName = modelComponent.displayName,
                        filePath = modelComponent.filePath
                    });
                }
            }
              return modelList;
        }

        /// <summary>
        /// 移除模型资源
        /// </summary>
        public void RemoveModelResource(string modelId)
        {
            Transform modelObj = modelContainer.Find($"Model_{modelId}");
            if (modelObj != null)
            {
                var modelComponent = modelObj.GetComponent<ModelComponent>();
                if (modelComponent != null)
                {
                    Debug.Log($"移除模型资源: {modelComponent.displayName} (ID: {modelId})");
                    
                    // 断开所有关联
                    DisconnectAllModelAssociations(modelId);
                    
                    // 删除对应的Actor实例
                    for (int i = actorContainer.childCount - 1; i >= 0; i--)
                    {
                        Transform actorChild = actorContainer.GetChild(i);
                        var actorComponent = actorChild.GetComponent<SceneActorComponent>();
                        if (actorComponent != null && actorComponent.modelRef != null && actorComponent.modelRef.id == modelId)
                        {
                            DestroyImmediate(actorChild.gameObject);
                        }
                    }
                    
                    // 删除模型资源
                    DestroyImmediate(modelObj.gameObject);
                    
                    EventManager.Instance?.TriggerEvent("ModelListChanged");
                    EventManager.Instance?.TriggerEvent("ActorListChanged");
                }
            }
        }
        
        public List<ActorData> GetActorList()
        {
            List<ActorData> actorList = new List<ActorData>();
            
            for (int i = 0; i < actorContainer.childCount; i++)
            {
                Transform child = actorContainer.GetChild(i);
                var actorComponent = child.GetComponent<SceneActorComponent>();
                if (actorComponent != null)
                {
                    actorList.Add(new ActorData
                    {
                        id = actorComponent.actorId,
                        displayName = actorComponent.displayName,
                        filePath = actorComponent.modelRef != null ? actorComponent.modelRef.filePath : ""
                    });
                }
            }
            
            return actorList;
        }

        // ===== 动作资源管理 =====
          public string AddMotion(string filePath)
        {
            string id = System.Guid.NewGuid().ToString();
            string displayName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            
            // 创建动作资源GameObject
            GameObject motionObj = new GameObject($"Motion_{id}");
            motionObj.transform.SetParent(motionContainer);
            
            // 添加组件
            var motionComponent = motionObj.AddComponent<MotionComponent>();
            motionComponent.id = id;
            motionComponent.displayName = displayName;
            motionComponent.filePath = filePath;
            
            // 验证VMD文件是否存在
            if (!File.Exists(filePath))
            {
                Debug.LogError($"VMD文件不存在: {filePath}");
            }
            else
            {
                Debug.Log($"VMD文件验证成功: {displayName}");
            }
            
            EventManager.Instance?.TriggerEvent("MotionListUpdated");
            Debug.Log($"动作已添加: {displayName} (ID: {id})");
            
            return id;
        }        public void RemoveMotion(string motionId)
        {
            Transform motionObj = motionContainer.Find($"Motion_{motionId}");
            if (motionObj != null)
            {
                DestroyImmediate(motionObj.gameObject);
                EventManager.Instance?.TriggerEvent("MotionListUpdated");
                Debug.Log($"动作已移除: {motionId}");
            }
            else
            {
                Debug.LogWarning($"未找到要删除的动作: Motion_{motionId}");
            }
        }public void AssignMotionToActor(string motionId, string actorId)
        {
            Transform motionObj = motionContainer.Find($"Motion_{motionId}");
            
            // 修复：通过SceneActorComponent的actorId字段来查找Actor，而不是依赖GameObject名称
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
            
            if (motionObj != null && actorTransform != null)
            {
                var motionComponent = motionObj.GetComponent<MotionComponent>();
                var mmdGameObject = actorTransform.GetComponent<LibMMD.Unity3D.MmdGameObject>();
                
                if (motionComponent != null && mmdGameObject != null && !string.IsNullOrEmpty(motionComponent.filePath))
                {
                    try
                    {
                        if (File.Exists(motionComponent.filePath))
                        {
                            mmdGameObject.LoadMotion(motionComponent.filePath);
                            mmdGameObject.UpdateConfig(new LibMMD.Unity3D.MmdUnityConfig
                            {
                                EnableDrawSelfShadow = LibMMD.Unity3D.MmdConfigSwitch.ForceFalse,
                                EnableCastShadow = LibMMD.Unity3D.MmdConfigSwitch.ForceFalse,
                            });
                            mmdGameObject.PhysicsMode = LibMMD.Unity3D.MmdGameObject.PhysicsModeEnum.Bullet;
                            // 不再在actorComponent上记录currentMotionId
                            if (isPlaying)
                            {
                                mmdGameObject.SetMotionPos(playTime);
                                mmdGameObject.Playing = true;
                            }
                            Debug.Log($"动作 {motionComponent.displayName} 成功分配给演员 {actorId} 并启用物理效果");
                        }
                        else
                        {
                            Debug.LogError($"VMD文件不存在: {motionComponent.filePath}");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"动作分配失败: {e.Message}\n{e.StackTrace}");
                    }
                }
                else
                {
                    Debug.LogError("动作组件或MMD游戏对象为空，或VMD文件路径无效");
                }
            }
            else
            {
                Debug.LogError($"找不到动作 {motionId} 或演员 {actorId}");
            }
        }        public List<MotionData> GetMotionList()
        {
            List<MotionData> motionList = new List<MotionData>();
            
            for (int i = 0; i < motionContainer.childCount; i++)
            {
                Transform child = motionContainer.GetChild(i);
                var motionComponent = child.GetComponent<MotionComponent>();
                if (motionComponent != null)
                {
                    // 查找哪个Actor在使用这个Motion（此处不再支持，assignedActorId留空或后续用映射管理）
                    string assignedActorId = "";
                    motionList.Add(new MotionData
                    {
                        id = motionComponent.id,
                        displayName = motionComponent.displayName,
                        filePath = motionComponent.filePath,
                        assignedActorId = assignedActorId
                    });
                }
            }
              return motionList;
        }

        /// <summary>
        /// 移除动作资源
        /// </summary>
        public void RemoveMotionResource(string motionId)
        {
            Transform motionObj = motionContainer.Find($"Motion_{motionId}");
            if (motionObj != null)
            {
                var motionComponent = motionObj.GetComponent<MotionComponent>();
                if (motionComponent != null)
                {
                    Debug.Log($"移除动作资源: {motionComponent.displayName} (ID: {motionId})");
                    
                    // 断开所有关联
                    DisconnectAllMotionAssociations(motionId);
                    
                    // 删除动作资源
                    DestroyImmediate(motionObj.gameObject);
                    
                    EventManager.Instance?.TriggerEvent("MotionListChanged");
                }
            }
        }

        /// <summary>
        /// 获取动作数据列表（为控制器提供）
        /// </summary>
        public List<MotionData> GetMotionDataList()
        {
            return GetMotionList();
        }

        // ===== 播放控制 =====
          public void Play()
        {
            // 修复：如果currentActiveMusicId无效，自动选择第一个可用音乐
            if (string.IsNullOrEmpty(currentActiveMusicId) || musicContainer.Find($"Music_{currentActiveMusicId}") == null)
            {
                if (musicContainer.childCount > 0)
                {
                    var firstMusic = musicContainer.GetChild(0).GetComponent<MusicComponent>();
                    if (firstMusic != null && !string.IsNullOrEmpty(firstMusic.id))
                    {
                        SetActiveMusic(firstMusic.id);
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(currentActiveMusicId))
            {
                Transform musicObj = musicContainer.Find($"Music_{currentActiveMusicId}");
                if (musicObj != null)
                {
                    var audioSource = musicObj.GetComponent<AudioSource>();
                    if (audioSource != null && audioSource.clip != null)
                    {
                        audioSource.time = playTime;
                        audioSource.Play();
                        isPlaying = true;
                        Debug.Log("播放开始");
                    }
                }
            }
            else
            {
                // 没有音乐也可以播放（仅播放动画和摄像机）
                isPlaying = true;
                Debug.Log("无音乐播放开始");
            }
            
            // 同步所有演员动作的播放状态
            // Original: SyncAllActorMotions();
            UpdateAllActorMotionStates(true); // Call the new method
            
            EventManager.Instance?.TriggerEvent("PlaybackStateChanged");
        }        public void Pause()
        {
            if (!string.IsNullOrEmpty(currentActiveMusicId))
            {
                Transform musicObj = musicContainer.Find($"Music_{currentActiveMusicId}");
                if (musicObj != null)
                {
                    var audioSource = musicObj.GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        audioSource.Pause();
                    }
                }
            }
            
            isPlaying = false;
            Debug.Log("播放暂停");
            
            // 暂停所有演员动作
            PauseAllActorMotions();
            
            EventManager.Instance?.TriggerEvent("PlaybackStateChanged");
        }

        /// <summary>
        /// 暂停所有演员的动作播放
        /// </summary>
        private void PauseAllActorMotions()
        {
            for (int i = 0; i < actorContainer.childCount; i++)
            {
                Transform actorObj = actorContainer.GetChild(i);
                var mmdGameObject = actorObj.GetComponent<LibMMD.Unity3D.MmdGameObject>();
                if (mmdGameObject != null)
                {
                    mmdGameObject.Playing = false;
                }
            }
        }        public void SeekTo(float time)
        {
            playTime = Mathf.Clamp(time, 0f, totalDuration);
            
            if (!string.IsNullOrEmpty(currentActiveMusicId))
            {
                Transform musicObj = musicContainer.Find($"Music_{currentActiveMusicId}");
                if (musicObj != null)
                {
                    var audioSource = musicObj.GetComponent<AudioSource>();
                    if (audioSource != null && audioSource.clip != null)
                    {
                        audioSource.time = playTime;
                    }
                }
            }
            
            // 同步所有演员动作时间
            // Original: SyncAllActorMotions();
            UpdateAllActorMotionStates(isPlaying); // Call the new method, pass current playing state
            
            Debug.Log($"跳转到时间: {playTime:F2}s");
            EventManager.Instance?.TriggerEvent("PlaybackTimeChanged");
        }        // Added method to replace SyncAllActorMotions
        private void UpdateAllActorMotionStates(bool play)
        {
            for (int i = 0; i < actorContainer.childCount; i++)
            {
                Transform actorObj = actorContainer.GetChild(i);
                var mmdGameObject = actorObj.GetComponent<LibMMD.Unity3D.MmdGameObject>();
                if (mmdGameObject != null)
                {
                    mmdGameObject.Playing = play;
                    if (play) // If playing, also sync time
                    {
                         mmdGameObject.SetMotionPos(playTime); // SetMotionPos只接受一个参数
                    }
                }
            }
        }

        // ===== 工具方法 =====
        
        public float GetMusicDuration()
        {
            if (!string.IsNullOrEmpty(currentActiveMusicId))
            {
                Transform musicObj = musicContainer.Find($"Music_{currentActiveMusicId}");
                if (musicObj != null)
                {
                    AudioSource audioSource = musicObj.GetComponent<AudioSource>();
                    if (audioSource != null && audioSource.clip != null)
                    {
                        return audioSource.clip.length;
                    }
                }
            }
            return 0f;
        }

        public void SetMusicVolume(float volume)
        {
            if (!string.IsNullOrEmpty(currentActiveMusicId))
            {
                Transform musicObj = musicContainer.Find($"Music_{currentActiveMusicId}");
                if (musicObj != null)
                {
                    AudioSource audioSource = musicObj.GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        audioSource.volume = volume;
                    }
                }
            }
        }        // Free Camera控制
        public void UpdateFreeCameraTransform(Vector3 position, Quaternion rotation, float fov)
        {
            Transform freeCamerasContainer = cameraContainer.Find("FreeCameras");
            Transform freeCamObj = freeCamerasContainer.Find("FreeCamera_Resource");
            if (freeCamObj != null)
            {
                var freeCamComponent = freeCamObj.GetComponent<FreeCameraComponent>();
                if (freeCamComponent != null)
                {
                    freeCamComponent.position = position;
                    freeCamComponent.rotation = rotation;
                    freeCamComponent.fieldOfView = fov;
                    
                    // 如果当前正在使用Free Camera，立即应用
                    if (currentActiveCameraId == "BUILTIN_FREE_CAMERA")
                    {
                        ApplyFreeCameraState();
                    }
                }
            }
        }
          // ===== 数据获取API（供UI使用）=====
        
        public List<MusicData> GetMusicDataList()
        {
            return GetMusicList(); // 直接调用现有方法
        }
        
        public List<CameraData> GetCameraDataList()
        {
            return GetCameraList(); // 直接调用现有方法
        }

        // ===== 缺失的API方法 =====
        
        public void SetActiveMusic(string musicId)
        {
            ActivateMusic(musicId);
        }

        public void SetActiveCamera(string cameraId)
        {
            ActivateCamera(cameraId);
        }

        public void RemoveMusicResource(string musicId)
        {
            RemoveMusic(musicId);
        }

        public void RemoveCameraResource(string cameraId)
        {
            RemoveCamera(cameraId);
        }

        public CameraData GetActiveCameraData()
        {
            if (string.IsNullOrEmpty(currentActiveCameraId))
                return null;

            var cameraList = GetCameraList();
            return cameraList.Find(c => c.id == currentActiveCameraId);
        }
        
        // ===== 模型-动作关联管理 =====
        
        /// <summary>
        /// 关联模型和动作
        /// </summary>
        public void AssociateModelWithMotion(string modelId, string motionId)
        {
            if (string.IsNullOrEmpty(modelId) || string.IsNullOrEmpty(motionId))
                return;
                
            if (!modelMotionAssociations.ContainsKey(modelId))
            {
                modelMotionAssociations[modelId] = new List<string>();
            }
            
            if (!modelMotionAssociations[modelId].Contains(motionId))
            {
                modelMotionAssociations[modelId].Add(motionId);
                Debug.Log($"关联模型 {modelId} 与动作 {motionId}");
                EventManager.Instance?.TriggerEvent("ModelMotionAssociationChanged");
            }
        }
        
        /// <summary>
        /// 取消模型和动作的关联
        /// </summary>
        public void DisassociateModelFromMotion(string modelId, string motionId)
        {
            if (modelMotionAssociations.ContainsKey(modelId))
            {
                if (modelMotionAssociations[modelId].Remove(motionId))
                {
                    Debug.Log($"取消关联模型 {modelId} 与动作 {motionId}");
                    EventManager.Instance?.TriggerEvent("ModelMotionAssociationChanged");
                }
                
                if (modelMotionAssociations[modelId].Count == 0)
                {
                    modelMotionAssociations.Remove(modelId);
                }
            }
        }
        
        /// <summary>
        /// 断开模型的所有动作关联
        /// </summary>
        public void DisconnectAllModelAssociations(string modelId)
        {
            if (modelMotionAssociations.ContainsKey(modelId))
            {
                modelMotionAssociations.Remove(modelId);
                Debug.Log($"断开模型 {modelId} 的所有关联");
                // 为模型加载空动作
                LoadEmptyMotionForModel(modelId);
                EventManager.Instance?.TriggerEvent("ModelMotionAssociationChanged");
            }
        }
        
        /// <summary>
        /// 为模型加载空动作
        /// </summary>
        public void LoadEmptyMotionForModel(string modelId)
        {
            // 查找模型对应的Actor GameObject
            Transform actorTransform = actorContainer?.Find(modelId);
            if (actorTransform != null)
            {
                // 查找MmdGameObject组件
                var mmdGameObject = actorTransform.GetComponentInChildren<LibMMD.Unity3D.MmdGameObject>();
                if (mmdGameObject != null)
                {
                    // 重置到默认姿态（无动作）
                    mmdGameObject.ResetMotion();
                    Debug.Log($"已为模型 {modelId} 加载空动作");
                }
                else
                {
                    Debug.LogWarning($"模型 {modelId} 没有找到MmdGameObject组件");
                }
            }
            else
            {
                Debug.LogWarning($"模型 {modelId} 的Actor对象未找到");
            }
        }
        
        /// <summary>
        /// 重新加载模型的动作列表（用于动作断开连接后的刷新）
        /// </summary>
        public void ReloadModelMotions(string modelId)
        {
            // 查找模型对应的Actor GameObject
            Transform actorTransform = actorContainer?.Find(modelId);
            if (actorTransform != null)
            {
                var mmdGameObject = actorTransform.GetComponentInChildren<LibMMD.Unity3D.MmdGameObject>();
                if (mmdGameObject != null)
                {
                    // 获取模型当前关联的动作
                    var associatedMotions = GetModelAssociatedMotions(modelId);
                    // 清除当前动作
                    mmdGameObject.ResetMotion();
                    if (associatedMotions.Count > 0)
                    {
                        var motionData = GetMotionResourceById(associatedMotions[0]);
                        if (motionData != null)
                        {
                            mmdGameObject.LoadMotion(motionData.FilePath);
                            Debug.Log($"已为模型 {modelId} 重新加载动作: {motionData.DisplayName}");
                        }
                    }
                    else
                    {
                        // 如果没有关联动作，加载空动作
                        LoadEmptyMotionForModel(modelId);
                    }
                }
            }
        }
        
        /// <summary>
        /// 断开动作的所有模型关联
        /// </summary>
        public void DisconnectAllMotionAssociations(string motionId)
        {
            var keysToUpdate = new List<string>();
            foreach (var kvp in modelMotionAssociations)
            {
                if (kvp.Value.Contains(motionId))
                {
                    keysToUpdate.Add(kvp.Key);
                }
            }
            foreach (var key in keysToUpdate)
            {
                modelMotionAssociations[key].Remove(motionId);
                if (modelMotionAssociations[key].Count == 0)
                {
                    modelMotionAssociations.Remove(key);
                }
                // 为每个受影响的模型重新加载动作列表
                ReloadModelMotions(key);
            }
            if (keysToUpdate.Count > 0)
            {
                Debug.Log($"断开动作 {motionId} 的所有关联，并重新加载了 {keysToUpdate.Count} 个模型的动作");
                EventManager.Instance?.TriggerEvent("ModelMotionAssociationChanged");
            }
        }

        /// <summary>
        /// 获取模型关联的动作列表
        /// </summary>
        public List<string> GetModelAssociatedMotions(string modelId)
        {
            if (modelMotionAssociations.ContainsKey(modelId))
            {
                return new List<string>(modelMotionAssociations[modelId]);
            }
            return new List<string>();
        }
        
        /// <summary>
        /// 获取模型关联的动作列表
        /// </summary>
        public List<string> GetAssociatedMotions(string modelId)
        {
            if (modelMotionAssociations.ContainsKey(modelId))
            {
                return new List<string>(modelMotionAssociations[modelId]);
            }
            return new List<string>();
        }
        
        /// <summary>
        /// 获取动作关联的模型列表
        /// </summary>
        public List<string> GetAssociatedModels(string motionId)
        {
            var result = new List<string>();
            foreach (var kvp in modelMotionAssociations)
            {
                if (kvp.Value.Contains(motionId))
                {
                    result.Add(kvp.Key);
                }
            }
            return result;
        }
        
        // ===== 模型状态管理 =====        
        /// <summary>
        /// 切换模型启用/禁用状态
        /// </summary>
        public void ToggleModel(string modelId)
        {
            if (IsModelDisabled(modelId))
            {
                // 当前是禁用状态，启用它
                disabledModelIds.Remove(modelId);
                UpdateModelVisibility(modelId, true);
                Debug.Log($"启用模型 {modelId}");
            }
            else
            {
                // 当前是启用状态，禁用它
                disabledModelIds.Add(modelId);
                UpdateModelVisibility(modelId, false);
                Debug.Log($"禁用模型 {modelId}");
            }
            EventManager.Instance?.TriggerEvent("ModelStateChanged");
        }

        /// <summary>
        /// 禁用模型
        /// </summary>
        public void DisableModel(string modelId)
        {
            disabledModelIds.Add(modelId);
            UpdateModelVisibility(modelId, false);
            Debug.Log($"禁用模型 {modelId}");
            EventManager.Instance?.TriggerEvent("ModelStateChanged");
        }
          /// <summary>
        /// 启用模型
        /// </summary>
        public void EnableModel(string modelId)
        {
            disabledModelIds.Remove(modelId); // 不管是否存在都尝试移除
            UpdateModelVisibility(modelId, true);
            Debug.Log($"启用模型 {modelId}");
            EventManager.Instance?.TriggerEvent("ModelStateChanged");
        }
        
        /// <summary>
        /// 检查模型是否被禁用
        /// </summary>
        public bool IsModelDisabled(string modelId)
        {
            return disabledModelIds.Contains(modelId);
        }
          /// <summary>
        /// 更新模型可见性
        /// </summary>
        private void UpdateModelVisibility(string modelId, bool visible)
        {
            Debug.Log($"[UpdateModelVisibility] Try set modelId={modelId} visible={visible}, actorContainer.childCount={actorContainer.childCount}");
            bool found = false;
            
            for (int i = 0; i < actorContainer.childCount; i++)
            {
                Transform actorChild = actorContainer.GetChild(i);
                var actorComponent = actorChild.GetComponent<SceneActorComponent>();
                if (actorComponent == null)
                {
                    Debug.Log($"[UpdateModelVisibility] actorChild {actorChild.name} has no SceneActorComponent");
                    continue;
                }
                if (actorComponent.modelRef == null)
                {
                    Debug.Log($"[UpdateModelVisibility] actorChild {actorChild.name} SceneActorComponent.modelRef is null");
                    continue;
                }
                Debug.Log($"[UpdateModelVisibility] actorChild {actorChild.name} modelRef.id={actorComponent.modelRef.id} (type={actorComponent.modelRef.id?.GetType()})");
                if (actorComponent.modelRef.id == modelId)
                {
                    found = true;
                    Debug.Log($"[UpdateModelVisibility] MATCHED: Before SetActive({visible}) for {actorChild.name}, current active state: {actorChild.gameObject.activeSelf}");
                    
                    // 确保父容器是激活的
                    if (visible && !actorContainer.gameObject.activeSelf)
                    {
                        Debug.Log($"[UpdateModelVisibility] Parent actorContainer is inactive, activating it first");
                        actorContainer.gameObject.SetActive(true);
                    }
                    
                    actorChild.gameObject.SetActive(visible);
                    Debug.Log($"[UpdateModelVisibility] After SetActive({visible}) for {actorChild.name}, current active state: {actorChild.gameObject.activeSelf}");
                    break;
                }
            }
            
            if (!found)
            {
                Debug.LogWarning($"[UpdateModelVisibility] No actor found with modelId={modelId}");
            }
        }
        
        // ===== 测试辅助方法 =====
        
        /// <summary>
        /// 为测试创建演员（不需要文件路径）
        /// </summary>
        public void AddActorForTesting(string actorId, string displayName)
        {
            string filePath = $"TestData/{displayName}.pmx";
            string id = displayName;
            // 先查找modelContainer下是否已有对应ModelComponent（通过filePath）
            ModelComponent modelComponent = null;
            for (int i = 0; i < modelContainer.childCount; i++)
            {
                var child = modelContainer.GetChild(i);
                var mc = child.GetComponent<ModelComponent>();
                if (mc != null && mc.filePath == filePath)
                {
                    modelComponent = mc;
                    break;
                }
            }
            // 没有则新建
            if (modelComponent == null)
            {
                GameObject modelObj = new GameObject($"Model_{id}_Test");
                modelObj.transform.SetParent(modelContainer);
                modelComponent = modelObj.AddComponent<ModelComponent>();
                modelComponent.id = id;
                modelComponent.displayName = displayName;
                modelComponent.filePath = filePath;
            }

            // 创建演员资源GameObject
            GameObject actorObj = new GameObject($"Actor_{id}");
            actorObj.transform.SetParent(actorContainer);
            
            // 添加组件
            var actorComponent = actorObj.AddComponent<SceneActorComponent>();
            actorComponent.modelRef = modelComponent;
            actorComponent.actorId = modelComponent.id;
            actorComponent.displayName = modelComponent.displayName;
            
            // 创建可视化占位符
            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            placeholder.name = $"{displayName}_Placeholder";
            placeholder.transform.SetParent(actorObj.transform);
            placeholder.transform.localPosition = Vector3.zero;
            placeholder.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            
            // 设置材质颜色以区分不同演员
            var renderer = placeholder.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.8f, 1f);
                renderer.material = material;
            }
            
            Debug.Log($"测试演员已添加: {displayName} (ID: {id})");
            EventManager.Instance?.TriggerEvent("ActorListUpdated");
        }
        
        /// <summary>
        /// 为测试创建动作（不需要文件路径）
        /// </summary>
        public void AddMotionForTesting(string motionId, string displayName)
        {
            // 创建动作资源GameObject
            GameObject motionObj = new GameObject($"Motion_{motionId}");
            motionObj.transform.SetParent(motionContainer);
            
            // 添加组件
            var motionComponent = motionObj.AddComponent<MotionComponent>();
            motionComponent.id = motionId;
            motionComponent.displayName = displayName;
            motionComponent.filePath = $"TestData/{displayName}.vmd"; // 虚拟路径
            
            // 创建可视化指示器
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.name = $"{displayName}_Indicator";
            indicator.transform.SetParent(motionObj.transform);
            indicator.transform.localPosition = Vector3.zero;
            indicator.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            
            // 设置材质颜色
            var renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.8f, 0.3f, 0.8f, 1f); // 紫色调
                renderer.material = material;
            }
            
            Debug.Log($"测试动作已添加: {displayName} (ID: {motionId})");
            EventManager.Instance?.TriggerEvent("MotionListUpdated");
        }
        
        /// <summary>
        /// 添加模型资源（仅资源，不实例化到场景）
        /// </summary>
        public string AddModel(string filePath)
        {
            string id = System.Guid.NewGuid().ToString();
            string displayName = System.IO.Path.GetFileNameWithoutExtension(filePath);

            // 创建模型资源GameObject
            GameObject modelObj = new GameObject($"Model_{id}");
            modelObj.transform.SetParent(modelContainer);

            // 添加组件
            var modelComponent = modelObj.AddComponent<ModelComponent>();
            modelComponent.id = id;
            modelComponent.displayName = displayName;
            modelComponent.filePath = filePath;

            EventManager.OnActorListChanged?.Invoke();
            return id;
        }

        /// <summary>
        /// 添加动作资源（仅资源，不实例化到场景）
        /// </summary>
        // public string AddMotion(string filePath)
        // {
        //     var motionData = new MotionData
        //     {
        //         id = System.Guid.NewGuid().ToString(),
        //         displayName = Path.GetFileNameWithoutExtension(filePath),
        //         filePath = filePath
        //     };
        //     if (motionDataList == null) motionDataList = new List<MotionData>();
        //     motionDataList.Add(motionData);
        //     EventManager.OnMotionListChanged?.Invoke();
        //     return motionData.id;
        // }
        
        // ===== 通过ID获取实际GameObject和组件的核心API =====
        
        /// <summary>
        /// 通过ID获取模型GameObject
        /// </summary>
        public GameObject GetModelObjectById(string id)
        {
            Transform modelObj = modelContainer.Find($"Model_{id}");
            return modelObj?.gameObject;
        }
        
        /// <summary>
        /// 通过ID获取模型组件
        /// </summary>
        public ModelComponent GetModelComponentById(string id)
        {
            GameObject modelObj = GetModelObjectById(id);
            return modelObj?.GetComponent<ModelComponent>();
        }
        
        /// <summary>
        /// 通过ID获取音乐GameObject
        /// </summary>
        public GameObject GetMusicObjectById(string id)
        {
            Transform musicObj = musicContainer.Find($"Music_{id}");
            return musicObj?.gameObject;
        }
        
        /// <summary>
        /// 通过ID获取音乐组件
        /// </summary>
        public MusicComponent GetMusicComponentById(string id)
        {
            GameObject musicObj = GetMusicObjectById(id);
            return musicObj?.GetComponent<MusicComponent>();
        }
        
        /// <summary>
        /// 通过ID获取动作GameObject
        /// </summary>
        public GameObject GetMotionObjectById(string id)
        {
            Transform motionObj = motionContainer.Find($"Motion_{id}");
            return motionObj?.gameObject;
        }
        
        /// <summary>
        /// 通过ID获取动作组件
        /// </summary>
        public MotionComponent GetMotionComponentById(string id)
        {
            GameObject motionObj = GetMotionObjectById(id);
            return motionObj?.GetComponent<MotionComponent>();
        }
        
        /// <summary>
        /// 通过ID获取摄像机GameObject（包括VMD摄像机和Free摄像机）
        /// </summary>
        public GameObject GetCameraObjectById(string id)
        {
            if (id == "BUILTIN_FREE_CAMERA")
            {
                Transform freeCamerasContainer = cameraContainer.Find("FreeCameras");
                Transform freeCamObj = freeCamerasContainer?.Find("FreeCamera_Resource");
                return freeCamObj?.gameObject;
            }
            else
            {
                Transform mmdCamerasContainer = cameraContainer.Find("MMDCameras");
                Transform cameraObj = mmdCamerasContainer?.Find($"VMDCamera_{id}");
                return cameraObj?.gameObject;
            }
        }
        
        /// <summary>
        /// 通过ID获取演员GameObject
        /// </summary>
        public GameObject GetActorObjectById(string id)
        {
            Transform actorObj = actorContainer.Find($"Actor_{id}");
            return actorObj?.gameObject;
        }
        
        /// <summary>
        /// 通过ID获取演员组件
        /// </summary>
        public SceneActorComponent GetActorComponentById(string id)
        {
            GameObject actorObj = GetActorObjectById(id);
            return actorObj?.GetComponent<SceneActorComponent>();
        }        /// <summary>
        /// 通过ID获取Motion资源
        /// </summary>
        private MotionData GetMotionResourceById(string motionId)
        {
            if (motionContainer == null) return null;
            // 兼容命名方式：Motion_{id}
            Transform motionTransform = motionContainer.Find($"Motion_{motionId}");
            if (motionTransform != null)
            {
                var motionComponent = motionTransform.GetComponent<MotionComponent>();
                if (motionComponent != null)
                {
                    return new MotionData
                    {
                        id = motionComponent.id,
                        displayName = motionComponent.displayName,
                        filePath = motionComponent.filePath
                    };
                }
            }
            // 兼容直接用id
            motionTransform = motionContainer.Find(motionId);
            if (motionTransform != null)
            {
                var motionComponent = motionTransform.GetComponent<MotionComponent>();
                if (motionComponent != null)
                {
                    return new MotionData
                    {
                        id = motionComponent.id,
                        displayName = motionComponent.displayName,
                        filePath = motionComponent.filePath
                    };
                }
            }
            return null;
        }
    }

    // ===== 资源组件定义 =====
    
    /// <summary>
    /// 音乐组件 - 存储音乐资源的元数据
    /// </summary>
    public class MusicComponent : MonoBehaviour
    {
        public string id;
        public string displayName;
        public string filePath;
        public AudioSource audioSource;
    }    /// <summary>
    /// 模型组件 - 存储模型资源的元数据
    /// </summary>
    public class ModelComponent : MonoBehaviour
    {
        [Header("资源信息")]
        public string id;
        public string displayName;
        public string filePath;
        
        [Header("元数据")]
        public System.DateTime loadTime; // 加载时间，用于去重判断
        
        private void Awake()
        {
            // 确保ID唯一性
            if (string.IsNullOrEmpty(id))
            {
                GenerateUniqueId();
            }
            loadTime = System.DateTime.Now;
        }
        
        /// <summary>
        /// 生成唯一ID，避免重复加载时的冲突
        /// </summary>
        public void GenerateUniqueId()
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                // 基于文件路径和时间戳生成唯一ID
                string fileHash = filePath.GetHashCode().ToString("X");
                string timeStamp = System.DateTime.Now.Ticks.ToString();
                id = $"Model_{fileHash}_{timeStamp}";
            }
            else
            {
                // 如果没有文件路径，使用GUID
                id = $"Model_{System.Guid.NewGuid():N}";
            }
        }
        
        /// <summary>
        /// 检查是否与其他Model是同一个文件
        /// </summary>
        public bool IsSameFile(string otherFilePath)
        {
            return !string.IsNullOrEmpty(filePath) && 
                   !string.IsNullOrEmpty(otherFilePath) && 
                   System.IO.Path.GetFullPath(filePath).Equals(System.IO.Path.GetFullPath(otherFilePath));
        }
    }

    /// <summary>
    /// 场景演员组件 - 存储场景中演员的元数据和运行时状态
    /// </summary>
    public class SceneActorComponent : MonoBehaviour
    {
        [Header("唯一关联的Model")]
        public ModelComponent modelRef;
        public string actorId; // == modelRef.id
        public string displayName; // == modelRef.displayName
    }
    
    /// <summary>
    /// Motion实例数据 - 记录Motion在特定Actor上的运行时状态
    /// </summary>
    [System.Serializable]
    public class MotionInstanceData
    {
        public string motionId;
        public bool isActive = true;
        public System.DateTime assignTime;
        public float playTime = 0f;
        public bool isLooping = false;
        public float playSpeed = 1f;
    }    /// <summary>
    /// 动作组件 - 存储动作资源的元数据
    /// </summary>
    public class MotionComponent : MonoBehaviour
    {
        [Header("资源信息")]
        public string id;
        public string displayName;
        public string filePath;
        
        [Header("元数据")]
        public float duration = 0f; // 动作时长
        public bool isLooping = false;
        public System.DateTime loadTime; // 加载时间，用于去重判断
        
        private void Awake()
        {
            // 确保ID唯一性
            if (string.IsNullOrEmpty(id))
            {
                GenerateUniqueId();
            }
            loadTime = System.DateTime.Now;
        }
        
        /// <summary>
        /// 生成唯一ID，避免重复加载时的冲突
        /// </summary>
        public void GenerateUniqueId()
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                // 基于文件路径和时间戳生成唯一ID
                string fileHash = filePath.GetHashCode().ToString("X");
                string timeStamp = System.DateTime.Now.Ticks.ToString();
                id = $"Motion_{fileHash}_{timeStamp}";
            }
            else
            {
                // 如果没有文件路径，使用GUID
                id = $"Motion_{System.Guid.NewGuid():N}";
            }
        }
        
        /// <summary>
        /// 检查是否与其他Motion是同一个文件
        /// </summary>
        public bool IsSameFile(MotionComponent other)
        {
            return !string.IsNullOrEmpty(filePath) && 
                   !string.IsNullOrEmpty(other.filePath) && 
                   filePath.Equals(other.filePath, System.StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Free摄像机组件 - 存储Free Camera的状态
    /// </summary>
    public class FreeCameraComponent : MonoBehaviour
    {
        public string id;
        public string displayName;
        public Vector3 position = Vector3.zero;
        public Quaternion rotation = Quaternion.identity;
        public float fieldOfView = 60f;
    }

    // ===== UI兼容性数据类 =====
    
    public class MusicData : IResourceInfo
    {
        public string id;
        public string title;
        public string filePath;

        public string ID => id;
        public string DisplayName => title;
        public string FilePath => filePath;
        public ResourceType Type => ResourceType.Music;    }

    public class ActorData : IResourceInfo
    {
        public string id;
        public string displayName;
        public string filePath;

        public string ID => id;        public string DisplayName => displayName;
        public string FilePath => filePath;
        public ResourceType Type => ResourceType.Model;
    }

    public class ModelData : IResourceInfo
    {
        public string id;
        public string displayName;
        public string filePath;

        public string ID => id;
        public string DisplayName => displayName;
        public string FilePath => filePath;
        public ResourceType Type => ResourceType.Model;
    }

    public class MotionData : IResourceInfo
    {
        public string id;
        public string displayName;
        public string filePath;
        public string assignedActorId;

        public string ID => id;
        public string DisplayName => displayName;        public string FilePath => filePath;
        public ResourceType Type => ResourceType.Motion;
    }
}
