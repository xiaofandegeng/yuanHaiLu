# Full Art Integration and QA Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the playable Demo and all character/environment spawn paths with formal catalog assets, expose all 12 player appearances, connect all generated maps, run visual/runtime regression, and update project memory and handoff documentation.

**Architecture:** Gameplay code selects stable art IDs and delegates visuals to `CharacterVisual` and region catalogs. `DemoSceneGenerator` becomes a gameplay-content overlay on the formal Yanliu scene rather than a texture painter. Appearance selection is persisted with backward-compatible defaults, and scene generation/build settings use the validated region definitions produced by the environment plan.

**Tech Stack:** Unity 6000.4.10f1; C#; Animator/Tilemap; PlayerPrefs JSON save migration; Unity Test Framework; deterministic screenshot capture; macOS Unity Editor manual QA.

## Global Constraints

- The playable world must use formal 32×32 character art and 16×16 environment art; no player, NPC, enemy, Boss, building, tree, water, road, or prop may be a runtime color block.
- Existing movement, combat, interaction, quest, inventory, and save behavior remains compatible except for required appearance persistence and visual bindings.
- New-game appearance offers exactly two genders × six professions; old saves default to male swordsman without data loss.
- All ten outdoor and thirteen interior scenes save, reopen, load from build settings, and resolve stable anchors.
- EditMode and PlayMode suites pass; Unity Console has zero errors; final changes and docs land on `main`.
- Run shell commands through `rtk`.

---

## File Structure

- Create `Assets/Scripts/Character/PlayerAppearance.cs`: gender/profession selection and stable player art ID.
- Create `Assets/Scripts/Character/CharacterArtBinding.cs`: role ID to `CharacterVisual` binding helper for spawners.
- Modify `Assets/Scripts/Core/GameManager.cs`: current appearance state and new-game initialization.
- Modify `Assets/Scripts/System/SaveManager.cs`: save version 4 appearance fields and v1–v3 migration defaults.
- Modify `Assets/Scripts/UI/MainMenu.cs`: character-selection entry before new game.
- Modify `Assets/Scripts/Editor/MainMenuSceneGenerator.cs`: minimal 2×6 appearance selector UI and preview.
- Modify `Assets/Scripts/Editor/DemoSceneGenerator.cs`: overlay gameplay objects on formal Yanliu map.
- Modify `Assets/Scripts/Core/SceneBootstrapper.cs`: formal catalog binding; remove fallback player/NPC/enemy visuals.
- Modify `Assets/Scripts/Map/EventTrigger.cs`: spawned enemies use formal art IDs/prefabs.
- Modify `Assets/Scripts/Map/SceneDirector.cs`: formal player selection and region anchor usage.
- Modify `Assets/Scripts/Editor/SetupBuildSettings.cs`: include all formal region/interior scenes.
- Create `Assets/Scripts/Editor/Art/GameplaySceneIntegrator.cs`: add gameplay managers/entities/anchors to generated region scenes.
- Create `Assets/Scripts/Editor/Art/VisualRegressionCapture.cs`: fixed-camera screenshot capture.
- Create `Assets/Tests/EditMode/PlayerAppearanceTests.cs`.
- Create `Assets/Tests/EditMode/FormalSceneIntegrationTests.cs`.
- Create `Assets/Tests/PlayMode/FormalArtFlowPlayModeTests.cs`.
- Create `Assets/Tests/VisualBaselines/`: approved PNG captures for MainMenu, Yanliu, and nine other outdoor regions.
- Modify `AGENTS.md`, `README.md`, `SETUP_GUIDE.md`, and `docs/01-art-style-guide.md`.
- Create `docs/HANDOFF-art-production.md`: final asset-generation and QA handoff.

---

### Task 1: Persist and Resolve the 12 Player Appearances

**Files:**
- Create: `Assets/Scripts/Character/PlayerAppearance.cs`
- Modify: `Assets/Scripts/Core/GameManager.cs`
- Modify: `Assets/Scripts/System/SaveManager.cs`
- Create: `Assets/Tests/EditMode/PlayerAppearanceTests.cs`
- Modify: `Assets/Tests/EditMode/PersistenceTests.cs`

