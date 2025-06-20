using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using MikuMikuXR.SceneController;
using MikuMikuXR.UI.Page;
using TinyTeam.UI;
using LibMMD.Unity3D;

namespace MikuMikuXR.UI.Desktop
{
    /// <summary>
    /// 简单的桌面UI控制器，连接UI元素到现有的MainSceneController
    /// </summary>
    public class SimpleUIController : MonoBehaviour
    {
        [SerializeField] private UIDocument mainUIDocument;
        
        // UI元素引用
        private Button _addModelButton;
        private Button _selectMotionButton;
        private Button _selectMusicButton;
        private Button _settingsButton;
        
        private Button _playButton;
        private Button _pauseButton;
        private Button _stopButton;
        
        private Slider _timelineSlider;
        private Label _currentTimeLabel;
        private Label _totalTimeLabel;
        private Slider _volumeSlider;
        
        private Button _showModelListButton;
        private VisualElement _leftPanel;
        private Button _closeLeftPanelButton;
        
        private VisualElement _rightPanel;
        private Button _closeRightPanelButton;
        
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
        
        private void OnEnable()
        {
            if (mainUIDocument == null || mainUIDocument.rootVisualElement == null)
            {
                Debug.LogError("UI文档未找到或未加载！");
                return;
            }
            
            // 获取UI根元素
            VisualElement root = mainUIDocument.rootVisualElement;
            
            // 初始化UI引用
            InitializeUIReferences(root);
            
            // 注册事件
            RegisterEventHandlers();
        }
        
        private void InitializeUIReferences(VisualElement root)
        {
            // 顶部工具栏按钮
            _addModelButton = root.Q<Button>("AddModelButton");
            _selectMotionButton = root.Q<Button>("SelectMotionButton");
            _selectMusicButton = root.Q<Button>("SelectMusicButton");
            _settingsButton = root.Q<Button>("SettingsButton");
            
            // 播放控制按钮
            _playButton = root.Q<Button>("PlayButton");
            _pauseButton = root.Q<Button>("PauseButton");
            _stopButton = root.Q<Button>("StopButton");
            
            // 时间轴和音量控制
            _timelineSlider = root.Q<Slider>("TimelineSlider");
            _currentTimeLabel = root.Q<Label>("CurrentTimeLabel");
            _totalTimeLabel = root.Q<Label>("TotalTimeLabel");
            _volumeSlider = root.Q<Slider>("VolumeSlider");
            
            // 面板相关
            _showModelListButton = root.Q<Button>("ShowModelListButton");
            _leftPanel = root.Q<VisualElement>("LeftPanel");
            _closeLeftPanelButton = root.Q<Button>("CloseLeftPanelButton");
            _rightPanel = root.Q<VisualElement>("RightPanel");
            _closeRightPanelButton = root.Q<Button>("CloseRightPanelButton");
            
            // 初始化面板状态
            if (_leftPanel != null) _leftPanel.RemoveFromClassList("visible");
            if (_rightPanel != null) _rightPanel.RemoveFromClassList("visible");
        }
        
        private void RegisterEventHandlers()
        {
            // 顶部工具栏按钮事件
            if (_addModelButton != null)
            {
                _addModelButton.clicked += OnAddModelClicked;
            }
            
            if (_selectMotionButton != null)
            {
                _selectMotionButton.clicked += OnSelectMotionClicked;
            }
            
            if (_selectMusicButton != null)
            {
                _selectMusicButton.clicked += OnSelectMusicClicked;
            }
            
            if (_settingsButton != null)
            {
                _settingsButton.clicked += () => TogglePanel(_rightPanel, ref _rightPanelVisible);
            }
            
            // 播放控制按钮事件
            if (_playButton != null)
            {
                _playButton.clicked += OnPlayClicked;
            }
            
            if (_pauseButton != null)
            {
                _pauseButton.clicked += OnPauseClicked;
            }
            
            if (_stopButton != null)
            {
                _stopButton.clicked += OnStopClicked;
            }
            
            // 时间轴事件
            if (_timelineSlider != null)
            {
                _timelineSlider.RegisterValueChangedCallback(OnTimelineValueChanged);
            }
            
            // 音量事件
            if (_volumeSlider != null)
            {
                _volumeSlider.RegisterValueChangedCallback(OnVolumeChanged);
            }
            
            // 面板相关事件
            if (_showModelListButton != null)
            {
                _showModelListButton.clicked += () => TogglePanel(_leftPanel, ref _leftPanelVisible);
            }
            
            if (_closeLeftPanelButton != null)
            {
                _closeLeftPanelButton.clicked += () => TogglePanel(_leftPanel, ref _leftPanelVisible);
            }
            
            if (_closeRightPanelButton != null)
            {
                _closeRightPanelButton.clicked += () => TogglePanel(_rightPanel, ref _rightPanelVisible);
            }
        }
        
