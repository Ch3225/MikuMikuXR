using System.Collections.Generic;
using UnityEngine;

namespace MikuMikuXR.Core.Interfaces
{
    /// <summary>
    /// 动作管理器接口，负责处理模型动作的播放和控制
    /// </summary>
    public interface IMotionManager
    {
        /// <summary>
        /// 播放当前加载的所有动作
        /// </summary>
        void Play();
        
        /// <summary>
        /// 暂停当前播放的所有动作
        /// </summary>
        void Pause();
        
        /// <summary>
        /// 停止并重置所有动作
        /// </summary>
        void Stop();
        
        /// <summary>
        /// 设置动作播放速度
        /// </summary>
        /// <param name="speed">播放速度倍数</param>
        void SetSpeed(float speed);
        
        /// <summary>
        /// 获取当前动作播放速度
        /// </summary>
        /// <returns>播放速度倍数</returns>
        float GetSpeed();
        
        /// <summary>
        /// 设置动作播放位置
        /// </summary>
        /// <param name="time">时间位置（秒）</param>
        void SetTime(float time);
        
        /// <summary>
        /// 获取当前动作播放位置
        /// </summary>
        /// <returns>当前播放位置（秒）</returns>
        float GetTime();
        
        /// <summary>
        /// 获取当前加载的动作总时长
        /// </summary>
        /// <returns>动作总时长（秒）</returns>
        float GetDuration();
        
        /// <summary>
        /// 检查是否有动作正在播放
        /// </summary>
        /// <returns>是否正在播放</returns>
        bool IsPlaying();
        
        /// <summary>
        /// 设置是否循环播放
        /// </summary>
        /// <param name="loop">是否循环</param>
        void SetLoop(bool loop);
        
        /// <summary>
        /// 获取是否循环播放
        /// </summary>
        /// <returns>是否循环</returns>
        bool GetLoop();
        
        /// <summary>
        /// 同步所有模型动作到当前时间
        /// </summary>
        void SyncAllMotions();
        
        /// <summary>
        /// 重置所有模型动作到初始状态
        /// </summary>
        void ResetAllMotions();
        
        /// <summary>
        /// 检查与音乐的同步状态
        /// </summary>
        /// <returns>是否与音乐同步</returns>
        bool IsSyncWithMusic();
        
        /// <summary>
        /// 设置是否与音乐同步
        /// </summary>
        /// <param name="sync">是否同步</param>
        void SetSyncWithMusic(bool sync);
    }
}