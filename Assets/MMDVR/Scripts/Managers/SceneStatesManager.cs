using System.Collections.Generic;
using UnityEngine;
using MMDVR.Scripts.UIInteraction;
using MMDVR.Scripts.Components;
using MMDVR.Scripts.Data;
using System.Collections;
using System.Linq;
using UnityEngine.Networking;
using System.IO;
using LibMMD.Unity3D;

namespace MMDVR.Scripts.Managers
{    /// <summary>
    /// 场景状态管理器 - 桥接层（已废弃，建议直接使用ResourceManager、SceneDisplayManager、PlaybackManager、SystemStateManager）
    /// 此类仅为向后兼容性保留，所有方法都转发到相应的专门管理器
    /// 
    /// 新架构：
    /// - ResourceManager: 资源管理（模型、动作、音乐、摄像机的加载和存储）
    /// - SceneDisplayManager: 场景展示管理（资源在场景中的激活、控制、演员管理）
    /// - PlaybackManager: 播放控制和状态管理
    /// - SystemStateManager: 系统状态（VR/桌面切换等）
    /// </summary>
    [System.Obsolete("SceneStatesManager已废弃，请直接使用ResourceManager、SceneDisplayManager、PlaybackManager、SystemStateManager")]
    public class SceneStatesManager : MonoBehaviour
    {
        public static SceneStatesManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        // ========== 播放状态属性（转发到PlaybackManager） ==========
        
        /// <summary>
        /// 是否正在播放 - 已废弃，请使用PlaybackManager.Instance.isPlaying
        /// </summary>
        [System.Obsolete("使用PlaybackManager.Instance.isPlaying")]
        public bool isPlaying => PlaybackManager.Instance?.isPlaying ?? false;
          /// <summary>
        /// 当前播放进度 - 已废弃，请使用PlaybackManager.Instance.playTime
        /// </summary>
        [System.Obsolete("使用PlaybackManager.Instance.playTime")]
        public float playTime 
        { 
            get => PlaybackManager.Instance?.playTime ?? 0f;
            set 
            {
                if (PlaybackManager.Instance != null)
                    PlaybackManager.Instance.playTime = value;
            }
        }
        
        /// <summary>
        /// 播放时长 - 已废弃，请使用PlaybackManager.Instance.totalDuration
        /// </summary>
        [System.Obsolete("使用PlaybackManager.Instance.totalDuration")]
        public float totalDuration => PlaybackManager.Instance?.totalDuration ?? 0f;

        // ========== 当前激活状态（转发到SceneDisplayManager） ==========
          /// <summary>
        /// 当前激活音乐ID - 已废弃，请使用SceneDisplayManager.Instance.currentActiveMusicId
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager.Instance.currentActiveMusicId")]
        public string currentActiveMusicId => SceneDisplayManager.Instance?.currentActiveMusicId ?? "";
        
        /// <summary>
        /// 当前激活摄像机ID - 已废弃，请使用SceneDisplayManager.Instance.currentActiveCameraId
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager.Instance.currentActiveCameraId")]
        public string currentActiveCameraId => SceneDisplayManager.Instance?.currentActiveCameraId ?? "";

        // ========== 摄像机管理（转发到SystemStateManager） ==========
        
        /// <summary>
        /// 获取当前活动摄像机 - 已废弃，请使用SystemStateManager.Instance.GetActiveCamera()
        /// </summary>
        [System.Obsolete("使用SystemStateManager.Instance.GetActiveCamera()")]
        public Camera GetActiveCamera()
        {
            return SystemStateManager.Instance?.GetActiveCamera() ?? Camera.main;
        }

        // ========== 播放控制方法（转发到PlaybackManager） ==========
        
        /// <summary>
        /// 开始播放 - 已废弃，请使用PlaybackManager.Instance.Play()
        /// </summary>
        [System.Obsolete("使用PlaybackManager.Instance.Play()")]
        public void Play()
        {
            PlaybackManager.Instance?.Play();
        }
        
