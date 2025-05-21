using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using MMDVR.Managers;

namespace MMDVR.VR
{
    /// <summary>
    /// VR系统初始化和管理
    /// 在场景中作为主要的VR系统启动入口
    /// </summary>
    public class VRSystem : MonoBehaviour
    {
        public static VRSystem Instance { get; private set; }

        [Header("VR组件")]
        public GameObject vrManagerPrefab;
        public GameObject vrCameraControllerPrefab;
        public GameObject vrInputControllerPrefab;
        public GameObject vrConfigPrefab;

        [Header("VR设置")]
        public bool startInVRMode = true;
        public bool integrateWithMMDCamera = true;
        
        private GameObject _vrManagerInstance;
        private GameObject _vrCameraControllerInstance;
        private GameObject _vrInputControllerInstance;
        private GameObject _vrConfigInstance;

        private bool _vrInitialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // 检查XR系统是否可用
            if (XRSettings.isDeviceActive && startInVRMode)
            {
                Debug.Log("XR设备已激活：" + XRSettings.loadedDeviceName);
                InitVRSystem();
            }
            else if (startInVRMode)
            {
                Debug.Log("尝试启动VR系统...");
                StartCoroutine(InitializeXR());
            }
        }

        /// <summary>
        /// 异步初始化XR系统
        /// </summary>
        private System.Collections.IEnumerator InitializeXR()
        {
            Debug.Log("正在初始化XR...");
            
            yield return null;

            var xrSettings = XRGeneralSettings.Instance;
            if (xrSettings == null)
            {
                Debug.LogWarning("XR设置未找到，无法启动VR模式");
                yield break;
            }

            var xrManager = xrSettings.Manager;
            if (xrManager == null)
            {
                Debug.LogWarning("XR管理器未找到，无法启动VR模式");
                yield break;
            }

            if (!xrManager.isInitializationComplete)
            {
                xrManager.InitializeLoaderSync();
            }
                
            if (xrManager.activeLoader != null)
            {
                Debug.Log("XR已成功初始化：" + XRSettings.loadedDeviceName);
                InitVRSystem();
            }
            else
            {
                Debug.LogWarning("未找到可用的XR设备加载器");
            }
        }

        /// <summary>
        /// 初始化VR系统组件
        /// </summary>
        private void InitVRSystem()
        {
            if (_vrInitialized)
                return;

            // 创建VR管理器
            if (vrManagerPrefab != null)
            {
                _vrManagerInstance = Instantiate(vrManagerPrefab);
                _vrManagerInstance.name = "VRManager";
                DontDestroyOnLoad(_vrManagerInstance);
            }
            else
            {
                _vrManagerInstance = new GameObject("VRManager");
                _vrManagerInstance.AddComponent<VRManager>();
                DontDestroyOnLoad(_vrManagerInstance);
            }

            // 创建VR相机控制器
            if (vrCameraControllerPrefab != null)
            {
                _vrCameraControllerInstance = Instantiate(vrCameraControllerPrefab);
                _vrCameraControllerInstance.name = "VRCameraController";
            }
            else
            {
                _vrCameraControllerInstance = new GameObject("VRCameraController");
                _vrCameraControllerInstance.AddComponent<VRCameraController>();
            }

            // 创建VR输入控制器
            if (vrInputControllerPrefab != null)
            {
                _vrInputControllerInstance = Instantiate(vrInputControllerPrefab);
                _vrInputControllerInstance.name = "VRInputController";
            }
            else
            {
                _vrInputControllerInstance = new GameObject("VRInputController");
                _vrInputControllerInstance.AddComponent<VRInputController>();
            }

            // 创建VR配置
            if (vrConfigPrefab != null)
            {
                _vrConfigInstance = Instantiate(vrConfigPrefab);
                _vrConfigInstance.name = "VRConfig";
            }
            else
            {
                _vrConfigInstance = new GameObject("VRConfig");
                _vrConfigInstance.AddComponent<VRConfig>();
            }

            // 如果选择集成MMD相机，查找并连接
            if (integrateWithMMDCamera && CameraManager.Instance != null)
            {
                var mmdCam = CameraManager.Instance.mmdCamera;
                if (mmdCam != null && mmdCam.GetComponent<Camera>() != null)
                {
                    VRCameraController.Instance?.LinkToMMDCamera(mmdCam.GetComponent<Camera>());
                }
            }

            // 将VR相机添加到CameraManager中
            if (VRManager.Instance != null && VRManager.Instance.VRCamera != null && CameraManager.Instance != null)
            {
                CameraManager.Instance.AddCamera(VRManager.Instance.VRCamera.gameObject);
            }

            _vrInitialized = true;
            Debug.Log("VR系统已成功初始化");
        }

        /// <summary>
        /// 关闭VR系统
        /// </summary>
        public void ShutdownVRSystem()
        {
            if (!_vrInitialized)
                return;

            // 关闭VR系统
            if (VRManager.Instance != null)
            {
                VRManager.Instance.ShutdownVR();
            }

            var xrSettings = XRGeneralSettings.Instance;
            if (xrSettings != null && xrSettings.Manager != null)
            {
                if (xrSettings.Manager.activeLoader != null)
                {
                    xrSettings.Manager.DeinitializeLoader();
                }
            }

            // 销毁已创建的组件
            if (_vrManagerInstance != null)
                Destroy(_vrManagerInstance);
            
            if (_vrCameraControllerInstance != null)
                Destroy(_vrCameraControllerInstance);
            
            if (_vrInputControllerInstance != null)
                Destroy(_vrInputControllerInstance);
            
            if (_vrConfigInstance != null)
                Destroy(_vrConfigInstance);

            _vrInitialized = false;
            Debug.Log("VR系统已关闭");
        }

        /// <summary>
        /// VR系统是否已初始化
        /// </summary>
        public bool IsVRInitialized()
        {
            return _vrInitialized;
        }

        /// <summary>
        /// 重新启动VR系统
        /// </summary>
        public void RestartVRSystem()
        {
            ShutdownVRSystem();
            StartCoroutine(InitializeXR());
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                ShutdownVRSystem();
            }
        }
    }
}
