using UnityEngine;

namespace MikuMikuXR.Core.Interfaces
{
    /// <summary>
    /// 配置管理器接口，负责处理应用的各种设置
    /// </summary>
    public interface IConfigManager
    {
        /// <summary>
        /// 设置渲染质量等级
        /// </summary>
        /// <param name="level">质量等级</param>
        void SetQualityLevel(int level);
        
        /// <summary>
        /// 获取当前渲染质量等级
        /// </summary>
        /// <returns>质量等级</returns>
        int GetQualityLevel();
        
        /// <summary>
        /// 设置模型细节级别
        /// </summary>
        /// <param name="level">细节级别</param>
        void SetModelDetailLevel(int level);
        
        /// <summary>
        /// 获取当前模型细节级别
        /// </summary>
        /// <returns>细节级别</returns>
        int GetModelDetailLevel();
        
        /// <summary>
        /// 设置抗锯齿级别
        /// </summary>
        /// <param name="level">抗锯齿级别</param>
        void SetAntiAliasingLevel(int level);
        
        /// <summary>
        /// 获取当前抗锯齿级别
        /// </summary>
        /// <returns>抗锯齿级别</returns>
        int GetAntiAliasingLevel();
        
        /// <summary>
        /// 设置物理模拟质量
        /// </summary>
        /// <param name="quality">物理模拟质量</param>
        void SetPhysicsQuality(PhysicsQuality quality);
        
        /// <summary>
        /// 获取物理模拟质量
        /// </summary>
        /// <returns>物理模拟质量</returns>
        PhysicsQuality GetPhysicsQuality();
        
        /// <summary>
        /// 设置阴影质量
        /// </summary>
        /// <param name="quality">阴影质量</param>
        void SetShadowQuality(ShadowQuality shadowQuality);
        
        /// <summary>
        /// 获取阴影质量
        /// </summary>
        /// <returns>阴影质量</returns>
        ShadowQuality GetShadowQuality();
        
        /// <summary>
        /// 设置纹理质量
        /// </summary>
        /// <param name="quality">纹理质量</param>
        void SetTextureQuality(TextureQuality quality);
        
        /// <summary>
        /// 获取纹理质量
        /// </summary>
        /// <returns>纹理质量</returns>
        TextureQuality GetTextureQuality();
        
        /// <summary>
        /// 保存当前配置
        /// </summary>
        /// <returns>保存是否成功</returns>
        bool SaveConfig();
        
        /// <summary>
        /// 加载已保存的配置
        /// </summary>
        /// <returns>加载是否成功</returns>
        bool LoadConfig();
        
        /// <summary>
        /// 恢复默认配置
        /// </summary>
        void RestoreDefaults();
    }
    
    /// <summary>
    /// 物理模拟质量
    /// </summary>
    public enum PhysicsQuality
    {
        /// <summary>关闭物理效果</summary>
        Disabled,
        
        /// <summary>低质量物理效果</summary>
        Low,
        
        /// <summary>中质量物理效果</summary>
        Medium,
        
        /// <summary>高质量物理效果</summary>
        High
    }
    
    /// <summary>
    /// 阴影质量
    /// </summary>
    public enum ShadowQuality
    {
        /// <summary>关闭阴影</summary>
        Disabled,
        
        /// <summary>硬阴影</summary>
        HardOnly,
        
        /// <summary>软阴影</summary>
        SoftShadows
    }
    
    /// <summary>
    /// 纹理质量
    /// </summary>
    public enum TextureQuality
    {
        /// <summary>低质量纹理</summary>
        Low,
        
        /// <summary>中质量纹理</summary>
        Medium,
        
        /// <summary>高质量纹理</summary>
        High
    }
}