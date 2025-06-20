using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Diagnostics;

namespace MMDVR.UI.Controls
{
    public class ShutdownButton : MonoBehaviour
    {
        // 供Button OnClick事件绑定
        public void OnShutdownClicked()
        {
            UnityEngine.Debug.Log("Shutdown Clicked");
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
            // 强制退出（仅限Windows平台）
            Process.GetCurrentProcess().Kill();
#endif
        }
    }
}
