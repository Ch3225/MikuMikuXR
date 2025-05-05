using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MikuMikuXR.XR
{
    // 这个类原本使用了 Vuforia 的 AR 功能，现在我们将其修改为简化版本
    // 在 Windows VR 环境下，可能不会使用这个控制器，但保留它以防止其他脚本引用出错
    public class ArUserDefinedController : IXrController
    {
        private GameObject _dummyObject;

        public ArUserDefinedController()
        {
            Tracking = false;
        }

        public bool Tracking { get; private set; }

        public void Create()
        {
            // 创建一个空的游戏对象来替代原有的 Vuforia 对象
            _dummyObject = new GameObject("DummyArObject");
            
            // 记录日志，提醒用户 AR 功能已被禁用
            Debug.LogWarning("AR 功能已被禁用。项目已转换为仅支持 VR 模式。");
        }

        public void Destroy()
        {
            if (_dummyObject != null)
            {
                Object.Destroy(_dummyObject);
            }
        }

        public XrType GetType()
        {
            return XrType.ArUserDefined;
        }

        public bool EnableGesture()
        {
            return false;
        }

        public bool BuildTarget()
        {
            Debug.LogWarning("AR BuildTarget() 方法已被禁用。请使用 VR 模式。");
            return false;
        }

        public void ClearTargets()
        {
            Debug.LogWarning("AR ClearTargets() 方法已被禁用。请使用 VR 模式。");
            Tracking = false;
        }
    }
}