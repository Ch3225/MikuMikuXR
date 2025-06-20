using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using System.Collections;
using System.Collections.Generic;

namespace MMDVR.Events
{
    /// <summary>
    /// 系统级事件 - 处理应用生命周期、场景管理等系统级事件
    /// </summary>
    public static class SystemEvents
    {        // ==================== 应用生命周期事件 ====================
        public static Action OnApplicationStart;
        public static Action OnApplicationPause;
        public static Action OnApplicationQuit;
        
        // ==================== 场景管理事件 ====================
        public static Action<string> OnSceneLoadStart; // 场景开始加载
        public static Action<string> OnSceneLoadComplete; // 场景加载完成
        public static Action<string> OnSceneUnload; // 场景卸载        // ==================== 设备管理事件 ====================
        public static Action<bool> OnXRSystemStateChanged; // XR系统状态变化
        public static Action OnInputModuleChanged; // 输入模块切换
        public static Action OnCameraReset; // 相机重置
        public static Action<bool> OnVRModeDetected; // VR模式检测结果
        
        // ==================== 便捷触发方法 ====================
        public static void TriggerApplicationStart()
        {
            OnApplicationStart?.Invoke();
        }
        
        public static void TriggerApplicationPause()
        {
            OnApplicationPause?.Invoke();
        }
        
        public static void TriggerApplicationQuit()
        {
            OnApplicationQuit?.Invoke();
        }
        
        public static void TriggerSceneLoadStart(string sceneName)
        {
            OnSceneLoadStart?.Invoke(sceneName);
        }
        
        public static void TriggerSceneLoadComplete(string sceneName)
        {
            OnSceneLoadComplete?.Invoke(sceneName);
        }
        
        public static void TriggerSceneUnload(string sceneName)
        {
            OnSceneUnload?.Invoke(sceneName);
        }
        
        public static void TriggerXRSystemStateChanged(bool isActive)
        {
            OnXRSystemStateChanged?.Invoke(isActive);
        }
          public static void TriggerInputModuleChanged()
        {
            OnInputModuleChanged?.Invoke();
        }
          public static void TriggerCameraReset()
        {
            OnCameraReset?.Invoke();
        }
        
        public static void TriggerVRModeDetected(bool isVRActive)
        {
            OnVRModeDetected?.Invoke(isVRActive);
        }
        
        // ==================== VR检测相关方法 ====================
        
        /// <summary>
        /// 开始VR设备检测协程
        /// </summary>
        /// <param name="monoBehaviour">用于启动协程的MonoBehaviour实例</param>
        public static void StartVRDetection(MonoBehaviour monoBehaviour)
        {
            monoBehaviour.StartCoroutine(CheckAndNotifyVRMode());
        }
        
        /// <summary>
        /// 检测VR设备并通知结果
        /// </summary>
        private static IEnumerator CheckAndNotifyVRMode()
        {
            // 等待XR系统初始化（最多2秒）
            float timer = 0f;
            bool isVRActive = false;
            while (timer < 2f)
            {
                if (IsVRActive())
                {
                    isVRActive = true;
                    break;
                }
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            // 通知VR检测结果
            TriggerVRModeDetected(isVRActive);
        }

        /// <summary>
        /// 检查VR是否激活 - 双重判断：XRInputSubsystem.running + HMD设备检测
        /// </summary>
        /// <returns>VR是否激活</returns>
        public static bool IsVRActive()
        {
            // 方案一：XRInputSubsystem.running
            List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var subsystem in subsystems)
            {
                if (subsystem.running)
                    return true;
            }
            
            // 方案二：HMD设备检测
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);
            if (devices.Count > 0)
                return true;
                
            return false;
        }
          /// <summary>
        /// 清除所有系统事件订阅（用于场景清理）
        /// </summary>
        public static void ClearAllEvents()
        {
            OnApplicationStart = null;
            OnApplicationPause = null;
            OnApplicationQuit = null;
            OnSceneLoadStart = null;
            OnSceneLoadComplete = null;
            OnSceneUnload = null;
            OnXRSystemStateChanged = null;
            OnInputModuleChanged = null;
            OnCameraReset = null;
            OnVRModeDetected = null;
        }
    }
}
