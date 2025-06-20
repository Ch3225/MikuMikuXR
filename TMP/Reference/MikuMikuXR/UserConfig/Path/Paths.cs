using System;
using System.IO;
using UnityEngine;

namespace MikuMikuXR.UserConfig.Path
{
    public class Paths
    {
        private static readonly IPathGetter PathGetter;

        private static bool _directoryCreated;
        
        static Paths()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    PathGetter = new AndroidPathGetter();
                    break;
                case RuntimePlatform.IPhonePlayer:
                    PathGetter = new IosPathGetter();
                    break;
                case RuntimePlatform.OSXEditor:
                    PathGetter = new MacEditorPathGetter();
                    break;
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    PathGetter = new WindowsPathGetter();
                    break;
                default:
                    Debug.LogWarning("不支持的平台 " + Application.platform + "，使用通用 Windows 路径处理");
                    PathGetter = new WindowsPathGetter();
                    break;
            }
        }

        public static IPathGetter Getter()
        {
            return PathGetter;
        }

        public static string RelativeToHomePath(string path)
        {
            if (System.IO.Path.IsPathRooted(path))
            {
                return path;
            }
            return Getter().Home() + "/" + path;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (_directoryCreated)
            {
                return;
            }
            Directory.CreateDirectory(PathGetter.ConfigFolder());
            Directory.CreateDirectory(PathGetter.SceneFolder());
            Directory.CreateDirectory(PathGetter.BonePoseFolder());
            _directoryCreated = true;
        }

    }

    // 为 Windows 平台添加路径处理实现
    public class WindowsPathGetter : IPathGetter
    {
        private readonly string _homeDir;

        public WindowsPathGetter()
        {
            // 在 Windows 上使用我的文档或应用程序数据文件夹作为主目录
            _homeDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MikuMikuXR");
            
            // 确保主目录存在
            if (!Directory.Exists(_homeDir))
            {
                Directory.CreateDirectory(_homeDir);
            }
        }

        public string Home()
        {
            return _homeDir;
        }

        public string ConfigFolder()
        {
            return System.IO.Path.Combine(_homeDir, "Config");
        }

        public string ResourceList()
        {
            return System.IO.Path.Combine(_homeDir, "resources.json");
        }

        public string SceneFolder()
        {
            return System.IO.Path.Combine(_homeDir, "Scenes");
        }

        public string BonePoseFolder()
        {
            return System.IO.Path.Combine(_homeDir, "BonePoses");
        }
    }
}