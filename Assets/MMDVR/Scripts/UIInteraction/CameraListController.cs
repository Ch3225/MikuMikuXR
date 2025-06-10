using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using MMDVR.Scripts.UIInteraction;
using MMDVR.Managers; // For CameraManager and MMDCameraManager
using System.IO;

public class CameraData : IResourceInfo
{
    public string id;
    public string displayName;
    public string filePath; // Null or empty for Free Camera
    public bool isFreeCamera;

    public string ID => id;
    public string DisplayName => displayName;
    public string FilePath => filePath;
    public ResourceType Type => ResourceType.Camera;
}

public class CameraListController : MonoBehaviour
{
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

    void Start()
    {
        if (cameraManager == null) cameraManager = CameraManager.Instance;
        if (mmdCameraManager == null) mmdCameraManager = MMDCameraManager.Instance;

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

        LoadAndDisplayItems();
        EventManager.OnCameraListChanged += OnExternalCameraListChanged; // Subscribe to event
    }

    void OnDestroy() // Remember to unsubscribe
    {
        EventManager.OnCameraListChanged -= OnExternalCameraListChanged;
        if (listSortableAreaDropZone != null)
        {
            listSortableAreaDropZone.onItemDropped.RemoveListener(HandleDropOnListArea);
        }
        if (uninstallDropZone != null)
        {
            uninstallDropZone.onItemDropped.RemoveListener(HandleDropOnUninstallZone);
        }
    }

    void LoadAndDisplayItems()
    {
        foreach (Transform child in listContainer)
        {
            Destroy(child.gameObject);
        }
        uiListItemObjects.Clear();
        internalResourceList.Clear();

        // 1. Add Free Camera
        internalResourceList.Add(new CameraData
        {
            id = FREE_CAMERA_ID,
            displayName = FREE_CAMERA_DISPLAY_NAME,
            filePath = null,
            isFreeCamera = true
        });

        // 2. Add VMD Cameras
        if (mmdCameraManager != null && mmdCameraManager.vmdCameraPaths != null)
        {
            foreach (var path in mmdCameraManager.vmdCameraPaths)
            {
                internalResourceList.Add(new CameraData
                {
                    id = path, // Use path as ID for VMD cameras
                    displayName = Path.GetFileNameWithoutExtension(path),
                    filePath = path,
                    isFreeCamera = false
                });
            }
        }
        RefreshResourceListUI();
    }

    void RefreshResourceListUI()
    {
        foreach (Transform child in listContainer)
        {
            Destroy(child.gameObject);
        }
        uiListItemObjects.Clear();

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
        DraggableItem droppedItem = droppedGameObject.GetComponent<DraggableItem>();
        if (droppedItem == null || droppedItem.Data == null || !(droppedItem.Data is CameraData)) return;

        IResourceInfo droppedResourceData = droppedItem.Data;

        int newIndex = 0;
        for (int i = 0; i < listContainer.childCount; i++)
        {
            if (listContainer.GetChild(i) == droppedGameObject.transform)
            {
                newIndex = i;
                break;
            }
        }

        droppedGameObject.transform.SetParent(listContainer);
        droppedGameObject.transform.SetSiblingIndex(newIndex);

        internalResourceList.Remove(droppedResourceData);
        internalResourceList.Insert(newIndex, droppedResourceData);

        Debug.Log($"{droppedResourceData.DisplayName} moved to index {newIndex} in {listContainer.name}");

        if (newIndex == 0) // Item moved to the top, activate it
        {
            ActivateResource(droppedResourceData);
        }
        UpdateAllItemVisuals();
    }

