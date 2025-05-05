using UnityEngine;

namespace MikuMikuXR.Core.Interfaces
{
    /// <summary>
    /// 相机管理器接口，负责处理相机的控制和动画
    /// </summary>
    public interface ICameraManager
    {
        /// <summary>
        /// 获取主相机
        /// </summary>
        /// <returns>主相机引用</returns>
        Camera GetMainCamera();

        /// <summary>
        /// 切换相机模式
        /// </summary>
        /// <param name="mode">相机模式</param>
        /// <returns>切换是否成功</returns>
        bool SwitchCameraMode(CameraMode mode);

        /// <summary>
        /// 获取当前相机模式
        /// </summary>
        /// <returns>当前相机模式</returns>
        CameraMode GetCurrentMode();

        /// <summary>
        /// 加载相机动画
        /// </summary>
        /// <param name="path">相机动画文件路径</param>
        /// <returns>加载是否成功</returns>
        bool LoadCameraMotion(string path);

        /// <summary>
        /// 设置相机位置
        /// </summary>
        /// <param name="position">目标位置</param>
        void SetPosition(Vector3 position);

        /// <summary>
        /// 设置相机旋转
        /// </summary>
        /// <param name="rotation">目标旋转</param>
        void SetRotation(Quaternion rotation);

        /// <summary>
        /// 设置相机是否跟随目标模型
        /// </summary>
        /// <param name="follow">是否跟随</param>
        /// <param name="modelId">模型ID，-1表示当前活动模型</param>
        void SetFollowModel(bool follow, int modelId = -1);

        /// <summary>
        /// 重置相机位置到默认
        /// </summary>
        void ResetCamera();

        /// <summary>
        /// 设置相机视场角
        /// </summary>
        /// <param name="fov">视场角（度）</param>
        void SetFOV(float fov);

        /// <summary>
        /// 获取相机视场角
        /// </summary>
        /// <returns>视场角（度）</returns>
        float GetFOV();
    }
    
    /// <summary>
    /// 相机模式枚举
    /// </summary>
    public enum CameraMode
    {
        /// <summary>自由控制模式</summary>
        Free,
        
        /// <summary>动画模式（使用相机动作文件）</summary>
        Animation,
        
        /// <summary>跟随模式（跟随特定模型）</summary>
        Follow,
        
        /// <summary>VR模式（用于VR视角）</summary>
        VR
    }
}