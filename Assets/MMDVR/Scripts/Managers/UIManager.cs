using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;
using MMDVR.Scripts.Managers;
using MMDVR.Scripts.Controls;
using MMDVR.Events;

namespace MMDVR.Scripts.Managers
{
    /// <summary>
    /// UI管理器 - 负责VR/桌面模式的UI适配、Canvas模式切换、InputModule管理等
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("VR相关设置")]
        [Tooltip("VR摄像机，不设置则尝试使用Camera.main")]
        public Camera vrCamera;
        
        [Tooltip("UI距离头盔的偏移（米）")]
        public Vector3 vrUIOffset = new Vector3(0, 0.0f, 3.0f);

        [Header("UI面板统一管理")]
        [Tooltip("主UI面板")]
        public GameObject mainUIPanel;
        [Tooltip("桌面覆盖UI面板")]
        public GameObject desktopOverlayUI;        [Header("按钮状态更新")]
        [Tooltip("MainUI切换按钮")]
        public UnityEngine.UI.Button mainUIToggleButton;
        [Tooltip("VR模式切换按钮")]
        public UnityEngine.UI.Button vrModeToggleButton;

        // UI显示来源
        public enum UIShowSource
        {
            Desktop,
            VR
        }

        // 私有字段
        private Canvas mainCanvas;
        private bool isCurrentlyInVRMode = false;
        private bool lastVRActiveState = false;

        // InputModule管理
        private EventSystem eventSystem;
        private XRUIInputModule xrInputModule;
        private StandaloneInputModule standaloneInputModule;

        // 兼容性属性
        public GameObject uiPanel => mainUIPanel;

        void Awake()
        {
            // 单例模式
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }        void Start()
        {
            InitializeUI();
            InitializeInputModules();
            
            // 自动查找UI组件
            AutoFindUIComponents();
            
            // 监听系统状态变化
            SystemEvents.OnVRModeDetected += OnSystemVRModeChanged;
            
            // 监听UI切换事件
            InputEvents.OnUIToggleRequested += OnUIToggleRequested;
            
            // 根据当前系统状态设置UI模式
            bool vrActive = IsSystemInVRMode();
            SetUIMode(vrActive);
            lastVRActiveState = vrActive;
        }

        void Update()
        {
            // 检查VR状态变化（备用机制，主要依赖事件）
            bool currentVRActive = IsSystemInVRMode();
            if (currentVRActive != lastVRActiveState)
            {
                SetUIMode(currentVRActive);
                lastVRActiveState = currentVRActive;
                Debug.Log($"UIManager: VR状态变化检测到 {(currentVRActive ? "VR模式" : "桌面模式")}");
            }
        }

        void OnDestroy()
        {
            // 清理事件订阅
            SystemEvents.OnVRModeDetected -= OnSystemVRModeChanged;
            InputEvents.OnUIToggleRequested -= OnUIToggleRequested;
        }

        void OnEnable()
        {
            if (VRControllerInputManager.Instance != null)
                VRControllerInputManager.OnMenuButtonPressed += OnVRMenuButtonPressed;
        }

        void OnDisable()
        {
            if (VRControllerInputManager.Instance != null)
                VRControllerInputManager.OnMenuButtonPressed -= OnVRMenuButtonPressed;
        }

        /// <summary>
        /// 初始化UI组件
        /// </summary>
        private void InitializeUI()
        {
            if (mainUIPanel == null)
            {
                Debug.LogError("UIManager: MainUIPanel未设置！请在Inspector中指定主UI面板。");
                return;
            }

            mainCanvas = mainUIPanel.GetComponent<Canvas>();
            if (mainCanvas == null)
            {
                Debug.LogError("UIManager: MainUIPanel必须包含Canvas组件！");
                return;
            }

            // 初始时隐藏UI
            mainUIPanel.SetActive(false);

            // 确保VR摄像机设置
            if (vrCamera == null)
            {
                vrCamera = Camera.main;
                if (vrCamera == null)
                {
                    Debug.LogWarning("UIManager: 未找到VR摄像机，UI定位可能不正确。请在Inspector中指定VR摄像机。");
                }
            }
        }

        /// <summary>
        /// 初始化输入模块管理
        /// </summary>
        private void InitializeInputModules()
        {
            eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogError("UIManager: 场景中未找到EventSystem，请添加EventSystem。");
                return;
            }

            // 获取现有的InputModule或创建
            xrInputModule = eventSystem.GetComponent<XRUIInputModule>();
            standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();

            if (xrInputModule == null && standaloneInputModule == null)
            {
                Debug.LogError("UIManager: EventSystem上未找到合适的InputModule，请添加XRUIInputModule或StandaloneInputModule。");
                return;
            }

            // 如果缺少某个InputModule，尝试创建
            if (xrInputModule == null)
            {
                xrInputModule = eventSystem.gameObject.AddComponent<XRUIInputModule>();
                Debug.Log("UIManager: 自动创建了XRUIInputModule用于VR模式");
            }

            if (standaloneInputModule == null)
            {
                standaloneInputModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
                Debug.Log("UIManager: 自动创建了StandaloneInputModule用于桌面模式");
            }
        }        /// <summary>
        /// 系统VR模式变化事件处理
        /// </summary>
        private void OnSystemVRModeChanged(bool isVRActive)
        {
            SetUIMode(isVRActive);
            
            // 更新VR模式切换按钮状态
            UpdateVRModeToggleButtonState(isVRActive);
            
            Debug.Log($"UIManager: 响应系统状态变化，切换到{(isVRActive ? "VR" : "桌面")}UI模式");
        }        /// <summary>
        /// 更新VR模式切换按钮状态
        /// </summary>
        /// <param name="isVRMode">当前是否为VR模式</param>
        private void UpdateVRModeToggleButtonState(bool isVRMode)
        {
            if (vrModeToggleButton != null)
            {
                var textComponent = vrModeToggleButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = isVRMode ? "Toggle Desktop(V)" : "Toggle VR(V)";
                    Debug.Log($"UIManager: 更新VR模式按钮文本: {textComponent.text}");
                }
            }
        }

        /// <summary>
        /// 更新MainUI切换按钮状态
        /// </summary>
        /// <param name="isUIVisible">MainUI是否可见</param>
        private void UpdateMainUIToggleButtonState(bool isUIVisible)
        {
            if (mainUIToggleButton != null)
            {
                var colors = mainUIToggleButton.colors;
                var targetGraphic = mainUIToggleButton.targetGraphic;
                if (targetGraphic != null)
                {
                    targetGraphic.color = isUIVisible ? colors.pressedColor : colors.normalColor;
                    Debug.Log($"UIManager: 更新MainUI切换按钮状态: {(isUIVisible ? "激活" : "正常")}");
                }
            }
        }

        /// <summary>
        /// UI切换请求事件处理
        /// </summary>
        private void OnUIToggleRequested(InputEvents.InputSource source)
        {
            UIShowSource uiSource = source == InputEvents.InputSource.VR ? UIShowSource.VR : UIShowSource.Desktop;
            ToggleUI(uiSource);
        }

        private void OnVRMenuButtonPressed()
        {
            // VR控制器菜单键切换UI
            ToggleUI(UIShowSource.VR);
        }

        /// <summary>
        /// 检查系统是否处于VR模式
        /// </summary>
        private bool IsSystemInVRMode()
        {
            if (SystemStateManager.Instance != null)
            {
                return SystemStateManager.Instance.currentCameraMode == CameraMode.VR;
            }
            
            // 备用检测
            return SystemEvents.IsVRActive();
        }

        /// <summary>
        /// 设置UI模式（VR或桌面）
        /// </summary>
        public void SetUIMode(bool vrMode)
        {
            if (mainCanvas == null) return;

            if (vrMode)
            {
                SetupForVRMode();
            }
            else
            {
                SetupForDesktopMode();
            }

            UpdateInputModules(vrMode);
        }

        /// <summary>
        /// 配置VR模式UI
        /// </summary>
        private void SetupForVRMode()
        {
            if (mainCanvas == null) return;
            
            mainCanvas.renderMode = RenderMode.WorldSpace;
            mainUIPanel.transform.localScale = Vector3.one * 0.001f; // VR适配缩放
            isCurrentlyInVRMode = true;
            
            // 通知UI模式改变
            UIEvents.NotifyUIModeChanged(true);
            
            Debug.Log("UIManager: 切换到VR UI模式");
        }

        /// <summary>
        /// 配置桌面模式UI
        /// </summary>
        private void SetupForDesktopMode()
        {
            if (mainCanvas == null) return;
            
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainUIPanel.transform.localScale = Vector3.one; // 默认缩放
            isCurrentlyInVRMode = false;
            
            // 通知UI模式改变
            UIEvents.NotifyUIModeChanged(false);
            
            Debug.Log("UIManager: 切换到桌面UI模式");
        }

        /// <summary>
        /// 更新输入模块
        /// </summary>
        private void UpdateInputModules(bool vrMode)
        {
            if (eventSystem == null || xrInputModule == null || standaloneInputModule == null)
                return;

            if (vrMode)
            {
                // VR模式：启用XRUIInputModule，禁用StandaloneInputModule
                xrInputModule.enabled = true;
                standaloneInputModule.enabled = false;
            }
            else
            {
                // 桌面模式：启用StandaloneInputModule，禁用XRUIInputModule
                xrInputModule.enabled = false;
                standaloneInputModule.enabled = true;
            }
        }

        /// <summary>
        /// 切换UI显示状态
        /// </summary>
        public void ToggleUI(UIShowSource source)
        {
            if (mainUIPanel == null || mainCanvas == null) return;

            bool shouldShow = !mainUIPanel.activeSelf;
            
            if (shouldShow)
            {
                ShowUI(source);
            }
            else
            {
                HideUI();
            }
        }        /// <summary>
        /// 切换MainUI显示状态 - 供Button直接调用，使用平滑动画
        /// </summary>
        public void ToggleMainUIVisibility()
        {
            Debug.Log("🎨 平滑切换UI显示状态");
            
            if (mainUIPanel != null)
            {
                bool isCurrentlyVisible = mainUIPanel.activeInHierarchy;
                
                if (isCurrentlyVisible)
                {
                    HidePanelSmooth(mainUIPanel, 0.25f);
                    Debug.Log("🔄 平滑隐藏MainUI");
                }
                else
                {
                    ShowPanelSmooth(mainUIPanel, 0.25f);
                    Debug.Log("🔄 平滑显示MainUI");
                }
            }
            else
            {
                // 后备方案：使用原来的直接切换
                ToggleUI(UIShowSource.Desktop);
            }
        }

        /// <summary>
        /// 切换VR模式 - 供Button直接调用
        /// </summary>
        public void ToggleVRMode()
        {
            SystemStateManager.Instance?.ToggleVRMode();
        }

        /// <summary>
        /// 显示UI
        /// </summary>
        public void ShowUI(UIShowSource source)
        {
            if (mainUIPanel == null) return;

            // 根据显示来源设置UI模式
            bool useVRMode = (source == UIShowSource.VR) || IsSystemInVRMode();
            SetUIMode(useVRMode);

            // 如果是VR模式，定位UI到头盔前方
            if (useVRMode && vrCamera != null)
            {
                PositionUIInVR();
            }            mainUIPanel.SetActive(true);
            
            // 更新MainUI切换按钮状态
            UpdateMainUIToggleButtonState(true);
            Debug.Log($"UIManager: 显示UI ({source}模式)");
            // 新增：UI显示后，刷新Musics、Cameras高亮，重建连线
            MMDVR.Scripts.UIInteraction.ResourceManagement.ListController.MusicListController.Instance?.UpdateAllItemVisuals();
            MMDVR.Scripts.UIInteraction.ResourceManagement.ListController.CameraListController.Instance?.UpdateAllItemVisuals();
            MMDVR.Scripts.UIInteraction.ResourceManagement.ConnectionManagement.ConnectionManager.Instance?.RebuildAllConnections();
        }

        /// <summary>
        /// 隐藏UI
        /// </summary>
        public void HideUI()
        {
            if (mainUIPanel == null) return;            mainUIPanel.SetActive(false);
            
            // 更新MainUI切换按钮状态
            UpdateMainUIToggleButtonState(false);

            Debug.Log("UIManager: 隐藏UI");
        }

        /// <summary>
        /// 在VR模式下定位UI到头盔前方
        /// </summary>
        private void PositionUIInVR()
        {
            if (vrCamera == null || mainUIPanel == null) return;

            Vector3 headPosition = vrCamera.transform.position;
            Vector3 headForward = vrCamera.transform.forward;
            
            // 计算UI位置：头盔前方指定偏移距离
            Vector3 uiPosition = headPosition + headForward * vrUIOffset.z + 
                                vrCamera.transform.up * vrUIOffset.y + 
                                vrCamera.transform.right * vrUIOffset.x;
            
            mainUIPanel.transform.position = uiPosition;
            
            // UI面向用户
            mainUIPanel.transform.LookAt(headPosition);
            mainUIPanel.transform.Rotate(0, 180, 0); // 翻转，让UI正面朝向用户
        }

        /// <summary>
        /// 获取当前UI是否显示
        /// </summary>
        public bool IsUIVisible()
        {
            return mainUIPanel != null && mainUIPanel.activeSelf;
        }

        /// <summary>
        /// 获取当前是否为VR UI模式
        /// </summary>
        public bool IsVRUIMode()
        {
            return isCurrentlyInVRMode;
        }

        /// <summary>
        /// 强制刷新UI状态（用于调试或特殊情况）
        /// </summary>
        public void RefreshUIState()
        {
            bool vrActive = IsSystemInVRMode();
            SetUIMode(vrActive);
            lastVRActiveState = vrActive;
            Debug.Log("UIManager: 强制刷新UI状态");
        }        /// <summary>
        /// 自动查找UI组件
        /// </summary>
        private void AutoFindUIComponents()
        {
            // 查找DesktopOverlayUI
            if (desktopOverlayUI == null)
            {
                desktopOverlayUI = GameObject.Find("DesktopOverlayUI");
                if (desktopOverlayUI != null)
                {
                    Debug.Log("UIManager: 自动找到了DesktopOverlayUI");
                }
            }
            
            if (desktopOverlayUI != null)
            {
                // 自动查找MainUI切换按钮
                if (mainUIToggleButton == null)
                {
                    var buttons = desktopOverlayUI.GetComponentsInChildren<UnityEngine.UI.Button>();
                    foreach (var button in buttons)
                    {
                        if (button.name.Contains("UI") && (button.name.Contains("Toggle") || button.name.Contains("Show")))
                        {
                            mainUIToggleButton = button;
                            Debug.Log($"UIManager: 自动找到了MainUI切换按钮: {button.name}");
                            break;
                        }
                    }
                }
                
                // 自动查找VR模式切换按钮
                if (vrModeToggleButton == null)
                {
                    var buttons = desktopOverlayUI.GetComponentsInChildren<UnityEngine.UI.Button>();
                    foreach (var button in buttons)
                    {
                        if (button.name.Contains("VR") || button.name.Contains("Mode"))
                        {
                            vrModeToggleButton = button;
                            Debug.Log($"UIManager: 自动找到了VR模式切换按钮: {button.name}");
                            break;
                        }
                    }
                }
            }
        }
        
        // ==================== UI动画协程功能 ====================
        
        /// <summary>
        /// 平滑显示UI面板
        /// </summary>
        public void ShowPanelSmooth(GameObject panel, float duration = 0.3f)
        {
            if (panel != null)
                ResourceManager.Instance.StartGlobalCoroutine(ShowPanelCoroutine(panel, duration));
        }
        
        /// <summary>
        /// 平滑隐藏UI面板
        /// </summary>
        public void HidePanelSmooth(GameObject panel, float duration = 0.3f)
        {
            if (panel != null)
                ResourceManager.Instance.StartGlobalCoroutine(HidePanelCoroutine(panel, duration));
        }
        
        private System.Collections.IEnumerator ShowPanelCoroutine(GameObject panel, float duration)
        {
            if (panel == null) yield break;
            
            var canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = panel.AddComponent<CanvasGroup>();
            
            panel.SetActive(true);
            
            float startAlpha = canvasGroup.alpha;
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, progress);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }
        
        private System.Collections.IEnumerator HidePanelCoroutine(GameObject panel, float duration)
        {
            if (panel == null) yield break;
            
            var canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = panel.AddComponent<CanvasGroup>();
            
            float startAlpha = canvasGroup.alpha;
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
            panel.SetActive(false);
        }
        
        /// <summary>
        /// 平滑切换UI面板
        /// </summary>
        public void SwitchPanelSmooth(GameObject hidePanel, GameObject showPanel, float duration = 0.3f)
        {
            ResourceManager.Instance.StartGlobalCoroutine(SwitchPanelCoroutine(hidePanel, showPanel, duration));
        }
        
        private System.Collections.IEnumerator SwitchPanelCoroutine(GameObject hidePanel, GameObject showPanel, float duration)
        {
            // 同时开始隐藏和显示动画
            Coroutine hideCoroutine = null;
            Coroutine showCoroutine = null;
            
            if (hidePanel != null)
                hideCoroutine = ResourceManager.Instance.StartGlobalCoroutine(HidePanelCoroutine(hidePanel, duration * 0.6f));
            
            // 稍微延迟显示动画，创造更好的视觉效果
            yield return new WaitForSeconds(duration * 0.2f);
            
            if (showPanel != null)
                showCoroutine = ResourceManager.Instance.StartGlobalCoroutine(ShowPanelCoroutine(showPanel, duration * 0.8f));
            
            // 等待所有动画完成
            yield return new WaitForSeconds(duration);
        }

        // ==================== 原有的UI管理功能 ====================
    }
}
