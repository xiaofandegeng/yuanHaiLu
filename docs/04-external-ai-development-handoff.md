# Full Art Completion and Safe Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在不覆盖 `main` 既有修复的前提下，安全迁移现有完整美术工作树，补齐场景、角色、视觉回归与人工验收缺口，并交付一个可审查、可合并到 `main` 的分支。

**Architecture:** `main` 是唯一权威基线；`codex/full-art-production` 及其工作树只作为素材来源，不允许直接合并。接手 AI 先把经过排除清单过滤的差异迁移到从 `main` 创建的新分支，再按测试先行方式修复规格缺口，最后提交完整证据包供独立复审。

**Tech Stack:** Unity `6000.4.10f1`、C#、Unity Test Framework、Python 3 `unittest`、确定性像素美术流水线、Git worktree。

## Global Constraints

- 所有 shell 命令以 `rtk` 开头。
- 唯一合并目标是 `main`；接手开发分支使用 `codex/` 前缀。
- Unity 项目根目录固定为 `/Users/lhw/code/yuanHaiLu`。
- 迁移来源固定为 `/Users/lhw/code/yuanHaiLu/.worktrees/full-art-production`，来源提交为 `75eb1ad08670bcffed52470975991be5d68fea29`。
- 计划编写时 `main` 固定点为 `83c6aff7c009b3d23c0270889ae0b2e4d85433d8`；开始开发时若 `main` 已前进，以最新 `main` 为基线并在交付记录中写明新哈希。
- 来源分支与 `main` 没有共同祖先；禁止执行 `git merge codex/full-art-production`。
- `AGENTS.md` 是长期事实权威；默认外观和 v1–v3 迁移必须保持 `player_female_swordsman`。
- 不得覆盖 `main` 中 `QuestTarget`“只有成功推进目标才锁定”的修复。
- 不得提交 `Library/`、`Temp/`、日志、`.csproj`、`.sln`、`.zcode/`、`.vscode/` 或 `docs/superpowers/`。
- 不得提交本轮无关的 `ProjectSettings/ProjectSettings.asset` 平台版本变化或 `MobileDependencyResolver/*.pdb.meta` 删除。
- 正式美术禁止运行时创建 `Texture2D`/`Sprite` 色块回退；所有资源必须来自持久 Unity 资产。
- 像素规范固定为内部分辨率 `480×270`、瓦片 `16×16`、角色帧 `32×32`、PPU `16`、Point、Compression None。
- 视觉基线只有在人工看图确认后才允许更新；失败时不得直接覆盖基线图片。

---

## 1. 当前事实快照

| 项 | 当前事实 |
|---|---|
| 权威分支 | `main`，计划编写时 HEAD=`83c6aff` |
| 来源分支 | `codex/full-art-production`，HEAD=`75eb1ad` |
| 来源状态 | 约 547 个受控文件变化、163 个未跟踪路径，包含大量生成的 Controller、Prefab、Tile 与 Scene |
| 来源与 main 关系 | 无共同祖先，不能普通 merge/rebase |
| 最近可复现验证 | Python `35/35`；构建 `built=0 skipped=120`；资产 validator 通过 |
| Unity 历史证据 | 较早版本 `90/90 EditMode`、`9/9 PlayMode`；不是当前最终差异的全量结论 |
| 未验收项 | 最新 Unity 全量测试、最新视觉基线、可见窗口人工 Play QA |

## 2. 文件职责图

