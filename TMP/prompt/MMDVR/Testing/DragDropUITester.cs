using UnityEngine;
using MMDVR.Managers;
using MMDVR.Scripts.UIInteraction;

namespace MMDVR.Testing
{
    /// <summary>
    /// 拖拽UI系统测试脚本
    /// </summary>
    public class DragDropUITester : MonoBehaviour
    {
        void Start()
        {
            TestUIConfiguration();
        }
        
        void TestUIConfiguration()
        {
            Debug.Log("=== 拖拽UI系统测试开始 ===");
            
            // 测试SceneStatesManager
            if (SceneStatesManager.Instance != null)
            {
                Debug.Log("✓ SceneStatesManager实例已找到");
                
                // 测试添加模型资源
                string testModelPath = "Assets/TestModel.pmx";
                string modelId = SceneStatesManager.Instance.AddModel(testModelPath);
                Debug.Log($"✓ 测试模型已添加，ID: {modelId}");
                
                // 测试添加动作资源
                string testMotionPath = "Assets/TestMotion.vmd";
                string motionId = SceneStatesManager.Instance.AddMotion(testMotionPath);
                Debug.Log($"✓ 测试动作已添加，ID: {motionId}");
                
                // 测试模型-动作关联
                SceneStatesManager.Instance.AssociateModelWithMotion(modelId, motionId);
                Debug.Log($"✓ 模型-动作关联已建立");
                
                // 测试获取数据列表
                var modelList = SceneStatesManager.Instance.GetModelList();
                var motionList = SceneStatesManager.Instance.GetMotionList();
                Debug.Log($"✓ 模型数量: {modelList.Count}, 动作数量: {motionList.Count}");
                
                // 清理测试数据
                SceneStatesManager.Instance.RemoveModelResource(modelId);
                SceneStatesManager.Instance.RemoveMotionResource(motionId);
                Debug.Log("✓ 测试数据已清理");
            }
            else
            {
                Debug.LogError("✗ SceneStatesManager实例未找到！");
            }
            
            // 测试列表控制器
            TestListControllers();
            
            Debug.Log("=== 拖拽UI系统测试完成 ===");
        }
        
        void TestListControllers()
        {
            // 测试ModelListController
            var modelListController = FindObjectOfType<ModelListController>();
            if (modelListController != null)
            {
                Debug.Log("✓ ModelListController已找到");
                if (modelListController.listItemPrefab != null)
                    Debug.Log("✓ ModelListController prefab已配置");
                else
                    Debug.LogWarning("⚠ ModelListController prefab未配置");
            }
            else
            {
                Debug.LogError("✗ ModelListController未找到！");
            }
            
            // 测试MotionListController
            var motionListController = FindObjectOfType<MotionListController>();
            if (motionListController != null)
            {
                Debug.Log("✓ MotionListController已找到");
                if (motionListController.listItemPrefab != null)
                    Debug.Log("✓ MotionListController prefab已配置");
                else
                    Debug.LogWarning("⚠ MotionListController prefab未配置");
            }
            else
            {
                Debug.LogError("✗ MotionListController未找到！");
            }
            
            // 测试MusicListController
            var musicListController = FindObjectOfType<MusicListController>();
            if (musicListController != null)
            {
                Debug.Log("✓ MusicListController已找到");
            }
            else
            {
                Debug.LogError("✗ MusicListController未找到！");
            }
            
            // 测试CameraListController
            var cameraListController = FindObjectOfType<CameraListController>();
            if (cameraListController != null)
            {
                Debug.Log("✓ CameraListController已找到");
            }
            else
            {
                Debug.LogError("✗ CameraListController未找到！");
            }
        }
        
        [ContextMenu("Run UI Test")]
        public void RunTest()
        {
            TestUIConfiguration();
        }
    }
}
