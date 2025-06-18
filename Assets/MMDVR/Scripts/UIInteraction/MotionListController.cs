using UnityEngine;
using System.Collections.Generic;
using TMPro;
using MMDVR.Scripts.UIInteraction;
using MMDVR.Managers;
using UnityEngine.EventSystems;

/// <summary>
/// 动作列表控制器 - 管理动作资源的列表展示和拖拽功能
/// </summary>
public class MotionListController : MonoBehaviour
{
    public static MotionListController Instance { get; private set; }

    [Header("UI References")]
    public GameObject listItemPrefab;
    public Transform listContainer;
    public DropZone listSortableAreaDropZone;
    public DropZone uninstallDropZone;
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
            Debug.LogError("MotionListController: UI References not set!");
            enabled = false;
            return;
        }        // 自动查找所有Uninstall类型DropZone并绑定
        DropZone[] allDropZones = FindObjectsOfType<DropZone>();
        foreach (DropZone dz in allDropZones)
        {
            if (dz.actionType == DropZone.DropActionType.Uninstall)
            {
                dz.onItemDropped.AddListener(HandleDropOnUninstallZone);
                Debug.Log($"MotionListController: Bound HandleDropOnUninstallZone to Uninstall DropZone: {dz.gameObject.name}");
            }
            else if (dz.actionType == DropZone.DropActionType.Disconnect)
            {
                dz.onItemDropped.AddListener(HandleDropOnDisconnectZone);
                Debug.Log($"MotionListController: Bound HandleDropOnDisconnectZone to Disconnect DropZone: {dz.gameObject.name}");
            }
        }

        if (listSortableAreaDropZone != null)
        {
            listSortableAreaDropZone.onItemDropped.AddListener(HandleDropOnListArea);
        }
        if (uninstallDropZone != null)
        {
            uninstallDropZone.onItemDropped.AddListener(HandleDropOnUninstallZone);
        }
        if (disconnectDropZone != null)
        {
            disconnectDropZone.onItemDropped.AddListener(HandleDropOnDisconnectZone);
        }

        // 监听事件（修正为直接用 += 绑定静态事件）
        EventManager.OnMotionListChanged += RefreshList;
        // 如有 ModelMotionAssociationChanged 事件，也应在 EventManager 中声明为 public static Action
        // EventManager.OnModelMotionAssociationChanged += UpdateAllItemVisuals;

        RefreshList();
    }

    void OnDestroy()
    {
        EventManager.OnMotionListChanged -= RefreshList;
        // EventManager.OnModelMotionAssociationChanged -= UpdateAllItemVisuals;
    }    public void RefreshList()
    {
        Debug.Log($"MotionListController: RefreshList called. Current UI items count: {uiListItemObjects.Count}");
        
        // 清除旧的UI项注册
        if (ConnectionManager.Instance != null)
        {
            foreach (var item in internalResourceList)
            {
                if (item is MotionData)
                {
                    ConnectionManager.Instance.UnregisterMotionItem(item.ID);
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
            var motionDataList = SceneStatesManager.Instance.GetMotionDataList();
            Debug.Log($"MotionListController: Refreshed data. Motion count: {motionDataList.Count}");
            foreach (var motionData in motionDataList)
            {
                internalResourceList.Add(motionData);
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
            }            // 配置DropZone用于接受模型拖拽
            DropZone dropZone = listItemGO.GetComponentInChildren<DropZone>();
            if (dropZone != null)
            {
                dropZone.actionType = DropZone.DropActionType.LinkToMotion;
                dropZone.acceptedResourceTypes = new List<ResourceType> { ResourceType.Model };
                var currentMotionData = resourceData as MotionData;
                dropZone.onItemDropped.RemoveAllListeners();
                dropZone.onItemDropped.AddListener((draggedGO) => {
                    var draggedItem = draggedGO.GetComponent<DraggableItem>();
                    var modelData = draggedItem?.Data as ModelData;
                    if (modelData != null && currentMotionData != null)
                    {
                        Debug.Log($"拖拽模型 {modelData.DisplayName} 到动作 {currentMotionData.DisplayName}");
                        if (SceneStatesManager.Instance != null)
                        {
                            SceneStatesManager.Instance.AssignMotionToActor(currentMotionData.ID, modelData.ID);
                            // 创建连线
                            if (ConnectionManager.Instance != null)
                            {
                                ConnectionManager.Instance.CreateConnection(modelData.ID, currentMotionData.ID);
                            }
                        }
                    }
                });
            }

            // 向ConnectionManager注册此UI项
            if (ConnectionManager.Instance != null && resourceData is MotionData)
            {
                ConnectionManager.Instance.RegisterMotionItem(resourceData.ID, listItemGO.GetComponent<RectTransform>());
            }

            TextMeshProUGUI titleText = listItemGO.GetComponentInChildren<TextMeshProUGUI>();
            if (titleText != null)
            {
                titleText.text = resourceData.DisplayName;
            }

            // 添加点击事件监听 - 动作不需要激活逻辑，但可以用于预览或其他操作
            UnityEngine.UI.Button button = listItemGO.GetComponent<UnityEngine.UI.Button>();
            if (button == null)
                button = listItemGO.AddComponent<UnityEngine.UI.Button>();
            
            var resourceDataCopy = resourceData;
            button.onClick.AddListener(() => SelectMotion(resourceDataCopy));

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

        // 支持动作列表内排序 和 模型拖到动作上建立关联
        if (droppedItemComponent.Data is MotionData)
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

            Debug.Log("动作列表重新排序");
            UpdateAllItemVisuals();
        }
        else if (droppedItemComponent.Data is ModelData)
        {
            // 模型拖到动作列表区域，需要确定具体拖到哪个动作上
            // 这需要更精确的拖拽检测，暂时简化为拖到第一个动作
            if (internalResourceList.Count > 0 && internalResourceList[0] is MotionData)
            {
                var modelData = droppedItemComponent.Data as ModelData;
                var motionData = internalResourceList[0] as MotionData;
                
                if (SceneStatesManager.Instance != null)
                {
                    SceneStatesManager.Instance.AssociateModelWithMotion(modelData.ID, motionData.ID);
                    Debug.Log($"关联模型 {modelData.DisplayName} 到动作 {motionData.DisplayName}");
                }
            }
        }
    }

    public void HandleDropOnUninstallZone(GameObject droppedGameObject)
    {
        // 先缓存数据，避免销毁后访问
        DraggableItem draggableItem = droppedGameObject != null ? droppedGameObject.GetComponent<DraggableItem>() : null;
        var data = draggableItem != null ? draggableItem.Data : null;
        if (data == null)
        {
            Debug.LogWarning("MotionListController: DraggableItem or Data is null");
            return;
        }
        Debug.Log($"MotionListController: Processing item with type: {(data as IResourceInfo)?.Type}, id: {(data as IResourceInfo)?.ID}, 实际类型: {data.GetType().Name}");
        MotionData droppedMotionData = data as MotionData;
        if (droppedMotionData == null)
        {
            Debug.LogWarning($"MotionListController: Dropped item is not a valid MotionData. Actual type: {data.GetType().Name}");
            return;
        }
        Debug.Log($"MotionListController: Dropped motion data: {droppedMotionData.DisplayName}, FilePath: {droppedMotionData.FilePath}");
        int beforeCount = internalResourceList.Count;
        Debug.Log($"Before deletion: Internal list count = {beforeCount}, UI objects count = {uiListItemObjects.Count}");
        if (SceneStatesManager.Instance != null)
        {
            SceneStatesManager.Instance.RemoveMotionResource(droppedMotionData.ID);
            Debug.Log($"MotionListController: Requested uninstall for Motion: {droppedMotionData.DisplayName}");
            Debug.Log("Motion deletion request completed. UI should refresh via event system.");
        }
        else
        {
            Debug.LogError("SceneStatesManager.Instance is null!");
        }
    }

    public void HandleDropOnDisconnectZone(GameObject droppedGameObject)
    {
        if (droppedGameObject == null || droppedGameObject.Equals(null)) return;
        DraggableItem draggableItem = droppedGameObject.GetComponent<DraggableItem>();
        if (draggableItem == null || draggableItem.Data == null) 
            return;

        MotionData droppedMotionData = draggableItem.Data as MotionData;
        if (droppedMotionData == null)
            return;

        if (SceneStatesManager.Instance != null)
        {            // 1. 找到所有挂载该动作的模型
            var affectedModels = SceneStatesManager.Instance.GetAssociatedModels(droppedMotionData.ID);
            Debug.Log($"断开动作 {droppedMotionData.ID}，找到受影响模型数量: {affectedModels.Count}");
            
            // 调试：打印当前所有关联关系
            var allModels = SceneStatesManager.Instance.GetModelList();
            Debug.Log($"当前系统中共有 {allModels.Count} 个模型");
            foreach (var model in allModels)
            {
                var motions = SceneStatesManager.Instance.GetModelAssociatedMotions(model.ID);
                Debug.Log($"模型 {model.ID}({model.DisplayName}) 关联的动作数量: {motions.Count}");
                foreach (var motionId in motions)
                {
                    if (motionId == droppedMotionData.ID)
                    {
                        Debug.Log($"  找到关联: 模型{model.ID} <-> 动作{motionId}");
                    }
                }
            }
            
            foreach (var modelId in affectedModels)
            {
                Debug.Log($"处理受影响模型: {modelId}");
                // 只断开该动作与模型的关联
                SceneStatesManager.Instance.DisassociateModelFromMotion(modelId, droppedMotionData.ID);                // 让该模型的actor重新加载动作
                var actorObj = SceneStatesManager.Instance.GetActorObjectById(modelId);
                Debug.Log($"查找模型 {modelId} 的actor对象: {(actorObj != null ? "找到" : "未找到")}");
                
                // 如果直接查找失败，尝试遍历所有actor查找
                if (actorObj == null)
                {
                    var actorList = SceneStatesManager.Instance.GetActorList();
                    Debug.Log($"尝试从 {actorList.Count} 个actor中查找匹配的actor");
                    foreach (var actor in actorList)
                    {
                        Debug.Log($"  Actor: id={actor.id}, displayName={actor.displayName}");
                        if (actor.id == modelId)
                        {
                            actorObj = SceneStatesManager.Instance.GetActorObjectById(actor.id);
                            Debug.Log($"  找到匹配的actor: {actorObj?.name}");
                            break;
                        }
                    }
                }
                
                if (actorObj != null)
                {
                    var mmdGameObject = actorObj.GetComponent<LibMMD.Unity3D.MmdGameObject>();
                    Debug.Log($"模型 {modelId} 的MmdGameObject: {(mmdGameObject != null ? "找到" : "未找到")}");
                    if (mmdGameObject != null)
                    {
                        // 如果还有剩余动作，加载第一个；否则通过加载空路径重置为T姿势
                        var motions = SceneStatesManager.Instance.GetModelAssociatedMotions(modelId);
                        Debug.Log($"模型 {modelId} 剩余动作数量: {motions.Count}");
                        if (motions.Count > 0)
                        {
                            var motionComponent = SceneStatesManager.Instance.GetMotionComponentById(motions[0]);
                            if (motionComponent != null && !string.IsNullOrEmpty(motionComponent.filePath))
                            {
                                Debug.Log($"模型 {modelId} 加载动作: {motionComponent.filePath}");
                                mmdGameObject.LoadMotion(motionComponent.filePath);
                            }
                        }
                        else
                        {
                            // 没有动作时，重置为T姿势
                            Debug.Log($"[MotionListController] Model {modelId} has no more motions. Calling ResetToTPose().");
                            mmdGameObject.ResetToTPose();
                        }
                    }
                    else
                    {
                        Debug.LogError($"[MotionListController] MmdGameObject component NOT FOUND on actor for model ID {modelId}");
                    }
                }
                else
                {
                    Debug.LogError($"[MotionListController] GetActorObjectById returned NULL for model ID {modelId}");
                }
            }            Debug.Log($"仅断开所有模型对动作 {droppedMotionData.DisplayName} 的关联，并刷新受影响模型的动作");
        }        // 先清理相关连线再刷新UI
        if (ConnectionManager.Instance != null)
        {
            ConnectionManager.Instance.RebuildConnectionsForMotion(droppedMotionData.ID);
        }
        RefreshList();
        // 刷新连线端点引用
        if (ConnectionManager.Instance != null)
        {
            ConnectionManager.Instance.RefreshConnectionEndPoints();
        }
        
        // 清除UI选择，防止意外高亮
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    // 通过索引获取资源信息，用于向后兼容
    public IResourceInfo GetResourceInfoAt(int index)
    {
        if (index >= 0 && index < internalResourceList.Count)
        {
            return internalResourceList[index];
        }
        Debug.LogWarning($"MotionListController.GetResourceInfoAt: Index {index} is out of bounds for internalResourceList count {internalResourceList.Count}.");
        return null;
    }

    // 选择动作（点击或其他方式）
    void SelectMotion(IResourceInfo resourceData)
    {
        if (!(resourceData is MotionData)) return;
        MotionData motionDataToSelect = resourceData as MotionData;

        Debug.Log($"Selecting Motion: {motionDataToSelect.DisplayName}");
        
        // 动作选择逻辑可以在这里添加，比如预览、高亮显示等
        // 暂时只是日志输出
    }

    void UpdateAllItemVisuals()
    {
        Dictionary<string, List<string>> modelMotionAssociations = new Dictionary<string, List<string>>();
        
        // 这里可以获取关联信息来显示哪些动作已经被关联
        // 由于关联是存储在 modelMotionAssociations (Map<Model, List<Motion>>) 中
        // 我们需要反向查找哪些动作被关联了

        for (int i = 0; i < uiListItemObjects.Count; i++)
        {
            GameObject uiItemGO = uiListItemObjects[i];
            DraggableItem draggable = uiItemGO.GetComponent<DraggableItem>();
            if (draggable != null && draggable.Data != null && draggable.Data is MotionData)
            {
                MotionData currentItemMotionData = draggable.Data as MotionData;
                bool isAssociated = IsMotionAssociated(currentItemMotionData.ID);
                UpdateItemVisual(uiItemGO, currentItemMotionData, isAssociated);
            }
        }
    }    bool IsMotionAssociated(string motionId)
    {
        // 检查是否有任何模型关联了这个动作
        if (SceneStatesManager.Instance != null)
        {
            var modelList = SceneStatesManager.Instance.GetModelList();
            foreach (var model in modelList)
            {
                var associations = SceneStatesManager.Instance.GetModelAssociatedMotions(model.ID);
                if (associations != null && associations.Contains(motionId))
                {
                    return true;
                }
            }
        }
        return false;
    }

    void UpdateItemVisual(GameObject itemGO, MotionData motionData, bool isAssociated)
    {
        UnityEngine.UI.Image bgImage = itemGO.GetComponent<UnityEngine.UI.Image>();
        if (bgImage != null)
        {
            if (isAssociated)
            {
                bgImage.color = Color.green; // 已关联的动作为绿色
            }
            else
            {
                bgImage.color = Color.white; // 未关联的动作为白色
            }
        }

        // 可以添加其他视觉指示器
        Transform associationIndicator = itemGO.transform.Find("AssociationIndicator");
        if (associationIndicator != null)
        {
            associationIndicator.gameObject.SetActive(isAssociated);
        }
    }
}
