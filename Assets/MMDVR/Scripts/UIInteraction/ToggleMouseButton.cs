using UnityEngine;
using UnityEngine.UI;
using MMDVR.Scripts.Components.SceneItems;

namespace MMDVR.Scripts.UIInteraction
{
    /// <summary>
    /// 鼠标控制切换按钮
    /// - 按下时：摄像机始终可以自由移动
    /// - 未按下时：只有按住右键时才能移动摄像机
    /// </summary>
    public class ToggleMouseButton : MonoBehaviour
    {
        [SerializeField] private GameObject freeCamera;
        [SerializeField] private Button button; // 按钮本身
        [SerializeField] private CameraComponent cameraController; // 摄像机控制器
        
        private bool isToggleMode = false; // 是否为Toggle模式（按下状态）
        private Color normalColor;
        private Color pressedColor;
        private Image targetGraphic;

        void Start()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (button != null && button.targetGraphic != null)
            {
                targetGraphic = button.targetGraphic as Image;
                normalColor = button.colors.normalColor;
                pressedColor = button.colors.pressedColor;
            }
            
            // 查找摄像机控制器
            if (cameraController == null && freeCamera != null)
            {
                cameraController = freeCamera.GetComponent<CameraComponent>();
            }
        }        void Update()
        {
            // Tab键切换现在由KeyboardInputManager统一管理
            // Tab键处理已移动到KeyboardInputManager中
            
            // 更新按钮视觉状态
            UpdateButtonVisual();
            
            // 处理摄像机控制逻辑
            HandleCameraControl();
        }

        public void OnToggleMouseClicked()
        {
            ToggleMouseMode();
        }        private void ToggleMouseMode()
        {
            if (freeCamera != null && freeCamera.activeInHierarchy)
            {
                isToggleMode = !isToggleMode;
                Debug.Log($"ToggleMouseButton: 切换到 {(isToggleMode ? "Toggle模式（始终移动）" : "右键模式（按住右键移动）")}");
                
                if (cameraController != null)
                {
                    cameraController.SetControlMode(isToggleMode ? CameraComponent.ControlMode.Always : CameraComponent.ControlMode.RightClickOnly);
                    
                    // 立即应用新模式的鼠标状态
                    if (!isToggleMode)
                    {
                        // 切换到右键模式时，立即释放鼠标锁定
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                        cameraController.SetMouseLocked(false);
                        Debug.Log("ToggleMouseButton: 鼠标已解锁并显示");
                    }
                }
            }
        }private void HandleCameraControl()
        {
            if (freeCamera == null || !freeCamera.activeInHierarchy || cameraController == null)
                return;
                
            if (isToggleMode)
            {
                // Toggle模式：始终可以移动，鼠标锁定
                if (!cameraController.IsMouseLocked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    cameraController.SetMouseLocked(true);
                }
            }
            else
            {
                // 右键模式：只有按住右键时才能移动
                if (UnityEngine.Input.GetMouseButtonDown(1))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    cameraController.SetMouseLocked(true);
                }
                else if (UnityEngine.Input.GetMouseButtonUp(1))
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    cameraController.SetMouseLocked(false);
                }
                else if (!UnityEngine.Input.GetMouseButton(1) && cameraController.IsMouseLocked)
                {
                    // 确保在非右键状态下鼠标是解锁和可见的
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    cameraController.SetMouseLocked(false);
                }
            }
        }

        private void UpdateButtonVisual()
        {
            if (targetGraphic == null) return;
            if (isToggleMode)
                targetGraphic.color = pressedColor;
            else
                targetGraphic.color = normalColor;
        }
    }
}
