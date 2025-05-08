using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MikuMikuXR.SceneController;

namespace MikuMikuXR.UI.Desktop
{
    /// <summary>
    /// UGUI界面控制器，管理整体UI界面和播放控制
    /// </summary>
    public class UGUIController : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private UGUIFileManager fileManager;
        
        [Header("Playback Controls")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Slider timelineSlider;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Text currentTimeText;
        [SerializeField] private Text totalTimeText;
        
        [Header("Panel Controls")]
        [SerializeField] private Button showModelListButton;
        [SerializeField] private GameObject leftPanel;
        [SerializeField] private Button closeLeftPanelButton;
        [SerializeField] private GameObject rightPanel;
        [SerializeField] private Button closeRightPanelButton;
        
        // 主场景控制器引用
        private MainSceneController _mainSceneController;
        
        // 面板状态
        private bool _leftPanelVisible = false;
        private bool _rightPanelVisible = false;
        
        // 上次更新时间
        private float _lastTimelineUpdate = 0f;
        
        private void Awake()
        {
            // 获取主场景控制器引用
            _mainSceneController = FindObjectOfType<MainSceneController>();
            if (_mainSceneController == null)
            {
                Debug.LogError("找不到MainSceneController！UI功能将不可用。");
            }
        }
        
        private void Start()
        {
            // 初始化UI状态
            InitializeUI();
            
            // 注册事件监听
            RegisterEventHandlers();
            
            // 注册MainSceneController事件监听
            RegisterMainSceneControllerEvents();
        }
        
        private void OnDestroy()
        {
            // 清除MainSceneController事件监听
            UnregisterMainSceneControllerEvents();
        }
        
        private void Update()
        {
            // 每帧更新时间轴，但限制更新频率以提高性能
            if (_mainSceneController == null) return;
            
            if (Time.time - _lastTimelineUpdate > 0.1f)
            {
                UpdateTimeDisplay();
                _lastTimelineUpdate = Time.time;
            }
        }
        
        /// <summary>
        /// 初始化UI状态
        /// </summary>
        private void InitializeUI()
        {
            // 初始化播放按钮状态
            if (playButton != null && pauseButton != null)
            {
                playButton.gameObject.SetActive(true);
                pauseButton.gameObject.SetActive(false);
            }
            
            // 初始化面板状态
            if (leftPanel != null) leftPanel.SetActive(false);
            if (rightPanel != null) rightPanel.SetActive(false);
            
            // 初始化音量滑块
            if (volumeSlider != null && _mainSceneController != null)
            {
                AudioSource audioSource = _mainSceneController.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    volumeSlider.value = audioSource.volume;
                }
            }
        }
        
