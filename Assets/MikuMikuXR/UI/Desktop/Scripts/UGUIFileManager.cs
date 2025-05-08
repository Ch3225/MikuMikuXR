using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;
using MikuMikuXR.SceneController;
using MikuMikuXR.Utils;
using TMPro;

namespace MikuMikuXR.UI.Desktop 
{
    /// <summary>
    /// 负责处理UGUI界面的文件选择和加载操作
    /// 集成SimpleFileBrowser插件处理文件浏览功能
    /// </summary>
    public class UGUIFileManager : MonoBehaviour 
    {
        [Header("UI References")]
        [SerializeField] private TMP_Dropdown modelDropdown;
        [SerializeField] private TMP_Dropdown motionDropdown;
        [SerializeField] private TMP_Dropdown cameraDropdown;
        [SerializeField] private TMP_Dropdown musicDropdown;

        private MainSceneController _mainSceneController;

        // 文件过滤器定义
        private readonly FileBrowser.Filter _modelFilter = new FileBrowser.Filter("模型文件", ".pmd", ".pmx");
        private readonly FileBrowser.Filter _motionFilter = new FileBrowser.Filter("动作文件", ".vmd");
        private readonly FileBrowser.Filter _cameraFilter = new FileBrowser.Filter("相机文件", ".vmd");
        private readonly FileBrowser.Filter _musicFilter = new FileBrowser.Filter("音乐文件", ".wav", ".mp3", ".ogg");
        
        // 文件类型枚举
        private enum FileType 
        {
            Model,
            Motion,
            Camera,
            Music
        }

        private void Awake() 
        {
            _mainSceneController = FindObjectOfType<MainSceneController>();
            if (_mainSceneController == null) 
            {
                Debug.LogError("没有找到MainSceneController，文件加载功能将不可用！");
            }
            
            InitFileBrowser();
        }

        private void Start()
        {
            // 初始化下拉菜单
            SetupDropdowns();
            
            // 更新模型列表
            UpdateModelList();
        }

