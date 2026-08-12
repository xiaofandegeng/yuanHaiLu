# 剩余修复 + Git 初始化 + 交接文档

## A. 代码修复（4 项）

### 1. 清理重复 using（🟢 零风险）
- `Assets/Scripts/Dialogue/DialogueManager.cs`：删除第 5 行重复的 `using YuanHaiLu.Character;`
- `Assets/Scripts/System/SaveManager.cs`：删除第 4 行重复的 `using YuanHaiLu.Core;`
（上次因 plan mode 写入受限未完成。）

### 2. 武学快捷键与对话数字键冲突（🟡 bug）
- **问题**：`MartialArtsSystem.HandleSkillInput()`（数字键 1-4 释放技能）与 `DialogueManager`（数字键 1-9 选对话分支）共用按键；`MartialArtsSystem` 已挂载在 Demo 玩家上，对话中按 1 会误触技能。
- **修复**：在 `MartialArtsSystem.Update()` 顶部加守卫 —— 当 `GameManager` 处于非 `CanPlayerAct()` 状态（对话/暂停/菜单/过场）时，跳过技能输入读取。冷却计时仍继续。

### 3. 武学学习链路接通（🟡 功能缺失）
- **问题**：`InventoryManager.LearnSkill(ItemData book)` 只 `Debug.Log`，从不真正让玩家学会武学；且它仍引用已 `[Obsolete]` 的 `CharacterStats.learnedSkills`。
- **修复**：改为通过 `MartialSkillDatabase.Get(book.teachSkillId)` 取招式 → 调用玩家的 `MartialArtsSystem.LearnSkill(skill)`（该方法已实现：含已学检查、自动装备到空槽）。删除对废弃字段的引用。

### 4. 装备属性加成生效（🟡 功能缺失）
- **问题**：`InventoryManager.ApplyEquipmentStats()` 是空壳（只有 TODO）；装备武器/防具不提供任何属性。
- **修复**（引入"基础值 + 装备加成"分离，内部自洽）：
  - **`CharacterStats.cs`**：新增 `_base*`（Awake 时从序列化字段捕获）与 `_eq*`（装备加成）私有字段；新增 `RecomputeDerived()`、`SetEquipmentBonus(atk,def,agi,hp,mp)`；`LevelUp()` 改为修改 `_base*` 后重算；新增 `SetBaseFromLoad(...)` 供读档使用。
  - **`InventoryManager.ApplyEquipmentStats()`**：遍历已装备的 weapon/armor/accessory，累加 `bonusAttack/Defense/Agility/MaxHp/MaxMp`，调用 `stats.SetEquipmentBonus(...)`。
  - **`SaveManager.LoadGame`**：把对 `stats.attack/defense/...` 的直接赋值改为 `stats.SetBaseFromLoad(...)`（读档时视为无装备，基础值=存档总值；与"背包未接入存档"的现状一致）。
  - 保留 `Die()` 中对 `currentHp<=0` 的判断语义不变。

> 说明：背包/任务/武学的**存档**目前本就未接入 `SaveManager`（既有缺口），本次不扩展存档范围，仅在 AGENTS.md 记录。

## B. Git 初始化

1. 创建 Unity 标准 `.gitignore`：忽略 `Library/ Temp/ Obj/ Build/ Builds/ Logs/ UserSettings/ MemoryCaptures/`、`*.csproj *.sln *.slnx *.user`、`.DS_Store`、`unity_*.log`、构建产物（`*.app *.apk *.ipa *.unitypackage`）；**保留 `.meta`**。
2. 清理根目录垃圾：删除 16 个 `unity_*.log`（旧编译/导入日志）与 `.DS_Store`。
3. `git init` → `git add -A` → 初始提交 `chore: initial commit (渊海录 Demo)`，建立基线分支。
4. 提供 GitHub 推送命令（用户自行执行 push）。

## C. 交接文档 / 项目记忆

创建 **`AGENTS.md`**（项目根，未来 AI 接手首选入口），内容：
- 项目速览（引擎/类型/规模/状态）
- 如何运行（生成场景 / 操作键 / 已修复的阻塞性问题）
- 架构总览（命名空间分层、单例管理器、状态机、事件驱动、程序化场景生成）
- 关键约定（像素规格、SortingLayer、Layer、输入轴、脚本存放）
- **已知问题与未完成项**（装备存档、交互键系统、占位美术、Animator 等，含优先级）
- 开发流程（编辑器工具菜单、修改 `.asset` 需重启 Unity、无测试套件）
- 文件地图（文档/脚本/资源位置）
- 本次修复历史记录

## 验证
- `grep` 复核 4 处代码改动 + `.gitignore` 生效（`git status` 不含 Library/日志）。
- 无法在此环境启动 Unity，将提供 Unity 内 Play 验证清单。
