using UnityEngine;
using UnityEngine.UI;

namespace MMDVR.UI.Controls
{
    public class ToggleMouseButton : MonoBehaviour
    {
        [SerializeField] private GameObject freeCamera;
        [SerializeField] private Button button; // 按钮本身
        private bool isMouseLocked = false;
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
        }

        void Update()
        {
            // Tab键切换鼠标锁定
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleMouseLock();
            }
            // 按钮视觉状态同步
            UpdateButtonVisual();
        }

        public void OnToggleMouseClicked()
        {
            ToggleMouseLock();
        }

        private void ToggleMouseLock()
        {
            if (freeCamera != null && freeCamera.activeInHierarchy)
            {
                isMouseLocked = !isMouseLocked;
                if (isMouseLocked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }

        private void UpdateButtonVisual()
        {
            if (targetGraphic == null) return;
            if (isMouseLocked)
                targetGraphic.color = pressedColor;
            else
                targetGraphic.color = normalColor;
        }
    }
}