        /// <summary>
        /// 暂停播放 - 已废弃，请使用PlaybackManager.Instance.Pause()
        /// </summary>
        [System.Obsolete("使用PlaybackManager.Instance.Pause()")]
        public void Pause()
        {
            PlaybackManager.Instance?.Pause();
        }
        
        /// <summary>
        /// 跳转到指定时间 - 已废弃，请使用PlaybackManager.Instance.SeekTo()
        /// </summary>
        [System.Obsolete("使用PlaybackManager.Instance.SeekTo()")]
        public void SeekTo(float time)
        {
            PlaybackManager.Instance?.SeekTo(time);
        }
        
        /// <summary>
        /// 设置音乐音量 - 已废弃，请使用PlaybackManager.Instance.SetMusicVolume()
        /// </summary>
        [System.Obsolete("使用PlaybackManager.Instance.SetMusicVolume()")]
        public void SetMusicVolume(float volume)
        {
            PlaybackManager.Instance?.SetMusicVolume(volume);
        }
        
        /// <summary>
        /// 获取音乐时长 - 已废弃，请使用PlaybackManager.Instance.GetMusicDuration()
        /// </summary>
        [System.Obsolete("使用PlaybackManager.Instance.GetMusicDuration()")]
        public float GetMusicDuration()
        {
            return PlaybackManager.Instance?.GetMusicDuration() ?? 0f;
        }

        // ========== 资源管理方法（转发到ResourceManager和SceneDisplayManager） ==========
          /// <summary>
        /// 添加音乐 - 已废弃，请使用ResourceManager.Instance.AddMusic()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.AddMusic()")]
        public void AddMusic(string filePath)
        {
            ResourceManager.Instance?.AddMusic(filePath);
        }
        
        /// <summary>
        /// 移除音乐 - 已废弃，请使用ResourceManager.Instance.RemoveMusic()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.RemoveMusic()")]
        public void RemoveMusic(string musicId)
        {
            ResourceManager.Instance?.RemoveMusic(musicId);
        }
          /// <summary>
        /// 激活音乐 - 已废弃，请使用SceneDisplayManager.Instance.ActivateMusic()
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager.Instance.ActivateMusic()")]
        public void ActivateMusic(string musicId)
        {
            SceneDisplayManager.Instance?.ActivateMusic(musicId);
        }
          /// <summary>
        /// 添加演员 - 已废弃，请使用ResourceManager.Instance.LoadModel() + SceneDisplayManager.Instance.AddActor()
        /// </summary>        [System.Obsolete("使用ResourceManager.Instance.LoadModel() + SceneDisplayManager.Instance.AddActor()")]
        public void AddActor(string filePath)
        {
            Debug.Log($"SceneStatesManager.AddActor: 开始加载演员 {filePath}");
            
            // 检查ResourceManager是否存在
            if (ResourceManager.Instance == null)
            {
                Debug.LogError("SceneStatesManager.AddActor: ResourceManager.Instance为null");
                return;
            }
            
            // 先加载模型资源，再添加到场景显示
            Debug.Log("SceneStatesManager.AddActor: 调用ResourceManager.LoadModel");
            var modelId = ResourceManager.Instance.LoadModel(filePath);
            
            if (!string.IsNullOrEmpty(modelId))
            {
                Debug.Log($"SceneStatesManager.AddActor: 模型加载成功，ID={modelId}，现在添加到场景");
                
                // 检查SceneDisplayManager是否存在
                if (SceneDisplayManager.Instance == null)
                {
                    Debug.LogError("SceneStatesManager.AddActor: SceneDisplayManager.Instance为null");
                    return;
                }
                
                var actorId = SceneDisplayManager.Instance.AddActor(modelId);
                Debug.Log($"SceneStatesManager.AddActor: 演员添加完成，ActorID={actorId}");
            }
            else
            {
                Debug.LogError($"SceneStatesManager.AddActor: 模型加载失败 {filePath}");
            }
        }
        
        /// <summary>
        /// 移除演员 - 已废弃，请使用SceneDisplayManager.Instance.RemoveActor()
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager.Instance.RemoveActor()")]
        public void RemoveActor(string actorId)
        {
            SceneDisplayManager.Instance?.RemoveActor(actorId);
        }
        
