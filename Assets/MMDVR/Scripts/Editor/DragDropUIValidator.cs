using UnityEngine;
using UnityEditor;
using MMDVR.Scripts.UIInteraction;
using MMDVR.Scripts.Managers;
using System.Collections.Generic;

namespace MMDVR.Scripts.Editor
{
    /// <summary>
    /// 拖拽UI系统配置验证器
    /// </summary>
    public class DragDropUIValidator : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool showDetails = true;
        
        [MenuItem("MMDVR/Validate Drag Drop UI Setup")]
        static void ShowWindow()
        {
            DragDropUIValidator window = GetWindow<DragDropUIValidator>();
            window.titleContent = new GUIContent("拖拽UI验证器");
            window.Show();
        }
        
        void OnGUI()
        {
            GUILayout.Label("拖拽UI系统配置验证", EditorStyles.boldLabel);
            
            showDetails = EditorGUILayout.Toggle("显示详细信息", showDetails);
            
            if (GUILayout.Button("验证配置"))
            {
                ValidateConfiguration();
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // 显示验证结果
            DisplayValidationResults();
            
            EditorGUILayout.EndScrollView();
        }
        
        private List<string> validationResults = new List<string>();
        private int passedChecks = 0;
        private int totalChecks = 0;
        
        void ValidateConfiguration()
        {
            validationResults.Clear();
            passedChecks = 0;
            totalChecks = 0;
            
            AddResult("=== 开始验证拖拽UI系统配置 ===", MessageType.Info);
            
            // 验证核心管理器
            ValidateSceneStatesManager();
            
            // 验证列表控制器
            ValidateListControllers();
            
            // 验证DropZone配置
            ValidateDropZones();
            
            // 验证Prefab引用
            ValidatePrefabReferences();
            
            // 验证场景结构
            ValidateSceneStructure();
            
            // 显示总结
            AddResult($"=== 验证完成: {passedChecks}/{totalChecks} 检查通过 ===", 
                     passedChecks == totalChecks ? MessageType.Info : MessageType.Warning);
        }
        
        void ValidateSceneStatesManager()
        {
            AddResult("--- SceneStatesManager验证 ---", MessageType.None);
            
            CheckAndAdd("SceneStatesManager实例", 
                      SceneStatesManager.Instance != null);
            
            if (SceneStatesManager.Instance != null)
            {
                var ssm = SceneStatesManager.Instance;
                CheckAndAdd("modelContainer引用", 
                          ssm.modelContainer != null);
                CheckAndAdd("actorContainer引用", 
                          ssm.actorContainer != null);
                CheckAndAdd("motionContainer引用", 
                          ssm.motionContainer != null);
            }
        }
        
        void ValidateListControllers()
        {
            AddResult("--- 列表控制器验证 ---", MessageType.None);
            
            // ModelListController
            var modelController = FindObjectOfType<ModelListController>();
            CheckAndAdd("ModelListController存在", modelController != null);
            if (modelController != null)
            {
                CheckAndAdd("ModelList prefab引用", modelController.listItemPrefab != null);
                CheckAndAdd("ModelList container引用", modelController.listContainer != null);
                CheckAndAdd("ModelList DropZone引用", modelController.listSortableAreaDropZone != null);
            }
            
            // MotionListController
            var motionController = FindObjectOfType<MotionListController>();
            CheckAndAdd("MotionListController存在", motionController != null);
            if (motionController != null)
            {
                CheckAndAdd("MotionList prefab引用", motionController.listItemPrefab != null);
                CheckAndAdd("MotionList container引用", motionController.listContainer != null);
                CheckAndAdd("MotionList DropZone引用", motionController.listSortableAreaDropZone != null);
            }
            
            // MusicListController
            var musicController = FindObjectOfType<MusicListController>();
            CheckAndAdd("MusicListController存在", musicController != null);
            if (musicController != null)
            {
                CheckAndAdd("MusicList prefab引用", musicController.listItemPrefab != null);
                CheckAndAdd("MusicList container引用", musicController.listContainer != null);
            }
            
            // CameraListController
            var cameraController = FindObjectOfType<CameraListController>();
            CheckAndAdd("CameraListController存在", cameraController != null);
            if (cameraController != null)
            {
                CheckAndAdd("CameraList prefab引用", cameraController.listItemPrefab != null);
                CheckAndAdd("CameraList container引用", cameraController.listContainer != null);
            }
        }
        
        void ValidateDropZones()
        {
            AddResult("--- DropZone配置验证 ---", MessageType.None);
            
            var dropZones = FindObjectsOfType<DropZone>();
            CheckAndAdd("DropZone组件存在", dropZones.Length > 0);
            
            foreach (var dropZone in dropZones)
            {
                string zoneName = dropZone.gameObject.name;
                if (showDetails)
                {
                    AddResult($"DropZone: {zoneName}", MessageType.None);
                    AddResult($"  动作类型: {dropZone.actionType}", MessageType.None);
                    AddResult($"  接受类型数量: {dropZone.acceptedResourceTypes.Count}", MessageType.None);
                    AddResult($"  事件监听器数量: {dropZone.onItemDropped.GetPersistentEventCount()}", MessageType.None);
                }
            }
        }
        
        void ValidatePrefabReferences()
        {
            AddResult("--- Prefab引用验证 ---", MessageType.None);
            
            string prefabPath = "Assets/MMDVR/UI/Prefabs/MainUI/ResourceManagement/GenericListItemCard.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            CheckAndAdd("GenericListItemCard prefab存在", prefab != null);
            
            if (prefab != null)
            {
                var draggableItem = prefab.GetComponent<DraggableItem>();
                CheckAndAdd("Prefab包含DraggableItem组件", draggableItem != null);
            }
        }
        
        void ValidateSceneStructure()
        {
            AddResult("--- 场景结构验证 ---", MessageType.None);
            
            CheckAndAdd("ModelList GameObject存在", 
                      GameObject.Find("ModelList") != null);
            CheckAndAdd("MotionList GameObject存在", 
                      GameObject.Find("MotionList") != null);
            CheckAndAdd("MusicList GameObject存在", 
                      GameObject.Find("MusicList") != null);
            CheckAndAdd("UniversalListArea存在", 
                      GameObject.Find("UniversalListArea") != null);
            
            // 检查DropZone GameObject
            CheckAndAdd("DropzoneUnload存在", 
                      GameObject.Find("DropzoneUnload") != null);
            CheckAndAdd("DropZoneEnabling存在", 
                      GameObject.Find("DropZoneEnabling") != null);
            CheckAndAdd("DropZoneDisconnect存在", 
                      GameObject.Find("DropZoneDisconnect") != null);
        }
        
        void CheckAndAdd(string checkName, bool condition)
        {
            totalChecks++;
            if (condition)
            {
                passedChecks++;
                AddResult($"✓ {checkName}", MessageType.Info);
            }
            else
            {
                AddResult($"✗ {checkName}", MessageType.Error);
            }
        }
        
        void AddResult(string message, MessageType type)
        {
            string prefix = type switch
            {
                MessageType.Error => "[ERROR] ",
                MessageType.Warning => "[WARN] ",
                MessageType.Info => "[INFO] ",
                _ => ""
            };
            validationResults.Add(prefix + message);
        }
        
        void DisplayValidationResults()
        {
            foreach (string result in validationResults)
            {
                if (result.StartsWith("[ERROR]"))
                {
                    EditorGUILayout.HelpBox(result.Substring(7), MessageType.Error);
                }
                else if (result.StartsWith("[WARN]"))
                {
                    EditorGUILayout.HelpBox(result.Substring(6), MessageType.Warning);
                }
                else if (result.StartsWith("[INFO]"))
                {
                    EditorGUILayout.HelpBox(result.Substring(6), MessageType.Info);
                }
                else
                {
                    EditorGUILayout.LabelField(result);
                }
            }
        }
    }
}
