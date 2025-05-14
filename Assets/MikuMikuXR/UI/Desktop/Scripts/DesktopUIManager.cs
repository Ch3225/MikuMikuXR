using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleFileBrowser;
using LibMMD.Unity3D;
using System.IO;

namespace MikuMikuXR.UI.Desktop
{
    /// <summary>
    /// 桌面端UI总管理器，统一管理模型、动作、音乐、相机等加载与UI刷新
    /// </summary>
    public class DesktopUIManager : MonoBehaviour
    {
        [Header("UI 组件")]
        public TMP_Dropdown modelDropdown;
        public TMP_Dropdown musicDropdown;
        public Button addModelButton;
        public Button addMusicButton;
        public TMP_Dropdown MotionDropDown;
        public Button BtnAddMotion;
        public Button PlayButton;
        public Slider PlaySlider;
        public TextMeshProUGUI Timer;
        public TextMeshProUGUI PlayButtonText; // 播放按钮下方的文字
        public Button BtnMute;
        public Slider VolumnBar;
        public TMP_Dropdown CameraDropdown; // 摄像机动作下拉框
        public Button BtnAddCamera;         // 添加摄像机动作按钮
        private AudioSource musicSource;
        private double currentTime = 0;
        private double totalTime = 0;
        private string currentMusicName = null; // 当前音乐文件名
        // 可继续添加其它UI引用，如动作、相机等

        [Header("子功能管理器")]
        public ModelFileLoader modelFileLoader;
        // public MusicFileLoader musicFileLoader; // 预留

        // 每个模型的动作列表，key为模型GameObject，value为动作路径列表
        private System.Collections.Generic.Dictionary<GameObject, System.Collections.Generic.List<string>> modelMotions = new System.Collections.Generic.Dictionary<GameObject, System.Collections.Generic.List<string>>();
        // 全局摄像机动作路径列表
        private System.Collections.Generic.List<string> cameraVmdPaths = new System.Collections.Generic.List<string>();
        private int currentCameraIndex = -1;

        private bool isPlaying = false;
        private bool isSliderDragging = false;

        // 缓存路径：模型一个，动作/相机/音乐共用一个
        private string lastModelPath = null;
        private string lastResourcePath = null;

        void Awake()
        {
            // 初始化各子功能
            if (modelFileLoader == null)
                modelFileLoader = GetComponent<ModelFileLoader>();
            // if (musicFileLoader == null)
            //     musicFileLoader = GetComponent<MusicFileLoader>();
        }

