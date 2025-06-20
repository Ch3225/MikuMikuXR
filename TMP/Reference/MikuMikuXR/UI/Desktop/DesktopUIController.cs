using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;
using MikuMikuXR.SceneController;
using MikuMikuXR.XR; // For XrType
using System.Linq; // For FirstOrDefault

public class DesktopUIController : MonoBehaviour
{
    private VisualElement root;
    private DropdownField modelDropdown;
    private DropdownField motionDropdown;
    private DropdownField musicDropdown;
    private DropdownField cameraDropdown;

    private Button settingsButton;
    private VisualElement leftPanel;
    private ListView modelListDisplay; // Renamed to avoid confusion with UXML name if it's different
    private Button closeLeftPanelButton;

    private VisualElement rightPanel;
    private Button closeRightPanelButton;
    private Button resetSettingsButton;
    private Button applySettingsButton;
    
    // Settings Panel Controls
    private SliderInt qualityLevelSlider;
    private SliderInt antiAliasingSlider;
    private DropdownField shadowQualityDropdown;
    private DropdownField physicsQualityDropdown;
    private SliderInt physicsUpdateRateSlider;
    private Toggle autoHideControlsToggle;
    private SliderInt uiOpacitySlider;


    private Slider timelineSlider;
    private Label currentTimeLabel;
    private Label totalTimeLabel;
    private Button prevButton, playButton, pauseButton, stopButton, nextButton;
    private Button muteButton;
    private Slider volumeSlider;

    private const string LoadModelText = "加载模型";
    private const string LoadMotionText = "加载动作";
    private const string LoadMusicText = "加载音乐";
    private const string LoadCameraText = "加载相机";

