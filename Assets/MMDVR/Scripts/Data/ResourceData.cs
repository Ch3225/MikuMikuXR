using UnityEngine;
using MMDVR.Scripts.UIInteraction;

namespace MMDVR.Scripts.Data
{
    /// <summary>
    /// Free摄像机组件 - 存储Free Camera的状态
    /// </summary>
    public class FreeCameraComponent : MonoBehaviour
    {
        public string id;
        public string displayName;
        public Vector3 position = Vector3.zero;
        public Quaternion rotation = Quaternion.identity;
        public float fieldOfView = 60f;
    }

    // ===== UI兼容性数据类 =====
      public class MusicData : IResourceInfo
    {
        public string id;
        public string displayName;
        public string filePath;
        
        public string ID => id;
        public string DisplayName => displayName;
        public string FilePath => filePath;
        public ResourceType Type => ResourceType.Music;
    }

    public class ActorData : IResourceInfo
    {
        public string id;
        public string displayName;
        public string filePath;
        public string modelId;
        public string motionId;
        public bool isVisible = true;

        public string ID => id;
        public string DisplayName => displayName;
        public string FilePath => filePath;
        public ResourceType Type => ResourceType.Model;
    }

    public class ModelData : IResourceInfo
    {
        public string id;
        public string displayName;
        public string filePath;

        public string ID => id;
        public string DisplayName => displayName;
        public string FilePath => filePath;
        public ResourceType Type => ResourceType.Model;
    }

    public class MotionData : IResourceInfo
    {
        public string id;
        public string displayName;
        public string filePath;
        public string assignedActorId;

        public string ID => id;
        public string DisplayName => displayName;
        public string FilePath => filePath;
        public ResourceType Type => ResourceType.Motion;
    }    public class CameraData : IResourceInfo
    {
        public string id;
        public string displayName;
        public string filePath;
        public bool isMMDCamera;
        public bool isFreeCamera;

        public string ID => id;
        public string DisplayName => displayName;
        public string FilePath => filePath;
        public ResourceType Type => ResourceType.Camera;
    }
}
