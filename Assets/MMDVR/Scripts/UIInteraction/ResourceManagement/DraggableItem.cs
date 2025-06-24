using UnityEngine;
using UnityEngine.EventSystems;
using MMDVR.Scripts.Model; // For IResourceInfo

namespace MMDVR.Scripts.UIInteraction.ResourceManagement
{
    public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public IResourceInfo Data { get; set; }
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Transform originalParent;
        private int originalSiblingIndex;
        private Canvas canvas;
        private Vector3 offsetToMouse;

        [Header("Drag Configuration")]
        [Tooltip("Assign the dedicated UI layer for dragged items. If not assigned, will try to find 'Canvas/MainUI/DragLayerPanel'.")]
        public Transform explicitDragLayer;

        private Transform dragLayerToUse;

        private GameObject dragGhostInstance;
        private CanvasGroup dragGhostCanvasGroup;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            canvas = GetComponentInParent<Canvas>();

            if (explicitDragLayer != null)
            {
                dragLayerToUse = explicitDragLayer;
                // Debug.Log("DraggableItem: Using explicitDragLayer: " + dragLayerToUse.name, this);
            }
            else if (canvas != null)
            {
                // Try to find MainUI first, then DragLayerPanel within MainUI
                // This assumes MainUI is a direct child of the Canvas, or at least a known structure.
                // You might need to adjust this path if your hierarchy is different.
                Transform mainUiTransform = null;
                // Check if the canvas itself is named MainUI or if MainUI is a child
                if (canvas.name == "MainUI") { // Or whatever your MainUI root is named if it's the canvas panel itself
                     mainUiTransform = canvas.transform;
                } else {
                     mainUiTransform = canvas.transform.Find("MainUI"); // Common case: Canvas -> MainUI -> DragLayerPanel
                }
                
                if (mainUiTransform != null)
                {
                    Transform foundDragLayer = mainUiTransform.Find("DragLayerPanel");
                    if (foundDragLayer != null)
                    {
                        dragLayerToUse = foundDragLayer;
                        // Debug.Log("DraggableItem: Found DragLayerPanel at Canvas/MainUI/DragLayerPanel: " + dragLayerToUse.name, this);
                    }
                    else
                    {
                        // Debug.LogWarning("DraggableItem: Could not find 'DragLayerPanel' under 'MainUI'.", this);
                    }
                }
                else
                {
                    // Debug.LogWarning("DraggableItem: Could not find 'MainUI' under the Canvas.", this);
                }
            }

            if (dragLayerToUse == null && canvas != null)
            {
                Debug.LogWarning("DraggableItem: DragLayerPanel not found via explicit assignment or standard search (Canvas/MainUI/DragLayerPanel). Falling back to Canvas root. This might cause layout issues if Canvas root has a LayoutGroup.", this);
                dragLayerToUse = canvas.transform; // Fallback
            }
            else if (dragLayerToUse == null && canvas == null)
            {
                 Debug.LogError("DraggableItem: Canvas not found. Dragging will not work correctly.", this);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (rectTransform == null || canvas == null || dragLayerToUse == null)
            {
                return;
            }
            // 创建副本（Ghost）
            dragGhostInstance = Instantiate(gameObject, dragLayerToUse);
            dragGhostInstance.name = gameObject.name + "_DragGhost";
            // 移除副本上的 DraggableItem 组件，避免递归拖拽
            var ghostDraggable = dragGhostInstance.GetComponent<DraggableItem>();
            if (ghostDraggable != null) Destroy(ghostDraggable);
            // 设置半透明
            dragGhostCanvasGroup = dragGhostInstance.GetComponent<CanvasGroup>();
            if (dragGhostCanvasGroup == null) dragGhostCanvasGroup = dragGhostInstance.AddComponent<CanvasGroup>();
            dragGhostCanvasGroup.alpha = 0.5f;
            dragGhostCanvasGroup.blocksRaycasts = false;
            // 跟随鼠标
            RectTransform ghostRect = dragGhostInstance.GetComponent<RectTransform>();
            Vector3 globalMousePos;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out globalMousePos))
            {
                ghostRect.position = globalMousePos;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragGhostInstance == null || canvas == null) return;
            RectTransform ghostRect = dragGhostInstance.GetComponent<RectTransform>();
            Vector3 globalMousePos;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out globalMousePos))
            {
                ghostRect.position = globalMousePos;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragGhostInstance != null)
            {
                Destroy(dragGhostInstance);
                dragGhostInstance = null;
            }
        }
    }
}
