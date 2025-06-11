using MMDVR.Managers; // For ResourceType

namespace MMDVR.Scripts.UIInteraction
{
    public class CameraData : IResourceInfo
    {
        public string id;
        public string displayName;
        public string filePath; // Null or empty for Free Camera
        public bool isFreeCamera;

        public string ID => id;
        public string DisplayName => displayName;
        public string FilePath => filePath;
        public ResourceType Type => ResourceType.Camera;
    }
}
