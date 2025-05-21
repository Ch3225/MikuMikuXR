using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using System.Collections.Generic;
using MMDVR.Managers;

namespace MMDVR.VR
{
    /// <summary>
    /// 管理VR设备和交互的主要组件
    /// </summary>
    public class VRManager : MonoBehaviour
    {
        public static VRManager Instance { get; private set; }

        [Header("VR相机设置")]
        public Camera VRCamera;
        public GameObject LeftController;
        public GameObject RightController;

        [Header("VR设备状态")]
        public bool IsVREnabled = false;

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
            // 检查XR设备是否可用并初始化
            InitializeXR();
        }

        /// <summary>
        /// 初始化XR设备
        /// </summary>
        public void InitializeXR()
        {
            var xrSettings = XRGeneralSettings.Instance;
            if (xrSettings == null)
            {
                Debug.LogWarning("XR设置未找到，无法启动VR模式");
                return;
            }

            var xrManager = xrSettings.Manager;
            if (xrManager == null)
            {
                Debug.LogWarning("XR管理器未找到，无法启动VR模式");
                return;
            }

            // 启动VR
            if (!xrManager.isInitializationComplete)
            {
                xrManager.InitializeLoaderSync();
            }
            
            if (xrManager.activeLoader != null)
            {
                IsVREnabled = true;
                Debug.Log("VR设备已激活: " + xrManager.activeLoader.name);
                SetupVRCamera();
            }
            else
            {
                Debug.LogWarning("没有找到可用的VR设备");
                IsVREnabled = false;
            }
        }

        /// <summary>
        /// 设置VR相机和控制器
        /// </summary>
        private void SetupVRCamera()
        {
            // 如果没有指定VR相机，尝试在场景中查找或创建一个
            if (VRCamera == null)
            {
                // 首先尝试查找现有的主相机
                VRCamera = Camera.main;
                
                // 如果没有主相机，创建一个
                if (VRCamera == null)
                {
                    GameObject cameraObj = new GameObject("VR Camera");
                    VRCamera = cameraObj.AddComponent<Camera>();
                    cameraObj.tag = "MainCamera"; // 设置为主相机
                    VRCamera.nearClipPlane = 0.01f;
                    VRCamera.farClipPlane = 1000f;
                }
            }

            // 确保相机启用了立体渲染
            VRCamera.stereoTargetEye = StereoTargetEyeMask.Both;
            
            // 如果使用SteamVR，可以添加SteamVR_Camera组件
            // 当您安装了SteamVR插件后，取消这里的注释
            // if (!VRCamera.gameObject.GetComponent<SteamVR_Camera>())
            // {
            //     VRCamera.gameObject.AddComponent<SteamVR_Camera>();
            // }

            // 设置控制器（当安装了SteamVR后可以扩展这部分）
            SetupControllers();
        }

        /// <summary>
        /// 设置VR控制器
        /// </summary>
        private void SetupControllers()
        {
            // 当安装了SteamVR插件后，这部分可以扩展为使用SteamVR控制器
            // 现在我们简单地创建一些游戏对象代表控制器位置
            if (LeftController == null)
            {
                LeftController = new GameObject("Left Controller");
                LeftController.transform.parent = VRCamera.transform;
                LeftController.transform.localPosition = new Vector3(-0.2f, -0.2f, 0.3f);
            }

            if (RightController == null)
            {
                RightController = new GameObject("Right Controller");
                RightController.transform.parent = VRCamera.transform;
                RightController.transform.localPosition = new Vector3(0.2f, -0.2f, 0.3f);
            }

            // 添加简单的可视化效果
            AddControllerVisuals(LeftController);
            AddControllerVisuals(RightController);
        }

        /// <summary>
        /// 为控制器添加简单的可视化
        /// </summary>
        private void AddControllerVisuals(GameObject controller)
        {
            if (controller.GetComponent<MeshFilter>() == null)
            {
                var meshFilter = controller.AddComponent<MeshFilter>();
                meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                
                var renderer = controller.AddComponent<MeshRenderer>();
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = Color.blue;
                
                // 调整大小
                controller.transform.localScale = new Vector3(0.05f, 0.05f, 0.1f);
            }
        }

        /// <summary>
        /// 关闭并清理XR系统
        /// </summary>
        public void ShutdownVR()
        {
            var xrSettings = XRGeneralSettings.Instance;
            if (xrSettings != null && xrSettings.Manager != null)
            {
                xrSettings.Manager.DeinitializeLoader();
                IsVREnabled = false;
                Debug.Log("VR设备已关闭");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                ShutdownVR();
            }
        }

        /// <summary>
        /// 获取VR相机位置和朝向
        /// </summary>
        public (Vector3 position, Quaternion rotation) GetHeadPose()
        {
            if (VRCamera != null)
            {
                return (VRCamera.transform.position, VRCamera.transform.rotation);
            }
            return (Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// 获取左手控制器位置和朝向
        /// </summary>
        public (Vector3 position, Quaternion rotation) GetLeftControllerPose()
        {
            if (LeftController != null)
            {
                return (LeftController.transform.position, LeftController.transform.rotation);
            }
            return (Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// 获取右手控制器位置和朝向
        /// </summary>
        public (Vector3 position, Quaternion rotation) GetRightControllerPose()
        {
            if (RightController != null)
            {
                return (RightController.transform.position, RightController.transform.rotation);
            }
            return (Vector3.zero, Quaternion.identity);
        }
    }
}