        private void Update()
        {
            if (_mainSceneController == null) return;
            
            // 每帧更新时间轴，但限制更新频率以提高性能
            if (Time.time - _lastTimelineUpdate > 0.1f)
            {
                UpdateTimeDisplay();
                _lastTimelineUpdate = Time.time;
            }
        }
        
        private void OnAddModelClicked()
        {
            if (_mainSceneController == null) return;
            
            // 使用TTUIPage静态类显示文件选择器
            TTUIPage.ShowPage<MmdFileSelector>(new MmdFileSelector.Context
            {
                Type = MmdFileSelector.FileType.Model,
                Title = "添加模型",
                PathMode = true,
                OnFileSelect = filePath =>
                {
                    try
                    {
                        if (_mainSceneController.AddModel(filePath))
                        {
                            ShowMessage($"成功加载模型: {Path.GetFileName(filePath)}");
                            UpdateModelList();
                        }
                        else
                        {
                            ShowMessage("加载模型失败");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        ShowMessage("加载模型失败: " + e.Message);
                    }
                }
            });
        }
        
        private void OnSelectMotionClicked()
        {
            if (_mainSceneController == null || _mainSceneController.GetModelCount() == 0)
            {
                ShowMessage("请先添加模型");
                return;
            }
            
            // 如果有多个模型，弹出模型选择界面
            if (_mainSceneController.GetModelCount() > 1)
            {
                TTUIPage.ShowPage<MmdModelSelectPanel>();
            }
            else
            {
                // 使用TTUIPage静态类显示文件选择器
                TTUIPage.ShowPage<MmdFileSelector>(new MmdFileSelector.Context
                {
                    Type = MmdFileSelector.FileType.Motion,
                    Title = "选择动作",
                    PathMode = true,
                    OnFileSelect = filePath =>
                    {
                        try
                        {
                            _mainSceneController.ChangeCurrentMotion(filePath);
                            ShowMessage($"成功加载动作: {Path.GetFileName(filePath)}");
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            ShowMessage("加载动作失败: " + e.Message);
                        }
                    }
                });
            }
        }
        
        private void OnSelectMusicClicked()
        {
            if (_mainSceneController == null) return;
            
            // 使用TTUIPage静态类显示文件选择器
            TTUIPage.ShowPage<MmdFileSelector>(new MmdFileSelector.Context
            {
                Type = MmdFileSelector.FileType.Music,
                Title = "选择音乐",
                PathMode = true,
                OnFileSelect = filePath =>
                {
                    try
                    {
                        _mainSceneController.ChangeMusic(filePath);
                        ShowMessage($"成功加载音乐: {Path.GetFileName(filePath)}");
                        UpdateTotalTime();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        ShowMessage("加载音乐失败: " + e.Message);
                    }
                }
            });
        }
        
        private void OnPlayClicked()
        {
            if (_mainSceneController == null) return;
            
            _mainSceneController.SwitchPlayPause(true);
            
            // 切换播放/暂停按钮显示状态 - 使用CSS类控制而非直接设置style
            if (_playButton != null) _playButton.AddToClassList("hidden");
            if (_pauseButton != null) _pauseButton.RemoveFromClassList("hidden");
        }
        
        private void OnPauseClicked()
        {
            if (_mainSceneController == null) return;
            
            _mainSceneController.SwitchPlayPause(false);
            
            // 切换播放/暂停按钮显示状态 - 使用CSS类控制而非直接设置style
            if (_playButton != null) _playButton.RemoveFromClassList("hidden");
            if (_pauseButton != null) _pauseButton.AddToClassList("hidden");
        }
        
        private void OnStopClicked()
        {
            if (_mainSceneController == null) return;
            
            _mainSceneController.SwitchPlayPause(false);
            _mainSceneController.ResetAll();
            
            // 重置UI状态 - 使用CSS类控制而非直接设置style
            if (_playButton != null) _playButton.RemoveFromClassList("hidden");
            if (_pauseButton != null) _pauseButton.AddToClassList("hidden");
            if (_timelineSlider != null) _timelineSlider.value = 0;
            UpdateTimeDisplay();
        }
        
        private void OnTimelineValueChanged(ChangeEvent<float> evt)
        {
            if (_mainSceneController == null) return;
            
            // 直接使用AudioSource组件
            var audioSource = _mainSceneController.gameObject.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.time = evt.newValue;
            }
            
            // 更新显示时间
            UpdateTimeLabel(evt.newValue, _currentTimeLabel);
        }
        