| 范围 | 主要文件 | 职责 |
|---|---|---|
| 角色运行时 | `Assets/Scripts/Character/PlayerCombat.cs`、`EnemyAI.cs`、`PlayerController.cs` | 动画参数、攻击命中帧、输入状态 |
| 角色生成 | `Assets/Scripts/Editor/Art/CharacterAnimationBuilder.cs`、`CharacterShowcaseGenerator.cs` | 97 个 Controller/Prefab 动画状态与总览验收 |
| 场景生成 | `Assets/Scripts/Editor/Art/RegionSceneBuilder.cs`、`EnvironmentTileBuilder.cs` | 消费布局、生成 23 个正式场景、碰撞与前景 |
| 场景运行时 | `Assets/Scripts/Core/SceneBootstrapper.cs`、`Assets/Scripts/Map/AreaTrigger.cs`、`FormalSceneTravelGraph.cs` | 直开场景、跨场景落点、玩家状态与输入保持 |
| 环境状态 | `Assets/Scripts/Art/RegionEnvironmentController.cs`、环境 manifest/layout | 昼夜、天气、序章村庄状态 |
| 菜单与存档 | `Assets/Scripts/UI/MainMenu.cs`、`PlayerAppearance.cs`、`SaveManager.cs` | 选角确认/取消、默认外观、旧档迁移 |
| 回归验收 | `Assets/Scripts/Editor/Art/VisualRegressionCapture.cs`、`Assets/Tests/VisualBaselines/` | 固定机位捕获与像素差异比较 |
| 长期事实 | `AGENTS.md`、`README.md`、`SETUP_GUIDE.md`、`docs/03-art-production-handoff.md` | 只记录真实完成和真实验证结果 |

---

### Task 1: 从 `main` 建立可审计的整合分支

**Files:**
- Source: `/Users/lhw/code/yuanHaiLu/.worktrees/full-art-production`
- Create worktree: `/Users/lhw/code/yuanHaiLu/.worktrees/external-ai-art-completion`
- Create patch: `/private/tmp/yuanhailu-full-art-curated.patch`

**Interfaces:**
- Consumes: `main` 当前 HEAD、来源工作树全部改动。
- Produces: 基于 `main`、仅包含允许范围变化的 `codex/external-ai-art-completion` 分支。

- [ ] **Step 1: 记录两个工作树的固定点和状态**

```bash
rtk git -C /Users/lhw/code/yuanHaiLu status --short --branch
rtk git -C /Users/lhw/code/yuanHaiLu rev-parse HEAD
rtk git -C /Users/lhw/code/yuanHaiLu/.worktrees/full-art-production status --short --branch
rtk git -C /Users/lhw/code/yuanHaiLu/.worktrees/full-art-production rev-parse HEAD
```

Expected: 权威工作树位于 `main`；来源 HEAD 为 `75eb1ad`，并显示大量未提交资源。

- [ ] **Step 2: 从最新 `main` 创建隔离工作树**

```bash
rtk git -C /Users/lhw/code/yuanHaiLu worktree add \
  /Users/lhw/code/yuanHaiLu/.worktrees/external-ai-art-completion \
  -b codex/external-ai-art-completion main
```

Expected: 新工作树干净，且 `git merge-base HEAD main` 返回当前 `main` HEAD。

- [ ] **Step 3: 在来源工作树暂存全部素材以便生成二进制补丁**

```bash
rtk git -C /Users/lhw/code/yuanHaiLu/.worktrees/full-art-production add -A
```

Expected: PNG、Controller、Prefab、Scene、脚本和对应 `.meta` 都进入来源索引；工作文件内容不变。

- [ ] **Step 4: 把禁止迁移的路径在来源索引中还原为 `main` 版本**

```bash
rtk git -C /Users/lhw/code/yuanHaiLu/.worktrees/full-art-production restore \
  --source=main --staged -- \
  .gitignore AGENTS.md ProjectSettings/ProjectSettings.asset \
  Assets/MobileDependencyResolver Assets/Scripts/System/QuestTarget.cs
rtk git -C /Users/lhw/code/yuanHaiLu/.worktrees/full-art-production rm \
  -r --cached --ignore-unmatch docs/superpowers .vscode .zcode
```

Expected: 相对 `main` 的 staged diff 不含上述路径；来源工作树中的原始文件仍保留，可继续作为参考。

- [ ] **Step 5: 生成并检查可迁移补丁**

```bash
rtk git -C /Users/lhw/code/yuanHaiLu/.worktrees/full-art-production diff \
  --cached --binary main > /private/tmp/yuanhailu-full-art-curated.patch
rtk git -C /Users/lhw/code/yuanHaiLu/.worktrees/full-art-production diff \
  --cached --name-only main
```

