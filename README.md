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
2–11  Assets/Scenes/Regions/*.unity（10 个户外区域）
12–24 Assets/Scenes/Interiors/*.unity（13 个室内场景）
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
- 稳定任务模板、运行时目标深复制、NPC 接取/提交、玩法目标上报与幂等奖励结算。
- v3 存档：玩家身份/等级/位置/基础属性/当前 HP、MP、背包、装备、金钱、武学、活跃任务进度和已完成任务。
- 新游戏、读档、普通运行使用显式场景进入模式，读档回调单次执行。
- 主菜单运行时补齐全局管理器并绑定按钮。
- 主菜单可选择 2 种性别 × 6 种职业；稳定角色 ID 写入 v4 存档并跨场景保持。
- 97 套正式角色资源：12 主角、15 剧情角色、36 NPC、24 敌人、10 BOSS；全部有独立 PNG、动画 Controller 与 Prefab。
- 10 个户外区域、13 个室内场景、23 个正式环境场景；统一 7 层 Tilemap、地标、锚点和持久化 Tile 资源。
- 十个户外区域使用独立的地标构图和区域专属地形簇；序章村庄可在保持锚点/碰撞的前提下切换 normal / burned 环境状态。
- 烟柳镇 Demo 已接入正式 Tilemap、桥、水岸、建筑、角色、NPC、敌人、战斗、交互与 UI，不再生成色块占位地图。
- 确定性美术流水线：稳定 PNG/JSON 源、Python baker、SHA-256 校验、Unity 精确切片和目录校验。
- v4 存档：在 v3 任务/背包/武学基础上增加正式主角外观稳定 ID，旧档迁移到女剑客。
- 编辑器菜单可重建美术、角色动画/Prefab、23 个场景、主菜单和烟柳镇 Demo。

当前存档仍不包含敌人/唯一拾取物/一次性事件状态和其他世界标志。`M01_01`–`M01_05` 模板已可运行，但尚未全部配置进烟柳镇场景。

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
├── Tests/EditMode/   Unity Test Framework 编辑器测试
├── Tests/PlayMode/   Unity Test Framework 运行时测试
├── ArtSource/        可编辑 PNG/JSON、角色模块、环境布局和清单
├── Art/              97 套角色、23 套环境输出、预览与哈希元数据
├── Prefabs/Characters/           97 个正式角色 Prefab
├── AnimatorControllers/Characters/ 97 个角色 Controller
├── Tilemaps/Formal/  23 套持久 Tile 资源
├── Scenes/           MainMenu、Demo、10 Regions、13 Interiors、2 Showcase
└── Resources/Art/    CharacterArtCatalog + EnvironmentArtCatalog
```

当前运行时/编辑器代码为 68 个 C# 文件；另有 19 个测试/测试工具文件。

## 自动验证

项目使用 Unity Test Framework `1.6.0`，当前全量结果为 **101 个 EditMode + 7 个 PlayMode 全通过**；Python 美术流水线另有 **45 个测试**。覆盖：

- 场景进入模式；
- 背包/装备和资源值恢复；
- 武学、活跃/已完成任务的替换式恢复；
- v4 JSON 往返、外观迁移、损坏任务进度诊断和 v2/v3 迁移；
- 任务模板不可变、运行时目标、奖励幂等及各玩法目标来源；
- NPC 对话结束后接取/推进/提交任务的真实 PlayMode 事件链；
- 交互组件幂等接入；
- 全局系统幂等补全。
- 主菜单鼠标/键盘入口、像素摄像机清屏与边界；
- Demo 摄像机位置、默认暂停面板、缺失音效去重；
- 无 Animator Controller 时的运行时兼容。
- 97 个角色目录计数、Prefab/Controller、代表角色 idle→walk 动画；
- 23 个正式环境场景、7 层 Tilemap、持久化地面/结构 Tile、锚点可达性；
- 主菜单 12 套外观选择、菜单→Demo 外观保持、正式烟柳镇场景绑定；
- GameManager 尚未引导时 PlayerCombat 的空值安全。
- 序章 normal/burned 环境状态的 Tile/地标替换与导航不变量。
- 固定 `480×270` 视觉截图及其场景、Canvas 与渲染状态恢复。

确定性资源验证：

```bash
python3 -m unittest discover -s tools/art_pipeline/tests -v
python3 -m tools.art_pipeline.build --all   # 正常应 built=0 skipped=121
python3 -m tools.art_pipeline.validate --all
```

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

运行 PlayMode 测试时把 `-testPlatform EditMode` 改为 `-testPlatform PlayMode`，并使用不同的结果/日志文件。

## 编辑器工具

| 菜单 | 功能 |
|------|------|
| Tools/渊海录/生成主菜单场景 | 重建主菜单 |
| Tools/渊海录/生成Demo场景 | 重建烟柳镇 Demo |
| Tools/渊海录/初始化项目设置 | 配置 Tags、Layers、SortingLayers |
| Tools/渊海录/配置所有精灵为像素模式 | 批量配置 Point/无压缩 |
| Tools/渊海录/美术/重建角色动画与Prefab | 重建 97 个角色 Controller/Prefab |
| Tools/渊海录/美术/生成全部正式环境场景 | 重建 10 户外 + 13 室内 |
| Tools/渊海录/美术/生成环境总览场景 | 重建环境 Showcase |
| Tools/渊海录/美术/截取正式烟柳镇预览 | 生成实际相机验收图 |
| Tools/渊海录/美术/截取临时正式美术验收图 | 输出主菜单、10 户外及序章焚毁态的 480×270 临时审查图 |
| Tools/渊海录/切分角色精灵表(48×48) | 角色切片 |
| Tools/渊海录/切分瓦片集(16×16) | 瓦片切片 |

## 主要待办

- 在当前模块化正式像素资源基础上继续人工精修细节、动作节奏和区域差异。
- 将物品/任务设计稿制作成正式 ScriptableObject 资源。
- 把 `M01_01`–`M01_05` 的发布者、目标和奖励物配置进烟柳镇场景。
- 持久化敌人、唯一拾取物、一次性事件和其他世界状态。
- 增加商店 UI、武学树、小地图、BOSS 玩法和 BGM/SFX。

更完整的架构、约定、已知限制和修复历史见 `AGENTS.md`。
