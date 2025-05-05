using System.Collections.Generic;
using MikuMikuXR.SceneController;
using UnityEngine;

namespace MikuMikuXR.XR
{
    public class UdtEventHandler : MonoBehaviour
    {
        // 移除了对 Vuforia 类型的引用并替换为 GameObject
        public GameObject ImageTargetTemplate;
        
        private int _targetCounter;

        public int LastTargetIndex
        {
            get { return (_targetCounter - 1) % 5; }
        }

        private void Start()
        {
            Debug.LogWarning("UdtEventHandler: AR 功能已被禁用，只支持 VR 模式。");
        }

        // 简化的接口方法，不再依赖 Vuforia
        public void OnInitialized()
        {
            Debug.LogWarning("UdtEventHandler: AR 初始化已跳过。");
        }

        // 使用我们自定义的 FrameQuality 枚举
        public void OnFrameQualityChanged(MainSceneController.FrameQuality frameQuality)
        {
            Debug.Log("Frame quality changed: " + frameQuality);
            MainSceneController.Instance.OnArFrameQualityChanged.Invoke(frameQuality);
        }

        // 简化版本，不再创建 AR 跟踪目标
        public void OnNewTrackableSource(object trackableSource)
        {
            Debug.LogWarning("UdtEventHandler: AR 创建跟踪目标已跳过。");
        }

        // 返回 false，表示无法创建 AR 目标
        public bool BuildNewTarget()
        {
            Debug.LogWarning("UdtEventHandler: 无法创建 AR 目标，AR 功能已禁用。");
            return false;
        }

        // 空方法，保留接口
        public void ClearTargets()
        {
            Debug.LogWarning("UdtEventHandler: 清理 AR 目标已跳过。");
        }
    }
}