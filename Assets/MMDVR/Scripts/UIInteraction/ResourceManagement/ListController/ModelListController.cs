using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using MMDVR.Scripts.UIInteraction;
using MMDVR.Scripts.Model;
using MMDVR.Scripts.Managers; // Manager类引用
using MMDVR.Scripts.Components; // Component类引用
using MMDVR.Scripts.UIInteraction.ResourceManagement.ConnectionManagement; // for ConnectionManager
using UnityEngine.EventSystems;
using UnityEngine.UI; // 新增：用于强制刷新布局
using MMDVR.Events; // 新增：用于ResourceEvents

namespace MMDVR.Scripts.UIInteraction.ResourceManagement.ListController
{
    /// <summary>
    /// 模型列表控制器 - 管理模型资源（resources）的列表展示和拖拽功能
    /// </summary>
    public class ModelListController : MonoBehaviour
{
    public static ModelListController Instance { get; private set; }

    [Header("UI References")]
    public GameObject listItemPrefab;
    public Transform listContainer;
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
            if (dz.actionType == DropZone.DropActionType.PanelUninstall)
            {
                dz.onItemDropped.AddListener(HandleDropOnUninstallZone);
                Debug.Log($"ModelListController: Bound HandleDropOnUninstallZone to Uninstall DropZone: {dz.gameObject.name}");
            }
            else if (dz.actionType == DropZone.DropActionType.PanelDisconnect)
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
        // }        if (uninstallDropZone != null)
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
        }        // 监听基础事件（保持原有逻辑）        EventManager.OnActorListChanged += RefreshList;
        EventManager.OnMotionListChanged += UpdateAllItemVisuals;
        ResourceEvents.OnModelListChanged += RefreshList; // 监听模型列表变化
        // 预留：监听模型激活事件，后续如有高亮需求可直接刷新
        // ResourceEvents.OnModelActivated += OnModelActivated;
        RefreshList();
    }

    void OnDestroy()
    {
        // 取消订阅基础事件
        EventManager.OnActorListChanged -= RefreshList;
        EventManager.OnMotionListChanged -= UpdateAllItemVisuals;
        ResourceEvents.OnModelListChanged -= RefreshList; // 取消监听模型列表变化
        // ResourceEvents.OnModelActivated -= OnModelActivated;
    }

    // 预留：模型激活事件回调
    // private void OnModelActivated(string modelId)
    // {
    //     UpdateAllItemVisuals();
    // }
    public void RefreshList()
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
        }        // 重新获取最新数据
        if (ResourceManager.Instance != null)
        {
            internalResourceList.Clear();
            var modelList = ResourceManager.Instance.GetModelDataList(); // 使用现有方法
            Debug.Log($"ModelListController: Refreshed data. Model count: {modelList.Count}");
            foreach (var model in modelList)
            {
                internalResourceList.Add(model);
            }
        }

        // 为每个资源创建UI项
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
            Debug.Log($"[ModelListController] 设置DraggableItem.Data: ID={resourceData.ID}, DisplayName={resourceData.DisplayName}, Type={resourceData.Type}");            // 为Model类型动态添加DropZone组件
            DropZone dropZone = listItemGO.GetComponent<DropZone>();
            if (dropZone == null)
            {
                dropZone = listItemGO.AddComponent<DropZone>();
            }
              // 确保onItemDropped事件已初始化
            if (dropZone.onItemDropped == null)
            {
                dropZone.onItemDropped = new GameObjectUnityEvent();
            }
            
            // 配置DropZone用于接受动作拖拽
            dropZone.actionType = DropZone.DropActionType.MotionLinkToModel;
            dropZone.acceptedResourceTypes = new List<ResourceType> { ResourceType.Motion };
            var currentModelData = resourceData as ModelData;
            dropZone.onItemDropped.RemoveAllListeners();
            dropZone.onItemDropped.AddListener((draggedGO) => {
                var draggedItem = draggedGO.GetComponent<DraggableItem>();
                var motionData = draggedItem?.Data as MotionData;

                if (motionData != null && currentModelData != null)
                {
                    Debug.Log($"拖拽动作 {motionData.DisplayName} 到模型 {currentModelData.DisplayName}");
                    // 使用新的AssociationManager进行关联
                    if (AssociationManager.Instance != null)
                    {
                        AssociationManager.Instance.AssociateModelWithMotion(currentModelData.ID, motionData.ID);
                        // 创建连线
                        if (ConnectionManager.Instance != null)
                        {
                            ConnectionManager.Instance.CreateConnection(currentModelData.ID, motionData.ID);
                        }
                    }
                }
            });

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
        // 强制刷新布局
        if (listContainer is RectTransform rect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
    }    public void HandleDropOnUninstallZone(GameObject droppedGameObject)
    {
        if (droppedGameObject == null || droppedGameObject.Equals(null)) return;
        DraggableItem draggableItem = droppedGameObject.GetComponent<DraggableItem>();
        if (draggableItem == null || draggableItem.Data == null) 
            return;
        ModelData droppedModelData = draggableItem.Data as ModelData;
        if (droppedModelData == null)
            return;
        // 使用UserActionManager进行用户操作
        if (UserActionManager.Instance != null)
        {
            UserActionManager.Instance.UnloadModel(droppedModelData.ID, () => {
                Debug.Log($"ModelListController: 模型卸载完成 {droppedModelData.DisplayName}");
                // 其余刷新由 UserActionManager 保证
            });
        }
        else
        {
            Debug.LogError("UserActionManager.Instance is null!");
        }
    }    public void HandleDropOnToggleZone(GameObject droppedGameObject)
    {
        // 添加空值检查，防止访问已销毁的GameObject
        if (droppedGameObject == null)
        {
            Debug.LogWarning("ModelListController: droppedGameObject is null or destroyed in HandleDropOnToggleZone");
            return;
        }
        
        DraggableItem draggableItem = droppedGameObject.GetComponent<DraggableItem>();
        if (draggableItem == null || draggableItem.Data == null) 
            return;

        ModelData droppedModelData = draggableItem.Data as ModelData;
        if (droppedModelData == null)
            return;

        // 使用UserActionManager进行用户操作
        if (UserActionManager.Instance != null)
        {
            SceneDisplayManager.Instance.ToggleActorVisibility(droppedModelData.ID);
        }
        else
        {
            Debug.LogError("UserActionManager.Instance is null!");
        }
    }    public void HandleDropOnDisconnectZone(GameObject droppedGameObject)
    {
        if (droppedGameObject == null || droppedGameObject.Equals(null)) return;
        DraggableItem draggableItem = droppedGameObject.GetComponent<DraggableItem>();
        if (draggableItem == null || draggableItem.Data == null) 
            return;

        ModelData droppedModelData = draggableItem.Data as ModelData;
        if (droppedModelData == null)
            return;

        // 使用UserActionManager进行用户操作
        if (UserActionManager.Instance != null)
        {
            UserActionManager.Instance.UnlinkAllMotionsFromActor(droppedModelData.ID, () => {
                Debug.Log($"ModelListController: 模型关联断开完成 {droppedModelData.DisplayName}");
                
                // 刷新UI和连线
                RefreshList();
                if (ConnectionManager.Instance != null)
                {
                    ConnectionManager.Instance.RebuildConnectionsForModel(droppedModelData.ID);
                    ConnectionManager.Instance.RefreshConnectionEndPoints();
                }
                
                // 清除UI选择，防止意外高亮
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
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
        {            // 获取禁用的模型ID列表 - 检查ModelComponent的状态
            for (int i = 0; i < internalResourceList.Count; i++)
            {
                if (internalResourceList[i] is ModelData modelData)
                {
                    // 通过检查ModelComponent的isEnabled状态来判断模型是否禁用
                    var modelComponent = GetModelComponentById(modelData.ID);
                    if (modelComponent != null && !modelComponent.IsEnabled)
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
            associationIndicator.gameObject.SetActive(!isDisabled);        }
    }

    /// <summary>
    /// 根据ID获取ModelComponent
    /// </summary>
    private ModelComponent GetModelComponentById(string modelId)
    {
        if (ResourceManager.Instance?.modelContainer == null) return null;
        var modelComponents = ResourceManager.Instance.modelContainer.GetComponentsInChildren<ModelComponent>();        return modelComponents.FirstOrDefault(m => m.id == modelId);
    }
}
}
