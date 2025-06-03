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

        FileBrowser.SetFilters(true, new FileBrowser.Filter("资源", fileExtensions));
        FileBrowser.SetDefaultFilter(fileExtensions.Length > 0 ? fileExtensions[0] : "");
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");

        // 使用新版API，直接用回调方式处理成功和取消
        FileBrowser.ShowLoadDialog(
            (paths) => {
                if (paths != null && paths.Length > 0)
                {
                    string folderPath = paths[0];
                    lastPath = folderPath;
                    PlayerPrefs.SetString(prefsKey, lastPath);
                    var files = new List<string>();
                    foreach (var ext in fileExtensions)
                    {
                        files.AddRange(System.IO.Directory.GetFiles(folderPath, "*" + ext, System.IO.SearchOption.TopDirectoryOnly));
                    }
                    if (files.Count > maxFileCount)
                    {
                        Debug.LogWarning($"文件数量超出上限({maxFileCount})，仅加载前{maxFileCount}个。");
                        files = files.GetRange(0, maxFileCount);
                    }
                    // TODO: 这里可以调用 MotionManager/ActorManager 进行批量加载
                }
                RestoreMainPanel();
            },
            () => {
                RestoreMainPanel();
            },
            FileBrowser.PickMode.Folders,
            false,
            lastPath
        );
    }

    private void RestoreMainPanel()
    {
        if (sideMenuPanel != null) sideMenuPanel.SetActive(true);
        if (mainControlPanel != null) mainControlPanel.SetActive(true);
        if (simpleFileBrowserPanel != null) simpleFileBrowserPanel.SetActive(false);
    }
}