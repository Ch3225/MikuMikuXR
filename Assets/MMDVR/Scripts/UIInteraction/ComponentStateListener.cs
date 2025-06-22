using UnityEngine;
using System.Collections.Generic;
using MMDVR.Scripts.Components;
using MMDVR.Scripts.Managers;

namespace MMDVR.Scripts.UIInteraction
{
    /// <summary>
    /// 组件状态监听器 - 演示如何正确监听场景对象的状态变化
    /// 
    /// 关键理解：
    /// 1. 存在性（Resources）：由ResourceManager管理，检查容器下是否有对象
    /// 2. 行为状态（Behavior）：由具体组件自己管理，如播放状态、启用状态等
    /// 3. 监听目标：场景中具体对象的属性变化，而不是Manager的抽象事件
    /// </summary>
    public class ComponentStateListener : MonoBehaviour
    {
        [Header("监听配置")]
        [Tooltip("是否启用音乐状态监听")] public bool listenToMusicState = true;
        [Tooltip("是否启用模型状态监听")] public bool listenToModelState = true;
        [Tooltip("是否启用动作状态监听")] public bool listenToMotionState = true;

        [Header("调试显示")]
        public bool enableDebugLog = true;

        private List<MusicComponent> trackedMusicComponents = new List<MusicComponent>();
        private List<ModelComponent> trackedModelComponents = new List<ModelComponent>();
        private List<MotionComponent> trackedMotionComponents = new List<MotionComponent>();

        void Start()
        {
            // 开始监听现有组件
            RefreshComponentTracking();
            
            // 可以定期刷新来捕获新创建的组件
            InvokeRepeating(nameof(RefreshComponentTracking), 2f, 2f);
        }

        void OnDestroy()
        {
            // 清理所有监听
            UnsubscribeFromAllComponents();
        }

        /// <summary>
        /// 刷新组件跟踪 - 检查容器下的新组件并开始监听
        /// </summary>
        void RefreshComponentTracking()
        {
            if (ResourceManager.Instance == null) return;

            // 监听音乐组件状态变化
            if (listenToMusicState)
            {
                TrackMusicComponents();
            }

            // 监听模型组件状态变化
            if (listenToModelState)
            {
                TrackModelComponents();
            }

            // 监听动作组件状态变化
            if (listenToMotionState)
            {
                TrackMotionComponents();
            }
        }

        /// <summary>
        /// 跟踪音乐组件 - 检查musicContainer下的MusicComponent
        /// </summary>
        void TrackMusicComponents()
        {
            if (ResourceManager.Instance.musicContainer == null) return;

            // 查找所有MusicComponent
            var musicComponents = ResourceManager.Instance.musicContainer.GetComponentsInChildren<MusicComponent>();
            
            foreach (var musicComp in musicComponents)
            {
                if (!trackedMusicComponents.Contains(musicComp))
                {
                    // 开始监听这个音乐组件的状态变化
                    musicComp.OnActiveStateChanged += OnMusicActiveStateChanged;
                    trackedMusicComponents.Add(musicComp);
                    
                    if (enableDebugLog)
                        Debug.Log($"[ComponentStateListener] 开始监听音乐组件: {musicComp.displayName}");
                }
            }
        }

        /// <summary>
        /// 跟踪模型组件
        /// </summary>
        void TrackModelComponents()
        {
            if (ResourceManager.Instance.modelContainer == null) return;

            var modelComponents = ResourceManager.Instance.modelContainer.GetComponentsInChildren<ModelComponent>();
            
            foreach (var modelComp in modelComponents)
            {
                if (!trackedModelComponents.Contains(modelComp))
                {
                    // 监听模型的可见性和启用状态变化
                    modelComp.OnVisibilityChanged += OnModelVisibilityChanged;
                    modelComp.OnEnabledStateChanged += OnModelEnabledStateChanged;
                    trackedModelComponents.Add(modelComp);
                    
                    if (enableDebugLog)
                        Debug.Log($"[ComponentStateListener] 开始监听模型组件: {modelComp.displayName}");
                }
            }
        }

        /// <summary>
        /// 跟踪动作组件
        /// </summary>
        void TrackMotionComponents()
        {
            if (ResourceManager.Instance.motionContainer == null) return;

            var motionComponents = ResourceManager.Instance.motionContainer.GetComponentsInChildren<MotionComponent>();
            
            foreach (var motionComp in motionComponents)
            {
                if (!trackedMotionComponents.Contains(motionComp))
                {
                    // 监听动作的播放状态和关联变化
                    motionComp.OnPlayStateChanged += OnMotionPlayStateChanged;
                    motionComp.OnActorAssignmentChanged += OnMotionActorAssignmentChanged;
                    motionComp.OnModelAssignmentChanged += OnMotionModelAssignmentChanged;
                    trackedMotionComponents.Add(motionComp);
                    
                    if (enableDebugLog)
                        Debug.Log($"[ComponentStateListener] 开始监听动作组件: {motionComp.displayName}");
                }
            }
        }

        // ==================== 事件处理方法 ====================

        /// <summary>
        /// 音乐激活状态变化处理
        /// </summary>
        void OnMusicActiveStateChanged(MusicComponent musicComp, bool isActive)
        {
            if (enableDebugLog)
                Debug.Log($"[ComponentStateListener] 音乐 {musicComp.displayName} 播放状态变化: {isActive}");

            // 这里是UI响应音乐状态变化的地方
            // 例如：更新播放按钮状态、进度条等
            UpdateMusicUI(musicComp, isActive);
        }

