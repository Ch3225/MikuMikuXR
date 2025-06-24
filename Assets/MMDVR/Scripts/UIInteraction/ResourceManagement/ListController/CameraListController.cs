using UnityEngine;
using System.Collections.Generic;
using TMPro;
using MMDVR.Scripts.Model; // For CameraData and IResourceInfo
using MMDVR.Scripts.Managers;
using UnityEngine.UI;

/// <summary>
/// 摄像机列表控制器 - 直接与SceneStatesManager交互
/// </summary>

namespace MMDVR.Scripts.UIInteraction.ResourceManagement.ListController
{
    public class CameraListController : MonoBehaviour
{
    public static CameraListController Instance { get; private set; }    [Header("UI References")]
    public GameObject listItemPrefab;
    public Transform listContainer;
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
        
        // 备用：查找所有Action Type为Uninstall的DropZone并绑�?
        DropZone[] allDropZones = FindObjectsOfType<DropZone>();
        foreach (DropZone dz in allDropZones)
        {
            if (dz.actionType == DropZone.DropActionType.PanelUninstall)
            {
                dz.onItemDropped.AddListener(HandleDropOnUninstallZone);
                Debug.Log($"CameraListController: Bound HandleDropOnUninstallZone to Uninstall DropZone: {dz.gameObject.name}");
            }
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
        // 添加列表变化监听，通知SceneDisplayManager同步
        EventManager.OnCameraListChanged += NotifySceneDisplayManager;
        // 启动时主动刷新一�?
        RefreshResourceListUI();
    }    void OnDestroy()
    {
        EventManager.OnCameraListChanged -= RefreshResourceListUI;
        EventManager.OnCameraListChanged -= NotifySceneDisplayManager;
        EventManager.OnCameraActivated -= OnCameraActivated;
        
        if (uninstallDropZone != null)
        {
            uninstallDropZone.onItemDropped.RemoveListener(HandleDropOnUninstallZone);
        }
    }