Expected: 清单包含正式美术、脚本、测试和正式文档；不含排除路径。

- [ ] **Step 6: 将补丁应用到新整合工作树**

```bash
rtk git -C /Users/lhw/code/yuanHaiLu/.worktrees/external-ai-art-completion apply \
  --index --binary /private/tmp/yuanhailu-full-art-curated.patch
rtk git -C /Users/lhw/code/yuanHaiLu/.worktrees/external-ai-art-completion status --short
```

Expected: 补丁无冲突；所有新增 Unity 资源都有对应 `.meta`；排除路径保持 `main` 内容。

- [ ] **Step 7: 清空来源索引的临时暂存状态**

```bash
rtk git -C /Users/lhw/code/yuanHaiLu/.worktrees/full-art-production restore --staged :/
```

Expected: 来源工作树回到迁移前的未提交状态，未删除任何素材。

- [ ] **Step 8: 提交迁移检查点**

```bash
rtk git -C /Users/lhw/code/yuanHaiLu/.worktrees/external-ai-art-completion commit \
  -m "chore: migrate curated full art worktree onto main"
```

Expected: 新分支拥有与 `main` 的正常共同祖先；后续每个任务可独立审查和回退。

---

### Task 2: 恢复兼容契约并清理无关变化

**Files:**
- Modify: `Assets/Scripts/Core/PlayerAppearance.cs`
- Modify: `Assets/Scripts/System/SaveManager.cs`
- Preserve: `Assets/Scripts/System/QuestTarget.cs`
- Modify: `Assets/Tests/EditMode/PlayerAppearanceTests.cs`
- Modify: `Assets/Tests/EditMode/PersistenceTests.cs`
- Modify later: `README.md`、`SETUP_GUIDE.md`、`docs/03-art-production-handoff.md`

**Interfaces:**
- Consumes: `PlayerAppearance.DefaultArtId`、`SaveManager.ResolvePlayerArtId`。
- Produces: 新游戏和 v1–v3 存档一致回退到 `player_female_swordsman`；任务目标只有真实匹配时才锁定。

- [ ] **Step 1: 写入默认外观与旧档迁移回归断言**

```csharp
Assert.That(PlayerAppearance.Default.ArtId,
    Is.EqualTo("player_female_swordsman"));
Assert.That(SaveManager.ResolvePlayerArtIdForTests(v3Save),
    Is.EqualTo("player_female_swordsman"));
```

- [ ] **Step 2: 运行两个定向测试并确认冲突版本失败**

```bash
rtk /Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath /Users/lhw/code/yuanHaiLu/.worktrees/external-ai-art-completion \
  -runTests -testPlatform EditMode \
  -testFilter "YuanHaiLu.Tests.EditMode.PlayerAppearanceTests;YuanHaiLu.Tests.EditMode.PersistenceTests" \
  -testResults /private/tmp/yuanhailu-appearance-red.xml \
  -logFile /private/tmp/yuanhailu-appearance-red.log
```

Expected: 男剑客冲突版本至少有一项失败。

- [ ] **Step 3: 恢复默认值和迁移值**

```csharp
public const string DefaultArtId = "player_female_swordsman";
public static PlayerAppearance Default =>
    new PlayerAppearance(PlayerGender.Female, PlayerProfession.Swordsman);
```

`SaveManager` 对 `saveVersion < 4`、空 ID 和非法 ID 均返回 `PlayerAppearance.Default.ArtId`。

- [ ] **Step 4: 检查任务目标修复没有被覆盖**

```csharp
if (QuestManager.Instance != null && QuestManager.Instance.UpdateObjective(
        objectiveType, targetId, Mathf.Max(1, amount)))
{
    _reported = true;
}
```

Expected: `_reported` 不得在 `UpdateObjective` 调用之前赋值。

- [ ] **Step 5: 重跑定向测试**

使用 Step 2 命令，将结果文件改为 `/private/tmp/yuanhailu-appearance-green.xml`。

