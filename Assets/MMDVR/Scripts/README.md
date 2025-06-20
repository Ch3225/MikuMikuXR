# MMDVR Scripts 架构说明

## � 概述

MMDVR项目采用了**三层管理器架构**，实现了清晰的职责分离。每个管理器专注于特定的功能领域，并通过专门的事件系统进行通信。

## 🏗️ 核心架构

### 三层管理器体系

```
📁 Managers/
├── 🎛️ SystemStateManager      # 系统状态管理器
├── 📦 ResourceManager         # 资源管理器
├── 🎬 SceneDisplayManager     # 场景展示管理器
├── ▶️ PlaybackManager         # 播放控制管理器
├── 🖥️ UIManager               # UI管理器
├── 🎯 EventManager            # 事件协调管理器
└── ⚠️ SceneStatesManager      # 桥接层（已废弃）
```

### 管理器职责分工

| 管理器 | 主要职责 | 核心功能 |
|--------|----------|----------|
| **SystemStateManager** | 系统级状态管理 | VR/桌面切换、设备检测、摄像机引用 |
| **ResourceManager** | 资源生命周期管理 | 资源加载、卸载、存储管理 |
| **SceneDisplayManager** | 场景展示控制 | 场景资源展示、Actor管理、激活控制 |
| **PlaybackManager** | 播放状态管理 | 播放控制、进度管理、同步协调 |
| **UIManager** | 用户界面管理 | UI适配、界面控制、交互处理 |
| **EventManager** | 事件协调中心 | 事件分发、向后兼容、统一接口 |

### 事件系统架构

```
📁 Events/
├── �️ InputEvents       # 输入相关事件
├── 📦 ContentEvents     # 内容管理事件
├── 🖥️ UIEvents          # UI交互事件
├── 🎛️ SystemEvents      # 系统级事件
└── ▶️ PlaybackEvents    # 播放控制事件
```

## 🔄 管理器与事件系统对应关系

| 管理器 | 对应事件类 | 主要事件 |
|--------|------------|----------|
| SystemStateManager | SystemEvents | VR模式检测、设备状态变化 |
| ContentManager | ContentEvents | 资源列表更新、关联变化 |
| PlaybackManager | PlaybackEvents | 播放状态变化、进度更新 |
| - | InputEvents | 输入处理、UI切换 |
| - | UIEvents | UI状态变化、交互响应 |

## 🚀 使用指南

### 推荐使用方式

```csharp
// ✅ 推荐：直接使用专门的管理器
ContentManager.Instance.AddActor(filePath);
PlaybackManager.Instance.Play();
SystemStateManager.Instance.GetActiveCamera();

// ✅ 推荐：使用专门的事件类
ContentEvents.TriggerActorListChanged();
PlaybackEvents.TriggerPlaybackStateChanged();
```

### 避免使用（已废弃）

```csharp
// ⚠️ 废弃：SceneStatesManager（仅为向后兼容保留）
SceneStatesManager.Instance.AddActor(filePath);  // 请使用 ContentManager
SceneStatesManager.Instance.Play();              // 请使用 PlaybackManager
```

## 📦 组件与数据类

### 数据类组织

```
📁 Data/
└── ResourceData.cs          # 资源数据类集合
    ├── MusicData           # 音乐数据
    ├── ActorData           # 演员数据
    ├── ModelData           # 模型数据
    ├── MotionData          # 动作数据
    ├── CameraData          # 摄像机数据
    └── FreeCameraComponent # Free摄像机组件
```

### 组件类组织

```
📁 Components/
├── MusicComponent.cs        # 音乐组件
├── ModelComponent.cs        # 模型组件
├── MotionComponent.cs       # 动作组件
└── MMDCameraComponent.cs    # MMD摄像机组件
```

## 📁 完整文件结构

```
Assets/MMDVR/Scripts/
├── 🔧 Core/                    # 核心功能
│   └── MMDVRInitializer.cs
├── 🧩 Components/              # MMD组件
│   ├── MMDCameraComponent.cs
│   ├── MusicComponent.cs
│   ├── ModelComponent.cs
│   └── MotionComponent.cs
├── � Data/                    # 数据类
│   └── ResourceData.cs
├── �📡 Events/                  # 事件系统
│   ├── InputEvents.cs
│   ├── ContentEvents.cs
│   ├── UIEvents.cs
│   ├── SystemEvents.cs
│   └── PlaybackEvents.cs
├── ⌨️ Input/                   # 输入管理
│   └── KeyboardInputManager.cs
├── 👔 Managers/                # 各类管理器
│   ├── SystemStateManager.cs   # 系统状态管理
│   ├── ContentManager.cs       # 内容资源管理
│   ├── PlaybackManager.cs      # 播放控制管理
│   ├── UIManager.cs            # UI界面管理
│   ├── EventManager.cs         # 事件协调管理
│   └── SceneStatesManager.cs   # 桥接层（已废弃）
├── 🎮 Controls/                # UI控件
├── 🛠️ Editor/                  # 编辑器扩展
├── 🧪 Testing/                 # 测试脚本
├── 🖼️ UI/                      # UI相关脚本
├── 🖱️ UIInteraction/           # UI交互脚本
└── 📖 README.md                # 本文档
```

