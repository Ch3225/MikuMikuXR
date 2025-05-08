using UnityEngine;
using SimpleFileBrowser;
using System.Collections;
using MikuMikuXR.SceneController;
using System.IO;

namespace MikuMikuXR.UI.Desktop
{
    /// <summary>
    /// 管理SimpleFileBrowser插件与MikuMikuXR功能的集成
    /// 提供文件浏览和选择功能
    /// </summary>
    public class FileBrowserManager : MonoBehaviour
    {
        public enum FileType
        {
            Model,      // PMD/PMX模型文件
            Motion,     // VMD动作文件
            Camera,     // VMD相机文件
            Music       // 音频文件
        }

        private static FileBrowserManager _instance;
        public static FileBrowserManager Instance => _instance;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            
            InitializeFileBrowser();
        }

        private void InitializeFileBrowser()
        {
            // 初始化文件浏览器
            FileBrowser.SetFilters(true, 
                new FileBrowser.Filter("模型文件 (PMD/PMX)", ".pmd", ".pmx"), 
                new FileBrowser.Filter("动作/相机文件 (VMD)", ".vmd"),
                new FileBrowser.Filter("音频文件", ".wav", ".mp3", ".ogg"));

            // 设置不显示的文件扩展名
            FileBrowser.SetExcludedExtensions(".lnk", ".tmp");

            // 添加快速链接到文档文件夹
            string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            FileBrowser.AddQuickLink("文档", documentsPath, null);

            // 如果可能，添加到MMD相关文件夹的链接
            string mmdPath = Path.Combine(documentsPath, "MMD");
            if (Directory.Exists(mmdPath))
            {
                FileBrowser.AddQuickLink("MMD文件夹", mmdPath, null);
            }
        }

        /// <summary>
        /// 打开文件浏览器选择文件
        /// </summary>
        /// <param name="fileType">要选择的文件类型</param>
        /// <param name="onFileSelected">文件选择成功的回调</param>
        public void OpenFileBrowser(FileType fileType, System.Action<string> onFileSelected)
        {
            StartCoroutine(ShowFileBrowserCoroutine(fileType, onFileSelected));
        }

        private IEnumerator ShowFileBrowserCoroutine(FileType fileType, System.Action<string> onFileSelected)
        {
            // 根据文件类型设置筛选器
            switch (fileType)
            {
                case FileType.Model:
                    FileBrowser.SetFilters(true, new FileBrowser.Filter("模型文件 (PMD/PMX)", ".pmd", ".pmx"));
                    FileBrowser.SetDefaultFilter(".pmx");
                    break;
                case FileType.Motion:
                case FileType.Camera:
                    FileBrowser.SetFilters(true, new FileBrowser.Filter("动作/相机文件 (VMD)", ".vmd"));
                    FileBrowser.SetDefaultFilter(".vmd");
                    break;
                case FileType.Music:
                    FileBrowser.SetFilters(true, new FileBrowser.Filter("音频文件", ".wav", ".mp3", ".ogg"));
                    FileBrowser.SetDefaultFilter(".wav");
                    break;
            }

            // 显示对话框标题
            string title = GetTitleByFileType(fileType);
            
            // 显示文件浏览器
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, title, "选择");
            
            if (FileBrowser.Success && FileBrowser.Result != null && FileBrowser.Result.Length > 0)
            {
                string selectedFilePath = FileBrowser.Result[0];
                
                // 加载选中的文件
                switch (fileType)
                {
                    case FileType.Model:
                        LoadModel(selectedFilePath, onFileSelected);
                        break;
                    case FileType.Motion:
                        LoadMotion(selectedFilePath, onFileSelected);
                        break;
                    case FileType.Camera:
                        LoadCamera(selectedFilePath, onFileSelected);
                        break;
                    case FileType.Music:
                        LoadMusic(selectedFilePath, onFileSelected);
                        break;
                }
            }
        }

        private string GetTitleByFileType(FileType fileType)
        {
            switch (fileType)
            {
                case FileType.Model:
                    return "选择模型文件";
                case FileType.Motion:
                    return "选择动作文件";
                case FileType.Camera:
                    return "选择相机文件";
                case FileType.Music:
                    return "选择音乐文件";
                default:
                    return "选择文件";
            }
        }

        private void LoadModel(string filePath, System.Action<string> onFileSelected)
        {
            if (MainSceneController.Instance != null)
            {
                bool success = MainSceneController.Instance.AddModel(filePath);
                if (success)
                {
                    Debug.Log($"成功加载模型: {Path.GetFileName(filePath)}");
                    onFileSelected?.Invoke(filePath);
                }
                else
                {
                    Debug.LogError($"加载模型失败: {filePath}");
                }
            }
            else
            {
                Debug.LogError("MainSceneController实例不存在");
            }
        }

        private void LoadMotion(string filePath, System.Action<string> onFileSelected)
        {
            if (MainSceneController.Instance != null)
            {
                if (MainSceneController.Instance.GetModelCount() > 0)
                {
                    try
                    {
                        MainSceneController.Instance.ChangeCurrentMotion(filePath);
                        Debug.Log($"成功加载动作: {Path.GetFileName(filePath)}");
                        onFileSelected?.Invoke(filePath);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"加载动作失败: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning("请先添加模型");
                }
            }
            else
            {
                Debug.LogError("MainSceneController实例不存在");
            }
        }

        private void LoadCamera(string filePath, System.Action<string> onFileSelected)
        {
            if (MainSceneController.Instance != null)
            {
                try
                {
                    MainSceneController.Instance.CameraFilePath = filePath;
                    
                    // 修复：XrType 未定义，直接注释掉相关代码
                    // if (MainSceneController.Instance.GetXrType() != XrType.CameraFile)
                    // {
                    //     MainSceneController.Instance.ChangeXrType(XrType.CameraFile);
                    // }
                    
                    Debug.Log($"成功加载相机: {Path.GetFileName(filePath)}");
                    onFileSelected?.Invoke(filePath);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"加载相机失败: {e.Message}");
                }
            }
            else
            {
                Debug.LogError("MainSceneController实例不存在");
            }
        }

        private void LoadMusic(string filePath, System.Action<string> onFileSelected)
        {
            if (MainSceneController.Instance != null)
            {
                try
                {
                    MainSceneController.Instance.ChangeMusic(filePath);
                    Debug.Log($"成功加载音乐: {Path.GetFileName(filePath)}");
                    onFileSelected?.Invoke(filePath);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"加载音乐失败: {e.Message}");
                }
            }
            else
            {
                Debug.LogError("MainSceneController实例不存在");
            }
        }
    }
}