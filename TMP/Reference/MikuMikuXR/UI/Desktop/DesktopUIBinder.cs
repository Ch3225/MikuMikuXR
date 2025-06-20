using UnityEngine;
using UnityEngine.UIElements;
using MikuMikuXR.SceneController;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using TinyTeam.UI;
using MikuMikuXR.UI.Page;
using MikuMikuXR.XR;

// If MmdFileSelector is in a namespace and accessible:
// using MikuMikuXR.UI.Page; 

// If MmdFileSelector.FileType is not directly accessible, define it locally or ensure the namespace is correct.
// For this example, we'll assume it's available or define a similar one if needed.
// Let's assume MikuMikuXR.UI.Page.MmdFileSelector.FileType is accessible.
// If not, you might need to define:
namespace MikuMikuXR.UI.Desktop
{
    public enum FileTypeForSelector
    {
        Model,
        Motion,
        Music,
        Camera // Assuming VMD for camera is treated like motion for selection
    }
}
// Using a placeholder for the file type enum if the original is not accessible.
// Replace with actual MikuMikuXR.UI.Page.MmdFileSelector.FileType if available.
public enum DesktopFileSelectorType
{
    Model,
    Motion,
    Music,
    Camera
}


public class DesktopUIBinder : MonoBehaviour
{
    private VisualElement rootElement;
    private DropdownField modelDropdown;
    private DropdownField motionDropdown;
    private DropdownField musicDropdown;
    private DropdownField cameraDropdown;