**Interfaces:**
- Produces enums `PlayerGender { Male, Female }`, `PlayerProfession { Swordsman, Boxer, HiddenWeapon, Healer, Scholar, Mystic }`.
- Produces `PlayerAppearance.ArtId`, returning exactly `player_<gender>_<profession>` in snake case.
- Save format version becomes 4 with string fields `playerGender` and `playerProfession`.

- [ ] **Step 1: Write failing appearance and migration tests**

```csharp
[TestCase(PlayerGender.Male, PlayerProfession.Swordsman, "player_male_swordsman")]
[TestCase(PlayerGender.Female, PlayerProfession.HiddenWeapon, "player_female_hidden_weapon")]
[TestCase(PlayerGender.Female, PlayerProfession.Mystic, "player_female_mystic")]
public void AppearanceBuildsStableArtId(PlayerGender gender, PlayerProfession profession, string expected)
{
    Assert.That(new PlayerAppearance(gender, profession).ArtId, Is.EqualTo(expected));
}

[Test]
public void VersionThreeSaveMigratesToMaleSwordsman()
{
    var migrated = SaveManager.MigrateForTest(new SaveData { version = 3 });
    Assert.That(migrated.version, Is.EqualTo(4));
    Assert.That(migrated.playerGender, Is.EqualTo("male"));
    Assert.That(migrated.playerProfession, Is.EqualTo("swordsman"));
}
```

- [ ] **Step 2: Run focused EditMode tests and verify failure**

Expected: compile/test failure because appearance types and v4 fields do not exist.

- [ ] **Step 3: Implement immutable appearance resolution**

Reject undefined enum values. Expose `PlayerAppearance.Default` as male swordsman. Store current selection in `GameManager` and reset it only on explicit new game.

- [ ] **Step 4: Implement v4 save round-trip and migration**

Persist lowercase enum tokens. Unknown tokens migrate to defaults and emit one warning; v1–v3 saves retain all existing player, inventory, quest, and position data.

- [ ] **Step 5: Run persistence and appearance tests**

Expected: all focused and existing persistence tests PASS.

- [ ] **Step 6: Commit appearance persistence**

```bash
rtk git add Assets/Scripts/Character/PlayerAppearance.cs Assets/Scripts/Core/GameManager.cs Assets/Scripts/System/SaveManager.cs Assets/Tests/EditMode
rtk git commit -m "feat: persist player visual appearance"
```

### Task 2: Add Main Menu Appearance Selection

**Files:**
- Modify: `Assets/Scripts/UI/MainMenu.cs`
- Modify: `Assets/Scripts/Editor/MainMenuSceneGenerator.cs`
- Modify: `Assets/Tests/EditMode/MainMenuSceneTests.cs`
- Create: `Assets/Tests/PlayMode/FormalArtFlowPlayModeTests.cs`

**Interfaces:**
- Produces `MainMenu.SelectAppearance(PlayerGender, PlayerProfession)` and `MainMenu.ConfirmNewGame()`.
- New Game opens the selector; confirmation starts the game using `GameManager.CurrentAppearance`.

- [ ] **Step 1: Write failing menu behavior tests**

Assert New Game displays 12 selectable combinations, selecting female healer updates the preview to `player_female_healer`, cancel returns without clearing save data, and confirm initializes a new game with the selected appearance.

- [ ] **Step 2: Run focused tests and verify failure**

Expected: FAIL because selection APIs and generated controls do not exist.

- [ ] **Step 3: Implement selector state and preview**

Generate a two-row, six-column selector with keyboard arrows and mouse buttons. Preview uses `CharacterArtCatalog`; selector labels are localized profession names but control IDs use stable art IDs.

- [ ] **Step 4: Implement confirmation and cancellation**

Only confirmation calls new-game initialization. ESC/cancel restores the main menu without changing current save or appearance.

- [ ] **Step 5: Run menu EditMode and PlayMode tests**

Expected: all menu tests PASS.

- [ ] **Step 6: Commit appearance selection**

```bash
rtk git add Assets/Scripts/UI/MainMenu.cs Assets/Scripts/Editor/MainMenuSceneGenerator.cs Assets/Tests
rtk git commit -m "feat: add player appearance selection"
```