        /// <summary>
        /// 添加动作 - 已废弃，请使用ResourceManager.Instance.AddMotion()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.AddMotion()")]
        public string AddMotion(string filePath)
        {
            return ResourceManager.Instance?.AddMotion(filePath) ?? "";
        }
        
        /// <summary>
        /// 移除动作 - 已废弃，请使用ResourceManager.Instance.RemoveMotion()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.RemoveMotion()")]
        public void RemoveMotion(string motionId)
        {
            ResourceManager.Instance?.RemoveMotion(motionId);
        }        /// <summary>
        /// 分配动作到演员 - 已废弃，请使用SceneDisplayManager.Instance.AssignMotionToActor()
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager.Instance.AssignMotionToActor()")]
        public void AssignMotionToActor(string motionId, string actorId)
        {
            SceneDisplayManager.Instance?.AssignMotionToActor(actorId, motionId);
        }
          /// <summary>
        /// 添加VMD摄像机 - 已废弃，请使用ResourceManager.Instance.AddVMDCamera()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.AddVMDCamera()")]
        public void AddVMDCamera(string filePath)
        {
            ResourceManager.Instance?.AddVMDCamera(filePath);
        }
        
        /// <summary>
        /// 移除摄像机 - 已废弃，请使用ResourceManager.Instance.RemoveCamera()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.RemoveCamera()")]
        public void RemoveCamera(string cameraId)
        {
            ResourceManager.Instance?.RemoveCamera(cameraId);
        }
        
        /// <summary>
        /// 激活摄像机 - 已废弃，请使用SceneDisplayManager.Instance.ActivateCamera()
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager.Instance.ActivateCamera()")]
        public void ActivateCamera(string cameraId)
        {
            SceneDisplayManager.Instance?.ActivateCamera(cameraId);
        }        // ========== 资源查询方法（转发到ResourceManager和SceneDisplayManager） ==========
        
        /// <summary>
        /// 获取音乐列表 - 已废弃，请使用ResourceManager.Instance.GetMusicList()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.GetMusicList()")]
        public List<MusicData> GetMusicList()
        {
            return ResourceManager.Instance?.GetMusicList() ?? new List<MusicData>();
        }
        
        /// <summary>
        /// 获取音乐数据列表 - 已废弃，请使用ResourceManager.Instance.GetMusicDataList()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.GetMusicDataList()")]
        public List<MusicData> GetMusicDataList()
        {
            return ResourceManager.Instance?.GetMusicDataList() ?? new List<MusicData>();
        }          /// <summary>
        /// 获取摄像机列表 - 已废弃，请使用ResourceManager.Instance.GetCameraList()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.GetCameraList()")]
        public List<MMDVR.Scripts.Data.CameraData> GetCameraList()
        {
            return ResourceManager.Instance?.GetCameraList() ?? new List<MMDVR.Scripts.Data.CameraData>();
        }
          /// <summary>
        /// 获取摄像机数据列表 - 已废弃，请使用ResourceManager.Instance.GetCameraDataList()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.GetCameraDataList()")]
        public List<MMDVR.Scripts.Data.CameraData> GetCameraDataList()
        {
            return ResourceManager.Instance?.GetCameraDataList() ?? new List<MMDVR.Scripts.Data.CameraData>();
        }
        
        /// <summary>
        /// 获取演员列表 - 已废弃，请使用SceneDisplayManager.Instance.GetActorList()
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager.Instance.GetActorList()")]
        public List<ActorData> GetActorList()
        {
            return SceneDisplayManager.Instance?.GetActorList() ?? new List<ActorData>();
        }        /// <summary>
        /// 获取模型列表 - 已废弃，请使用ResourceManager.Instance.GetModelList()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.GetModelList()")]
        public List<ModelData> GetModelList()
        {
            var modelComponents = ResourceManager.Instance?.GetModelList() ?? new List<ModelComponent>();
            var modelDataList = new List<ModelData>();
            foreach (var modelComponent in modelComponents)
            {
                var modelData = new ModelData
                {
                    id = modelComponent.modelId,
                    displayName = modelComponent.displayName,
                    filePath = modelComponent.filePath
                };
                modelDataList.Add(modelData);
            }
            return modelDataList;
        }
          /// <summary>
        /// 获取动作列表 - 已废弃，请使用ResourceManager.Instance.GetMotionList()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.GetMotionList()")]
        public List<MotionData> GetMotionList()
        {
            return ResourceManager.Instance?.GetMotionDataList() ?? new List<MotionData>();
        }
        
