using UnityEngine;
using MMDVR.Scripts.Components;
using System.Collections.Generic;

namespace MMDVR.Scripts.UIInteraction
{
    /// <summary>
    /// 示例：如何正确监听场景对象的属性变化
    /// 这个控制器监听ActorComponent的属性变化，而不是直接监听Manager事件
    /// </summary>
    public class ActorStateUIController : MonoBehaviour
    {
        [Header("UI引用")]
        public Transform actorListParent;
        public GameObject actorUIItemPrefab;
        
        private Dictionary<string, GameObject> actorUIItems = new Dictionary<string, GameObject>();

        void Start()
        {
            // 监听ActorComponent的属性变化事件
            ActorComponent.OnPropertyChanged += HandleActorPropertyChanged;
            ActorComponent.OnActorMotionChanged += HandleActorMotionChanged;
            ActorComponent.OnActorEnabledChanged += HandleActorEnabledChanged;
            
            // 初始化时查找所有现有的Actor
            RefreshActorList();
        }

        void OnDestroy()
        {
            // 重要：取消事件订阅
            ActorComponent.OnPropertyChanged -= HandleActorPropertyChanged;
            ActorComponent.OnActorMotionChanged -= HandleActorMotionChanged;
            ActorComponent.OnActorEnabledChanged -= HandleActorEnabledChanged;
        }

        /// <summary>
        /// 处理Actor属性变化（通用处理）
        /// </summary>
        private void HandleActorPropertyChanged(ActorComponent actor, string propertyName, object oldValue, object newValue)
        {
            Debug.Log($"ActorStateUIController: Actor {actor.ActorId} property {propertyName} changed from {oldValue} to {newValue}");
            
            // 更新对应的UI项
            UpdateActorUIItem(actor);
        }

        /// <summary>
        /// 处理Actor动作变化（专门处理）
        /// </summary>
        private void HandleActorMotionChanged(string actorId, string oldMotionId, string newMotionId)
        {
            Debug.Log($"ActorStateUIController: Actor {actorId} motion changed from {oldMotionId} to {newMotionId}");
            
            // 更新UI中的动作显示
            if (actorUIItems.TryGetValue(actorId, out GameObject uiItem))
            {
                var motionLabel = uiItem.transform.Find("MotionLabel")?.GetComponent<TMPro.TextMeshProUGUI>();
                if (motionLabel != null)
                {
                    motionLabel.text = string.IsNullOrEmpty(newMotionId) ? "无动作" : $"动作: {newMotionId}";
                    motionLabel.color = string.IsNullOrEmpty(newMotionId) ? Color.gray : Color.white;
                }
            }
        }

        /// <summary>
        /// 处理Actor启用状态变化
        /// </summary>
        private void HandleActorEnabledChanged(string actorId, bool isEnabled)
        {
            Debug.Log($"ActorStateUIController: Actor {actorId} enabled state changed to {isEnabled}");
            
            // 更新UI中的启用状态显示
            if (actorUIItems.TryGetValue(actorId, out GameObject uiItem))
            {
                var enabledToggle = uiItem.transform.Find("EnabledToggle")?.GetComponent<UnityEngine.UI.Toggle>();
                if (enabledToggle != null)
                {
                    enabledToggle.isOn = isEnabled;
                }
                
                // 改变UI项的透明度来反映启用状态
                var canvasGroup = uiItem.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = isEnabled ? 1.0f : 0.5f;
                }
            }
        }

        /// <summary>
        /// 刷新Actor列表（查找场景中的所有ActorComponent）
        /// </summary>
        public void RefreshActorList()
        {
            // 清除现有UI项
            foreach (var kvp in actorUIItems)
            {
                if (kvp.Value != null)
                {
                    DestroyImmediate(kvp.Value);
                }
            }
            actorUIItems.Clear();

            // 查找场景中的所有ActorComponent
            ActorComponent[] actors = FindObjectsOfType<ActorComponent>();
            
            foreach (ActorComponent actor in actors)
            {
                CreateActorUIItem(actor);
            }
            
            Debug.Log($"ActorStateUIController: Refreshed actor list, found {actors.Length} actors");
        }

        /// <summary>
        /// 为Actor创建UI项
        /// </summary>
        private void CreateActorUIItem(ActorComponent actor)
        {
            if (actorUIItemPrefab == null || actorListParent == null)
                return;

            GameObject uiItem = Instantiate(actorUIItemPrefab, actorListParent);
            uiItem.name = $"ActorUI_{actor.ActorId}";
            
            // 设置基本信息
            var nameLabel = uiItem.transform.Find("NameLabel")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (nameLabel != null)
            {
                nameLabel.text = actor.ActorId;
            }

            // 设置动作信息
            var motionLabel = uiItem.transform.Find("MotionLabel")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (motionLabel != null)
            {
                motionLabel.text = string.IsNullOrEmpty(actor.AssociatedMotionId) ? "无动作" : $"动作: {actor.AssociatedMotionId}";
                motionLabel.color = string.IsNullOrEmpty(actor.AssociatedMotionId) ? Color.gray : Color.white;
            }

            // 设置启用状态切换
            var enabledToggle = uiItem.transform.Find("EnabledToggle")?.GetComponent<UnityEngine.UI.Toggle>();
            if (enabledToggle != null)
            {
                enabledToggle.isOn = actor.IsEnabled;
                enabledToggle.onValueChanged.AddListener((bool isOn) => {
                    // 直接修改Actor的属性，这会触发属性变化事件
                    actor.IsEnabled = isOn;
                });
            }

            // 添加UI项到字典
            actorUIItems[actor.ActorId] = uiItem;
        }

        /// <summary>
        /// 更新特定Actor的UI项
        /// </summary>
        private void UpdateActorUIItem(ActorComponent actor)
        {
            if (actorUIItems.TryGetValue(actor.ActorId, out GameObject uiItem))
            {
                // 更新动作显示
                var motionLabel = uiItem.transform.Find("MotionLabel")?.GetComponent<TMPro.TextMeshProUGUI>();
                if (motionLabel != null)
                {
                    motionLabel.text = string.IsNullOrEmpty(actor.AssociatedMotionId) ? "无动作" : $"动作: {actor.AssociatedMotionId}";
                    motionLabel.color = string.IsNullOrEmpty(actor.AssociatedMotionId) ? Color.gray : Color.white;
                }

                // 更新启用状态
                var enabledToggle = uiItem.transform.Find("EnabledToggle")?.GetComponent<UnityEngine.UI.Toggle>();
                if (enabledToggle != null)
                {
                    enabledToggle.SetIsOnWithoutNotify(actor.IsEnabled); // 避免循环触发
                }

                // 更新透明度
                var canvasGroup = uiItem.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = actor.IsEnabled ? 1.0f : 0.5f;
                }
            }
        }
    }
}
