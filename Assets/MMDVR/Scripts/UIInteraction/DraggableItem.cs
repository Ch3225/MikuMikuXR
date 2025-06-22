using UnityEngine;
using UnityEngine.EventSystems;
using MMDVR.Scripts.Model; // For IResourceInfo

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
            // Debug.LogError("DraggableItem: Missing references (rectTransform, canvas, or dragLayerToUse). Cannot begin drag.", this);
            return;
        }

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        Vector3 globalMousePos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out globalMousePos))
        {
            offsetToMouse = rectTransform.position - globalMousePos;
        }
        else
        {
            offsetToMouse = Vector3.zero;
        }

        transform.SetParent(dragLayerToUse, true);
        transform.SetAsLastSibling();

        Vector3 newPos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out newPos))
        {
            rectTransform.position = newPos + offsetToMouse;
        }

        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null || canvas == null || dragLayerToUse == null) return;

        Vector3 globalMousePos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out globalMousePos))
        {
            rectTransform.position = globalMousePos + offsetToMouse;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (rectTransform == null || canvas == null || dragLayerToUse == null) return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (transform.parent == dragLayerToUse)
        {
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalSiblingIndex);
        }
        offsetToMouse = Vector3.zero;
    }
}
