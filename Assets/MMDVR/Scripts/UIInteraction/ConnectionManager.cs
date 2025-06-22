using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using MMDVR.Scripts.Managers;

namespace MMDVR.Scripts.UIInteraction
{
    /// <summary>
    /// 连线管理器，负责管理模型和动作之间的UI连线显示
    /// </summary>
    public class ConnectionManager : MonoBehaviour
    {
        public static ConnectionManager Instance { get; private set; }
        
        [Header("连线层配置")]
        public Canvas connectionCanvas; // 专门用于显示连线的Canvas
        
        [Header("连线样式")]
        public Color defaultLineColor = Color.yellow; // 默认黄色
        public float defaultLineWidth = 10f; // 增加到10像素，更容易看见
        
        // 存储所有活动的连线
        private Dictionary<string, ConnectionLine> activeConnections = new Dictionary<string, ConnectionLine>();
        
        // 存储UI项的RectTransform，用于获取位置
        private Dictionary<string, RectTransform> modelItemTransforms = new Dictionary<string, RectTransform>();
        private Dictionary<string, RectTransform> motionItemTransforms = new Dictionary<string, RectTransform>();
        
        [Header("Connection Layer 对象（强制指定，推荐直接拖拽ConnectionLayer GameObject）")]
        public GameObject connectionLayerObject;
        
        [Header("Connection Layer 配置")]
        [Tooltip("可选：显式指定ConnectionLayer。如果不指定，将自动查找Canvas/MainUI/ConnectionLayer。")]
        public Transform explicitConnectionLayer;
        private Transform connectionLayerToUse;
        
        [Header("连线材质（HDRP专用）")]
        public Material connectionLineMaterial;
        
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
            // 如果没有设置连线Canvas，创建一个
            if (connectionCanvas == null)
            {
                CreateConnectionCanvas();
            }
            InitConnectionLayer();
            // 监听关联变化事件 - 使用static Action
            EventManager.OnMotionListChanged += RefreshAllConnections;
            // 监听模型-动作关联变化事件（这是最重要的）
            EventManager.OnModelMotionAssociationChanged += RebuildAllConnections;
        }
        
        void OnDestroy()
        {
            // 取消事件监听
            EventManager.OnMotionListChanged -= RefreshAllConnections;
            EventManager.OnModelMotionAssociationChanged -= RebuildAllConnections;
        }
        
        /// <summary>
        /// 创建连线Canvas
        /// </summary>
        private void CreateConnectionCanvas()
        {
            GameObject canvasGO = new GameObject("ConnectionCanvas");
            // 不设置父对象，让它独立存在
            
            connectionCanvas = canvasGO.AddComponent<Canvas>();
            connectionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            connectionCanvas.overrideSorting = true;
            connectionCanvas.sortingOrder = 1; // 设置为较低的层级，确保在其他UI下方
            
            // 不添加CanvasScaler，使用像素完美模式
            // 不添加GraphicRaycaster，避免阻挡UI交互
            
            Debug.Log($"创建ConnectionCanvas: sortingOrder={connectionCanvas.sortingOrder}, renderMode={connectionCanvas.renderMode}");
        }
        
        /// <summary>
        /// 获取GameObject的完整路径（用于调试）
        /// </summary>
        private string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return "null";
            
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
        
        /// <summary>
        /// 创建连线GameObject（直接创建，不使用预制体）
        /// </summary>
        private GameObject CreateConnectionLineGameObject()
        {
            GameObject lineGO = new GameObject("ConnectionLine");
            RectTransform rectTransform = lineGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            
            // 详细调试ConnectionLayer状态
            Debug.Log($"[DEBUG] connectionLayerToUse: {(connectionLayerToUse != null ? connectionLayerToUse.name : "null")}");
            Debug.Log($"[DEBUG] connectionLayerToUse的GameObject路径: {(connectionLayerToUse != null ? GetGameObjectPath(connectionLayerToUse.gameObject) : "null")}");
            
            // 父对象设为ConnectionLayer
            if (connectionLayerToUse != null)
            {
                lineGO.transform.SetParent(connectionLayerToUse, true); // 改为true，保持世界坐标
                Debug.Log($"ConnectionLine已设置父对象为: {connectionLayerToUse.name}");
                Debug.Log($"[验证] ConnectionLine实际父对象: {(lineGO.transform.parent != null ? lineGO.transform.parent.name : "null")}");
                Debug.Log($"[验证] ConnectionLine完整路径: {GetGameObjectPath(lineGO)}");
            }
            else
            {
                lineGO.transform.SetParent(connectionCanvas.transform, true);
                Debug.Log($"ConnectionLine设置父对象为ConnectionCanvas: {connectionCanvas.name}");
            }
            
            // 添加CanvasRenderer组件
            CanvasRenderer canvasRenderer = lineGO.AddComponent<CanvasRenderer>();
            
            // 添加ConnectionLine组件
            ConnectionLine connectionLine = lineGO.AddComponent<ConnectionLine>();
            connectionLine.lineWidth = defaultLineWidth;
            connectionLine.color = defaultLineColor;
            connectionLine.raycastTarget = false;
            
            // 不设置材质，优先用color属性
            Debug.Log($"创建ConnectionLine: width={defaultLineWidth}, color={defaultLineColor}");
            
            return lineGO;
        }
        
