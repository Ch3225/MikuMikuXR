using UnityEngine;
using UnityEngine.UI;

namespace MMDVR.UI.Controls
{
    public class ToggleUISectionButton : MonoBehaviour
    {
        [SerializeField] private GameObject targetUI;
        [SerializeField] private Button button;
        private Image targetGraphic;
        private Color normalColor;
        private Color pressedColor;

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
            UpdateVisual();
        }

        public void OnToggleUIClicked()
        {
            if (targetUI != null)
            {
                targetUI.SetActive(!targetUI.activeSelf);
                UpdateVisual();
            }
        }

        private void UpdateVisual()
        {
            if (targetGraphic == null || targetUI == null) return;
            targetGraphic.color = targetUI.activeSelf ? normalColor : pressedColor;
        }
    }
}
