using UnityEngine;
using UnityEngine.XR;
using MMDVR.Events;

/// <summary>
/// 监听XR控制器菜单键，触发主UI显示/隐藏
/// </summary>
public class VRMenuInputListener : MonoBehaviour
{
    // XR通用菜单键特征
    private InputFeatureUsage<bool> menuButton = CommonUsages.menuButton;
    private bool lastMenuPressed = false;

    void Update()
    {
        if (!XRSettings.isDeviceActive) return;
        var devices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);
        foreach (var device in devices)
        {
            if (device.TryGetFeatureValue(menuButton, out bool pressed))
            {
                if (pressed && !lastMenuPressed)
                {
                    // 菜单键按下，触发UI切换
                    InputEvents.TriggerUIToggle(InputEvents.InputSource.VR);
                }
                lastMenuPressed = pressed;
                break;
            }
        }
    }
}