        private void InitConnectionLayer()
        {
            Debug.Log("[DEBUG] 开始初始化ConnectionLayer...");
            
            if (connectionLayerObject != null)
            {
                connectionLayerToUse = connectionLayerObject.transform;
                Debug.Log($"使用显式指定的ConnectionLayer对象: {connectionLayerToUse.name}");
                Debug.Log($"[DEBUG] ConnectionLayer对象路径: {GetGameObjectPath(connectionLayerObject)}");
            }
            else if (explicitConnectionLayer != null)
            {
                connectionLayerToUse = explicitConnectionLayer;
                Debug.Log($"使用显式指定的ConnectionLayer: {connectionLayerToUse.name}");
                Debug.Log($"[DEBUG] explicitConnectionLayer路径: {GetGameObjectPath(explicitConnectionLayer.gameObject)}");
            }
            else
            {
                Debug.Log("[DEBUG] 开始自动查找ConnectionLayer...");
                // 查找主UI Canvas下的ConnectionLayer
                Canvas[] allCanvases = FindObjectsOfType<Canvas>();
                Debug.Log($"[DEBUG] 找到 {allCanvases.Length} 个Canvas");
                
                foreach (Canvas canvas in allCanvases)
                {
                    Debug.Log($"[DEBUG] 检查Canvas: {canvas.name}");
                    Transform mainUiTransform = canvas.name == "MainUI"
                        ? canvas.transform
                        : canvas.transform.Find("MainUI");
                    
                    if (mainUiTransform != null)
                    {
                        Debug.Log($"[DEBUG] 找到MainUI: {mainUiTransform.name}");
                        var foundLayer = mainUiTransform.Find("ConnectionLayer");
                        if (foundLayer != null)
                        {
                            connectionLayerToUse = foundLayer;
                            Debug.Log($"找到ConnectionLayer: {connectionLayerToUse.name}");
                            Debug.Log($"[DEBUG] 自动找到的ConnectionLayer路径: {GetGameObjectPath(foundLayer.gameObject)}");
                            break;
                        }
                        else
                        {
                            Debug.Log($"[DEBUG] 在 {mainUiTransform.name} 下未找到ConnectionLayer");
                        }
                    }
                }
            }
            
            if (connectionLayerToUse == null)
            {
                Debug.LogWarning("ConnectionLayer未找到，连线将放在ConnectionCanvas根节点。建议在MainUI下创建ConnectionLayer。", this);
                connectionLayerToUse = connectionCanvas.transform;
            }
            else
            {
                Debug.Log($"[SUCCESS] 最终使用的ConnectionLayer: {connectionLayerToUse.name}, 路径: {GetGameObjectPath(connectionLayerToUse.gameObject)}");
            }
        }
        
        /// <summary>
        /// 注册模型UI项
        /// </summary>
        public void RegisterModelItem(string modelId, RectTransform transform)
        {
            modelItemTransforms[modelId] = transform;
            Debug.Log($"注册模型UI项: {modelId}");
        }
        
        /// <summary>
        /// 注册动作UI项
        /// </summary>
        public void RegisterMotionItem(string motionId, RectTransform transform)
        {
            motionItemTransforms[motionId] = transform;
            Debug.Log($"注册动作UI项: {motionId}");
        }
        
