using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SimpleFileBrowser;
using MMDVR.Managers;
using MMDVR.Scripts.UIInteraction; // Added this line

namespace MMDVR.Managers
{
    /// <summary>
    /// 新版桌面端UI管理器，只负责UI事件收集和转发，业务逻辑交由SceneStatesManager等Manager处理
    /// </summary>
    public class DesktopUIManager : MonoBehaviour
    {
        [Header("模型相关UI组件")]
        public TMP_Dropdown modelDropdown;
        public Button addModelButton;
        public TMP_Dropdown motionDropdown;
        public Button addMotionButton;

        [Header("相机相关UI组件")]
        public TMP_Dropdown cameraDropdown;
        public Button addCameraButton;

        [Header("音乐相关UI组件")]
        public TMP_Dropdown musicDropdown;
        public Button addMusicButton;
        public Button muteButton;
        public Slider volumeSlider;

        [Header("播放控制UI组件")]
        public Button playButton;
        public Slider playSlider;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI playButtonText;

        private SceneStatesManager sceneStates;
        private bool isSliderDragging = false;
        private bool wasPlayingBeforeDrag = false;

        // 事件：定位到某个时间点
        public delegate void SeekTimeChangedHandler(float time);
        public static event SeekTimeChangedHandler OnSeekTimeChanged;

        void Awake()
        {
            sceneStates = FindObjectOfType<SceneStatesManager>();
            // 监听模型列表变更事件
            EventManager.OnActorListChanged += RefreshModelDropdown;
            EventManager.OnCameraListChanged += RefreshCameraDropdown;
            EventManager.OnMotionListChanged += RefreshMotionDropdown;
        }

