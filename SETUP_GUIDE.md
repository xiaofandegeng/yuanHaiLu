# 渊海录 — Unity 工程搭建与验证指南

## 1. 环境要求

| 项目 | 要求 |
|------|------|
| Unity | `6000.4.10f1` |
| 模板/渲染 | 2D Core / 内置 2D，**不是 URP** |
| 推荐平台 | macOS Apple Silicon |
| 输入 | Legacy Input Manager |
| 测试 | Unity Test Framework `1.6.0` |

本仓库已经是完整 Unity 项目，不需要新建工程或复制脚本。用 Unity Hub 直接打开仓库根目录即可。

## 2. 首次打开

1. Unity Hub → **Open** → 选择 `yuanHaiLu` 根目录。
2. 确认编辑器为 `6000.4.10f1`。
3. 等待 `Library/` 首次导入和 C# 编译完成。
4. Console 中不应存在编译错误。

如果修改过 `ProjectSettings/InputManager.asset`、`TagManager.asset` 或其他 `ProjectSettings/*.asset`，请关闭并重新打开 Unity，确保配置被重新加载。

## 3. 场景与运行

仓库维护两个可运行场景：

```text
Assets/Scenes/MainMenu.unity
Assets/Scenes/Demo_YanLiuTown.unity
```

需要重建时使用：

```text
Tools → 渊海录 → 生成主菜单场景
Tools → 渊海录 → 生成Demo场景
```

在 **File → Build Profiles**（部分界面仍显示 Build Settings）中按顺序加入：

```text
0  MainMenu
1  Demo_YanLiuTown
```

日常运行可从 `MainMenu` 开始；需要快速调试场景时也可直接打开 `Demo_YanLiuTown`，它会按新游戏入口初始化。

## 4. 项目设置基线

### 4.1 物理 Layer

`ProjectSettings/TagManager.asset` 中固定为：

```text
6  Player
7  Enemy
8  NPC
9  Environment
```

不要新增或改名为 `Interactable`：交互通过 `IInteractable` 组件过滤，NPC 使用 `NPC` 层。

### 4.2 Sorting Layer

从后到前必须为：

```text
Ground → Environment → Character → Foreground → UI
```

这些名字与 `GameConfig.SORTING_*` 常量直接对应，拼写不一致会让渲染静默落到默认层。

### 4.3 像素规格

| 设置 | 值 |
|------|-----|
| 内部分辨率 | `480 × 270` |
| Pixels Per Unit | `16` |
| 瓦片 | `16 × 16` |
| 角色帧 | `48 × 48` |
| Filter Mode | Point |
| Compression | None |
| Anti Aliasing | Disabled |
| VSync | Don't Sync |

可用 `Tools → 渊海录 → 配置所有精灵为像素模式` 批量修正导入设置。

### 4.4 输入轴

实际配置位于 `ProjectSettings/InputManager.asset`：

| 动作 | 主键 | 备用键 |
|------|------|--------|
| Horizontal / Vertical | 方向键 | WASD |
| Attack | J | Left Ctrl |
| Dash | Left Shift | Space |
| Interact | K | E |
| Submit | Return / Enter | Space |
| Cancel | Escape | 手柄 Button 1 |

背包 Tab、任务 Q、武学 1–4 等功能直接读取 `KeyCode`。对话/暂停状态由 `GameManager` 输入门阻止战斗和武学误触。

## 5. 运行时对象约定

主菜单和游戏场景都通过 `GlobalSystemsBootstrapper` 补齐以下持久化管理器：

```text
GameManager
├── SaveManager
├── InventoryManager
├── QuestManager
├── GameTimeManager
└── DialogueManager
```

`AudioManager` 由场景入口单独保障。不要在新场景中实现另一套管理器创建逻辑。

玩家对象至少需要：

```text
SpriteRenderer
Animator
Rigidbody2D
BoxCollider2D
PlayerController
CharacterStats
PlayerCombat
PlayerInteraction
MartialArtsSystem（武学功能需要）
```

`PlayerInteraction.EnsureOn` 是幂等入口；场景生成器、`SceneBootstrapper` 和 `SceneDirector` 都会调用它，因此既有 Demo 场景无需重新生成即可获得 K/E 交互。

## 6. Animator 约定

当前脚本会写入下列参数，但仓库尚未提供正式 Animator Controller 和动画剪辑：

```text
MoveX       Float
MoveY       Float
Speed       Float
IsDashing   Bool
IsAttacking Bool
AttackIndex Int
```

创建控制器时必须沿用这些名字；攻击判定由动画事件调用 `PlayerCombat.OnAttackHitFrame()`。

## 7. 自动测试

在 macOS 上运行全部 EditMode 测试：

```bash
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /absolute/path/to/yuanHaiLu \
  -runTests \
  -testPlatform EditMode \
  -testResults /tmp/yuanHaiLu-editmode.xml \
  -logFile /tmp/yuanHaiLu-editmode.log
```

注意：`-runTests` 命令不要附加 `-quit`。仅做编译/导入检查时才使用：

```bash
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /absolute/path/to/yuanHaiLu \
  -quit \
  -logFile /tmp/yuanHaiLu-compile.log
```

## 8. Play 验证清单

重启 Unity 后至少验证：

- [ ] 主菜单“新游戏”进入 `Demo_YanLiuTown`。
- [ ] NPC 附近出现提示，K 与 E 都能开始对话。
- [ ] 自动事件不显示交互提示；按键事件可以触发且一次性事件不会重复出现。
- [ ] J 攻击、Shift 冲刺、数字键武学在探索状态可用。
- [ ] 存档后修改位置、HP/MP、背包、装备、金钱、武学和已完成任务，读档可精确恢复。
- [ ] 读档不追加初始物资、不覆盖出生点；卸下装备后属性正确。
- [ ] 再次加载其他场景不会重复应用旧存档。

活跃任务和世界状态目前不在存档范围，不能把它们列为通过项。

## 9. 常见问题

- **找不到场景**：确认 Build Profiles 中名字为 `Demo_YanLiuTown`，不是 `YanLiuTown`。
- **K/E 无响应**：重启 Unity，并检查 `InputManager.asset` 中只有一个 `Interact` 轴。
- **NPC 无法检测**：确认物理层为 `NPC`，Collider2D 可进入交互半径，组件实现 `IInteractable`。
- **物品 ID 找不到**：`InventoryManager` 先加载 `ItemDatabase` 代码表，再用 `Resources/Items` 下同 ID 的 SO 覆盖。
- **读档后属性叠加**：v2 存档应保存基础属性；装备加成由背包恢复后统一重算。
- **大量命名空间错误**：系统代码必须使用 `YuanHaiLu.GameSystem`，不要使用 `YuanHaiLu.System`。