        /// <summary>
        /// 取消注册模型UI项
        /// </summary>
        public void UnregisterModelItem(string modelId)
        {
            modelItemTransforms.Remove(modelId);
        }
        
        /// <summary>
        /// 取消注册动作UI项
        /// </summary>
        public void UnregisterMotionItem(string motionId)
        {
            motionItemTransforms.Remove(motionId);
        }
        
        /// <summary>
        /// 创建连线
        /// </summary>
        public void CreateConnection(string modelId, string motionId)
        {
            string connectionKey = GetConnectionKey(modelId, motionId);
            
            // 如果连线已存在，不重复创建
            if (activeConnections.ContainsKey(connectionKey))
            {
                return;
            }
            
            // 检查是否有对应的UI项
            if (!modelItemTransforms.ContainsKey(modelId) || !motionItemTransforms.ContainsKey(motionId))
            {
                Debug.LogWarning($"无法创建连线：找不到对应的UI项 Model:{modelId} Motion:{motionId}");
                return;
            }
            
            // 创建连线GameObject
            GameObject lineGO = CreateConnectionLineGameObject();
            lineGO.name = $"Connection_{modelId}_{motionId}";
            
            // 获取ConnectionLine组件并设置连线
            ConnectionLine connectionLine = lineGO.GetComponent<ConnectionLine>();
            RectTransform startPoint = modelItemTransforms[modelId];
            RectTransform endPoint = motionItemTransforms[motionId];
            
            connectionLine.SetPoints(startPoint, endPoint, modelId, motionId);
            
            // 存储连线
            activeConnections[connectionKey] = connectionLine;
            
            Debug.Log($"创建连线: {modelId} -> {motionId}, 起点:{startPoint.position:F2}, 终点:{endPoint.position:F2}");
        }
        
        /// <summary>
        /// 移除连线
        /// </summary>
        public void RemoveConnection(string modelId, string motionId)
        {
            string connectionKey = GetConnectionKey(modelId, motionId);
            if (activeConnections.ContainsKey(connectionKey))
            {
                ConnectionLine connection = activeConnections[connectionKey];
                if (connection != null)
                {
                    Destroy(connection.gameObject);
                }
                activeConnections.Remove(connectionKey);
                Debug.Log($"移除连线: {modelId} -> {motionId}");
            }
        }
        
        /// <summary>
        /// 清除所有连线
        /// </summary>
        public void ClearAllConnections()
        {
            foreach (var connection in activeConnections.Values)
            {
                if (connection != null)
                {
                    Destroy(connection.gameObject);
                }
            }
            activeConnections.Clear();
            Debug.Log("清除所有连线");
        }
          /// <summary>
        /// 刷新所有连线
        /// </summary>
        public void RefreshAllConnections()
        {
            foreach (var connection in activeConnections.Values)
            {
                if (connection != null)
                {
                    connection.SetVerticesDirty();
                }
            }
        }
        
        /// <summary>
        /// 获取连线的唯一标识
        /// </summary>
        private string GetConnectionKey(string modelId, string motionId)
        {
            return $"{modelId}_{motionId}";
        }
        
        /// <summary>
        /// 设置连线样式
        /// </summary>
        public void SetConnectionStyle(Color color, float width)
        {
            defaultLineColor = color;
            defaultLineWidth = width;
            // 更新现有连线样式
            foreach (var connection in activeConnections.Values)
            {
                if (connection != null)
                {
                    connection.color = color;
                    connection.lineWidth = width;
                }
            }
        }
        
        /// <summary>
        /// 临时禁用所有连线更新（用于调试UI卡死问题）
        /// </summary>
        public void DisableAllConnectionUpdates()
        {
            foreach (var connection in activeConnections.Values)
            {
                if (connection != null)
                {
                    connection.enabled = false;
                }
            }
            Debug.Log("已禁用所有连线更新");
        }
        
        /// <summary>
        /// 重新启用所有连线更新
        /// </summary>
        public void EnableAllConnectionUpdates()
        {
            foreach (var connection in activeConnections.Values)
            {
                if (connection != null)
                {
                    connection.enabled = true;
                }
            }
            Debug.Log("已启用所有连线更新");
        }
        