### Task 3: Bind All Gameplay Character Spawn Paths to Formal Art

**Files:**
- Create: `Assets/Scripts/Character/CharacterArtBinding.cs`
- Modify: `Assets/Scripts/Core/SceneBootstrapper.cs`
- Modify: `Assets/Scripts/Map/EventTrigger.cs`
- Modify: `Assets/Scripts/Map/SceneDirector.cs`
- Modify: `Assets/Tests/EditMode/FormalSceneIntegrationTests.cs`
- Modify: `Assets/Tests/PlayMode/FormalArtFlowPlayModeTests.cs`

**Interfaces:**
- Produces `CharacterArtBinding.Apply(GameObject target, string artId)`.
- Scene player uses `GameManager.CurrentAppearance.ArtId`; NPC/enemy/Boss spawn data carries exact catalog IDs.

- [ ] **Step 1: Write failing binding tests**

```csharp
[Test]
public void BindingUnknownFormalCharacterFailsInsteadOfCreatingPlaceholder()
{
    var actor = new GameObject("Actor");
    Assert.That(() => CharacterArtBinding.Apply(actor, "missing_actor"), Throws.InvalidOperationException);
    Assert.That(actor.GetComponent<SpriteRenderer>(), Is.Null);
    Assert.That(actor.GetComponent<CharacterVisual>(), Is.Null);
}
```

Also test direct scene bootstrap, event-wave enemy spawn, and scene-director player setup resolve formal catalog entries.

- [ ] **Step 2: Run focused tests and verify failure**

Expected: compile/test failure before binding helper exists.

- [ ] **Step 3: Implement formal binding helper**

Ensure SpriteRenderer, Animator, and `CharacterVisual` exist, then apply the catalog entry. It never creates pixels. Development/editor builds throw on missing IDs; release builds disable the invalid actor and log one error.

- [ ] **Step 4: Replace fallback actor visuals**

Remove player/NPC/enemy sprite construction and tinting from `SceneBootstrapper`, `EventTrigger`, and `SceneDirector`. Assign explicit IDs to current Demo roles and event-wave enemies.

- [ ] **Step 5: Run binding and existing gameplay tests**

Expected: all tests PASS; spawned actors have controllers and formal sprites.

- [ ] **Step 6: Commit formal spawn binding**

```bash
rtk git add Assets/Scripts/Character/CharacterArtBinding.cs Assets/Scripts/Core/SceneBootstrapper.cs Assets/Scripts/Map Assets/Tests
rtk git commit -m "fix: bind gameplay actors to formal art"
```

### Task 4: Replace DemoSceneGenerator with Formal Yanliu Composition

**Files:**
- Create: `Assets/Scripts/Editor/Art/GameplaySceneIntegrator.cs`
- Modify: `Assets/Scripts/Editor/DemoSceneGenerator.cs`
- Modify: `Assets/Tests/EditMode/FormalSceneIntegrationTests.cs`
- Regenerate: `Assets/Scenes/Demo_YanLiuTown.unity`

**Interfaces:**
- Produces `GameplaySceneIntegrator.AddDemoContent(Scene scene, string regionId)`.
- `DemoSceneGenerator.Generate()` first builds formal Yanliu, then adds managers, player, current NPCs, enemies, crates, quest targets, event triggers, HUD, dialogue, pause, and exits at validated anchors.

- [ ] **Step 1: Write failing formal-Demo tests**

Assert the generated scene contains formal Grid layers, three Yanliu landmarks, formal player/NPC/enemy art IDs, no runtime-generated texture references, and the existing managers/UI/quest components.

- [ ] **Step 2: Run focused EditMode tests and verify failure**

Expected: FAIL because current generator still constructs flat ground, walls, trees, signs, actors, and crates.

- [ ] **Step 3: Split gameplay composition from environment construction**

Delete `DrawGroundTiles`, `CreateWall` sprite construction, temporary tree/well/sign drawing, temporary player/NPC/enemy drawing, and crate texture drawing. Load formal Yanliu scene definition and place gameplay content by stable anchors.

