using UnityEngine;
using System.Collections.Generic;
using TMPro;
using MMDVR.Scripts.UIInteraction;
using MMDVR.Managers; // 确保 ModelData/MotionData/MusicData 类型引用

/// <summary>
/// 模型列表控制器 - 管理模型资源（resources）的列表展示和拖拽功能
/// </summary>
public class ModelListController : MonoBehaviour
{
    public static ModelListController Instance { get; private set; }

    [Header("UI References")]
    public GameObject listItemPrefab;
    public Transform listContainer;
    public DropZone listSortableAreaDropZone;
    public DropZone uninstallDropZone;
    public DropZone enableDropZone;
    public DropZone disableDropZone;
    public DropZone disconnectDropZone;

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
            Debug.LogError("ModelListController: UI References not set!");
            enabled = false;
            return;
        }        // 自动查找所有Uninstall类型DropZone并绑定
        DropZone[] allDropZones = FindObjectsOfType<DropZone>();
        foreach (DropZone dz in allDropZones)
        {
            if (dz.actionType == DropZone.DropActionType.Uninstall)
            {
                dz.onItemDropped.AddListener(HandleDropOnUninstallZone);
                Debug.Log($"ModelListController: Bound HandleDropOnUninstallZone to Uninstall DropZone: {dz.gameObject.name}");
            }
            else if (dz.actionType == DropZone.DropActionType.Disconnect)
            {
                dz.onItemDropped.AddListener(HandleDropOnDisconnectZone);
                Debug.Log($"ModelListController: Bound HandleDropOnDisconnectZone to Disconnect DropZone: {dz.gameObject.name}");
            }
        }// 自动查找所有EnableDisable类型DropZone并绑定
        // 注意：这里不再自动绑定，而是依赖Inspector中的具体配置
        // foreach (DropZone dz in allDropZones)
        // {
        //     if (dz.actionType == DropZone.DropActionType.EnableDisable)
        //     {
        //         dz.onItemDropped.AddListener(HandleDropOnEnableZone);
        //         Debug.Log($"ModelListController: Bound HandleDropOnEnableZone to EnableDropZone: {dz.gameObject.name}");
        //     }
        // }        if (listSortableAreaDropZone != null)
        {
            listSortableAreaDropZone.onItemDropped.AddListener(HandleDropOnListArea);
        }
        if (uninstallDropZone != null)
        {
            uninstallDropZone.onItemDropped.AddListener(HandleDropOnUninstallZone);
        }        if (enableDropZone != null)
        {
            enableDropZone.onItemDropped.AddListener(HandleDropOnToggleZone);
            Debug.Log($"Bound enableDropZone {enableDropZone.name} to HandleDropOnToggleZone");
        }
        // 暂时注释掉disableDropZone，避免重复调用
        // if (disableDropZone != null)
        // {
        //     disableDropZone.onItemDropped.AddListener(HandleDropOnToggleZone);
        // }
        if (disconnectDropZone != null)
        {
            disconnectDropZone.onItemDropped.AddListener(HandleDropOnDisconnectZone);
        }

        // 监听事件（修正为直接用 += 绑定静态事件）
        EventManager.OnActorListChanged += RefreshList;
        EventManager.OnMotionListChanged += UpdateAllItemVisuals;
        // 如有 ModelStateChanged、ModelMotionAssociationChanged 事件，也应在 EventManager 中声明为 public static Action
        // EventManager.OnModelStateChanged += UpdateAllItemVisuals;
        // EventManager.OnModelMotionAssociationChanged += UpdateAllItemVisuals;

        RefreshList();
    }

    void OnDestroy()
    {
        EventManager.OnActorListChanged -= RefreshList;
        EventManager.OnMotionListChanged -= UpdateAllItemVisuals;
        // EventManager.OnModelStateChanged -= UpdateAllItemVisuals;
        // EventManager.OnModelMotionAssociationChanged -= UpdateAllItemVisuals;
    }    public void RefreshList()
    {
        Debug.Log($"ModelListController: RefreshList called. Current UI items count: {uiListItemObjects.Count}");
        
        // 清除旧的UI项注册
        if (ConnectionManager.Instance != null)
        {
            foreach (var item in internalResourceList)
            {
                if (item is ModelData)
                {
                    ConnectionManager.Instance.UnregisterModelItem(item.ID);
                }
            }
        }
        
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
            var modelDataList = SceneStatesManager.Instance.GetModelList();
            Debug.Log($"ModelListController: Refreshed data. Model count: {modelDataList.Count}");
            foreach (var modelData in modelDataList)
            {
                internalResourceList.Add(modelData);
            }
        }

        // 为每个资源创建UI项
        for (int i = 0; i < internalResourceList.Count; i++)
        {
            IResourceInfo resourceData = internalResourceList[i];
            GameObject listItemGO = Instantiate(listItemPrefab, listContainer);
            listItemGO.name = resourceData.Type + "_Item_" + resourceData.DisplayName.Replace(" ", "");            DraggableItem draggableItem = listItemGO.GetComponent<DraggableItem>();
            if (draggableItem != null)
            {
                draggableItem.Data = resourceData;
                Debug.Log($"[ModelListController] 设置DraggableItem.Data: ID={resourceData.ID}, DisplayName={resourceData.DisplayName}, Type={resourceData.Type}");
            }
            else
            {
                Debug.LogError($"[ModelListController] listItemPrefab上没有DraggableItem组件！");
            }            // 配置DropZone用于接受动作拖拽
            DropZone dropZone = listItemGO.GetComponentInChildren<DropZone>();
            if (dropZone != null)
            {
                dropZone.actionType = DropZone.DropActionType.LinkToModel;
                dropZone.acceptedResourceTypes = new List<ResourceType> { ResourceType.Motion };
                var currentModelData = resourceData as ModelData;
                dropZone.onItemDropped.RemoveAllListeners();
                dropZone.onItemDropped.AddListener((draggedGO) => {
                    var draggedItem = draggedGO.GetComponent<DraggableItem>();
                    var motionData = draggedItem?.Data as MotionData;
                    if (motionData != null && currentModelData != null)
                    {
                        Debug.Log($"拖拽动作 {motionData.DisplayName} 到模型 {currentModelData.DisplayName}");
                        if (SceneStatesManager.Instance != null)
                        {
                            SceneStatesManager.Instance.AssignMotionToActor(motionData.ID, currentModelData.ID);
                            // 创建连线
                            if (ConnectionManager.Instance != null)
                            {
                                ConnectionManager.Instance.CreateConnection(currentModelData.ID, motionData.ID);
                            }
                        }
                    }
                });
            }

            // 向ConnectionManager注册此UI项
            if (ConnectionManager.Instance != null && resourceData is ModelData)
            {
                ConnectionManager.Instance.RegisterModelItem(resourceData.ID, listItemGO.GetComponent<RectTransform>());
            }

            TextMeshProUGUI titleText = listItemGO.GetComponentInChildren<TextMeshProUGUI>();
            if (titleText != null)
            {
                titleText.text = resourceData.DisplayName;
            }

            // 添加点击事件监听 - 模型不需要激活逻辑，但可以用于选择或其他操作
            UnityEngine.UI.Button button = listItemGO.GetComponent<UnityEngine.UI.Button>();
            if (button == null)
                button = listItemGO.AddComponent<UnityEngine.UI.Button>();
            
            var resourceDataCopy = resourceData;
            button.onClick.AddListener(() => SelectModel(resourceDataCopy));

            uiListItemObjects.Add(listItemGO);
        }

        UpdateAllItemVisuals();
        
        // 刷新连线显示
        if (ConnectionManager.Instance != null)
        {
            ConnectionManager.Instance.RefreshAllConnections();
        }
    }

    public void HandleDropOnListArea(GameObject droppedGameObject)
    {
        DraggableItem droppedItemComponent = droppedGameObject.GetComponent<DraggableItem>();
        if (droppedItemComponent == null || droppedItemComponent.Data == null) 
            return;

        // 支持模型列表内排序 和 动作拖到模型上建立关联
        if (droppedItemComponent.Data is ModelData)
        {
            // 重新构建内部列表基于UI顺序（列表排序）
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

            Debug.Log("模型列表重新排序");
            UpdateAllItemVisuals();
        }
        else if (droppedItemComponent.Data is MotionData)
        {
            // 动作拖到模型列表区域，需要确定具体拖到哪个模型上
            // 这需要更精确的拖拽检测，暂时简化为拖到第一个模型
            if (internalResourceList.Count > 0 && internalResourceList[0] is ModelData)
            {
                var motionData = droppedItemComponent.Data as MotionData;
                var modelData = internalResourceList[0] as ModelData;
                
                if (SceneStatesManager.Instance != null)
                {
                    SceneStatesManager.Instance.AssociateModelWithMotion(modelData.ID, motionData.ID);
                    Debug.Log($"关联动作 {motionData.DisplayName} 到模型 {modelData.DisplayName}");
                }
            }
        }
    }    public void HandleDropOnUninstallZone(GameObject droppedGameObject)
    {
        // 先缓存数据，避免销毁后访问
        DraggableItem draggableItem = droppedGameObject != null ? droppedGameObject.GetComponent<DraggableItem>() : null;
        var data = draggableItem != null ? draggableItem.Data : null;
        if (data == null)
        {
            Debug.LogWarning("ModelListController: DraggableItem or Data is null");
            return;
        }
        Debug.Log($"ModelListController: Processing item with type: {(data as IResourceInfo)?.Type}, id: {(data as IResourceInfo)?.ID}, 实际类型: {data.GetType().Name}");
        ModelData droppedModelData = data as ModelData;
        if (droppedModelData == null)
        {
            Debug.LogWarning($"ModelListController: Dropped item is not a valid ModelData. Actual type: {data.GetType().Name}");
            return;
        }
        Debug.Log($"ModelListController: Dropped model data: {droppedModelData.DisplayName}, FilePath: {droppedModelData.FilePath}");
        int beforeCount = internalResourceList.Count;
        Debug.Log($"Before deletion: Internal list count = {beforeCount}, UI objects count = {uiListItemObjects.Count}");
        if (SceneStatesManager.Instance != null)
        {
            SceneStatesManager.Instance.RemoveModelResource(droppedModelData.ID);
            Debug.Log($"ModelListController: Requested uninstall for Model: {droppedModelData.DisplayName}");
            Debug.Log("Model deletion request completed. UI should refresh via event system.");
        }
        else
        {
            Debug.LogError("SceneStatesManager.Instance is null!");
        }
    }    public void HandleDropOnToggleZone(GameObject droppedGameObject)
    {
        DraggableItem draggableItem = droppedGameObject.GetComponent<DraggableItem>();
        if (draggableItem == null || draggableItem.Data == null) 
            return;

        ModelData droppedModelData = draggableItem.Data as ModelData;
        if (droppedModelData == null)
            return;

        if (SceneStatesManager.Instance != null)
        {
            SceneStatesManager.Instance.ToggleModel(droppedModelData.ID);
            Debug.Log($"Toggled Model: {droppedModelData.DisplayName}");
        }
    }

    public void HandleDropOnDisconnectZone(GameObject droppedGameObject)
    {
        // 防御：对象已被销毁或为null时直接返回，避免MissingReferenceException
        if (droppedGameObject == null || droppedGameObject.Equals(null)) return;
        DraggableItem draggableItem = droppedGameObject.GetComponent<DraggableItem>();
        if (draggableItem == null || draggableItem.Data == null) 
            return;

        ModelData droppedModelData = draggableItem.Data as ModelData;
        if (droppedModelData == null)
            return;

        if (SceneStatesManager.Instance != null)
        {
            SceneStatesManager.Instance.DisconnectAllModelAssociations(droppedModelData.ID);
            // 断开后强制刷新模型动作（让模型重新读入动作）
            SceneStatesManager.Instance.ReloadModelMotions(droppedModelData.ID);
            Debug.Log($"Disconnected all associations for Model: {droppedModelData.DisplayName}，并强制刷新模型动作");
        }
        // 强制刷新UI
        RefreshList();
        // 强制更新连线显示
        if (ConnectionManager.Instance != null)
        {
            ConnectionManager.Instance.RebuildAllConnections();
        }
    }

    // 通过索引获取资源信息，用于向后兼容
    public IResourceInfo GetResourceInfoAt(int index)
    {
        if (index >= 0 && index < internalResourceList.Count)
        {
            return internalResourceList[index];
        }
        Debug.LogWarning($"ModelListController.GetResourceInfoAt: Index {index} is out of bounds for internalResourceList count {internalResourceList.Count}.");
        return null;
    }

    // 选择模型（点击或其他方式）
    void SelectModel(IResourceInfo resourceData)
    {
        if (!(resourceData is ModelData)) return;
        ModelData modelDataToSelect = resourceData as ModelData;

        Debug.Log($"Selecting Model: {modelDataToSelect.DisplayName}");
        
        // 模型选择逻辑可以在这里添加，比如高亮显示等
        // 暂时只是日志输出
    }

    void UpdateAllItemVisuals()
    {
        HashSet<string> disabledModelIds = new HashSet<string>();
        Dictionary<string, List<string>> modelMotionAssociations = new Dictionary<string, List<string>>();
        
        if (SceneStatesManager.Instance != null)
        {
            // 获取禁用的模型ID列表和模型-动作关联
            for (int i = 0; i < internalResourceList.Count; i++)
            {
                if (internalResourceList[i] is ModelData modelData)
                {
                    if (SceneStatesManager.Instance.IsModelDisabled(modelData.ID))
                    {
                        disabledModelIds.Add(modelData.ID);
                    }
                }
            }
        }

        for (int i = 0; i < uiListItemObjects.Count; i++)
        {
            GameObject uiItemGO = uiListItemObjects[i];
            DraggableItem draggable = uiItemGO.GetComponent<DraggableItem>();
            if (draggable != null && draggable.Data != null && draggable.Data is ModelData)
            {
                ModelData currentItemModelData = draggable.Data as ModelData;
                bool isDisabled = disabledModelIds.Contains(currentItemModelData.ID);
                UpdateItemVisual(uiItemGO, currentItemModelData, isDisabled);
            }
        }
    }

    void UpdateItemVisual(GameObject itemGO, ModelData modelData, bool isDisabled)
    {
        UnityEngine.UI.Image bgImage = itemGO.GetComponent<UnityEngine.UI.Image>();
        if (bgImage != null)
        {
            if (isDisabled)
            {
                bgImage.color = Color.gray; // 禁用状态为灰色
            }
            else
            {
                bgImage.color = Color.white; // 正常状态为白色
            }
        }

        // 可以添加其他视觉指示器，如关联数量标记等
        Transform associationIndicator = itemGO.transform.Find("AssociationIndicator");
        if (associationIndicator != null)
        {
            // 显示关联的动作数量等信息
            associationIndicator.gameObject.SetActive(!isDisabled);
        }
    }
}