Expected: 全部通过。

- [ ] **Step 6: 提交兼容修复**

```bash
rtk git add Assets/Scripts/Core/PlayerAppearance.cs \
  Assets/Scripts/System/SaveManager.cs Assets/Scripts/System/QuestTarget.cs \
  Assets/Tests/EditMode/PlayerAppearanceTests.cs Assets/Tests/EditMode/PersistenceTests.cs
rtk git commit -m "fix: preserve appearance and quest migration contracts"
```

---

### Task 3: 让正式布局成为场景结构的唯一数据源

**Files:**
- Modify: `Assets/Scripts/Editor/Art/RegionSceneBuilder.cs`
- Modify: `Assets/ArtSource/Environment/Layouts/*.json`
- Modify: `Assets/ArtSource/Environment/Layouts/interiors/*.json`
- Modify: `tools/art_pipeline/map_layout.py`
- Modify: `tools/art_pipeline/tests/test_map_layout.py`
- Modify: `Assets/Tests/EditMode/EnvironmentArtTests.cs`

**Interfaces:**
- Consumes: `layers[layerName]` 中的 `[x, y, token]`、`collisions`、`foregroundSpans`、`anchors`。
- Produces: 23 个场景的道路、装饰、建筑、前景和碰撞均由各自 JSON 声明；生成器不再用统一公式画房屋和散布装饰。

- [ ] **Step 1: 增加布局独立性和完整消费测试**

```python
def test_outdoor_structural_coordinate_signatures_are_unique():
    layouts = load_all_outdoor_layouts()
    signatures = {
        layout.id: layout.structural_coordinate_signature()
        for layout in layouts
    }
    assert len(set(signatures.values())) == len(signatures)
```

Unity 测试同时断言每个 JSON `Buildings`、`Lower Environment`、`Foreground` 声明的坐标在重建场景中存在对应 Tile 或 Landmark。

- [ ] **Step 2: 运行 Python 与 EnvironmentArt 定向测试并确认失败**

```bash
rtk python3 -m unittest tools.art_pipeline.tests.test_map_layout -v
rtk /Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath /Users/lhw/code/yuanHaiLu/.worktrees/external-ai-art-completion \
  -runTests -testPlatform EditMode \
  -testFilter YuanHaiLu.Tests.EditMode.EnvironmentArtTests \
  -testResults /private/tmp/yuanhailu-layout-red.xml \
  -logFile /private/tmp/yuanhailu-layout-red.log
```

Expected: 当前统一公式生成版本失败。

- [ ] **Step 3: 扩充 23 份布局的显式结构坐标**

每份 JSON 必须明确列出：主路/支路 Tile、区域专属装饰、建筑 Tile、三个 Landmark marker、Foreground spans、碰撞格和入口/出口/室内锚点。十个户外场景的结构坐标签名必须互不相同；十三个室内布局必须体现其用途，例如客栈柜台、药铺药柜、书院桌案、地牢栅栏、船舱货物。

- [ ] **Step 4: 删除公式化结构绘制路径**

删除 `PaintRegionStructure`、`PaintHouse`、`PaintInteriorStructure` 及固定坐标装饰循环。统一通过：

```csharp
ApplyDeclaredLayer(maps["Ground"], tiles, id, layout, "Ground");
ApplyDeclaredLayer(maps["Water"], tiles, id, layout, "Water");
ApplyDeclaredLayer(maps["Lower Environment"], tiles, id, layout, "Lower Environment");
ApplyDeclaredLayer(maps["Buildings"], tiles, id, layout, "Buildings");
ApplyDeclaredLayer(maps["Foreground"], tiles, id, layout, "Foreground");
```

`landmark_<id>` token 只由 `AddLandmarks` 消费，普通 Tile token 必须通过 `TryParseRoleToken` 严格解析；未知 token 中止生成。

- [ ] **Step 5: 重建 23 个场景**