        /// <summary>
        /// 调试方法：检查所有连线的状态
        /// </summary>
        [ContextMenu("Debug Connections")]
        public void DebugConnections()
        {
            Debug.Log($"=== 连线调试信息 ===");
            Debug.Log($"ConnectionCanvas: {(connectionCanvas != null ? connectionCanvas.name : "null")}");
            Debug.Log($"SortingOrder: {(connectionCanvas != null ? connectionCanvas.sortingOrder.ToString() : "null")}");
            Debug.Log($"活动连线数量: {activeConnections.Count}");
            foreach (var kvp in activeConnections)
            {
                string key = kvp.Key;
                ConnectionLine line = kvp.Value;
                if (line != null)
                {
                    Debug.Log($"连线 {key}:");
                    Debug.Log($"  - GameObject: {line.gameObject.name}");
                    Debug.Log($"  - Position: {line.rectTransform.anchoredPosition}");
                    Debug.Log($"  - Size: {line.rectTransform.sizeDelta}");
                    Debug.Log($"  - StartPoint: {(line.startPoint != null ? line.startPoint.name : "null")}");
                    Debug.Log($"  - EndPoint: {(line.endPoint != null ? line.endPoint.name : "null")}");
                    Debug.Log($"  - LineWidth: {line.lineWidth}");
                    Debug.Log($"  - LineColor: {line.color}");
                    Debug.Log($"  - Active: {line.gameObject.activeInHierarchy}");
                    Debug.Log($"  - Canvas: {(line.canvas != null ? line.canvas.name : "null")}");
                }
                else
                {
                    Debug.Log($"连线 {key}: null");
                }
            }
            Debug.Log($"==================");
        }
        
        /// <summary>
        /// 强制刷新所有连线
        /// </summary>
        [ContextMenu("Force Refresh All Connections")]
        public void ForceRefreshConnections()
        {
            foreach (var connection in activeConnections.Values)
            {
                if (connection != null)
                {
                    connection.SetVerticesDirty();
                }
            }
            Debug.Log("强制刷新所有连线");
        }
        
