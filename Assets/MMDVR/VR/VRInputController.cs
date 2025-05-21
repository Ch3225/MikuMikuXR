using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace MMDVR.VR
{
    /// <summary>
    /// 处理VR控制器输入
    /// </summary>
    public class VRInputController : MonoBehaviour
    {
        public static VRInputController Instance { get; private set; }

        [Header("输入设置")]
        public bool trackControllerInput = true;

        // 存储控制器状态
        private Dictionary<string, bool> _buttonStates = new Dictionary<string, bool>();
        private Vector2 _leftThumbstick = Vector2.zero;
        private Vector2 _rightThumbstick = Vector2.zero;
        private float _leftTrigger = 0f;
        private float _rightTrigger = 0f;
        private float _leftGrip = 0f;
        private float _rightGrip = 0f;

        // 按钮事件委托
        public delegate void ButtonEvent(bool pressed);
        
        // 按钮事件字典
        private Dictionary<string, List<ButtonEvent>> _buttonEvents = new Dictionary<string, List<ButtonEvent>>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (trackControllerInput)
            {
                UpdateControllerInput();
            }
        }

        /// <summary>
        /// 更新控制器输入状态
        /// </summary>
        private void UpdateControllerInput()
        {
            List<InputDevice> leftHandedControllers = new List<InputDevice>();
            List<InputDevice> rightHandedControllers = new List<InputDevice>();
            
            // 获取左右手控制器
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left, leftHandedControllers);
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, rightHandedControllers);

            // 处理左手控制器
            if (leftHandedControllers.Count > 0)
            {
                ProcessControllerInput(leftHandedControllers[0], "left");
            }

            // 处理右手控制器
            if (rightHandedControllers.Count > 0)
            {
                ProcessControllerInput(rightHandedControllers[0], "right");
            }
        }

        /// <summary>
        /// 处理控制器输入
        /// </summary>
        private void ProcessControllerInput(InputDevice device, string hand)
        {
            // 主按钮 (A/X)
            bool primaryButtonValue;
            if (device.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButtonValue))
            {
                string buttonName = $"{hand}_primary";
                CheckButtonStateChange(buttonName, primaryButtonValue);
            }

            // 次要按钮 (B/Y)
            bool secondaryButtonValue;
            if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryButtonValue))
            {
                string buttonName = $"{hand}_secondary";
                CheckButtonStateChange(buttonName, secondaryButtonValue);
            }

            // 扳机键
            float triggerValue;
            if (device.TryGetFeatureValue(CommonUsages.trigger, out triggerValue))
            {
                if (hand == "left")
                {
                    _leftTrigger = triggerValue;
                }
                else
                {
                    _rightTrigger = triggerValue;
                }

                // 扳机键按下事件（阈值检测）
                bool triggerPressed = triggerValue > 0.7f;
                string triggerName = $"{hand}_trigger";
                CheckButtonStateChange(triggerName, triggerPressed);
            }

            // 抓取键
            float gripValue;
            if (device.TryGetFeatureValue(CommonUsages.grip, out gripValue))
            {
                if (hand == "left")
                {
                    _leftGrip = gripValue;
                }
                else
                {
                    _rightGrip = gripValue;
                }

                // 抓取键按下事件
                bool gripPressed = gripValue > 0.7f;
                string gripName = $"{hand}_grip";
                CheckButtonStateChange(gripName, gripPressed);
            }

            // 摇杆/触摸板
            Vector2 joystickValue;
            if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystickValue))
            {
                if (hand == "left")
                {
                    _leftThumbstick = joystickValue;
                }
                else
                {
                    _rightThumbstick = joystickValue;
                }
            }

            // 菜单按钮
            bool menuButtonValue;
            if (device.TryGetFeatureValue(CommonUsages.menuButton, out menuButtonValue))
            {
                string menuName = $"{hand}_menu";
                CheckButtonStateChange(menuName, menuButtonValue);
            }
        }

        /// <summary>
        /// 检查按钮状态变化并触发事件
        /// </summary>
        private void CheckButtonStateChange(string buttonName, bool currentValue)
        {
            // 检查按钮之前的状态
            bool previousValue = false;
            _buttonStates.TryGetValue(buttonName, out previousValue);

            // 如果状态发生变化
            if (previousValue != currentValue)
            {
                _buttonStates[buttonName] = currentValue;
                
                // 触发相应的事件
                if (_buttonEvents.ContainsKey(buttonName))
                {
                    foreach (var evt in _buttonEvents[buttonName])
                    {
                        evt?.Invoke(currentValue);
                    }
                }
            }
        }

        /// <summary>
        /// 注册按钮事件
        /// </summary>
        public void RegisterButtonEvent(string buttonName, ButtonEvent callback)
        {
            if (!_buttonEvents.ContainsKey(buttonName))
            {
                _buttonEvents[buttonName] = new List<ButtonEvent>();
            }
            _buttonEvents[buttonName].Add(callback);
        }

        /// <summary>
        /// 注销按钮事件
        /// </summary>
        public void UnregisterButtonEvent(string buttonName, ButtonEvent callback)
        {
            if (_buttonEvents.ContainsKey(buttonName))
            {
                _buttonEvents[buttonName].Remove(callback);
            }
        }

        /// <summary>
        /// 获取左手摇杆/触摸板值
        /// </summary>
        public Vector2 GetLeftThumbstick()
        {
            return _leftThumbstick;
        }

        /// <summary>
        /// 获取右手摇杆/触摸板值
        /// </summary>
        public Vector2 GetRightThumbstick()
        {
            return _rightThumbstick;
        }

        /// <summary>
        /// 获取左手扳机键值
        /// </summary>
        public float GetLeftTrigger()
        {
            return _leftTrigger;
        }

        /// <summary>
        /// 获取右手扳机键值
        /// </summary>
        public float GetRightTrigger()
        {
            return _rightTrigger;
        }

        /// <summary>
        /// 获取左手抓取键值
        /// </summary>
        public float GetLeftGrip()
        {
            return _leftGrip;
        }

        /// <summary>
        /// 获取右手抓取键值
        /// </summary>
        public float GetRightGrip()
        {
            return _rightGrip;
        }

        /// <summary>
        /// 获取按钮当前状态
        /// </summary>
        public bool GetButtonState(string buttonName)
        {
            if (_buttonStates.ContainsKey(buttonName))
            {
                return _buttonStates[buttonName];
            }
            return false;
        }

        /// <summary>
        /// 振动左手控制器
        /// </summary>
        public void VibrateLeftController(float amplitude, float duration)
        {
            List<InputDevice> leftHandedControllers = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left, leftHandedControllers);
            
            if (leftHandedControllers.Count > 0)
            {
                HapticCapabilities capabilities;
                if (leftHandedControllers[0].TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
                {
                    leftHandedControllers[0].SendHapticImpulse(0, amplitude, duration);
                }
            }
        }

        /// <summary>
        /// 振动右手控制器
        /// </summary>
        public void VibrateRightController(float amplitude, float duration)
        {
            List<InputDevice> rightHandedControllers = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, rightHandedControllers);
            
            if (rightHandedControllers.Count > 0)
            {
                HapticCapabilities capabilities;
                if (rightHandedControllers[0].TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
                {
                    rightHandedControllers[0].SendHapticImpulse(0, amplitude, duration);
                }
            }
        }
    }
}
