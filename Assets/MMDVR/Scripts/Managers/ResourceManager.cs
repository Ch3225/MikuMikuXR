using UnityEngine;
using System.Collections.Generic;
using MMDVR.Scripts.Data;
using MMDVR.Scripts.Components;
using MMDVR.Scripts.Events;
using MMDVR.Events;

namespace MMDVR.Scripts.Managers
{    /// <summary>
    /// 资源管理器 - 专门负责资源的加载、卸载、存储管理
    /// 不涉及场景展示逻辑，只管理资源的生命周期
    /// 同时承担全局协程管理功能，确保异步操作能够正常进行
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [Header("资源容器")]
        [Tooltip("模型资源容器")] public Transform modelContainer;
        [Tooltip("音乐资源容器")] public Transform musicContainer;
        [Tooltip("动作资源容器")] public Transform motionContainer;
        [Tooltip("摄像机资源容器")] public Transform cameraContainer;

        [Header("资源数据")]
        [Tooltip("音乐数据列表")] public List<MusicData> musicList = new List<MusicData>();
        [Tooltip("摄像机数据列表")] public List<CameraData> cameraList = new List<CameraData>();

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
            InitializeContainers();
        }        /// <summary>
        /// 初始化资源容器
        /// </summary>
        private void InitializeContainers()
        {
            if (modelContainer == null)
                modelContainer = CreateResourceContainer("Models");
            if (musicContainer == null)
                musicContainer = CreateResourceContainer("Musics");
            if (motionContainer == null)
                motionContainer = CreateResourceContainer("Motions");
            if (cameraContainer == null)
            {
                cameraContainer = CreateResourceContainer("Cameras");
                // 创建摄像机子容器
                CreateCameraSubContainers();
            }
        }

        /// <summary>
        /// 创建摄像机子容器并初始化内置Free Camera
        /// </summary>
        private void CreateCameraSubContainers()
        {
            // 创建MMDCameras容器
            GameObject mmdCamerasObj = new GameObject("MMDCameras");
            mmdCamerasObj.transform.SetParent(cameraContainer);
            
            // 创建FreeCameras容器
            GameObject freeCamerasObj = new GameObject("FreeCameras");
            freeCamerasObj.transform.SetParent(cameraContainer);
            
            // 创建内置Free Camera资源对象
            GameObject freeCameraResource = new GameObject("FreeCamera_Resource");
            freeCameraResource.transform.SetParent(freeCamerasObj.transform);
            
            // 添加Free Camera组件
            var freeCamComponent = freeCameraResource.AddComponent<FreeCameraComponent>();
            freeCamComponent.id = "BUILTIN_FREE_CAMERA";
            freeCamComponent.displayName = "Free Camera";
            freeCamComponent.position = Vector3.zero;
            freeCamComponent.rotation = Quaternion.identity;
            freeCamComponent.fieldOfView = 60f;
            
            Debug.Log("ResourceManager: 创建了摄像机子容器和内置Free Camera");
        }

        /// <summary>
        /// 创建资源容器
        /// </summary>
        private Transform CreateResourceContainer(string containerName)
        {
            GameObject container = new GameObject(containerName);
            container.transform.SetParent(transform);
            Debug.Log($"ResourceManager: 创建了{containerName}容器");
            return container.transform;
        }

        // ==================== 异步加载示例 ====================
        
        /// <summary>
        /// 异步加载模型的示例方法
        /// </summary>
        /// <param name="modelPath">模型路径</param>
        /// <param name="onComplete">完成回调</param>
        public void LoadModelAsync(string modelPath, System.Action<string> onComplete = null)
        {
            StartGlobalCoroutine(LoadModelCoroutine(modelPath, onComplete));
        }
        
        private System.Collections.IEnumerator LoadModelCoroutine(string modelPath, System.Action<string> onComplete)
        {
            Debug.Log($"开始异步加载模型: {modelPath}");
            
            // 模拟异步加载过程
            yield return new WaitForEndOfFrame();
            
            // 实际加载模型
            string modelId = LoadModel(modelPath);
            
            Debug.Log($"模型异步加载完成: {modelId}");
            
            // 调用回调
            onComplete?.Invoke(modelId);
        }

        // ==================== 模型资源管理 ====================        /// <summary>
        /// 加载模型资源
        /// </summary>
        public string LoadModel(string modelPath)
        {
            Debug.Log($"ResourceManager.LoadModel: 开始加载模型 {modelPath}");
            
            if (string.IsNullOrEmpty(modelPath))
            {
                Debug.LogError("ResourceManager: 模型路径为空");
                return null;
            }

            if (modelContainer == null)
            {
                Debug.LogError("ResourceManager: modelContainer为null，尝试创建");
                modelContainer = CreateResourceContainer("Models");
            }

            Debug.Log($"ResourceManager: modelContainer有 {modelContainer.childCount} 个子对象");

            // 检查是否已有该模型
            for (int i = 0; i < modelContainer.childCount; i++)
            {
                var child = modelContainer.GetChild(i);
                var mc = child.GetComponent<ModelComponent>();
                if (mc != null && mc.filePath == modelPath)
                {
                    Debug.Log($"ResourceManager: 模型已存在 {modelPath}，返回ID={mc.modelId}");
                    return mc.modelId;
                }
            }

            // 创建新模型对象
            string modelId = System.Guid.NewGuid().ToString();
            GameObject modelObj = new GameObject($"Model_{modelId}");
            modelObj.transform.SetParent(modelContainer);

            Debug.Log($"ResourceManager: 创建模型对象 Model_{modelId}，父容器={modelContainer.name}");

            // 添加模型组件
            var modelComponent = modelObj.AddComponent<ModelComponent>();
            modelComponent.filePath = modelPath;
            modelComponent.modelId = modelId;

            Debug.Log($"ResourceManager: 模型资源加载完成: {modelPath}，ID={modelId}");
            Debug.Log($"ResourceManager: modelContainer现在有 {modelContainer.childCount} 个子对象");
            
            // 触发资源事件
            ResourceEvents.TriggerResourceLoaded("model", modelComponent.modelId);
            
            return modelComponent.modelId;
        }

        /// <summary>
        /// 卸载模型资源
        /// </summary>
        public void UnloadModel(string modelId)
        {
            Transform modelObj = modelContainer.Find($"Model_{modelId}");
            if (modelObj != null)
            {
                Debug.Log($"ResourceManager: 卸载模型资源: {modelId}");
                ResourceEvents.TriggerResourceUnloaded("model", modelId);
                Destroy(modelObj.gameObject);
            }
        }

        /// <summary>
        /// 获取模型组件
        /// </summary>
        public ModelComponent GetModel(string modelId)
        {
            Transform modelObj = modelContainer.Find($"Model_{modelId}");
            return modelObj?.GetComponent<ModelComponent>();
        }

        // ==================== 音乐资源管理 ====================

        /// <summary>
        /// 添加音乐资源
        /// </summary>
        public string AddMusic(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("ResourceManager: 音乐路径为空");
                return null;
            }            // 创建音乐对象
            string musicId = System.Guid.NewGuid().ToString(); // 完整GUID
            GameObject musicObj = new GameObject($"Music_{musicId}");
            musicObj.transform.SetParent(musicContainer);

            // 添加音乐组件
            var musicComponent = musicObj.AddComponent<MusicComponent>();
            musicComponent.filePath = filePath;
            musicComponent.musicId = musicId;            // 添加到数据列表
            var musicData = new MusicData
            {
                id = musicComponent.musicId,
                displayName = System.IO.Path.GetFileNameWithoutExtension(filePath),
                filePath = filePath
            };
            musicList.Add(musicData);
            
            Debug.Log($"ResourceManager: 添加音乐资源: {filePath}");
            ResourceEvents.TriggerResourceLoaded("music", musicComponent.musicId);
            ResourceEvents.TriggerMusicListChanged();

            return musicComponent.musicId;
        }

        /// <summary>
        /// 移除音乐资源
        /// </summary>
        public void RemoveMusic(string musicId)
        {
            Transform musicObj = musicContainer.Find($"Music_{musicId}");
            if (musicObj != null)
            {                // 从数据列表中移除
                musicList.RemoveAll(m => m.id == musicId);
                
                Debug.Log($"ResourceManager: 移除音乐资源: {musicId}");
                ResourceEvents.TriggerResourceUnloaded("music", musicId);
                ResourceEvents.TriggerMusicListChanged();
                
                Destroy(musicObj.gameObject);
            }
        }

        /// <summary>
        /// 获取音乐组件
        /// </summary>
        public MusicComponent GetMusic(string musicId)
        {
            Transform musicObj = musicContainer.Find($"Music_{musicId}");
            return musicObj?.GetComponent<MusicComponent>();
        }

        // ==================== 动作资源管理 ====================

        /// <summary>
        /// 添加动作资源
        /// </summary>
        public string AddMotion(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("ResourceManager: 动作路径为空");
                return null;
            }            // 创建动作对象
            string motionId = System.Guid.NewGuid().ToString(); // 完整GUID
            GameObject motionObj = new GameObject($"Motion_{motionId}");
            motionObj.transform.SetParent(motionContainer);

            // 添加动作组件
            var motionComponent = motionObj.AddComponent<MotionComponent>();
            motionComponent.filePath = filePath;
            motionComponent.motionId = motionId;
            
            Debug.Log($"ResourceManager: 添加动作资源: {filePath}");
            ResourceEvents.TriggerResourceLoaded("motion", motionComponent.motionId);
            ResourceEvents.TriggerMotionListChanged();

            return motionComponent.motionId;
        }

        /// <summary>
        /// 移除动作资源
        /// </summary>
        public void RemoveMotion(string motionId)
        {
            Transform motionObj = motionContainer.Find($"Motion_{motionId}");
            if (motionObj != null)
            {
                Debug.Log($"ResourceManager: 移除动作资源: {motionId}");
                ResourceEvents.TriggerResourceUnloaded("motion", motionId);
                ResourceEvents.TriggerMotionListChanged();
                
                Destroy(motionObj.gameObject);
            }
        }

        /// <summary>
        /// 获取动作组件
        /// </summary>
        public MotionComponent GetMotion(string motionId)
        {
            Transform motionObj = motionContainer.Find($"Motion_{motionId}");
            return motionObj?.GetComponent<MotionComponent>();
        }        // ==================== 摄像机资源管理 ====================        /// <summary>
        /// 添加VMD摄像机资源
        /// </summary>
        public string AddVMDCamera(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("ResourceManager: 摄像机路径为空");
                return null;
            }

            // 确保MMDCameras子容器存在
            Transform mmdCamerasContainer = cameraContainer.Find("MMDCameras");
            if (mmdCamerasContainer == null)
            {
                GameObject mmdCamerasObj = new GameObject("MMDCameras");
                mmdCamerasObj.transform.SetParent(cameraContainer);
                mmdCamerasContainer = mmdCamerasObj.transform;
            }

            // 创建VMD摄像机对象（按原系统命名）
            string cameraId = System.Guid.NewGuid().ToString(); // 完整GUID
            GameObject cameraObj = new GameObject($"VMDCamera_{cameraId}");
            cameraObj.transform.SetParent(mmdCamerasContainer);

            // 添加MMD摄像机组件
            var cameraComponent = cameraObj.AddComponent<MMDCameraComponent>();
            cameraComponent.cameraId = cameraId;
            cameraComponent.filePath = filePath;
            cameraComponent.displayName = System.IO.Path.GetFileNameWithoutExtension(filePath);

            // 加载VMD数据
            try
            {
                bool loadSuccess = cameraComponent.LoadVMDData(filePath);
                if (!loadSuccess)
                {
                    Debug.LogError($"ResourceManager: VMD摄像机数据加载失败 {filePath}");
                    Destroy(cameraObj);
                    return null;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ResourceManager: VMD摄像机加载异常: {e.Message}");
                Destroy(cameraObj);
                return null;
            }

            // 添加到数据列表
            var cameraData = new CameraData
            {
                id = cameraComponent.cameraId,
                displayName = cameraComponent.displayName,
                filePath = filePath,
                isMMDCamera = true,
                isFreeCamera = false
            };
            cameraList.Add(cameraData);

            Debug.Log($"ResourceManager: 添加VMD摄像机资源: {filePath} (ID: {cameraId})");
            ResourceEvents.TriggerResourceLoaded("camera", cameraComponent.cameraId);
            ResourceEvents.TriggerCameraListChanged();

            return cameraComponent.cameraId;
        }        /// <summary>
        /// 移除摄像机资源
        /// </summary>
        public void RemoveCamera(string cameraId)
        {
            // 不能删除内置Free Camera
            if (cameraId == "BUILTIN_FREE_CAMERA") return;
            
            // 在MMDCameras容器中查找VMD摄像机
            Transform mmdCamerasContainer = cameraContainer.Find("MMDCameras");
            Transform cameraObj = mmdCamerasContainer?.Find($"VMDCamera_{cameraId}");
            
            if (cameraObj != null)
            {
                // 从数据列表中移除
                cameraList.RemoveAll(c => c.id == cameraId);

                Debug.Log($"ResourceManager: 移除摄像机资源: {cameraId}");
                ResourceEvents.TriggerResourceUnloaded("camera", cameraId);
                ResourceEvents.TriggerCameraListChanged();
                
                Destroy(cameraObj.gameObject);
            }
            else
            {
                Debug.LogError($"ResourceManager: 未找到要删除的摄像机: VMDCamera_{cameraId}");
            }
        }        /// <summary>
        /// 获取摄像机组件
        /// </summary>
        public MMDCameraComponent GetCamera(string cameraId)
        {
            // 内置Free Camera没有MMDCameraComponent
            if (cameraId == "BUILTIN_FREE_CAMERA") return null;
            
            // 在MMDCameras容器中查找VMD摄像机
            Transform mmdCamerasContainer = cameraContainer.Find("MMDCameras");
            Transform cameraObj = mmdCamerasContainer?.Find($"VMDCamera_{cameraId}");
            return cameraObj?.GetComponent<MMDCameraComponent>();
        }

        // ==================== 通用资源管理 ====================        /// <summary>
        /// 清理所有资源
        /// </summary>
        public void ClearAllResources()
        {
            Debug.Log("ResourceManager: 清理所有资源");

            // 清理各类资源
            ClearContainer(modelContainer);
            ClearContainer(musicContainer);
            ClearContainer(motionContainer);
            ClearContainer(cameraContainer);

            // 清理数据列表
            musicList.Clear();
            cameraList.Clear();

            // 触发清理事件
            ResourceEvents.TriggerMusicListChanged();
            ResourceEvents.TriggerCameraListChanged();
            ResourceEvents.TriggerMotionListChanged();
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
            // 清理所有资源
            if (Instance == this)
            {
                ClearAllResources();
            }
        }

        // ==================== 资源列表获取方法 ====================

        /// <summary>
        /// 获取音乐列表
        /// </summary>
        public List<MusicData> GetMusicList()
        {
            return new List<MusicData>(musicList);
        }

        /// <summary>
        /// 获取音乐数据列表
        /// </summary>
        public List<MusicData> GetMusicDataList()
        {
            return GetMusicList();
        }

        /// <summary>
        /// 获取摄像机列表
        /// </summary>
        public List<CameraData> GetCameraList()
        {
            List<CameraData> cameraList = new List<CameraData>();
            
            // 添加内置Free Camera
            cameraList.Add(new CameraData
            {
                id = "BUILTIN_FREE_CAMERA",
                displayName = "Free Camera",
                filePath = "",
                isFreeCamera = true,
                isMMDCamera = false
            });
              // 添加VMD摄像机 - 从MMDCameras容器中查找
            Transform mmdCamerasContainer = cameraContainer?.Find("MMDCameras");
            if (mmdCamerasContainer != null)
            {
                for (int i = 0; i < mmdCamerasContainer.childCount; i++)
                {
                    Transform child = mmdCamerasContainer.GetChild(i);
                    if (child != null)
                    {
                        var mmdCameraComponent = child.GetComponent<MMDCameraComponent>();
                        if (mmdCameraComponent != null)
                        {
                            cameraList.Add(new CameraData
                            {
                                id = mmdCameraComponent.cameraId,
                                displayName = mmdCameraComponent.displayName,
                                filePath = mmdCameraComponent.filePath,
                                isFreeCamera = false,
                                isMMDCamera = true
                            });
                        }
                    }
                }
            }
            
            return cameraList;
        }

        /// <summary>
        /// 获取摄像机数据列表
        /// </summary>
        public List<CameraData> GetCameraDataList()
        {
            return GetCameraList();
        }        /// <summary>
        /// 获取模型列表
        /// </summary>
        public List<ModelComponent> GetModelList()
        {
            var modelList = new List<ModelComponent>();
            
            // 检查容器是否还存在
            if (modelContainer == null)
                return modelList;
                
            for (int i = 0; i < modelContainer.childCount; i++)
            {
                var child = modelContainer.GetChild(i);
                if (child != null)
                {
                    var modelComponent = child.GetComponent<ModelComponent>();
                    if (modelComponent != null)
                    {
                        modelList.Add(modelComponent);
                    }
                }
            }
            return modelList;
        }

        /// <summary>        /// 获取动作列表
        /// </summary>
        public List<MotionComponent> GetMotionList()
        {
            var motionList = new List<MotionComponent>();
            
            // 检查容器是否还存在
            if (motionContainer == null)
                return motionList;
                
            for (int i = 0; i < motionContainer.childCount; i++)
            {
                var child = motionContainer.GetChild(i);
                if (child != null)
                {
                    var motionComponent = child.GetComponent<MotionComponent>();
                    if (motionComponent != null)
                    {
                        motionList.Add(motionComponent);
                    }
                }
            }
            return motionList;
        }

        /// <summary>
        /// 获取动作数据列表
        /// </summary>
        public List<MotionData> GetMotionDataList()
        {
            var motionDataList = new List<MotionData>();
            var motionComponents = GetMotionList();
            foreach (var motion in motionComponents)
            {
                var motionData = new MotionData
                {
                    id = motion.motionId,
                    displayName = motion.displayName,
                    filePath = motion.filePath,
                    assignedActorId = motion.assignedActorId
                };
                motionDataList.Add(motionData);
            }
            return motionDataList;
        }

        /// <summary>
        /// 移除模型资源
        /// </summary>
        public void RemoveModel(string modelId)
        {
            UnloadModel(modelId);
        }

        /// <summary>
        /// 更新自由摄像机变换
        /// </summary>
        public void UpdateFreeCameraTransform(Vector3 position, Quaternion rotation, float fov = 60f)
        {
            // 这个方法应该由摄像机管理器处理，这里保留为兼容性方法
            Debug.Log($"ResourceManager: 更新自由摄像机变换 位置:{position} 旋转:{rotation} FOV:{fov}");
        }

        // ==================== 协程使用说明和示例 ====================
        
        /*
         * 协程(Coroutine)是什么？
         * 
         * 协程是Unity中处理异步操作的重要机制，它允许你在多个帧之间分布执行代码，
         * 而不会阻塞主线程。这对于以下场景特别有用：
         * 
         * 1. 文件加载 - 加载大型PMX模型文件时不卡顿
         * 2. 网络请求 - 下载资源时不阻塞UI
         * 3. 动画播放 - 平滑的动画过渡
         * 4. 定时任务 - 定期检查状态或更新
         * 5. 分帧处理 - 将复杂计算分散到多个帧中
         * 
         * 为什么需要全局协程管理？
         * 
         * Unity的协程必须在MonoBehaviour上启动，如果启动协程的GameObject被销毁，
         * 协程就会停止。ResourceManager使用了DontDestroyOnLoad，确保协程不会因为
         * 场景切换或UI隐藏而意外停止。
         * 
         * 什么时候使用？
         * 
         * - 需要加载大文件时（PMX模型、VMD动作、音频等）
         * - 需要平滑动画或渐变效果时
         * - 需要定期检查状态时（播放进度、网络状态等）
         * - 需要分帧处理大量数据时
         * - 其他任何需要异步执行的场景
         */
        
        /// <summary>
        /// 示例：分帧加载多个模型，避免卡顿
        /// </summary>
        public void LoadMultipleModelsAsync(string[] modelPaths, System.Action onAllComplete = null)
        {
            StartGlobalCoroutine(LoadMultipleModelsCoroutine(modelPaths, onAllComplete));
        }
        
        private System.Collections.IEnumerator LoadMultipleModelsCoroutine(string[] modelPaths, System.Action onAllComplete)
        {
            Debug.Log($"开始分帧加载{modelPaths.Length}个模型");
            
            for (int i = 0; i < modelPaths.Length; i++)
            {
                Debug.Log($"加载模型 {i + 1}/{modelPaths.Length}: {modelPaths[i]}");
                
                // 加载模型
                LoadModel(modelPaths[i]);
                
                // 每加载一个模型后等待一帧，避免卡顿
                yield return new WaitForEndOfFrame();
                
                // 或者等待固定时间
                // yield return new WaitForSeconds(0.1f);
            }
            
            Debug.Log("所有模型加载完成");
            onAllComplete?.Invoke();
        }
        
        /// <summary>
        /// 示例：带进度回调的异步加载
        /// </summary>
        public void LoadModelWithProgress(string modelPath, System.Action<float> onProgress = null, System.Action<string> onComplete = null)
        {
            StartGlobalCoroutine(LoadModelWithProgressCoroutine(modelPath, onProgress, onComplete));
        }
        
        private System.Collections.IEnumerator LoadModelWithProgressCoroutine(string modelPath, System.Action<float> onProgress, System.Action<string> onComplete)
        {
            Debug.Log($"开始加载模型: {modelPath}");
            
            // 模拟加载进度
            for (float progress = 0f; progress <= 1f; progress += 0.1f)
            {
                onProgress?.Invoke(progress);
                yield return new WaitForSeconds(0.1f); // 模拟加载时间
            }
            
            // 实际加载模型
            string modelId = LoadModel(modelPath);
            
            onProgress?.Invoke(1f); // 完成
            onComplete?.Invoke(modelId);
        }

        // ==================== 原有的全局协程管理方法 ====================
          /// <summary>
        /// 全局启动协程 - 确保即使UI被隐藏也能正常运行协程
        /// </summary>
        /// <param name="routine">要启动的协程</param>
        /// <returns>协程句柄</returns>
        public Coroutine StartGlobalCoroutine(System.Collections.IEnumerator routine)
        {
            return StartCoroutine(routine);
        }
        
        /// <summary>
        /// 全局停止协程
        /// </summary>
        /// <param name="routine">要停止的协程句柄</param>
        public void StopGlobalCoroutine(Coroutine routine)
        {
            if (routine != null)
                StopCoroutine(routine);
        }
        
        /// <summary>
        /// 全局停止所有协程
        /// </summary>        /// <summary>
        /// 全局停止所有协程
        /// </summary>
        public void StopAllGlobalCoroutines()
        {
            StopAllCoroutines();        }
    }
}
