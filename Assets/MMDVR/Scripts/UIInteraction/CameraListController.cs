using UnityEngine;
using System.Collections.Generic;
using TMPro;
using MMDVR.Scripts.UIInteraction;
using UICameraData = MMDVR.Scripts.UIInteraction.CameraData;
using MMDVR.Scripts.Managers;
using UnityEngine.UI;

/// <summary>
/// 摄像机列表控制器 - 直接与SceneStatesManager交互
/// </summary>
public class CameraListController : MonoBehaviour
{
    public static CameraListController Instance { get; private set; }    [Header("UI References")]
    public GameObject listItemPrefab;
    public Transform listContainer;
    public DropZone listSortableAreaDropZone;
    public DropZone uninstallDropZone;
    public DropZone enableDropZone;

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
    }    void Start()
    {
        if (listItemPrefab == null || listContainer == null)
        {
            Debug.LogError("CameraListController: UI References not set!");
            enabled = false;
            return;
        }
          // 自动查找并绑定DropzoneUnload
        DropZone dropzoneUnload = GameObject.Find("DropzoneUnload")?.GetComponent<DropZone>();
        if (dropzoneUnload != null)
        {
            dropzoneUnload.onItemDropped.AddListener(HandleDropOnUninstallZone);
            Debug.Log("CameraListController: Auto-found and bound HandleDropOnUninstallZone to DropzoneUnload");
        }
        else
        {
            Debug.LogError("CameraListController: Could not find DropzoneUnload GameObject in scene!");
        }
        
        // 备用：查找所有Action Type为Uninstall的DropZone并绑定
        DropZone[] allDropZones = FindObjectsOfType<DropZone>();
        foreach (DropZone dz in allDropZones)
        {
            if (dz.actionType == DropZone.DropActionType.Uninstall)
            {
                dz.onItemDropped.AddListener(HandleDropOnUninstallZone);
                Debug.Log($"CameraListController: Bound HandleDropOnUninstallZone to Uninstall DropZone: {dz.gameObject.name}");
            }
        }
        
        if (listSortableAreaDropZone != null)
        {
            listSortableAreaDropZone.onItemDropped.AddListener(HandleDropOnListArea);
            Debug.Log("CameraListController: Bound HandleDropOnListArea to listSortableAreaDropZone");
        }
        if (uninstallDropZone != null)
        {
            uninstallDropZone.onItemDropped.AddListener(HandleDropOnUninstallZone);
            Debug.Log("CameraListController: Bound HandleDropOnUninstallZone to uninstallDropZone (Inspector)");
        }
        if (enableDropZone != null)
        {
            enableDropZone.onItemDropped.AddListener(HandleDropOnEnableZone);
            Debug.Log("CameraListController: Bound HandleDropOnEnableZone to enableDropZone");
        }

        // 统一刷新机制：只通过事件刷新
        EventManager.OnCameraListChanged += RefreshResourceListUI;
        // 启动时主动刷新一次
        RefreshResourceListUI();
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
    }    void RefreshResourceListUI()
    {
        Debug.Log($"CameraListController: RefreshResourceListUI called. Current UI items count: {uiListItemObjects.Count}");
        
        // 立即销毁现有UI项
        foreach (GameObject obj in uiListItemObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        uiListItemObjects.Clear();
        
        // 额外确保容器完全清空
        List<Transform> childrenToDestroy = new List<Transform>();
        foreach (Transform child in listContainer)
        {
            childrenToDestroy.Add(child);
        }
        foreach (Transform child in childrenToDestroy)
        {
            if (child != null)
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // 重新获取最新数据
        if (SceneStatesManager.Instance != null)
        {
            internalResourceList.Clear();
            var cameraDataList = SceneStatesManager.Instance.GetCameraDataList();
            foreach (var cameraData in cameraDataList)
            {
                internalResourceList.Add(cameraData);
            }
            Debug.Log($"CameraListController: Refreshed data. Camera count: {cameraDataList.Count}");
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
        // 强制刷新布局
        if (listContainer is RectTransform rect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
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
            UICameraData newTopCamera = internalResourceList[0] as UICameraData;            if (ResourceManager.Instance != null && newTopCamera != null)
            {
                ResourceManager.Instance.SetActiveCamera(newTopCamera.ID);
            }
        }

        UpdateAllItemVisuals();
    }    public void HandleDropOnUninstallZone(GameObject droppedGameObject)
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
            return; 
        }
        
        // 使用UserActionManager进行用户操作
        if (UserActionManager.Instance != null)
        {
            UserActionManager.Instance.UnloadCamera(droppedCamData.ID, () => {
                Debug.Log($"CameraListController: 摄像机卸载完成 {droppedCamData.DisplayName}");
            });
        }
        else
        {
            Debug.LogError("UserActionManager.Instance is null!");
        }
    }

    // 添加一个简化的备用方法，用于Unity Inspector绑定
    public void OnCameraDroppedForUninstall(GameObject droppedObject)
    {
        HandleDropOnUninstallZone(droppedObject);
    }

    public void HandleDropOnEnableZone(GameObject droppedGameObject)
    {
        DraggableItem draggableItem = droppedGameObject.GetComponent<DraggableItem>();
        if (draggableItem == null || draggableItem.Data == null) 
            return;

        UICameraData droppedCamData = draggableItem.Data as UICameraData;
        if (droppedCamData == null)
            return;

        // 使用UserActionManager进行用户操作
        if (UserActionManager.Instance != null)
        {
            UserActionManager.Instance.ActivateCamera(droppedCamData.ID, () => {
                Debug.Log($"CameraListController: 摄像机激活完成 {droppedCamData.DisplayName}");
            });
        }
        else
        {
            Debug.LogError("UserActionManager.Instance is null!");
        }
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
          if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.SetActiveCamera(camDataToActivate.ID);
        }
        else
        {
            Debug.LogError("ResourceManager.Instance is null!");
        }
    }

    // 摄像机激活事件处理
    void OnCameraActivated(UICameraData activatedCameraData)
    {
        Debug.Log($"CameraListController: OnCameraActivated - {activatedCameraData?.DisplayName}");
        UpdateAllItemVisuals();
    }    void UpdateAllItemVisuals()
    {
        MMDVR.Scripts.UIInteraction.CameraData activeCamData = null;
        if (SceneStatesManager.Instance != null)
        {
            var dataActiveCamData = SceneDisplayManager.Instance.GetActiveCameraData();
            if (dataActiveCamData != null)
            {                // 转换为UIInteraction的CameraData类型
                activeCamData = new MMDVR.Scripts.UIInteraction.CameraData
                {
                    id = dataActiveCamData.id,
                    displayName = dataActiveCamData.displayName,
                    filePath = dataActiveCamData.filePath
                };
            }
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