    void LoadAndDisplayItems()
    {
        internalResourceList.Clear();

        // 从SceneStatesManager获取摄像机数�?
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
        
        // 立即销毁现有UI�?
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

        // 重新获取最新数�?
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

        // 为每个资源创建UI�?
        for (int i = 0; i < internalResourceList.Count; i++)
        {
            IResourceInfo resourceData = internalResourceList[i];
            GameObject listItemGO = Instantiate(listItemPrefab, listContainer);
            listItemGO.name = resourceData.Type + "_Item_" + resourceData.DisplayName.Replace(" ", "");            // 确保DraggableItem组件存在
            DraggableItem draggableItem = listItemGO.GetComponent<DraggableItem>();
            if (draggableItem == null)
            {
                draggableItem = listItemGO.AddComponent<DraggableItem>();
            }
            
            draggableItem.Data = resourceData;
            Debug.Log($"[CameraListController] 设置DraggableItem.Data: ID={resourceData.ID}, DisplayName={resourceData.DisplayName}, Type={resourceData.Type}");

            // Camera类型不需要DropZone，因为摄像机不接受其他资源的拖拽关联

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
    }    public void HandleDropOnUninstallZone(GameObject droppedGameObject)
    {
        Debug.Log("=== CameraListController: HandleDropOnUninstallZone called ===");
        
        // 添加空值检查，防止访问已销毁的GameObject
        if (droppedGameObject == null)
        {
            Debug.LogWarning("CameraListController: droppedGameObject is null or destroyed");
            return;
        }
        
        DraggableItem draggableItem = droppedGameObject.GetComponent<DraggableItem>();
        if (draggableItem == null || draggableItem.Data == null) 
        {
            Debug.LogWarning("DraggableItem or Data is null");
            return;
        }
        
        CameraData droppedCamData = draggableItem.Data as CameraData;
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
                Debug.Log($"CameraListController: 摄像机卸载完�?{droppedCamData.DisplayName}");
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
    }    public void HandleDropOnEnableZone(GameObject droppedGameObject)
    {
        // 添加空值检查，防止访问已销毁的GameObject
        if (droppedGameObject == null)
        {
            Debug.LogWarning("CameraListController: droppedGameObject is null or destroyed in HandleDropOnEnableZone");
            return;
        }
        
        DraggableItem draggableItem = droppedGameObject.GetComponent<DraggableItem>();
        if (draggableItem == null || draggableItem.Data == null) 
            return;

        CameraData droppedCamData = draggableItem.Data as CameraData;
        if (droppedCamData == null)
            return;

        // 使用UserActionManager进行用户操作
        UserActionManager.Instance?.ActivateCamera(droppedCamData.ID);
        Debug.Log($"CameraListController: 摄像机激活完毕 {droppedCamData.DisplayName}");
    }

    // 通过索引获取资源信息，用于向后兼�?
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
        if (!(resourceData is CameraData)) return;
        CameraData camDataToActivate = resourceData as CameraData;
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
    // 摄像机激活事件处�?
    void OnCameraActivated(CameraData activatedCameraData)
    {
        Debug.Log($"CameraListController: OnCameraActivated - {activatedCameraData?.DisplayName}");
        UpdateAllItemVisuals();    }    public void UpdateAllItemVisuals()
    {
        CameraData activeCamData = null;
        if (SceneStatesManager.Instance != null)
        {
            activeCamData = SceneDisplayManager.Instance.GetActiveCameraData();
        }

        // 检查是否有激活相机
        bool hasActive = (activeCamData != null);
        int freeCameraIndex = -1;

        for (int i = 0; i < uiListItemObjects.Count; i++)
        {
            GameObject uiItemGO = uiListItemObjects[i];
            DraggableItem draggable = uiItemGO.GetComponent<DraggableItem>();
            if (draggable != null && draggable.Data != null && draggable.Data is CameraData)
            {
                CameraData currentItemCamData = draggable.Data as CameraData;
                bool isActive = false;
                // 常规模式下，按摄像机ID匹配
                isActive = (activeCamData != null && activeCamData.ID == currentItemCamData.ID);
                if (currentItemCamData.isFreeCamera) freeCameraIndex = i;
                UpdateItemVisual(uiItemGO, currentItemCamData, isActive);
            }
        }
        // 如果没有激活相机，FreeCamera高亮绿色
        if (!hasActive && freeCameraIndex >= 0)
        {
            GameObject freeCamGO = uiListItemObjects[freeCameraIndex];
            DraggableItem draggable = freeCamGO.GetComponent<DraggableItem>();
            if (draggable != null && draggable.Data is CameraData)
            {
                UpdateItemVisual(freeCamGO, (CameraData)draggable.Data, true);
            }
        }
    }

    void UpdateItemVisual(GameObject itemGO, CameraData camData, bool isActive)
    {
        UnityEngine.UI.Image bgImage = itemGO.GetComponent<UnityEngine.UI.Image>();
        if (bgImage != null)
        {
            if (isActive)
            {
                bgImage.color = Color.green; // 激活项为绿色
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

    // ==================== 公共查询接口 ====================

    /// <summary>
    /// 获取当前第一项（激活项）的摄像机ID
    /// </summary>
    public string GetFirstCameraId()
    {
        if (listContainer != null && listContainer.childCount > 0)
        {
            var firstChild = listContainer.GetChild(0);
            var draggableItem = firstChild.GetComponent<DraggableItem>();
            if (draggableItem != null && draggableItem.Data != null)
            {
                return draggableItem.Data.ID;
            }
        }
        return null;
    }

    /// <summary>
    /// 获取所有摄像机ID列表（按当前UI顺序）
    /// </summary>
    public List<string> GetCameraIdList()
    {
        var cameraIds = new List<string>();
        if (listContainer != null)
        {
            for (int i = 0; i < listContainer.childCount; i++)
            {
                var child = listContainer.GetChild(i);
                var draggableItem = child.GetComponent<DraggableItem>();
                if (draggableItem != null && draggableItem.Data != null)
                {
                    cameraIds.Add(draggableItem.Data.ID);
                }
            }
        }
        return cameraIds;
    }

    /// <summary>
    /// 检查指定摄像机是否在列表中
    /// </summary>
    public bool HasCamera(string cameraId)
    {
        return GetCameraIdList().Contains(cameraId);
    }

    /// <summary>
    /// 获取指定摄像机在列表中的位置
    /// </summary>
    public int GetCameraIndex(string cameraId)
    {
        var cameraIds = GetCameraIdList();
        return cameraIds.IndexOf(cameraId);
    }

    /// <summary>
    /// 强制刷新摄像机列表数据和UI
    /// </summary>
    public void ForceRefresh()
    {
        RefreshResourceListUI();
    }

    /// <summary>
    /// 通知SceneDisplayManager进行同步
    /// </summary>
    private void NotifySceneDisplayManager()
    {
        if (SceneDisplayManager.Instance != null)
        {
            SceneDisplayManager.Instance.SyncActiveCameraWithList();
        }
    }
}
// 修复：补全命名空间大括号
}
