using MikuMikuXR.SceneController;
using MikuMikuXR.XR;
using UnityEngine;

namespace MikuMikuXR.UI.Page
{
    public class XrSelecror : HideOtherPage
    {
        public XrSelecror()
        {
            uiPath = PrefabPaths.XrSelectorPath;
        }

        public override void Awake(GameObject go)
        {
            SetButtonListener("BtnVRManual", () =>
            {
                ChangeXrType(XrType.VrManual);
            });
            SetButtonListener("BtnVRSingle", () =>
            {
                ChangeXrType(XrType.VrSingle);
            });
            SetButtonListener("BtnVRGlass", () =>
            {
                ChangeXrType(XrType.VrGlass);
            });
            SetButtonListener("BtnCameraFile", () =>
            {
                ChangeXrType(XrType.CameraFile);
            });
            
            // 隐藏不需要的按钮
            HideButton("BtnArUserDefined");
            HideButton("BtnPyramid");
            
            SetButtonListener("BtnBack", ClosePage);
        }

        private static void ChangeXrType(XrType xrType)
        {
            MainSceneController.Instance.ChangeXrType(xrType);
            ClosePage();
        }
        
        private void HideButton(string buttonName)
        {
            Transform button = transform.Find(buttonName);
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }
        }
    }
}