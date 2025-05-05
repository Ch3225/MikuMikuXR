using MikuMikuXR.Core;
using MikuMikuXR.Core.Interfaces;
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
        
        private Button _playButton;
        private Button _pauseButton;
        private Button _stopButton;
        private Slider _timelineSlider;
        private Label _timeLabel;
        private Slider _volumeSlider;
        
        // 移除未实现的控制器引用
        // private PlaybackController _playbackController;
        // private ModelPanelController _modelPanelController;
        
        private void Awake()
        {
            // 确保UI Toolkit文档已分配
            if (_mainDocument == null)
            {
                Debug.LogError("主UI文档未分配到DesktopUIController");
                return;
            }
            
            // 初始化简化版本
            // 移除对未实现控制器的初始化
            // _playbackController = GetComponent<PlaybackController>() ?? gameObject.AddComponent<PlaybackController>();
        }
        
        private void OnEnable()
        {
            if (_mainDocument == null) return;
            
            // 获取UI根元素
            _root = _mainDocument.rootVisualElement;
            
            if (_overlayDocument != null)
            {
                _overlayRoot = _overlayDocument.rootVisualElement;
            }
            
            // 初始化UI元素引用
            InitializeUIReferences();
            
            // 绑定UI事件
            BindUIEvents();
        }
        
        /// <summary>
        /// 初始化UI元素引用
        /// </summary>
        private void InitializeUIReferences()
        {
            // 播放控制按钮
            _playButton = _root.Q<Button>("PlayButton");
            _pauseButton = _root.Q<Button>("PauseButton");
            _stopButton = _root.Q<Button>("StopButton");
            
            // 时间轴和标签
            _timelineSlider = _root.Q<Slider>("TimelineSlider");
            _timeLabel = _root.Q<Label>("TimeLabel");
            
            // 音量控制
            _volumeSlider = _root.Q<Slider>("VolumeSlider");
        }
        
        /// <summary>
        /// 绑定UI事件到相应的处理函数
        /// </summary>
        private void BindUIEvents()
        {
            if (_playButton != null)
            {
                _playButton.clicked += () => {
                    MmdPlayer.Instance.Play();
                    UpdatePlaybackUI(true);
                };
            }
            
            if (_pauseButton != null)
            {
                _pauseButton.clicked += () => {
                    MmdPlayer.Instance.Pause();
                    UpdatePlaybackUI(false);
                };
            }
            
            if (_stopButton != null)
            {
                _stopButton.clicked += () => {
                    MmdPlayer.Instance.Stop();
                    UpdatePlaybackUI(false);
                    if (_timelineSlider != null) _timelineSlider.value = 0;
                    UpdateTimeDisplay(0);
                };
            }
            
            if (_timelineSlider != null)
            {
                _timelineSlider.RegisterValueChangedCallback(evt => {
                    MmdPlayer.Instance.SetTime(evt.newValue);
                    UpdateTimeDisplay(evt.newValue);
                });
            }
            
            if (_volumeSlider != null && MmdPlayer.Instance.MusicManager != null)
            {
                _volumeSlider.value = MmdPlayer.Instance.MusicManager.GetVolume();
                
                _volumeSlider.RegisterValueChangedCallback(evt => {
                    if (MmdPlayer.Instance.MusicManager != null)
                    {
                        MmdPlayer.Instance.MusicManager.SetVolume(evt.newValue);
                    }
                });
            }
        }
        
        private void Update()
        {
            // 如果时间线滑块存在且音乐正在播放，则更新滑块位置
            if (_timelineSlider != null && MmdPlayer.Instance.MusicManager != null && MmdPlayer.Instance.MusicManager.IsPlaying())
            {
                float currentTime = MmdPlayer.Instance.MusicManager.GetTime();
                
                // 修改：使用focusedElement属性检查元素是否被聚焦
                if (_timelineSlider.focusController == null || _timelineSlider.focusController.focusedElement != _timelineSlider)
                {
                    _timelineSlider.value = currentTime;
                    UpdateTimeDisplay(currentTime);
                }
            }
        }
        
        /// <summary>
        /// 更新播放控制UI状态
        /// </summary>
        /// <param name="isPlaying">是否正在播放</param>
        private void UpdatePlaybackUI(bool isPlaying)
        {
            if (_playButton != null) _playButton.style.display = isPlaying ? DisplayStyle.None : DisplayStyle.Flex;
            if (_pauseButton != null) _pauseButton.style.display = isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        /// <summary>
        /// 更新时间显示
        /// </summary>
        /// <param name="timeInSeconds">当前时间（秒）</param>
        private void UpdateTimeDisplay(float timeInSeconds)
        {
            if (_timeLabel == null) return;
            
            int minutes = (int)(timeInSeconds / 60);
            int seconds = (int)(timeInSeconds % 60);
            _timeLabel.text = $"{minutes:00}:{seconds:00}";
        }
        
        /// <summary>
        /// 设置时间线最大值（音乐总长度）
        /// </summary>
        /// <param name="duration">总时长（秒）</param>
        public void SetTimelineDuration(float duration)
        {
            if (_timelineSlider != null)
            {
                _timelineSlider.highValue = duration;
            }
        }
        
        /// <summary>
        /// 显示消息
        /// </summary>
        /// <param name="message">要显示的消息</param>
        /// <param name="duration">显示时长（秒）</param>
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
    }
}