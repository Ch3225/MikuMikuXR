using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMDVR.Scripts.Components;
using MMDVR.Scripts.Model;

namespace MMDVR.Scripts.Managers
{    /// <summary>
    /// 用户行为管理器 - 协调各个管理器，处理用户级别的操作
    /// 职责: 编排复杂的用户操作流程，确保各管理器正确协调工作
    /// </summary>
    public class UserActionManager : MonoBehaviour
    {
        public static UserActionManager Instance { get; private set; }

        // 用户行为事件
        public static event Action<string> OnModelLoadStarted;
        public static event Action<string> OnModelLoadCompleted;
        public static event Action<string> OnModelUnloadCompleted;
        public static event Action<string> OnMotionLoadCompleted;
        public static event Action<string, string> OnModelMotionAssigned;
        public static event Action<string> OnSceneCleared;        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ==================== 模型相关用户行为 ====================
        
        /// <summary>
        /// 用户行为：加载模型并在场景中显示
        /// </summary>
        public void LoadAndShowModel(string modelPath, System.Action<string> onComplete = null)
        {
            StartCoroutine(LoadAndShowModelCoroutine(modelPath, onComplete));
        }        private IEnumerator LoadAndShowModelCoroutine(string modelPath, System.Action<string> onComplete)
        {
            OnModelLoadStarted?.Invoke(modelPath);            string modelId = null;
            
            // 步骤1: 加载模型资源
            modelId = ResourceManager.Instance.LoadModel(modelPath);
            if (string.IsNullOrEmpty(modelId))
            {
                Debug.LogError($"UserAction: 加载模型失败 {modelPath}");
                onComplete?.Invoke(null);
                yield break;
            }
            
            yield return new WaitForEndOfFrame(); // 等待资源加载完成
            
            // 步骤2: 在场景中创建演员
            string actorId = SceneDisplayManager.Instance.AddActor(modelId);
            if (string.IsNullOrEmpty(actorId))
            {
                Debug.LogError($"UserAction: 创建演员失败 {modelId}");
                onComplete?.Invoke(null);
                yield break;
            }
            
            yield return new WaitForEndOfFrame(); // 等待演员创建完成
            
            OnModelLoadCompleted?.Invoke(modelId);
            onComplete?.Invoke(modelId);
        }

        /// <summary>
        /// 用户行为：卸载模型并从场景中移除
        /// </summary>
        public void UnloadAndHideModel(string modelId, System.Action onComplete = null)
        {
            StartCoroutine(UnloadAndHideModelCoroutine(modelId, onComplete));
        }        private IEnumerator UnloadAndHideModelCoroutine(string modelId, System.Action onComplete)
        {
            Debug.Log($"UserAction: 开始卸载并隐藏模型 {modelId}");            
            // 步骤1: 清除所有关联
            AssociationManager.Instance.ClearModelAssociations(modelId);
            yield return new WaitForEndOfFrame();
            
            // 步骤2: 从场景中移除演员
            SceneDisplayManager.Instance.RemoveActor(modelId);
            yield return new WaitForEndOfFrame();
            
            // 步骤3: 卸载模型资源
            ResourceManager.Instance.UnloadModel(modelId);
            yield return new WaitForEndOfFrame();
            
            Debug.Log($"UserAction: 模型卸载完成 {modelId}");
            OnModelUnloadCompleted?.Invoke(modelId);
            onComplete?.Invoke();
        }

        // ==================== 动作相关用户行为 ====================
          /// <summary>
        /// 用户行为：加载动作
        /// </summary>
        public void LoadMotion(string motionPath, System.Action<string> onComplete = null)
        {
            StartCoroutine(LoadMotionCoroutine(motionPath, onComplete));
        }        private IEnumerator LoadMotionCoroutine(string motionPath, System.Action<string> onComplete)
        {
            Debug.Log($"UserAction: 开始加载动作 {motionPath}");
            
            string motionId = ResourceManager.Instance.AddMotion(motionPath);
            yield return new WaitForEndOfFrame();
            
            Debug.Log($"UserAction: 动作加载完成 {motionId}");
            OnMotionLoadCompleted?.Invoke(motionId);
            onComplete?.Invoke(motionId);
        }

