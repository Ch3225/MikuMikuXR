using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events; // Required for UnityEvent
using UnityEngine.UI; // Required for LayoutGroup components
using MMDVR.Scripts.Model; // For ResourceType and IResourceInfo
using System.Collections; // Required for IEnumerator and Coroutines
using System.Collections.Generic; // Added for List<>
using System.Linq; // Added for LINQ's Select method

namespace MMDVR.Scripts.UIInteraction.ResourceManagement
{
    // Define a UnityEvent that can pass a GameObject (the dropped item)
    [System.Serializable]
    public class GameObjectUnityEvent : UnityEvent<GameObject> { }

    public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler    {
        // NEW: Enum to define the action this drop zone performs
        public enum DropActionType
        {
            None,
            PanelUninstall,               // [Panel] Uninstall - For uninstalling any resource type
            MotionLinkToModel,            // [Motion] Link to Model - Dragging a motion onto a model item
            ModelLinkToMotion,            // [Model] Link to Motion - Dragging a model onto a motion item
            PanelToggle,                  // [Panel] Toggle - Enable/Disable models, Activate music/camera
            PanelDisconnect,              // [Panel] Disconnect - Disconnect all associations
        }
        public DropActionType actionType = DropActionType.None;        [Header("Accepted Resource Types")]
        [Tooltip("Which resource types can be dropped onto this zone. Leave empty to accept all.")]
        public List<ResourceType> acceptedResourceTypes = new List<ResourceType>();

        // Optional: For visual feedback
        private UnityEngine.UI.Image backgroundImage; 
        private Color originalColor;
        public Color highlightColor = Color.yellow;
        
        // Event to be configured in the Inspector, e.g., to call a method on MusicListController
        public GameObjectUnityEvent onItemDropped;        void Awake()
        {
            // 确保onItemDropped事件已初始化
            if (onItemDropped == null)
            {
                onItemDropped = new GameObjectUnityEvent();
            }
            
            backgroundImage = GetComponent<UnityEngine.UI.Image>();
            if (backgroundImage != null)
            {
                originalColor = backgroundImage.color;
            }
        }        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;
            DraggableItem draggable = eventData.pointerDrag.GetComponent<DraggableItem>();

            if (draggable != null && IsAccepted(draggable.Data))
            {
                // 常规高亮
                if (backgroundImage != null)
                {
                    backgroundImage.color = highlightColor;
                }
                
                Debug.Log($"[DropZone {gameObject.name}] OnPointerEnter: {draggable.name} (Type: {draggable.Data?.Type}) entered. Accepted. Highlighting.");
            }
            else if (draggable != null) // Draggable but not accepted
            {
                Debug.Log($"[DropZone {gameObject.name}] OnPointerEnter: {draggable.name} (Type: {draggable.Data?.Type}) entered. Rejected. Not highlighting.");
                if (backgroundImage != null && backgroundImage.color == highlightColor) 
                {
                     backgroundImage.color = originalColor;
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 恢复常规高亮
            if (backgroundImage != null && backgroundImage.color == highlightColor)
            {
                backgroundImage.color = originalColor;
            }
            
            if (eventData.pointerDrag != null) 
            {
                 DraggableItem draggable = eventData.pointerDrag.GetComponent<DraggableItem>();
                 if (draggable != null) 
                 {
                    Debug.Log($"[DropZone {gameObject.name}] OnPointerExit: {draggable.name} exited.");
                 }
            } 
        }        public void OnDrop(PointerEventData eventData)
        {
            DraggableItem draggable = eventData.pointerDrag.GetComponent<DraggableItem>();
            if (draggable != null) 
            {
                bool canAccept = IsAccepted(draggable.Data); 
                Debug.Log($"[DropZone {gameObject.name}] OnDrop: Draggable '{draggable.name}' (Type: {draggable.Data?.Type}), IsAccepted returned: {canAccept}");

                if (canAccept)
                {
                    Debug.Log($"[DropZone {gameObject.name}] OnDrop: Drop ACCEPTED for {draggable.name}. Invoking onItemDropped.");
                    if (backgroundImage != null)
                    {
                        backgroundImage.color = originalColor; 
                    }
                    
                    // 执行拖拽操作
                    onItemDropped.Invoke(eventData.pointerDrag);
                    
                    // 在拖拽操作完成后触发UI刷新
                    StartCoroutine(DelayedRefreshAfterDrop());
                }
                else
                {
                    Debug.Log($"[DropZone {gameObject.name}] OnDrop: Drop REJECTED for {draggable.name}. Accepted types: [{string.Join(", ", acceptedResourceTypes)}]");
                    if (backgroundImage != null)
                    {
                        backgroundImage.color = originalColor;
                    }
                }
            }
            else 
            {
                Debug.LogWarning($"[DropZone {gameObject.name}] OnDrop: eventData.pointerDrag has no DraggableItem component.");
                if (backgroundImage != null)
                {
                    backgroundImage.color = originalColor;
                }
            }
        }

        private bool IsAccepted(IResourceInfo resourceInfo)
        {
            if (resourceInfo == null)
            {
                Debug.Log($"[DropZone {gameObject.name}] IsAccepted: resourceInfo is null. Returning false.");
                return false;
            }

            string acceptedTypesString = acceptedResourceTypes.Count == 0 ? "ALL (list empty)" : string.Join(", ", acceptedResourceTypes.Select(rt => rt.ToString()).ToArray());
            Debug.Log($"[DropZone {gameObject.name}] IsAccepted: Checking item type '{resourceInfo.Type}' against accepted types: [{acceptedTypesString}]. List count: {acceptedResourceTypes.Count}");

            if (acceptedResourceTypes.Count == 0)
            {
                Debug.Log($"[DropZone {gameObject.name}] IsAccepted: Accepted list is empty, accepting all. Returning true.");
                return true; 
            }

            bool isContained = acceptedResourceTypes.Contains(resourceInfo.Type);
            Debug.Log($"[DropZone {gameObject.name}] IsAccepted: Item type '{resourceInfo.Type}' {(isContained ? "IS" : "IS NOT")} in accepted list. Returning {isContained}.");
            return isContained;
        }

        /// <summary>
        /// 拖拽操作完成后延迟刷新UI
        /// </summary>
        private IEnumerator DelayedRefreshAfterDrop()
        {
            // 等待一帧，确保所有拖拽相关的操作都完成
            yield return new WaitForEndOfFrame();
            
            // 使用静态方法触发刷新，处理MainControlPanel可能未激活的情况
            MainControlPanelManager.TriggerGlobalLayoutRefresh();
            Debug.Log($"[DropZone {gameObject.name}] 触发MainControlPanel刷新");
        }

        // ==================== Existing Methods ====================
    }
}
