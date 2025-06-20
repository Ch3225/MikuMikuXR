using UnityEngine;
using TMPro;

namespace MMDVR.Scripts.Controls
{
    public class ToggleVRButton : MonoBehaviour
    {
        [SerializeField] private GameObject desktopCamerasGroup;
        [SerializeField] private TextMeshProUGUI buttonText;
        private bool isVRMode = false;

        public void OnToggleVRClicked()
        {
            isVRMode = !isVRMode;
            if (desktopCamerasGroup != null)
                desktopCamerasGroup.SetActive(!isVRMode);
            if (buttonText != null)
                buttonText.text = isVRMode ? "Toggle Desktop(V)" : "Toggle VR(V)";
        }
    }
}
