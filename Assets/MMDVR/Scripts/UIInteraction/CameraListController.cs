using UnityEngine;
using System.Collections.Generic;
using TMPro;
using MMDVR.Scripts.UIInteraction;
using UICameraData = MMDVR.Scripts.UIInteraction.CameraData;
using MMDVR.Managers;

/// <summary>
/// 摄像机列表控制器 - 直接与SceneStatesManager交互
/// </summary>
public class CameraListController : MonoBehaviour
{
    public static CameraListController Instance { get; private set; }

    [Header("UI References")]
    public GameObject listItemPrefab;
    public Transform listContainer;
    public DropZone listSortableAreaDropZone;
    public DropZone uninstallDropZone;

    private List<IResourceInfo> internalResourceList = new List<IResourceInfo>();
    private List<GameObject> uiListItemObjects = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (listItemPrefab == null || listContainer == null)
        {
            Debug.LogError("CameraListController: UI References not set!");
            enabled = false;
            return;
        }

        // 设置拖拽事件
        if (listSortableAreaDropZone != null)
        {
            listSortableAreaDropZone.onItemDropped.AddListener(HandleDropOnListArea);
        }
        if (uninstallDropZone != null)
        {
            uninstallDropZone.onItemDropped.AddListener(HandleDropOnUninstallZone);
        }

        // 监听事件
        EventManager.OnCameraListChanged += RefreshResourceListUI;
        EventManager.OnCameraActivated += OnCameraActivated;

