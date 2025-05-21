using UnityEngine;
using System.Collections.Generic;
using LibMMD.Unity3D.BonePose;

namespace LibMMD.Unity3D
{
    /// <summary>
    /// 管理MmdGameObject相关的全局资源，确保在域重载或应用退出时正确释放
    /// </summary>
    [DefaultExecutionOrder(-100)] // 确保这个组件早于其他组件初始化和销毁
    public class MmdResourceManager : MonoBehaviour
    {
        private static MmdResourceManager _instance;
        private static readonly List<BonePoseCalculatorWorker> _workers = new List<BonePoseCalculatorWorker>();

        /// <summary>
        /// 注册一个BonePoseCalculatorWorker以便在应用退出时清理
        /// </summary>
        public static void RegisterWorker(BonePoseCalculatorWorker worker)
        {
            if (worker != null && !_workers.Contains(worker))
            {
                _workers.Add(worker);
            }
            
            // 确保管理器已创建
            EnsureInstance();
        }

        /// <summary>
        /// 确保管理器已实例化
        /// </summary>
        private static void EnsureInstance()
        {
            if (_instance == null)
            {
                // 查找已存在的实例
                _instance = FindObjectOfType<MmdResourceManager>();
                
                // 如果没有找到，创建一个新的
                if (_instance == null)
                {
                    GameObject go = new GameObject("MmdResourceManager");
                    _instance = go.AddComponent<MmdResourceManager>();
                    DontDestroyOnLoad(go); // 确保在场景加载间保持存在
                }
            }
        }

        private void Awake()
        {
            // 如果已存在实例，销毁这个新实例
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 注册域重载处理（仅在编辑器模式）
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += CleanupBeforeDomainReload;
#endif
        }

        private void OnApplicationQuit()
        {
            CleanupAllResources();
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= CleanupBeforeDomainReload;
#endif
            
            if (_instance == this)
            {
                CleanupAllResources();
            }
        }

#if UNITY_EDITOR
        private void CleanupBeforeDomainReload()
        {
            Debug.Log("MmdResourceManager: Cleaning up resources before domain reload");
            CleanupAllResources();
        }
#endif

        /// <summary>
        /// 清理所有注册的资源
        /// </summary>
        private void CleanupAllResources()
        {
            // 停止并终止所有工作线程
            foreach (var worker in _workers)
            {
                if (worker != null)
                {
                    try
                    {
                        worker.StopAllAndTerminate();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Error terminating worker: " + e.Message);
                    }
                }
            }
            
            _workers.Clear();
        }
    }
}