        void Start()
        {
            // 绑定按钮事件
            if (addModelButton != null)
                addModelButton.onClick.AddListener(OnAddModelClicked);
            if (addMusicButton != null)
                addMusicButton.onClick.AddListener(OnAddMusicClicked);
            if (BtnAddMotion != null)
                BtnAddMotion.onClick.AddListener(OnAddMotionClicked);
            if (MotionDropDown != null)
                MotionDropDown.onValueChanged.AddListener(OnMotionSelected);
            if (modelDropdown != null)
                modelDropdown.onValueChanged.AddListener(delegate { RefreshMotionDropDown(); });
            if (PlayButton != null)
                PlayButton.onClick.AddListener(OnPlayButtonClicked);
            if (PlaySlider != null)
            {
                PlaySlider.onValueChanged.AddListener(OnPlaySliderChanged);
                // 注册拖拽事件
                var eventTrigger = PlaySlider.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (eventTrigger == null)
                    eventTrigger = PlaySlider.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                // PointerDown
                var entryDown = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown };
                entryDown.callback.AddListener((data) => { isSliderDragging = true; });
                eventTrigger.triggers.Add(entryDown);
                // PointerUp
                var entryUp = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp };
                entryUp.callback.AddListener((data) => { isSliderDragging = false; OnPlaySliderDragEnd(); });
                eventTrigger.triggers.Add(entryUp);
            }
            if (Timer == null)
            {
                var timerObj = GameObject.Find("Timer");
                if (timerObj != null)
                    Timer = timerObj.GetComponent<TextMeshProUGUI>();
            }
            if (PlayButtonText == null)
            {
                var playTextObj = GameObject.Find("PlayButtonText");
                if (playTextObj != null)
                    PlayButtonText = playTextObj.GetComponent<TextMeshProUGUI>();
            }
            musicSource = FindObjectOfType<AudioSource>();
            // 获取Canvas下的AudioSource
            var canvasGo = GameObject.Find("Canvas");
            AudioSource canvasAudioSource = null;
            if (canvasGo != null)
                canvasAudioSource = canvasGo.GetComponent<AudioSource>();
            // 找到实际播放音乐的AudioSource（有clip的那个）
            AudioSource realAudioSource = null;
            var audioSourceObj = GameObject.Find("AudioSource");
            if (audioSourceObj != null)
                realAudioSource = audioSourceObj.GetComponent<AudioSource>();
            if (BtnMute != null && realAudioSource != null)
            {
                BtnMute.onClick.AddListener(() => {
                    realAudioSource.mute = !realAudioSource.mute;
                    BtnMute.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = realAudioSource.mute ? "Unmute" : "Mute";
                });
                BtnMute.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = realAudioSource.mute ? "Unmute" : "Mute";
            }
            if (VolumnBar != null && realAudioSource != null)
            {
                VolumnBar.value = realAudioSource.volume;
                VolumnBar.onValueChanged.AddListener((value) => {
                    realAudioSource.volume = value;
                    if (realAudioSource.volume == 0f)
                    {
                        realAudioSource.mute = true;
                        if (BtnMute != null)
                            BtnMute.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Unmute";
                    }
                    else
                    {
                        realAudioSource.mute = false;
                        if (BtnMute != null)
                            BtnMute.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Mute";
                    }
                });
            }
            if (BtnAddCamera != null)
                BtnAddCamera.onClick.AddListener(OnAddCameraClicked);
            if (CameraDropdown != null)
                CameraDropdown.onValueChanged.AddListener(OnCameraDropdownChanged);
        }

        void Update()
        {
            UpdateTotalTime();
            UpdateTimerText();
            if (isPlaying)
            {
                // 只在没有音乐时由libmmd自己推进动画，有音乐时不每帧同步动画进度
                if (musicSource != null && musicSource.clip != null && musicSource.isPlaying)
                {
                    currentTime = musicSource.time;
                    // 不每帧SetAllModelMotionPos(currentTime)
                }
                // 没有音乐时，libmmd自己推进动画
            }
            // 只有未拖拽时自动更新slider
            if (PlaySlider != null && totalTime > 0 && !isSliderDragging)
                PlaySlider.value = (float)(currentTime / totalTime);
        }

        public void OnAddModelClicked()
        {
            FileBrowser.SetFilters(true, new FileBrowser.Filter("MMD模型", ".pmd", ".pmx"));
            FileBrowser.SetDefaultFilter(".pmx");
            FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
            // 旧版本不支持设置初始路径
            FileBrowser.ShowLoadDialog(
                (paths) => { OnModelFileSelected(paths); },
                () => { Debug.Log("取消选择模型文件"); },
                FileBrowser.PickMode.Files,
                false, // 不允许多选
                null, null, "选择MMD模型", "加载"
            );
        }

        private void OnModelFileSelected(string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return;

            lastModelPath = Path.GetDirectoryName(paths[0]);
            string modelPath = paths[0];
            string fileNameNoExt = Path.GetFileNameWithoutExtension(modelPath);
            var actors = GameObject.Find("Actors");
            int modelIndex = actors != null ? actors.transform.childCount + 1 : 1;
            string modelObjectName = $"MMDModel_{modelIndex}";
            // 创建MMD模型GameObject并加载，确保有SkinnedMeshRenderer和MeshFilter
            var go = new GameObject(modelObjectName);
            go.AddComponent<SkinnedMeshRenderer>();
            go.AddComponent<MeshFilter>();
            var mmdGo = go.AddComponent<MmdGameObject>();
            // 添加自定义meta脚本存储文件名
            var meta = go.AddComponent<MmdModelMeta>();
            meta.modelFileNameNoExt = fileNameNoExt;
            bool success = mmdGo.LoadModel(modelPath);
            if (!success)
            {
                Debug.LogError("模型加载失败: " + modelPath);
                Destroy(go);
            }
            else
            {
                // 挂到Actors下
                if (actors != null)
                {
                    go.transform.SetParent(actors.transform, false);
                }
                else
                {
                    Debug.LogWarning("未找到Actors对象，模型将放在场景根节点");
                }
                // 不再设置tag，避免Unity报错
                // go.tag = fileNameNoExt;
                Debug.Log($"模型加载成功: {modelObjectName} ({fileNameNoExt})");
                // 自动刷新下拉列表（方案B）
                UpdateModelDropdownByActors();
            }
        }

        private void OnAddMotionClicked()
        {
            FileBrowser.SetFilters(true, new FileBrowser.Filter("MMD动作", ".vmd"));
            FileBrowser.SetDefaultFilter(".vmd");
            FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
            // 旧版本不支持设置初始路径
            FileBrowser.ShowLoadDialog(
                (paths) => { OnMotionFileSelected(paths); },
                () => { Debug.Log("取消选择动作文件"); },
                FileBrowser.PickMode.Files,
                false,
                null, null, "选择MMD动作", "加载"
            );
        }

        private void OnMotionFileSelected(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;
            lastResourcePath = Path.GetDirectoryName(paths[0]);
            var model = GetSelectedModel();
            if (model == null) return;
            if (!modelMotions.ContainsKey(model))
                modelMotions[model] = new System.Collections.Generic.List<string>();
            string motionPath = paths[0];
            modelMotions[model].Add(motionPath);
            // 加载动作
            var mmdGo = model.GetComponent<MmdGameObject>();
            if (mmdGo != null)
                mmdGo.LoadMotion(motionPath);
            RefreshMotionDropDown();
        }

        private void RefreshMotionDropDown()
        {
            if (MotionDropDown == null) return;
            var model = GetSelectedModel();
            MotionDropDown.ClearOptions();
            if (model == null || !modelMotions.ContainsKey(model) || modelMotions[model].Count == 0)
            {
                MotionDropDown.AddOptions(new System.Collections.Generic.List<string> { "无动作" });
                return;
            }
            var options = new System.Collections.Generic.List<string>();
            foreach (var path in modelMotions[model])
                options.Add(Path.GetFileName(path));
            MotionDropDown.AddOptions(options);
        }

        private void OnMotionSelected(int index)
        {
            var model = GetSelectedModel();
            if (model == null || !modelMotions.ContainsKey(model)) return;
            var motions = modelMotions[model];
            if (index < 0 || index >= motions.Count) return;
            var mmdGo = model.GetComponent<MmdGameObject>();
            if (mmdGo != null)
                mmdGo.LoadMotion(motions[index]);
        }

        private GameObject GetSelectedModel()
        {
            var actors = GameObject.Find("Actors");
            if (actors == null) return null;
            int index = modelDropdown != null ? modelDropdown.value - 1 : -1; // 下拉第0项是“添加模型...”
            if (index < 0 || index >= actors.transform.childCount) return null;
            return actors.transform.GetChild(index).gameObject;
        }

        // 可扩展：统一刷新所有下拉列表
        public void RefreshAllDropdowns()
        {
            var fileManager = FindObjectOfType<UGUIFileManager>();
            if (fileManager != null)
            {
                fileManager.UpdateModelList();
                fileManager.UpdateMusicDropdown("");
                // 其它刷新方法...
            }
        }

        // 方案B：直接遍历Actors下的MMDModel，刷新模型下拉列表
        public void UpdateModelDropdownByActors()
        {
            if (modelDropdown == null) return;
            var actors = GameObject.Find("Actors");
            if (actors == null)
            {
                modelDropdown.ClearOptions();
                modelDropdown.AddOptions(new System.Collections.Generic.List<string> { "添加模型..." });
                return;
            }
            var modelList = new System.Collections.Generic.List<string> { "添加模型..." };
            foreach (Transform child in actors.transform)
            {
                var meta = child.GetComponent<MmdModelMeta>();
                string displayName = meta != null && !string.IsNullOrEmpty(meta.modelFileNameNoExt)
                    ? meta.modelFileNameNoExt
                    : child.gameObject.name;
                modelList.Add(displayName);
            }
            modelDropdown.ClearOptions();
            modelDropdown.AddOptions(modelList);
        }

        private void OnPlayButtonClicked()
        {
            isPlaying = !isPlaying;
            var actors = GameObject.Find("Actors");
            if (actors != null)
            {
                foreach (Transform child in actors.transform)
                {
                    var mmdGo = child.GetComponent<MmdGameObject>();
                    if (mmdGo != null)
                        mmdGo.Playing = isPlaying;
                }
            }
            // 同步MMD相机播放状态
            var mainCamera = GameObject.Find("MainCamera");
            if (mainCamera != null)
            {
                var mmdCam = mainCamera.GetComponent<MmdCameraObject>();
                if (mmdCam != null && mmdCam.enabled)
                    mmdCam.Playing = isPlaying;
            }
            if (musicSource != null && musicSource.clip != null)
            {
                if (isPlaying) musicSource.Play();
                else musicSource.Pause();
            }
            // --- 新增：切换播放按钮文字 ---
            if (PlayButtonText != null)
                PlayButtonText.text = isPlaying ? "Pause" : "Play";
        }

        private void OnPlaySliderChanged(float value)
        {
            // 只在拖拽时响应
            if (!isSliderDragging) return;
            if (totalTime > 0)
            {
                currentTime = value * totalTime;
                SetAllModelMotionPos(currentTime);
                if (musicSource != null && musicSource.clip != null)
                    musicSource.time = (float)currentTime;
            }
        }

        private void OnPlaySliderDragEnd()
        {
            // 拖拽结束时，强制同步一次
            if (totalTime > 0)
            {
                currentTime = PlaySlider.value * totalTime;
                SetAllModelMotionPos(currentTime);
                if (musicSource != null && musicSource.clip != null)
                    musicSource.time = (float)currentTime;
            }
        }

        private void UpdateTotalTime()
        {
            double maxMotion = 0;
            var actors = GameObject.Find("Actors");
            if (actors != null)
            {
                foreach (Transform child in actors.transform)
                {
                    var mmdGo = child.GetComponent<MmdGameObject>();
                    if (mmdGo != null)
                    {
                        var motionPlayerField = typeof(MmdGameObject).GetField("_motionPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var motionPlayer = motionPlayerField?.GetValue(mmdGo) as LibMMD.Motion.MotionPlayer;
                        if (motionPlayer != null)
                        {
                            // 确保单位为秒
                            double t = motionPlayer.GetMotionTimeLength();
                            if (t > maxMotion) maxMotion = t;
                        }
                    }
                }
            }
            double musicLen = (musicSource != null && musicSource.clip != null) ? musicSource.clip.length : 0;
            // 优先用音乐时长
            totalTime = musicLen > 0 ? musicLen : maxMotion;
        }

        private void SetAllModelMotionPos(double time)
        {
            var actors = GameObject.Find("Actors");
            if (actors != null)
            {
                foreach (Transform child in actors.transform)
                {
                    var mmdGo = child.GetComponent<MmdGameObject>();
                    if (mmdGo != null)
                    {
                        mmdGo.SetMotionPos(time);
                        // 拖动进度条时强制刷新一帧
                        mmdGo.Playing = false;
                        mmdGo.Playing = isPlaying;
                    }
                }
            }
            // 同步MMD相机进度
            var mainCamera = GameObject.Find("MainCamera");
            if (mainCamera != null)
            {
                var mmdCam = mainCamera.GetComponent<MmdCameraObject>();
                if (mmdCam != null && mmdCam.enabled)
                    mmdCam.SetPlayPos(time);
            }
        }

        private void UpdateTimerText()
        {
            int cur = Mathf.FloorToInt((float)currentTime);
            int tot = Mathf.FloorToInt((float)totalTime);
            if (Timer != null)
                Timer.text = string.Format("{0:D2}:{1:D2}/{2:D2}:{3:D2}", cur / 60, cur % 60, tot / 60, tot % 60);
        }

        private void OnAddMusicClicked()
        {
            FileBrowser.SetFilters(true, new FileBrowser.Filter("音频", ".mp3", ".wav", ".ogg"));
            FileBrowser.SetDefaultFilter(".mp3");
            FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
            // 旧版本不支持设置初始路径
            FileBrowser.ShowLoadDialog(
                (paths) => { OnMusicFileSelected(paths); },
                () => { Debug.Log("取消选择音乐文件"); },
                FileBrowser.PickMode.Files,
                false,
                null, null, "选择音乐", "加载"
            );
        }

        private void OnMusicFileSelected(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;
            lastResourcePath = Path.GetDirectoryName(paths[0]);
            string musicPath = paths[0];
            StartCoroutine(LoadMusicCoroutine(musicPath));
        }

        private System.Collections.IEnumerator LoadMusicCoroutine(string path)
        {
            var url = "file:///" + path.Replace("\\", "/");
            using (var www = new UnityEngine.Networking.UnityWebRequest(url))
            {
                www.downloadHandler = new UnityEngine.Networking.DownloadHandlerAudioClip(url, AudioType.UNKNOWN);
                yield return www.SendWebRequest();
                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                    if (musicSource == null)
                        musicSource = FindObjectOfType<AudioSource>();
                    if (musicSource == null)
                        musicSource = gameObject.AddComponent<AudioSource>();
                    bool wasPlaying = isPlaying;
                    musicSource.Stop();
                    musicSource.clip = clip;
                    musicSource.time = 0;
                    currentTime = 0;
                    isPlaying = false;
                    // --- 新增：记录当前音乐名并刷新下拉列表 ---
                    currentMusicName = Path.GetFileName(path);
                    UpdateMusicDropdown();
                    UpdateTotalTime();
                    UpdateTimerText();
                    if (wasPlaying)
                    {
                        isPlaying = true;
                        musicSource.Play();
                    }
                }
                else
                {
                    Debug.LogError("音乐加载失败: " + www.error);
                }
            }
        }

        // 新增：刷新音乐下拉列表，显示当前音乐名
        private void UpdateMusicDropdown()
        {
            if (musicDropdown == null) return;
            musicDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(currentMusicName))
                options.Add(currentMusicName);
            options.Add("选择音乐...");
            musicDropdown.AddOptions(options);
            // 选中当前音乐
            if (!string.IsNullOrEmpty(currentMusicName))
                musicDropdown.value = 0;
            else
                musicDropdown.value = 1;
        }

        private void OnAddCameraClicked()
        {
            FileBrowser.SetFilters(true, new FileBrowser.Filter("MMD相机", ".vmd"));
            FileBrowser.SetDefaultFilter(".vmd");
            FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
            FileBrowser.ShowLoadDialog(
                (paths) => { OnCameraFileSelected(paths); },
                () => { Debug.Log("取消选择相机动作文件"); },
                FileBrowser.PickMode.Files,
                false,
                null, null, "选择MMD相机动作", "加载"
            );
        }

        private void OnCameraFileSelected(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;
            lastResourcePath = System.IO.Path.GetDirectoryName(paths[0]);
            string cameraPath = paths[0];
            cameraVmdPaths.Add(cameraPath);
            RefreshCameraDropdown();
            CameraDropdown.value = cameraVmdPaths.Count - 1;
            CameraDropdown.RefreshShownValue();
            // 自动切换到MMD相机并加载VMD
            SwitchToMmdCamera(cameraPath);
        }

        private void RefreshCameraDropdown()
        {
            if (CameraDropdown == null) return;
            CameraDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>();
            foreach (var path in cameraVmdPaths)
                options.Add(System.IO.Path.GetFileName(path));
            options.Add("Free Camera"); // 始终加在最后
            if (options.Count == 1) // 只有Free Camera
                CameraDropdown.value = 0;
            CameraDropdown.AddOptions(options);
        }

        private void OnCameraDropdownChanged(int index)
        {
            if (index == cameraVmdPaths.Count) // Free Camera
            {
                currentCameraIndex = -1;
                SwitchToFreeCamera();
                return;
            }
            if (index < 0 || index >= cameraVmdPaths.Count) {
                currentCameraIndex = -1;
                SwitchToFreeCamera();
                return;
            }
            currentCameraIndex = index;
            string cameraPath = cameraVmdPaths[index];
            SwitchToMmdCamera(cameraPath);
        }

        // 切换到自由相机
        private void SwitchToFreeCamera()
        {
            var mainCamera = GameObject.Find("MainCamera");
            if (mainCamera != null) mainCamera.SetActive(true);
            // 查找并禁用/销毁MmdCameraObject
            var mmdCameraObj = GameObject.Find("MmdCameraObject_Instance");
            if (mmdCameraObj != null) mmdCameraObj.SetActive(false);
        }

        // 切换到MMD相机
        private void SwitchToMmdCamera(string vmdPath)
        {
            var mainCamera = GameObject.Find("MainCamera");
            if (mainCamera != null) mainCamera.SetActive(false);
            // 查找或创建MmdCameraObject
            var mmdCameraObj = GameObject.Find("MmdCameraObject_Instance");
            if (mmdCameraObj == null)
            {
                mmdCameraObj = LibMMD.Unity3D.MmdCameraObject.CreateGameObject("MmdCameraObject_Instance");
                mmdCameraObj.transform.position = new Vector3(0, 18, -40); // 可根据需要调整
                mmdCameraObj.transform.rotation = Quaternion.identity;
            }
            mmdCameraObj.SetActive(true);
            var mmd = mmdCameraObj.GetComponent<LibMMD.Unity3D.MmdCameraObject>();
            if (mmd != null)
            {
                mmd.LoadCameraMotion(vmdPath);
                mmd.Playing = true;
            }
        }
    }
}
