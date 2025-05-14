using System;

namespace MMDVR.Managers
{
    public static class EventManager
    {
        // path: 模型文件路径
        public static Action<string> OnModelLoadRequest;

        // 模型列表变更事件
        public static Action OnActorListChanged;

        // 相机列表变更事件
        public static Action OnCameraListChanged;

        // 动作列表变更事件
        public static Action OnMotionListChanged;
    }
}
