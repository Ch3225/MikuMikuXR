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

        // 用户行为事件全部移除，统一用 ResourceEvents/SceneDisplayEvents 分发

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        // ==================== 模型相关用户行为 ====================
        /// <summary>
        /// 加载模型并在场景中显示，返回演员ID
        /// </summary>
        public void LoadAndShowModel(string modelPath, System.Action<string> onComplete)
        {
            StartCoroutine(LoadAndShowModelCoroutine(modelPath, onComplete));
        }
        private IEnumerator LoadAndShowModelCoroutine(string modelPath, System.Action<string> onComplete)
        {
            MMDVR.Events.ResourceEvents.TriggerModelLoadRequest(modelPath);
            string modelId = null;
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
            // 统一用 ResourceEvents 分发加载完成
            MMDVR.Events.ResourceEvents.TriggerResourceLoaded("model", modelId);
            onComplete?.Invoke(actorId); // 返回演员ID
        }

        /// <summary>
        /// 用户行为：卸载模型并从场景中移除
        /// </summary>
        public void UnloadModel(string modelId, System.Action onComplete = null)
        {
            StartCoroutine(UnloadModelWithUnlinkCoroutine(modelId, onComplete));
        }
        private IEnumerator UnloadModelWithUnlinkCoroutine(string modelId, System.Action onComplete)
        {
            AssociationManager.Instance.ClearModelAssociations(modelId);
            yield return new WaitForEndOfFrame();
            SceneDisplayManager.Instance.RemoveActor(modelId);
            yield return new WaitForEndOfFrame();
            ResourceManager.Instance.UnloadModel(modelId);
            yield return new WaitForEndOfFrame();
            onComplete?.Invoke();
        }

        // ==================== 动作相关用户行为 ====================
        /// <summary>
        /// 加载动作，返回动作ID
        /// </summary>
        public void LoadMotion(string motionPath, System.Action<string> onComplete)
        {
            StartCoroutine(LoadMotionCoroutine(motionPath, onComplete));
        }
        private IEnumerator LoadMotionCoroutine(string motionPath, System.Action<string> onComplete)
        {
            Debug.Log($"UserAction: 开始加载动作 {motionPath}");
            string motionId = ResourceManager.Instance.AddMotion(motionPath);
            yield return new WaitForEndOfFrame();
            Debug.Log($"UserAction: 动作加载完成 {motionId}");
            MMDVR.Events.ResourceEvents.TriggerResourceLoaded("motion", motionId);
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
            MMDVR.Events.SceneDisplayEvents.TriggerModelMotionAssociationChanged(modelId, motionId, true);
            onComplete?.Invoke();
        }

        // ==================== 摄像机相关用户行为 ====================
        
        /// <summary>
        /// 加载相机，返回相机ID
        /// </summary>
        public void LoadVMDCamera(string cameraPath, System.Action<string> onComplete)
        {
            StartCoroutine(LoadVMDCameraCoroutine(cameraPath, onComplete));
        }
        private IEnumerator LoadVMDCameraCoroutine(string cameraPath, System.Action<string> onComplete)
        {
            Debug.Log($"UserAction: 开始加载VMD摄像机 {cameraPath}");
            string cameraId = ResourceManager.Instance.AddVMDCamera(cameraPath);
            yield return new WaitForEndOfFrame();
            Debug.Log($"UserAction: VMD摄像机加载完成 {cameraId}");
            onComplete?.Invoke(cameraId);
        }

        // ==================== 音乐相关用户行为 ====================
        
        /// <summary>
        /// 加载音乐并自动设为活动项
        /// </summary>
        public void LoadMusic(string musicPath, System.Action<string> onComplete)
        {
            StartCoroutine(LoadMusicAndActivateCoroutine(musicPath, onComplete));
        }
        private IEnumerator LoadMusicAndActivateCoroutine(string musicPath, System.Action<string> onComplete)
        {
            Debug.Log($"UserAction: 开始加载音乐 {musicPath}");
            string musicId = ResourceManager.Instance.AddMusic(musicPath);
            yield return new WaitForEndOfFrame();
            Debug.Log($"UserAction: 音乐加载完成 {musicId}");
            ActivateMusic(musicId, () => {
                onComplete?.Invoke(musicId);
            });
        }

        // ==================== 动作管理用户行为 ====================
        
        /// <summary>
        /// 卸载动作（先断开所有关联再卸载）
        /// </summary>
        public void UnloadMotion(string motionId, System.Action onComplete = null)
        {
            StartCoroutine(UnloadMotionWithUnlinkCoroutine(motionId, onComplete));
        }
        private IEnumerator UnloadMotionWithUnlinkCoroutine(string motionId, System.Action onComplete)
        {
            AssociationManager.Instance.ClearMotionAssociations(motionId);
            yield return new WaitForEndOfFrame();
            ResourceManager.Instance.RemoveMotion(motionId);
            yield return new WaitForEndOfFrame();
            onComplete?.Invoke();
        }

        // ==================== 音乐管理用户行为 ====================
        
        /// <summary>
        /// 加载音乐并自动设为活动项
        /// </summary>
        // 以下为重复定义，已注释以修复 CS0111 错误
        // public void LoadMusic(string musicPath, System.Action<string> onComplete)
        // {
        //     StartCoroutine(LoadMusicAndActivateCoroutine(musicPath, onComplete));
        // }
        // private IEnumerator LoadMusicAndActivateCoroutine(string musicPath, System.Action<string> onComplete)
        // {
        //     Debug.Log($"UserAction: 开始加载音乐 {musicPath}");
        //     string musicId = ResourceManager.Instance.AddMusic(musicPath);
        //     yield return new WaitForEndOfFrame();
        //     Debug.Log($"UserAction: 音乐加载完成 {musicId}");
        //     ActivateMusic(musicId, () => {
        //         onComplete?.Invoke(musicId);
        //     });
        // }

        /// <summary>
        /// 激活音乐（设为当前播放项）
        /// </summary>
        public void ActivateMusic(string musicId, System.Action onComplete = null)
        {
            // 只通过 SceneDisplayManager/PlaybackManager 激活
            SceneDisplayManager.Instance?.SetActiveMusic(musicId);
            PlaybackManager.Instance?.SetActiveMusic(musicId);
            // 触发事件驱动UI刷新
            MMDVR.Events.ResourceEvents.TriggerMusicActivated(musicId);
            onComplete?.Invoke();
        }

        /// <summary>
        /// 卸载音乐（如为首选项需切换）
        /// </summary>
        public void UnloadMusic(string musicId, System.Action onComplete = null)
        {
            StartCoroutine(UnloadMusicWithCheckCoroutine(musicId, onComplete));
        }
        private IEnumerator UnloadMusicWithCheckCoroutine(string musicId, System.Action onComplete)
        {
            // 检查是否为激活音乐，若是则切换到下一个
            string currentActive = SceneDisplayManager.Instance?.GetCurrentActiveMusicId();
            ResourceManager.Instance.RemoveMusic(musicId);
            yield return new WaitForEndOfFrame();
            if (currentActive == musicId)
            {
                // 自动激活下一个音乐
                var musicList = ResourceManager.Instance.GetMusicList();
                if (musicList.Count > 0)
                {
                    ActivateMusic(musicList[0].id);
                }
                else
                {
                    ActivateMusic(null); // 无音乐可激活
                }
            }
            // 触发事件驱动UI刷新
            MMDVR.Events.ResourceEvents.TriggerResourceUnloaded("Music", musicId);
            onComplete?.Invoke();
        }
        // ==================== 相机管理用户行为 ====================
        /// <summary>
        /// 卸载相机（如为首选项需切换到自由相机）
        /// </summary>
        public void UnloadCamera(string cameraId, System.Action onComplete = null)
        {
            StartCoroutine(UnloadCameraWithCheckCoroutine(cameraId, onComplete));
        }
        private IEnumerator UnloadCameraWithCheckCoroutine(string cameraId, System.Action onComplete)
        {
            // TODO: 检查是否为激活相机并切换，需实现CameraManager的相关方法
            ResourceManager.Instance.RemoveCamera(cameraId);
            yield return new WaitForEndOfFrame();
            onComplete?.Invoke();
        }

        // ==================== 关联相关用户行为 ====================
        /// <summary>
        /// 关联动作到演员
        /// </summary>
        public void LinkMotionToActor(string actorId, string motionId, System.Action onComplete = null)
        {
            AssociationManager.Instance.AssociateModelWithMotion(actorId, motionId);
            onComplete?.Invoke();
        }
        /// <summary>
        /// 解除演员的所有动作关联
        /// </summary>
        public void UnlinkAllMotionsFromActor(string actorId, System.Action onComplete = null)
        {
            AssociationManager.Instance.ClearModelAssociations(actorId);
            onComplete?.Invoke();
        }
        /// <summary>
        /// 解除动作与所有演员的关联
        /// </summary>
        public void UnlinkMotionFromAllActors(string motionId, System.Action onComplete = null)
        {
            AssociationManager.Instance.ClearMotionAssociations(motionId);
            onComplete?.Invoke();
        }

        // ==================== 播放控制相关用户行为 ====================
        public void StartPlayback(System.Action onComplete = null)
        {
            PlaybackManager.Instance?.Play();
            onComplete?.Invoke();
        }
        public void PausePlayback(System.Action onComplete = null)
        {
            PlaybackManager.Instance?.Pause();
            onComplete?.Invoke();
        }
        public void SeekToTime(float time, System.Action onComplete = null)
        {
            PlaybackManager.Instance?.SeekTo(time);
            onComplete?.Invoke();
        }
        public void SetVolume(float volume, System.Action onComplete = null)
        {
            PlaybackManager.Instance?.SetMusicVolume(volume);
            onComplete?.Invoke();
        }
        public void SetMute(bool mute, System.Action onComplete = null)
        {
            PlaybackManager.Instance?.SetMusicMute(mute);
            onComplete?.Invoke();
        }
    }
}