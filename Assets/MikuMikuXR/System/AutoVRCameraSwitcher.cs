using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 自动检测VR设备并切换相机（桌面/VR）
/// 挂载到System相关GameObject上，Inspector中拖入桌面相机和XR Origin对象
/// </summary>
public class AutoVRCameraSwitcher : MonoBehaviour
{
    [Header("桌面相机父物体（如有多个可用父物体包裹）")]
    public GameObject desktopCameras; // 包含FreeCamera、MMDCamera等
    [Header("XR Origin或VR相机父物体")]
    public GameObject xrOrigin; // XR Origin或VR相机的父物体

    private void Start()
    {
        StartCoroutine(CheckAndSwitchVRCamera());
    }

    private IEnumerator CheckAndSwitchVRCamera()
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

        if (isVRActive)
        {
            if (desktopCameras != null) desktopCameras.SetActive(false);
            if (xrOrigin != null) xrOrigin.SetActive(true);
        }
        else
        {
            if (desktopCameras != null) desktopCameras.SetActive(true);
            if (xrOrigin != null) xrOrigin.SetActive(false);
        }
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
