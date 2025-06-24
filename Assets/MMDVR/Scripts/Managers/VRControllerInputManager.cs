using UnityEngine;
using UnityEngine.XR;
using System;

/// <summary>
/// 全局VR控制器输入管理器，只负责分发按钮事件，不直接操作UI
/// </summary>
public class VRControllerInputManager : MonoBehaviour
{
    public static VRControllerInputManager Instance { get; private set; }

    // 菜单键事件
    public static event Action OnMenuButtonPressed;

    // 可扩展：其它全局按钮事件
    // public static event Action OnPlayPausePressed;
    // ...

    // 支持多设备的按键去抖
    private System.Collections.Generic.Dictionary<uint, bool> lastButtonStates = new System.Collections.Generic.Dictionary<uint, bool>();

    // 可配置的按钮定义
    public InputFeatureUsage<bool> menuButton = CommonUsages.menuButton;
    public InputFeatureUsage<bool> secondaryButton = CommonUsages.secondaryButton;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    void Update()
    {
        if (!XRSettings.isDeviceActive) return;
        var devices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);
        foreach (var device in devices)
        {
            bool menuPressed = false;
            bool secondaryPressed = false;
            device.TryGetFeatureValue(menuButton, out menuPressed);
            device.TryGetFeatureValue(secondaryButton, out secondaryPressed);
            bool pressed = menuPressed || secondaryPressed;

            uint deviceKey = unchecked((uint)device.GetHashCode());
            bool lastPressed = false;
            lastButtonStates.TryGetValue(deviceKey, out lastPressed);

            if (pressed && !lastPressed)
            {
                OnMenuButtonPressed?.Invoke();
            }
            lastButtonStates[deviceKey] = pressed;
        }
    }
}
