using UnityEngine;
using UnityEngine.UI;
using MMDVR.Scripts.UIInteraction;

namespace MMDVR.Scripts.Testing
{
    /// <summary>
    /// 连线测试脚本，用于验证ConnectionLine和ConnectionManager的功能
    /// </summary>
    public class ConnectionLineTest : MonoBehaviour
    {
        [Header("测试UI元素")]
        public RectTransform testStartPoint;
        public RectTransform testEndPoint;
        public Button testButton;
        
        [Header("测试配置")]
        public string testModelId = "TestModel";
        public string testMotionId = "TestMotion";
        
        private ConnectionManager connectionManager;
        
        void Start()
        {
            // 获取或创建ConnectionManager
            connectionManager = FindObjectOfType<ConnectionManager>();
            if (connectionManager == null)
            {
                GameObject managerGO = new GameObject("ConnectionManager");
                connectionManager = managerGO.AddComponent<ConnectionManager>();
            }
            
            // 绑定测试按钮
            if (testButton != null)
            {
                testButton.onClick.AddListener(TestCreateConnection);
            }
            
            Debug.Log("ConnectionLineTest 初始化完成");
        }
          /// <summary>
        /// 测试创建连线
        /// </summary>
        public void TestCreateConnection()
        {
            if (testStartPoint == null || testEndPoint == null)
            {
                Debug.LogError("测试端点未设置！");
                return;
            }
            
            Debug.Log("开始创建测试连线...");
            
            // 注册UI项（分别使用对应的注册方法）
            connectionManager.RegisterModelItem(testModelId, testStartPoint);
            connectionManager.RegisterMotionItem(testMotionId, testEndPoint);
            
            // 创建连线
            connectionManager.CreateConnection(testModelId, testMotionId);
            
            Debug.Log($"创建连线: {testModelId} -> {testMotionId}");
        }
        
        /// <summary>
        /// 测试移除连线
        /// </summary>
        public void TestRemoveConnection()
        {
            connectionManager.RemoveConnection(testModelId, testMotionId);
            Debug.Log($"移除连线: {testModelId} -> {testMotionId}");
        }
        
        /// <summary>
        /// 调试连线状态
        /// </summary>
        public void DebugConnectionState()
        {
            connectionManager.DebugConnections();
        }
        
        void OnValidate()
        {
            // 自动查找UI元素（用于编辑器）
            if (testStartPoint == null)
            {
                var buttons = FindObjectsOfType<Button>();
                if (buttons.Length > 0)
                {
                    testStartPoint = buttons[0].GetComponent<RectTransform>();
                }
            }
            
            if (testEndPoint == null)
            {
                var buttons = FindObjectsOfType<Button>();
                if (buttons.Length > 1)
                {
                    testEndPoint = buttons[1].GetComponent<RectTransform>();
                }
            }
        }
    }
}