```bash
rtk /Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath /Users/lhw/code/yuanHaiLu/.worktrees/external-ai-art-completion \
  -executeMethod YuanHaiLu.Editor.RegionSceneBuilder.GenerateFromCommandLine \
  -logFile /private/tmp/yuanhailu-region-rebuild.log -quit
```

Expected: 日志明确生成 23 个正式场景，且不存在 missing token、missing sprite 或序列化错误。

- [ ] **Step 6: 重跑布局测试并提交**

Expected: Python 布局测试和 Unity EnvironmentArt 定向测试全部通过。

```bash
rtk git add Assets/Scripts/Editor/Art/RegionSceneBuilder.cs \
  Assets/ArtSource/Environment/Layouts Assets/Scenes/Regions Assets/Scenes/Interiors \
  tools/art_pipeline/map_layout.py tools/art_pipeline/tests/test_map_layout.py \
  Assets/Tests/EditMode/EnvironmentArtTests.cs
rtk git commit -m "feat: build formal scenes entirely from declared layouts"
```

---

### Task 4: 增加序章村庄正常/焚毁双状态

**Files:**
- Modify: `Assets/ArtSource/Environment/Manifests/regions.json`
- Modify: `tools/art_pipeline/schema.py`
- Modify: `tools/art_pipeline/environment_baker.py`
- Modify: `Assets/Scripts/Art/RegionSceneDefinition.cs`
- Modify: `Assets/Scripts/Art/RegionEnvironmentController.cs`
- Test: `tools/art_pipeline/tests/test_environment_roster.py`
- Test: `Assets/Tests/EditMode/EnvironmentArtTests.cs`
- Test: `Assets/Tests/PlayMode/RuntimePresentationPlayModeTests.cs`

**Interfaces:**
- Consumes: `prologue_village` 正常布局及相同锚点/碰撞。
- Produces: `RegionEnvironmentController.SetEnvironmentState("normal" | "burned")`；切换只替换环境 Tile/Sprite 和天气表现，不改变场景 ID、锚点或碰撞。

- [ ] **Step 1: 写入状态资产和运行时切换测试**

```csharp
var beforeAnchors = definition.Anchors.ToArray();
var beforeBounds = FindCollisionBounds();
controller.SetEnvironmentState("burned");
Assert.That(controller.CurrentEnvironmentState, Is.EqualTo("burned"));
CollectionAssert.AreEqual(beforeAnchors, definition.Anchors);
Assert.That(FindCollisionBounds(), Is.EqualTo(beforeBounds));
Assert.That(CurrentTileAssetIds(), Is.Not.EqualTo(normalTileAssetIds));
```

- [ ] **Step 2: 运行状态测试并确认当前单状态实现失败**

Expected: 缺少 `SetEnvironmentState` 或 burned 资产而失败。

- [ ] **Step 3: 扩展 manifest/schema 并烘焙焚毁资产**

`prologue_village` 增加 `stateVariants`，固定包含 `normal` 与 `burned`；`burned` 使用炭化 wall/roof/foliage、破损地标和 `ember_wind`，仍保持同一布局尺寸及锚点 ID。

- [ ] **Step 4: 实现严格状态切换**

```csharp
public void SetEnvironmentState(string stateId)
```

只接受 `normal` 和 `burned`；未知状态抛出 `ArgumentException`。场景数仍保持 10 个户外、13 个室内和总计 25 个 Build Settings 场景。

- [ ] **Step 5: 重建资源与场景并重跑测试**

```bash
rtk python3 -m tools.art_pipeline.build --all
rtk python3 -m tools.art_pipeline.validate --all
```

Expected: baker、validator 和双状态测试全部通过。

- [ ] **Step 6: 提交双状态支持**

```bash
rtk git add Assets/ArtSource/Environment/Manifests/regions.json \
  Assets/ArtSource/Environment/Regions/prologue_village \
  Assets/Art/Environment/Regions/prologue_village \
  Assets/Scripts/Art/RegionSceneDefinition.cs \
  Assets/Scripts/Art/RegionEnvironmentController.cs \
  tools/art_pipeline Assets/Tests
rtk git commit -m "feat: add normal and burned prologue village states"
```

