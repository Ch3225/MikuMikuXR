using SimpleFileBrowser;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MMDVR.Managers;

public class LoadFolderButton : MonoBehaviour
{
    [Header("UI区域切换")]
    [SerializeField] private GameObject sideMenuPanel;
    [SerializeField] private GameObject mainControlPanel;
    [SerializeField] private GameObject simpleFileBrowserPanel; // Panels下的FileBrowser实例
    [Header("文件类型设置")]
    [SerializeField] private string[] fileExtensions = { ".pmx", ".pmd", ".vmd", ".mp3", ".wav", ".ogg" };
    [SerializeField] private int maxFileCount = 20;
    [Header("路径缓存Key")]
    [SerializeField] private string prefsKey = "LoadFolder_LastPath";

    private Button button;
    private string lastPath = "";
    private FileBrowser fileBrowserInstance;
    private Coroutine waitCoroutine;

    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnButtonClicked);
        lastPath = PlayerPrefs.GetString(prefsKey, Application.dataPath);
        if (simpleFileBrowserPanel != null)
            fileBrowserInstance = simpleFileBrowserPanel.GetComponent<FileBrowser>();
    }

    private void ShowFileBrowserPanel()
    {
        if (simpleFileBrowserPanel != null)
        {
            simpleFileBrowserPanel.SetActive(true);
             /*
            // 强制重设自身RectTransform为Stretch并铺满父节点，并设置合适宽高
            var rt = simpleFileBrowserPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1); // 左侧竖直拉伸
            rt.pivot = new Vector2(0, 1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(800, 700); // 你可以自定义宽高
            */
            // 强制重设SimpleFileBrowserWindow的RectTransform为左上角对齐+固定宽高
            var window = simpleFileBrowserPanel.transform.Find("SimpleFileBrowserWindow");
            if (window != null)
            {
                /*
                var winRt = window.GetComponent<RectTransform>();
                winRt.anchorMin = new Vector2(0, 1);
                winRt.anchorMax = new Vector2(1, 1);
                winRt.pivot = new Vector2(0, 1);
                winRt.anchoredPosition = Vector2.zero;
                winRt.sizeDelta = new Vector2(0, 0); // 跟随父节点宽高
                */
            }
        }
    }

    private void OnButtonClicked()
    {
        if (sideMenuPanel != null) sideMenuPanel.SetActive(false);
        if (mainControlPanel != null) mainControlPanel.SetActive(false);
        ShowFileBrowserPanel();

        // 文件夹选择模式不需要设置文件过滤器
        // FileBrowser.SetFilters(true, new FileBrowser.Filter("资源", fileExtensions));
        // FileBrowser.SetDefaultFilter(fileExtensions.Length > 0 ? fileExtensions[0] : "");
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");

        // 使用 ShowLoadDialog 并设置 pickMode 为 Folders 来选择文件夹
        FileBrowser.ShowLoadDialog(
            (paths) => { // 成功回调
                if (paths != null && paths.Length > 0)
                {
                    string folderPath = paths[0];
                    if (!string.IsNullOrEmpty(folderPath) && System.IO.Directory.Exists(folderPath))
                    {
                        lastPath = folderPath;
                        PlayerPrefs.SetString(prefsKey, lastPath);
                        LoadResourcesFromFolder(folderPath);
                    }
                }
                RestoreMainPanel();
            },
            () => { // 取消回调
                RestoreMainPanel();
            },
            pickMode: FileBrowser.PickMode.Folders, // 设置为文件夹选择模式
            allowMultiSelection: false, // 不允许多选
            initialPath: lastPath, // 初始路径
            title: "Select Folder", // 标题
            loadButtonText: "Select" // 选择按钮文本
        );
    }

    private void LoadResourcesFromFolder(string folderPath)
    {
        Debug.Log($"开始从文件夹加载资源: {folderPath}");
        var filesToLoad = new List<string>();
        foreach (var ext in fileExtensions)
        {
            filesToLoad.AddRange(System.IO.Directory.GetFiles(folderPath, "*" + ext, System.IO.SearchOption.TopDirectoryOnly));
        }

        if (filesToLoad.Count > maxFileCount)
        {
            Debug.LogWarning($"文件数量({filesToLoad.Count})超出上限({maxFileCount})，仅加载前{maxFileCount}个。");
            filesToLoad = filesToLoad.GetRange(0, maxFileCount);
        }

        foreach (var filePath in filesToLoad)
        {
            string extension = System.IO.Path.GetExtension(filePath).ToLower();
            string fileName = System.IO.Path.GetFileName(filePath).ToLower();

            switch (extension)
            {
                case ".pmx":
                case ".pmd":
                    Debug.Log($"加载模型: {filePath}");
                    SceneStatesManager.Instance?.AddActor(filePath);
                    break;
                case ".vmd":
                    {
                        // 使用新方法判断VMD文件类型
                        var vmdType = GetVmdFileType(filePath);
                        if (vmdType == VmdType.Camera)
                        {
                            Debug.Log($"加载VMD相机: {filePath}");
                            SceneStatesManager.Instance?.AddVMDCamera(filePath);
                        }
                        else if (vmdType == VmdType.Motion)
                        {
                            Debug.Log($"加载VMD动作: {filePath}");
                            SceneStatesManager.Instance?.AddMotion(filePath);
                        }
                        else
                        {
                            Debug.LogWarning($"无法识别的VMD文件类型或文件损坏: {filePath}");
                        }
                    }
                    break;
                case ".mp3":
                case ".wav":
                case ".ogg":
                    Debug.Log($"加载音乐: {filePath}");
                    SceneStatesManager.Instance?.AddMusic(filePath);
                    break;
            }
        }
        Debug.Log("文件夹资源加载完成。");
    }

    private enum VmdType { Motion, Camera, Unknown }

    private VmdType GetVmdFileType(string filePath)
    {
        try
        {
            using (var reader = new System.IO.BinaryReader(System.IO.File.OpenRead(filePath)))
            {
                if (reader.BaseStream.Length < 54) return VmdType.Unknown; // 文件太小，无法判断

                // 1. 读取并验证Header (30 bytes)
                string header = new string(reader.ReadChars(30));
                if (!header.StartsWith("Vocaloid Motion Data"))
                {
                    return VmdType.Unknown;
                }

                // 2. 跳过ModelName (20 bytes)
                reader.ReadBytes(20);

                // 3. 读取MotionCount (4 bytes, uint)
                uint motionCount = reader.ReadUInt32();
                if (motionCount > 0)
                {
                    return VmdType.Motion;
                }

                // 4. 如果MotionCount为0，需要计算MorphCount并跳过，再读取CameraCount
                //    读取MorphCount (4 bytes, uint)
                uint morphCount = reader.ReadUInt32();
                if (morphCount > 0)
                {
                    return VmdType.Motion; // 包含表情数据，也认为是动作文件
                }

                // 5. 跳过空的Morph数据块 (0 bytes since count is 0)
                //    读取CameraCount (4 bytes, uint)
                uint cameraCount = reader.ReadUInt32();
                if (cameraCount > 0)
                {
                    return VmdType.Camera;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"判断VMD文件类型时出错 '{filePath}': {ex.Message}");
        }

        return VmdType.Unknown; // 默认或发生错误时
    }

    private void RestoreMainPanel()
    {
        if (sideMenuPanel != null) sideMenuPanel.SetActive(true);
        if (mainControlPanel != null) mainControlPanel.SetActive(true);
        if (simpleFileBrowserPanel != null) simpleFileBrowserPanel.SetActive(false);
    }
}