- [ ] **Step 4: Map Demo props and actors to catalog IDs**

Use `innkeeper_zhao`, `su_wanqing`, `fishing_elder`, Yanliu enemy family IDs, formal crate/well/sign/landmark sprites, and the selected player art ID. Preserve current dialogue, quest, stats, colliders, loot, and event logic.

- [ ] **Step 5: Regenerate and reopen the Demo scene**

Run Unity batch compile/import, execute the generator, close Unity, reopen the scene in batch, and run formal-Demo tests. Expected: PASS with zero errors.

- [ ] **Step 6: Commit formal Yanliu Demo**

```bash
rtk git add Assets/Scripts/Editor/DemoSceneGenerator.cs Assets/Scripts/Editor/Art/GameplaySceneIntegrator.cs Assets/Scenes/Demo_YanLiuTown.unity Assets/Tests/EditMode
rtk git commit -m "feat: rebuild Demo with formal Yanliu art"
```

### Task 5: Integrate All Region and Interior Scenes into Build Settings

**Files:**
- Modify: `Assets/Scripts/Editor/SetupBuildSettings.cs`
- Modify: `Assets/Scripts/Map/AreaTrigger.cs`
- Modify: `Assets/Scripts/Core/GameManager.cs`
- Modify: `Assets/Tests/EditMode/FormalSceneIntegrationTests.cs`
- Modify generated scenes under `Assets/Scenes/Regions/`.

**Interfaces:**
- Produces build-scene IDs identical to region/interior stable IDs and valid entry-anchor travel.

- [ ] **Step 1: Write failing build-settings and travel tests**

Assert MainMenu, Demo, ten outdoor, and thirteen interior scenes are enabled in build settings exactly once. For each inter-scene exit, assert destination scene and destination anchor exist.

- [ ] **Step 2: Run focused tests and verify failure**

Expected: FAIL because build settings contain only current scenes and region exits are incomplete.

- [ ] **Step 3: Add deterministic build-settings setup**

Order MainMenu, Demo, outdoor regions, then interiors. Use canonical scene paths from `EnvironmentArtCatalog`; reject missing scene assets.

- [ ] **Step 4: Connect region/interior travel anchors**

Area transitions use catalog scene IDs and explicit destination anchors. Invalid destinations fail editor validation and never silently load the active scene.

- [ ] **Step 5: Run build-settings and scene-load tests**

Expected: all 25 scenes are enabled and every destination resolves.

- [ ] **Step 6: Commit scene integration**

Commit build settings, travel binding, tests, and regenerated formal scenes.

### Task 6: Add Deterministic Visual Regression Capture

**Files:**
- Create: `Assets/Scripts/Editor/Art/VisualRegressionCapture.cs`
- Create: `Assets/Tests/EditMode/VisualRegressionTests.cs`
- Create: `Assets/Tests/VisualBaselines/*.png`

**Interfaces:**
- Produces `VisualRegressionCapture.Capture(string sceneId, string cameraAnchor, string outputPath)` and perceptual-difference reports.

- [ ] **Step 1: Write failing capture tests**

Capture Yanliu twice from the same fixed camera and assert byte-identical PNG output. Capture each outdoor region and assert output is 480×270, non-empty, and uses more than 24 unique opaque colors.

- [ ] **Step 2: Run and verify failure**

Expected: compile/test failure because capture tool does not exist.

- [ ] **Step 3: Implement deterministic capture**

Disable time progression, animate at a fixed sample time, set a fixed camera anchor, render to 480×270 ARGB32, and write PNG. Restore all editor state in `finally` blocks.

- [ ] **Step 4: Create approved baselines**

Generate MainMenu and ten outdoor-region images. Inspect each at 1× and 4×. Baselines with missing sprites, solid-color majority, broken foreground, or illegible characters are rejected.

- [ ] **Step 5: Implement comparison thresholds**

Exact deterministic fixtures require zero pixel difference. Approved scene baselines allow at most 0.5% changed pixels for metadata/order-only rebuilds; visual changes require intentional baseline review.

- [ ] **Step 6: Commit capture tooling and baselines**

Commit capture code, tests, and approved PNG baselines.