        /// <summary>
        /// 用户行为：为模型分配动作
        /// </summary>
        public void AssignMotionToModel(string modelId, string motionId, System.Action onComplete = null)
        {
            StartCoroutine(AssignMotionToModelCoroutine(modelId, motionId, onComplete));
        }        private IEnumerator AssignMotionToModelCoroutine(string modelId, string motionId, System.Action onComplete)
        {
            Debug.Log($"UserAction: 为模型 {modelId} 分配动作 {motionId}");
            
            // 建立关联
            AssociationManager.Instance.AssociateModelWithMotion(modelId, motionId);
            yield return new WaitForEndOfFrame();
            
            Debug.Log($"UserAction: 动作分配完成 {modelId} <- {motionId}");
            OnModelMotionAssigned?.Invoke(modelId, motionId);
            onComplete?.Invoke();
        }

        // ==================== 摄像机相关用户行为 ====================
        
        /// <summary>
        /// 用户行为：加载VMD摄像机
        /// </summary>
        public void LoadVMDCamera(string cameraPath, System.Action<string> onComplete = null)
        {
            StartCoroutine(LoadVMDCameraCoroutine(cameraPath, onComplete));
        }        private IEnumerator LoadVMDCameraCoroutine(string cameraPath, System.Action<string> onComplete)
        {
            Debug.Log($"UserAction: 开始加载VMD摄像机 {cameraPath}");
            
            string cameraId = ResourceManager.Instance.AddVMDCamera(cameraPath);
            yield return new WaitForEndOfFrame();
            
            Debug.Log($"UserAction: VMD摄像机加载完成 {cameraId}");
            onComplete?.Invoke(cameraId);
        }

        // ==================== 音乐相关用户行为 ====================
        
        /// <summary>
        /// 用户行为：加载音乐
        /// </summary>
        public void LoadMusic(string musicPath, System.Action<string> onComplete = null)
        {
            StartCoroutine(LoadMusicCoroutine(musicPath, onComplete));
        }        private IEnumerator LoadMusicCoroutine(string musicPath, System.Action<string> onComplete)
        {
            Debug.Log($"UserAction: 开始加载音乐 {musicPath}");
            
            string musicId = ResourceManager.Instance.AddMusic(musicPath);
            yield return new WaitForEndOfFrame();
            
            Debug.Log($"UserAction: 音乐加载完成 {musicId}");
            onComplete?.Invoke(musicId);
        }

        // ==================== 动作管理用户行为 ====================
        
        /// <summary>
        /// 用户行为：卸载动作
        /// </summary>
        public void UnloadMotion(string motionId, System.Action onComplete = null)
        {
            StartCoroutine(UnloadMotionCoroutine(motionId, onComplete));        }        private IEnumerator UnloadMotionCoroutine(string motionId, System.Action onComplete)
        {
            Debug.Log($"UserAction: 开始卸载动作 {motionId}");
            
            // 步骤1: 清除关联层 - 清除所有与该动作相关的关联
            // 注意：这会触发OnModelMotionDisassociated事件，SceneDisplayManager会自动清理表现层
            AssociationManager.Instance.ClearMotionAssociations(motionId);
            yield return new WaitForEndOfFrame();
              
            // 步骤2: 清除数据层 - 卸载动作资源
            ResourceManager.Instance.RemoveMotion(motionId);
            yield return new WaitForEndOfFrame();
            
            Debug.Log($"UserAction: 动作卸载完成 {motionId}");
            onComplete?.Invoke();
        }

        /// <summary>
        /// 用户行为：断开动作的所有关联
        /// </summary>
        public void DisconnectMotionAssociations(string motionId, System.Action onComplete = null)
        {
            StartCoroutine(DisconnectMotionAssociationsCoroutine(motionId, onComplete));
        }        private IEnumerator DisconnectMotionAssociationsCoroutine(string motionId, System.Action onComplete)
        {
            Debug.Log($"UserAction: 断开动作关联 {motionId}");
              AssociationManager.Instance.ClearMotionAssociations(motionId);
            yield return new WaitForEndOfFrame();
            
            Debug.Log($"UserAction: 动作关联已断开 {motionId}");
            onComplete?.Invoke();
        }

        // ==================== 模型管理用户行为 ====================
        
        /// <summary>
        /// 用户行为：切换模型可见性
        /// </summary>
        public void ToggleModelVisibility(string modelId, System.Action onComplete = null)
        {
            StartCoroutine(ToggleModelVisibilityCoroutine(modelId, onComplete));
        }        private IEnumerator ToggleModelVisibilityCoroutine(string modelId, System.Action onComplete)
        {
            Debug.Log($"UserAction: 切换模型可见性 {modelId}");
            
            // 通过SceneDisplayManager切换演员的可见性
            SceneDisplayManager.Instance.ToggleActorVisibility(modelId);
            yield return new WaitForEndOfFrame();
            
            Debug.Log($"UserAction: 模型可见性已切换 {modelId}");
            onComplete?.Invoke();
        }