        /// <summary>
        /// 注册UI事件监听
        /// </summary>
        private void RegisterEventHandlers()
        {
            // 播放控制按钮
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);
            
            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPauseClicked);
            
            if (stopButton != null)
                stopButton.onClick.AddListener(OnStopClicked);
            
            // 时间轴滑块
            if (timelineSlider != null)
                timelineSlider.onValueChanged.AddListener(OnTimelineValueChanged);
            
            // 音量滑块
            if (volumeSlider != null)
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            
            // 面板控制按钮
            if (showModelListButton != null)
                showModelListButton.onClick.AddListener(() => TogglePanel(leftPanel, ref _leftPanelVisible));
            
            if (closeLeftPanelButton != null)
                closeLeftPanelButton.onClick.AddListener(() => TogglePanel(leftPanel, ref _leftPanelVisible));
            
            if (closeRightPanelButton != null)
                closeRightPanelButton.onClick.AddListener(() => TogglePanel(rightPanel, ref _rightPanelVisible));
        }
        
        /// <summary>
        /// 注册MainSceneController事件
        /// </summary>
        private void RegisterMainSceneControllerEvents()
        {
            if (_mainSceneController == null) return;
            
            _mainSceneController.OnPlayPause.AddListener(HandlePlayPauseEvent);
            _mainSceneController.OnResetAll.AddListener(HandleResetAllEvent);
        }
        
        /// <summary>
        /// 取消注册MainSceneController事件
        /// </summary>
        private void UnregisterMainSceneControllerEvents()
        {
            if (_mainSceneController == null) return;
            
            _mainSceneController.OnPlayPause.RemoveListener(HandlePlayPauseEvent);
            _mainSceneController.OnResetAll.RemoveListener(HandleResetAllEvent);
        }
        
        /// <summary>
        /// 处理播放/暂停事件
        /// </summary>
        private void HandlePlayPauseEvent(bool isPlaying)
        {
            if (playButton != null && pauseButton != null)
            {
                playButton.gameObject.SetActive(!isPlaying);
                pauseButton.gameObject.SetActive(isPlaying);
            }
        }
        
        /// <summary>
        /// 处理重置事件
        /// </summary>
        private void HandleResetAllEvent()
        {
            if (playButton != null && pauseButton != null)
            {
                playButton.gameObject.SetActive(true);
                pauseButton.gameObject.SetActive(false);
            }
            
            if (timelineSlider != null)
            {
                timelineSlider.value = 0f;
            }
            
            UpdateTimeDisplay();
        }
        
        /// <summary>
        /// 播放按钮点击事件
        /// </summary>
        private void OnPlayClicked()
        {
            if (_mainSceneController == null) return;
            
            _mainSceneController.SwitchPlayPause(true);
        }
        
        /// <summary>
        /// 暂停按钮点击事件
        /// </summary>
        private void OnPauseClicked()
        {
            if (_mainSceneController == null) return;
            
            _mainSceneController.SwitchPlayPause(false);
        }
        
        /// <summary>
        /// 停止按钮点击事件
        /// </summary>
        private void OnStopClicked()
        {
            if (_mainSceneController == null) return;
            
            _mainSceneController.SwitchPlayPause(false);
            _mainSceneController.ResetAll();
        }
        
        /// <summary>
        /// 时间轴值变化事件
        /// </summary>
        private void OnTimelineValueChanged(float value)
        {
            if (_mainSceneController == null) return;
            
            // 使用AudioSource直接设置时间
            AudioSource audioSource = _mainSceneController.GetComponent<AudioSource>();
            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.time = value;
                
                // 更新时间显示
                UpdateTimeLabel(value, currentTimeText);
            }
        }
        
        /// <summary>
        /// 音量值变化事件
        /// </summary>
        private void OnVolumeChanged(float value)
        {
            if (_mainSceneController == null) return;
            
            // 设置音量
            AudioSource audioSource = _mainSceneController.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.volume = value;
            }
        }
        
        /// <summary>
        /// 切换面板显示状态
        /// </summary>
        private void TogglePanel(GameObject panel, ref bool state)
        {
            if (panel == null) return;
            
            state = !state;
            panel.SetActive(state);
        }
        
        /// <summary>
        /// 更新时间显示
        /// </summary>
        private void UpdateTimeDisplay()
        {
            if (_mainSceneController == null) return;
            
            AudioSource audioSource = _mainSceneController.GetComponent<AudioSource>();
            if (audioSource == null || audioSource.clip == null) return;
            
            float currentTime = audioSource.time;
            float totalTime = audioSource.clip.length;
            
            // 更新时间轴滑块
            if (timelineSlider != null)
            {
                // 设置最大值
                timelineSlider.maxValue = totalTime;
                
                // 仅当用户未拖动滑块时更新
                if (!timelineSlider.gameObject.GetComponent<UnityEngine.EventSystems.EventSystem>().IsPointerOverGameObject())
                {
                    timelineSlider.value = currentTime;
                }
            }
            
            // 更新时间文本
            UpdateTimeLabel(currentTime, currentTimeText);
            UpdateTimeLabel(totalTime, totalTimeText);
        }
        
        /// <summary>
        /// 更新时间标签显示
        /// </summary>
        private void UpdateTimeLabel(float timeInSeconds, Text label)
        {
            if (label == null) return;
            
            int minutes = (int)(timeInSeconds / 60);
            int seconds = (int)(timeInSeconds % 60);
            label.text = $"{minutes:00}:{seconds:00}";
        }
        
        /// <summary>
        /// 检查是否有可用模型
        /// </summary>
        private bool HasModels()
        {
            return _mainSceneController != null && _mainSceneController.GetModelCount() > 0;
        }
    }
}