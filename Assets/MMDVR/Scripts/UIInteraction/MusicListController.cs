using UnityEngine;
using System.Collections.Generic;
using TMPro;
using MMDVR.Scripts.UIInteraction;
using MMDVR.Managers;

/// <summary>
/// 音乐列表控制器 - 直接与SceneStatesManager交互
/// </summary>
public class MusicListController : MonoBehaviour
{
    public static MusicListController Instance { get; private set; }

    [Header("UI References")]
    public GameObject listItemPrefab;
    public Transform listContainer;
    public DropZone listSortableAreaDropZone;
    public DropZone uninstallDropZone;

    private List<GameObject> uiListItemObjects = new List<GameObject>();
    private List<IResourceInfo> internalResourceList = new List<IResourceInfo>();

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
            Debug.LogError("MusicListController: UI References not set!");
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

        // 监听事件
        EventManager.OnMusicListChanged += RefreshResourceListUI;

        LoadAndDisplayItems();
    }

    void OnDestroy()
    {
        EventManager.OnMusicListChanged -= RefreshResourceListUI;
        
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

        // 从SceneStatesManager获取音乐数据
        if (SceneStatesManager.Instance != null)
        {
            var musicDataList = SceneStatesManager.Instance.GetMusicDataList();
            foreach (var musicData in musicDataList)
            {
                internalResourceList.Add(musicData);
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
            var musicDataList = SceneStatesManager.Instance.GetMusicDataList();
            foreach (var musicData in musicDataList)
            {
                internalResourceList.Add(musicData);
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
        if (droppedItemComponent == null || droppedItemComponent.Data == null || !(droppedItemComponent.Data is MusicData)) 
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
            MusicData newTopMusic = internalResourceList[0] as MusicData;
            if (SceneStatesManager.Instance != null && newTopMusic != null)
            {
                SceneStatesManager.Instance.SetActiveMusic(newTopMusic.ID);
            }
        }

        UpdateAllItemVisuals();
    }

    public void HandleDropOnUninstallZone(GameObject droppedGameObject)
    {
        Debug.Log("=== MusicListController: HandleDropOnUninstallZone called ===");
        
        DraggableItem draggableItem = droppedGameObject.GetComponent<DraggableItem>();
        if (draggableItem == null || draggableItem.Data == null) 
        {
            Debug.LogWarning("DraggableItem or Data is null");
            return;
        }

        MusicData droppedMusicData = draggableItem.Data as MusicData;
        if (droppedMusicData == null)
        {
            Debug.LogWarning("Dropped item is not a valid MusicData.");
            return;
        }

        Debug.Log($"Dropped music data: {droppedMusicData.DisplayName}, FilePath: {droppedMusicData.FilePath}");

        // 通过SceneStatesManager删除音乐资源
        if (SceneStatesManager.Instance != null)
        {
            SceneStatesManager.Instance.RemoveMusicResource(droppedMusicData.ID);
            Debug.Log($"Requested uninstall for Music: {droppedMusicData.DisplayName}");
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
        Debug.LogWarning($"MusicListController.GetResourceInfoAt: Index {index} is out of bounds for internalResourceList count {internalResourceList.Count}.");
        return null;
    }

    // 激活资源（点击或其他方式）
    void ActivateResource(IResourceInfo resourceData)
    {
        if (!(resourceData is MusicData)) return;
        MusicData musicDataToActivate = resourceData as MusicData;

        Debug.Log($"Activating Music by click: {musicDataToActivate.DisplayName}");
        
        if (SceneStatesManager.Instance != null)
        {
            SceneStatesManager.Instance.SetActiveMusic(musicDataToActivate.ID);
        }
        else
        {
            Debug.LogError("SceneStatesManager.Instance is null!");
        }
    }

    void UpdateAllItemVisuals()
    {
        string activeMusicId = null;
        if (SceneStatesManager.Instance != null)
        {
            activeMusicId = SceneStatesManager.Instance.currentActiveMusicId;
        }

        for (int i = 0; i < uiListItemObjects.Count; i++)
        {
            GameObject uiItemGO = uiListItemObjects[i];
            DraggableItem draggable = uiItemGO.GetComponent<DraggableItem>();
            if (draggable != null && draggable.Data != null && draggable.Data is MusicData)
            {
                MusicData currentItemMusicData = draggable.Data as MusicData;
                bool isActive = (activeMusicId != null && activeMusicId == currentItemMusicData.ID);
                UpdateItemVisual(uiItemGO, currentItemMusicData, isActive);
            }
        }
    }

    void UpdateItemVisual(GameObject itemGO, MusicData musicData, bool isActive)
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
}