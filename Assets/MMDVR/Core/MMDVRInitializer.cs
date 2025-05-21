using UnityEngine;
using LibMMD.Unity3D;

namespace MMDVR
{
    [DefaultExecutionOrder(-200)] // 确保在其他脚本之前执行
    public class MMDVRInitializer : MonoBehaviour
    {
        private static bool _initialized = false;
        
        private void Awake()
        {
            if (!_initialized)
            {
                Debug.Log("初始化MMDVR核心系统...");
                
                // 确保MmdResourceManager已创建
                var managerGo = new GameObject("MmdResourceManager");
                var manager = managerGo.AddComponent<MmdResourceManager>();
                DontDestroyOnLoad(managerGo);
                
                // 其他初始化逻辑...
                
                _initialized = true;
            }
        }
        
        // 在这里添加应用级别的清理逻辑
        private void OnApplicationQuit()
        {
            Debug.Log("MMDVR应用退出，执行全局清理...");
            
            // 查找所有可能需要清理的资源
            var mmdObjects = FindObjectsOfType<MmdGameObject>();
            foreach (var mmd in mmdObjects)
            {
                // 确保所有MmdGameObject组件都调用了适当的清理
                if (mmd != null)
                {
                    try
                    {
                        // 激活OnApplicationQuit
                        mmd.SendMessage("OnApplicationQuit", SendMessageOptions.DontRequireReceiver);
                    }
                    catch
                    {
                        // 忽略错误
                    }
                }
            }
        }
    }
}