        /// <summary>
        /// 获取动作数据列表 - 已废弃，请使用ResourceManager.Instance.GetMotionDataList()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.GetMotionDataList()")]
        public List<MotionData> GetMotionDataList()
        {
            return ResourceManager.Instance?.GetMotionDataList() ?? new List<MotionData>();
        }        // ========== 关联管理方法（转发到SceneDisplayManager） ==========
        
        /// <summary>
        /// 获取模型关联的动作 - 已废弃，请使用SceneDisplayManager.Instance.GetModelAssociatedMotions()
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager.Instance.GetModelAssociatedMotions()")]
        public List<string> GetModelAssociatedMotions(string modelId)
        {
            return SceneDisplayManager.Instance?.GetModelAssociatedMotions(modelId) ?? new List<string>();
        }
        
        /// <summary>
        /// 获取关联的动作 - 已废弃，请使用SceneDisplayManager.Instance.GetAssociatedMotions()
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager.Instance.GetAssociatedMotions()")]
        public List<string> GetAssociatedMotions(string modelId)
        {
            return SceneDisplayManager.Instance?.GetAssociatedMotions(modelId) ?? new List<string>();
        }
        
        /// <summary>
        /// 获取关联的模型 - 已废弃，请使用SceneDisplayManager.Instance.GetAssociatedModels()
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager.Instance.GetAssociatedModels()")]
        public List<string> GetAssociatedModels(string motionId)
        {
            return SceneDisplayManager.Instance?.GetAssociatedModels(motionId) ?? new List<string>();
        }        // ========== 对象获取方法（转发到ResourceManager和SceneDisplayManager） ==========
          /// <summary>
        /// 根据ID获取模型对象 - 已废弃，请使用ResourceManager.Instance.GetModel()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.GetModel()")]
        public GameObject GetModelObjectById(string id)
        {
            var modelComponent = ResourceManager.Instance?.GetModel(id);
            return modelComponent?.gameObject;
        }
        
        /// <summary>
        /// 根据ID获取演员对象 - 已废弃，请使用SceneDisplayManager.Instance.GetActorGameObject()
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager.Instance.GetActorGameObject()")]
        public GameObject GetActorObjectById(string id)
        {
            return SceneDisplayManager.Instance?.GetActorGameObject(id);
        }
        
        /// <summary>
        /// 根据ID获取模型组件 - 已废弃，请使用ResourceManager.Instance.GetModel()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.GetModel()")]
        public ModelComponent GetModelComponentById(string id)
        {
            var modelObj = ResourceManager.Instance?.GetModel(id);
            return modelObj?.GetComponent<ModelComponent>();
        }
        
        /// <summary>
        /// 根据ID获取音乐对象 - 已废弃，请使用ResourceManager.Instance.GetMusic()
        /// </summary>        [System.Obsolete("使用ResourceManager.Instance.GetMusic()")]
        public GameObject GetMusicObjectById(string id)
        {
            var musicComponent = ResourceManager.Instance?.GetMusic(id);
            return musicComponent?.gameObject;
        }
        
        /// <summary>
        /// 根据ID获取音乐组件 - 已废弃，请使用ResourceManager.Instance.GetMusic()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.GetMusic()")]
        public MusicComponent GetMusicComponentById(string id)
        {
            var musicObj = ResourceManager.Instance?.GetMusic(id);
            return musicObj?.GetComponent<MusicComponent>();
        }        /// <summary>
        /// 根据ID获取动作对象 - 已废弃，请使用ResourceManager.Instance.GetMotion()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.GetMotion()")]
        public GameObject GetMotionObjectById(string id)
        {
            var motionComponent = ResourceManager.Instance?.GetMotion(id);
            return motionComponent?.gameObject;
        }
        