        /// <summary>
        /// 用户行为：断开模型的所有关联
        /// </summary>
        public void DisconnectModelAssociations(string modelId, System.Action onComplete = null)
        {
            StartCoroutine(DisconnectModelAssociationsCoroutine(modelId, onComplete));
        }        private IEnumerator DisconnectModelAssociationsCoroutine(string modelId, System.Action onComplete)
        {
            Debug.Log($"UserAction: 断开模型关联 {modelId}");            
            AssociationManager.Instance.ClearModelAssociations(modelId);
            yield return new WaitForEndOfFrame();
            
            Debug.Log($"UserAction: 模型关联已断开 {modelId}");
            onComplete?.Invoke();
        }

        // ==================== 音乐管理用户行为 ====================
        
        /// <summary>
        /// 用户行为：卸载音乐
        /// </summary>
        public void UnloadMusic(string musicId, System.Action onComplete = null)
        {
            StartCoroutine(UnloadMusicCoroutine(musicId, onComplete));
        }        private IEnumerator UnloadMusicCoroutine(string musicId, System.Action onComplete)
        {
            Debug.Log($"UserAction: 开始卸载音乐 {musicId}");
              // 如果音乐正在播放，先停止
            if (PlaybackManager.Instance != null && PlaybackManager.Instance.IsPlayingMusic(musicId))
            {
                PlaybackManager.Instance.StopMusic();
            }
            
            ResourceManager.Instance.RemoveMusic(musicId);
            yield return new WaitForEndOfFrame();
            
            Debug.Log($"UserAction: 音乐卸载完成 {musicId}");
            onComplete?.Invoke();
        }

        /// <summary>
        /// 用户行为：激活音乐(设为当前播放音乐)
        /// </summary>
        public void ActivateMusic(string musicId, System.Action onComplete = null)
        {
            StartCoroutine(ActivateMusicCoroutine(musicId, onComplete));
        }        private IEnumerator ActivateMusicCoroutine(string musicId, System.Action onComplete)
        {
            Debug.Log($"UserAction: 激活音乐 {musicId}");            
            if (PlaybackManager.Instance != null)
                PlaybackManager.Instance.SetActiveMusic(musicId);
            yield return new WaitForEndOfFrame();
            
            Debug.Log($"UserAction: 音乐已激活 {musicId}");
            onComplete?.Invoke();
        }

        // ==================== 摄像机管理用户行为 ====================
        
        /// <summary>
        /// 用户行为：卸载摄像机
        /// </summary>
        public void UnloadCamera(string cameraId, System.Action onComplete = null)
        {
            StartCoroutine(UnloadCameraCoroutine(cameraId, onComplete));
        }        private IEnumerator UnloadCameraCoroutine(string cameraId, System.Action onComplete)
        {
            Debug.Log($"UserAction: 开始卸载摄像机 {cameraId}");
            
            ResourceManager.Instance.RemoveCamera(cameraId);
            yield return new WaitForEndOfFrame();
            
            Debug.Log($"UserAction: 摄像机卸载完成 {cameraId}");
            onComplete?.Invoke();
        }

        /// <summary>
        /// 用户行为：激活摄像机(切换到指定摄像机)
        /// </summary>
        public void ActivateCamera(string cameraId, System.Action onComplete = null)
        {
            StartCoroutine(ActivateCameraCoroutine(cameraId, onComplete));
        }        private IEnumerator ActivateCameraCoroutine(string cameraId, System.Action onComplete)
        {
            Debug.Log($"UserAction: 激活摄像机 {cameraId}");
            
            // TODO: 摄像机激活功能需要由专门的摄像机管理器处理
            // ResourceManager.Instance.SetActiveCamera(cameraId);
            Debug.LogWarning("摄像机激活功能暂时禁用，需要实现专门的摄像机管理器");
            yield return new WaitForEndOfFrame();
            
            Debug.Log($"UserAction: 摄像机已激活 {cameraId}");
            onComplete?.Invoke();
        }

