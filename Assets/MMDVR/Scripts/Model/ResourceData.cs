using UnityEngine;
using System.Collections.Generic;

namespace MMDVR.Scripts.Model
{
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
    }    public class ActorData : IResourceInfo
    {
        public string id;
        public string displayName;
        public string filePath;
        public string modelId;
        public List<string> motionIds = new List<string>(); // 支持多个动作
        public bool isVisible = true;

        // 兼容性属性 - 获取或设置第一个动作ID
        public string motionId 
        { 
            get => motionIds.Count > 0 ? motionIds[0] : ""; 
            set 
            { 
                if (motionIds.Count == 0)
                    motionIds.Add(value);
                else
                    motionIds[0] = value;
            } 
        }

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
    }      public class CameraData : IResourceInfo
    {
        public string id;
        public string displayName;
        public string filePath; // Null or empty for Free Camera
        public bool isMMDCamera;
        public bool isFreeCamera;

        public string ID => id;
        public string DisplayName => displayName;
        public string FilePath => filePath;
        public ResourceType Type => ResourceType.Camera;
    }
}
