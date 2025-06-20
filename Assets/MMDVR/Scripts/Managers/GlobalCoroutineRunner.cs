using UnityEngine;

namespace MMDVR.Scripts.Managers
{    /// <summary>
    /// 全局协程管理器，保证即使UI被隐藏也能正常启动/停止协程。
    /// 挂载到 MainUI 或 Canvas 下即可。
    /// 
    /// [已废弃] 请使用 ResourceManager.Instance.StartGlobalCoroutine() 替代
    /// ResourceManager已经集成了全局协程管理功能
    /// </summary>
    [System.Obsolete("请使用 ResourceManager.Instance.StartGlobalCoroutine() 替代")]
    public class GlobalCoroutineRunner : MonoBehaviour
    {
        public static GlobalCoroutineRunner Instance { get; private set; }
        void Awake()
        {
            Debug.LogWarning("GlobalCoroutineRunner已废弃，请使用ResourceManager.Instance.StartGlobalCoroutine()替代");
            
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// [已废弃] 请使用 ResourceManager.Instance.StartGlobalCoroutine() 替代
        /// </summary>
        [System.Obsolete("请使用 ResourceManager.Instance.StartGlobalCoroutine() 替代")]
        public Coroutine StartGlobalCoroutine(System.Collections.IEnumerator routine)
        {
            return StartCoroutine(routine);
        }
    }
}