    // Delegate for file selection callback
    public delegate void FileSelectedCallback(string filePath);

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component not found on this GameObject!");
            this.enabled = false;
            return;
        }
        rootElement = uiDocument.rootVisualElement;

        if (MainSceneController.Instance == null)
        {
            Debug.LogError("MainSceneController.Instance is not yet available. DesktopUIBinder might initialize too early.");
            // Optionally, retry later or ensure MainSceneController initializes first.
            // For now, we'll proceed, but this could be an issue.
        }

        // Get UI elements
        modelDropdown = rootElement.Q<DropdownField>("ModelDropdown");
        motionDropdown = rootElement.Q<DropdownField>("MotionDropdown");
        musicDropdown = rootElement.Q<DropdownField>("MusicDropdown");
        cameraDropdown = rootElement.Q<DropdownField>("CameraDropdown");

        if (modelDropdown == null || motionDropdown == null || musicDropdown == null || cameraDropdown == null)
        {
            Debug.LogError("One or more DropdownFields not found in MainUI.uxml. Check their names.");
            return;
        }

        // Setup initial state and callbacks
        SetupModelDropdown();
        SetupMotionDropdown();
        SetupMusicDropdown();
        SetupCameraDropdown();
    }

    #region Model
    private void SetupModelDropdown()
    {
        UpdateModelDropdownChoices(); // Initial population
        modelDropdown.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == "加载模型")
            {
                // Check if the dropdown itself has focus to prevent re-triggering from code
                if (modelDropdown.focusController?.focusedElement == modelDropdown || IsManuallyTriggered(modelDropdown))
                {
                    OpenFileSelector(DesktopFileSelectorType.Model, "选择模型 (PMD, PMX)", filePath =>
                    {
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            Debug.Log($"尝试加载模型: {filePath}");
                            // Proper loading UI should be displayed here
                            try
                            {
                                if (MainSceneController.Instance.AddModel(filePath))
                                {
                                    Debug.Log("模型加载成功。");
                                    // AddModel internally calls SetSelectedModelIndex.
                                    // The GetModelNames will include the new model.
                                    string modelName = Path.GetFileNameWithoutExtension(filePath);
                                    UpdateModelDropdownChoices(modelName);
                                }
                                else
                                {
                                    Debug.LogError("加载模型失败。请确认模型存在且为正确的MikuMikuDance模型。");
                                    modelDropdown.SetValueWithoutNotify("加载模型"); // Reset
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                                Debug.LogError($"加载模型时发生错误: {e.Message}");
                                modelDropdown.SetValueWithoutNotify("加载模型"); // Reset
                            }
                            // Proper loading UI should be hidden here
                        }
                        else
                        {
                            // User cancelled dialog, reset to a sensible default if needed
                            ResetDropdownToLoadOption(modelDropdown, "加载模型");
                        }
                    });
                }
            }
            else
            {
                // User selected an existing model
                int modelIndex = modelDropdown.choices.IndexOf(evt.newValue) - 1; // Adjust for "加载模型"
                if (modelIndex >= 0 && MainSceneController.Instance != null && modelIndex < MainSceneController.Instance.GetModelCount())
                {
                    MainSceneController.Instance.SelectModel(modelIndex);
                }
            }
        });
    }

    private void UpdateModelDropdownChoices(string newlyLoadedModelName = null)
    {
        if (MainSceneController.Instance == null) return;

        List<string> choices = new List<string> { "加载模型" };
        var modelNames = MainSceneController.Instance.GetModelNames();
        if (modelNames != null)
        {
            choices.AddRange(modelNames);
        }
        modelDropdown.choices = choices;

        if (!string.IsNullOrEmpty(newlyLoadedModelName) && choices.Contains(newlyLoadedModelName))
        {
            modelDropdown.SetValueWithoutNotify(newlyLoadedModelName);
        }
        else if (choices.Count > 1)
        {
            // If no specific model to select, try to select the current one from MainSceneController or first one
            // This part might need more sophisticated logic based on MainSceneController's current selected model state
            var currentModelPath = MainSceneController.Instance.GetCurrentModelPath();
            if (!string.IsNullOrEmpty(currentModelPath)) {
                string currentModelName = Path.GetFileNameWithoutExtension(currentModelPath);
                if (choices.Contains(currentModelName)) {
                    modelDropdown.SetValueWithoutNotify(currentModelName);
                    return;
                }
            }
            modelDropdown.SetValueWithoutNotify(choices[1]); // Select the first actual model if any
        }
        else
        {
            modelDropdown.SetValueWithoutNotify("加载模型");
        }
    }
    #endregion

    #region Motion
    private void SetupMotionDropdown()
    {
        UpdateDropdownState(motionDropdown, "加载动作", MainSceneController.Instance?.GetCurrentMotionPath());
        motionDropdown.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == "加载动作")
            {
                if (motionDropdown.focusController?.focusedElement == motionDropdown || IsManuallyTriggered(motionDropdown))
                {
                    OpenFileSelector(DesktopFileSelectorType.Motion, "选择动作 (VMD)", filePath =>
                    {
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            Debug.Log($"尝试加载动作: {filePath}");
                            try
                            {
                                MainSceneController.Instance.ChangeCurrentMotion(filePath);
                                Debug.Log("动作加载成功。");
                                UpdateDropdownState(motionDropdown, "加载动作", filePath);
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                                Debug.LogError($"加载动作时发生错误: {e.Message}");
                                ResetDropdownToLoadOption(motionDropdown, "加载动作");
                            }
                        }
                        else
                        {
                            ResetDropdownToLoadOption(motionDropdown, "加载动作");
                        }
                    });
                }
            }
            // No 'else' needed, as this dropdown primarily loads new motions.
            // If you want to switch between a list of pre-loaded motions, this needs more logic.
        });
    }
    #endregion

    #region Music
    private void SetupMusicDropdown()
    {
        // MainSceneController doesn't have a GetCurrentMusicPath, so we can't pre-fill easily
        // We'll assume it starts with "加载音乐"
        UpdateDropdownState(musicDropdown, "加载音乐", null); // No easy way to get current music path initially
        musicDropdown.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == "加载音乐")
            {
                 if (musicDropdown.focusController?.focusedElement == musicDropdown || IsManuallyTriggered(musicDropdown))
                {
                    OpenFileSelector(DesktopFileSelectorType.Music, "选择音乐 (WAV, MP3)", filePath =>
                    {
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            Debug.Log($"尝试加载音乐: {filePath}");
                            try
                            {
                                MainSceneController.Instance.ChangeMusic(filePath);
                                Debug.Log("音乐加载成功。");
                                UpdateDropdownState(musicDropdown, "加载音乐", filePath);
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                                Debug.LogError($"加载音乐时发生错误: {e.Message}");
                                ResetDropdownToLoadOption(musicDropdown, "加载音乐");
                            }
                        }
                        else
                        {
                           ResetDropdownToLoadOption(musicDropdown, "加载音乐");
                        }
                    });
                }
            }
        });
    }
    #endregion

    #region Camera
    private void SetupCameraDropdown()
    {
        UpdateDropdownState(cameraDropdown, "加载相机", MainSceneController.Instance?.CameraFilePath);
        cameraDropdown.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == "加载相机")
            {
                if (cameraDropdown.focusController?.focusedElement == cameraDropdown || IsManuallyTriggered(cameraDropdown))
                {
                    OpenFileSelector(DesktopFileSelectorType.Camera, "选择相机 (VMD)", filePath =>
                    {
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            Debug.Log($"尝试加载相机: {filePath}");
                            try
                            {
                                // IMPORTANT: Change XR type for camera file controller
                                MainSceneController.Instance.ChangeXrType(XrType.CameraFile);
                                MainSceneController.Instance.CameraFilePath = filePath;
                                var cameraFileController = MainSceneController.Instance.GetXrController() as CameraFileController;
                                if (cameraFileController != null)
                                {
                                    if (cameraFileController.CameraObject.LoadCameraMotion(filePath))
                                    {
                                        Debug.Log("相机数据加载成功。");
                                        UpdateDropdownState(cameraDropdown, "加载相机", filePath);
                                    }
                                    else
                                    {
                                        Debug.LogError("加载的文件中不含镜头数据或加载失败。");
                                        ResetDropdownToLoadOption(cameraDropdown, "加载相机");
                                    }
                                }
                                else
                                {
                                    Debug.LogError("相机控制器 (CameraFileController) 未找到或类型不匹配。确保XR模式已正确切换。");
                                    ResetDropdownToLoadOption(cameraDropdown, "加载相机");
                                }
                                MainSceneController.Instance.ResetAll(); // Reset playback after loading new camera
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                                Debug.LogError($"加载相机时发生错误: {e.Message}");
                                ResetDropdownToLoadOption(cameraDropdown, "加载相机");
                            }
                        }
                        else
                        {
                            ResetDropdownToLoadOption(cameraDropdown, "加载相机");
                        }
                    });
                }
            }
        });
    }
    #endregion

    #region Helper Methods
    // Helper to update dropdowns for Motion, Music, Camera (single item + load option)
    private void UpdateDropdownState(DropdownField dropdown, string loadOptionText, string currentItemPath)
    {
        List<string> choices = new List<string> { loadOptionText };
        string currentItemName = null;

        if (!string.IsNullOrEmpty(currentItemPath))
        {
            currentItemName = Path.GetFileNameWithoutExtension(currentItemPath);
            if (currentItemName != loadOptionText) // Avoid adding loadOptionText twice
            {
                choices.Add(currentItemName);
            }
        }
        dropdown.choices = choices;

        if (!string.IsNullOrEmpty(currentItemName) && currentItemName != loadOptionText)
        {
            dropdown.SetValueWithoutNotify(currentItemName);
        }
        else
        {
            dropdown.SetValueWithoutNotify(loadOptionText);
        }
    }
    
    private void ResetDropdownToLoadOption(DropdownField dropdown, string loadOptionText)
    {
        // If the current value is already the load option, do nothing.
        // Otherwise, set it. If other items exist, it might be better to select the previously selected valid item.
        // For simplicity, we reset to the load option or the first available actual item.
        if (dropdown.choices.Count > 1 && dropdown.choices[0] == loadOptionText) {
             // Check if there's an actual item other than load option
            bool actualItemExists = dropdown.choices.Any(c => c != loadOptionText);
            if (actualItemExists && dropdown.value != loadOptionText) {
                // Keep current value if it's an actual item
            } else {
                 dropdown.SetValueWithoutNotify(loadOptionText);
            }
        } else {
            dropdown.SetValueWithoutNotify(loadOptionText);
        }
    }

    // This is a simple way to check if the event might be from actual user interaction
    // rather than programmatic changes, especially after a dialog.
    // A more robust solution might involve tracking dialog states.
    private bool IsManuallyTriggered(DropdownField dropdown)
    {
        // If a file dialog was just shown, the focus might be lost and then regained.
        // This is a heuristic. A more reliable method would be to manage state explicitly.
        return true; // For now, assume it's manual if the callback is hit with "加载..."
    }

    #endregion

    #region File Selector Placeholder
    // 替换文件选择器为内嵌UI弹窗
    private void OpenFileSelector(DesktopFileSelectorType fileType, string title, FileSelectedCallback onFileSelect)
    {
        // 映射到MmdFileSelector的FileType
        MmdFileSelector.FileType selectorType = MmdFileSelector.FileType.Model;
        switch (fileType)
        {
            case DesktopFileSelectorType.Model:
                selectorType = MmdFileSelector.FileType.Model;
                break;
            case DesktopFileSelectorType.Motion:
                selectorType = MmdFileSelector.FileType.Motion;
                break;
            case DesktopFileSelectorType.Music:
                selectorType = MmdFileSelector.FileType.Music;
                break;
            case DesktopFileSelectorType.Camera:
                selectorType = MmdFileSelector.FileType.Motion; // 相机用VMD，和动作一致
                break;
        }
        // 弹出MmdFileSelector页面
        TTUIPage.ShowPage<MmdFileSelector>(new MmdFileSelector.Context
        {
            Type = selectorType,
            Title = title,
            PathMode = true,
            OnFileSelect = path =>
            {
                // 关闭文件选择器后回调
                onFileSelect?.Invoke(path);
            }
        });
    }
    #endregion
}