        /// <summary>
        /// 根据ID获取动作组件 - 已废弃，请使用ResourceManager.Instance.GetMotion()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.GetMotion()")]
        public MotionComponent GetMotionComponentById(string id)
        {
            var motionObj = ResourceManager.Instance?.GetMotion(id);
            return motionObj?.GetComponent<MotionComponent>();
        }

        // ========== 其他兼容方法 ==========
          /// <summary>
        /// 获取活动摄像机数据 - 已废弃，请使用ResourceManager.Instance.GetActiveCameraData()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.GetActiveCameraData()")]
        public MMDVR.Scripts.Data.CameraData GetActiveCameraData()
        {
            // 这个功能需要通过SceneDisplayManager获取当前激活摄像机，然后从ResourceManager获取数据
            var activeCameraId = SceneDisplayManager.Instance?.currentActiveCameraId;
            if (string.IsNullOrEmpty(activeCameraId)) return null;
            
            var cameraList = ResourceManager.Instance?.GetCameraList();
            return cameraList?.FirstOrDefault(c => c.id == activeCameraId);
        }
        
        /// <summary>
        /// 更新Free Camera变换 - 已废弃，请使用ResourceManager.Instance.UpdateFreeCameraTransform()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.UpdateFreeCameraTransform()")]
        public void UpdateFreeCameraTransform(Vector3 position, Quaternion rotation, float fov)
        {
            ResourceManager.Instance?.UpdateFreeCameraTransform(position, rotation, fov);
        }
        
        /// <summary>
        /// 设置活动音乐 - 已废弃，请使用SceneDisplayManager.Instance.ActivateMusic()
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager.Instance.ActivateMusic()")]
        public void SetActiveMusic(string musicId)
        {
            SceneDisplayManager.Instance?.ActivateMusic(musicId);
        }
        
        /// <summary>
        /// 设置活动摄像机 - 已废弃，请使用SceneDisplayManager.Instance.ActivateCamera()
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager.Instance.ActivateCamera()")]
        public void SetActiveCamera(string cameraId)
        {
            SceneDisplayManager.Instance?.ActivateCamera(cameraId);
        }
        
        /// <summary>
        /// 移除音乐资源 - 已废弃，请使用ResourceManager.Instance.RemoveMusic()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.RemoveMusic()")]
        public void RemoveMusicResource(string musicId)
        {
            ResourceManager.Instance?.RemoveMusic(musicId);
        }
          
        /// <summary>
        /// 移除摄像机资源 - 已废弃，请使用ResourceManager.Instance.RemoveCamera()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.RemoveCamera()")]
        public void RemoveCameraResource(string cameraId)
        {
            ResourceManager.Instance?.RemoveCamera(cameraId);
        }
        
        /// <summary>
        /// 移除模型资源 - 已废弃，请使用ResourceManager.Instance.RemoveModel()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.RemoveModel()")]
        public void RemoveModelResource(string modelId)
        {
            ResourceManager.Instance?.RemoveModel(modelId);
        }
        
        /// <summary>
        /// 移除动作资源 - 已废弃，请使用ResourceManager.Instance.RemoveMotion()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.RemoveMotion()")]        public void RemoveMotionResource(string motionId)
        {
            ResourceManager.Instance?.RemoveMotion(motionId);
        }

        /// <summary>
        /// 测试用：添加演员 - 已废弃，请使用SceneDisplayManager.Instance进行测试演员添加
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager.Instance进行测试演员添加")]
        public void AddActorForTesting(string actorId, string displayName)        {
            // 这个功能不再支持，直接返回
            Debug.LogWarning("AddActorForTesting已废弃，请使用新的Manager架构");
        }
        
        /// <summary>
        /// 测试用：添加动作 - 已废弃，请使用ResourceManager.Instance进行测试动作添加
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance进行测试动作添加")]
        public void AddMotionForTesting(string motionId, string displayName)
        {
            // 这个功能不再支持，直接返回
            Debug.LogWarning("AddMotionForTesting已废弃，请使用新的Manager架构");
        }

