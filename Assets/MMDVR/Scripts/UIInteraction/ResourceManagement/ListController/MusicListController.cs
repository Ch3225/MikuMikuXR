using UnityEngine;
using System.Collections.Generic;
using TMPro;
using MMDVR.Scripts.Model; // for MusicData
using MMDVR.Scripts.Managers;
using UnityEngine.UI;

namespace MMDVR.Scripts.UIInteraction.ResourceManagement.ListController
{
    /// <summary>
    /// 音乐列表控制器 - 管理音乐资源的列表展示
    /// </summary>
    public class MusicListController : MonoBehaviour
    {
        public static MusicListController Instance { get; private set; }

        [Header("UI References")]
        public GameObject listItemPrefab;    public Transform listContainer;
        public DropZone uninstallDropZone;
        public DropZone enableDropZone;

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

            // 自动查找所有Uninstall类型DropZone并绑定
            DropZone[] allDropZones = FindObjectsOfType<DropZone>();
            foreach (DropZone dz in allDropZones)
            {
                if (dz.actionType == DropZone.DropActionType.PanelUninstall)
                {
                    dz.onItemDropped.AddListener(HandleDropOnUninstallZone);
                    Debug.Log($"MusicListController: Bound HandleDropOnUninstallZone to Uninstall DropZone: {dz.gameObject.name}");
                }
            }

            if (uninstallDropZone != null)
            {
                uninstallDropZone.onItemDropped.AddListener(HandleDropOnUninstallZone);
            }
            if (enableDropZone != null)
            {
                enableDropZone.onItemDropped.AddListener(HandleDropOnEnableZone);
            }            // 统一刷新机制：只通过事件刷新
            EventManager.OnMusicListChanged += RefreshResourceListUI;
            EventManager.OnMusicListChanged += NotifySceneDisplayManager;
            // 新增：监听音乐激活事件，自动刷新UI
            MMDVR.Events.ResourceEvents.OnMusicActivated += OnMusicActivated;
            // 启动时主动刷新一次
            RefreshResourceListUI();
        }

        void OnDestroy()
        {
            EventManager.OnMusicListChanged -= RefreshResourceListUI;
            EventManager.OnMusicListChanged -= NotifySceneDisplayManager;
            MMDVR.Events.ResourceEvents.OnMusicActivated -= OnMusicActivated;
            
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
        }    void RefreshResourceListUI()
        {
            Debug.Log($"MusicListController: RefreshResourceListUI called. Current UI items count: {uiListItemObjects.Count}");
            
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
                var musicDataList = SceneStatesManager.Instance.GetMusicDataList();
                Debug.Log($"MusicListController: Refreshed data. Music count: {musicDataList.Count}");
                foreach (var musicData in musicDataList)
                {
                    internalResourceList.Add(musicData);
                }
            }