        /// <summary>
        /// 模型可见性变化处理
        /// </summary>
        void OnModelVisibilityChanged(ModelComponent modelComp, bool isVisible)
        {
            if (enableDebugLog)
                Debug.Log($"[ComponentStateListener] 模型 {modelComp.displayName} 可见性变化: {isVisible}");

            // 这里响应模型可见性变化
            UpdateModelVisibilityUI(modelComp, isVisible);
        }

        /// <summary>
        /// 模型启用状态变化处理
        /// </summary>
        void OnModelEnabledStateChanged(ModelComponent modelComp, bool isEnabled)
        {
            if (enableDebugLog)
                Debug.Log($"[ComponentStateListener] 模型 {modelComp.displayName} 启用状态变化: {isEnabled}");

            UpdateModelEnabledUI(modelComp, isEnabled);
        }

        /// <summary>
        /// 动作播放状态变化处理
        /// </summary>
        void OnMotionPlayStateChanged(MotionComponent motionComp, bool isPlaying)
        {
            if (enableDebugLog)
                Debug.Log($"[ComponentStateListener] 动作 {motionComp.displayName} 播放状态变化: {isPlaying}");

            UpdateMotionPlayUI(motionComp, isPlaying);
        }

        /// <summary>
        /// 动作-Actor关联变化处理
        /// </summary>
        void OnMotionActorAssignmentChanged(MotionComponent motionComp, string oldActorId, string newActorId)
        {
            if (enableDebugLog)
                Debug.Log($"[ComponentStateListener] 动作 {motionComp.displayName} Actor关联变化: {oldActorId} -> {newActorId}");

            UpdateMotionActorAssignmentUI(motionComp, oldActorId, newActorId);
        }

        /// <summary>
        /// 动作-Model关联变化处理
        /// </summary>
        void OnMotionModelAssignmentChanged(MotionComponent motionComp, string oldModelId, string newModelId)
        {
            if (enableDebugLog)
                Debug.Log($"[ComponentStateListener] 动作 {motionComp.displayName} Model关联变化: {oldModelId} -> {newModelId}");

            UpdateMotionModelAssignmentUI(motionComp, oldModelId, newModelId);
        }

        // ==================== UI更新方法 ====================

        /// <summary>
        /// 更新音乐UI - 响应音乐播放状态变化
        /// </summary>
        void UpdateMusicUI(MusicComponent musicComp, bool isActive)
        {
            // 示例：找到对应的UI元素并更新
            // var musicUI = FindUIElementForMusic(musicComp.musicId);
            // if (musicUI != null)
            // {
            //     musicUI.SetPlayButtonState(isActive);
            //     musicUI.UpdateProgressBar(musicComp.GetTime() / musicComp.GetDuration());
            // }
        }

        /// <summary>
        /// 更新模型可见性UI
        /// </summary>
        void UpdateModelVisibilityUI(ModelComponent modelComp, bool isVisible)
        {
            // 示例：更新模型列表中的可见性指示器
            // var modelListItem = FindUIElementForModel(modelComp.modelId);
            // if (modelListItem != null)
            // {
            //     modelListItem.SetVisibilityIndicator(isVisible);
            // }
        }

        /// <summary>
        /// 更新模型启用状态UI
        /// </summary>
        void UpdateModelEnabledUI(ModelComponent modelComp, bool isEnabled)
        {
            // 示例：更新模型的启用/禁用按钮状态
        }

        /// <summary>
        /// 更新动作播放UI
        /// </summary>
        void UpdateMotionPlayUI(MotionComponent motionComp, bool isPlaying)
        {
            // 示例：更新动作的播放指示器
        }

        /// <summary>
        /// 更新动作-Actor关联UI
        /// </summary>
        void UpdateMotionActorAssignmentUI(MotionComponent motionComp, string oldActorId, string newActorId)
        {
            // 示例：更新连线显示、关联指示器等
            if (ConnectionManager.Instance != null)
            {
                if (!string.IsNullOrEmpty(oldActorId))
                    ConnectionManager.Instance.RemoveConnection(oldActorId, motionComp.motionId);
                    
                if (!string.IsNullOrEmpty(newActorId))
                    ConnectionManager.Instance.CreateConnection(newActorId, motionComp.motionId);
            }
        }

        /// <summary>
        /// 更新动作-Model关联UI
        /// </summary>
        void UpdateMotionModelAssignmentUI(MotionComponent motionComp, string oldModelId, string newModelId)
        {
            // 示例：更新模型-动作关联显示
        }

        /// <summary>
        /// 清理所有组件监听
        /// </summary>
        void UnsubscribeFromAllComponents()
        {
            // 取消音乐组件监听
            foreach (var musicComp in trackedMusicComponents)
            {
                if (musicComp != null)
                    musicComp.OnActiveStateChanged -= OnMusicActiveStateChanged;
            }
            trackedMusicComponents.Clear();

            // 取消模型组件监听
            foreach (var modelComp in trackedModelComponents)
            {
                if (modelComp != null)
                {
                    modelComp.OnVisibilityChanged -= OnModelVisibilityChanged;
                    modelComp.OnEnabledStateChanged -= OnModelEnabledStateChanged;
                }
            }
            trackedModelComponents.Clear();

            // 取消动作组件监听
            foreach (var motionComp in trackedMotionComponents)
            {
                if (motionComp != null)
                {
                    motionComp.OnPlayStateChanged -= OnMotionPlayStateChanged;
                    motionComp.OnActorAssignmentChanged -= OnMotionActorAssignmentChanged;
                    motionComp.OnModelAssignmentChanged -= OnMotionModelAssignmentChanged;
                }
            }
            trackedMotionComponents.Clear();
        }
    }
}