        // ========== 缺失的方法（向后兼容） ==========
        
        /// <summary>
        /// 添加模型 - 已废弃，请使用ResourceManager.Instance.LoadModel() + SceneDisplayManager.Instance.AddActor()
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.LoadModel() + SceneDisplayManager.Instance.AddActor()")]
        public string AddModel(string filePath)
        {
            var modelId = ResourceManager.Instance?.LoadModel(filePath);
            if (!string.IsNullOrEmpty(modelId))
            {
                SceneDisplayManager.Instance?.AddActor(modelId);
            }
            return modelId ?? System.Guid.NewGuid().ToString(); // 返回一个ID用于兼容
        }
        
        /// <summary>
        /// 关联模型和动作 - 已废弃，请使用SceneDisplayManager.Instance.AssignMotionToActor()
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager.Instance.AssignMotionToActor()")]
        public void AssociateModelWithMotion(string modelId, string motionId)
        {
            SceneDisplayManager.Instance?.AssignMotionToActor(motionId, modelId);
        }
        
        /// <summary>
        /// 解除模型和动作关联 - 已废弃，请使用SceneDisplayManager相应方法
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager相应方法")]
        public void DisassociateModelFromMotion(string modelId, string motionId)
        {            // 这个功能需要在SceneDisplayManager中实现
            Debug.LogWarning("DisassociateModelFromMotion功能需要在SceneDisplayManager中实现");
        }
        
        /// <summary>
        /// 切换模型状态 - 已废弃，请使用SceneDisplayManager相应方法
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager相应方法")]
        public void ToggleModel(string modelId)
        {
            // 这个功能需要在SceneDisplayManager中实现
            Debug.LogWarning("ToggleModel功能需要在SceneDisplayManager中实现");
        }
        
        /// <summary>        /// 检查模型是否被禁用 - 已废弃，请使用ResourceManager相应方法
        /// </summary>
        [System.Obsolete("使用ResourceManager相应方法")]
        public bool IsModelDisabled(string modelId)
        {
            // 这个功能需要在ResourceManager中实现
            Debug.LogWarning("IsModelDisabled功能需要在ResourceManager中实现");
            return false;
        }

        // ========== 容器属性（只读访问，转发到ResourceManager和SceneDisplayManager） ==========
        
        /// <summary>
        /// 音乐容器 - 已废弃，请使用ResourceManager.Instance.musicContainer
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.musicContainer")]
        public Transform musicContainer => ResourceManager.Instance?.musicContainer;
          /// <summary>
        /// 演员容器 - 已废弃，请使用SceneDisplayManager.Instance.actorContainer
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager.Instance.actorContainer")]
        public Transform actorContainer => SceneDisplayManager.Instance?.actorContainer;
        
        /// <summary>
        /// 模型容器 - 已废弃，请使用ResourceManager.Instance.modelContainer
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.modelContainer")]
        public Transform modelContainer => ResourceManager.Instance?.modelContainer;
        
        /// <summary>
        /// 动作容器 - 已废弃，请使用ResourceManager.Instance.motionContainer
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.motionContainer")]
        public Transform motionContainer => ResourceManager.Instance?.motionContainer;
        
        /// <summary>
        /// 摄像机容器 - 已废弃，请使用ResourceManager.Instance.cameraContainer
        /// </summary>
        [System.Obsolete("使用ResourceManager.Instance.cameraContainer")]
        public Transform cameraContainer => ResourceManager.Instance?.cameraContainer;
          /// <summary>
        /// 应用摄像机状态 - 已废弃，请使用SceneDisplayManager.Instance.ApplyCameraState()
        /// </summary>
        [System.Obsolete("使用SceneDisplayManager.Instance.ApplyCameraState()")]
        public void PublicApplyCameraState(MMDVR.Scripts.Components.CameraState cameraState)
        {
            SceneDisplayManager.Instance?.ApplyCameraState(cameraState);
        }
    }
}