            // 为每个资源创建UI项
            Debug.Log($"[DEBUG] 准备创建 {internalResourceList.Count} 个UI项");
            for (int i = 0; i < internalResourceList.Count; i++)
            {
                IResourceInfo resourceData = internalResourceList[i];
                GameObject listItemGO = Instantiate(listItemPrefab, listContainer);                // 确保DraggableItem组件存在
                DraggableItem draggableItem = listItemGO.GetComponent<DraggableItem>();
                if (draggableItem == null)
                {
                    draggableItem = listItemGO.AddComponent<DraggableItem>();
                }
                
                draggableItem.Data = resourceData;
                Debug.Log($"[MusicListController] 设置DraggableItem.Data: ID={resourceData.ID}, DisplayName={resourceData.DisplayName}, Type={resourceData.Type}");

                // Music类型不需要DropZone，因为音乐不接受其他资源的拖拽关联

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
        }        public void HandleDropOnUninstallZone(GameObject droppedGameObject)
        {
            // 添加空值检查，防止访问已销毁的GameObject
            if (droppedGameObject == null)
            {
                Debug.LogWarning("MusicListController: droppedGameObject is null or destroyed");
                return;
            }
            
            // 先缓存数据，避免销毁后访问
            DraggableItem draggableItem = droppedGameObject.GetComponent<DraggableItem>();
            var data = draggableItem != null ? draggableItem.Data : null;
            if (data == null)
            {
                Debug.LogWarning("MusicListController: DraggableItem or Data is null");
                return;
            }
            
            Debug.Log($"MusicListController: Processing item with type: {(data as IResourceInfo)?.Type}, id: {(data as IResourceInfo)?.ID}, 实际类型: {data.GetType().Name}");
            MusicData droppedMusicData = data as MusicData;
            if (droppedMusicData == null)
            {
                Debug.LogWarning($"MusicListController: Dropped item is not a valid MusicData. Actual type: {data.GetType().Name}");
                return;
            }
            Debug.Log($"MusicListController: Preparing to delete music: ID={droppedMusicData.ID}, DisplayName={droppedMusicData.DisplayName}");
            
            // 使用UserActionManager进行用户操作
            if (UserActionManager.Instance != null)
            {
                UserActionManager.Instance.UnloadMusic(droppedMusicData.ID, () => {
                    Debug.Log($"MusicListController: 音乐卸载完成 {droppedMusicData.DisplayName}");
                });
            }
            else
            {
                Debug.LogError("UserActionManager.Instance is null!");
            }
        }        public void HandleDropOnEnableZone(GameObject droppedGameObject)
        {
            // 添加空值检查，防止访问已销毁的GameObject
            if (droppedGameObject == null)
            {
                Debug.LogWarning("MusicListController: droppedGameObject is null or destroyed in HandleDropOnEnableZone");
                return;
            }
            DraggableItem draggableItem = droppedGameObject.GetComponent<DraggableItem>();
            if (draggableItem == null || draggableItem.Data == null)
                return;
            MusicData droppedMusicData = draggableItem.Data as MusicData;
            if (droppedMusicData == null)
                return;
            // 正确做法：直接激活音乐，不再加载
            if (UserActionManager.Instance != null)
            {
                UserActionManager.Instance.ActivateMusic(droppedMusicData.ID);
                Debug.Log($"MusicListController: 音乐激活完成 {droppedMusicData.DisplayName}");
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
            Debug.LogWarning($"MusicListController.GetResourceInfoAt: Index {index} is out of bounds for internalResourceList count {internalResourceList.Count}.");
            return null;
        }

        // 激活资源（点击或其他方式）
        void ActivateResource(IResourceInfo resourceData)
        {
            var musicDataToActivate = resourceData as MusicData;
            Debug.Log($"Activating Music by click: {musicDataToActivate.DisplayName}");
            // 通过UserActionManager执行激活
            if (UserActionManager.Instance != null)
            {
                UserActionManager.Instance.LoadMusic(musicDataToActivate.ID, id => {
                    UserActionManager.Instance.StartPlayback();
                });
            }
            else
            {
                Debug.LogError("UserActionManager.Instance is null!");
            }
        }

        public void UpdateAllItemVisuals()
        {
            string activeMusicId = null;
            // 获取当前播放的音乐ID
            if (SceneDisplayManager.Instance != null)
            {
                activeMusicId = SceneDisplayManager.Instance.GetCurrentActiveMusicId();
            }
            for (int i = 0; i < uiListItemObjects.Count; i++)
            {
                GameObject uiItemGO = uiListItemObjects[i];
                DraggableItem draggable = uiItemGO.GetComponent<DraggableItem>();
                if (draggable != null && draggable.Data != null && draggable.Data is MusicData)
                {
                    MusicData currentItemMusicData = draggable.Data as MusicData;
                    bool isActive = false;
                    isActive = (activeMusicId != null && activeMusicId == currentItemMusicData.ID);
                    UpdateItemVisual(uiItemGO, currentItemMusicData, isActive);
                }
            }
        }

        void UpdateItemVisual(GameObject itemGO, MusicData musicData, bool isActive)
        {
            UnityEngine.UI.Image bgImage = itemGO.GetComponent<UnityEngine.UI.Image>();
            if (bgImage != null)
            {                if (isActive)
                {
                    bgImage.color = Color.green; // 激活项为绿色
                }
                else
                {
                    bgImage.color = Color.white; // 非激活项为白色
                }
            }            Transform activeIndicator = itemGO.transform.Find("ActiveIndicator");            
            if (activeIndicator != null)
            {
                activeIndicator.gameObject.SetActive(isActive);
            }
        }

        // ==================== 公共查询接口 ====================

        /// <summary>
        /// 获取当前第一项（激活项）的音乐ID
        /// </summary>
        public string GetFirstMusicId()
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
        /// 获取所有音乐ID列表（按当前UI顺序）
        /// </summary>
        public List<string> GetMusicIdList()
        {
            var musicIds = new List<string>();
            if (listContainer != null)
            {
                for (int i = 0; i < listContainer.childCount; i++)
                {
                    var child = listContainer.GetChild(i);
                    var draggableItem = child.GetComponent<DraggableItem>();
                    if (draggableItem != null && draggableItem.Data != null)
                    {
                        musicIds.Add(draggableItem.Data.ID);
                    }
                }
            }
            return musicIds;
        }

        /// <summary>
        /// 检查指定音乐是否在列表中
        /// </summary>
        public bool HasMusic(string musicId)
        {
            return GetMusicIdList().Contains(musicId);
        }

        /// <summary>
        /// 获取指定音乐在列表中的位置
        /// </summary>
        public int GetMusicIndex(string musicId)
        {
            var musicIds = GetMusicIdList();
            return musicIds.IndexOf(musicId);
        }

        /// <summary>
        /// 强制刷新音乐列表数据和UI
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
                SceneDisplayManager.Instance.SyncActiveMusicWithList();
            }
        }

        // 音乐激活事件回调，刷新UI高亮
        private void OnMusicActivated(string musicId)
        {
            UpdateAllItemVisuals();
        }
    }
}