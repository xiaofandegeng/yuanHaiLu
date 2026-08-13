# 外部 AI 美术完工整合 —— 交付与审查交接

> 交付分支：`codex/external-ai-art-completion` @ `572acf4`
> 基线 main：`77a8322`
> 编写时间：2026-08-13
> 执行依据：`docs/04-external-ai-development-handoff.md`（8 任务计划）

## 0. 一句话结论

Task 1（整合迁移）、2（契约）、3（签名/CLI）、5（角色总览窗口）、6（天气精确化 + 捕获状态恢复）的**代码**已交付；Task 4、7 因强依赖 Unity **暂缓/硬阻塞**；Task 8 的 **Python 流水线已实跑**，Unity 全量验证未跑（按约定跳过批处理）。**所有 C# 未经编译/测试验证，须由你在 Editor 里确认 0 error + 测试通过。**

## 1. 分支状态

| 项 | 值 |
|----|----|
| 分支 | `codex/external-ai-art-completion` |
| HEAD | `572acf4` |
| main 基线 | `77a8322` |
| 领先 main 提交数 | 5（1 迁移 + 4 功能） |
| 本会话功能改动（迁移之后） | 16 文件，+561/−53 |
| 全量相对 main | 644 文件，+418983/−282053（绝大部分为迁移带入的已构建美术资产） |

本会话功能提交（迁移点 `bea8c68` 之后）：

```
572acf4 feat(art): character showcase window and labelled overview scene
3c03b59 fix(art): strict weather profiles and deterministic main menu capture
47609b4 feat(layout): structural coordinate signatures + scene CLI entry
6410669 fix: preserve appearance and quest migration contracts
```

## 2. 任务状态

| 任务 | 状态 | 说明 |
|------|------|------|
| 1 整合分支 + curated 迁移 | ✅ 完成 | 从 main 建分支，迁移 codex 工作树完工增量；排除清单已核对 |
| 2 外观/任务迁移契约 | ✅ 完成 | 默认 `player_female_swordsman`、v1–v3 回退、QuestTarget「成功才锁定」均已核对/修复并补测试 |
| 3 布局成为唯一数据源 | ⚠️ 部分 | 结构坐标签名 + 唯一性测试 + CLI 已交付并实跑通过；**公式化移除 + 23 布局扩充暂缓**（强 Unity 耦合，需场景重建验证） |
| 4 序章 normal/burned 双状态 | ⏸️ 暂缓 | C# 运行时 Tile 交换强 Unity 耦合；Python 数据若无消费者为空转，整体留到 Unity 侧一起做 |
| 5 角色总览窗口 + 标签 | ✅ 完成（代码） | `CharacterShowcaseWindow` + 生成器 TextMesh 标签 + EditMode/PlayMode 断言 |
| 6 天气精确化 + 捕获状态恢复 | ✅ 完成（代码） | 严格 `WeatherProfiles` 查表 + `CaptureMainMenu` try/finally 全状态恢复 + 测试 |
| 7 视觉基线 + Play QA | ⛔ 硬阻塞 | 依赖 `VisualRegressionCapture`（Unity 编辑器方法）；本会话不跑 Unity |
| 8 全量验证 + 文档 + 证据 | ✅ 完成（Python/文档侧） | 见下；Unity 项标 pending |

## 3. Python 流水线验证（实跑，真实数字）

工作目录：`.worktrees/external-ai-art-completion`

| 命令 | 结果 |
|------|------|
| `python3 -m unittest discover -s tools/art_pipeline/tests` | **36 用例，35 通过，1 error** |
| `python3 -m tools.art_pipeline.build --manifest .../regions.json` | `built=0 skipped=10`（10 个户外环境已最新） |
| `python3 -m tools.art_pipeline.validate --all` | `EXIT=0`，校验 `Assets/Art` 已构建产物通过 |
| `git diff main --check`（文本源） | `exit=0`，无空白错误（二进制 PNG 重命名会让全量 `--check` 返回 2，属误报） |

唯一失败的用例：

```
ERROR: test_every_recipe_uses_six_visible_editable_modules
  (test_character_roster.CharacterRosterTests)
ValueError: player_male_swordsman hair module is missing:
  Assets/ArtSource/Characters/Hair/library/full/male_hair_05.png
```

**这是 main 上既有的缺陷，非本会话引入**（见 §5 风险项 R1）。

## 4. Unity 验证状态（全部 pending）

按约定本会话**未跑任何 Unity 批处理**。以下项须由你在 Editor 确认：