---

### Task 5: 补齐 97 角色的总览与动作验收入口

**Files:**
- Modify: `Assets/Scripts/Editor/Art/CharacterShowcaseGenerator.cs`
- Create: `Assets/Scripts/Editor/Art/CharacterShowcaseWindow.cs`
- Create: `Assets/Scripts/Editor/Art/CharacterShowcaseWindow.cs.meta`
- Modify: `Assets/Scripts/Editor/Art/CharacterAnimationBuilder.cs`
- Modify: `Assets/Tests/EditMode/CharacterArtTests.cs`
- Modify: `Assets/Tests/PlayMode/CharacterAnimationPlayModeTests.cs`

**Interfaces:**
- Consumes: 97 个 Catalog entry、Prefab、Animator Controller 和命名 Clip。
- Produces: 每个角色的稳定 ID 标签；动作选择 `idle/walk/dash/attack1/attack2/attack3/skill1/skill2/hurt/death`；缩放 `1x/4x/8x`；可从编辑器窗口统一预览。

- [ ] **Step 1: 写入总览结构与动作覆盖测试**

```csharp
Assert.That(showcaseLabels.Select(label => label.text).Distinct().Count(), Is.EqualTo(97));
CollectionAssert.IsSubsetOf(
    new[] { "idle", "walk", "dash", "attack1", "attack2", "attack3", "skill1", "skill2", "hurt", "death" },
    CharacterShowcaseWindow.SupportedActions);
CollectionAssert.AreEqual(new[] { 1, 4, 8 }, CharacterShowcaseWindow.SupportedScales);
```

- [ ] **Step 2: 运行 CharacterArt 和 CharacterAnimation 定向测试并确认失败**

Expected: 当前没有标签、动作控制器和缩放入口。

- [ ] **Step 3: 为场景生成稳定标签**

每个 Prefab 下方生成 `TextMesh`，文本严格等于 Catalog stable ID，Sorting Layer 为 `UI`；标签不参与角色 Animator。

- [ ] **Step 4: 实现编辑器控制窗口**

```csharp
public static readonly string[] SupportedActions =
{
    "idle", "walk", "dash", "attack1", "attack2", "attack3",
    "skill1", "skill2", "hurt", "death"
};
public static readonly int[] SupportedScales = { 1, 4, 8 };
public static void PreviewAction(string actionId);
public static void SetPreviewScale(int scale);
```

未知动作或缩放值抛出 `ArgumentOutOfRangeException`；动作通过 Controller 中的实际 state/clip 播放，不允许替换成静态首帧。

- [ ] **Step 5: 重建角色 Controller、Prefab 和总览场景**

```bash
rtk /Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath /Users/lhw/code/yuanHaiLu/.worktrees/external-ai-art-completion \
  -executeMethod YuanHaiLu.Editor.CharacterShowcaseGenerator.GenerateFromCommandLine \
  -logFile /private/tmp/yuanhailu-character-showcase.log -quit
```

Expected: 97 个标签、97 个 Prefab，所有支持动作都有持久 Clip。

- [ ] **Step 6: 重跑定向测试并提交**

```bash
rtk git add Assets/Scripts/Editor/Art/CharacterShowcaseGenerator.cs \
  Assets/Scripts/Editor/Art/CharacterShowcaseWindow.cs \
  Assets/Scripts/Editor/Art/CharacterShowcaseWindow.cs.meta \
  Assets/Scripts/Editor/Art/CharacterAnimationBuilder.cs \
  Assets/Scenes/CharacterShowcase.unity Assets/Tests
rtk git commit -m "feat: add complete character showcase controls"
```

---

### Task 6: 严格化天气配置并修复视觉捕获状态污染

**Files:**
- Modify: `Assets/Scripts/Art/RegionEnvironmentController.cs`
- Modify: `Assets/Scripts/Editor/Art/VisualRegressionCapture.cs`
- Modify: `Assets/Tests/EditMode/VisualRegressionTests.cs`
- Modify: `Assets/Tests/PlayMode/RuntimePresentationPlayModeTests.cs`

