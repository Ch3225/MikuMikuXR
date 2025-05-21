using UnityEngine;
using System.Collections;

namespace MMDVR.VR
{
    /// <summary>
    /// 用于连接Unity XR系统和SteamVR的桥接类
    /// 注意：需要先安装SteamVR插件，之后取消相应代码的注释
    /// </summary>
    public class SteamVRBridge : MonoBehaviour
    {
        public static SteamVRBridge Instance { get; private set; }

        [Header("SteamVR组件")]
        public bool useSteamVRInput = true;
        public bool useSteamVRTracking = true;

        // SteamVR相关组件（安装SteamVR后取消注释）
        // public SteamVR_Action_Vibration hapticAction;
        // public SteamVR_Action_Boolean grabPinchAction;
        // public SteamVR_Action_Boolean grabGripAction;

        private bool _steamVRInitialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // 启动时初始化SteamVR（如果需要）
            if (useSteamVRInput || useSteamVRTracking)
            {
                StartCoroutine(InitializeSteamVR());
            }
        }

        /// <summary>
        /// 初始化SteamVR
        /// </summary>
        private IEnumerator InitializeSteamVR()
        {
            Debug.Log("正在初始化SteamVR...");
            
            // 检查SteamVR是否可用
            // 注意：这里的代码需要在安装SteamVR插件后取消注释
            /*
            try {
                var steamVR = SteamVR.instance;
                if (steamVR != null)
                {
                    Debug.Log("SteamVR已启动");
                    _steamVRInitialized = true;
                    
                    // 配置SteamVR组件
                    ConfigureSteamVR();
                }
                else
                {
                    Debug.LogWarning("无法获取SteamVR实例");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("SteamVR初始化错误: " + e.Message);
            }
            */

            // 在这种情况下，我们将使用Unity XR系统
            Debug.Log("将使用Unity XR系统代替SteamVR");
            _steamVRInitialized = false;

            yield return null;
        }

        /// <summary>
        /// 配置SteamVR组件
        /// </summary>
        private void ConfigureSteamVR()
        {
            // 注意：这里的代码需要在安装SteamVR插件后取消注释
            /*
            // 查找和配置SteamVR相机
            var vrCamera = VRManager.Instance?.VRCamera;
            if (vrCamera != null)
            {
                // 添加SteamVR相机组件
                var steamVRCamera = vrCamera.gameObject.GetComponent<SteamVR_Camera>();
                if (steamVRCamera == null)
                {
                    steamVRCamera = vrCamera.gameObject.AddComponent<SteamVR_Camera>();
                }
            }

            // 配置SteamVR输入
            if (useSteamVRInput)
            {
                SteamVR_Actions.PreInitialize();
                SteamVR_Input.Initialize();
            }

            // 配置手柄
            var leftController = VRManager.Instance?.LeftController;
            var rightController = VRManager.Instance?.RightController;

            if (leftController != null)
            {
                var trackedObject = leftController.GetComponent<SteamVR_Behaviour_Pose>();
                if (trackedObject == null)
                {
                    trackedObject = leftController.AddComponent<SteamVR_Behaviour_Pose>();
                    trackedObject.inputSource = SteamVR_Input_Sources.LeftHand;
                }
                
                // 可以添加可视化模型
                // var handModel = leftController.AddComponent<SteamVR_Skeleton_Poser>();
            }

            if (rightController != null)
            {
                var trackedObject = rightController.GetComponent<SteamVR_Behaviour_Pose>();
                if (trackedObject == null)
                {
                    trackedObject = rightController.AddComponent<SteamVR_Behaviour_Pose>();
                    trackedObject.inputSource = SteamVR_Input_Sources.RightHand;
                }
                
                // 可以添加可视化模型
                // var handModel = rightController.AddComponent<SteamVR_Skeleton_Poser>();
            }
            */
            
            Debug.Log("SteamVR已配置完成");
        }

        /// <summary>
        /// 振动控制器
        /// </summary>
        public void HapticPulse(bool isLeft, float duration, float amplitude, float frequency)
        {
            if (!_steamVRInitialized)
            {
                // 使用Unity XR的振动
                if (VRInputController.Instance != null)
                {
                    if (isLeft)
                        VRInputController.Instance.VibrateLeftController(amplitude, duration);
                    else
                        VRInputController.Instance.VibrateRightController(amplitude, duration);
                }
                return;
            }

            // 注意：这里的代码需要在安装SteamVR插件后取消注释
            /*
            if (hapticAction != null)
            {
                var hand = isLeft ? SteamVR_Input_Sources.LeftHand : SteamVR_Input_Sources.RightHand;
                hapticAction.Execute(0, duration, frequency, amplitude, hand);
            }
            */
        }

        /// <summary>
        /// 获取是否使用SteamVR进行跟踪
        /// </summary>
        public bool IsUsingSteamVRTracking()
        {
            return _steamVRInitialized && useSteamVRTracking;
        }

        /// <summary>
        /// 获取是否使用SteamVR输入
        /// </summary>
        public bool IsUsingSteamVRInput()
        {
            return _steamVRInitialized && useSteamVRInput;
        }

        /// <summary>
        /// SteamVR是否已初始化
        /// </summary>
        public bool IsSteamVRInitialized()
        {
            return _steamVRInitialized;
        }

        /// <summary>
        /// 启用SteamVR跟踪
        /// </summary>
        public void EnableSteamVRTracking(bool enable)
        {
            useSteamVRTracking = enable;
            
            // 如果SteamVR已初始化，应用更改
            if (_steamVRInitialized)
            {
                ConfigureSteamVR();
            }
        }

        /// <summary>
        /// 启用SteamVR输入
        /// </summary>
        public void EnableSteamVRInput(bool enable)
        {
            useSteamVRInput = enable;
            
            // 如果SteamVR已初始化，应用更改
            if (_steamVRInitialized)
            {
                ConfigureSteamVR();
            }
        }
    }
}
