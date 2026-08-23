# 外部 AI 开发完成后的独立审查与验证计划

> 本文由最终验收者执行。开发 AI 不得用自己的实现说明替代这里的独立验证。

开发完成后，可用以下消息触发复审：

```text
外部 AI 已完成。待审分支：<branch>，最终提交：<commit>，开发基线：<main-commit>。
请严格按 docs/05-post-development-review-plan.md 独立验证，不要直接合并。
```

## 1. 启动条件

只有开发 AI 提供以下信息后才开始最终审查：

- 基线 `main` 提交哈希。
- 待审分支和最终 HEAD。
- 工作树干净证明。
- 完整提交列表与 `git diff --stat main...HEAD`。
- Python、EditMode、PlayMode 结果文件及日志的绝对路径。
- 十一个最终视觉截图、基线路径和人工 Play QA 记录。
- 明确列出的剩余风险。

缺少任何一项时，审查状态记为“证据不足”，不进入合并阶段。

## 2. 固定审查范围

审查开始时立即记录：

```bash
rtk git rev-parse main
rtk git rev-parse <待审分支>
rtk git merge-base main <待审分支>
rtk git status --short --branch
```

要求：待审分支必须与 `main` 有正常共同祖先，且 merge-base 等于开发时声明的基线或可解释的更新后基线。禁止审查或合并 `codex/full-art-production` 本身。

## 3. 第一阶段：仓库完整性审查

### 3.1 分支与污染检查

```bash
rtk git diff --check main...<待审分支>
rtk git diff --name-status main...<待审分支>
rtk git ls-files | rtk rg '(^|/)(Library|Temp|\.zcode|\.vscode|docs/superpowers)/'
```

必须满足：

- 没有 `Library/`、`Temp/`、日志、本地编辑器或 AI 会话文件。
- 没有无关 `ProjectSettings` 平台最低版本变化。
- 没有 `MobileDependencyResolver/*.pdb.meta` 删除。
- 所有新增/删除 Unity 资源都成对包含 `.meta`。
- `Packages/manifest.json` 的新增包有代码上的实际使用和锁文件对应项。

### 3.2 兼容契约检查

逐项静态确认：

- `PlayerAppearance.DefaultArtId == "player_female_swordsman"`。
- v1–v3 外观迁移返回相同默认值；v4 非法 ID 告警后回退。
- `SaveData.saveVersion` 仍为 4，既有字段语义不变。
- `QuestTarget` 只有 `UpdateObjective` 返回 true 后才设置 `_reported=true`。
- `AreaTrigger` 跨场景后保留玩家运行时状态，并由目标场景引导系统恢复输入。
- 正式 2D 摄像机 z 为 `-10` 或严格小于 `-nearClipPlane`。

任一不满足均为 P1，退回开发 AI 修复。

## 4. 第二阶段：双轴代码审查

### Standards 轴

固定点使用 `main` merge-base；检查项目约定、Unity 生命周期、空引用、重复单例、序列化、`.meta`、命名空间和文档真实性。重点文件：

- `Assets/Scripts/System/GlobalSystemsBootstrapper.cs`
- `Assets/Scripts/Map/AreaTrigger.cs`
- `Assets/Scripts/UI/MainMenu.cs`
- `Assets/Scripts/System/SaveManager.cs`
- `Assets/Scripts/Art/RegionEnvironmentController.cs`
- `Assets/Scripts/Editor/Art/*.cs`

### Spec 轴

逐条对照 `docs/04-external-ai-development-handoff.md` 的全局约束和 Task 1–8 验收条件，另外确认：

- 精确 97 角色：12 Player、15 Named、36 NPC、24 Enemy、10 Boss。
- 97 个 Controller 和 Prefab 全量存在并可解析。
- 四方向 idle/walk/dash/三连击可达，攻击帧真实造成伤害。
- 总览场景有 97 个 stable ID 标签、10 类动作入口和 1×/4×/8×缩放。
- 正式场景为 10 户外 + 13 室内，Build Settings 总计 25。
- 23 个场景道路、装饰、建筑、前景和碰撞来自各自布局，不是统一公式模板。
- 十个户外结构坐标签名不同。
- 序章村庄具有正常/焚毁状态，切换不改变锚点和碰撞。
- 场景转移图能到达所有 23 个正式场景。
- MainMenu 选角只有确认后写入，取消恢复原值，键盘焦点正确。
- 正式路径不存在运行时色块回退。
- MainMenu 与十个户外场景具备获批视觉基线。

所有 P0/P1 必须修复或由用户明确接受；不得把 Spec 缺口混写成一般代码风格建议。

## 5. 第三阶段：独立自动验证

审查者必须重新运行，不能只读取开发 AI 的日志。

### 5.1 Python 美术流水线

```bash
rtk python3 -m unittest discover -s tools/art_pipeline/tests -v
rtk python3 -m tools.art_pipeline.build --all
rtk python3 -m tools.art_pipeline.validate --all
```

