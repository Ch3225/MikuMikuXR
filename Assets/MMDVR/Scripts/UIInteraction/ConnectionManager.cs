using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using MMDVR.Managers;

namespace MMDVR.Scripts.UIInteraction
{
    /// <summary>
    /// 连线管理器，负责管理模型和动作之间的UI连线显示
    /// </summary>
    public class ConnectionManager : MonoBehaviour
    {
        public static ConnectionManager Instance { get; private set; }
          [Header("连线层配置")]
        public Canvas connectionCanvas; // 专门用于显示连线的Canvas        [Header("连线样式")]
        public Color defaultLineColor = Color.magenta; // 改为紫红色，更醒目
        public float defaultLineWidth = 10f; // 增加到10像素，更容易看见
        
        // 存储所有活动的连线
        private Dictionary<string, ConnectionLine> activeConnections = new Dictionary<string, ConnectionLine>();
        
        // 存储UI项的RectTransform，用于获取位置
        private Dictionary<string, RectTransform> modelItemTransforms = new Dictionary<string, RectTransform>();
        private Dictionary<string, RectTransform> motionItemTransforms = new Dictionary<string, RectTransform>();
        
        [Header("Connection Layer 配置")]
        [Tooltip("可选：显式指定ConnectionLayer。如果不指定，将自动查找Canvas/MainUI/ConnectionLayer。")]
        public Transform explicitConnectionLayer;
        private RectTransform connectionLayerToUse;
        
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
        }        void Start()
        {
            // 如果没有设置连线Canvas，创建一个
            if (connectionCanvas == null)
            {
                CreateConnectionCanvas();
            }
            InitConnectionLayer();
            // 监听关联变化事件 - 使用static Action
            EventManager.OnMotionListChanged += RefreshAllConnections;
        }
        
        void OnDestroy()
        {
            // 取消事件监听
            EventManager.OnMotionListChanged -= RefreshAllConnections;
        }        /// <summary>
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
        }/// <summary>
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
            // 父对象设为ConnectionLayer
            if (connectionLayerToUse != null)
                lineGO.transform.SetParent(connectionLayerToUse, false);
            else
                lineGO.transform.SetParent(connectionCanvas.transform, false);
            CanvasRenderer canvasRenderer = lineGO.AddComponent<CanvasRenderer>();
            ConnectionLine connectionLine = lineGO.AddComponent<ConnectionLine>();
            connectionLine.lineWidth = defaultLineWidth;
            connectionLine.lineColor = defaultLineColor;
            connectionLine.raycastTarget = false;
            if (connectionLineMaterial != null)
                connectionLine.material = connectionLineMaterial;
            return lineGO;
        }
        
        private void InitConnectionLayer()
        {
            if (explicitConnectionLayer != null)
            {
                connectionLayerToUse = explicitConnectionLayer as RectTransform;
            }
            else if (connectionCanvas != null)
            {
                Transform mainUiTransform = connectionCanvas.name == "MainUI"
                    ? connectionCanvas.transform
                    : connectionCanvas.transform.Find("MainUI");
                if (mainUiTransform != null)
                {
                    var foundLayer = mainUiTransform.Find("ConnectionLayer");
                    if (foundLayer != null)
                        connectionLayerToUse = foundLayer as RectTransform;
                }
            }
            if (connectionLayerToUse == null && connectionCanvas != null)
            {
                Debug.LogWarning("ConnectionLayer未找到，使用Canvas根节点作为兜底。", this);
                connectionLayerToUse = connectionCanvas.transform as RectTransform;
            }
            else if (connectionLayerToUse == null)
            {
                Debug.LogError("未找到可用的ConnectionLayer和Canvas！", this);
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
                return;            }            // 创建连线GameObject
            GameObject lineGO = CreateConnectionLineGameObject();
            lineGO.name = $"Connection_{modelId}_{motionId}";
            lineGO.transform.SetParent(connectionCanvas.transform, false);
              // 获取ConnectionLine组件
            ConnectionLine connectionLine = lineGO.GetComponent<ConnectionLine>();
            
            // 设置连线端点
            connectionLine.SetPoints(
                modelItemTransforms[modelId], 
                motionItemTransforms[motionId], 
                modelId, 
                motionId
            );
            
            activeConnections[connectionKey] = connectionLine;
            Debug.Log($"创建连线: {modelId} -> {motionId}, 起点:{modelItemTransforms[modelId].position}, 终点:{motionItemTransforms[motionId].position}");
        }
        
        /// <summary>
        /// 移除连线
        /// </summary>
        public void RemoveConnection(string modelId, string motionId)
        {
            string connectionKey = GetConnectionKey(modelId, motionId);
            
            if (activeConnections.ContainsKey(connectionKey))
            {
                if (activeConnections[connectionKey] != null)
                {
                    Destroy(activeConnections[connectionKey].gameObject);
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
        /// 刷新所有连线（根据SceneStatesManager中的关联数据）
        /// </summary>
        public void RefreshAllConnections()
        {
            Debug.Log("刷新所有连线");
            
            // 清除现有连线
            ClearAllConnections();
            
            // 根据SceneStatesManager重新创建连线
            if (SceneStatesManager.Instance != null)
            {
                var modelList = SceneStatesManager.Instance.GetModelList();
                var motionList = SceneStatesManager.Instance.GetMotionList();
                
                // 遍历所有模型-动作关联
                foreach (var model in modelList)
                {
                    foreach (var motion in motionList)
                    {
                        // 检查是否有关联（这里需要SceneStatesManager提供查询方法）
                        if (IsModelMotionAssociated(model.ID, motion.ID))
                        {
                            CreateConnection(model.ID, motion.ID);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 检查模型和动作是否已关联（需要SceneStatesManager提供此方法）
        /// </summary>
        private bool IsModelMotionAssociated(string modelId, string motionId)
        {
            // 这里需要SceneStatesManager提供查询关联的方法
            // 暂时返回false，等待SceneStatesManager添加相应方法
            return false;
        }
        
        /// <summary>
        /// 生成连线的唯一键
        /// </summary>
        private string GetConnectionKey(string modelId, string motionId)
        {
            return $"{modelId}->{motionId}";
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
                    connection.lineColor = color;
                    connection.lineWidth = width;
                    connection.color = color;
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
                    Debug.Log($"  - LineColor: {line.lineColor}");
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
    }
}