    private List<string> loadedModelPaths = new List<string>();
    private List<string> loadedMotionPaths = new List<string>();
    private List<string> loadedMusicPaths = new List<string>();
    private List<string> loadedCameraPaths = new List<string>();

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("DesktopUIController: UIDocument component not found!");
            this.enabled = false;
            return;
        }
        root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("DesktopUIController: UIDocument rootVisualElement is null!");
            this.enabled = false;
            return;
        }

        // Top Toolbar
        modelDropdown = root.Q<DropdownField>("ModelDropdown");
        motionDropdown = root.Q<DropdownField>("MotionDropdown");
        musicDropdown = root.Q<DropdownField>("MusicDropdown");
        cameraDropdown = root.Q<DropdownField>("CameraDropdown");
        settingsButton = root.Q<Button>("SettingsButton");

        // Left Panel
        leftPanel = root.Q<VisualElement>("LeftPanel");
        modelListDisplay = root.Q<ListView>("ModelList"); // UXML uses "ModelList"
        closeLeftPanelButton = root.Q<Button>("CloseLeftPanelButton");
        if (leftPanel != null) leftPanel.style.display = DisplayStyle.None;

        // Right Panel (Settings)
        rightPanel = root.Q<VisualElement>("RightPanel");
        closeRightPanelButton = root.Q<Button>("CloseRightPanelButton");
        resetSettingsButton = root.Q<Button>("ResetSettingsButton");
        applySettingsButton = root.Q<Button>("ApplySettingsButton");
        if (rightPanel != null) rightPanel.style.display = DisplayStyle.None;
        
        qualityLevelSlider = root.Q<SliderInt>("QualityLevelSlider");
        antiAliasingSlider = root.Q<SliderInt>("AntiAliasingSlider");
        shadowQualityDropdown = root.Q<DropdownField>("ShadowQualityDropdown");
        physicsQualityDropdown = root.Q<DropdownField>("PhysicsQualityDropdown");
        physicsUpdateRateSlider = root.Q<SliderInt>("PhysicsUpdateRateSlider");
        autoHideControlsToggle = root.Q<Toggle>("AutoHideControlsToggle");
        uiOpacitySlider = root.Q<SliderInt>("UIOpacitySlider");


        // Bottom Controls
        timelineSlider = root.Q<Slider>("TimelineSlider");
        currentTimeLabel = root.Q<Label>("CurrentTimeLabel");
        totalTimeLabel = root.Q<Label>("TotalTimeLabel");
        prevButton = root.Q<Button>("PrevButton");
        playButton = root.Q<Button>("PlayButton");
        pauseButton = root.Q<Button>("PauseButton");
        stopButton = root.Q<Button>("StopButton");
        nextButton = root.Q<Button>("NextButton");
        muteButton = root.Q<Button>("MuteButton");
        volumeSlider = root.Q<Slider>("VolumeSlider");
        
        if (pauseButton != null) pauseButton.style.display = DisplayStyle.None;

        SetupDropdown(modelDropdown, LoadModelText, HandleLoadModel, HandleSelectModel);
        SetupDropdown(motionDropdown, LoadMotionText, HandleLoadMotion, HandleSelectMotion);
        SetupDropdown(musicDropdown, LoadMusicText, HandleLoadMusic, HandleSelectMusic);
        SetupDropdown(cameraDropdown, LoadCameraText, HandleLoadCamera, HandleSelectCamera);

        if (settingsButton != null) settingsButton.clicked += ToggleRightPanel;
        if (closeLeftPanelButton != null) closeLeftPanelButton.clicked += () => { if (leftPanel != null) leftPanel.style.display = DisplayStyle.None; };
        if (closeRightPanelButton != null) closeRightPanelButton.clicked += () => { if (rightPanel != null) rightPanel.style.display = DisplayStyle.None; };
        
        if (playButton != null) playButton.clicked += OnPlayClicked;
        if (pauseButton != null) pauseButton.clicked += OnPauseClicked;
        if (stopButton != null) stopButton.clicked += OnStopClicked;

        InitializeDropdowns();
        RegisterMainSceneControllerEvents();
        LoadSettings(); // Load and apply settings
        
        if (applySettingsButton != null) applySettingsButton.clicked += ApplySettings;
        if (resetSettingsButton != null) resetSettingsButton.clicked += ResetAndApplyDefaultSettings;

        // ModelList in UXML seems to be for listing models. Let's make the "Model" label in TopToolbar toggle it.
        var modelLabel = root.Q<Label>("模型"); // Assuming this is the label next to ModelDropdown
        modelLabel?.RegisterCallback<ClickEvent>(evt => ToggleLeftPanel());
    }
    
    void OnDisable()
    {
        UnregisterMainSceneControllerEvents();
        // Unregister other callbacks if necessary
        if (settingsButton != null) settingsButton.clicked -= ToggleRightPanel;
        if (closeLeftPanelButton != null) closeLeftPanelButton.clicked -= () => { if (leftPanel != null) leftPanel.style.display = DisplayStyle.None; };
        if (closeRightPanelButton != null) closeRightPanelButton.clicked -= () => { if (rightPanel != null) rightPanel.style.display = DisplayStyle.None; };
        if (playButton != null) playButton.clicked -= OnPlayClicked;
        if (pauseButton != null) pauseButton.clicked -= OnPauseClicked;
        if (stopButton != null) stopButton.clicked -= OnStopClicked;
        if (applySettingsButton != null) applySettingsButton.clicked -= ApplySettings;
        if (resetSettingsButton != null) resetSettingsButton.clicked -= ResetAndApplyDefaultSettings;
        
        var modelLabel = root.Q<Label>("模型");
        // It's tricky to remove anonymous delegates directly without storing them.
        // For robust cleanup, avoid anonymous delegates for event handlers that need removal,
        // or ensure the object is destroyed, which cleans up UIElements event listeners.
    }

    private void InitializeDropdowns()
    {
        UpdateDropdownChoices(modelDropdown, LoadModelText, loadedModelPaths.ConvertAll(Path.GetFileName));
        UpdateDropdownChoices(motionDropdown, LoadMotionText, loadedMotionPaths.ConvertAll(Path.GetFileName));
        UpdateDropdownChoices(musicDropdown, LoadMusicText, loadedMusicPaths.ConvertAll(Path.GetFileName));
        UpdateDropdownChoices(cameraDropdown, LoadCameraText, loadedCameraPaths.ConvertAll(Path.GetFileName));
    }

    private void SetupDropdown(DropdownField dropdown, string loadText, System.Action<string> loadActionCallback, System.Action<string> selectActionCallback)
    {
        if (dropdown == null) 
        {
            Debug.LogWarning($"Dropdown for '{loadText}' not found.");
            return;
        }
        dropdown.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == loadText)
            {
                OpenFilePanelFor(loadText, loadActionCallback);
                // After attempting to load, reset dropdown to previous valid selection or loadText
                // This prevents "Load X" from staying selected.
                dropdown.SetValueWithoutNotify(evt.previousValue ?? loadText);
            }
            else
            {
                // User selected an already loaded item
                int selectedIndexInChoices = dropdown.choices.IndexOf(evt.newValue);
                int actualItemIndex = selectedIndexInChoices - 1; // Because index 0 is "Load X"

                string selectedPath = "";
                if (dropdown == modelDropdown && actualItemIndex >= 0 && actualItemIndex < loadedModelPaths.Count) selectedPath = loadedModelPaths[actualItemIndex];
                else if (dropdown == motionDropdown && actualItemIndex >= 0 && actualItemIndex < loadedMotionPaths.Count) selectedPath = loadedMotionPaths[actualItemIndex];
                else if (dropdown == musicDropdown && actualItemIndex >= 0 && actualItemIndex < loadedMusicPaths.Count) selectedPath = loadedMusicPaths[actualItemIndex];
                else if (dropdown == cameraDropdown && actualItemIndex >= 0 && actualItemIndex < loadedCameraPaths.Count) selectedPath = loadedCameraPaths[actualItemIndex];

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    selectActionCallback(selectedPath);
                }
                else if (selectedIndexInChoices > 0) // Selected something other than "Load X" but path not found (should not happen)
                {
                    Debug.LogWarning($"Selected item '{evt.newValue}' in {dropdown.name} but could not find corresponding path.");
                }
            }
        });
    }

    private void OpenFilePanelFor(string type, System.Action<string> callback)
    {
        string title = "";
        string[] filters = null; 

        switch (type)
        {
            case LoadModelText:
                title = "选择模型文件";
                filters = new string[] { "MMD Model Files", "pmd,pmx", "All Files", "*" };
                break;
            case LoadMotionText:
                title = "选择动作文件";
                filters = new string[] { "VMD Motion Files", "vmd", "All Files", "*" };
                break;
            case LoadMusicText:
                title = "选择音乐文件";
                filters = new string[] { "Audio Files", "wav,mp3", "All Files", "*" };
                break;
            case LoadCameraText:
                title = "选择相机文件";
                filters = new string[] { "VMD Camera Files", "vmd", "All Files", "*" };
                break;
        }

        #if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanelWithFilters(title, "", filters);
        if (!string.IsNullOrEmpty(path))
        {
            callback(path);
        }
        #else
        Debug.LogWarning("File panel not implemented for runtime build. Please implement a runtime file browser.");
        // For non-editor builds, you'll need a runtime file browser solution.
        // This could be a Unity Asset Store package or a custom implementation.
        #endif
    }

    private void HandleLoadModel(string path)
    {
        if (string.IsNullOrEmpty(path) || MainSceneController.Instance == null) return;
        
        bool success = MainSceneController.Instance.AddModel(path);
        if (success)
        {
            if (!loadedModelPaths.Contains(path)) loadedModelPaths.Add(path);
            UpdateDropdownChoices(modelDropdown, LoadModelText, loadedModelPaths.ConvertAll(Path.GetFileName), Path.GetFileName(path));
            UpdateModelListDisplay(); // Update the ListView for models
            Debug.Log($"Model loaded: {path}");
        }
        else
        {
            Debug.LogError($"Failed to load model: {path}");
        }
    }
     private void HandleSelectModel(string path)
    {
        if (string.IsNullOrEmpty(path) || MainSceneController.Instance == null) return;
        int modelIndex = loadedModelPaths.IndexOf(path);
        if (modelIndex != -1)
        {
            MainSceneController.Instance.SelectModel(modelIndex);
            Debug.Log($"Selected model: {path}");
            // If a model is selected, update motion dropdown to reflect its current motion, or "Load Motion"
            string currentMotionPath = MainSceneController.Instance.GetCurrentMotionPath();
            if (!string.IsNullOrEmpty(currentMotionPath) && loadedMotionPaths.Contains(currentMotionPath))
            {
                 UpdateDropdownChoices(motionDropdown, LoadMotionText, loadedMotionPaths.ConvertAll(Path.GetFileName), Path.GetFileName(currentMotionPath));
            }
            else
            {
                 UpdateDropdownChoices(motionDropdown, LoadMotionText, loadedMotionPaths.ConvertAll(Path.GetFileName));
            }
        }
    }


    private void HandleLoadMotion(string path)
    {
        if (string.IsNullOrEmpty(path) || MainSceneController.Instance == null) return;

        if (MainSceneController.Instance.GetModelCount() > 0)
        {
            MainSceneController.Instance.ChangeCurrentMotion(path);
            if (!loadedMotionPaths.Contains(path)) loadedMotionPaths.Add(path);
            UpdateDropdownChoices(motionDropdown, LoadMotionText, loadedMotionPaths.ConvertAll(Path.GetFileName), Path.GetFileName(path));
            Debug.Log($"Motion loaded: {path}");
        }
        else Debug.LogWarning("No model loaded. Cannot load motion.");
    }
    private void HandleSelectMotion(string path) // This is effectively the same as loading for the current model
    {
        HandleLoadMotion(path);
    }


    private void HandleLoadMusic(string path)
    {
        if (string.IsNullOrEmpty(path) || MainSceneController.Instance == null) return;
        MainSceneController.Instance.ChangeMusic(path);
        if (!loadedMusicPaths.Contains(path)) loadedMusicPaths.Add(path);
        UpdateDropdownChoices(musicDropdown, LoadMusicText, loadedMusicPaths.ConvertAll(Path.GetFileName), Path.GetFileName(path));
        Debug.Log($"Music loaded: {path}");
    }
    private void HandleSelectMusic(string path) // This is effectively the same as loading
    {
        HandleLoadMusic(path);
    }

    private void HandleLoadCamera(string path)
    {
        if (string.IsNullOrEmpty(path) || MainSceneController.Instance == null) return;

        MainSceneController.Instance.CameraFilePath = path;
        if (MainSceneController.Instance.GetXrType() != XrType.CameraFile)
        {
            MainSceneController.Instance.ChangeXrType(XrType.CameraFile);
        }

        var xrController = MainSceneController.Instance.GetXrController();
        if (xrController is CameraFileController cameraFileController)
        {
            if (cameraFileController.CameraObject != null)
            {
                if (cameraFileController.CameraObject.LoadCameraMotion(path))
                {
                    if (!loadedCameraPaths.Contains(path)) loadedCameraPaths.Add(path);
                    UpdateDropdownChoices(cameraDropdown, LoadCameraText, loadedCameraPaths.ConvertAll(Path.GetFileName), Path.GetFileName(path));
                    Debug.Log($"Camera data loaded: {path}");
                }
                else Debug.LogError($"Failed to load camera motion from: {path}");
            }
            else Debug.LogError("CameraFileController.CameraObject is null.");
        }
        else Debug.LogError("Failed to get CameraFileController.");
    }
    private void HandleSelectCamera(string path) // This is effectively the same as loading
    {
        HandleLoadCamera(path);
    }


    private void UpdateDropdownChoices(DropdownField dropdown, string loadText, IList<string> itemNames, string currentSelectionName = null)
    {
        if (dropdown == null) return;
        var choices = new List<string> { loadText };
        if (itemNames != null) choices.AddRange(itemNames);
        dropdown.choices = choices;

        if (!string.IsNullOrEmpty(currentSelectionName) && choices.Contains(currentSelectionName))
        {
            dropdown.SetValueWithoutNotify(currentSelectionName);
        }
        else if (dropdown.choices.Count > 0)
        {
             // If current selection is not valid, try to set to the first actual item, or "Load X" if no items
            dropdown.SetValueWithoutNotify(dropdown.choices.Count > 1 ? dropdown.choices[1] : loadText);
        }
    }
    
    private void UpdateModelListDisplay()
    {
        if (modelListDisplay == null || MainSceneController.Instance == null) return;

        var modelNames = MainSceneController.Instance.GetModelNames();
        modelListDisplay.itemsSource = (System.Collections.IList)modelNames;
        modelListDisplay.makeItem = () => new Label();
        modelListDisplay.bindItem = (element, i) => (element as Label).text = modelNames[i];
        
        // Clear old selection if any, or handle selection persistence if needed
        modelListDisplay.ClearSelection();
        modelListDisplay.onSelectionChange += OnModelListSelectionChanged;
        modelListDisplay.Rebuild(); // Refresh the ListView
    }

    private void OnModelListSelectionChanged(IEnumerable<object> selectedItems)
    {
        var selectedModelName = selectedItems.FirstOrDefault() as string;
        if (!string.IsNullOrEmpty(selectedModelName) && MainSceneController.Instance != null)
        {
            var modelNames = MainSceneController.Instance.GetModelNames();
            int modelIndex = modelNames.IndexOf(selectedModelName);
            if (modelIndex != -1)
            {
                MainSceneController.Instance.SelectModel(modelIndex);
                // Update the main model dropdown to reflect this selection
                string selectedModelPath = loadedModelPaths[modelIndex]; // Assuming loadedModelPaths is in sync
                UpdateDropdownChoices(modelDropdown, LoadModelText, loadedModelPaths.ConvertAll(Path.GetFileName), Path.GetFileName(selectedModelPath));
                Debug.Log($"Model selected from list: {selectedModelName}");
            }
        }
    }


    private void ToggleLeftPanel()
    {
        if (leftPanel != null)
        {
            bool isActive = leftPanel.style.display == DisplayStyle.Flex;
            leftPanel.style.display = isActive ? DisplayStyle.None : DisplayStyle.Flex;
            if (!isActive) UpdateModelListDisplay(); // Update list when showing
        }
    }

    private void ToggleRightPanel()
    {
        if (rightPanel != null)
        {
            rightPanel.style.display = rightPanel.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void OnPlayClicked()
    {
        if (MainSceneController.Instance != null) MainSceneController.Instance.SwitchPlayPause(true);
    }

    private void OnPauseClicked()
    {
        if (MainSceneController.Instance != null) MainSceneController.Instance.SwitchPlayPause(false);
    }
    
    private void OnStopClicked()
    {
        if (MainSceneController.Instance != null)
        {
            MainSceneController.Instance.ResetAll(); 
            // SwitchPlayPause(false) is called within ResetAll or by its event
        }
    }

    private void RegisterMainSceneControllerEvents()
    {
        if (MainSceneController.Instance == null) return;
        MainSceneController.Instance.OnPlayPause.AddListener(HandlePlayPauseEvent);
        MainSceneController.Instance.OnResetAll.AddListener(HandleResetAllEvent);
        // Add other event listeners as needed (e.g., for time updates)
    }

    private void UnregisterMainSceneControllerEvents()
    {
        if (MainSceneController.Instance == null) return;
        MainSceneController.Instance.OnPlayPause.RemoveListener(HandlePlayPauseEvent);
        MainSceneController.Instance.OnResetAll.RemoveListener(HandleResetAllEvent);
    }

    private void HandlePlayPauseEvent(bool isPlaying)
    {
        if (playButton != null) playButton.style.display = isPlaying ? DisplayStyle.None : DisplayStyle.Flex;
        if (pauseButton != null) pauseButton.style.display = isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void HandleResetAllEvent()
    {
        if (timelineSlider != null) timelineSlider.value = 0;
        if (currentTimeLabel != null) currentTimeLabel.text = "00:00";
        // Ensure play/pause buttons are in correct state (usually paused after reset)
        if (playButton != null) playButton.style.display = DisplayStyle.Flex;
        if (pauseButton != null) pauseButton.style.display = DisplayStyle.None;
    }
    
    // --- Settings Logic ---
    private void LoadSettings()
    {
        // Load settings from PlayerPrefs or a config file and apply them to UI controls
        if (qualityLevelSlider != null) qualityLevelSlider.value = PlayerPrefs.GetInt("QualityLevel", 2);
        if (antiAliasingSlider != null) antiAliasingSlider.value = PlayerPrefs.GetInt("AntiAliasing", 2);
        if (shadowQualityDropdown != null) shadowQualityDropdown.index = PlayerPrefs.GetInt("ShadowQuality", 1);
        // ... and so on for other settings
        // Apply these settings immediately
        ApplySettings();
    }

    private void ApplySettings()
    {
        if (MainSceneController.Instance == null) return;

        // Apply settings from UI controls to the game and save them
        if (qualityLevelSlider != null) PlayerPrefs.SetInt("QualityLevel", qualityLevelSlider.value);
        // Example: QualitySettings.SetQualityLevel(qualityLevelSlider.value, true);

        if (antiAliasingSlider != null) PlayerPrefs.SetInt("AntiAliasing", antiAliasingSlider.value);
        // Example: QualitySettings.antiAliasing = antiAliasingSlider.value * 2; (0, 2, 4, 8)

        if (shadowQualityDropdown != null) PlayerPrefs.SetInt("ShadowQuality", shadowQualityDropdown.index);
        // Example: Convert index to ShadowQuality enum and apply

        // Apply to MMDObject specific settings if needed, e.g., self-shadow
        // This might require iterating through MMDObjects or having MainSceneController handle it.
        // var config = mmdGameObject.GetConfig();
        // config.EnableDrawSelfShadow = ...
        // mmdGameObject.UpdateConfig(config);

        PlayerPrefs.Save();
        Debug.Log("Settings applied and saved.");
        
        // You might need to call methods in MainSceneController or other components
        // to make some settings take effect immediately.
    }
    
    private void ResetAndApplyDefaultSettings()
    {
        // Reset UI controls to default values
        if (qualityLevelSlider != null) qualityLevelSlider.value = 2;
        if (antiAliasingSlider != null) antiAliasingSlider.value = 2;
        if (shadowQualityDropdown != null) shadowQualityDropdown.index = 1;
        // ... reset other settings ...
        
        // Apply these default settings
        ApplySettings();
        Debug.Log("Settings reset to defaults and applied.");
    }

    void Update()
    {
        // Update timeline slider and labels if playing
        if (MainSceneController.Instance != null && MainSceneController.Instance.IsPlaying)
        {
            AudioSource audioSource = MainSceneController.Instance.GetComponent<AudioSource>(); // Assuming AudioSource is on MainSceneController
            if (audioSource != null && audioSource.clip != null)
            {
                if (timelineSlider != null)
                {
                    timelineSlider.highValue = audioSource.clip.length;
                    timelineSlider.value = audioSource.time;
                }
                if (currentTimeLabel != null) currentTimeLabel.text = FormatTime(audioSource.time);
                if (totalTimeLabel != null) totalTimeLabel.text = FormatTime(audioSource.clip.length);
            }
        }
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = (int)timeInSeconds / 60;
        int seconds = (int)timeInSeconds % 60;
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
