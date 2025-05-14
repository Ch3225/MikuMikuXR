using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SimpleFileBrowser;
using MMDVR.Managers;

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
                    string modelPath = paths[0];
                    // 通过事件系统请求加载模型
                    EventManager.OnModelLoadRequest?.Invoke(modelPath);
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
            FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
            FileBrowser.ShowLoadDialog(
                (paths) => {
                    if (paths == null || paths.Length == 0) return;
                    string musicPath = paths[0];
                    MusicManager.Instance?.LoadMusic(musicPath);
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
                    string motionPath = paths[0];
                    // 让用户选择目标模型
                    int actorIndex = modelDropdown != null ? modelDropdown.value : -1;
                    var actorMgr = ActorManager.Instance;
                    if (actorMgr != null && actorIndex >= 0 && actorIndex < actorMgr.actors.Count)
                    {
                        var actor = actorMgr.actors[actorIndex];
                        MotionManager.Instance.LoadAndAssignMotion(motionPath, actor);
                    }
                    else
                    {
                        Debug.LogWarning("请先选择一个模型再加载动作");
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

        private void HandleSeekTimeChanged(float time) { /* 已废弃，不再直接操作模型/相机/音乐 */ }

        private void UpdateTimerText(float time)
        {
            float total = MusicManager.Instance?.GetCurrentLength() ?? 0f;
            timerText.text = $"{FormatTime(time)}/{FormatTime(total)}";
        }

        private string FormatTime(float t)
        {
            int min = Mathf.FloorToInt(t / 60f);
            int sec = Mathf.FloorToInt(t % 60f);
            return $"{min:00}:{sec:00}";
        }
        private void OnMuteClicked()
        {
            var musicMgr = MusicManager.Instance;
            if (musicMgr != null && musicMgr.musics.Count > 0)
            {
                var audio = musicMgr.musics[0];
                audio.mute = !audio.mute;
                if (volumeSlider != null)
                {
                    if (audio.mute)
                        volumeSlider.value = 0f;
                    else
                        volumeSlider.value = audio.volume > 0 ? audio.volume : 1f;
                }
                if (muteButton != null && muteButton.GetComponentInChildren<TMPro.TextMeshProUGUI>() != null)
                {
                    muteButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = audio.mute ? "Unmute" : "Mute";
                }
            }
        }
        private void OnVolumeChanged(float value)
        {
            // 触发音量变更
            MusicManager.Instance?.SetVolume(value);
        }
        private void OnModelDropdownChanged(int index)
        {
            RefreshMotionDropdown();
        }
        private void OnMotionDropdownChanged(int index)
        {
            // 触发动作切换
        }
        private void OnMusicDropdownChanged(int index)
        {
            MusicManager.Instance?.Play(index);
        }
        private void OnCameraDropdownChanged(int index)
        {
            CameraManager.Instance?.ActivateCamera(index);
            if (index > 0)
            {
                MMDCameraManager.Instance?.SetActiveVmdCamera(index - 1);
            }
        }
        private void OnAddCameraClicked()
        {
            // 弹出文件选择器，选择VMD相机动作
            FileBrowser.SetFilters(true, new FileBrowser.Filter("MMD相机动作", ".vmd"));
            FileBrowser.SetDefaultFilter(".vmd");
            FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
            FileBrowser.ShowLoadDialog(
                (paths) => {
                    if (paths == null || paths.Length == 0) return;
                    string vmdPath = paths[0];
                    CameraManager.Instance?.LoadMmdCamera(vmdPath);
                },
                () => { Debug.Log("取消选择相机动作文件"); },
                FileBrowser.PickMode.Files,
                false,
                null, null, "选择MMD相机动作", "加载"
            );
        }

        // 刷新模型下拉列表
        private void RefreshModelDropdown()
        {
            var actorMgr = ActorManager.Instance;
            if (modelDropdown == null || actorMgr == null) return;
            modelDropdown.ClearOptions();
            var names = new List<string>();
            foreach (var go in actorMgr.actors)
            {
                names.Add(go.name);
            }
            modelDropdown.AddOptions(names);
        }

        // 刷新相机下拉列表
        private void RefreshCameraDropdown()
        {
            var mmdCamMgr = MMDCameraManager.Instance;
            if (cameraDropdown == null || mmdCamMgr == null) return;
            cameraDropdown.ClearOptions();
            var names = new List<string> {"Free Camera"};
            foreach (var path in mmdCamMgr.vmdCameraPaths)
            {
                names.Add(System.IO.Path.GetFileNameWithoutExtension(path));
            }
            cameraDropdown.AddOptions(names);
            cameraDropdown.value = 0;
            cameraDropdown.RefreshShownValue();
        }

        // 刷新动作下拉列表，只显示当前选中模型的动作
        private void RefreshMotionDropdown()
        {
            var motionMgr = MotionManager.Instance;
            var sceneStates = SceneStatesManager.Instance;
            if (motionDropdown == null || motionMgr == null || sceneStates == null) return;
            motionDropdown.ClearOptions();
            int modelIdx = modelDropdown != null ? modelDropdown.value : -1;
            var actorMgr = ActorManager.Instance;
            if (actorMgr == null || modelIdx < 0 || modelIdx >= actorMgr.actors.Count) return;
            var modelGo = actorMgr.actors[modelIdx];
            var motions = sceneStates.GetMotionsForModel(modelGo);
            var names = new List<string>();
            if (motions != null)
            {
                foreach (var go in motions)
                {
                    names.Add(go.name);
                }
            }
            motionDropdown.AddOptions(names);
            if (names.Count > 0)
            {
                motionDropdown.value = names.Count - 1;
                motionDropdown.RefreshShownValue();
            }
        }

        private void RefreshMusicDropdown()
        {
            var musicMgr = MusicManager.Instance;
            if (musicDropdown == null || musicMgr == null) return;
            musicDropdown.ClearOptions();
            var names = new List<string>();
            foreach (var audio in musicMgr.musics)
            {
                names.Add(audio.gameObject.name);
            }
            musicDropdown.AddOptions(names);
            if (names.Count > 0)
            {
                musicDropdown.value = names.Count - 1;
                musicDropdown.RefreshShownValue();
            }
        }

        void Update()
        {
            // 实时刷新音乐进度和时长显示
            var musicMgr = MusicManager.Instance;
            if (musicMgr != null && musicMgr.musics.Count > 0 && musicMgr.currentIndex >= 0)
            {
                float cur = musicMgr.GetCurrentTime();
                float total = musicMgr.GetCurrentLength();
                if (playSlider != null && total > 0)
                {
                    playSlider.maxValue = total;
                    playSlider.value = cur;
                }
                if (timerText != null)
                {
                    timerText.text = $"{FormatTime(cur)}/{FormatTime(total)}";
                }
            }
            if (!isSliderDragging && SceneStatesManager.Instance.isPlaying)
            {
                float cur = MusicManager.Instance?.GetCurrentTime() ?? 0f;
                playSlider.value = cur;
                UpdateTimerText(cur);
            }
        }

        // 可扩展：UI刷新方法、与SceneStatesManager的联动等
    }
}
