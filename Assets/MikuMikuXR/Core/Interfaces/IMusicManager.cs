using UnityEngine;

namespace MikuMikuXR.Core.Interfaces
{
    /// <summary>
    /// 音乐管理器接口，负责处理音乐的加载、播放和控制
    /// </summary>
    public interface IMusicManager
    {
        /// <summary>
        /// 加载音乐
        /// </summary>
        /// <param name="path">音乐文件路径</param>
        /// <returns>加载是否成功</returns>
        bool LoadMusic(string path);

        /// <summary>
        /// 播放音乐
        /// </summary>
        void Play();

        /// <summary>
        /// 暂停音乐
        /// </summary>
        void Pause();

        /// <summary>
        /// 停止音乐
        /// </summary>
        void Stop();

        /// <summary>
        /// 设置音乐音量
        /// </summary>
        /// <param name="volume">音量，范围0-1</param>
        void SetVolume(float volume);

        /// <summary>
        /// 获取当前音量
        /// </summary>
        /// <returns>当前音量值</returns>
        float GetVolume();

        /// <summary>
        /// 设置播放位置
        /// </summary>
        /// <param name="time">时间位置（秒）</param>
        void SetTime(float time);

        /// <summary>
        /// 获取当前播放位置
        /// </summary>
        /// <returns>当前播放位置（秒）</returns>
        float GetTime();

        /// <summary>
        /// 获取音乐总时长
        /// </summary>
        /// <returns>音乐总时长（秒）</returns>
        float GetDuration();

        /// <summary>
        /// 获取当前音乐路径
        /// </summary>
        /// <returns>当前音乐文件路径</returns>
        string GetMusicPath();

        /// <summary>
        /// 检查音乐是否已加载
        /// </summary>
        /// <returns>是否已加载音乐</returns>
        bool IsMusicLoaded();

        /// <summary>
        /// 检查音乐是否正在播放
        /// </summary>
        /// <returns>是否正在播放</returns>
        bool IsPlaying();

        /// <summary>
        /// 获取音频源组件
        /// </summary>
        /// <returns>AudioSource组件引用</returns>
        AudioSource GetAudioSource();
    }
}