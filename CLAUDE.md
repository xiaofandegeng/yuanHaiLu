# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

渊海录 (YuanHaiLu) — a top-down 2D pixel-art wuxia RPG. Unity `6000.4.10f1`, **built-in 2D renderer (NOT URP)**, Legacy Input Manager, macOS Apple Silicon. Chinese-language UI/docs; commit messages in English.

Current phase: single-hero MVP vertical slice (docs/15) — fixed male protagonist `player_male_swordsman` + three weapon styles (sword/gauntlets/dart), dense pixel module assembly (docs/18), quest `MVP_01` across 烟柳镇 ↔ 客栈 scenes.

## Where Truth Lives

- **`AGENTS.md` is the authoritative handoff/memory document** — architecture, conventions, known issues, and fix history. When it conflicts with `README.md`/`SETUP_GUIDE.md`, AGENTS.md wins (e.g. save format is **v5** with fixed male protagonist; README still describes the older v4 multi-appearance flow).
- Design docs: `docs/01-art-style-guide.md`, `docs/02-story-design.md`, `docs/15-single-hero-mvp-design.md` (frozen MVP scope), `docs/18-dense-pixel-mvp-implementation-handoff.md` (current implementation plan).
- After non-trivial changes, update AGENTS.md (or docs/) so long-term facts stay accurate.

## Commands

### Unity tests (batch mode)

```bash
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /Users/lhw/code/yuanHaiLu \
  -runTests \
  -testPlatform EditMode \
  -testResults /tmp/yuanhailu-editmode.xml \
  -logFile /tmp/yuanhailu-editmode.log
```

- PlayMode: change `-testPlatform PlayMode` and use separate result/log files.
- **Never pass `-quit` together with `-runTests`** (Unity exits before results are written). `-quit` is only for compile/import checks (no `-runTests`).
- Single test/class: add `-testFilter YuanHaiLu.EditModeTests.PersistenceTests.TestName` (full name; supports `.`-separated partial matches).
- If the log shows `Unsupported protocol version '1.18.1'` or licensing hangs: quit Unity Hub completely and kill stale Unity batch processes first, then rerun.
- Test evidence XML must be newer than the commit it proves.

### Deterministic art pipeline (Python 3)

```bash
python3 -m unittest discover -s tools/art_pipeline/tests -v
python3 -m tools.art_pipeline.build --all     # expect built=0, all skipped
python3 -m tools.art_pipeline.validate --all
```

### Baseline (2026-08-29, docs/18 §6.B)

139 EditMode + 14 PlayMode + 52 Python, all passing. Code size: 75 runtime/editor `.cs` + test files in `Assets/Tests/{EditMode,PlayMode}`.

## Architecture

### Namespace layout (maps 1:1 to `Assets/Scripts/<folder>`)

- `YuanHaiLu.Core` — GameManager (game state + scene entry mode), GameConfig (sorting-layer constants), WeaponStyle, camera
- `YuanHaiLu.Art` — CharacterArtCatalog / EnvironmentArtCatalog / MvpArtCatalog (stable snake_case asset IDs), MvpWorldModule, RegionEnvironmentController
- `YuanHaiLu.Character` — PlayerController/Combat/Interaction, CharacterStats, EnemyAI, NPCBase, MartialArtsSystem
- `YuanHaiLu.Map` — AreaTrigger, TeleportPoint, ItemPickup, EventTrigger, SceneDirector, Destructible
- `YuanHaiLu.Dialogue` — DialogueManager (typewriter, conditions, branches)
- `YuanHaiLu.GameSystem` — GlobalSystemsBootstrapper, SaveManager, InventoryManager, QuestManager/Database/Giver/Target/StageGate, MartialSkillDatabase, AudioManager, GameTimeManager
- `YuanHaiLu.UI` — HUD, MainMenu, PauseMenu, DialogueUI
- `YuanHaiLu.Editor` — scene generators (Demo/MainMenu/Inn + shared PlaySceneAssembler), art import/validation, region builders

**The systems namespace is `YuanHaiLu.GameSystem`, never `YuanHaiLu.System`** (collides with .NET `System`).

### Core patterns

