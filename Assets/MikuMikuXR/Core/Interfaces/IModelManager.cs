using System.Collections.Generic;
using UnityEngine;

namespace MikuMikuXR.Core.Interfaces
{
    /// <summary>
    /// 模型管理器接口，负责处理MMD模型的加载、卸载和操作
    /// </summary>
    public interface IModelManager
    {
        /// <summary>
        /// 添加模型到场景
        /// </summary>
        /// <param name="path">模型文件路径</param>
        /// <returns>添加的模型ID，失败返回-1</returns>
        int AddModel(string path);

        /// <summary>
        /// 从场景中移除模型
        /// </summary>
        /// <param name="modelId">要移除的模型ID</param>
        /// <returns>移除是否成功</returns>
        bool RemoveModel(int modelId);

        /// <summary>
        /// 设置当前活动模型
        /// </summary>
        /// <param name="modelId">要设置为活动的模型ID</param>
        /// <returns>设置是否成功</returns>
        bool SetActiveModel(int modelId);

        /// <summary>
        /// 获取当前活动模型的ID
        /// </summary>
        /// <returns>当前活动模型ID，如果没有活动模型则返回-1</returns>
        int GetActiveModelId();

        /// <summary>
        /// 获取所有已加载模型的ID列表
        /// </summary>
        /// <returns>模型ID列表</returns>
        List<int> GetAllModelIds();

        /// <summary>
        /// 获取特定模型的GameObject引用
        /// </summary>
        /// <param name="modelId">模型ID</param>
        /// <returns>模型的GameObject，不存在则返回null</returns>
        GameObject GetModelObject(int modelId);

        /// <summary>
        /// 获取特定模型的变换组件
        /// </summary>
        /// <param name="modelId">模型ID</param>
        /// <returns>模型的变换组件，不存在则返回null</returns>
        Transform GetModelTransform(int modelId);

        /// <summary>
        /// 为模型加载动作
        /// </summary>
        /// <param name="modelId">模型ID</param>
        /// <param name="motionPath">动作文件路径</param>
        /// <returns>加载是否成功</returns>
        bool LoadMotionForModel(int modelId, string motionPath);

        /// <summary>
        /// 为模型加载骨骼姿势
        /// </summary>
        /// <param name="modelId">模型ID</param>
        /// <param name="posePath">姿势文件路径</param>
        /// <returns>加载是否成功</returns>
        bool LoadBonePoseForModel(int modelId, string posePath);

        /// <summary>
        /// 获取模型信息
        /// </summary>
        /// <param name="modelId">模型ID</param>
        /// <returns>模型名称</returns>
        string GetModelName(int modelId);

        /// <summary>
        /// 获取模型当前动作路径
        /// </summary>
        /// <param name="modelId">模型ID</param>
        /// <returns>动作文件路径</returns>
        string GetModelMotionPath(int modelId);

        /// <summary>
        /// 获取模型数量
        /// </summary>
        /// <returns>当前场景中的模型数量</returns>
        int GetModelCount();
    }
}