    public void HandleDropOnUninstallZone(GameObject droppedGameObject)
    {
        DraggableItem droppedItem = droppedGameObject.GetComponent<DraggableItem>();
        if (droppedItem == null || droppedItem.Data == null || !(droppedItem.Data is CameraData)) return;

        CameraData cameraToUninstall = droppedItem.Data as CameraData;

        if (cameraToUninstall.isFreeCamera)
        {
            // Debug.Log("Free Camera cannot be uninstalled.");
            // // Optional: Re-add to UI if it was visually removed by drag
            // RefreshResourceListUI(); 
            return; // Do nothing if it's the free camera
        }

        Debug.Log($"Requesting uninstall for VMD Camera: {cameraToUninstall.DisplayName}");

        bool isActiveCameraUninstalled = false;
        // int activeMMDIndex = mmdCameraManager.activeVmdCameraIndex;
        int activeMMDIndex = mmdCameraManager.currentIndex;
        if (activeMMDIndex != -1 && activeMMDIndex < mmdCameraManager.vmdCameraPaths.Count && 
            mmdCameraManager.vmdCameraPaths[activeMMDIndex] == cameraToUninstall.FilePath)
        {
            isActiveCameraUninstalled = true;
        }

        if (isActiveCameraUninstalled)
        {
            cameraManager.ActivateCamera(0); // Switch to Free Camera, this should set MMDCameraManager.currentIndex to -1
        }

        int vmdPathIndex = mmdCameraManager.vmdCameraPaths.IndexOf(cameraToUninstall.FilePath);
        if (vmdPathIndex != -1)
        {
            // MMDCameraManager.Instance.RemoveVmdCamera(cameraToUninstall.FilePath); // Reverted: This method doesn't exist on MMDCameraManager
            mmdCameraManager.vmdCameraPaths.RemoveAt(vmdPathIndex); // Use direct list manipulation for now
            
            // Adjust currentIndex if needed (copied from original logic before erroneous change)
            if (mmdCameraManager.currentIndex == vmdPathIndex) 
            {
                // This case is handled by cameraManager.ActivateCamera(0) above, which should lead to currentIndex being -1
            }
            else if (mmdCameraManager.currentIndex > vmdPathIndex)
            {
                mmdCameraManager.currentIndex--;
            }
            EventManager.OnCameraListChanged?.Invoke(); // Manually invoke after list modification
        }
        
        LoadAndDisplayItems(); 
    }

    void ActivateResource(IResourceInfo resourceData)
    {
        if (cameraManager == null || !(resourceData is CameraData)) return;

        CameraData camData = resourceData as CameraData;
        int activationIndex = internalResourceList.IndexOf(camData);

        if (activationIndex != -1)
        {
            Debug.Log($"Activating Camera: {camData.DisplayName} with UI list index: {activationIndex}");
            cameraManager.ActivateCamera(activationIndex);
        }
        else
        {
            Debug.LogError($"Could not find camera {camData.DisplayName} in internal list for activation.");
        }
        UpdateAllItemVisuals();
    }

    void UpdateAllItemVisuals()
    {
        if (cameraManager == null || mmdCameraManager == null) return;

        // bool freeCamActive = (mmdCameraManager.activeVmdCameraIndex == -1); 
        bool freeCamActive = (mmdCameraManager.currentIndex == -1);

        string activeVmdPath = null;
        // if (!freeCamActive && mmdCameraManager.activeVmdCameraIndex < mmdCameraManager.vmdCameraPaths.Count)
        if (!freeCamActive && mmdCameraManager.currentIndex >=0 && mmdCameraManager.currentIndex < mmdCameraManager.vmdCameraPaths.Count)
        {
            // activeVmdPath = mmdCameraManager.vmdCameraPaths[mmdCameraManager.activeVmdCameraIndex];
            activeVmdPath = mmdCameraManager.vmdCameraPaths[mmdCameraManager.currentIndex];
        }

        for (int i = 0; i < uiListItemObjects.Count; i++)
        {
            GameObject uiItemGO = uiListItemObjects[i];
            DraggableItem draggable = uiItemGO.GetComponent<DraggableItem>();
            if (draggable != null && draggable.Data != null && draggable.Data is CameraData)
            {
                CameraData camData = draggable.Data as CameraData;
                bool isActive = false;
                if (camData.isFreeCamera)
                {
                    isActive = freeCamActive;
                }
                else
                {
                    isActive = (!freeCamActive && camData.FilePath == activeVmdPath);
                }
                UpdateItemVisual(uiItemGO, camData, isActive, i == 0);
            }
        }
    }

    void UpdateItemVisual(GameObject itemGO, CameraData camData, bool isActive, bool isFirstInList)
    {
        UnityEngine.UI.Image bgImage = itemGO.GetComponent<UnityEngine.UI.Image>();
        if (bgImage != null)
        {
            // If it's active, yellow. If it's first in list (and not active), light gray. Else white.
            if (isActive)
            {
                bgImage.color = Color.yellow;
            }
            else
            {
                // bgImage.color = isFirstInList ? Color.lightGray : Color.white; 
                // For cameras, the "first in list" might always be FreeCamera after sorting.
                // The primary distinction is active (yellow) vs not active (white).
                bgImage.color = Color.white;
            }
        }

        Transform activeIndicator = itemGO.transform.Find("ActiveIndicator");
        if (activeIndicator != null)
        {
            activeIndicator.gameObject.SetActive(isActive);
        }
    }
    
    // Call this if MMDCameraManager.vmdCameraPaths changes externally
    public void OnExternalCameraListChanged()
    {
        LoadAndDisplayItems();
    }
}
