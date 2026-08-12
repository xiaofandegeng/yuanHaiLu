# 交互、存档与场景生命周期实施记录

> 实施日期：2026-08-12  
> 长期项目事实以仓库根目录 `AGENTS.md` 为准。  
> 规格：`docs/superpowers/specs/2026-08-12-interaction-save-lifecycle-design.md`  
> 计划：`docs/superpowers/plans/2026-08-12-interaction-save-lifecycle.md`

## 目标

补齐近期未完成的 K/E 交互和完整存档改动，同时修复新游戏、读档、主菜单进入 Demo 时暴露的生命周期问题，并建立可重复的 Unity EditMode 回归测试。

## 已完成

### 1. 交互

- 新增 `PlayerInteraction`，扫描最近且可用的 `IInteractable`。
- `Interact` 输入轴配置为 K 主键、E 备用键。
- `EventTrigger` 接入 `IInteractable`；自动型事件不参与手动目标选择。
- 玩家组件通过 `PlayerInteraction.EnsureOn` 幂等接入：
  - `DemoSceneGenerator`；
  - `SceneBootstrapper`；
  - `SceneDirector`（保障已保存的 Demo 场景）。
- NPC 物理层从不存在的 `Interactable` 修正为 `NPC`。

### 2. 属性、背包和装备

- `CharacterStats` 暴露基础攻击、防御、敏捷、HP/MP 上限。
- 装备重算区分普通装备和读档恢复：读档只 clamp 当前资源，不治疗。
- `InventoryManager` 将代码 `ItemDatabase` 作为默认数据源，Resources SO 可按 ID 覆盖。
- 背包恢复先清空全部槽位，对空数组、长度不一致和未知 ID 做安全处理。
- 新游戏清空背包/装备并恢复初始金钱。

### 3. 武学与任务

- 武学恢复会先清空已学列表和四个装备槽，空/未知 ID 不阻断加载。
- 已完成任务恢复会去空、去重并替换旧状态。
- 新游戏重置活跃任务与已完成任务。
- 已删除无人使用的 `MartialSkillLegacy` 兼容字段和类型。

### 4. v2 存档

- 保存版本号和基础属性，避免装备加成重复进入基础值。
- 保存/恢复玩家身份、等级、经验、位置、HP/MP、章节、背包、装备、金钱、武学和已完成任务。
- 旧版/过渡版存档可迁移：若旧总属性同时带装备 ID，会先扣除装备加成再统一恢复。
- JSON、场景名、Build Settings、玩家和必要组件都有明确校验/错误日志。
- 修复原逻辑在 `LoadScene` 之后才订阅 `sceneLoaded`，导致同步加载回调丢失的问题。
- 匿名永久回调改为加载前注册的具名单次回调；重复读档先清理旧订阅。

### 5. 场景生命周期与主菜单

- `GameManager.SceneEntryMode` 区分 NewGame / LoadGame / SceneTransition / Active。
- `SceneDirector` 只在新游戏初始化出生点、基础属性、武学和初始物资。
- `GlobalSystemsBootstrapper` 统一补齐 Save/Inventory/Quest/GameTime/Dialogue 管理器。
- 主菜单运行时按按钮名绑定行为，首场景修正为 `Demo_YanLiuTown`。
- 新游戏会清理旧背包/任务会话、恢复玩家名和章节并设置 NewGame 入口。
- 设置面板引用缺失时改为 warning，不再抛空引用。
- 修复主菜单生成器重复添加 `Canvas` 组件的问题。

### 6. 测试与工程化

- 新增 `Assets/Tests/EditMode/YuanHaiLu.EditModeTests.asmdef`。
- 新增测试工具 `TestSceneFactory`，清理真实组件、SO 和静态单例。
- 当前 14 个 EditMode 测试：
  - 场景进入模式 2；
  - 持久化 7；
  - 交互 2；
  - 全局系统与主菜单生命周期 3。
- 分组验证结果：2/2、7/7、2/2、3/3 均通过。
- Unity `-runTests` 命令已确认不能同时使用 `-quit`，否则可能无结果 XML。

## 实施中发现并修复的根因

1. 已保存 Demo 场景不会自动获得新组件，仅修改生成器不够。
2. `SceneDirector` 无入口区分，会覆盖读档位置/属性并重复发物资。
3. 读档回调订阅时机晚于同步 `LoadScene`，并且匿名回调无法解除。
4. 保存派生总属性再恢复装备会造成属性语义不一致。
5. 代码预置物品未注册到 `InventoryManager`，导致 Demo 初始物品和装备 ID 查询失败。
6. 较短存档数组不会清空当前会话的剩余槽位。
7. 武学和任务加载会保留旧会话残余状态。
8. 主菜单只创建部分管理器，进入 Demo 后重复 GameManager 被销毁时会连带丢失场景内系统。
9. 主菜单场景名与 Build Settings 不一致，且生成的按钮没有监听器。
10. 主菜单生成器对同一对象添加两次 `Canvas`。
11. 返回主菜单时持久化 `GameManager` 仍处于探索状态；无效首场景会在报错前清空会话。
12. 装备提高 HP/MP 上限时，当前资源会在装备恢复前被基础上限提前裁剪。
13. 直接 Play Demo 时 `GameManager.Start` 将状态设为主菜单，导致玩家输入被锁定。
14. 编辑器初始化工具仍会创建旧 SortingLayer 和不存在的 `Interactable` Layer；新资源元数据也未应用像素导入基线。

## 明确不在本次范围

- 活跃任务目标进度持久化。
- 敌人、拾取物、宝箱、区域事件和其他世界状态持久化。
- 正式美术、Animator、BGM/SFX 和后续地图内容。
- 推送、合并或发布。

## 最终人工 Play 清单

修改了 `ProjectSettings/InputManager.asset`，因此验证前必须重启 Unity：

- [ ] 主菜单“新游戏”进入 `Demo_YanLiuTown`。
- [ ] K/E 均可触发 NPC 对话、按键事件和传送点。
- [ ] 自动事件不显示提示，一次性事件结束后不可再次选择。
- [ ] 存档后修改位置、HP/MP、背包、装备、金钱、武学、已完成任务，再读档精确恢复。
- [ ] 读档不覆盖出生点、不重复发初始物资。
- [ ] 读档后卸下/更换装备，属性正确变化。
- [ ] 再次加载场景不会重复应用旧存档。
