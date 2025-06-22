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
            ListSortAndActivate,          // [List] Sort & Activate - For list sorting and activating items
            PanelUninstall,               // [Panel] Uninstall - For uninstalling any resource type
            MotionLinkToModel,            // [Motion] Link to Model - Dragging a motion onto a model item
            ModelLinkToMotion,            // [Model] Link to Motion - Dragging a model onto a motion item
            PanelToggle,                  // [Panel] Toggle - Enable/Disable models, Activate music/camera
            PanelDisconnect,              // [Panel] Disconnect - Disconnect all associations
            ListListSort                  // [List] ListSort - For sorting within lists
        }
        public DropActionType actionType = DropActionType.None;        [Header("Accepted Resource Types")]
        [Tooltip("Which resource types can be dropped onto this zone. Leave empty to accept all.")]
        public List<ResourceType> acceptedResourceTypes = new List<ResourceType>();

        [Header("List Insertion Preview")]
        [Tooltip("Prefab to show insertion preview for List-type actions")]
        public GameObject insertionPreviewPrefab;

        // Optional: For visual feedback
        private UnityEngine.UI.Image backgroundImage; 
        private Color originalColor;
        public Color highlightColor = Color.yellow;
        
        // List insertion preview
        private GameObject currentPreviewInstance;
        private Coroutine updatePreviewCoroutine;

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
                  // 如果是List类型的ActionType，显示插入预览
                if (IsListActionType() && insertionPreviewPrefab != null)
                {
                    ShowInsertionPreview();
                    // 开始实时更新预览位置
                    if (updatePreviewCoroutine == null)
                    {
                        updatePreviewCoroutine = StartCoroutine(UpdateInsertionPreviewCoroutine());
                    }
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
            
            // 隐藏插入预览
            HideInsertionPreview();
            
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
                    
                    // 隐藏插入预览
                    HideInsertionPreview();
                    
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
                    // 隐藏插入预览
                    HideInsertionPreview();
                }
            }
            else 
            {
                Debug.LogWarning($"[DropZone {gameObject.name}] OnDrop: eventData.pointerDrag has no DraggableItem component.");
                if (backgroundImage != null)
                {
                    backgroundImage.color = originalColor;
                }
                // 隐藏插入预览
                HideInsertionPreview();
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

        // ==================== List Insertion Preview Methods ====================
        
        /// <summary>
        /// 检查当前ActionType是否是List类型
        /// </summary>
        private bool IsListActionType()
        {
            return actionType == DropActionType.ListSortAndActivate || 
                   actionType == DropActionType.ListListSort;
        }
          /// <summary>
        /// 显示插入预览（根据鼠标位置智能选择插入位置）
        /// </summary>
        private void ShowInsertionPreview()
        {
            if (insertionPreviewPrefab == null) return;

            // 隐藏旧的预览
            HideInsertionPreview();
            
            // 查找列表容器（通常是父级或兄弟级的容器）
            Transform listContainer = FindListContainer();
            if (listContainer == null) return;

            // 获取鼠标位置
            Vector2 mousePosition = Input.mousePosition;
            
            // 找到最佳插入位置
            int insertionIndex = FindBestInsertionIndex(listContainer, mousePosition);
            
            // 创建预览对象
            currentPreviewInstance = Instantiate(insertionPreviewPrefab);
            
            // 将预览对象设置为列表容器的子对象，并调整位置
            currentPreviewInstance.transform.SetParent(listContainer, false);
            currentPreviewInstance.transform.SetSiblingIndex(insertionIndex);
            
            Debug.Log($"[DropZone {gameObject.name}] 在索引 {insertionIndex} 显示插入预览");
        }
        
        /// <summary>
        /// 查找列表容器
        /// </summary>
        private Transform FindListContainer()
        {
            // 首先尝试查找父级中是否有LayoutGroup组件
            Transform current = transform.parent;
            while (current != null)
            {
                if (current.GetComponent<LayoutGroup>() != null)
                {
                    return current;
                }
                current = current.parent;
            }
            
            // 如果没找到，尝试查找兄弟级对象中的列表容器
            if (transform.parent != null)
            {
                for (int i = 0; i < transform.parent.childCount; i++)
                {
                    Transform sibling = transform.parent.GetChild(i);
                    if (sibling.GetComponent<LayoutGroup>() != null)
                    {
                        return sibling;
                    }
                }
            }
            
            return null;
        }
          /// <summary>
        /// 根据鼠标位置找到最佳插入索引
        /// </summary>
        private int FindBestInsertionIndex(Transform listContainer, Vector2 mousePosition)
        {
            int bestIndex = listContainer.childCount; // 默认插入到末尾
            
            // 将鼠标屏幕坐标转换为Canvas坐标
            Canvas canvas = listContainer.GetComponentInParent<Canvas>();
            if (canvas == null) return bestIndex;
            
            Camera camera = canvas.worldCamera ?? Camera.main;
            if (camera == null) 
            {
                camera = FindObjectOfType<Camera>();
            }
            if (camera == null) return bestIndex;
            
            RectTransform containerRect = listContainer.GetComponent<RectTransform>();
            if (containerRect == null) return bestIndex;
            
            // 将鼠标位置转换为容器的本地坐标
            Vector2 localMousePos;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                containerRect, mousePosition, camera, out localMousePos))
            {
                return bestIndex;
            }
            
            // 获取布局方向
            LayoutGroup layoutGroup = listContainer.GetComponent<LayoutGroup>();
            bool isVertical = layoutGroup is VerticalLayoutGroup;
            
            float closestDistance = float.MaxValue;
            int closestIndex = bestIndex;
            
            // 遍历所有子项，找到最接近的位置
            for (int i = 0; i < listContainer.childCount; i++)
            {
                Transform child = listContainer.GetChild(i);
                RectTransform childRect = child.GetComponent<RectTransform>();
                if (childRect == null) continue;
                
                // 获取子项在容器中的本地位置
                Vector3 childLocalPos = containerRect.InverseTransformPoint(childRect.position);
                
                // 计算距离和插入位置
                float distance;
                bool insertBefore;
                
                if (isVertical)
                {
                    // 垂直布局：比较Y坐标
                    distance = Mathf.Abs(localMousePos.y - childLocalPos.y);
                    insertBefore = localMousePos.y > childLocalPos.y; // Unity UI中Y轴向上为正
                }
                else
                {
                    // 水平布局：比较X坐标
                    distance = Mathf.Abs(localMousePos.x - childLocalPos.x);
                    insertBefore = localMousePos.x < childLocalPos.x; // X轴向右为正
                }
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = insertBefore ? i : i + 1;
                }
            }
            
            // 如果没有子项，或者鼠标在所有子项之外，检查是否应该插入到开头或结尾
            if (listContainer.childCount > 0)
            {
                RectTransform firstChild = listContainer.GetChild(0).GetComponent<RectTransform>();
                RectTransform lastChild = listContainer.GetChild(listContainer.childCount - 1).GetComponent<RectTransform>();
                
                if (firstChild != null && lastChild != null)
                {
                    Vector3 firstPos = containerRect.InverseTransformPoint(firstChild.position);
                    Vector3 lastPos = containerRect.InverseTransformPoint(lastChild.position);
                    
                    if (isVertical)
                    {
                        if (localMousePos.y > firstPos.y + firstChild.rect.height * 0.5f)
                        {
                            closestIndex = 0; // 插入到开头
                        }
                        else if (localMousePos.y < lastPos.y - lastChild.rect.height * 0.5f)
                        {
                            closestIndex = listContainer.childCount; // 插入到结尾
                        }
                    }
                    else
                    {
                        if (localMousePos.x < firstPos.x - firstChild.rect.width * 0.5f)
                        {
                            closestIndex = 0; // 插入到开头
                        }
                        else if (localMousePos.x > lastPos.x + lastChild.rect.width * 0.5f)
                        {
                            closestIndex = listContainer.childCount; // 插入到结尾
                        }
                    }
                }
            }
            
            int finalIndex = Mathf.Clamp(closestIndex, 0, listContainer.childCount);
            Debug.Log($"[DropZone {gameObject.name}] 鼠标位置: {localMousePos}, 插入索引: {finalIndex}");
            
            return finalIndex;
        }
        
        /// <summary>
        /// 隐藏插入预览
        /// </summary>
        private void HideInsertionPreview()
        {
            // 停止实时更新协程
            if (updatePreviewCoroutine != null)
            {
                StopCoroutine(updatePreviewCoroutine);
                updatePreviewCoroutine = null;
            }
            
            if (currentPreviewInstance != null)
            {
                Destroy(currentPreviewInstance);
                currentPreviewInstance = null;
                Debug.Log($"[DropZone {gameObject.name}] 隐藏插入预览");
            }
        }

        /// <summary>
        /// 实时更新插入预览位置的协程
        /// </summary>
        private IEnumerator UpdateInsertionPreviewCoroutine()
        {
            while (currentPreviewInstance != null)
            {
                // 获取当前鼠标位置
                Vector2 mousePosition = Input.mousePosition;
                
                // 查找列表容器
                Transform listContainer = FindListContainer();
                if (listContainer != null)
                {
                    // 找到最佳插入位置
                    int insertionIndex = FindBestInsertionIndex(listContainer, mousePosition);
                    
                    // 更新预览对象位置
                    if (currentPreviewInstance != null && currentPreviewInstance.transform.GetSiblingIndex() != insertionIndex)
                    {
                        currentPreviewInstance.transform.SetSiblingIndex(insertionIndex);
                    }
                }
                
                yield return new WaitForSeconds(0.05f); // 每50ms更新一次，保持流畅但不过于频繁
            }
        }        /// <summary>
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
