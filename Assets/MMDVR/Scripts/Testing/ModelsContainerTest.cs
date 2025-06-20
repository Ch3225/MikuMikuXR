using UnityEngine;
using MMDVR.Scripts.Managers;

namespace MMDVR.Scripts.Testing
{
    /// <summary>
    /// 专门测试Models容器的问题
    /// </summary>
    public class ModelsContainerTest : MonoBehaviour
    {
        [Header("测试按钮")]
        [SerializeField] private bool runTest = false;
        
        private void Update()
        {
            if (runTest)
            {
                runTest = false;
                RunModelsContainerTest();
            }
        }
        
        private void RunModelsContainerTest()
        {
            Debug.Log("=== Models Container Test 开始 ===");
            
            // 检查ResourceManager
            if (ResourceManager.Instance == null)
            {
                Debug.LogError("ResourceManager.Instance为null");
                return;
            }
            
            Debug.Log("ResourceManager.Instance存在");
            
            // 检查SceneDisplayManager
            if (SceneDisplayManager.Instance == null)
            {
                Debug.LogError("SceneDisplayManager.Instance为null");
                return;
            }
            
            Debug.Log("SceneDisplayManager.Instance存在");
            
            // 测试LoadModel
            string testModelPath = "TMP/MMDTest/Models/Sour miku/Sour Miku1.pmx";
            string projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
            string fullModelPath = System.IO.Path.Combine(projectRoot, testModelPath);
            
            Debug.Log($"测试模型路径: {fullModelPath}");
            Debug.Log($"文件存在: {System.IO.File.Exists(fullModelPath)}");
            
            // 调用LoadModel
            Debug.Log("调用ResourceManager.LoadModel...");
            string modelId = ResourceManager.Instance.LoadModel(fullModelPath);
            
            if (string.IsNullOrEmpty(modelId))
            {
                Debug.LogError("LoadModel返回null或空");
            }
            else
            {
                Debug.Log($"LoadModel成功，ID={modelId}");
                
                // 检查Models容器
                var modelsContainer = GameObject.Find("Models");
                if (modelsContainer == null)
                {
                    Debug.LogError("找不到Models容器");
                }
                else
                {
                    Debug.Log($"Models容器找到，子对象数量: {modelsContainer.transform.childCount}");
                    for (int i = 0; i < modelsContainer.transform.childCount; i++)
                    {
                        var child = modelsContainer.transform.GetChild(i);
                        Debug.Log($"  子对象 {i}: {child.name}");
                    }
                }
                
                // 测试AddActor
                Debug.Log("调用SceneDisplayManager.AddActor...");
                string actorId = SceneDisplayManager.Instance.AddActor(modelId);
                
                if (string.IsNullOrEmpty(actorId))
                {
                    Debug.LogError("AddActor返回null或空");
                }
                else
                {
                    Debug.Log($"AddActor成功，ID={actorId}");
                    
                    // 检查Actors容器
                    var actorsContainer = GameObject.Find("Actors");
                    if (actorsContainer == null)
                    {
                        Debug.LogError("找不到Actors容器");
                    }
                    else
                    {
                        Debug.Log($"Actors容器找到，子对象数量: {actorsContainer.transform.childCount}");
                        for (int i = 0; i < actorsContainer.transform.childCount; i++)
                        {
                            var child = actorsContainer.transform.GetChild(i);
                            Debug.Log($"  子对象 {i}: {child.name}");
                        }
                    }
                }
            }
            
            Debug.Log("=== Models Container Test 结束 ===");
        }
    }
}
