using System.Collections.Generic;
using UnityEngine;

namespace MikuMikuXR.Core.Interfaces
{
    /// <summary>
    /// 单个模型的接口，定义模型的属性和操作
    /// </summary>
    public interface IModel
    {
        /// <summary>
        /// 模型ID
        /// </summary>
        int Id { get; }
        
        /// <summary>
        /// 模型文件路径
        /// </summary>
        string FilePath { get; }
        
        /// <summary>
        /// 模型名称
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// 模型的Transform组件
        /// </summary>
        Transform Transform { get; }
        
        /// <summary>
        /// 模型是否可见
        /// </summary>
        bool IsVisible { get; set; }
        
        /// <summary>
        /// 为模型添加动作
        /// </summary>
        /// <param name="motionPath">动作文件路径</param>
        /// <returns>添加成功返回动作索引，失败返回-1</returns>
        int AddMotion(string motionPath);
        
        /// <summary>
        /// 移除动作
        /// </summary>
        /// <param name="motionIndex">动作索引</param>
        /// <returns>移除成功返回true，失败返回false</returns>
        bool RemoveMotion(int motionIndex);
        
        /// <summary>
        /// 设置当前活动动作
        /// </summary>
        /// <param name="motionIndex">动作索引</param>
        void SetActiveMotion(int motionIndex);
        
        /// <summary>
        /// 获取当前活动动作索引
        /// </summary>
        /// <returns>活动动作索引，如无活动动作返回-1</returns>
        int GetActiveMotionIndex();
        
        /// <summary>
        /// 获取所有动作路径列表
        /// </summary>
        /// <returns>动作路径列表</returns>
        List<string> GetAllMotionPaths();
        
        /// <summary>
        /// 获取动作数量
        /// </summary>
        /// <returns>动作数量</returns>
        int GetMotionCount();
        
        /// <summary>
        /// 更新模型，处理动画和物理
        /// </summary>
        /// <param name="deltaTime">帧时间间隔</param>
        void Update(float deltaTime);
    }
}