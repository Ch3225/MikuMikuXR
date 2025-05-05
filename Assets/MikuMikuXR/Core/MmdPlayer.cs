using MikuMikuXR.Core.Interfaces;
using UnityEngine;

namespace MikuMikuXR.Core
{
    /// <summary>
    /// MmdPlayer是整个MMD播放器系统的核心类，
    /// 它组合了所有管理器接口，提供对整个系统的访问点
    /// </summary>
    public class MmdPlayer : MonoBehaviour
    {
        // 单例实例
        private static MmdPlayer _instance;
        
        // 各种管理器接口
        private IModelManager _modelManager;
        private IMusicManager _musicManager;
        private ICameraManager _cameraManager;
        private IMotionManager _motionManager;
        private IConfigManager _configManager;
        
        /// <summary>
        /// 单例访问器
        /// </summary>
        public static MmdPlayer Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 在场景中尝试查找实例
                    _instance = FindObjectOfType<MmdPlayer>();
                    
                    // 如果找不到则创建新实例
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("MmdPlayer");
                        _instance = go.AddComponent<MmdPlayer>();
                        DontDestroyOnLoad(go);
                    }
                }
                
                return _instance;
            }
        }
        
        /// <summary>
        /// 模型管理器
        /// </summary>
        public IModelManager ModelManager => _modelManager;
        
        /// <summary>
        /// 音乐管理器
        /// </summary>
        public IMusicManager MusicManager => _musicManager;
        
        /// <summary>
        /// 相机管理器
        /// </summary>
        public ICameraManager CameraManager => _cameraManager;
        
        /// <summary>
        /// 动作管理器
        /// </summary>
        public IMotionManager MotionManager => _motionManager;
        
        /// <summary>
        /// 配置管理器
        /// </summary>
        public IConfigManager ConfigManager => _configManager;
        
        private void Awake()
        {
            // 确保单例的唯一性
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 初始化系统
            Initialize();
        }
        
        /// <summary>
        /// 初始化系统，创建并设置各个管理器
        /// </summary>
        private void Initialize()
        {
            // 在这里实例化各个管理器的具体实现
            // 这将在实现类创建后完成
            Debug.Log("MmdPlayer系统初始化...");
            
            // 初始化管理器，将在具体实现类完成后添加代码
            // _modelManager = new ModelManager();
            // _musicManager = new MusicManager();
            // 等等...
            
            // 加载配置
            // _configManager.LoadConfig();
            
            Debug.Log("MmdPlayer系统初始化完成");
        }
        
        /// <summary>
        /// 播放当前场景
        /// </summary>
        public void Play()
        {
            if (_motionManager != null)
                _motionManager.Play();
                
            if (_musicManager != null)
                _musicManager.Play();
        }
        
        /// <summary>
        /// 暂停当前场景
        /// </summary>
        public void Pause()
        {
            if (_motionManager != null)
                _motionManager.Pause();
                
            if (_musicManager != null)
                _musicManager.Pause();
        }
        
        /// <summary>
        /// 停止当前场景并重置
        /// </summary>
        public void Stop()
        {
            if (_motionManager != null)
                _motionManager.Stop();
                
            if (_musicManager != null)
                _musicManager.Stop();
        }
        
        /// <summary>
        /// 设置播放时间位置
        /// </summary>
        /// <param name="time">时间位置（秒）</param>
        public void SetTime(float time)
        {
            if (_motionManager != null)
                _motionManager.SetTime(time);
                
            if (_musicManager != null)
                _musicManager.SetTime(time);
        }
    }
}