        /// <summary>
        /// 重建所有连线（根据实际关联状态）
        /// </summary>
        public void RebuildAllConnections()
        {
            Debug.Log("开始重建所有连线...");
            
            // 1. 清理无效连线（端点已销毁的连线）
            CleanupInvalidConnections();
            
            // 2. 根据实际关联状态重建连线            if (SceneStatesManager.Instance != null && AssociationManager.Instance != null)
            {
                var modelList = SceneStatesManager.Instance.GetModelList();
                foreach (var model in modelList)
                {
                    var associatedMotions = AssociationManager.Instance.GetModelAssociatedMotions(model.ID);
                    foreach (var motionId in associatedMotions)
                    {
                        string connectionKey = GetConnectionKey(model.ID, motionId);
                        
                        // 如果连线不存在或无效，创建新连线
                        if (!activeConnections.ContainsKey(connectionKey) || 
                            activeConnections[connectionKey] == null || 
                            !activeConnections[connectionKey].IsValid())
                        {
                            CreateConnection(model.ID, motionId);
                        }
                    }
                }
                
                // 3. 删除不应该存在的连线
                var connectionsToRemove = new List<string>();                foreach (var kvp in activeConnections)
                {
                    if (kvp.Value != null && kvp.Value.IsValid())
                    {
                        string modelId = kvp.Value.modelId;
                        string motionId = kvp.Value.motionId;
                        var associatedMotions = AssociationManager.Instance.GetModelAssociatedMotions(modelId);
                        
                        // 如果实际关联中不包含这个动作，标记删除
                        if (!associatedMotions.Contains(motionId))
                        {
                            connectionsToRemove.Add(kvp.Key);
                        }
                    }
                }
                
                // 删除标记的连线
                foreach (string key in connectionsToRemove)
                {
                    if (activeConnections.ContainsKey(key))
                    {
                        if (activeConnections[key] != null)
                        {
                            Destroy(activeConnections[key].gameObject);
                        }
                        activeConnections.Remove(key);
                    }
                }
            }
            
            Debug.Log($"连线重建完成，当前活动连线数量: {activeConnections.Count}");
        }
          /// <summary>
        /// 只重建/清理与指定动作相关的连线
        /// </summary>
        public void RebuildConnectionsForMotion(string motionId)
        {
            Debug.Log($"重建与动作 {motionId} 相关的连线...");
            // 1. 清理与该动作相关的无效连线
            var keysToRemove = new List<string>();
            foreach (var kvp in activeConnections)
            {
                if (kvp.Value != null && kvp.Value.IsValid() && kvp.Value.motionId == motionId)
                {
                    // 检查该模型是否还与该动作关联
                    var associatedMotions = AssociationManager.Instance.GetModelAssociatedMotions(kvp.Value.modelId);
                    if (!associatedMotions.Contains(motionId))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
            }
            foreach (var key in keysToRemove)
            {
                if (activeConnections[key] != null)
                    Destroy(activeConnections[key].gameObject);
                activeConnections.Remove(key);
            }
            // 2. 为所有与该动作有关联的模型重建连线
            var modelList = SceneStatesManager.Instance.GetModelList();
            foreach (var model in modelList)            {
                var associatedMotions = AssociationManager.Instance.GetModelAssociatedMotions(model.ID);
                if (associatedMotions.Contains(motionId))
                {
                    string connectionKey = GetConnectionKey(model.ID, motionId);
                    if (!activeConnections.ContainsKey(connectionKey) || activeConnections[connectionKey] == null || !activeConnections[connectionKey].IsValid())
                    {
                        CreateConnection(model.ID, motionId);
                    }
                }
            }
            Debug.Log($"与动作 {motionId} 相关的连线重建完成");
        }

        /// <summary>
        /// 只重建/清理与指定模型相关的连线
        /// </summary>
        public void RebuildConnectionsForModel(string modelId)
        {
            Debug.Log($"重建与模型 {modelId} 相关的连线...");
            // 1. 清理与该模型相关的无效连线
            var keysToRemove = new List<string>();
            foreach (var kvp in activeConnections)
            {
                if (kvp.Value != null && kvp.Value.IsValid() && kvp.Value.modelId == modelId)
                {                    // 检查该模型是否还与该动作关联
                    var associatedMotions = AssociationManager.Instance.GetModelAssociatedMotions(modelId);
                    if (!associatedMotions.Contains(kvp.Value.motionId))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
            }
            foreach (var key in keysToRemove)
            {
                if (activeConnections[key] != null)
                    Destroy(activeConnections[key].gameObject);
                activeConnections.Remove(key);            }
            // 2. 为该模型所有实际有关联的动作重建连线
            var associatedMotionsNow = AssociationManager.Instance.GetModelAssociatedMotions(modelId);
            foreach (var motionId in associatedMotionsNow)
            {
                string connectionKey = GetConnectionKey(modelId, motionId);
                if (!activeConnections.ContainsKey(connectionKey) || activeConnections[connectionKey] == null || !activeConnections[connectionKey].IsValid())
                {
                    CreateConnection(modelId, motionId);
                }
            }
            Debug.Log($"与模型 {modelId} 相关的连线重建完成");
        }
        
        /// <summary>
        /// 刷新现有连线的端点引用（解决RefreshList后EndPoint Missing问题）
        /// </summary>
        public void RefreshConnectionEndPoints()
        {
            foreach (var kvp in activeConnections)
            {
                if (kvp.Value != null)
                {
                    string modelId = kvp.Value.modelId;
                    string motionId = kvp.Value.motionId;
                    
                    // 重新获取UI项的RectTransform
                    if (modelItemTransforms.ContainsKey(modelId) && motionItemTransforms.ContainsKey(motionId))
                    {
                        RectTransform newStartPoint = modelItemTransforms[modelId];
                        RectTransform newEndPoint = motionItemTransforms[motionId];
                        
                        // 更新连线的端点引用
                        kvp.Value.SetPoints(newStartPoint, newEndPoint, modelId, motionId);
                        Debug.Log($"刷新连线端点: {modelId} -> {motionId}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 清理无效连线（端点已销毁的连线）
        /// </summary>
        private void CleanupInvalidConnections()
        {
            var invalidConnections = new List<string>();
            
            foreach (var kvp in activeConnections)
            {
                if (kvp.Value == null || !kvp.Value.IsValid())
                {
                    invalidConnections.Add(kvp.Key);
                }
            }
            
            foreach (string key in invalidConnections)
            {
                if (activeConnections[key] != null)
                {
                    Destroy(activeConnections[key].gameObject);
                }
                activeConnections.Remove(key);
                Debug.Log($"清理无效连线: {key}");
            }
        }
    }
}
