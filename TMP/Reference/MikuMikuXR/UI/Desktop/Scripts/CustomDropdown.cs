using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MikuMikuXR.UI.Desktop
{
    /// <summary>
    /// 自定义下拉菜单组件，替代有问题的 Unity UI Toolkit DropdownField
    /// </summary>
    public class CustomDropdown
    {
        // 内部数据
        private Button _triggerButton;
        private VisualElement _dropdownContainer;
        private ScrollView _optionsContainer;
        private VisualElement _dropdown;
        private List<string> _choices = new List<string>();
        private string _currentValue = string.Empty;
        private bool _isOpen = false;

        // 事件
        public event Action<string> OnValueChanged;

        /// <summary>
        /// 下拉菜单选项列表
        /// </summary>
        public List<string> Choices
        {
            get => _choices;
            set
            {
                _choices = value ?? new List<string>();
                RebuildOptions();
            }
        }

        /// <summary>
        /// 当前选中的值
        /// </summary>
        public string Value
        {
            get => _currentValue;
            set
            {
                if (_currentValue != value)
                {
                    _currentValue = value;
                    if (_triggerButton != null)
                    {
                        _triggerButton.text = value;
                    }
                }
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="triggerButton">触发下拉菜单的按钮</param>
        /// <param name="container">下拉菜单的容器</param>
        public CustomDropdown(Button triggerButton, VisualElement container)
        {
            _triggerButton = triggerButton;
            _dropdownContainer = container;

            // 创建下拉面板
            CreateDropdown();

            // 绑定事件
            _triggerButton.clicked += ToggleDropdown;

            // 初始设置按钮文本
            if (!string.IsNullOrEmpty(_currentValue))
            {
                _triggerButton.text = _currentValue;
            }
        }

        /// <summary>
        /// 创建下拉面板
        /// </summary>
        private void CreateDropdown()
        {
            // 创建下拉框容器
            _dropdown = new VisualElement();
            _dropdown.name = "custom-dropdown";
            _dropdown.AddToClassList("custom-dropdown");
            _dropdown.style.position = Position.Absolute;
            _dropdown.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 0.95f));
            // 移除不兼容的样式属性
            // 使用USS类代替直接设置样式属性
            _dropdown.AddToClassList("dropdown-box");
            _dropdown.style.paddingTop = 4;
            _dropdown.style.paddingBottom = 4;
            _dropdown.style.display = DisplayStyle.None;
            _dropdown.style.maxHeight = 200;
            _dropdown.style.minWidth = 100;
            // 移除不兼容的boxShadow属性
            
            // 创建选项容器（可滚动）
            _optionsContainer = new ScrollView();
            _optionsContainer.style.flexGrow = 1;
            _dropdown.Add(_optionsContainer);

            // 添加到全局容器
            _dropdownContainer.Add(_dropdown);

            // 点击外部关闭下拉框
            _dropdownContainer.RegisterCallback<ClickEvent>(evt => {
                if (_isOpen && evt.target != _triggerButton)
                {
                    CloseDropdown();
                }
            });
        }

        /// <summary>
        /// 重建选项列表
        /// </summary>
        private void RebuildOptions()
        {
            // 清空当前选项
            _optionsContainer.Clear();

            // 添加新选项
            foreach (var choice in _choices)
            {
                var option = new Button();
                option.text = choice;
                option.AddToClassList("dropdown-option");
                option.style.backgroundColor = StyleKeyword.None;
                // 移除不兼容的样式属性
                // 使用USS类代替直接设置样式属性
                option.AddToClassList("dropdown-option-item");
                option.style.paddingLeft = 10;
                option.style.paddingRight = 10;
                option.style.paddingTop = 5;
                option.style.paddingBottom = 5;
                option.style.marginTop = 2;
                option.style.marginBottom = 2;
                option.style.marginLeft = 2;
                option.style.marginRight = 2;
                option.style.unityTextAlign = TextAnchor.MiddleLeft;
                option.style.flexGrow = 1;
                option.style.width = new Length(100, LengthUnit.Percent);

                // 悬停效果
                option.RegisterCallback<MouseOverEvent>(_ => {
                    option.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
                });
                option.RegisterCallback<MouseOutEvent>(_ => {
                    option.style.backgroundColor = StyleKeyword.None;
                });

                // 点击选择
                option.clicked += () => {
                    SelectOption(choice);
                };

                _optionsContainer.Add(option);
            }
        }

        /// <summary>
        /// 切换下拉框显示状态
        /// </summary>
        private void ToggleDropdown()
        {
            if (_isOpen)
            {
                CloseDropdown();
            }
            else
            {
                OpenDropdown();
            }
        }

        /// <summary>
        /// 打开下拉框
        /// </summary>
        private void OpenDropdown()
        {
            if (_isOpen) return;

            // 计算位置
            var buttonRect = _triggerButton.worldBound;
            float posX = buttonRect.x;
            float posY = buttonRect.y + buttonRect.height;

            // 设置位置和尺寸
            _dropdown.style.left = posX;
            _dropdown.style.top = posY;
            _dropdown.style.width = Math.Max(buttonRect.width, 100);
            _dropdown.style.display = DisplayStyle.Flex;
            
            // 确保下拉菜单在父容器可视范围内
            EnsureDropdownVisibility();

            _isOpen = true;
        }

        /// <summary>
        /// 确保下拉菜单在父容器可视范围内
        /// </summary>
        private void EnsureDropdownVisibility()
        {
            if (_dropdownContainer == null || _dropdown == null) return;
            
            // 等待一帧以获取正确的布局信息
            _dropdown.schedule.Execute(() => {
                var containerRect = _dropdownContainer.worldBound;
                var dropdownRect = _dropdown.worldBound;
                
                // 检查底部边界
                float bottomOverflow = dropdownRect.yMax - containerRect.yMax;
                if (bottomOverflow > 0)
                {
                    // 如果超出底部边界，向上移动
                    float currentTop = _dropdown.style.top.value.value;  // 获取 Length 值的实际 float 值
                    float newTop = Math.Max(0, currentTop - bottomOverflow);
                    _dropdown.style.top = newTop;
                }
                
                // 检查右侧边界
                float rightOverflow = dropdownRect.xMax - containerRect.xMax;
                if (rightOverflow > 0)
                {
                    // 如果超出右侧边界，向左移动
                    float currentLeft = _dropdown.style.left.value.value;  // 获取 Length 值的实际 float 值
                    float newLeft = Math.Max(0, currentLeft - rightOverflow);
                    _dropdown.style.left = newLeft;
                }
                
                // 检查左侧边界
                if (dropdownRect.x < containerRect.x)
                {
                    _dropdown.style.left = containerRect.x;
                }
                
                // 检查顶部边界
                if (dropdownRect.y < containerRect.y)
                {
                    _dropdown.style.top = containerRect.y;
                }
            }).ExecuteLater(10);  // 延迟10毫秒执行，确保布局已更新
        }

        /// <summary>
        /// 关闭下拉框
        /// </summary>
        private void CloseDropdown()
        {
            if (!_isOpen) return;
            
            _dropdown.style.display = DisplayStyle.None;
            _isOpen = false;
        }

        /// <summary>
        /// 选择选项
        /// </summary>
        private void SelectOption(string option)
        {
            if (_currentValue != option)
            {
                _currentValue = option;
                _triggerButton.text = option;
                
                // 触发值变更事件
                OnValueChanged?.Invoke(option);
            }
            
            CloseDropdown();
        }
    }
}