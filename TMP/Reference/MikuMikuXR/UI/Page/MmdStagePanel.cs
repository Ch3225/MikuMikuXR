using System;
using MikuMikuXR.SceneController;
using MikuMikuXR.Utils;
using MikuMikuXR.XR;
using UnityEngine;
using UnityEngine.UI;

namespace MikuMikuXR.UI.Page
{
    public class MmdStagePanel : HideOtherPage
    {
        private GameObject _cameraFilePanel;
        private GameObject _arUserDefinedPanel;
        
        public MmdStagePanel()
        {
            uiPath = PrefabPaths.MmdStagePanelPath;
        }

        public override void Awake(GameObject go)
        {
            // 获取界面元素引用
            _cameraFilePanel = transform.Find("MmdCamera").gameObject;
            _arUserDefinedPanel = transform.Find("ArUserDefined").gameObject;
            
            // 隐藏AR相关面板
            if (_arUserDefinedPanel != null)
            {
                _arUserDefinedPanel.SetActive(false);
            }
            
            // 设置XR模式切换监听器
            MainSceneController.Instance.OnXrTypeChanged.AddListener(xrType =>
            {
                // 只在相机文件模式下显示相机文件面板
                if (_cameraFilePanel != null)
                {
                    _cameraFilePanel.SetActive(xrType == XrType.CameraFile);
                }
                
                // 如果尝试切换到AR模式，自动切换回VR手动模式
                if (xrType == XrType.ArUserDefined)
                {
                    MainSceneController.Instance.ChangeXrType(XrType.VrManual);
                }
            });

            // 保留必要的按钮功能
            SetupModelButtons();
            SetupMusicButtons();
            SetupControlButtons();
            SetupCameraFileButtons();
        }
        
        private void SetupModelButtons()
        {
            // 添加模型按钮
            SetButtonListener("Functions/BtnAddModel", () =>
            {
                ShowPage<MmdFileSelector>(new MmdFileSelector.Context
                {
                    Type = MmdFileSelector.FileType.Model,
                    Title = "添加模型",
                    OnFileSelect = filePath =>
                    {
                        TtuiUtils.RunWithLoadingUI<LoadingDialog>(MainSceneController.Instance, () =>
                        {
                            try
                            {
                                if (!MainSceneController.Instance.AddModel(filePath))
                                {
                                    ShowAddModelFailTip();
                                    return;
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                                ShowAddModelFailTip();
                                return;
                            }
                            ShowPage<MmdModelPanel>();
                        });
                    }
                });
            });
            
            // 选择模型按钮
            SetButtonListener("Functions/BtnSelectModel", () =>
            {
                if (MainSceneController.Instance.GetModelCount() > 0)
                {
                    ShowPage<MmdModelSelectPanel>();
                }
            });
        }
        
        private void SetupMusicButtons()
        {
            // 选择音乐按钮
            SetButtonListener("Functions/BtnSelectMusic", () =>
            {
                ShowPage<MmdFileSelector>(new MmdFileSelector.Context
                {
                    Type = MmdFileSelector.FileType.Music,
                    Title = "选择音乐",
                    OnFileSelect = filePath =>
                    {
                        TtuiUtils.RunWithLoadingUI<LoadingDialog>(MainSceneController.Instance,
                            () =>
                            {
                                try
                                {
                                    MainSceneController.Instance.ChangeMusic(filePath);
                                }
                                catch (Exception e)
                                {
                                    Debug.LogException(e);
                                }
                            });
                    }
                });
            });
        }
        
        private void SetupControlButtons()
        {
            // 播放按钮
            SetButtonListener("Functions/BtnPlay", () =>
            {
                if (MainSceneController.Instance.GetXrType().Equals(XrType.VrGlass))
                {
                    ShowPage<VrCountdownPage>();
                }
                else
                {
                    MainSceneController.Instance.SwitchPlayPause(true);
                }
            });
            
            // XR模式选择按钮
            SetButtonListener("Functions/BtnXR", ShowPage<XrSelecror>);
            
            // 底部按钮
            SetButtonListener("Bottom/BtnQuit", ShowPage<ConfirmReturnTitlePage>);
            SetButtonListener("Bottom/BtnSave", ShowPage<SaveSceneDialog>);
            SetButtonListener("Bottom/BtnLoad", ShowPage<LoadSceneDilog>);
        }
        
        private void SetupCameraFileButtons()
        {
            // 相机数据选择按钮
            SetButtonListener("MmdCamera/BtnSelectCamera", () =>
            {
                ShowPage<MmdFileSelector>(new MmdFileSelector.Context
                {
                    Type = MmdFileSelector.FileType.Motion,
                    Title = "相机数据",
                    OnFileSelect = filePath =>
                    {
                        TtuiUtils.RunWithLoadingUI<LoadingDialog>(MainSceneController.Instance, () =>
                        {
                            try
                            {
                                var xrController = MainSceneController.Instance.GetXrController();
                                var cameraFileController = xrController as CameraFileController;
                                if (cameraFileController == null)
                                {
                                    return;
                                }
                                MainSceneController.Instance.SwitchPlayPause(false);
                                MainSceneController.Instance.ResetAll();
                                MainSceneController.Instance.CameraFilePath = filePath;
                                if (!cameraFileController.CameraObject.LoadCameraMotion(filePath))
                                {
                                    TtuiUtils.ShowPageAfterLoadingUI<OkDialog>(MainSceneController.Instance,
                                        new OkDialog.Context
                                        {
                                            Tip = "载入的文件中不含镜头数据。",
                                            Title = "提示"
                                        });
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                                TtuiUtils.ShowPageAfterLoadingUI<OkDialog>(MainSceneController.Instance,
                                    new OkDialog.Context
                                    {
                                        Tip = "载入镜头数据失败。",
                                        Title = "提示"
                                    });
                            }
                        });
                    }
                });
            });
        }

        private static void ShowAddModelFailTip()
        {
            TtuiUtils.ShowPageAfterLoadingUI<OkDialog>(MainSceneController.Instance,
                new OkDialog.Context
                {
                    Title = "提示",
                    Tip = "载入模型失败。请确认模型存在且为正确的MikuMikuDance模型。"
                });
        }

        public override void Active()
        {
            base.Active();
            var mainSceneController = MainSceneController.Instance;
            if (mainSceneController == null)
            {
                return;
            }
            mainSceneController.ShowSelectedMark(false);
        }
    }
}