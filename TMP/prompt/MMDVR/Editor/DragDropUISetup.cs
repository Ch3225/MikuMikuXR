using UnityEngine;
using UnityEditor;
using MMDVR.Scripts.UIInteraction;

namespace MMDVR.Editor
{
    /// <summary>
    /// Unity编辑器脚本，用于自动配置拖拽UI系统
    /// </summary>
    public class DragDropUISetup : EditorWindow
    {
        [MenuItem("MMDVR/Setup Drag Drop UI")]
        static void SetupDragDropUI()
        {
            Debug.Log("开始设置拖拽UI系统...");
            
            // 查找现有的UI元素
            SetupModelList();
            CreateMotionList();
            SetupDropZones();
            
            Debug.Log("拖拽UI系统设置完成！");
        }
        
        static void SetupModelList()
        {
            // 查找ModelList GameObject (第一个，作为拖拽列表容器)
            GameObject modelListGO = GameObject.Find("ModelList");
            if (modelListGO == null)
            {
                Debug.LogError("未找到ModelList GameObject");
                return;
            }
            
            // 检查是否已有ModelListController
            ModelListController controller = modelListGO.GetComponent<ModelListController>();
            if (controller == null)
            {
                controller = modelListGO.AddComponent<ModelListController>();
                Debug.Log("为ModelList添加了ModelListController组件");
            }
            
            // 设置prefab引用
            string prefabPath = "Assets/MMDVR/UI/Prefabs/MainUI/ResourceManagement/GenericListItemCard.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                controller.listItemPrefab = prefab;
                Debug.Log("设置了listItemPrefab引用");
            }
            
            // 设置listContainer (ModelList本身)
            controller.listContainer = modelListGO.transform;
            
            // 查找或创建DropZone组件
            DropZone listDropZone = modelListGO.GetComponent<DropZone>();
            if (listDropZone == null)
            {
                listDropZone = modelListGO.AddComponent<DropZone>();
            }
            listDropZone.actionType = DropZone.DropActionType.ListSort;
            listDropZone.acceptedResourceTypes.Clear();
            listDropZone.acceptedResourceTypes.Add(ResourceType.Model);
            
            controller.listSortableAreaDropZone = listDropZone;
            
            Debug.Log("ModelList配置完成");
        }
        
        static void CreateMotionList()
        {
            // 查找或创建MotionList
            GameObject motionListGO = GameObject.Find("MotionList");
            if (motionListGO == null)
            {
                // 在UniversalListArea下创建MotionList
                GameObject universalListArea = GameObject.Find("UniversalListArea");
                if (universalListArea != null)
                {
                    motionListGO = new GameObject("MotionList");
                    motionListGO.transform.SetParent(universalListArea.transform);
                    
                    // 添加必要的UI组件
                    var rectTransform = motionListGO.AddComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(0, 1);
                    rectTransform.anchoredPosition = new Vector2(300, -28);
                    rectTransform.sizeDelta = new Vector2(200, 0);
                    
                    var verticalLayoutGroup = motionListGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                    verticalLayoutGroup.spacing = 8;
                    verticalLayoutGroup.childControlHeight = false;
                    verticalLayoutGroup.childForceExpandHeight = false;
                    
                    var contentSizeFitter = motionListGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                    contentSizeFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                    
                    Debug.Log("创建了MotionList GameObject");
                }
            }
            
            if (motionListGO != null)
            {
                // 添加MotionListController
                MotionListController controller = motionListGO.GetComponent<MotionListController>();
                if (controller == null)
                {
                    controller = motionListGO.AddComponent<MotionListController>();
                    Debug.Log("为MotionList添加了MotionListController组件");
                }
                
                // 设置prefab引用
                string prefabPath = "Assets/MMDVR/UI/Prefabs/MainUI/ResourceManagement/GenericListItemCard.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    controller.listItemPrefab = prefab;
                }
                
                controller.listContainer = motionListGO.transform;
                
                // 添加DropZone
                DropZone listDropZone = motionListGO.GetComponent<DropZone>();
                if (listDropZone == null)
                {
                    listDropZone = motionListGO.AddComponent<DropZone>();
                }
                listDropZone.actionType = DropZone.DropActionType.ListSort;
                listDropZone.acceptedResourceTypes.Clear();
                listDropZone.acceptedResourceTypes.Add(ResourceType.Motion);
                
                controller.listSortableAreaDropZone = listDropZone;
                
                Debug.Log("MotionList配置完成");
            }
        }
        
        static void SetupDropZones()
        {
            // 查找并配置现有的DropZone
            SetupUninstallDropZone();
            SetupEnableDropZone();
            SetupDisconnectDropZone();
        }
        
        static void SetupUninstallDropZone()
        {
            GameObject uninstallZone = GameObject.Find("DropzoneUnload");
            if (uninstallZone != null)
            {
                DropZone dropZone = uninstallZone.GetComponent<DropZone>();
                if (dropZone == null)
                {
                    dropZone = uninstallZone.AddComponent<DropZone>();
                }
                  dropZone.actionType = DropZone.DropActionType.Uninstall;
                dropZone.acceptedResourceTypes.Clear();
                // 接受所有类型（Actor和Model是同一回事，都使用ResourceType.Model）
                dropZone.acceptedResourceTypes.Add(ResourceType.Model);
                dropZone.acceptedResourceTypes.Add(ResourceType.Motion);
                dropZone.acceptedResourceTypes.Add(ResourceType.Music);
                dropZone.acceptedResourceTypes.Add(ResourceType.Camera);
                
                Debug.Log("配置了Uninstall DropZone - 接受Model, Motion, Music, Camera类型");
            }
        }
        
        static void SetupEnableDropZone()
        {
            GameObject enableZone = GameObject.Find("DropZoneEnabling");
            if (enableZone != null)
            {
                DropZone dropZone = enableZone.GetComponent<DropZone>();
                if (dropZone == null)
                {
                    dropZone = enableZone.AddComponent<DropZone>();
                }
                
                dropZone.actionType = DropZone.DropActionType.EnableDisable;
                dropZone.acceptedResourceTypes.Clear();
                dropZone.acceptedResourceTypes.Add(ResourceType.Model);
                dropZone.acceptedResourceTypes.Add(ResourceType.Music);
                dropZone.acceptedResourceTypes.Add(ResourceType.Camera);
                
                Debug.Log("配置了Enable DropZone");
            }
        }
          static void SetupDisconnectDropZone()
        {
            GameObject disconnectZone = GameObject.Find("DropZoneDisconnect");
            if (disconnectZone != null)
            {
                DropZone dropZone = disconnectZone.GetComponent<DropZone>();
                if (dropZone == null)
                {
                    dropZone = disconnectZone.AddComponent<DropZone>();
                }
                
                dropZone.actionType = DropZone.DropActionType.Disconnect;
                dropZone.acceptedResourceTypes.Clear();
                // 添加Model和Motion类型（Actor和Model是同一回事）
                dropZone.acceptedResourceTypes.Add(ResourceType.Model);
                dropZone.acceptedResourceTypes.Add(ResourceType.Motion);
                
                Debug.Log("配置了Disconnect DropZone - 接受Model和Motion类型");
            }
        }
    }
}
