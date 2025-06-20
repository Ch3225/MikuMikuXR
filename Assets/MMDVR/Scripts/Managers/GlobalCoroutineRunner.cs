using UnityEngine;

namespace MMDVR.Scripts.Managers
{
    /// <summary>
    /// 全局协程管理器，保证即使UI被隐藏也能正常启动/停止协程。
    /// 挂载到 MainUI 或 Canvas 下即可。
    /// </summary>
    public class GlobalCoroutineRunner : MonoBehaviour
    {
        public static GlobalCoroutineRunner Instance { get; private set; }
        void Awake()
        {
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
    }
}