通过条件：测试失败数为 0；第二次 build 不产生未预期重建；validator 无缺失、哈希、尺寸或额外资源错误。

### 5.2 Unity EditMode

```bash
rtk /Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath /Users/lhw/code/yuanHaiLu \
  -runTests -testPlatform EditMode \
  -testResults /private/tmp/yuanhailu-review-editmode.xml \
  -logFile /private/tmp/yuanhailu-review-editmode.log
```

通过条件：XML `failed="0"`；Console 无 C# 编译错误、MissingReference、MissingComponent 或 Animator 参数告警刷屏。

### 5.3 Unity PlayMode

把上一命令平台改为 `PlayMode`，结果写入 `/private/tmp/yuanhailu-review-playmode.xml`，日志写入 `/private/tmp/yuanhailu-review-playmode.log`。

必须覆盖：菜单→Demo、真实攻击命中、正式场景直开、烟柳镇→客栈、玩家实例/HP/等级/输入保持、天气/昼夜、存档外观往返。

### 5.4 场景重建确定性

在保存树状态后连续重建角色与 23 个场景两次，第二次执行后：

```bash
rtk git status --short
```

通过条件：第二次生成没有产生新的非预期差异；若 Unity 序列化时间戳导致变化，生成器必须修到稳定，不能忽略。

## 6. 第四阶段：视觉复审

### 6.1 禁止自动批准基线

先把实际截图输出到 `/private/tmp/yuanhailu-review-visual/`。不得先运行覆盖仓库基线的命令。

### 6.2 自动比较

- 图片尺寸必须为 `480×270`。
- 同输入连续捕获必须字节一致。
- 与获批基线相比，非视觉改动的 changed pixel ratio 必须 `<=0.5%`。
- 超过阈值时保存差异图，并定位是场景变化、相机变化、字体、导入设置还是随机性。

### 6.3 人工看图

按 MainMenu、prologue_village、luoyuan、tianshu、yanliu、cangyue、jueyun、chisha、youhuang、hanyuan、zhenyue 的顺序，在 1× 和 4× 检查：

- 无纯色角色、整张 Sprite Sheet 拉花或丢图。
- 十个区域不仅靠整体换色区分。
- 路线、建筑、地标和前景层次清晰。
- 主角与敌人轮廓在地面和夜色下可读。
- 雨、雪、沙、余烬等表现与天气 ID 一致且不会遮挡玩法。
- 序章村庄正常/焚毁状态可明显区分。

任何一项失败均退回开发，不更新基线。

## 7. 第五阶段：可见窗口人工 Play 验证

在 Unity `6000.4.10f1` 中从 `MainMenu.unity` 开始：

1. 用键盘打开选角，方向键选择，取消后确认原外观未改变。
2. 再次打开，选择任一非默认职业并确认进入 Demo。
3. 检查正式角色、完整烟柳镇、HUD 和像素视口。
4. 验证 WASD/方向键移动、J 三连击伤害、Shift 冲刺、K/E NPC 对话、ESC 暂停。
5. 从烟柳镇进入客栈，再返回烟柳镇；核对落点、外观、HP、MP、等级、武学和输入。
6. 保存，返回主菜单，读档；核对位置、属性、背包、装备、金钱、武学和任务进度。
7. 直开一个户外和一个室内正式场景；确认摄像机可见、状态为 Exploration、玩家可操作。
8. 切换序章村庄焚毁状态；确认锚点和碰撞不变。

人工 QA 必须记录日期、Unity 版本、每项结果和截图路径。Mac 锁屏、许可证故障或无法操作 GUI 时，验收状态只能写“未完成”。

## 8. 失败处理与复验规则

| 严重级别 | 处理 |
|---|---|
| P0 | 立即停止合并；开发 AI 修复后从双轴审查重新开始 |
| P1 | 阻止合并；补回归测试，修复后重跑相关测试及全量测试 |
| P2 | 可在用户明确接受后记录为后续项；不得静默忽略 |

修复提交必须追加到同一待审分支。复验固定点仍是 `main` merge-base，避免只审最后一个补丁而漏掉前序变化。

## 9. 最终交付门

同时满足以下条件才建议合并：

- 待审分支基于 `main` 且工作树干净。
- Standards 与 Spec 两轴没有未解决 P0/P1。
- Python、EditMode、PlayMode、编译、场景重建和视觉回归均通过。
- 可见窗口人工 Play QA 全部完成。
- 文档中的测试数量来自最新 XML，完成状态与实际一致。
- `git diff --check` 通过，无无关配置或本地文件。
- 用户明确授权合并。

合并后在 `main` 再运行一次 Python、EditMode、PlayMode 和 2 分钟主流程冒烟；最终报告必须给出合并提交、测试结果、证据路径与剩余 P2。
