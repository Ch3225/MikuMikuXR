using UnityEngine;
using MMDVR.VR;

namespace MMDVR
{
    /// <summary>
    /// 启动时初始化VR系统
    /// </summary>
    public class VRBootstrap : MonoBehaviour
    {
        [Header("配置")]
        public bool autoStartVR = true;
        public bool loadSteamVR = true;
        
        [Header("预制体")]
        public GameObject vrSystemPrefab;
        public GameObject steamVRBridgePrefab;

        private void Awake()
        {
            if (autoStartVR)
            {
                InitializeVRSystem();
            }
        }

        /// <summary>
        /// 初始化VR系统
        /// </summary>
        public void InitializeVRSystem()
        {
            // 创建VR系统
            if (VRSystem.Instance == null)
            {
                if (vrSystemPrefab != null)
                {
                    Instantiate(vrSystemPrefab);
                }
                else
                {
                    var vrSystemGO = new GameObject("VR System");
                    vrSystemGO.AddComponent<VRSystem>();
                }
            }

            // 如果需要，创建SteamVR桥接
            if (loadSteamVR && SteamVRBridge.Instance == null)
            {
                if (steamVRBridgePrefab != null)
                {
                    Instantiate(steamVRBridgePrefab);
                }
                else
                {
                    var steamVRBridgeGO = new GameObject("SteamVR Bridge");
                    steamVRBridgeGO.AddComponent<SteamVRBridge>();
                }
            }
        }
    }
}