- [ ] 打开工作树，Console **0 C# 编译错误**。
- [ ] EditMode 全量测试通过（含新增 `VisualRegressionTests`、`CharacterArtTests` 两个用例）。
- [ ] PlayMode 全量测试通过（含新增 `RuntimePresentationPlayModeTests`、`CharacterAnimationPlayModeTests` 各一个用例）。
- [ ] `Tools/渊海录/美术/角色总览窗口` 可打开，选角色、点动作/缩放按钮可用。
- [ ] `Tools/渊海录/美术/生成角色总览场景` 生成后，每个角色上方有 TextMesh 标签（SortingLayer=UI）。
- [ ] 未知天气 ID 在运行时抛 `ArgumentException`（不静默回退）。
- [ ] Task 7 协议：在可见窗口 Play QA 后，逐张审批 11 张视觉基线（≤0.5% 像素差）。

## 5. 关键发现与风险项

### R1 — 角色源模块库从未入库（既有缺陷，非本会话引入）

- 5 个正式 roster 共 97 个角色，**92 个**的 recipe 引用 `Assets/ArtSource/Characters/<层>/library/full/<性别>_<层>_<NN>.png`，但这些源 PNG **从未提交**（main 上同样为 0；仅有空的 `library.meta` 占位）。
- 现存源模块只有 `beasts/`（5）与 `reference/`（2），共 7 个角色源完整。
- **影响**：Python 侧**无法从源重建角色**（`build --all` 会在角色 manifest 失败）；但 **已构建产物完整**（97 张 sprite sheet / prefab / controller + `CharacterArtCatalog.asset` 已入库），Unity 侧可直接使用。
- **归属判断**：`git ls-tree main` 确认 main 上同样缺失 → 既有缺陷，非迁移回归。`test_every_recipe_uses_six_visible_editable_modules` 在 main 上同样会失败。
- **建议**：源模块库需由美术侧补齐入库；在此之前，角色应以已构建产物为准，不要从源重建。

### R2 — 11 张视觉基线随迁移带入，未经 Task 7 审批

- `Assets/Tests/VisualBaselines/`（MainMenu + 10 户外，共 11 PNG + 11 .meta）由 Task 1 迁移提交 `bea8c68` 带入；main 上原为 0。
- 本会话**未**通过 `VisualRegressionCapture` 重新捕获或覆盖。
- **风险**：这些基线源自 codex 工作树，**未**经 Task 7 协议（重新捕获 + ≤0.5% 像素差 + 可见窗口 Play QA）审批。
- **建议**：在 Editor 运行 `Tools/渊海录/美术/重建全部视觉基线` 重新捕获并逐张比对后再视为权威；若要严格遵循「本会话不立基线」，可 `git rm Assets/Tests/VisualBaselines/` 后由 Task 7 重新建立（注意 `VisualRegressionTests` 会因此转为缺基线失败，属预期）。

### R3 — 动作命名与计划措辞偏差（已按实际数据对齐）

- 计划写 `SupportedActions={… attack1, attack2, attack3, skill1, skill2 …}`（无下划线）。
- 实际 manifest 与 Animator 状态命名为 `attack_1` / `skill_1`（带下划线，状态名如 `attack_1_down`）。
- 为使 `PreviewAction` 能真正 `Animator.Play` 到对应状态，`CharacterShowcaseWindow.SupportedActions` 采用**带下划线**命名，已在代码注释与测试中固化。这是对计划措辞的有意修正，非疏漏。

### R4 — 暂缓项（Unity 耦合）

- Task 3 核心：移除 `RegionSceneBuilder` 公式化绘制（`PaintHouse` 等）+ 扩充 23 布局的显式结构坐标。当前布局只声明 landmark/锚点/前景/碰撞，移除公式化会清空场景，需场景重建 + 视觉验证。
- Task 4：序章 `burned` 状态的运行时 Tile 交换与 `SetEnvironmentState("burned")`。

## 6. 审查者下一步建议

1. 在 Unity Hub 用 `6000.4.10f1` 打开 `.worktrees/external-ai-art-completion`，确认编译 0 error。
2. 跑 EditMode / PlayMode 全量测试，回填真实 total/passed/failed。
3. 人工 Play：主菜单 → Demo、角色总览窗口、天气表现、捕获不污染主菜单。
4. 决定 R2（基线保留重审 还是 删除重建）。
5. 视情况补齐 R1 源模块库，或明确「以已构建产物为准」。
6. 审查通过后再合并到 main（本会话未自行合并）。

## 7. 不变更项（无需改动）

- `README.md` / `SETUP_GUIDE.md`：已构建美术完整，setup 流程不变；源模块缺口不影响首次打开与运行。
- `ProjectSettings/ProjectSettings.asset`：迁移时已排除，未改动。
- 默认外观与存档语义：`player_female_swordsman` 默认、`saveVersion==4`、v1–v3 回退默认外观均保持。
