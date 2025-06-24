using UnityEngine;
using MMDVR.Scripts.Managers;

namespace MMDVR.Scripts.Components.SceneItems
{    /// <summary>
    /// 摄像机组件
    /// 支持两种控制模式：
    /// 1. Always - 始终可以移动摄像机
    /// 2. RightClickOnly - 只有按住右键时才能移动摄像机
    /// 
    /// 输入处理：
    /// - 内部处理：直接读取Unity Input（向后兼容）
    /// - 外部控制：通过KeyboardInputManager统一管理
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraComponent : MonoBehaviour
    {
        public enum ControlMode
        {
            Always,          // 始终可以移动
            RightClickOnly   // 只有按右键时才能移动
        }

        [Header("移动设置")]
        [Tooltip("移动速度")]
        public float movementSpeed = 10.0f;
        
        [Tooltip("快速移动倍数 (按住Shift)")]
        public float fastMovementSpeed = 3.0f;
        
        [Tooltip("移动平滑时间")]
        public float positionLerpTime = 0.2f;
        
        [Header("旋转设置")]
        [Tooltip("鼠标灵敏度")]
        public float mouseSensitivity = 2.0f;
        
        [Tooltip("旋转平滑时间")]
        public float rotationLerpTime = 0.01f;
        
        [Tooltip("是否反转Y轴")]
        public bool invertY = false;
        
        [Header("控制设置")]
        [Tooltip("控制模式")]
        public ControlMode controlMode = ControlMode.RightClickOnly;
        
        [Tooltip("是否激活摄像机控制")]
        public bool isActive = true;
        
        [Header("摄像机数据")]
        [Tooltip("摄像机ID")]
        public string id = "";
        
        [Tooltip("显示名称")]
        public string displayName = "";
        
        // 公共属性
        public bool IsActive => isActive;
        public Vector3 position 
        { 
            get => transform.position; 
            set => transform.position = value; 
        }
        public Quaternion rotation 
        { 
            get => transform.rotation; 
            set => transform.rotation = value; 
        }
        public float fieldOfView 
        { 
            get => GetComponent<Camera>().fieldOfView; 
            set => GetComponent<Camera>().fieldOfView = value; 
        }

        // 内部状态
        private class CameraState
        {
            public float yaw;
            public float pitch;
            public float roll;
            public Vector3 position;

            public void SetFromTransform(Transform t)
            {
                Vector3 eulerAngles = t.eulerAngles;
                pitch = eulerAngles.x;
                yaw = eulerAngles.y;
                roll = eulerAngles.z;
                position = t.position;
            }

            public void Translate(Vector3 translation)
            {
                Vector3 rotatedTranslation = Quaternion.Euler(pitch, yaw, roll) * translation;
                position += rotatedTranslation;
            }

            public void LerpTowards(CameraState target, float positionLerpPct, float rotationLerpPct)
            {
                yaw = Mathf.LerpAngle(yaw, target.yaw, rotationLerpPct);
                pitch = Mathf.LerpAngle(pitch, target.pitch, rotationLerpPct);
                roll = Mathf.LerpAngle(roll, target.roll, rotationLerpPct);
                position = Vector3.Lerp(position, target.position, positionLerpPct);
            }

            public void UpdateTransform(Transform t)
            {
                t.eulerAngles = new Vector3(pitch, yaw, roll);
                t.position = position;
            }
        }

        private CameraState m_TargetCameraState = new CameraState();
        private CameraState m_InterpolatingCameraState = new CameraState();
        private bool isMouseLocked = false;

        public bool IsMouseLocked => isMouseLocked;

        void OnEnable()
        {
            m_TargetCameraState.SetFromTransform(transform);
            m_InterpolatingCameraState.SetFromTransform(transform);
            if (KeyboardInputManager.Instance != null)
            {
                KeyboardInputManager.OnResetCamera += ResetToTransform;
            }
        }
        void OnDisable()
        {
            if (KeyboardInputManager.Instance != null)
            {
                KeyboardInputManager.OnResetCamera -= ResetToTransform;
            }
        }
        void Update()
        {
            // 只做插值和平滑，不再处理输入
            var positionLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / positionLerpTime) * Time.deltaTime);
            var rotationLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / rotationLerpTime) * Time.deltaTime);
            m_InterpolatingCameraState.LerpTowards(m_TargetCameraState, positionLerpPct, rotationLerpPct);
            m_InterpolatingCameraState.UpdateTransform(transform);
            // 用KeyboardInputManager单例配置处理移动/旋转
            var km = KeyboardInputManager.Instance;
            if (isActive && ShouldProcessInput() && km != null)
            {
                ProcessMovementInput_KeyboardManager(km);
                ProcessRotationInput_KeyboardManager();
            }
        }        private bool ShouldProcessInput()
        {
            switch (controlMode)
            {
                case ControlMode.Always:
                    return isMouseLocked;
                case ControlMode.RightClickOnly:
                    return UnityEngine.Input.GetMouseButton(1) && isMouseLocked;
                default:
                    return false;
            }
        }        private void ProcessRotationInput()
        {
            var mouseMovement = new Vector2(UnityEngine.Input.GetAxis("Mouse X"), UnityEngine.Input.GetAxis("Mouse Y") * (invertY ? 1 : -1));
            
            m_TargetCameraState.yaw += mouseMovement.x * mouseSensitivity;
            m_TargetCameraState.pitch += mouseMovement.y * mouseSensitivity;
            
            // 限制pitch角度，避免翻转
            m_TargetCameraState.pitch = Mathf.Clamp(m_TargetCameraState.pitch, -90f, 90f);
        }
        // 新增：用KeyboardInputManager配置处理移动
        private void ProcessMovementInput_KeyboardManager(KeyboardInputManager km)
        {
            Vector3 direction = Vector3.zero;
            if (Input.GetKey(km.moveForward)) direction += Vector3.forward;
            if (Input.GetKey(km.moveBackward)) direction += Vector3.back;
            if (Input.GetKey(km.moveLeft)) direction += Vector3.left;
            if (Input.GetKey(km.moveRight)) direction += Vector3.right;
            if (Input.GetKey(km.moveUp)) direction += Vector3.up;
            if (Input.GetKey(km.moveDown)) direction += Vector3.down;
            if (direction.magnitude > 0.1f)
            {
                direction = direction.normalized;
                float speed = movementSpeed;
                if (Input.GetKey(km.fastMovement)) speed *= fastMovementSpeed;
                Vector3 translation = direction * speed * Time.deltaTime;
                m_TargetCameraState.Translate(translation);
            }
        }
        // 新增：用鼠标输入处理旋转
        private void ProcessRotationInput_KeyboardManager()
        {
            var mouseMovement = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y") * (invertY ? 1 : -1));
            m_TargetCameraState.yaw += mouseMovement.x * mouseSensitivity;
            m_TargetCameraState.pitch += mouseMovement.y * mouseSensitivity;
            m_TargetCameraState.pitch = Mathf.Clamp(m_TargetCameraState.pitch, -90f, 90f);
        }

        // 公共方法
        public void SetControlMode(ControlMode mode)
        {
            controlMode = mode;
        }

        public void SetMouseLocked(bool locked)
        {
            isMouseLocked = locked;
        }

        /// <summary>
        /// 外部控制移动 - 供KeyboardInputManager调用
        /// </summary>
        /// <param name="direction">移动方向向量</param>
        /// <param name="useFastSpeed">是否使用快速移动</param>
        public void ApplyMovement(Vector3 direction, bool useFastSpeed = false)
        {
            if (!isActive || direction.sqrMagnitude < 1e-4f) return;
            float speed = movementSpeed;
            if (useFastSpeed) speed *= fastMovementSpeed;
            Vector3 translation = direction.normalized * speed * Time.deltaTime;
            m_TargetCameraState.Translate(translation);
        }

        public void ApplyRotation(float yawDelta, float pitchDelta)
        {
            if (!isActive) return;
            m_TargetCameraState.yaw += yawDelta * mouseSensitivity;
            m_TargetCameraState.pitch += pitchDelta * mouseSensitivity * (invertY ? 1 : -1);
            m_TargetCameraState.pitch = Mathf.Clamp(m_TargetCameraState.pitch, -90f, 90f);
        }

        /// <summary>
        /// 获取标准化的移动方向向量（供外部使用）
        /// </summary>
        /// <param name="forward">前进</param>
        /// <param name="back">后退</param>
        /// <param name="left">左移</param>
        /// <param name="right">右移</param>
        /// <param name="up">上升</param>
        /// <param name="down">下降</param>
        /// <returns>归一化的方向向量</returns>
        public static Vector3 GetMovementDirection(bool forward, bool back, bool left, bool right, bool up, bool down)
        {
            Vector3 direction = Vector3.zero;
            
            if (forward) direction += Vector3.forward;
            if (back) direction += Vector3.back;
            if (left) direction += Vector3.left;
            if (right) direction += Vector3.right;
            if (down) direction += Vector3.down;
            if (up) direction += Vector3.up;
                
            return direction;
        }

        public void ResetToTransform()
        {
            m_TargetCameraState.SetFromTransform(transform);
            m_InterpolatingCameraState.SetFromTransform(transform);
        }
    }
}