## 🎯 简化的键盘输入系统

### KeyboardInputManager.cs
简单实用的键盘快捷键管理：
- **Tab**: 切换UI显示/隐藏
- **Space**: 播放/暂停
- **Ctrl+S**: 停止播放
- **R**: 重置相机
- **F12**: 截图
- **F11**: 全屏切换

### 使用方法
1. 在场景中添加 KeyboardInputManager 组件
2. 自动启用默认快捷键
3. 可通过 `SetKeyboardInputEnabled(bool)` 控制启用状态

## � 扩展指南

### 添加新功能

1. **确定职责归属**：新功能属于系统、内容还是播放领域？
2. **选择合适管理器**：在对应的管理器中实现功能
3. **添加相应事件**：在对应的事件类中添加事件
4. **更新UI交互**：在相关UI控制器中处理新功能

### 迁移旧代码

1. **识别功能类型**：确定代码属于哪个管理器的职责范围
2. **使用新API**：将旧的SceneStatesManager调用替换为新的管理器调用
3. **更新事件处理**：使用专门的事件类替代旧的事件系统
4. **测试功能**：确保迁移后功能正常工作

## 📚 详细文档

### 管理器详细说明

- **SystemStateManager**: 管理VR/桌面模式切换、设备检测、主摄像机引用
- **ContentManager**: 管理模型、动作、音乐、摄像机等资源的加载、删除、激活和查询
- **PlaybackManager**: 管理播放状态、进度控制、同步协调、音量控制等
- **EventManager**: 作为事件系统的中心协调器和向后兼容层

### 事件系统详细说明

- **SystemEvents**: 处理系统级事件，如VR检测、应用暂停/退出
- **ContentEvents**: 处理内容相关事件，如资源列表更新、关联变化
- **PlaybackEvents**: 处理播放相关事件，如状态变化、进度更新
- **InputEvents**: 处理输入相关事件，如UI切换、交互响应
- **UIEvents**: 处理UI相关事件，如面板显示/隐藏、状态更新

## ⚠️ 迁移说明

**SceneStatesManager已被重构为桥接层**，所有方法都标记为废弃并转发到相应的专门管理器。这保证了向后兼容性，但建议逐步迁移到新的API。

### 迁移优先级

1. **高优先级**: 新开发的功能直接使用新的管理器
2. **中优先级**: 关键路径上的代码逐步迁移
3. **低优先级**: 测试代码和临时代码可以暂时保持现状

## 🏗️ 命名空间规范

- 核心功能：`MMDVR.Scripts.Core`
- 管理器：`MMDVR.Scripts.Managers`
- 数据类：`MMDVR.Scripts.Data`
- 输入系统：`MMDVR.Scripts.Input`
- UI控件：`MMDVR.Scripts.Controls`
- 组件：`MMDVR.Scripts.Components`
- 编辑器：`MMDVR.Scripts.Editor`
- 测试：`MMDVR.Scripts.Testing`
- 事件系统：`MMDVR.Events` (保持统一)

## 🎯 架构优势

1. **职责清晰**: 每个管理器专注于特定领域，代码更易理解和维护
2. **低耦合**: 通过事件系统解耦，减少直接依赖
3. **易扩展**: 新功能可以轻松添加到对应的管理器中
4. **易测试**: 独立的管理器便于单元测试和集成测试
5. **向后兼容**: 保留桥接层，确保现有代码正常工作

## ✅ 重构成果

- ✅ 统一文件结构到Scripts文件夹
- ✅ 实现三层管理器架构（系统、内容、播放）
- ✅ 创建专门的事件系统对应各管理器
- ✅ 删除冗余的AutoVRCameraSwitcher
- ✅ 重构SceneStatesManager为向后兼容的桥接层
- ✅ 修复所有编译错误并保持功能完整性
- ✅ 规范命名空间和代码组织
- ✅ 简化输入系统，去除过度复杂的功能
