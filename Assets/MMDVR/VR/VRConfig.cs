using UnityEngine;

namespace MMDVR.VR
{
    /// <summary>
    /// 存储VR系统配置
    /// </summary>
    [System.Serializable]
    public class VRSettings
    {
        // 相机设置
        public float eyeDistance = 0.064f; // 标准眼间距（米）
        public float nearClipPlane = 0.01f;
        public float farClipPlane = 1000f;

        // 控制器设置
        public float controllerSensitivity = 1.0f;
        public bool invertYAxis = false;

        // 运动设置
        public bool smoothLocomotion = true;
        public float movementSpeed = 2.0f;
        public float rotationSpeed = 90.0f; // 每秒旋转角度
        public bool snapTurn = false;
        public float snapTurnAngle = 30.0f;

        // 舒适度设置
        public bool vignetteEffect = true;
        public bool reduceMotionSickness = true;
    }

    /// <summary>
    /// VR配置管理器
    /// </summary>
    public class VRConfig : MonoBehaviour
    {
        public static VRConfig Instance { get; private set; }

        [Header("VR设置")]
        public VRSettings settings = new VRSettings();

        [Header("自动应用设置")]
        public bool autoApplySettings = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            
            LoadSettings();
        }

        private void Start()
        {
            if (autoApplySettings)
            {
                ApplySettings();
            }
        }

        /// <summary>
        /// 应用设置到VR系统
        /// </summary>
        public void ApplySettings()
        {
            // 应用相机设置
            if (VRCameraController.Instance != null)
            {
                VRCameraController.Instance.eyeDistanceScale = settings.eyeDistance;
                VRCameraController.Instance.nearClipPlane = settings.nearClipPlane;
                VRCameraController.Instance.farClipPlane = settings.farClipPlane;
                
                // 如果相机已经初始化过，重新应用这些设置
                if (VRCameraController.Instance.vrCamera != null)
                {
                    VRCameraController.Instance.vrCamera.nearClipPlane = settings.nearClipPlane;
                    VRCameraController.Instance.vrCamera.farClipPlane = settings.farClipPlane;
                }
            }
            
            // 其他设置可以在这里应用到相应的组件
        }

        /// <summary>
        /// 从PlayerPrefs加载设置
        /// </summary>
        public void LoadSettings()
        {
            settings.eyeDistance = PlayerPrefs.GetFloat("VR_EyeDistance", settings.eyeDistance);
            settings.nearClipPlane = PlayerPrefs.GetFloat("VR_NearClip", settings.nearClipPlane);
            settings.farClipPlane = PlayerPrefs.GetFloat("VR_FarClip", settings.farClipPlane);
            settings.controllerSensitivity = PlayerPrefs.GetFloat("VR_ControllerSensitivity", settings.controllerSensitivity);
            settings.invertYAxis = PlayerPrefs.GetInt("VR_InvertY", settings.invertYAxis ? 1 : 0) == 1;
            settings.smoothLocomotion = PlayerPrefs.GetInt("VR_SmoothLocomotion", settings.smoothLocomotion ? 1 : 0) == 1;
            settings.movementSpeed = PlayerPrefs.GetFloat("VR_MovementSpeed", settings.movementSpeed);
            settings.rotationSpeed = PlayerPrefs.GetFloat("VR_RotationSpeed", settings.rotationSpeed);
            settings.snapTurn = PlayerPrefs.GetInt("VR_SnapTurn", settings.snapTurn ? 1 : 0) == 1;
            settings.snapTurnAngle = PlayerPrefs.GetFloat("VR_SnapTurnAngle", settings.snapTurnAngle);
            settings.vignetteEffect = PlayerPrefs.GetInt("VR_Vignette", settings.vignetteEffect ? 1 : 0) == 1;
            settings.reduceMotionSickness = PlayerPrefs.GetInt("VR_ReduceMotionSickness", settings.reduceMotionSickness ? 1 : 0) == 1;
        }

        /// <summary>
        /// 保存设置到PlayerPrefs
        /// </summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetFloat("VR_EyeDistance", settings.eyeDistance);
            PlayerPrefs.SetFloat("VR_NearClip", settings.nearClipPlane);
            PlayerPrefs.SetFloat("VR_FarClip", settings.farClipPlane);
            PlayerPrefs.SetFloat("VR_ControllerSensitivity", settings.controllerSensitivity);
            PlayerPrefs.SetInt("VR_InvertY", settings.invertYAxis ? 1 : 0);
            PlayerPrefs.SetInt("VR_SmoothLocomotion", settings.smoothLocomotion ? 1 : 0);
            PlayerPrefs.SetFloat("VR_MovementSpeed", settings.movementSpeed);
            PlayerPrefs.SetFloat("VR_RotationSpeed", settings.rotationSpeed);
            PlayerPrefs.SetInt("VR_SnapTurn", settings.snapTurn ? 1 : 0);
            PlayerPrefs.SetFloat("VR_SnapTurnAngle", settings.snapTurnAngle);
            PlayerPrefs.SetInt("VR_Vignette", settings.vignetteEffect ? 1 : 0);
            PlayerPrefs.SetInt("VR_ReduceMotionSickness", settings.reduceMotionSickness ? 1 : 0);
            
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 将设置恢复为默认值
        /// </summary>
        public void ResetToDefaults()
        {
            settings = new VRSettings();
            SaveSettings();
            ApplySettings();
        }
    }
}