        /// <summary>
        /// 初始化文件浏览器
        /// </summary>
        private void InitFileBrowser() 
        {
            // 设置快捷链接
            FileBrowser.AddQuickLink("桌面", System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), null);
            FileBrowser.AddQuickLink("文档", System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), null);
            FileBrowser.AddQuickLink("下载", Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Downloads"), null);
            
            // 排除不需要的文件类型
            FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".exe", ".dll", ".zip", ".rar");
        }
        
        /// <summary>
        /// 设置下拉菜单选项和事件监听
        /// </summary>
        private void SetupDropdowns()
        {
            if (modelDropdown != null)
            {
                // 添加"添加模型"选项
                List<string> modelOptions = new List<string> { "添加模型..." };
                modelDropdown.ClearOptions();
                modelDropdown.AddOptions(modelOptions);
                modelDropdown.onValueChanged.AddListener(OnModelDropdownChanged);
            }
            
            if (motionDropdown != null) 
            {
                List<string> motionOptions = new List<string> { "添加动作..." };
                motionDropdown.ClearOptions();
                motionDropdown.AddOptions(motionOptions);
                motionDropdown.onValueChanged.AddListener(OnMotionDropdownChanged);
            }
            
            if (cameraDropdown != null) 
            {
                List<string> cameraOptions = new List<string> { "添加相机..." };
                cameraDropdown.ClearOptions();
                cameraDropdown.AddOptions(cameraOptions);
                cameraDropdown.onValueChanged.AddListener(OnCameraDropdownChanged);
            }
            
            if (musicDropdown != null) 
            {
                List<string> musicOptions = new List<string> { "添加音乐..." };
                musicDropdown.ClearOptions();
                musicDropdown.AddOptions(musicOptions);
                musicDropdown.onValueChanged.AddListener(OnMusicDropdownChanged);
            }
        }
        
        /// <summary>
        /// 更新模型下拉列表
        /// </summary>
        public void UpdateModelList()
        {
            if (modelDropdown == null || _mainSceneController == null) return;
            
            // 保存当前选项
            int selectedIndex = modelDropdown.value;
            
            // 清除现有选项
            modelDropdown.ClearOptions();
            
            // 添加"添加模型"选项
            List<string> options = new List<string>() { "添加模型..." };
            
            // 添加现有模型
            if (_mainSceneController.GetModelCount() > 0)
            {
                IList<string> modelNames = _mainSceneController.GetModelNames();
                options.AddRange(modelNames);
            }
            
            // 更新下拉菜单选项
            modelDropdown.AddOptions(options);
            
            // 如果之前选择的索引仍然有效，则恢复它
            if (selectedIndex < options.Count)
            {
                modelDropdown.value = selectedIndex;
            }
            else
            {
                modelDropdown.value = 0;
            }
        }
        
        /// <summary>
        /// 更新动作下拉列表
        /// </summary>
        public void UpdateMotionDropdown(string currentMotionPath)
        {
            if (motionDropdown == null) return;
            
            // 清除现有选项
            motionDropdown.ClearOptions();
            
            // 添加默认选项
            List<string> options = new List<string>() { "添加动作..." };
            
            // 如果有当前动作文件，添加它
            if (!string.IsNullOrEmpty(currentMotionPath))
            {
                options.Add(Path.GetFileName(currentMotionPath));
                motionDropdown.value = 1; // 选择当前动作
            }
            else
            {
                motionDropdown.value = 0;
            }
            
            motionDropdown.AddOptions(options);
        }
        
        /// <summary>
        /// 更新相机下拉列表
        /// </summary>
        public void UpdateCameraDropdown(string currentCameraPath)
        {
            if (cameraDropdown == null) return;
            
            // 清除现有选项
            cameraDropdown.ClearOptions();
            
            // 添加默认选项
            List<string> options = new List<string>() { "添加相机..." };
            
            // 如果有当前相机文件，添加它
            if (!string.IsNullOrEmpty(currentCameraPath))
            {
                options.Add(Path.GetFileName(currentCameraPath));
                cameraDropdown.value = 1; // 选择当前相机
            }
            else
            {
                cameraDropdown.value = 0;
            }
            
            cameraDropdown.AddOptions(options);
        }
        
        /// <summary>
        /// 更新音乐下拉列表
        /// </summary>
        public void UpdateMusicDropdown(string currentMusicPath)
        {
            if (musicDropdown == null) return;
            
            // 清除现有选项
            musicDropdown.ClearOptions();
            
            // 添加默认选项
            List<string> options = new List<string>() { "添加音乐..." };
            
            // 如果有当前音乐文件，添加它
            if (!string.IsNullOrEmpty(currentMusicPath))
            {
                options.Add(Path.GetFileName(currentMusicPath));
                musicDropdown.value = 1; // 选择当前音乐
            }
            else
            {
                musicDropdown.value = 0;
            }
            
            musicDropdown.AddOptions(options);
        }

        /// <summary>
        /// 模型下拉菜单变化事件处理
        /// </summary>
        private void OnModelDropdownChanged(int index)
        {
            if (_mainSceneController == null) return;
            
            if (index == 0) // "添加模型..."选项
            {
                ShowFileBrowser(FileType.Model);
            }
            else if (index > 0 && _mainSceneController.GetModelCount() > 0)
            {
                // 选择对应的模型
                _mainSceneController.SelectModel(index - 1);
                
                // 更新动作下拉菜单
                UpdateMotionDropdown(_mainSceneController.GetCurrentMotionPath());
            }
        }
        
        /// <summary>
        /// 动作下拉菜单变化事件处理
        /// </summary>
        private void OnMotionDropdownChanged(int index)
        {
            if (_mainSceneController == null) return;
            
            if (index == 0) // "添加动作..."选项
            {
                ShowFileBrowser(FileType.Motion);
            }
        }
        
        /// <summary>
        /// 相机下拉菜单变化事件处理
        /// </summary>
        private void OnCameraDropdownChanged(int index)
        {
            if (_mainSceneController == null) return;
            
            if (index == 0) // "添加相机..."选项
            {
                ShowFileBrowser(FileType.Camera);
            }
        }
        
        /// <summary>
        /// 音乐下拉菜单变化事件处理
        /// </summary>
        private void OnMusicDropdownChanged(int index)
        {
            if (_mainSceneController == null) return;
            
            if (index == 0) // "添加音乐..."选项
            {
                ShowFileBrowser(FileType.Music);
            }
        }

        /// <summary>
        /// 显示文件浏览器
        /// </summary>
        /// <param name="fileType">要加载的文件类型</param>
        private void ShowFileBrowser(FileType fileType)
        {
            // 设置恰当的过滤器
            switch (fileType)
            {
                case FileType.Model:
                    FileBrowser.SetFilters(true, _modelFilter);
                    FileBrowser.SetDefaultFilter(".pmx");
                    break;
                case FileType.Motion:
                    FileBrowser.SetFilters(true, _motionFilter);
                    FileBrowser.SetDefaultFilter(".vmd");
                    break;
                case FileType.Camera:
                    FileBrowser.SetFilters(true, _cameraFilter);
                    FileBrowser.SetDefaultFilter(".vmd");
                    break;
                case FileType.Music:
                    FileBrowser.SetFilters(true, _musicFilter);
                    FileBrowser.SetDefaultFilter(".wav");
                    break;
            }
            
            // 显示加载对话框
            FileBrowser.ShowLoadDialog(
                (paths) => { OnFilesSelected(paths, fileType); },
                () => { Debug.Log("文件选择已取消"); },
                FileBrowser.PickMode.Files,
                false,
                null,
                null,
                "选择文件",
                "选择"
            );
        }

        /// <summary>
        /// 处理选择的文件
        /// </summary>
        private void OnFilesSelected(string[] paths, FileType fileType)
        {
            if (paths.Length == 0 || _mainSceneController == null) return;
            
            string filePath = paths[0]; // 只处理第一个选择的文件
            
            switch (fileType)
            {
                case FileType.Model:
                    LoadModel(filePath);
                    break;
                case FileType.Motion:
                    LoadMotion(filePath);
                    break;
                case FileType.Camera:
                    LoadCamera(filePath);
                    break;
                case FileType.Music:
                    LoadMusic(filePath);
                    break;
            }
        }

        /// <summary>
        /// 加载模型文件
        /// </summary>
        private void LoadModel(string filePath)
        {
            if (_mainSceneController == null) return;
            
            try
            {
                // 显示加载中提示
                Debug.Log($"正在加载模型: {Path.GetFileName(filePath)}");
                
                // 加载模型
                bool success = _mainSceneController.AddModel(filePath);
                
                if (success)
                {
                    Debug.Log($"成功加载模型: {Path.GetFileName(filePath)}");
                    // 更新模型列表
                    UpdateModelList();
                    // 选择最新添加的模型
                    if (modelDropdown != null && _mainSceneController.GetModelCount() > 0)
                    {
                        modelDropdown.value = _mainSceneController.GetModelCount();
                    }
                    
                    // 更新动作下拉菜单
                    UpdateMotionDropdown(_mainSceneController.GetCurrentMotionPath());
                }
                else
                {
                    Debug.LogError($"无法加载模型: {Path.GetFileName(filePath)}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"加载模型时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 加载动作文件
        /// </summary>
        private void LoadMotion(string filePath)
        {
            if (_mainSceneController == null) return;
            
            // 检查是否有模型已经加载
            if (_mainSceneController.GetModelCount() <= 0)
            {
                Debug.LogWarning("请先加载一个模型");
                return;
            }
            
            try
            {
                Debug.Log($"正在加载动作: {Path.GetFileName(filePath)}");
                
                // 加载动作
                _mainSceneController.ChangeCurrentMotion(filePath);
                Debug.Log($"成功加载动作: {Path.GetFileName(filePath)}");
                
                // 更新动作下拉菜单
                UpdateMotionDropdown(filePath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"加载动作时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 加载相机文件
        /// </summary>
        private void LoadCamera(string filePath)
        {
            if (_mainSceneController == null) return;
            
            try
            {
                Debug.Log($"正在加载相机: {Path.GetFileName(filePath)}");
                
                // 保存相机文件路径
                _mainSceneController.CameraFilePath = filePath;
                
                // 切换到相机文件模式
                if (_mainSceneController.GetXrType() != MikuMikuXR.XR.XrType.CameraFile)
                {
                    _mainSceneController.ChangeXrType(MikuMikuXR.XR.XrType.CameraFile);
                }
                else
                {
                    // 如果已经是相机文件模式，重新加载相机文件
                    var xrController = _mainSceneController.GetXrController() as MikuMikuXR.XR.CameraFileController;
                    if (xrController != null)
                    {
                        xrController.CameraObject.LoadCameraMotion(filePath);
                    }
                }
                
                Debug.Log($"成功加载相机: {Path.GetFileName(filePath)}");
                
                // 更新相机下拉菜单
                UpdateCameraDropdown(filePath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"加载相机时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 加载音乐文件
        /// </summary>
        private void LoadMusic(string filePath)
        {
            if (_mainSceneController == null) return;
            
            try
            {
                Debug.Log($"正在加载音乐: {Path.GetFileName(filePath)}");
                
                // 加载音乐
                _mainSceneController.ChangeMusic(filePath);
                Debug.Log($"成功加载音乐: {Path.GetFileName(filePath)}");
                
                // 更新音乐下拉菜单
                UpdateMusicDropdown(filePath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"加载音乐时出错: {ex.Message}");
            }
        }
    }
}