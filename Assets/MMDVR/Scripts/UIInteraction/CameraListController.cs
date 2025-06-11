using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using MMDVR.Scripts.UIInteraction; // Ensures CameraData from this namespace is preferred
using UICameraData = MMDVR.Scripts.UIInteraction.CameraData; // Added alias
using MMDVR.Managers;
using System.IO;

public class CameraListController : MonoBehaviour
{
    public static CameraListController Instance { get; private set; } // Singleton instance

    [Header("UI References")]
    public GameObject listItemPrefab;
    public Transform listContainer;
    public DropZone listSortableAreaDropZone;
    public DropZone uninstallDropZone;

    [Header("Manager References")]
    public CameraManager cameraManager;
    public MMDCameraManager mmdCameraManager;

    private List<IResourceInfo> internalResourceList = new List<IResourceInfo>();
    private List<GameObject> uiListItemObjects = new List<GameObject>();

    private const string FREE_CAMERA_ID = "BUILTIN_FREE_CAMERA";
    private const string FREE_CAMERA_DISPLAY_NAME = "Free Camera";

    void Awake() // Awake for singleton initialization
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }    void Start()
    {
        if (cameraManager == null) cameraManager = CameraManager.Instance;
        if (mmdCameraManager == null) mmdCameraManager = MMDCameraManager.Instance;

        Debug.Log($"CameraListController.Start - cameraManager: {cameraManager}");
        Debug.Log($"CameraListController.Start - mmdCameraManager: {mmdCameraManager}");
        
        if (mmdCameraManager != null)
        {
            Debug.Log($"CameraListController.Start - mmdCameraManager.vmdCameraPaths count: {mmdCameraManager.vmdCameraPaths.Count}");
            Debug.Log($"CameraListController.Start - mmdCameraManager.gameObject.activeInHierarchy: {mmdCameraManager.gameObject.activeInHierarchy}");
            Debug.Log($"CameraListController.Start - mmdCameraManager.enabled: {mmdCameraManager.enabled}");
        }
        else
        {
            Debug.LogError("MMDCameraManager.Instance is null! The MMDCameraManager GameObject might not be active in the scene.");
            
            // Try to find it manually
            MMDCameraManager manualFind = FindObjectOfType<MMDCameraManager>();
            if (manualFind != null)
            {
                Debug.LogWarning($"Found MMDCameraManager manually: {manualFind.name}, active: {manualFind.gameObject.activeInHierarchy}");
                mmdCameraManager = manualFind;
            }
            else
            {
                Debug.LogError("No MMDCameraManager found in the scene at all!");
            }
        }

        if (listItemPrefab == null || listContainer == null || cameraManager == null || mmdCameraManager == null)
        {
            Debug.LogError("CameraListController: UI References or Managers not set!");
            enabled = false;
            return;
        }

        if (listSortableAreaDropZone != null)
        {
            listSortableAreaDropZone.onItemDropped.AddListener(HandleDropOnListArea);
        }
        if (uninstallDropZone != null)
        {
            uninstallDropZone.onItemDropped.AddListener(HandleDropOnUninstallZone);
        }

        PopulateInitialCameraList();
        EventManager.OnCameraListChanged += HandleExternalCameraListChange;
        EventManager.OnCameraActivated += HandleCameraActivatedEvent; 
    }

    void OnDestroy()
    {
        EventManager.OnCameraListChanged -= HandleExternalCameraListChange;
        EventManager.OnCameraActivated -= HandleCameraActivatedEvent; 
        if (listSortableAreaDropZone != null)
        {
            listSortableAreaDropZone.onItemDropped.RemoveListener(HandleDropOnListArea);
        }
        if (uninstallDropZone != null)
        {
            uninstallDropZone.onItemDropped.RemoveListener(HandleDropOnUninstallZone);
        }
    }

    void PopulateInitialCameraList()
    {
        internalResourceList.Clear();

        // 1. Ensure Free Camera is always present and is the first consideration.
        AddFreeCameraToListEnsuringUniqueness();

        // 2. Add VMD Cameras from MMDCameraManager
        if (mmdCameraManager != null && mmdCameraManager.vmdCameraPaths != null)
        {
            foreach (var path in mmdCameraManager.vmdCameraPaths)
            {
                // Add VMD if not already in the list (by path)
                if (!internalResourceList.Any(item => item is UICameraData cd && !cd.isFreeCamera && cd.FilePath == path)) // Changed
                {
                    internalResourceList.Add(new UICameraData // Changed
                    {
                        id = path, 
                        displayName = Path.GetFileNameWithoutExtension(path),
                        filePath = path,
                        isFreeCamera = false
                    });
                }
            }
        }
        RefreshResourceListUI(); 
    }

    // This handler is for external changes, e.g., TestCase2 loading new cameras.
    void HandleExternalCameraListChange()
    {
        Debug.Log("CameraListController: Detected external camera list change.");

        // Preserve the currently active camera if possible
        UICameraData previouslyActiveCamera = cameraManager.GetActiveCameraData(); // Changed

        // Start with a clean slate for VMDs, but preserve or add Free Camera.
        List<IResourceInfo> newInternalList = new List<IResourceInfo>();

        // 1. Ensure Free Camera is present.
        // Try to find an existing Free Camera instance to preserve its object identity if it matters.
        IResourceInfo existingFreeCam = internalResourceList.FirstOrDefault(r => r is UICameraData cd && cd.isFreeCamera); // Changed
        if (existingFreeCam != null)
        {
            newInternalList.Add(existingFreeCam);
        }
        else
        {
            newInternalList.Add(new UICameraData { id = FREE_CAMERA_ID, displayName = FREE_CAMERA_DISPLAY_NAME, filePath = null, isFreeCamera = true }); // Changed
        }

        // 2. Add VMDs from MMDCameraManager
        if (mmdCameraManager != null && mmdCameraManager.vmdCameraPaths != null)
        {
            foreach (var path in mmdCameraManager.vmdCameraPaths)
            {
                if (!newInternalList.Any(item => item is UICameraData cd && !cd.isFreeCamera && cd.FilePath == path)) // Changed
                {
                    newInternalList.Add(new UICameraData // Changed
                    {
                        id = path,
                        displayName = Path.GetFileNameWithoutExtension(path),
                        filePath = path,
                        isFreeCamera = false
                    });
                }
            }
        }
        
        // 3. Replace the old list
        internalResourceList = newInternalList;

        EnsureSingleFreeCameraInInternalList(); // Ensure data integrity

        // 4. Attempt to restore active camera if it still exists in the new list
        bool activeRestored = false;
        if (previouslyActiveCamera != null)
        {
            UICameraData foundActive = internalResourceList.FirstOrDefault(r => (r as UICameraData)?.ID == previouslyActiveCamera.ID) as UICameraData; // Changed
            if (foundActive != null)
            {
                activeRestored = true;
            }
        }

        // If active camera wasn't restored (e.g., it was removed), and list is not empty, activate the top one.
        if (!activeRestored && internalResourceList.Count > 0)
        {
            cameraManager.ActivateCameraByResource(internalResourceList[0] as UICameraData); // Changed
        }
        else if (!activeRestored && internalResourceList.Count == 0) // Should not happen with Free Camera logic
        {
            cameraManager.ActivateCameraByResource(null); // Activate Free Camera
        }

        RefreshResourceListUI();
    }

    private void AddFreeCameraToListEnsuringUniqueness()
    {
        if (!internalResourceList.Any(r => r is UICameraData cd && cd.isFreeCamera)) // Changed
        {
            internalResourceList.Insert(0, new UICameraData { id = FREE_CAMERA_ID, displayName = FREE_CAMERA_DISPLAY_NAME, filePath = null, isFreeCamera = true }); // Changed
        }
    }

    private void EnsureSingleFreeCameraInInternalList()
    {
        UICameraData firstFreeCamera = null; // Changed
        List<IResourceInfo> itemsToRemove = new List<IResourceInfo>();

        foreach (var item in internalResourceList)
        {
            if (item is UICameraData cd && cd.isFreeCamera) // Changed
            {
                if (firstFreeCamera == null)
                {
                    firstFreeCamera = cd;
                }
                else
                {
                    itemsToRemove.Add(item); // Mark subsequent Free Camera instances for removal
                }
            }
        }

        foreach (var itemToRemove in itemsToRemove)
        {
            internalResourceList.Remove(itemToRemove);
        }

        // If no free camera was found at all (should not happen if AddFreeCameraToListEnsuringUniqueness was called), add one.
        if (firstFreeCamera == null)
        {
             internalResourceList.Insert(0, new UICameraData { id = FREE_CAMERA_ID, displayName = FREE_CAMERA_DISPLAY_NAME, filePath = null, isFreeCamera = true }); // Changed
        }
    }

    void RefreshResourceListUI()
    {
        foreach (Transform child in listContainer)
        {
            Destroy(child.gameObject);
        }
        uiListItemObjects.Clear();

        EnsureSingleFreeCameraInInternalList(); // Ensure data integrity before building UI

        // If internalResourceList is empty after ensuring single free camera (which it shouldn't be),
        // explicitly add Free Camera again. This is a safeguard.
        if (internalResourceList.Count == 0)
        {
            Debug.LogWarning("internalResourceList was empty after EnsureSingleFreeCameraInInternalList. Re-adding Free Camera.");
            AddFreeCameraToListEnsuringUniqueness();
        }

        for (int i = 0; i < internalResourceList.Count; i++)
        {
            IResourceInfo resourceData = internalResourceList[i];
            GameObject listItemGO = Instantiate(listItemPrefab, listContainer);
            listItemGO.name = resourceData.Type + "_Item_" + resourceData.DisplayName.Replace(" ", "");

            DraggableItem draggableItem = listItemGO.GetComponent<DraggableItem>();
            if (draggableItem != null)
            {
                draggableItem.Data = resourceData;
            }

            TextMeshProUGUI titleText = listItemGO.GetComponentInChildren<TextMeshProUGUI>();
            if (titleText != null)
            {
                titleText.text = resourceData.DisplayName;
            }

            uiListItemObjects.Add(listItemGO);
        }
        UpdateAllItemVisuals();
    }

    public void HandleDropOnListArea(GameObject droppedGameObject)
    {
        DraggableItem droppedItemComponent = droppedGameObject.GetComponent<DraggableItem>();
        if (droppedItemComponent == null || droppedItemComponent.Data == null || !(droppedItemComponent.Data is UICameraData)) return; // Changed

        // 1. Rebuild internalResourceList based on the new UI order in listContainer
        List<IResourceInfo> newOrderedInternalList = new List<IResourceInfo>();
        for (int i = 0; i < listContainer.childCount; i++)
        {
            Transform child = listContainer.GetChild(i);
            DraggableItem item = child.GetComponent<DraggableItem>();
            if (item != null && item.Data != null)
            {
                newOrderedInternalList.Add(item.Data);
            }
        }
        internalResourceList = newOrderedInternalList;
        EnsureSingleFreeCameraInInternalList(); // Crucial step after rebuilding from UI

        // 2. Synchronize MMDCameraManager.vmdCameraPaths with VMDs in the new internalResourceList order
        List<string> newVmdCameraPaths = new List<string>();
        foreach (var resourceInfo in internalResourceList)
        {
            if (resourceInfo is UICameraData camData && !camData.isFreeCamera && !string.IsNullOrEmpty(camData.FilePath)) // Changed
            {
                newVmdCameraPaths.Add(camData.FilePath);
            }
        }
        if (mmdCameraManager != null)
        {
            mmdCameraManager.vmdCameraPaths = newVmdCameraPaths;
            // After reordering, the MMDCameraManager's currentIndex might be out of sync 
            // with the actual VMD file that was previously active if its path moved.
            // We need to find the new index of the previously active VMD (if any) or rely on ActivateCamera.
        }

        // 3. Activate the new top item (if any)
        if (cameraManager != null && internalResourceList.Count > 0)
        {
            // The CameraData object itself is what matters for activation, not just its string path.
            UICameraData newTopCamera = internalResourceList[0] as UICameraData; // Changed
            cameraManager.ActivateCameraByResource(newTopCamera);
        }
        else if (cameraManager != null) 
        {
            cameraManager.ActivateCameraByResource(null); // Signal to activate default (Free Camera)
        }
        
        // UpdateAllItemVisuals(); // Activation will trigger event that calls this
        EventManager.OnCameraListChanged?.Invoke(); // Notify other systems of the change in order/activation
    }    public void HandleDropOnUninstallZone(GameObject droppedGameObject)
    {
        Debug.Log("=== HandleDropOnUninstallZone called ===");
        
        DraggableItem draggableItem = droppedGameObject.GetComponent<DraggableItem>();
        if (draggableItem == null || draggableItem.Data == null) 
        {
            Debug.LogWarning("DraggableItem or Data is null");
            return;
        }

        UICameraData droppedCamData = draggableItem.Data as UICameraData; // Changed
        if (droppedCamData == null)
        {
            Debug.LogWarning("Dropped item is not a valid CameraData.");
            return;
        }

        Debug.Log($"Dropped camera data: {droppedCamData.DisplayName}, FilePath: {droppedCamData.FilePath}, isFreeCamera: {droppedCamData.isFreeCamera}");

        if (droppedCamData.isFreeCamera)
        {
            // RefreshResourceListUI(); // Re-add to UI if it was visually removed by drag - not needed if it can't be truly removed
            Debug.Log("Attempted to uninstall Free Camera. This action is blocked.");
            // Ensure UI is consistent if the drag operation visually removed it temporarily
            RefreshResourceListUI(); // This will ensure Free Camera is re-added if it was visually displaced
            return; 
        }

        Debug.Log($"Requesting uninstall for VMD Camera: {droppedCamData.DisplayName} (Path: {droppedCamData.FilePath})");

        // Check MMDCameraManager state before removal
        if (mmdCameraManager != null)
        {
            Debug.Log($"Before removal - MMDCameraManager.vmdCameraPaths count: {mmdCameraManager.vmdCameraPaths.Count}");
            Debug.Log($"Before removal - MMDCameraManager.currentIndex: {mmdCameraManager.currentIndex}");
        }
        else
        {
            Debug.LogError("mmdCameraManager is null!");
        }

        bool wasActive = false;
        if (mmdCameraManager != null && mmdCameraManager.currentIndex != -1 && 
            mmdCameraManager.currentIndex < mmdCameraManager.vmdCameraPaths.Count &&
            mmdCameraManager.vmdCameraPaths[mmdCameraManager.currentIndex] == droppedCamData.FilePath)
        {
            wasActive = true;
        }        // Remove from MMDCameraManager first
        if (mmdCameraManager != null)
        {
            Debug.Log("Calling mmdCameraManager.RemoveVmdCamera...");
            mmdCameraManager.RemoveVmdCamera(droppedCamData.FilePath); // Use the new method
            
            // Check state after removal
            Debug.Log($"After removal - MMDCameraManager.vmdCameraPaths count: {mmdCameraManager.vmdCameraPaths.Count}");
            Debug.Log($"After removal - MMDCameraManager.currentIndex: {mmdCameraManager.currentIndex}");
        }

        // Remove from our internal list
        Debug.Log($"Removing from internal list. Current count: {internalResourceList.Count}");
        bool removed = internalResourceList.Remove(droppedCamData);
        Debug.Log($"Removed from internal list: {removed}. New count: {internalResourceList.Count}");

        // Refresh UI from internal list
        Debug.Log("Refreshing UI...");
        RefreshResourceListUI();// If the uninstalled camera was active, or if the list is now empty or only has Free Camera,
        // activate the new top item (which will be Free Camera if no VMDs are left or if it's at the top).
        if (wasActive || internalResourceList.Count == 0 || (internalResourceList.Count > 0 && (internalResourceList[0] as UICameraData).isFreeCamera) )
        {
            if (cameraManager != null && internalResourceList.Count > 0)
            {
                cameraManager.ActivateCameraByResource(internalResourceList[0] as UICameraData);
            }
            else if (cameraManager != null) // List is now empty (shouldn't happen if FreeCam is always there)
            {
                 cameraManager.ActivateCameraByResource(null); // Activate Free Camera
            }
        }
        
        UpdateAllItemVisuals();
        EventManager.OnCameraListChanged?.Invoke(); // Notify about the list change
    }

    // Method to get resource info by index, used by CameraManager if it were still using index-based activation
    public IResourceInfo GetResourceInfoAt(int index)
    {
        if (index >= 0 && index < internalResourceList.Count)
        {
            return internalResourceList[index];
        }
        Debug.LogWarning($"CameraListController.GetResourceInfoAt: Index {index} is out of bounds for internalResourceList count {internalResourceList.Count}.");
        return null;
    }    // ActivateResource is called by item click, not used by drag/drop directly for activation.
    // Activation after drag/drop is handled by HandleDropOnListArea.
    void ActivateResource(IResourceInfo resourceData) // This is likely for item click selection
    {
        if (cameraManager == null || !(resourceData is UICameraData)) return;
        UICameraData camDataToActivate = resourceData as UICameraData;

        Debug.Log($"Activating Camera by click: {camDataToActivate.DisplayName}");
        cameraManager.ActivateCameraByResource(camDataToActivate);
        
        // Visuals are updated by the OnCameraListChanged event triggered by CameraManager, 
        // or directly if needed.
        // UpdateAllItemVisuals(); // CameraManager.ActivateCameraByResource should trigger event that leads here.
    }

    // This method is called when the active camera changes (e.g., by CameraManager)
    void HandleCameraActivatedEvent(UICameraData activatedCameraData) // Changed parameter type
    {
        // No need to change internalResourceList or MMDCameraManager here.
        // Just update the visuals to reflect the active camera.
        UpdateAllItemVisuals();
    }    void UpdateAllItemVisuals()
    {
        UICameraData activeCamData = cameraManager.GetActiveCameraData(); // Changed
        bool isFirstItem = true; // To track the first item for potential default activation visual

        for (int i = 0; i < uiListItemObjects.Count; i++)
        {
            GameObject uiItemGO = uiListItemObjects[i];
            DraggableItem draggable = uiItemGO.GetComponent<DraggableItem>();
            if (draggable != null && draggable.Data != null && draggable.Data is UICameraData)
            {
                UICameraData currentItemCamData = draggable.Data as UICameraData;
                bool isActive = (activeCamData != null && activeCamData.ID == currentItemCamData.ID);
                UpdateItemVisual(uiItemGO, currentItemCamData, isActive, isFirstItem);
                isFirstItem = false; // Only the first item in the list should be considered for default active visual
            }
        }
    }

    void UpdateItemVisual(GameObject itemGO, UICameraData camData, bool isActive, bool isFirstInList)
    {
        UnityEngine.UI.Image bgImage = itemGO.GetComponent<UnityEngine.UI.Image>();
        if (bgImage != null)
        {
            if (isActive)
            {
                bgImage.color = Color.yellow; // Active item is yellow
            }
            else
            {
                bgImage.color = Color.white; // Non-active items are white
            }
        }

        Transform activeIndicator = itemGO.transform.Find("ActiveIndicator");
        if (activeIndicator != null)
        {
            activeIndicator.gameObject.SetActive(isActive);
        }
    }
}
