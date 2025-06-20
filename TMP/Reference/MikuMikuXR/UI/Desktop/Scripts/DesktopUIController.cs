using MikuMikuXR.Core;
using MikuMikuXR.Core.Interfaces;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MikuMikuXR.SceneController;
using MikuMikuXR.UI.Page;
using MikuMikuXR.Utils;
using TinyTeam.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace MikuMikuXR.UI.Desktop
{
    /// <summary>
    /// 桌面UI的主控制器，负责管理UI界面的显示和交互
    /// </summary>
    public class DesktopUIController : MonoBehaviour
    {
        [SerializeField] private UIDocument _mainDocument;
        [SerializeField] private UIDocument _overlayDocument;
        
        private VisualElement _root;
        private VisualElement _overlayRoot;
        
        // 播放控制相关UI元素
        private Button _playButton;
        private Button _pauseButton;
        private Button _stopButton;
        private Slider _timelineSlider;
        private Label _currentTimeLabel;
        private Label _totalTimeLabel;
        private Slider _volumeSlider;
        
        // 自定义下拉菜单
        private CustomDropdown _modelDropdown;
        private CustomDropdown _motionDropdown;
        private CustomDropdown _musicDropdown;
        
        // 按钮元素
        private Button _modelDropdownButton;
        private Button _motionDropdownButton;
        private Button _musicDropdownButton;
        
        // 下拉菜单容器
        private VisualElement _dropdownContainer;
        
        // 设置按钮
        private Button _settingsButton;
        private VisualElement _rightPanel;
        
        // 模型相关数据模型
        private MmdModel _activeModel = null;
        private List<MmdModel> _loadedModels = new List<MmdModel>();
        
        // 当前音乐信息
        private MusicInfo _currentMusic = null;
        
        // 下拉列表的特殊选项
        private const string ADD_MODEL_TEXT = "+ 添加模型...";
        private const string ADD_MOTION_TEXT = "+ 选择动作...";
        private const string ADD_MUSIC_TEXT = "+ 选择音乐...";

        // 默认空白选项，防止下拉列表为空
        private const string EMPTY_OPTION = " ";
        
        // --- 新增：动作播放进度变量 ---
        private float _motionTime = 0f; // 仅无音乐时使用
        private float _lastUpdateTime = 0f;

        private void Start()
        {
            // 确保UI Toolkit文档已分配
            if (_mainDocument == null)
            {
                Debug.LogError("主UI文档未分配到DesktopUIController");
                return;
            }
            
            // 获取UI根元素
            _root = _mainDocument.rootVisualElement;
            
            if (_overlayDocument != null)
            {
                _overlayRoot = _overlayDocument.rootVisualElement;
            }
            
            // 初始化UI元素引用
            InitializeUIReferences();
            
            // 延迟初始化下拉菜单，确保UI组件完全加载
            StartCoroutine(DelayedInitialization());
        }

        private IEnumerator DelayedInitialization()
        {
            // 等待一帧，确保UI完全加载
            yield return null;
            
            // 创建并初始化自定义下拉菜单
            InitializeCustomDropdowns();
            
            // 绑定UI事件
            BindUIEvents();
        }
        
        /// <summary>
        /// 初始化自定义下拉菜单
        /// </summary>
        private void InitializeCustomDropdowns()
        {
            // 创建下拉菜单容器（如果不存在）
            if (_dropdownContainer == null)
            {
                _dropdownContainer = new VisualElement();
                _dropdownContainer.name = "dropdown-container";
                _dropdownContainer.style.position = Position.Absolute;
                _dropdownContainer.style.width = new Length(100, LengthUnit.Percent);
                _dropdownContainer.style.height = new Length(100, LengthUnit.Percent);
                _dropdownContainer.pickingMode = PickingMode.Position;
                _root.Add(_dropdownContainer);
            }
            
            // 创建模型下拉菜单
            if (_modelDropdownButton != null)
            {
                _modelDropdown = new CustomDropdown(_modelDropdownButton, _dropdownContainer);
                _modelDropdown.OnValueChanged += modelName => {
                    // 检查是否选择了"添加模型"选项
                    if (modelName == ADD_MODEL_TEXT)
                    {
                        // 打开文件选择器添加新模型
                        OpenModelFileSelector();
                    }
                    else if (modelName != EMPTY_OPTION)
                    {
                        // 选择已有模型
                        SelectModel(modelName);
                    }
                };
                
                // 初始化选项
                List<string> modelChoices = new List<string>();
                modelChoices.Add(ADD_MODEL_TEXT);
                _modelDropdown.Choices = modelChoices;
                _modelDropdown.Value = ADD_MODEL_TEXT;
            }
            
            // 创建动作下拉菜单
            if (_motionDropdownButton != null)
            {
                _motionDropdown = new CustomDropdown(_motionDropdownButton, _dropdownContainer);
                _motionDropdown.OnValueChanged += motionName => {
                    // 检查是否选择了"添加动作"选项
                    if (motionName == ADD_MOTION_TEXT)
                    {
                        // 打开文件选择器添加新动作
                        OpenMotionFileSelector();
                    }
                    else if (motionName != EMPTY_OPTION)
                    {
                        // 选择已有动作
                        SelectMotion(motionName);
                    }
                };
                
                // 初始化选项
                List<string> motionChoices = new List<string>();
                motionChoices.Add(ADD_MOTION_TEXT);
                _motionDropdown.Choices = motionChoices;
                _motionDropdown.Value = ADD_MOTION_TEXT;
            }
            
            // 创建音乐下拉菜单
            if (_musicDropdownButton != null)
            {
                _musicDropdown = new CustomDropdown(_musicDropdownButton, _dropdownContainer);
                _musicDropdown.OnValueChanged += musicName => {
                    // 检查是否选择了"添加音乐"选项
                    if (musicName == ADD_MUSIC_TEXT)
                    {
                        // 打开文件选择器添加新音乐
                        OpenMusicFileSelector();
                    }
                    else if (musicName != EMPTY_OPTION)
                    {
                        // 选择已有音乐
                        SelectMusic(musicName);
                    }
                };
                
                // 初始化选项
                List<string> musicChoices = new List<string>();
                musicChoices.Add(ADD_MUSIC_TEXT);
                _musicDropdown.Choices = musicChoices;
                _musicDropdown.Value = ADD_MUSIC_TEXT;
            }
        }
        
        /// <summary>
        /// 初始化UI元素引用
        /// </summary>
        private void InitializeUIReferences()
        {
            // 下拉菜单按钮 - 使用普通按钮替代下拉字段
            _modelDropdownButton = _root.Q<Button>("ModelDropdownButton");
            _motionDropdownButton = _root.Q<Button>("MotionDropdownButton");
            _musicDropdownButton = _root.Q<Button>("MusicDropdownButton");
            
            // 如果找不到下拉按钮，尝试查找原始下拉列表并替换为按钮
            TryCreateDropdownButton("ModelDropdown", ref _modelDropdownButton);
            TryCreateDropdownButton("MotionDropdown", ref _motionDropdownButton);
            TryCreateDropdownButton("MusicDropdown", ref _musicDropdownButton);
            
            // 播放控制按钮
            _playButton = _root.Q<Button>("PlayButton");
            _pauseButton = _root.Q<Button>("PauseButton");
            _stopButton = _root.Q<Button>("StopButton");
            
            // 时间轴和标签
            _timelineSlider = _root.Q<Slider>("TimelineSlider");
            _currentTimeLabel = _root.Q<Label>("CurrentTimeLabel");
            _totalTimeLabel = _root.Q<Label>("TotalTimeLabel");
            
            // 音量控制
            _volumeSlider = _root.Q<Slider>("VolumeSlider");
            
            // 设置面板
            _settingsButton = _root.Q<Button>("SettingsButton");
            _rightPanel = _root.Q<VisualElement>("RightPanel");
            
            // 关闭面板按钮
            Button closeRightPanelButton = _root.Q<Button>("CloseRightPanelButton");
            if (closeRightPanelButton != null)
            {
                closeRightPanelButton.clicked += () => _rightPanel.RemoveFromClassList("visible");
            }
        }
        
        /// <summary>
        /// 尝试将原始DropdownField替换为按钮
        /// </summary>
        private void TryCreateDropdownButton(string dropdownName, ref Button dropdownButton)
        {
            if (dropdownButton != null) return;
            
            DropdownField originalDropdown = _root.Q<DropdownField>(dropdownName);
            if (originalDropdown != null)
            {
                // 获取父元素
                VisualElement parent = originalDropdown.parent;
                
                // 记录原始下拉菜单的样式
                var originalDisplay = originalDropdown.style.display;
                var originalWidth = originalDropdown.style.width;
                var originalHeight = originalDropdown.style.height;
                var originalMargin = originalDropdown.style.marginLeft;
                var originalAlignItems = originalDropdown.style.alignItems;
                
                // 创建新按钮
                dropdownButton = new Button();
                dropdownButton.name = dropdownName + "Button";
                dropdownButton.text = "请选择...";
                dropdownButton.AddToClassList("dropdown-button");
                
                // 复制样式
                dropdownButton.style.display = originalDisplay;
                dropdownButton.style.width = originalWidth;
                dropdownButton.style.height = originalHeight;
                dropdownButton.style.marginLeft = originalMargin;
                dropdownButton.style.alignItems = originalAlignItems;
                
                // 替换原始下拉菜单
                parent.Remove(originalDropdown);
                parent.Add(dropdownButton);
            }
            else
            {
                // 如果找不到原始下拉菜单，创建一个默认按钮
                Debug.LogWarning($"找不到下拉菜单：{dropdownName}，创建默认按钮");
                dropdownButton = new Button();
                dropdownButton.name = dropdownName + "Button";
                dropdownButton.text = "请选择...";
                dropdownButton.AddToClassList("dropdown-button");
                
                // 添加到UI根元素
                _root.Add(dropdownButton);
            }
        }
        
        /// <summary>
        /// 绑定UI事件到相应的处理函数
        /// </summary>
        private void BindUIEvents()
        {
            if (_playButton != null)
            {
                _playButton.clicked += () => {
                    MainSceneController.Instance.SwitchPlayPause(true);
                    UpdatePlaybackUI(true);
                };
            }
            
            if (_pauseButton != null)
            {
                _pauseButton.clicked += () => {
                    MainSceneController.Instance.SwitchPlayPause(false);
                    UpdatePlaybackUI(false);
                };
            }
            
            if (_stopButton != null)
            {
                _stopButton.clicked += () => {
                    MainSceneController.Instance.SwitchPlayPause(false);
                    MainSceneController.Instance.ResetAll();
                    UpdatePlaybackUI(false);
                    if (_timelineSlider != null) _timelineSlider.value = 0;
                    UpdateTimeDisplay(0);
                };
            }
            
            if (_timelineSlider != null)
            {
                _timelineSlider.RegisterValueChangedCallback(evt => {
                    // 无音乐时，设置动作播放进度
                    _motionTime = evt.newValue;
                    SetMmdMotionTime(_motionTime);
                    UpdateTimeDisplay(evt.newValue);
                });
            }
            
            if (_volumeSlider != null)
            {
                _volumeSlider.value = 0.8f;
            }
            
            // 设置按钮点击事件
            if (_settingsButton != null)
            {
                _settingsButton.clicked += () => {
                    _rightPanel.AddToClassList("visible");
                };
            }
        }
        
        #region Model Operations
        
        /// <summary>
        /// 更新模型下拉列表
        /// </summary>
        public void UpdateModelDropdown()
        {
            if (_modelDropdown == null) return;
            
            try
            {
                List<string> choices = new List<string>();
                
                // 添加已加载的模型名称
                foreach (var model in _loadedModels)
                {
                    if (!string.IsNullOrEmpty(model.Name))
                    {
                        choices.Add(model.Name);
                    }
                }
                
                // 总是添加"添加模型"选项
                choices.Add(ADD_MODEL_TEXT);
                
                // 如果没有选项，添加空白选项防止错误
                if (choices.Count == 0)
                {
                    choices.Add(EMPTY_OPTION);
                }
                
                // 更新下拉列表选项
                _modelDropdown.Choices = choices;
                
                // 确定要设置的值
                string valueToSet;
                
                if (_activeModel != null && !string.IsNullOrEmpty(_activeModel.Name) && choices.Contains(_activeModel.Name))
                {
                    // 如果有活动模型且在选项中，选择它
                    valueToSet = _activeModel.Name;
                }
                else if (choices.Contains(ADD_MODEL_TEXT))
                {
                    // 否则默认到"添加模型"选项
                    valueToSet = ADD_MODEL_TEXT;
                }
                else
                {
                    // 兜底到第一个选项
                    valueToSet = choices[0];
                }
                
                // 设置新值
                _modelDropdown.Value = valueToSet;
            }
            catch (System.Exception e)
            {
                Debug.LogError("更新模型下拉菜单时出错: " + e.Message);
            }
        }
        
        /// <summary>
        /// 打开模型文件选择器
        /// </summary>
        private void OpenModelFileSelector()
        {
            OpenFileSelector(MmdFileSelector.FileType.Model, "添加模型", filePath => {
                TtuiUtils.RunWithLoadingUI<LoadingDialog>(MainSceneController.Instance, () => {
                    try 
                    {
                        if (MainSceneController.Instance.AddModel(filePath))
                        {
                            // 创建新的模型对象
                            var model = new MmdModel 
                            {
                                Name = Path.GetFileNameWithoutExtension(filePath),
                                FilePath = filePath
                            };
                            
                            _loadedModels.Add(model);
                            _activeModel = model;
                            UpdateModelDropdown();
                            UpdateMotionDropdown();
                        }
                        else
                        {
                            ShowMessage("加载模型失败", 3f);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                        ShowMessage("加载模型出错: " + e.Message, 3f);
                    }
                });
            });
        }
        
        /// <summary>
        /// 选择已有模型
        /// </summary>
        private void SelectModel(string modelName)
        {
            if (string.IsNullOrEmpty(modelName)) return;
            
            var selectedModel = _loadedModels.FirstOrDefault(m => m.Name == modelName);
            if (selectedModel != null)
            {
                _activeModel = selectedModel;
                
                // 找到模型在MainSceneController中的索引
                var modelNames = MainSceneController.Instance.GetModelNames();
                int modelIndex = modelNames.IndexOf(modelName);
                
                if (modelIndex >= 0)
                {
                    MainSceneController.Instance.SelectModel(modelIndex);
                }
                
                // 更新动作下拉列表
                UpdateMotionDropdown();
            }
        }
        
        #endregion
        
        #region Motion Operations
        
        /// <summary>
        /// 更新动作下拉列表
        /// </summary>
        public void UpdateMotionDropdown()
        {
            if (_motionDropdown == null) return;
            
            try
            {
                List<string> choices = new List<string>();
                
                // 添加当前模型的动作
                if (_activeModel != null && _activeModel.Motions != null)
                {
                    foreach (var motion in _activeModel.Motions)
                    {
                        if (!string.IsNullOrEmpty(motion.Name))
                        {
                            choices.Add(motion.Name);
                        }
                    }
                }
                
                // 总是添加"选择动作"选项
                choices.Add(ADD_MOTION_TEXT);
                
                // 如果没有选项，添加空白选项防止错误
                if (choices.Count == 0)
                {
                    choices.Add(EMPTY_OPTION);
                }
                
                // 更新下拉列表选项
                _motionDropdown.Choices = choices;
                
                // 确定要设置的值
                string valueToSet;
                
                if (_activeModel != null && _activeModel.CurrentMotion != null && 
                    !string.IsNullOrEmpty(_activeModel.CurrentMotion.Name) && 
                    choices.Contains(_activeModel.CurrentMotion.Name))
                {
                    // 如果有当前动作且在选项中，选择它
                    valueToSet = _activeModel.CurrentMotion.Name;
                }
                else if (choices.Contains(ADD_MOTION_TEXT))
                {
                    // 否则默认到"选择动作"选项
                    valueToSet = ADD_MOTION_TEXT;
                }
                else
                {
                    // 兜底到第一个选项
                    valueToSet = choices[0];
                }
                
                // 设置新值
                _motionDropdown.Value = valueToSet;
            }
            catch (System.Exception e)
            {
                Debug.LogError("更新动作下拉菜单时出错: " + e.Message);
            }
        }
        
        /// <summary>
        /// 打开动作文件选择器
        /// </summary>
        private void OpenMotionFileSelector()
        {
            if (_activeModel == null)
            {
                ShowMessage("请先选择一个模型", 3f);
                return;
            }
            
            OpenFileSelector(MmdFileSelector.FileType.Motion, "选择动作", filePath => {
                TtuiUtils.RunWithLoadingUI<LoadingDialog>(MainSceneController.Instance, () => {
                    try
                    {
                        MainSceneController.Instance.ChangeCurrentMotion(filePath);
                        
                        // 创建新的动作对象
                        var motion = new MmdMotion
                        {
                            Name = Path.GetFileNameWithoutExtension(filePath),
                            FilePath = filePath
                        };
                        
                        _activeModel.Motions.Add(motion);
                        _activeModel.CurrentMotion = motion;
                        UpdateMotionDropdown();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                        ShowMessage("加载动作出错: " + e.Message, 3f);
                    }
                });
            });
        }
        
        /// <summary>
        /// 选择已有动作
        /// </summary>
        private void SelectMotion(string motionName)
        {
            if (_activeModel == null || string.IsNullOrEmpty(motionName)) return;
            
            var selectedMotion = _activeModel.Motions.FirstOrDefault(m => m.Name == motionName);
            if (selectedMotion != null)
            {
                _activeModel.CurrentMotion = selectedMotion;
                
                TtuiUtils.RunWithLoadingUI<LoadingDialog>(MainSceneController.Instance, () => {
                    MainSceneController.Instance.ChangeCurrentMotion(_activeModel.CurrentMotion.FilePath);
                });
            }
        }
        
        #endregion
        
        #region Music Operations
        
        /// <summary>
        /// 更新音乐下拉列表
        /// </summary>
        public void UpdateMusicDropdown()
        {
            if (_musicDropdown == null) return;
            try
            {
                List<string> choices = new List<string>();
                if (_currentMusic != null && !string.IsNullOrEmpty(_currentMusic.Name))
                {
                    choices.Add(_currentMusic.Name);
                }
                choices.Add(ADD_MUSIC_TEXT);
                if (choices.Count == 0)
                {
                    choices.Add(EMPTY_OPTION);
                }
                _musicDropdown.Choices = choices;
                string valueToSet;
                if (_currentMusic != null && !string.IsNullOrEmpty(_currentMusic.Name) && choices.Contains(_currentMusic.Name))
                {
                    valueToSet = _currentMusic.Name;
                }
                else if (choices.Contains(ADD_MUSIC_TEXT))
                {
                    valueToSet = ADD_MUSIC_TEXT;
                }
                else
                {
                    valueToSet = choices[0];
                }
                _musicDropdown.Value = valueToSet;
            }
            catch (System.Exception e)
            {
                Debug.LogError("更新音乐下拉菜单时出错: " + e.Message);
            }
        }
        
        /// <summary>
        /// 打开音乐文件选择器
        /// </summary>
        private void OpenMusicFileSelector()
        {
            OpenFileSelector(MmdFileSelector.FileType.Music, "选择音乐", filePath => {
                TtuiUtils.RunWithLoadingUI<LoadingDialog>(MainSceneController.Instance, () => {
                    try
                    {
                        MainSceneController.Instance.ChangeMusic(filePath);
                        var audioSource = MainSceneController.Instance.GetComponent<AudioSource>();
                        float duration = 0f;
                        if (audioSource != null && audioSource.clip != null)
                        {
                            duration = audioSource.clip.length;
                        }
                        _currentMusic = new MusicInfo
                        {
                            Name = Path.GetFileNameWithoutExtension(filePath),
                            FilePath = filePath,
                            Duration = duration
                        };
                        UpdateMusicDropdown();
                        SetTimelineDuration(_currentMusic.Duration);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                        ShowMessage("加载音乐出错: " + e.Message, 3f);
                    }
                });
            });
        }
        
        /// <summary>
        /// 选择已有音乐
        /// </summary>
        private void SelectMusic(string musicName)
        {
            // 当前设计中只支持一首音乐，这里可能不会被调用
            // 如果将来需要支持多音乐，可以在这里添加处理逻辑
        }
        
        #endregion
        
        private void Update()
        {
            if (_timelineSlider == null || MainSceneController.Instance == null)
                return;
            float totalTime = 0f;
            float currentTime = 0f;
            if (_activeModel != null && _activeModel.CurrentMotion != null)
            {
                int frameCount = GetCurrentMotionFrameCount();
                totalTime = frameCount > 0 ? frameCount / 30f : 0f;
            }
            if (_timelineSlider.highValue != totalTime)
                _timelineSlider.highValue = totalTime;
            if (IsPlaying())
            {
                float delta = Time.time - _lastUpdateTime;
                _motionTime += delta;
                if (_motionTime > totalTime) _motionTime = totalTime;
                SetMmdMotionTime(_motionTime);
            }
            if (!_isSliderDragging)
                _timelineSlider.value = _motionTime;
            UpdateTimeDisplay(_timelineSlider.value);
            UpdateTotalTimeLabel(totalTime);
            _lastUpdateTime = Time.time;
        }

        // 新增：拖拽状态标记
        private bool _isSliderDragging = false;

        /// <summary>
        /// 获取当前是否正在播放
        /// </summary>
        private bool IsPlaying()
        {
            // 这是一个简化的判断，实际项目中可能需要更准确的方式
            return MainSceneController.Instance != null && MainSceneController.Instance.GetModelCount() > 0;
        }
        
        /// <summary>
        /// 获取当前播放时间
        /// </summary>
        private float GetCurrentPlayTime()
        {
            // 简化实现，实际项目中可能需要更准确的方式
            return _timelineSlider != null ? _timelineSlider.value : 0f;
        }
        
        /// <summary>
        /// 更新播放控制UI状态
        /// </summary>
        private void UpdatePlaybackUI(bool isPlaying)
        {
            if (_playButton != null) _playButton.style.display = isPlaying ? DisplayStyle.None : DisplayStyle.Flex;
            if (_pauseButton != null) _pauseButton.style.display = isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        /// <summary>
        /// 更新时间显示
        /// </summary>
        private void UpdateTimeDisplay(float timeInSeconds)
        {
            if (_currentTimeLabel == null) return;
            
            int minutes = (int)(timeInSeconds / 60);
            int seconds = (int)(timeInSeconds % 60);
            _currentTimeLabel.text = $"{minutes:00}:{seconds:00}";
        }
        
        /// <summary>
        /// 设置时间线最大值（音乐总长度）并更新总时长标签
        /// </summary>
        public void SetTimelineDuration(float duration)
        {
            if (_timelineSlider != null)
            {
                _timelineSlider.highValue = duration;
            }
            
            if (_totalTimeLabel != null)
            {
                int minutes = (int)(duration / 60);
                int seconds = (int)(duration % 60);
                _totalTimeLabel.text = $"{minutes:00}:{seconds:00}";
            }
        }
        
        /// <summary>
        /// 显示消息
        /// </summary>
        public void ShowMessage(string message, float duration = 3f)
        {
            if (_overlayRoot == null) return;
            
            var messageLabel = _overlayRoot.Q<Label>("MessageLabel");
            if (messageLabel != null)
            {
                messageLabel.text = message;
                messageLabel.style.opacity = 1;
                messageLabel.schedule.Execute(() => {
                    messageLabel.style.opacity = 0;
                }).StartingIn((long)(duration * 1000));
            }
        }
        
        /// <summary>
        /// 添加模型
        /// </summary>
        public void AddModel(MmdModel model)
        {
            if (model == null) return;
            
            _loadedModels.Add(model);
            _activeModel = model;
            UpdateModelDropdown();
            UpdateMotionDropdown();
        }
        
        /// <summary>
        /// 删除当前选中的模型
        /// </summary>
        public void DeleteCurrentModel()
        {
            if (_activeModel == null) return;
            
            MainSceneController.Instance.DeleteCurrentModel();
            _loadedModels.Remove(_activeModel);
            
            if (_loadedModels.Count > 0)
            {
                _activeModel = _loadedModels[0];
            }
            else
            {
                _activeModel = null;
            }
            
            UpdateModelDropdown();
            UpdateMotionDropdown();
        }
        
        /// <summary>
        /// 打开文件选择器
        /// </summary>
        private void OpenFileSelector(MmdFileSelector.FileType fileType, string title, MmdFileSelector.FileSelectHandler onFileSelect)
        {
            TTUIPage.ShowPage<MmdFileSelector>(new MmdFileSelector.Context
            {
                Type = fileType,
                Title = title,
                OnFileSelect = onFileSelect
            });
        }

        private bool HasMusic()
        {
            var audioSource = MainSceneController.Instance.GetComponent<AudioSource>();
            return audioSource != null && audioSource.clip != null;
        }

        private int GetCurrentMotionFrameCount()
        {
            var mmdObj = MainSceneController.Instance.GetComponentInChildren<LibMMD.Unity3D.MmdGameObject>();
            if (mmdObj != null)
            {
                // 尝试通过反射获取_motionPlayer的帧数，否则用动作时长*30
                var type = mmdObj.GetType();
                var motionPlayerField = type.GetField("_motionPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (motionPlayerField != null)
                {
                    var motionPlayer = motionPlayerField.GetValue(mmdObj);
                    if (motionPlayer != null)
                    {
                        var getFrameLength = motionPlayer.GetType().GetMethod("GetMotionFrameLength");
                        if (getFrameLength != null)
                        {
                            object frameCount = getFrameLength.Invoke(motionPlayer, null);
                            if (frameCount is int fc)
                                return fc;
                        }
                    }
                }
                // fallback: 2分钟动作，30fps
                return 2 * 60 * 30;
            }
            return 0;
        }

        private void SetMmdMotionTime(float time)
        {
            var mmdObj = MainSceneController.Instance.GetComponentInChildren<LibMMD.Unity3D.MmdGameObject>();
            if (mmdObj != null)
            {
                mmdObj.SetMotionPos(time); // 直接用秒为单位设置
            }
        }

        private void UpdateTotalTimeLabel(float totalTime)
        {
            if (_totalTimeLabel != null)
            {
                int minutes = (int)(totalTime / 60);
                int seconds = (int)(totalTime % 60);
                _totalTimeLabel.text = $"{minutes:00}:{seconds:00}";
            }
        }

        private bool IsSliderDragging()
        {
            return _timelineSlider != null && _timelineSlider.focusController != null && _timelineSlider.focusController.focusedElement == _timelineSlider;
        }
    }
    
    // 数据类保持不变
    /// <summary>
    /// MMD模型数据类
    /// 用于MVC模式中的Model部分
    /// </summary>
    public class MmdModel
    {
        public string Name { get; set; }
        public string FilePath { get; set; }
        public GameObject ModelObject { get; set; }
        public List<MmdMotion> Motions { get; set; } = new List<MmdMotion>();
        public MmdMotion CurrentMotion { get; set; } = null;
    }
    
    /// <summary>
    /// MMD动作数据类
    /// </summary>
    public class MmdMotion
    {
        public string Name { get; set; }
        public string FilePath { get; set; }
    }
    
    /// <summary>
    /// 音乐信息类
    /// </summary>
    public class MusicInfo
    {
        public string Name { get; set; }
        public string FilePath { get; set; }
        public float Duration { get; set; }
    }
}