        // ==================== 播放控制用户行为 ====================
          /// <summary>
        /// 用户行为：开始播放
        /// </summary>
        public void StartPlayback(System.Action onComplete = null)
        {
            Debug.Log("UserAction: 开始播放");            try
            {
                PlaybackManager.Instance?.Play();
                onComplete?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"UserAction: 开始播放时发生错误: {e.Message}");
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// 用户行为：暂停播放
        /// </summary>
        public void PausePlayback(System.Action onComplete = null)
        {
            Debug.Log("UserAction: 暂停播放");            try
            {
                PlaybackManager.Instance?.Pause();
                onComplete?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"UserAction: 暂停播放时发生错误: {e.Message}");
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// 用户行为：停止播放
        /// </summary>
        public void StopPlayback(System.Action onComplete = null)
        {
            Debug.Log("UserAction: 停止播放");            try
            {
                PlaybackManager.Instance?.Stop();
                onComplete?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"UserAction: 停止播放时发生错误: {e.Message}");
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// 用户行为：跳转到指定时间
        /// </summary>
        public void SeekToTime(float time, System.Action onComplete = null)
        {
            Debug.Log($"UserAction: 跳转到时间 {time}");            try
            {
                PlaybackManager.Instance?.SeekTo(time);
                onComplete?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"UserAction: 跳转时间时发生错误: {e.Message}");
                onComplete?.Invoke();
            }
        }

        // ==================== 组合用户行为 ====================
          /// <summary>
        /// 用户行为：加载模型和动作（不自动关联）
        /// </summary>
        public void LoadModelAndMotion(string modelPath, string motionPath, System.Action<string, string> onComplete = null)
        {
            StartCoroutine(LoadModelAndMotionCoroutine(modelPath, motionPath, onComplete));
        }        private IEnumerator LoadModelAndMotionCoroutine(string modelPath, string motionPath, System.Action<string, string> onComplete)
        {
            string modelId = null;
            string motionId = null;
            
            // 步骤1: 加载并显示模型
            bool modelLoaded = false;
            LoadAndShowModel(modelPath, (id) => 
            {
                modelId = id;
                modelLoaded = true;
            });
            
            yield return new WaitUntil(() => modelLoaded);
            
            if (string.IsNullOrEmpty(modelId))
            {
                Debug.LogError("UserAction: 模型加载失败，取消动作加载");
                onComplete?.Invoke(null, null);
                yield break;
            }
            
            // 步骤2: 加载动作
            bool motionLoaded = false;
            LoadMotion(motionPath, (id) => 
            {
                motionId = id;
                motionLoaded = true;
            });
            
            yield return new WaitUntil(() => motionLoaded);
            
            if (string.IsNullOrEmpty(motionId))
            {
                Debug.LogError("UserAction: 动作加载失败");
                onComplete?.Invoke(modelId, null);
                yield break;
            }
            
            Debug.Log($"UserAction: 模型和动作加载完成 {modelId}, {motionId}");
            Debug.Log("注意: 模型和动作已加载但未关联，请使用 AssignMotionToModel 进行关联");
            onComplete?.Invoke(modelId, motionId);
        }

        // ==================== 场景管理用户行为 ====================
        
        /// <summary>
        /// 用户行为：清空整个场景
        /// </summary>
        public void ClearScene(System.Action onComplete = null)
        {
            StartCoroutine(ClearSceneCoroutine(onComplete));
        }        private IEnumerator ClearSceneCoroutine(System.Action onComplete)
        {
            Debug.Log("UserAction: 开始清空场景");
            
            // 停止播放            PlaybackManager.Instance?.Stop();
            yield return new WaitForEndOfFrame();
              // 清除所有关联
            AssociationManager.Instance.ClearAllAssociations();
            yield return new WaitForEndOfFrame();
              // 清空场景显示
            SceneDisplayManager.Instance.ClearAllDisplayObjects();
            yield return new WaitForEndOfFrame();
            
            // 清空所有资源
            ResourceManager.Instance.ClearAllResources();
            yield return new WaitForEndOfFrame();
              Debug.Log("UserAction: 场景清空完成");
            OnSceneCleared?.Invoke("all");
            onComplete?.Invoke();
        }

        // ==================== 便利方法 ====================
        
        /// <summary>
        /// 获取所有可用的模型列表
        /// </summary>
        public List<ModelComponent> GetAvailableModels()
        {
            return ResourceManager.Instance?.GetModelList() ?? new List<ModelComponent>();
        }

        /// <summary>
        /// 获取所有可用的动作列表
        /// </summary>
        public List<MotionComponent> GetAvailableMotions()
        {
            return ResourceManager.Instance?.GetMotionList() ?? new List<MotionComponent>();
        }        /// <summary>
        /// 获取模型的关联动作
        /// </summary>
        public List<string> GetModelMotions(string modelId)
        {
            return AssociationManager.Instance?.GetModelAssociatedMotions(modelId) ?? new List<string>();
        }    }
}