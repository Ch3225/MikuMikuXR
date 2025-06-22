namespace MMDVR.Scripts.Model
{
    public interface IResourceInfo
    {
        string ID { get; }
        string DisplayName { get; }
        string FilePath { get; } // Or other relevant path/identifier
        ResourceType Type { get; }
    }
}
