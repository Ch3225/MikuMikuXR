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
        [Tooltip("音乐资源容器")] public Transform musicContainer;
        [Tooltip("演员资源容器")] public Transform actorContainer;
        [Tooltip("动作资源容器")] public Transform motionContainer;
        [Tooltip("摄像机资源容器")] public Transform cameraContainer;

        [Header("当前激活状态")]
        public string currentActiveMusicId;
        public string currentActiveCameraId;

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
            if (actorContainer == null)
                actorContainer = CreateResourceContainer("Actors");
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
        }

        public void RemoveMusic(string musicId)
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
        }

        public void RemoveCamera(string cameraId)
        {
            // 不能删除内置Free Camera
            if (cameraId == "BUILTIN_FREE_CAMERA") return;
            
            Transform cameraObj = cameraContainer.Find($"VMDCamera_{cameraId}");
            if (cameraObj != null)
            {
                // 如果正在使用这个摄像机，切换到Free Camera
                if (currentActiveCameraId == cameraId)
                {
                    ActivateCamera("BUILTIN_FREE_CAMERA");
                }
                
                DestroyImmediate(cameraObj.gameObject);
                EventManager.Instance?.TriggerEvent("CameraListUpdated");
                Debug.Log($"摄像机已移除: {cameraId}");
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
            string id = System.Guid.NewGuid().ToString();
            string displayName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            
            // 创建演员资源GameObject - 使用更友好的名称
            GameObject actorObj = new GameObject($"{displayName}_MmdGameObject");
            actorObj.transform.SetParent(actorContainer);
            
            // 添加组件
            var actorComponent = actorObj.AddComponent<ActorComponent>();
            actorComponent.id = id;
            actorComponent.displayName = displayName;
            actorComponent.filePath = filePath;
            
            // 加载PMX模型
            try
            {
                LoadPMXModel(filePath, actorObj, actorComponent);
                Debug.Log($"演员已添加: {displayName} (ID: {id})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"PMX模型加载失败: {e.Message}");
                DestroyImmediate(actorObj);
                return;
            }
              EventManager.Instance?.TriggerEvent("ActorListUpdated");
        }        private void LoadPMXModel(string filePath, GameObject actorObj, ActorComponent actorComponent)
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
                        actorComponent.isLoaded = true;
                        actorComponent.isPlaceholder = false;
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

        private void CreatePlaceholderModel(GameObject actorObj, ActorComponent actorComponent)
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
            actorComponent.isPlaceholder = true;        }

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

        public List<ActorData> GetActorList()
        {
            List<ActorData> actorList = new List<ActorData>();
            
            for (int i = 0; i < actorContainer.childCount; i++)
            {
                Transform child = actorContainer.GetChild(i);
                var actorComponent = child.GetComponent<ActorComponent>();
                if (actorComponent != null)
                {
                    actorList.Add(new ActorData
                    {
                        id = actorComponent.id,
                        displayName = actorComponent.displayName,
                        filePath = actorComponent.filePath
                    });
                }
            }
            
            return actorList;        }

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
        }

        public void RemoveMotion(string motionId)
        {
            Transform motionObj = motionContainer.Find($"Motion_{motionId}");
            if (motionObj != null)
            {
                DestroyImmediate(motionObj.gameObject);
                EventManager.Instance?.TriggerEvent("MotionListUpdated");
                Debug.Log($"动作已移除: {motionId}");
            }
        }        public void AssignMotionToActor(string motionId, string actorId)
        {
            Transform motionObj = motionContainer.Find($"Motion_{motionId}");
            
            // 修复：通过ActorComponent的id字段来查找Actor，而不是依赖GameObject名称
            Transform actorTransform = null; // Renamed from actorObj to avoid conflict
            ActorComponent currentActorComponent = null; // To store the found actor's component
            for (int i = 0; i < actorContainer.childCount; i++)
            {
                Transform child = actorContainer.GetChild(i);
                var ac = child.GetComponent<ActorComponent>(); // Renamed to ac
                if (ac != null && ac.id == actorId)
                {
                    actorTransform = child;
                    currentActorComponent = ac; // Store the component
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
                    {                        // 检查VMD文件是否存在
                        if (File.Exists(motionComponent.filePath))
                        {
                            // 使用文件路径加载动作到MMD模型
                            mmdGameObject.LoadMotion(motionComponent.filePath);
                              // 设置渲染配置，启用物理效果
                            mmdGameObject.UpdateConfig(new LibMMD.Unity3D.MmdUnityConfig
                            {
                                EnableDrawSelfShadow = LibMMD.Unity3D.MmdConfigSwitch.ForceFalse,
                                EnableCastShadow = LibMMD.Unity3D.MmdConfigSwitch.ForceFalse,
                                // 物理模式通过PhysicsMode属性直接设置，不在Config中
                            });
                            
                            // 设置物理模式
                            mmdGameObject.PhysicsMode = LibMMD.Unity3D.MmdGameObject.PhysicsModeEnum.Bullet;
                            
                            // 在Actor中记录当前动作ID，而不是在Motion中记录Actor
                            // Original: actorComponent.currentMotionId = motionId;
                            if (currentActorComponent != null) // Use the stored component
                            {
                                currentActorComponent.currentMotionId = motionId;
                            }
                              // 设置时间同步 - 确保动作从当前播放时间开始
                            if (isPlaying)
                            {
                                mmdGameObject.SetMotionPos(playTime); // SetMotionPos只接受一个参数
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
                    // 查找哪个Actor在使用这个Motion
                    string assignedActorId = "";
                    for (int j = 0; j < actorContainer.childCount; j++)
                    {
                        Transform actorChild = actorContainer.GetChild(j);
                        var actorComponent = actorChild.GetComponent<ActorComponent>();
                        if (actorComponent != null && actorComponent.currentMotionId == motionComponent.id)
                        {
                            assignedActorId = actorComponent.id;
                            break;
                        }
                    }
                    
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

        // ===== 播放控制 =====
          public void Play()
        {
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
        
        // ===== 测试辅助方法 =====
        
        /// <summary>
        /// 为测试创建演员（不需要文件路径）
        /// </summary>
        public void AddActorForTesting(string actorId, string displayName)
        {
            // 创建演员资源GameObject
            GameObject actorObj = new GameObject($"Actor_{actorId}");
            actorObj.transform.SetParent(actorContainer);
            
            // 添加组件
            var actorComponent = actorObj.AddComponent<ActorComponent>();
            actorComponent.id = actorId;
            actorComponent.displayName = displayName;
            actorComponent.filePath = $"TestData/{displayName}.pmx"; // 虚拟路径
            actorComponent.isPlaceholder = true; // 标记为测试占位符
            
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
            
            Debug.Log($"测试演员已添加: {displayName} (ID: {actorId})");
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
    /// 演员组件 - 存储演员资源的元数据和运行时状态
    /// </summary>
    public class ActorComponent : MonoBehaviour
    {
        [Header("资源信息")]
        public string id;
        public string displayName;
        public string filePath;
        public bool isLoaded = false;
        public bool isPlaceholder = false;
        
        [Header("运行时状态")]
        // 重构：支持多个Motion的映射关系
        [SerializeField] private List<string> assignedMotionIds = new List<string>();
        [SerializeField] private Dictionary<string, MotionInstanceData> motionInstances = new Dictionary<string, MotionInstanceData>();
        
        // 当前主要播放的Motion（用于UI显示和主要控制）
        public string primaryMotionId = "";
        
        // mmdComponent 可以通过GetComponent获取
        #if LIBMMD_AVAILABLE
        private LibMMD.Unity3D.MmdGameObject mmdGameObject;
        
        public LibMMD.Unity3D.MmdGameObject GetMmdGameObject()
        {
            if (mmdGameObject == null)
                mmdGameObject = GetComponent<LibMMD.Unity3D.MmdGameObject>();
            return mmdGameObject;
        }
        #endif
        
        // 管理Motion映射关系的方法
        public void AssignMotion(string motionId, bool setAsPrimary = true)
        {
            if (!assignedMotionIds.Contains(motionId))
            {
                assignedMotionIds.Add(motionId);
                motionInstances[motionId] = new MotionInstanceData
                {
                    motionId = motionId,
                    isActive = true,
                    assignTime = System.DateTime.Now
                };
            }
            
            if (setAsPrimary)
            {
                primaryMotionId = motionId;
            }
        }
        
        public void UnassignMotion(string motionId)
        {
            if (assignedMotionIds.Contains(motionId))
            {
                assignedMotionIds.Remove(motionId);
                motionInstances.Remove(motionId);
                
                // 如果移除的是主要Motion，重新设置
                if (primaryMotionId == motionId)
                {
                    primaryMotionId = assignedMotionIds.Count > 0 ? assignedMotionIds[0] : "";
                }
            }
        }
        
        public List<string> GetAssignedMotionIds()
        {
            return new List<string>(assignedMotionIds);
        }
        
        public bool HasMotionAssigned(string motionId)
        {
            return assignedMotionIds.Contains(motionId);
        }
        
        // 为了兼容现有代码，保留currentMotionId属性
        [System.Obsolete("Use primaryMotionId instead")]
        public string currentMotionId
        {
            get { return primaryMotionId; }
            set { 
                if (!string.IsNullOrEmpty(value))
                {
                    AssignMotion(value, true);
                }
                else
                {
                    primaryMotionId = "";
                }
            }
        }
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

        public string ID => id;
        public string DisplayName => displayName;
        public string FilePath => filePath;
        public ResourceType Type => ResourceType.Actor;
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