        void Start()
        {
            // 绑定UI事件
            if (addModelButton != null)
                addModelButton.onClick.AddListener(OnAddModelClicked);
            if (addMusicButton != null)
                addMusicButton.onClick.AddListener(OnAddMusicClicked);
            if (addMotionButton != null)
                addMotionButton.onClick.AddListener(OnAddMotionClicked);
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayButtonClicked);
            if (playSlider != null)
            {
                playSlider.onValueChanged.AddListener(OnPlaySliderChanged);
                var eventTrigger = playSlider.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (eventTrigger == null)
                    eventTrigger = playSlider.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var entryDown = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown };
                entryDown.callback.AddListener((data) => {
                    wasPlayingBeforeDrag = SceneStatesManager.Instance.isPlaying;
                    SceneStatesManager.Instance.Pause();
                    isSliderDragging = true;
                    float value = playSlider.value;
                    SceneStatesManager.Instance.SeekTo(value);
                    UpdateTimerText(value);
                });
                eventTrigger.triggers.Add(entryDown);
                var entryUp = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp };
                entryUp.callback.AddListener((data) => {
                    isSliderDragging = false;
                    float value = playSlider.value;
                    SceneStatesManager.Instance.SeekTo(value);
                    UpdateTimerText(value);
                    if (wasPlayingBeforeDrag) {
                        SceneStatesManager.Instance.Play();
                    }
                });
                eventTrigger.triggers.Add(entryUp);
                var entryClick = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick };
                entryClick.callback.AddListener((data) => {
                    wasPlayingBeforeDrag = SceneStatesManager.Instance.isPlaying;
                    SceneStatesManager.Instance.Pause();
                    var rect = playSlider.GetComponent<RectTransform>();
                    Vector2 localPoint;
                    var pointerData = data as UnityEngine.EventSystems.PointerEventData;
                    if (pointerData != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, pointerData.position, pointerData.pressEventCamera, out localPoint))
                    {
                        float pct = Mathf.InverseLerp(rect.rect.xMin, rect.rect.xMax, localPoint.x);
                        float value = Mathf.Lerp(playSlider.minValue, playSlider.maxValue, pct);
                        playSlider.value = value;
                        SceneStatesManager.Instance.SeekTo(value);
                        UpdateTimerText(value);
                    }
                    if (wasPlayingBeforeDrag) {
                        SceneStatesManager.Instance.Play();
                    }
                });
                eventTrigger.triggers.Add(entryClick);
            }
            if (muteButton != null)
                muteButton.onClick.AddListener(OnMuteClicked);
            if (volumeSlider != null)
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            if (modelDropdown != null)
                modelDropdown.onValueChanged.AddListener(OnModelDropdownChanged);
            if (motionDropdown != null)
                motionDropdown.onValueChanged.AddListener(OnMotionDropdownChanged);
            if (musicDropdown != null)
                musicDropdown.onValueChanged.AddListener(OnMusicDropdownChanged);
            if (cameraDropdown != null)
                cameraDropdown.onValueChanged.AddListener(OnCameraDropdownChanged);
            if (addCameraButton != null)
                addCameraButton.onClick.AddListener(OnAddCameraClicked);

            // 设置所有下拉框内容溢出为省略号
            SetDropdownEllipsis(modelDropdown);
            SetDropdownEllipsis(motionDropdown);
            SetDropdownEllipsis(cameraDropdown);
            SetDropdownEllipsis(musicDropdown);

            if (volumeSlider != null)
                volumeSlider.value = 1f;
            // 启动时主动刷新所有下拉框，保证有初始项
            RefreshAllDropdowns();
        }

        private void SetDropdownEllipsis(TMP_Dropdown dropdown)
        {
            if (dropdown == null) return;
            if (dropdown.captionText != null)
            {
                dropdown.captionText.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                dropdown.captionText.enableWordWrapping = false;
            }
            if (dropdown.itemText != null)
            {
                dropdown.itemText.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                dropdown.itemText.enableWordWrapping = false;
            }
        }

        void OnDestroy()
        {
            EventManager.OnActorListChanged -= RefreshModelDropdown;
            EventManager.OnCameraListChanged -= RefreshCameraDropdown;
            EventManager.OnMotionListChanged -= RefreshMotionDropdown;
        }

        void OnEnable()
        {
            OnSeekTimeChanged += HandleSeekTimeChanged;
        }

        void OnDisable()
        {
            OnSeekTimeChanged -= HandleSeekTimeChanged;
        }

        // 以下方法只负责收集UI事件，具体业务交由Manager处理
        private void OnAddModelClicked()
        {
            // 弹出文件选择器，选择PMX/PMD模型
            FileBrowser.SetFilters(true, new FileBrowser.Filter("MMD模型", ".pmd", ".pmx"));
            FileBrowser.SetDefaultFilter(".pmx");
            FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
            FileBrowser.ShowLoadDialog(
                (paths) => {
                    if (paths == null || paths.Length == 0) return;
                    string modelPath = paths[0];                    if (SceneStatesManager.Instance != null)
                        SceneStatesManager.Instance.AddActor(modelPath);
                },
                () => { Debug.Log("取消选择模型文件"); },
                FileBrowser.PickMode.Files,
                false,
                null, null, "选择MMD模型", "加载"
            );
        }
        private void OnAddMusicClicked()
        {
            FileBrowser.SetFilters(true, new FileBrowser.Filter("音乐", ".mp3", ".wav", ".ogg"));
            FileBrowser.SetDefaultFilter(".mp3");
            FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");            FileBrowser.ShowLoadDialog(
                (paths) => {
                    if (paths == null || paths.Length == 0) return;
                    string musicPath = paths[0];
                    SceneStatesManager.Instance?.AddMusic(musicPath);
                    RefreshMusicDropdown();
                },
                () => { Debug.Log("取消选择音乐文件"); },
                FileBrowser.PickMode.Files,
                false,
                null, null, "选择音乐文件", "加载"
            );
        }
        private void OnAddMotionClicked()
        {
            // 弹出文件选择器，选择VMD动作
            FileBrowser.SetFilters(true, new FileBrowser.Filter("MMD动作", ".vmd"));
            FileBrowser.SetDefaultFilter(".vmd");
            FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
            FileBrowser.ShowLoadDialog(
                (paths) => {
                    if (paths == null || paths.Length == 0) return;
                    string motionPath = paths[0];                    // 让用户选择目标模型
                    int actorIndex = modelDropdown != null ? modelDropdown.value : -1;                    if (SceneStatesManager.Instance != null)
                    {                        var actorList = SceneStatesManager.Instance.GetActorList();
                        if (actorIndex >= 0 && actorIndex < actorList.Count)
                        {
                            string actorId = actorList[actorIndex].id;
                            string motionId = SceneStatesManager.Instance.AddMotion(motionPath);
                            
                            // 将加载的动作分配给选中的演员
                            SceneStatesManager.Instance.AssignMotionToActor(motionId, actorId);
                            
                            // 刷新动作下拉列表
                            RefreshMotionDropdown();
                            
                            Debug.Log($"动作已加载并分配给演员 {actorId}: {motionPath}");
                        }
                        else
                        {
                            Debug.LogWarning("请先选择一个模型再加载动作");
                        }
                    }
                },
                () => { Debug.Log("取消选择动作文件"); },
                FileBrowser.PickMode.Files,
                false,
                null, null, "选择MMD动作", "加载"
            );
        }
        private void OnPlayButtonClicked()
        {
            var stateMgr = SceneStatesManager.Instance;
            if (stateMgr == null) return;
            if (stateMgr.isPlaying)
                stateMgr.Pause();
            else
                stateMgr.Play();
            playButtonText.text = stateMgr.isPlaying ? "Pause" : "Play";
        }
        private void OnPlaySliderChanged(float value)
        {
            // 拖拽或暂停时都同步
            if (isSliderDragging || !SceneStatesManager.Instance.isPlaying)
            {
                SceneStatesManager.Instance.SeekTo(value);
                UpdateTimerText(value);
            }
        }

        private void HandleSeekTimeChanged(float time) { /* 已废弃，不再直接操作模型/相机/音乐 */ }        private void UpdateTimerText(float time)
        {
            float total = SceneStatesManager.Instance?.GetMusicDuration() ?? 0f;
            timerText.text = $"{FormatTime(time)}/{FormatTime(total)}";
        }

        private string FormatTime(float t)
        {
            int min = Mathf.FloorToInt(t / 60f);
            int sec = Mathf.FloorToInt(t % 60f);
            return $"{min:00}:{sec:00}";
        }        private void OnMuteClicked()
        {
            // TODO: 实现静音功能，需要在SceneStatesManager中添加静音状态管理
            Debug.Log("静音功能需要在SceneStatesManager中实现");
        }        private void OnVolumeChanged(float value)
        {
            // 触发音量变更
            SceneStatesManager.Instance?.SetMusicVolume(value);
        }
        private void OnModelDropdownChanged(int index)
        {
            RefreshMotionDropdown();
        }
        private void OnMotionDropdownChanged(int index)
        {
            // 触发动作切换
        }        private void OnMusicDropdownChanged(int index)
        {
            if (SceneStatesManager.Instance != null)
            {
                var musicDataList = SceneStatesManager.Instance.GetMusicList();
                if (index >= 0 && index < musicDataList.Count)
                {
                    SceneStatesManager.Instance.ActivateMusic(musicDataList[index].id);
                }
            }
        }
        
        private void OnCameraDropdownChanged(int index)
        {
            if (SceneStatesManager.Instance != null)
            {
                var cameraDataList = SceneStatesManager.Instance.GetCameraList();
                if (index >= 0 && index < cameraDataList.Count)
                {
                    SceneStatesManager.Instance.ActivateCamera(cameraDataList[index].id);
                }
            }
        }private void OnAddCameraClicked()
        {
            // 弹出文件选择器，选择VMD相机动作
            FileBrowser.SetFilters(true, new FileBrowser.Filter("MMD相机动作", ".vmd"));
            FileBrowser.SetDefaultFilter(".vmd");
            FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
            FileBrowser.ShowLoadDialog(
                (paths) => {
                    if (paths == null || paths.Length == 0) return;
                    string vmdPath = paths[0];                    if (SceneStatesManager.Instance != null)
                        SceneStatesManager.Instance.AddVMDCamera(vmdPath);
                    RefreshCameraDropdown();
                },
                () => { Debug.Log("取消选择相机动作文件"); },
                FileBrowser.PickMode.Files,
                false,
                null, null, "选择MMD相机动作", "加载"
            );
        }        // 刷新模型下拉列表
        private void RefreshModelDropdown()
        {
            if (modelDropdown == null || SceneStatesManager.Instance == null) return;
            
            modelDropdown.ClearOptions();
            var names = new List<string>();
              var actorList = SceneStatesManager.Instance.GetActorList();
            foreach (var actor in actorList)
            {
                names.Add(actor.displayName);
            }
            
            modelDropdown.AddOptions(names);
        }        // 刷新相机下拉列表
        private void RefreshCameraDropdown()
        {
            if (cameraDropdown == null || SceneStatesManager.Instance == null) return;
            
            cameraDropdown.ClearOptions();
            var names = new List<string>();
              var cameraList = SceneStatesManager.Instance.GetCameraList();
            foreach (var camera in cameraList)
            {
                names.Add(camera.displayName);
            }
            
            cameraDropdown.AddOptions(names);
              // 尝试选择当前激活的摄像机
            if (!string.IsNullOrEmpty(SceneStatesManager.Instance.currentActiveCameraId))
            {
                int currentIndex = cameraList.FindIndex(c => c.id == SceneStatesManager.Instance.currentActiveCameraId);
                if (currentIndex != -1)
                {
                    cameraDropdown.value = currentIndex;
                }
                else
                {
                    cameraDropdown.value = 0; // 默认选择第一个
                }
            }
            else
            {
                cameraDropdown.value = 0; // 默认选择第一个
            }
            cameraDropdown.RefreshShownValue();
        }        // 刷新动作下拉列表，只显示当前选中模型的动作
        private void RefreshMotionDropdown()
        {
            if (motionDropdown == null || SceneStatesManager.Instance == null) return;
            
            motionDropdown.ClearOptions();
            var names = new List<string>();
            
            int modelIdx = modelDropdown != null ? modelDropdown.value : -1;
            var actorList = SceneStatesManager.Instance.GetActorList();
              if (modelIdx >= 0 && modelIdx < actorList.Count)
            {
                string actorId = actorList[modelIdx].id;
                var motionList = SceneStatesManager.Instance.GetMotionList();
                  // 筛选分配给当前演员的动作
                foreach (var motion in motionList)
                {
                    if (motion.assignedActorId == actorId)
                    {
                        names.Add(motion.displayName);
                    }
                }
            }
            
            motionDropdown.AddOptions(names);
            if (names.Count > 0)
            {
                motionDropdown.value = names.Count - 1;
                motionDropdown.RefreshShownValue();
            }
        }private void RefreshMusicDropdown()
        {
            if (musicDropdown == null || SceneStatesManager.Instance == null) return;
            
            musicDropdown.ClearOptions();
            var names = new List<string>();
              var musicList = SceneStatesManager.Instance.GetMusicList();
            foreach (var music in musicList)
            {
                names.Add(music.title);
            }
            
            musicDropdown.AddOptions(names);
            if (names.Count > 0)
            {                // 尝试选择当前激活的音乐
                if (!string.IsNullOrEmpty(SceneStatesManager.Instance.currentActiveMusicId))
                {
                    int currentIndex = musicList.FindIndex(m => m.id == SceneStatesManager.Instance.currentActiveMusicId);
                    if (currentIndex != -1)
                    {
                        musicDropdown.value = currentIndex;
                    }
                    else
                    {
                        musicDropdown.value = 0; // 默认选择第一个
                    }
                }
                else
                {
                    musicDropdown.value = 0; // 默认选择第一个
                }
                musicDropdown.RefreshShownValue();
            }
        }

        // 统一刷新所有下拉框
        public void RefreshAllDropdowns()
        {
            RefreshModelDropdown();
            RefreshMotionDropdown();
            RefreshCameraDropdown();
            RefreshMusicDropdown();
        }        void Update()
        {
            // 实时刷新音乐进度和时长显示
            if (SceneStatesManager.Instance != null)
            {
                float currentTime = SceneStatesManager.Instance.playTime;
                float totalDuration = SceneStatesManager.Instance.totalDuration;
                
                if (playSlider != null && totalDuration > 0)
                {
                    playSlider.maxValue = totalDuration;
                    if (!isSliderDragging)
                    {
                        playSlider.value = currentTime;
                    }
                }
                
                if (timerText != null)
                {
                    UpdateTimerText(currentTime);
                }
                
                // 更新播放按钮文本
                if (playButtonText != null)
                {
                    playButtonText.text = SceneStatesManager.Instance.isPlaying ? "Pause" : "Play";
                }
            }
        }

        // 可扩展：UI刷新方法、与SceneStatesManager的联动等
    }
}