**Interfaces:**
- Consumes: manifest 中的精确天气 ID、MainMenu Canvas/选择面板状态。
- Produces: 精确 `WeatherProfile` 查表；视觉捕获在成功和异常路径都恢复场景 UI、Canvas、Camera 和 QualitySettings。

- [ ] **Step 1: 写入未知天气和捕获恢复测试**

```csharp
Assert.Throws<ArgumentException>(() => controller.ConfigureWeather("unknown_weather"));

var before = MainMenuCaptureState.Read();
Assert.Throws<InvalidOperationException>(() =>
    VisualRegressionCapture.CaptureMainMenu(invalidOutputPath));
Assert.That(MainMenuCaptureState.Read(), Is.EqualTo(before));
```

- [ ] **Step 2: 运行定向测试并确认失败**

Expected: 字符串 `Contains` 版本接受未知 ID；异常捕获路径没有恢复 UI/Canvas。

- [ ] **Step 3: 建立精确天气映射**

```csharp
private static readonly IReadOnlyDictionary<string, WeatherProfile> Profiles;
private readonly struct WeatherProfile
{
    public Color Tint { get; }
    public Vector2 Velocity { get; }
}
```

映射键必须与 23 个环境 manifest 中声明的天气 ID 完全一致；未知 ID 抛出异常，不允许默认猜测。

- [ ] **Step 4: 用 `try/finally` 恢复捕获前状态**

进入捕获前保存 selector/button active、Canvas renderMode/worldCamera/planeDistance、Camera targetTexture、RenderTexture.active 和抗锯齿；在同一个 `finally` 中全部恢复。捕获函数不得保存或污染 MainMenu scene。

- [ ] **Step 5: 重跑测试并提交**

```bash
rtk git add Assets/Scripts/Art/RegionEnvironmentController.cs \
  Assets/Scripts/Editor/Art/VisualRegressionCapture.cs \
  Assets/Tests/EditMode/VisualRegressionTests.cs \
  Assets/Tests/PlayMode/RuntimePresentationPlayModeTests.cs
rtk git commit -m "fix: make weather and visual capture deterministic"
```

---

### Task 7: 重建视觉基线并完成人工 Play QA

**Files:**
- Modify only after visual approval: `Assets/Tests/VisualBaselines/*.png`
- Record: `docs/03-art-production-handoff.md`

**Interfaces:**
- Consumes: 最终 10 个户外场景与 MainMenu 的固定机位渲染。
- Produces: 人工确认后的 `480×270` 基线、视觉差异报告、完整可见窗口 QA 记录。

- [ ] **Step 1: 先将实际截图输出到临时目录**

```bash
rtk mkdir -p /private/tmp/yuanhailu-visual-review
```

使用 `VisualRegressionCapture` 将 MainMenu 和十个户外场景写入该目录，不改仓库基线。

- [ ] **Step 2: 在 1× 与 4× 检查十一张截图**

逐张确认：不是纯色/色块、角色可读、道路和地标可辨、前景遮挡合理、建筑构图不同、天气不遮挡交互区域、没有缺图或整表拉花。

- [ ] **Step 3: 对已有基线计算像素差异**

Expected: 无意图变化必须 `<=0.5%`；有意图变化必须保留旧/新对比图和变更原因，经人工确认后才复制为新基线。

- [ ] **Step 4: 在可见 Unity 窗口完成主流程 QA**

按顺序验证：主菜单键盘/鼠标选角与取消、进入 Demo、WASD/方向键、J 三连击命中、Shift 冲刺、K/E NPC 交互、ESC 暂停、烟柳镇→客栈→烟柳镇、HP/等级/武学/外观与输入保持、保存后退出再读档。

- [ ] **Step 5: 记录截图路径和人工结果**

`docs/03-art-production-handoff.md` 写入测试日期、Unity 版本、执行人/AI、通过项、失败项及修复提交。未完成任何一项时必须写“未验收”，不得写“全量完成”。

- [ ] **Step 6: 提交获批基线与 QA 记录**