        private void OnVolumeChanged(ChangeEvent<float> evt)
        {
            if (_mainSceneController == null) return;
            
            // 直接使用AudioSource组件
            var audioSource = _mainSceneController.gameObject.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.volume = evt.newValue;
            }
        }
        
        private void TogglePanel(VisualElement panel, ref bool state)
        {
            if (panel == null) return;
            
            state = !state;
            
            if (state)
            {
                panel.AddToClassList("visible");
            }
            else
            {
                panel.RemoveFromClassList("visible");
            }
        }
        
        private void UpdateTimeDisplay()
        {
            if (_mainSceneController == null) return;
            
            // 直接使用AudioSource组件
            var audioSource = _mainSceneController.gameObject.GetComponent<AudioSource>();
            if (audioSource == null) return;
            
            float currentTime = audioSource.time;
            
            // 更新时间轴滑块，检查元素是否有焦点
            if (_timelineSlider != null && (_timelineSlider.focusController == null || _timelineSlider.focusController.focusedElement != _timelineSlider))
            {
                _timelineSlider.value = currentTime;
            }
            
            // 更新时间标签
            UpdateTimeLabel(currentTime, _currentTimeLabel);
        }
        
        private void UpdateTotalTime()
        {
            if (_mainSceneController == null) return;
            
            // 直接使用AudioSource组件
            var audioSource = _mainSceneController.gameObject.GetComponent<AudioSource>();
            if (audioSource == null || audioSource.clip == null) return;
            
            float totalTime = audioSource.clip.length;
            
            // 更新时间轴最大值
            if (_timelineSlider != null)
            {
                _timelineSlider.highValue = totalTime;
            }
            
            // 更新总时间标签
            UpdateTimeLabel(totalTime, _totalTimeLabel);
        }
        
        private void UpdateTimeLabel(float timeInSeconds, Label label)
        {
            if (label == null) return;
            
            int minutes = (int)(timeInSeconds / 60);
            int seconds = (int)(timeInSeconds % 60);
            label.text = $"{minutes:00}:{seconds:00}";
        }
        
        private void UpdateModelList()
        {
            if (_mainSceneController == null) return;
            
            ListView modelList = _leftPanel?.Q<ListView>("ModelList");
            if (modelList == null) return;
            
            // 模型列表数据
            List<string> modelNames = new List<string>();
            int count = _mainSceneController.GetModelCount();
            
            // 获取模型列表（使用GetModelNames替代GetMmd）
            IList<string> names = _mainSceneController.GetModelNames();
            for (int i = 0; i < names.Count; i++)
            {
                modelNames.Add($"模型 {i + 1}: {names[i]}");
            }
            
            // 更新列表视图
            modelList.makeItem = () => new Label();
            modelList.bindItem = (element, i) => (element as Label).text = modelNames[i];
            modelList.itemsSource = modelNames;
            
            // 注册选择事件
            modelList.onSelectionChange += (objects) => {
                foreach (var obj in objects)
                {
                    int index = modelNames.IndexOf(obj.ToString());
                    if (index >= 0)
                    {
                        _mainSceneController.SelectModel(index);
                        break;
                    }
                }
            };
        }
        
        private void ShowMessage(string message)
        {
            Debug.Log(message);
            
            // 在这里可以添加显示在UI上的消息，如果有需要
        }
    }
}