        LoadAndDisplayItems();
    }

    void OnDestroy()
    {
        EventManager.OnCameraListChanged -= RefreshResourceListUI;
        EventManager.OnCameraActivated -= OnCameraActivated;
        
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
        internalResourceList.Clear();

        // 从SceneStatesManager获取摄像机数据
        if (SceneStatesManager.Instance != null)
        {
            var cameraDataList = SceneStatesManager.Instance.GetCameraDataList();
            foreach (var cameraData in cameraDataList)
            {
                internalResourceList.Add(cameraData);
            }
        }
        else
        {
            Debug.LogWarning("SceneStatesManager.Instance is null");
        }

        RefreshResourceListUI();
    }

    void RefreshResourceListUI()
    {
        // 清除现有UI项
        foreach (Transform child in listContainer)
        {
            Destroy(child.gameObject);
        }
        uiListItemObjects.Clear();

        // 重新获取最新数据
        if (SceneStatesManager.Instance != null)
        {
            internalResourceList.Clear();
            var cameraDataList = SceneStatesManager.Instance.GetCameraDataList();
            foreach (var cameraData in cameraDataList)
            {
                internalResourceList.Add(cameraData);
            }
        }

        // 为每个资源创建UI项
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

            // 添加点击事件监听
            UnityEngine.UI.Button button = listItemGO.GetComponent<UnityEngine.UI.Button>();
            if (button == null)
                button = listItemGO.AddComponent<UnityEngine.UI.Button>();
            
            var resourceDataCopy = resourceData; // 避免闭包问题
            button.onClick.AddListener(() => ActivateResource(resourceDataCopy));

            uiListItemObjects.Add(listItemGO);
        }

        UpdateAllItemVisuals();
    }

    public void HandleDropOnListArea(GameObject droppedGameObject)
    {
        DraggableItem droppedItemComponent = droppedGameObject.GetComponent<DraggableItem>();
        if (droppedItemComponent == null || droppedItemComponent.Data == null || !(droppedItemComponent.Data is UICameraData)) 
            return;

        // 重新构建内部列表基于UI顺序
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

        // 激活新的顶部项目
        if (internalResourceList.Count > 0)
        {
            UICameraData newTopCamera = internalResourceList[0] as UICameraData;
            if (SceneStatesManager.Instance != null && newTopCamera != null)
            {
                SceneStatesManager.Instance.SetActiveCamera(newTopCamera.ID);
            }
        }

        UpdateAllItemVisuals();
    }

    public void HandleDropOnUninstallZone(GameObject droppedGameObject)
    {
        Debug.Log("=== CameraListController: HandleDropOnUninstallZone called ===");
        
        DraggableItem draggableItem = droppedGameObject.GetComponent<DraggableItem>();
        if (draggableItem == null || draggableItem.Data == null) 
        {
            Debug.LogWarning("DraggableItem or Data is null");
            return;
        }

        UICameraData droppedCamData = draggableItem.Data as UICameraData;
        if (droppedCamData == null)
        {
            Debug.LogWarning("Dropped item is not a valid CameraData.");
            return;
        }

        Debug.Log($"Dropped camera data: {droppedCamData.DisplayName}, FilePath: {droppedCamData.FilePath}, isFreeCamera: {droppedCamData.isFreeCamera}");

        // 不能删除Free Camera
        if (droppedCamData.isFreeCamera)
        {
            Debug.Log("Attempted to uninstall Free Camera. This action is blocked.");
            RefreshResourceListUI(); // 确保UI一致性
            return; 
        }

        // 通过SceneStatesManager删除摄像机资源
        if (SceneStatesManager.Instance != null)
        {
            SceneStatesManager.Instance.RemoveCameraResource(droppedCamData.ID);
            Debug.Log($"Requested uninstall for VMD Camera: {droppedCamData.DisplayName}");
        }
        else
        {
            Debug.LogError("SceneStatesManager.Instance is null!");
        }

        // UI刷新会由事件触发
    }

    // 通过索引获取资源信息，用于向后兼容
    public IResourceInfo GetResourceInfoAt(int index)
    {
        if (index >= 0 && index < internalResourceList.Count)
        {
            return internalResourceList[index];
        }
        Debug.LogWarning($"CameraListController.GetResourceInfoAt: Index {index} is out of bounds for internalResourceList count {internalResourceList.Count}.");
        return null;
    }

    // 激活资源（点击或其他方式）
    void ActivateResource(IResourceInfo resourceData)
    {
        if (!(resourceData is UICameraData)) return;
        UICameraData camDataToActivate = resourceData as UICameraData;

        Debug.Log($"Activating Camera by click: {camDataToActivate.DisplayName}");
        
        if (SceneStatesManager.Instance != null)
        {
            SceneStatesManager.Instance.SetActiveCamera(camDataToActivate.ID);
        }
        else
        {
            Debug.LogError("SceneStatesManager.Instance is null!");
        }
    }

    // 摄像机激活事件处理
    void OnCameraActivated(UICameraData activatedCameraData)
    {
        Debug.Log($"CameraListController: OnCameraActivated - {activatedCameraData?.DisplayName}");
        UpdateAllItemVisuals();
    }

    void UpdateAllItemVisuals()
    {
        UICameraData activeCamData = null;
        if (SceneStatesManager.Instance != null)
        {
            activeCamData = SceneStatesManager.Instance.GetActiveCameraData();
        }

        for (int i = 0; i < uiListItemObjects.Count; i++)
        {
            GameObject uiItemGO = uiListItemObjects[i];
            DraggableItem draggable = uiItemGO.GetComponent<DraggableItem>();
            if (draggable != null && draggable.Data != null && draggable.Data is UICameraData)
            {
                UICameraData currentItemCamData = draggable.Data as UICameraData;
                bool isActive = (activeCamData != null && activeCamData.ID == currentItemCamData.ID);
                UpdateItemVisual(uiItemGO, currentItemCamData, isActive);
            }
        }
    }

    void UpdateItemVisual(GameObject itemGO, UICameraData camData, bool isActive)
    {
        UnityEngine.UI.Image bgImage = itemGO.GetComponent<UnityEngine.UI.Image>();
        if (bgImage != null)
        {
            if (isActive)
            {
                bgImage.color = Color.yellow; // 激活项为黄色
            }
            else
            {
                bgImage.color = Color.white; // 非激活项为白色
            }
        }

        Transform activeIndicator = itemGO.transform.Find("ActiveIndicator");
        if (activeIndicator != null)
        {
            activeIndicator.gameObject.SetActive(isActive);
        }
    }
}