```bash
rtk git add Assets/Tests/VisualBaselines docs/03-art-production-handoff.md
rtk git commit -m "test: approve final formal art visual baselines"
```

---

### Task 8: 全量验证、文档收敛和交付证据包

**Files:**
- Modify: `AGENTS.md`
- Modify: `README.md`
- Modify: `SETUP_GUIDE.md`
- Modify: `docs/03-art-production-handoff.md`
- Preserve: `docs/04-external-ai-development-handoff.md`
- Preserve: `docs/05-post-development-review-plan.md`

**Interfaces:**
- Consumes: 最终分支所有提交和全部验证结果。
- Produces: 一个干净、基于 `main`、可由独立审查者复核的交付分支。

- [ ] **Step 1: 运行 Python 全量验证**

```bash
rtk python3 -m unittest discover -s tools/art_pipeline/tests -v
rtk python3 -m tools.art_pipeline.build --all
rtk python3 -m tools.art_pipeline.validate --all
```

Expected: 全部测试通过，`built=0 skipped=120` 或与新增焚毁状态后明确记录的新稳定数量一致，validator 无错误。

- [ ] **Step 2: 运行 Unity EditMode 全量测试**

```bash
rtk /Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath /Users/lhw/code/yuanHaiLu/.worktrees/external-ai-art-completion \
  -runTests -testPlatform EditMode \
  -testResults /private/tmp/yuanhailu-final-editmode.xml \
  -logFile /private/tmp/yuanhailu-final-editmode.log
```

Expected: XML `failed="0"`；文档记录 XML 中真实 `total`，不得沿用旧数字。

- [ ] **Step 3: 运行 Unity PlayMode 全量测试**

使用 Step 2 命令，把平台改为 `PlayMode`，结果写入 `/private/tmp/yuanhailu-final-playmode.xml`，日志写入 `/private/tmp/yuanhailu-final-playmode.log`。

Expected: XML `failed="0"`。

- [ ] **Step 4: 检查最终差异和生成噪声**

```bash
rtk git diff main --check
rtk git diff --name-status main
rtk git status --short
```

Expected: 无空白错误；没有 `ProjectSettings` 平台噪声、Resolver `.meta` 删除和本地 AI/编辑器目录；提交前工作树只包含预期文档更新。

- [ ] **Step 5: 用真实结果更新长期事实**

同步更新脚本数、测试数、完成范围、未完成功能和人工 QA 状态。旧档默认必须仍写 `player_female_swordsman`；“完整游戏完成”不得作为美术阶段完成的同义词。

- [ ] **Step 6: 提交最终文档**

```bash
rtk git add AGENTS.md README.md SETUP_GUIDE.md docs
rtk git commit -m "docs: finalize external AI art completion handoff"
```

- [ ] **Step 7: 输出交付证据包**

交付消息必须逐项提供：

1. 分支名和最终 HEAD。
2. 开发基线 `main` 哈希。
3. `git diff --stat main...HEAD`。
4. Python、EditMode、PlayMode 的总数/通过数/失败数。
5. 三个 XML/日志绝对路径。
6. 十一个视觉截图和获批基线路径。
7. 人工 QA 逐项结果。
8. 尚未解决的风险；没有时明确写“没有已知未解决 P0/P1”。

Expected: 证据包交给独立审查者后再进入 `docs/05-post-development-review-plan.md`，不得自行合并到 `main`。

---

## 3. 接手 AI 的停止条件

出现以下任一情况时停止继续扩展，并把证据交回审查者：

- 需要改变 `SaveData.saveVersion == 4` 的既有字段语义。
- 需要改变默认外观或旧档迁移规则。
- 需要新增第三方包、切换 URP/3D 或改变 Unity 版本。
- 需要覆盖视觉基线但无法提供前后截图和人工确认。
- Unity 许可/锁屏导致无法完成当前全量测试或可见窗口 QA。
- 发现来源工作树中有无法判断归属的用户修改。

这些情况不是完成状态；交付记录应写明阻塞原因和已经完成的安全工作。