- `GameManager` owns game state AND `SceneEntryMode` (`NewGame`/`LoadGame`/`SceneTransition`/`Active`). Only `NewGame` lets `SceneDirector` grant initial stats/skills/items — load/transition must never re-grant.
- `GlobalSystemsBootstrapper` idempotently ensures exactly one of each manager (Save/Inventory/Quest/GameTime/Dialogue) in both menu and game scenes; managers persist via `Instance` + `DontDestroyOnLoad`. New managers must plug into the bootstrapper — never duplicate creation logic in a scene.
- Systems decouple via `event System.Action` (HP, quests, dialogue, skills).
- Catalogs (`CharacterArtCatalog`, `EnvironmentArtCatalog`, `MvpArtCatalog`) resolve stable snake_case IDs to persistent assets. Formal scenes must never create runtime `Texture`/`Sprite` as art fallback.
- `WeaponStyle` (sword/gauntlets/dart) is the immutable config table driving all PlayerCombat/MartialArtsSystem parameters; illegal IDs fall back to sword.
- `QuestManager` is the sole authority for quest accept/progress/complete/rewards/serialization. `ActiveQuest` deep-copies template objectives (runtime progress never writes back to templates). `QuestStageGate` activates scene objects only at the correct sequential-objective step (prevents soft-locks from pre-killing enemies / pre-looting quest items) — components under its control must start coroutines in `OnEnable` because deactivation kills coroutines.
- `QuestGiver` sits beside `NPCBase` but is NOT `IInteractable`; quest actions settle only after the dialogue it started ends.
- Save format is versioned (`CURRENT_SAVE_VERSION = 5`; v2 base stats, v3 quests, v4 appearance, v5 weapon style). Never lower the version or change existing field semantics; old saves migrate forward.
- Scenes are programmatically (re)generable via editor menu `Tools → 渊海录 → …`; shared manager/camera/player/UI assembly lives in `PlaySceneAssembler.cs` — gameplay generators only add differentiated content. Demo scenes assemble `Assets/Art/MVP/dense_pixel/` modules per layout JSONs in `Assets/ArtSource/MVP/dense_pixel/layouts/`.

### Deterministic art pipeline

Editable sources live in `Assets/ArtSource/` (committed); baked outputs in `Assets/Art/` (also committed), each with `.art.json` metadata + SHA-256. `tools/art_pipeline/` (pure Python) bakes 97 characters + 23 environments from modules/manifests/palettes. `ArtImportRules` slices precisely; `ArtAssetValidator` checks hashes/sizes. Character roster is fixed: 12 Player, 15 Named, 36 NPC, 24 Enemies, 10 Bosses.

## Hard Conventions (bug-proven)

- **Unity fake-null**: never use `??` or `?.` on `UnityEngine.Object` — use explicit two-stage `== null` checks (caused real `MissingComponentException` bugs).
- **Tilemap**: always batch `SetTiles` (plural) then save; per-tile `SetTile` failed to serialize in Unity 6 batch scene generation.
- Sorting Layers must match `GameConfig.SORTING_*`: `Ground → Environment → Character → Foreground → UI`.
- Physics layers: 6 Player, 7 Enemy, 8 NPC, 9 Environment. There is no `Interactable` layer — interaction filters by `IInteractable` interface.
- Pixel specs: internal 480×270, PPU 16, tiles 16×16, formal character frames 32×32, Point filter, no compression, no AA, VSync off.
- Animator parameters are fixed names: `MoveX`, `MoveY`, `Speed`, `IsDashing`, `IsAttacking`, `AttackIndex`. Attack hit frames fire via animation events → `PlayerCombat.OnAttackHitFrame()`.
- Commit `.meta` files together with assets; never commit `Library/`, `Temp/`, logs, `.csproj`, `.sln`.
- After editing `ProjectSettings/*.asset` (especially InputManager, TagManager), Unity must be restarted.
- Data SOs: class definitions in `Assets/Scripts/System/`; instances go to `Assets/Resources/Items|Quests/` and override the code-built `ItemDatabase`/`QuestDatabase` tables by matching stable IDs.
- docs/18 §6.C cleanup (removing v2 three-layer MVP assets, `mvp_scene_layer_builder.py`, `CreateMvpSceneLayers`) is **blocked until the user approves the 1× screenshots** (Gate R1).

## Development Workflow

1. Change code + its tests together.
2. Run the relevant EditMode/PlayMode tests (batch commands above).
3. Let Unity recompile and check the Console; restart Unity before Play-verifying anything involving scenes or input.
4. Run full test suite + batch compile + `git diff --check` before committing.
5. Automated tests do not replace manual Play QA: docs/15 acceptance requires one full manual playthrough per weapon style (currently pending). Off-screen 480×270 captures go through `Tools → 渊海录 → 美术 → 截取临时正式美术验收图` → `/private/tmp/yuanhailu-art-review/`.
