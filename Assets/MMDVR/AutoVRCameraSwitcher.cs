using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using System.Collections;
using System.Collections.Generic;
using MMDVR.Managers;

/// <summary>
/// 自动检测VR设备并通知SceneStatesManager切换摄像机模式
/// </summary>
public class AutoVRCameraSwitcher : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(CheckAndSwitchVRCamera());
    }

    private IEnumerator CheckAndSwitchVRCamera()
    {
        // 等待SceneStatesManager初始化
        while (SceneStatesManager.Instance == null)
        {
            yield return null;
        }

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

        // 通知SceneStatesManager设置摄像机模式
        SceneStatesManager.Instance.SetCameraMode(isVRActive ? CameraMode.VR : CameraMode.Desktop);
    }

    // 双重判断：XRInputSubsystem.running + HMD设备检测
    private bool IsVRActive()
    {
        // 方案一：XRInputSubsystem.running
        List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        foreach (var subsystem in subsystems)
        {
            if (subsystem.running)
                return true;
        }
        // 方案五：HMD设备检测
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);
        if (devices.Count > 0)
            return true;
        return false;
    }
}
