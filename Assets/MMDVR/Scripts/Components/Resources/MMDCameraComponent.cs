using UnityEngine;
using System.IO;

namespace MMDVR.Scripts.Components
{
    /// <summary>
    /// MMD摄像机组件 - 存储和处理VMD摄像机数据
    /// 复用现有MMDCameraManager的解析逻辑
    /// </summary>
    public class MMDCameraComponent : MonoBehaviour
    {
        [Header("组件标识")]
        public string cameraId;
        public string displayName;
        public string filePath;
    }
}
