using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events; // Required for UnityEvent
using MMDVR.Scripts.UIInteraction; // Added for ResourceType

// Define a UnityEvent that can pass a GameObject (the dropped item)
[System.Serializable]
public class GameObjectUnityEvent : UnityEvent<GameObject> { }

public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    // NEW: Enum to define the action this drop zone performs
    public enum DropActionType
    {
        None,
        SortAndActivate,          // For list sorting and activating the first item
        Uninstall,                // For uninstalling any resource type
        LinkToModel,              // Example: Dragging a motion onto a model item
        LinkToMotion,             // Example: Dragging a model onto a motion item
        // Add other specific actions as needed
        ModelEnable, 
        ModelDisable,
        ModelDisconnectMotion
    }
    public DropActionType actionType = DropActionType.None; // Assign in Inspector

    // OLD DropZoneType enum - can be removed or refactored if actionType covers all cases
    // public enum DropZoneType
    // {
    //     MusicListSortableArea, 
    //     MusicListActivationArea, 
    //     MusicUninstallAction,
    //     ModelDropOnMotion,
    //     MotionDropOnModel,
    //     ModelEnableAction,
    //     ModelDisableAction,
    //     ModelMotionDisconnectAction,
    //     ModelUninstallAction,
    //     MotionUninstallAction,
    //     CameraListSortableArea,
    //     CameraListActivationArea,
    //     CameraUninstallAction,
    //     GenericUninstallAction
    // }
    // public DropZoneType zoneType; // This can be replaced by actionType

    // Optional: For visual feedback
    private UnityEngine.UI.Image backgroundImage; 
    private Color originalColor;
    public Color highlightColor = Color.yellow;

    // Event to be configured in the Inspector, e.g., to call a method on MusicListController
    public GameObjectUnityEvent onItemDropped; 

    void Awake()
    {
        backgroundImage = GetComponent<UnityEngine.UI.Image>();
        if (backgroundImage != null)
        {
            originalColor = backgroundImage.color;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        DraggableItem draggable = eventData.pointerDrag.GetComponent<DraggableItem>();
        if (draggable != null)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = highlightColor;
            }
            // Changed to use actionType for logging
            Debug.Log(draggable.name + " entered " + gameObject.name + " (Action: " + actionType + ")");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Check if still dragging something, otherwise eventData.pointerDrag might be null if drag ended outside
        if (eventData.pointerDrag != null) 
        {
             DraggableItem draggable = eventData.pointerDrag.GetComponent<DraggableItem>();
             if (draggable != null) // Ensure it's a draggable item still
             {
                if (backgroundImage != null)
                {
                    backgroundImage.color = originalColor;
                }
                Debug.Log(draggable.name + " exited " + gameObject.name);
             }
        } else { // Drag might have ended or pointer left for other reasons
            if (backgroundImage != null && backgroundImage.color == highlightColor)
            {
                 backgroundImage.color = originalColor; // Reset if it was highlighted
            }
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Changed to use actionType for logging
        Debug.Log(eventData.pointerDrag.name + " was dropped on " + gameObject.name + " (Action: " + actionType + ")");
        if (backgroundImage != null)
        {
            backgroundImage.color = originalColor; // Reset visual feedback
        }

        DraggableItem draggable = eventData.pointerDrag.GetComponent<DraggableItem>();
        if (draggable != null)
        {
            // Invoke the UnityEvent, passing the dropped GameObject
            onItemDropped.Invoke(eventData.pointerDrag);
        }
    }
}
