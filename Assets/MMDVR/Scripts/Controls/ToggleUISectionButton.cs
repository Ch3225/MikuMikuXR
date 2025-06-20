using UnityEngine;
using UnityEngine.UI;
using MMDVR.Events;

namespace MMDVR.Scripts.Controls
{
    /// <summary>
    /// 桌面UI切换按钮 - 使用统一事件系统触发UI显示/隐藏
    /// </summary>
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
        }        public void OnToggleUIClicked()
        {
            // 通过统一事件系统触发UI切换（桌面输入源）
            InputEvents.TriggerUIToggle(InputEvents.InputSource.Desktop);
            
            // 更新视觉状态（基于targetUI的状态，如果有的话）
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (targetGraphic == null || targetUI == null) return;
            targetGraphic.color = targetUI.activeSelf ? normalColor : pressedColor;
        }
    }
}
