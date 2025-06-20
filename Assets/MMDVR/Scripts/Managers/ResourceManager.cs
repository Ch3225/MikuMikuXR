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
                cameraContainer = CreateResourceContainer("Cameras");
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

        // ==================== 模型资源管理 ====================

        /// <summary>
        /// 加载模型资源
        /// </summary>
        public string LoadModel(string modelPath)
        {
            if (string.IsNullOrEmpty(modelPath))
            {
                Debug.LogError("ResourceManager: 模型路径为空");
                return null;
            }

            // 检查是否已有该模型
            for (int i = 0; i < modelContainer.childCount; i++)
            {
                var child = modelContainer.GetChild(i);
                var mc = child.GetComponent<ModelComponent>();
                if (mc != null && mc.filePath == modelPath)
                {
                    Debug.Log($"ResourceManager: 模型已存在 {modelPath}");
                    return mc.modelId;
                }
            }

            // 创建新模型对象
            GameObject modelObj = new GameObject($"Model_{System.Guid.NewGuid().ToString("N")[..8]}");
            modelObj.transform.SetParent(modelContainer);

            // 添加模型组件
            var modelComponent = modelObj.AddComponent<ModelComponent>();
            modelComponent.filePath = modelPath;
            modelComponent.modelId = modelObj.name.Replace("Model_", "");

            Debug.Log($"ResourceManager: 加载模型资源: {modelPath}");
            
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
            }

            // 创建音乐对象
            GameObject musicObj = new GameObject($"Music_{System.Guid.NewGuid().ToString("N")[..8]}");
            musicObj.transform.SetParent(musicContainer);

            // 添加音乐组件
            var musicComponent = musicObj.AddComponent<MusicComponent>();
            musicComponent.filePath = filePath;
            musicComponent.musicId = musicObj.name.Replace("Music_", "");            // 添加到数据列表
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
            }

            // 创建动作对象
            GameObject motionObj = new GameObject($"Motion_{System.Guid.NewGuid().ToString("N")[..8]}");
            motionObj.transform.SetParent(motionContainer);

            // 添加动作组件
            var motionComponent = motionObj.AddComponent<MotionComponent>();
            motionComponent.filePath = filePath;
            motionComponent.motionId = motionObj.name.Replace("Motion_", "");
            
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
        }        // ==================== 摄像机资源管理 ====================

        /// <summary>
        /// 添加VMD摄像机资源
        /// </summary>
        public string AddVMDCamera(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("ResourceManager: 摄像机路径为空");
                return null;
            }

            // 创建摄像机对象
            GameObject cameraObj = new GameObject($"Camera_{System.Guid.NewGuid().ToString("N")[..8]}");
            cameraObj.transform.SetParent(cameraContainer);

            // 添加摄像机组件
            var cameraComponent = cameraObj.AddComponent<MMDCameraComponent>();
            cameraComponent.cameraId = cameraObj.name.Replace("Camera_", "");

            // 添加到数据列表
            var cameraData = new CameraData
            {
                id = cameraComponent.cameraId,
                displayName = System.IO.Path.GetFileNameWithoutExtension(filePath),
                filePath = filePath,
                isFreeCamera = false
            };
            cameraList.Add(cameraData);

            Debug.Log($"ResourceManager: 添加VMD摄像机资源: {filePath}");
            ResourceEvents.TriggerResourceLoaded("camera", cameraComponent.cameraId);
            ResourceEvents.TriggerCameraListChanged();

            return cameraComponent.cameraId;
        }        /// <summary>
        /// 移除摄像机资源
        /// </summary>
        public void RemoveCamera(string cameraId)
        {
            Transform cameraObj = cameraContainer.Find($"Camera_{cameraId}");
            if (cameraObj != null)
            {
                // 从数据列表中移除
                cameraList.RemoveAll(c => c.id == cameraId);

                Debug.Log($"ResourceManager: 移除摄像机资源: {cameraId}");
                ResourceEvents.TriggerResourceUnloaded("camera", cameraId);
                ResourceEvents.TriggerCameraListChanged();
                
                Destroy(cameraObj.gameObject);
            }
        }        /// <summary>
        /// 获取摄像机组件
        /// </summary>
        public MMDCameraComponent GetCamera(string cameraId)
        {
            Transform cameraObj = cameraContainer.Find($"Camera_{cameraId}");
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
            return new List<CameraData>(cameraList);
        }

        /// <summary>
        /// 获取摄像机数据列表
        /// </summary>
        public List<CameraData> GetCameraDataList()
        {
            return GetCameraList();
        }

        /// <summary>
        /// 获取模型列表
        /// </summary>
        public List<ModelComponent> GetModelList()
        {
            var modelList = new List<ModelComponent>();
            for (int i = 0; i < modelContainer.childCount; i++)
            {
                var modelComponent = modelContainer.GetChild(i).GetComponent<ModelComponent>();
                if (modelComponent != null)
                {
                    modelList.Add(modelComponent);
                }
            }
            return modelList;
        }

        /// <summary>
        /// 获取动作列表
        /// </summary>
        public List<MotionComponent> GetMotionList()
        {
            var motionList = new List<MotionComponent>();
            for (int i = 0; i < motionContainer.childCount; i++)
            {
                var motionComponent = motionContainer.GetChild(i).GetComponent<MotionComponent>();
                if (motionComponent != null)
                {
                    motionList.Add(motionComponent);
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
    }
}
