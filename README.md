# 渊海录（YuanHaiLu）— Unity 6 像素武侠 RPG

> Demo 阶段 | Unity `6000.4.10f1` | 俯视角 2D Pixel Art | 非 URP

## 快速开始

1. 用 Unity Hub 打开本仓库，编辑器版本必须为 `6000.4.10f1`。
2. 等待首次导入和编译完成。
3. 直接打开 `Assets/Scenes/MainMenu.unity` 或 `Assets/Scenes/Demo_YanLiuTown.unity`。
4. 按 **Play**。

如果需要重建场景：

```text
Tools → 渊海录 → 生成主菜单场景
Tools → 渊海录 → 生成Demo场景
```

Build Profiles / Build Settings 中应包含：

```text
0  Assets/Scenes/MainMenu.unity
1  Assets/Scenes/Demo_YanLiuTown.unity
```

## 操作说明

| 按键 | 功能 |
|------|------|
| WASD / 方向键 | 8 方向移动 |
| J | 攻击（3 连击，可暴击） |
| K / E | 交互（NPC、事件、传送点等） |
| Left Shift | 冲刺 |
| 数字键 1–4 | 释放已装备武学 |
| Space / Enter | 推进对话 |
| 数字键 1–9 | 选择对话分支 |
| Tab | 背包 |
| Q | 任务日志 |
| ESC | 暂停菜单 |

`Interact` 使用 Legacy Input Manager，主键为 K、备用键为 E。修改 `ProjectSettings/*.asset` 后必须重启 Unity。

## 当前能力

- 8 方向移动、冲刺、Y 轴排序。
- 三连击、暴击、剑气、敌人状态机和特效池。
- K/E 最近目标交互，统一支持 `IInteractable`。
- 打字机对话、条件、动作和分支选择。
- 背包、装备、金钱、任务、武学、商店、掉落和昼夜框架。
- v2 存档：玩家身份/等级/位置/基础属性/当前 HP、MP、背包、装备、金钱、武学和已完成任务。
- 新游戏、读档、普通运行使用显式场景进入模式，读档回调单次执行。
- 主菜单运行时补齐全局管理器并绑定按钮。
- 编辑器菜单可程序化生成主菜单和烟柳镇 Demo。

当前存档仍不包含活跃任务进度、敌人/拾取物状态和其他世界标志。

## 项目结构

```text
Assets/
├── Scripts/
│   ├── Core/         游戏状态、摄像机、场景引导
│   ├── Character/    玩家、NPC、敌人、属性、武学、交互
│   ├── Map/          区域、传送、事件、破坏物、拾取物
│   ├── Dialogue/     对话系统
│   ├── Effects/      命中特效、剑气、伤害数字
│   ├── System/       背包、任务、存档、音频、商店、昼夜
│   ├── UI/           HUD、主菜单、背包、任务、暂停、对话
│   ├── Combat/       战斗计算预留
│   └── Editor/       场景生成和项目配置工具
├── Tests/EditMode/   Unity Test Framework 测试
├── Scenes/           MainMenu + Demo_YanLiuTown
├── Resources/        物品/任务设计稿及后续 SO 资源位置
├── Sprites/          占位精灵
└── Art/              美术参考与瓦片资源
```

当前运行时代码为 48 个 C# 文件、约 10,600 行；另有 5 个 EditMode 测试/工具文件。

## 自动验证

项目使用 Unity Test Framework `1.6.0`，当前有 11 个 EditMode 测试，覆盖：

- 场景进入模式；
- 背包/装备和资源值恢复；
- 武学与已完成任务的替换式恢复；
- v2 JSON 往返和旧存档迁移；
- 交互组件幂等接入；
- 全局系统幂等补全。

命令行运行全部测试：

```bash
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /absolute/path/to/yuanHaiLu \
  -runTests \
  -testPlatform EditMode \
  -testResults /tmp/yuanHaiLu-editmode.xml \
  -logFile /tmp/yuanHaiLu-editmode.log
```

使用 `-runTests` 时不要同时传 `-quit`，否则 Unity 可能在 Test Runner 写结果前退出。

## 编辑器工具

| 菜单 | 功能 |
|------|------|
| Tools/渊海录/生成主菜单场景 | 重建主菜单 |
| Tools/渊海录/生成Demo场景 | 重建烟柳镇 Demo |
| Tools/渊海录/初始化项目设置 | 配置 Tags、Layers、SortingLayers |
| Tools/渊海录/配置所有精灵为像素模式 | 批量配置 Point/无压缩 |
| Tools/渊海录/切分角色精灵表(48×48) | 角色切片 |
| Tools/渊海录/切分瓦片集(16×16) | 瓦片切片 |

## 主要待办

- 替换程序生成的占位美术。
- 建立 Animator Controller 和动画剪辑。
- 将物品/任务设计稿制作成正式 ScriptableObject 资源。
- 持久化活跃任务和世界状态。
- 增加商店 UI、武学树、小地图、BOSS、BGM/SFX 和后续区域。

更完整的架构、约定、已知限制和修复历史见 `AGENTS.md`。
