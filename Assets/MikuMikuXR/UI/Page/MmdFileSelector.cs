using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MikuMikuXR.Components;
using MikuMikuXR.SceneController;
using MikuMikuXR.UI.ListItem;
using MikuMikuXR.UI.Resource;
using MikuMikuXR.UserConfig.Path;
using MikuMikuXR.UserConfig.Resource;
using MikuMikuXR.Utils;
using NSUListView;
using TinyTeam.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MikuMikuXR.UI.Page
{
    public class MmdFileSelector : HideOtherPage
    {
        private USimpleListView _fileListView;
        private Text _title;
        private Context _context;
        private Text _scanTipText;
        private RectTransform _pathBar;
        private RectTransform _viewPort;
        private RectTransform _titleBar;
        private Button _btnSwitchMode;
        private Button _btnScan;
        private Button _btnDrives; // 新增驱动器选择按钮
        private Text _directoryName;
        private bool _showingDrives = false; // 标记是否正在显示驱动器列表

        public delegate void FileSelectHandler(string path);

        public class Context
        {
            public string Title { get; set; }
            public FileType Type { get; set; }
            public FileSelectHandler OnFileSelect { get; set; }
            public bool PathMode { get; set; }
            public string Path { get; set; }
        }

        public enum FileType
        {
            Model,
            Motion,
            Music,
            BonePose
        }

        public MmdFileSelector()
        {
            uiPath = PrefabPaths.MmdFileSelectorPath;
        }

        public override void Awake(GameObject go)
        {
            // 先初始化基本UI组件
            _fileListView = FindCompoment<USimpleListView>("Viewport/Content/FileList");
            _title = FindCompoment<Text>("Title");
            _scanTipText = FindCompoment<Text>("Viewport/Content/ScanTip");
            _pathBar = FindCompoment<RectTransform>("Path");
            _viewPort = FindCompoment<RectTransform>("Viewport");
            _titleBar = FindCompoment<RectTransform>("Header");
            _directoryName = FindCompoment<Text>("Path/DirectoryName");
            
            // 设置基本按钮事件
            SetButtonListener("Bottom/BtnClose", ClosePage);
            _btnScan = FindCompoment<Button>("Bottom/BtnScan");
            UiUtils.SetButtonListener(_btnScan, ShowPage<ScanDialog>);
            _btnSwitchMode = FindCompoment<Button>("Bottom/BtnSwitchMode");
            UiUtils.SetButtonListener(_btnSwitchMode, () =>
            {
                if (_context != null)  // 确保_context不为null
                {
                    _context.PathMode = !_context.PathMode;
                    _showingDrives = false; // 切换模式时重置驱动器显示状态
                    Refresh();
                }
            });
            
            // 修改向上按钮功能，允许显示驱动器列表
            SetButtonListener("Path/DirectoryUp", () =>
            {
                if (_showingDrives)
                {
                    // 如果已经在显示驱动器列表，则不执行任何操作
                    return;
                }
                
                if (_context == null || string.IsNullOrEmpty(_context.Path))
                {
                    return;
                }
                
                try 
                {
                    var parent = new DirectoryInfo(_context.Path).Parent;
                    if (parent == null) 
                    {
                        // 已到达驱动器根目录，显示所有驱动器
                        ShowDrives();
                        return;
                    }
                    
                    var oldPath = _context.Path;
                    _context.Path = parent.FullName;
                    Refresh();
                }
                catch (Exception e)
                {
                    Debug.LogError("Error navigating to parent directory: " + e.Message);
                }
            });
            
            // 添加新的驱动器按钮
            AddDrivesButton();
        }
        
        // 将驱动器按钮创建逻辑提取为单独方法
        private void AddDrivesButton()
        {
            Transform pathTransform = transform.Find("Path");
            if (pathTransform == null || _directoryName == null)
            {
                Debug.LogWarning("无法创建驱动器按钮：Path或DirectoryName组件不存在");
                return;
            }
            
            Transform dirUpTransform = pathTransform.Find("DirectoryUp");
            if (dirUpTransform == null)
            {
                Debug.LogWarning("无法创建驱动器按钮：DirectoryUp组件不存在");
                return;
            }
            
            GameObject btnDrivesObj = new GameObject("BtnDrives", typeof(RectTransform), typeof(Button), typeof(Image));
            btnDrivesObj.transform.SetParent(pathTransform, false);
            
            RectTransform btnRect = btnDrivesObj.GetComponent<RectTransform>();
            Button btnDrivesComponent = btnDrivesObj.GetComponent<Button>();
            
            // 复制DirectoryUp按钮的样式和位置
            RectTransform dirUpRect = dirUpTransform.GetComponent<RectTransform>();
            btnRect.anchorMin = dirUpRect.anchorMin;
            btnRect.anchorMax = dirUpRect.anchorMax;
            btnRect.pivot = dirUpRect.pivot;
            btnRect.anchoredPosition = new Vector2(dirUpRect.anchoredPosition.x - dirUpRect.rect.width - 10, dirUpRect.anchoredPosition.y);
            btnRect.sizeDelta = dirUpRect.sizeDelta;
            
            // 设置按钮图像
            Image btnImage = btnDrivesObj.GetComponent<Image>();
            btnImage.sprite = Sprites.FileIconDirectory; // 使用文件夹图标
            btnImage.color = new Color(0.8f, 0.8f, 0.8f);
            
            // 创建按钮文本
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(btnDrivesObj.transform, false);
            Text textComp = textObj.GetComponent<Text>();
            textComp.text = "驱动器";
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.black;
            
            // 使用安全的方式获取字体
            if (_directoryName.font != null)
            {
                textComp.font = _directoryName.font;
                textComp.fontSize = _directoryName.fontSize;
            }
            else
            {
                // 使用默认字体
                textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                textComp.fontSize = 14;
            }
            
            // 设置文本区域
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            // 添加按钮事件
            btnDrivesComponent.onClick.AddListener(() => {
                ShowDrives();
            });
        }

        // 新增显示驱动器的方法
        private void ShowDrives()
        {
            if (_fileListView == null)
            {
                Debug.LogError("文件列表视图未初始化");
                return;
            }
            
            _showingDrives = true;
            _fileListView.SetData(new List<object>());
            
            List<object> driveItems = new List<object>();
            
            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                
                foreach (DriveInfo drive in drives)
                {
                    if (!drive.IsReady)
                        continue;
                    
                    string driveName = drive.Name;
                    string driveLabel;
                    
                    try {
                        driveLabel = string.IsNullOrEmpty(drive.VolumeLabel) 
                            ? $"{driveName} 本地磁盘" 
                            : $"{driveName} {drive.VolumeLabel}";
                    } catch {
                        driveLabel = driveName;
                    }
                    
                    driveItems.Add(new MmdFileListItemData
                    {
                        FileName = driveLabel.Replace(' ', '\u00A0'),
                        FilePath = driveName.Replace(' ', '\u00A0'),
                        Icon = Sprites.FileIconDirectory,
                        OnClick = () =>
                        {
                            _showingDrives = false;
                            if (_context != null)
                            {
                                _context.Path = driveName;
                                Refresh();
                            }
                        }
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error getting drives: " + e.Message);
            }
            
            _fileListView.SetData(driveItems);
            
            if (_directoryName != null)
            {
                _directoryName.text = "驱动器列表";
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            var context = (Context) data;
            if (context != null)
            {
                _context = context;
            }
            if (_context == null)
            {
                throw new ArgumentException("null context");
            }
            if (string.IsNullOrEmpty(_context.Path))
            {
                _context.Path = Paths.Getter().Home();
            }
            _title.text = _context.Title;
            RefreshPathBar();
            RefreshButtons();
            
            if (_showingDrives)
            {
                ShowDrives();
                return;
            }
            
            if (_context.PathMode)
            {
                RefreshByPath();
            }
            else
            {
                RefreshByResourceList();
            }
        }

        private void RefreshButtons()
        {
            _btnSwitchMode.transform.Find("Text").GetComponent<Text>().text = _context.PathMode ? "资源列表" : "文件夹";
        }

        private void RefreshByPath()
        {
            _fileListView.SetData(new List<object>());
            var currentDirectory = new DirectoryInfo(_context.Path);
            var directories = currentDirectory.GetDirectories();
            var files = currentDirectory.GetFiles();
            var list = (from dir in directories
                    let dir1 = dir
                    select new MmdFileListItemData
                    {
                        FileName = dir.Name.Replace(' ', '\u00A0'),
                        FilePath = dir.FullName.Replace(' ', '\u00A0'),
                        Icon = Sprites.FileIconDirectory,
                        OnClick = () =>
                        {
                            _context.Path = dir1.FullName;
                            Refresh();
                        }
                    }).Cast<object>()
                .ToList();
            list.AddRange((from file in files
                let file1 = file
                where GetFileExtsByFileType(_context.Type).Contains(file.Extension.ToLower())
                select new MmdFileListItemData
                {
                    FileName = file.Name.Replace(' ', '\u00A0'),
                    FilePath = file.FullName.Replace(' ', '\u00A0'),
                    Icon = GetIconByFileType(_context.Type),
                    OnClick = () =>
                    {
                        ClosePage();
                        _context.OnFileSelect(file1.FullName);
                    }
                }).Cast<object>());
            _fileListView.SetData(list);
        }

        private void RefreshByResourceList()
        {
            var listPath = Paths.Getter().ResourceList();
            AllResources allResources;
            try
            {
                allResources = ResourceListStore.Instance.Load(listPath);
            }
            catch (Exception e)
            {
                _scanTipText.gameObject.SetActive(true);
                Debug.LogWarning("load allResources exception. " + e);
                _fileListView.SetData(new List<object>());
                return;
            }
            _scanTipText.gameObject.SetActive(false);
            ResourceList resourceList;
            switch (_context.Type)
            {
                case FileType.Model:
                    resourceList = allResources.ModelList;
                    break;
                case FileType.Motion:
                    resourceList = allResources.MotionList;
                    break;
                case FileType.Music:
                    resourceList = allResources.MusicList;
                    break;
                case FileType.BonePose:
                    resourceList = allResources.BonePoseList;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            _fileListView.SetData(ConvertDataForUi(resourceList));
        }

        private void RefreshPathBar()
        {
            if (_context.PathMode)
            {
                _pathBar.gameObject.SetActive(true);
                _viewPort.offsetMax = new Vector2(_viewPort.offsetMax.x, -_titleBar.rect.height - _pathBar.rect.height);
                var directoryNameText = Path.GetFileName(_context.Path) ?? "";
                _directoryName.text = directoryNameText.Replace(' ', '\u00A0');
            }
            else
            {
                _pathBar.gameObject.SetActive(false);
                _viewPort.offsetMax = new Vector2(_viewPort.offsetMax.x, -_titleBar.rect.height);
            }
        }

        private List<object> ConvertDataForUi(ResourceList resourceList)
        {
            var list = resourceList.List;
            return list.Select(info => new MmdFileListItemData
                {
                    FileName = info.Title.Replace(' ', '\u00A0'), //防止换行
                    FilePath = info.FilePath.Replace(' ', '\u00A0'),
                    Icon =  GetIconByFileType(_context.Type),
                    OnClick = () =>
                    {
                        ClosePage();
                        _context.OnFileSelect(info.FilePath);
                    }
                })
                .Cast<object>()
                .ToList();
        }

        private static Sprite GetIconByFileType(FileType fileType)
        {
            switch (fileType)
            {
                case FileType.Model:
                    return Sprites.FileIconModel;
                case FileType.Motion:
                    return Sprites.FileIconMotion;
                case FileType.Music:
                    return Sprites.FileIconMusic;
                case FileType.BonePose:
                    return Sprites.FileIconMotion;
                default:
                    throw new ArgumentOutOfRangeException("fileType", fileType, null);
            }
        }

        private HashSet<string> GetFileExtsByFileType(FileType fileType)
        {
            switch (fileType)
            {
                case FileType.Model:
                    return Constants.ModelExts;
                case FileType.Motion:
                    return Constants.MotionExts;
                case FileType.Music:
                    return Constants.MusicExts;
                case FileType.BonePose:
                    return Constants.BonePoseExts;
                default:
                    throw new ArgumentOutOfRangeException("fileType", fileType, null);
            }
        }
    }
}