### Task 7: Run End-to-End Unity QA and Fix Integration Bugs

**Files:**
- Modify only files implicated by failing tests or observed visual/runtime defects.
- Update: `Assets/Tests/PlayMode/FormalArtFlowPlayModeTests.cs` with fixed end-to-end coverage.

**Interfaces:**
- Produces one verified path: MainMenu → appearance selection → new game → formal Yanliu → NPC interaction → combat → pause/save/load → region/interior transition.

- [ ] **Step 1: Add the end-to-end PlayMode flow**

The test selects female mystic, starts a new game, asserts the spawned player uses `player_female_mystic`, moves, attacks once, interacts with innkeeper, pauses/resumes, saves, reloads, and verifies appearance plus position survive.

- [ ] **Step 2: Run the full EditMode suite**

Use the exact Unity command in `README.md`. Expected: all tests PASS and results XML is written.

- [ ] **Step 3: Run the full PlayMode suite**

Use `-testPlatform PlayMode` without `-quit`. Expected: all tests PASS and results XML is written.

- [ ] **Step 4: Run batch compile/import**

Run Unity with `-batchmode -quit -projectPath ... -logFile /tmp/yuanHaiLu-art-compile.log`. Expected: exit 0, zero compile errors, zero missing GUIDs.

- [ ] **Step 5: Perform manual Unity QA**

Open Unity, generate formal scenes, select each player profession/gender, play Yanliu, inspect NPCs/enemies/props, walk across bridge and doors, enter one interior, trigger combat and dialogue, save/load, and inspect all ten region screenshots. Record defects with scene ID, object ID, and screenshot before fixing.

- [ ] **Step 6: Re-run all affected tests after fixes**

Expected: Python, EditMode, PlayMode, validator, and visual regression all PASS; Console remains 0 error.

- [ ] **Step 7: Commit QA fixes**

```bash
rtk git add Assets ProjectSettings
rtk git commit -m "fix: complete formal art integration QA"
```

### Task 8: Update Memory, Handoff, and Public Setup Documentation

**Files:**
- Modify: `AGENTS.md`
- Modify: `README.md`
- Modify: `SETUP_GUIDE.md`
- Modify: `docs/01-art-style-guide.md`
- Create: `docs/HANDOFF-art-production.md`

**Interfaces:**
- Produces a single accurate entry path for future developers and agents.

- [ ] **Step 1: Write the handoff content**

Document stable IDs, exact roster/region counts, source and baked directories, build/validate commands, Unity menus, scene generation commands, animation parameters/events, save v4 migration, visual baseline workflow, and the latest test counts.

- [ ] **Step 2: Remove obsolete guidance**

Replace HD-2D recommendations and 48×48 role instructions with the confirmed pure-2D, 32×32 standard. Mark old `Assets/Sprites/Generated` and `Assets/Art/Tilesets/yanliu_town_*` as legacy or remove references after no scene uses them.

- [ ] **Step 3: Update project status and known issues**

`AGENTS.md` must state which formal assets/scenes are complete, the exact last verification commands/results, and any remaining non-art content gaps. It must not claim the whole game is complete merely because art production is complete.

- [ ] **Step 4: Validate documentation commands**

Run every documented Python build/validation command and both documented Unity test commands. Correct any path or option mismatch.

- [ ] **Step 5: Commit documentation and verify clean main**

```bash
rtk git add AGENTS.md README.md SETUP_GUIDE.md docs
rtk git commit -m "docs: hand off formal art production"
rtk git status --short
```

Expected: working tree is clean on `main`.

## Plan Completion Gate

- Main menu exposes exactly 12 player appearances and save v4 persists the selection.
- Demo and all gameplay spawn paths use formal catalog assets.
- All ten outdoor and thirteen interior scenes are enabled, loadable, and anchor-connected.
- No formal actor or environment object falls back to runtime color blocks.
- Full Python, EditMode, PlayMode, batch compile/import, visual regression, and manual QA pass.
- `AGENTS.md`, `README.md`, `SETUP_GUIDE.md`, `docs/01-art-style-guide.md`, and `docs/HANDOFF-art-production.md` reflect the implemented state.
- Final worktree is clean on `main`.
