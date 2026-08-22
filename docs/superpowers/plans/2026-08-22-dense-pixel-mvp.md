# Dense Pixel Jianghu MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (- [ ]) syntax for tracking.

**Goal:** Replace both MVP scenes’ full-frame background layers with original modular high-density pixel worlds and a readable 48×48 male hero, without breaking the MVP_01 playable loop.

**Architecture:** The formal-character pipeline gets one scoped 48px exception for player_male_swordsman, which still produces the existing controller, prefab and stable catalog entry. A deterministic MVP art builder writes small persistent modules and actor sprites. An editor-only module assembler places those sprites in Ground → Environment → Character → Foreground, replacing the old three 480×270 backdrops.

**Tech Stack:** Unity 6000.4.10f1 built-in 2D, C# / UnityEditor, Python 3 + Pillow, NUnit EditMode/PlayMode, existing tools.art_pipeline.

## Global Constraints

- Keep 480×270, PPU 16, Point filtering, uncompressed textures and no antialiasing.
- Keep the existing 16×16 collision grid, gameplay coordinates, QuestStageGate, MVP_01 semantics, save v5 and weapon-style IDs.
- player_male_swordsman is the sole 48px formal-character exception; every other formal character remains 32px.
- New world art is only 16×16, 32×32, 48×48 or 64×64 persistent PNG modules. No 480×270 PNG may render in either Demo scene.
- Do not create runtime Texture2D/Sprite assets, copy reference-game assets/layouts/silhouettes/UI, change ProjectSettings, touch formal non-MVP scenes or alter the other 11 player appearances.
- Every task starts red, reaches green, then commits before the next task.

---

## File Structure

| Path | Responsibility |
| --- | --- |
| tools/art_pipeline/schema.py | Scope the 48px player exception. |
| tools/art_pipeline/mvp_dense_art_builder.py | Bake original player source modules, actor sprites and town/inn modules. |
| tools/art_pipeline/tests/test_mvp_dense_art_builder.py | Assert dimensions, non-mirrored directions, palette contract and deterministic output. |
| Assets/ArtSource/MVP/dense_pixel/layouts/{town,inn}.json | Own exact persistent module placements. |
| Assets/ArtSource/MVP/dense_pixel and Assets/Art/MVP/dense_pixel | Editable source and imported world modules. |
| Assets/Resources/Art/MVP/dense_pixel | Persistent actor and weapon-layer images. |
| Assets/Scripts/Art/MvpWorldModule.cs | Stores asset path/layer/order on generated module objects. |
| Assets/Scripts/Editor/MvpSceneModuleAssembler.cs | Loads, validates and places persistent module sprites. |
| Assets/Scripts/Editor/MvpDenseSceneLayouts.cs | Converts two JSON layouts into world placements. |
| PlaySceneAssembler.cs, DemoSceneGenerator.cs, InnSceneGenerator.cs | Replace old backdrop calls with module layout builders. |
| MvpSceneWiringTests.cs, MainFlowPlayModeTests.cs, VisualRegressionCapture.cs | Assert structure, preserve gameplay and capture three 1× frames. |

---

### Task 1: Make the 48px male-hero exception explicit

**Files:**
- Modify: tools/art_pipeline/schema.py:74-92
- Modify: tools/art_pipeline/tests/test_character_baker.py
- Modify: Assets/Scripts/Core/GameConfig.cs:13-17
- Modify: Assets/Tests/EditMode/CharacterArtTests.cs

**Interfaces:**
- Consumes: CharacterRecipe.from_dict(payload).
- Produces: only CharacterRecipe("player_male_swordsman", 48, ...) may use 48px.

- [ ] **Step 1: Write the failing test**

~~~python
def test_only_fixed_male_player_can_use_a_48_pixel_frame(self):
    payload = {
        "id": "player_male_swordsman", "frameSize": 48,
        "modules": ["body.png"], "animations": [],
    }
    self.assertEqual(CharacterRecipe.from_dict(payload).frame_size, 48)

    payload["id"] = "player_female_swordsman"
    with self.assertRaisesRegex(ManifestError, "frameSize must be 32"):
        CharacterRecipe.from_dict(payload)
~~~

- [ ] **Step 2: Verify red**

Run: python3 -m unittest tools.art_pipeline.tests.test_character_baker -v

Expected: the new test fails because the schema permits only frame size 32.

- [ ] **Step 3: Implement the exact narrow rule**

~~~python
allowed_frame_sizes = {32}
if art_id == "player_male_swordsman":
    allowed_frame_sizes.add(48)
if frame_size not in allowed_frame_sizes:
    expected = " or ".join(str(size) for size in sorted(allowed_frame_sizes))
    raise ManifestError("{} frameSize must be {}".format(art_id, expected))
~~~

Add public const int MVP_HERO_FRAME_SIZE = 48 next to CHARACTER_FRAME_SIZE; retain the latter at 32.

- [ ] **Step 4: Verify green**

Run the Python test. Confirm all non-default recipes still validate at 32px.

- [ ] **Step 5: Commit**

~~~bash
git add tools/art_pipeline/schema.py tools/art_pipeline/tests/test_character_baker.py Assets/Scripts/Core/GameConfig.cs Assets/Tests/EditMode/ArtAssetTests.cs
git commit -m "feat(art): allow 48px MVP male hero frames"
~~~

### Task 2: Bake the male hero master and three weapon layers

**Files:**
- Create: tools/art_pipeline/mvp_dense_art_builder.py
- Create: tools/art_pipeline/tests/test_mvp_dense_art_builder.py
- Modify: tools/art_pipeline/build.py
- Modify: Assets/Scripts/Editor/Art/CharacterAnimationBuilder.cs
- Modify: Assets/ArtSource/Characters/Manifests/player-roster.json
- Modify: Assets/ArtSource/Characters/Generated/player_male_swordsman/body.png
- Modify: Assets/ArtSource/Characters/Generated/player_male_swordsman/face.png
- Modify: Assets/ArtSource/Characters/Generated/player_male_swordsman/hair.png
- Modify: Assets/ArtSource/Characters/Generated/player_male_swordsman/outfit.png
- Modify: Assets/ArtSource/Characters/Generated/player_male_swordsman/weapon.png
- Modify: Assets/ArtSource/Characters/Generated/player_male_swordsman/accessory.png
- Modify: Assets/Resources/Art/MVP/weapon_sword.png
- Modify: Assets/Resources/Art/MVP/weapon_gauntlets.png
- Modify: Assets/Resources/Art/MVP/weapon_dart.png

**Interfaces:**
- Consumes: roster animation rows and the Task-1 schema rule.
- Produces: build_dense_mvp_art(project_root) -> tuple[Path, ...], six aligned 48px source modules, the existing MvpArtCatalog.Load("weapon_<style>") sprites and CharacterAnimationBuilder.RebuildOnly("player_male_swordsman").

- [ ] **Step 1: Write the failing source-art test**

~~~python
def test_dense_hero_has_48_pixel_nonmirrored_directions_and_three_weapon_layers(self):
    build_dense_mvp_art(self.root)
    hero = load_player_recipe(self.root, "player_male_swordsman")
    self.assertEqual(hero["frameSize"], 48)
    self.assertEqual(crop_direction_hash_count(self.root, "idle"), 4)
    self.assertEqual(crop_direction_hash_count(self.root, "walk"), 4)
    self.assertEqual(crop_direction_hash_count(self.root, "attack_1"), 4)
    self.assertEqual(len(set(load_weapon_hashes(self.root))), 3)
~~~

Direction helpers hash cropped RGBA frame bytes rather than file names. Add a second test that a second build writes no changed files.
Add an EditMode test that the rebuilt male catalog sprite rect is 48×48 while player_female_swordsman remains 32×32; this assertion must fail until the player source is rebuilt.

- [ ] **Step 2: Verify red**

Run: python3 -m unittest tools.art_pipeline.tests.test_mvp_dense_art_builder -v

Expected: import failure because the dense builder does not exist.

- [ ] **Step 3: Implement the 1× source master**

Implement build_dense_mvp_art with source-scale RGBA Pillow canvases. It writes all six current player module sheets at roster dimensions and redraws the three existing weapon layers under Assets/Resources/Art/MVP, preserving their stable IDs for MainMenu and PlayerCombat.

Every direction contains an 8–10px dark hair knot, 20–24px indigo short cloak, 12–18px paper inner robe, 4–6px vermilion waist band, 18–28px weapon silhouette and 3–5px ground shadow. Draw left/right independently. Preserve existing idle, walk, dash and attack rows/hit frames. Change only the fixed male roster entry to frameSize 48. Call build_dense_mvp_art(PROJECT_ROOT) before manifest bakes in build.py.

- [ ] **Step 4: Verify green**

~~~bash
python3 -m unittest tools.art_pipeline.tests.test_mvp_dense_art_builder -v
python3 -m tools.art_pipeline.build --manifest Assets/ArtSource/Characters/Manifests/player-roster.json
python3 -m tools.art_pipeline.validate --all
~~~

Add CharacterAnimationBuilder.RebuildOnly(string stableArtId): it finds the exact character metadata entry, applies import rules to that sheet, calls BuildCharacter only for that entry, then runs ArtCatalogBuilder.RebuildAll without deleting controller/prefab roots. Invoke RebuildOnly("player_male_swordsman") from the batch entry point. Confirm player_male_swordsman.art.json has frameSize 48 and only the fixed player controller/prefab/catalog entry changed.

- [ ] **Step 5: Commit**

~~~bash
git add tools/art_pipeline/mvp_dense_art_builder.py tools/art_pipeline/tests/test_mvp_dense_art_builder.py tools/art_pipeline/build.py Assets/Scripts/Editor/Art/CharacterAnimationBuilder.cs Assets/ArtSource/Characters/Manifests/player-roster.json Assets/ArtSource/Characters/Generated/player_male_swordsman Assets/Art/Characters/Player/player_male_swordsman* Assets/Resources/Art/MVP/weapon_sword.png Assets/Resources/Art/MVP/weapon_gauntlets.png Assets/Resources/Art/MVP/weapon_dart.png Assets/AnimatorControllers/Characters/Player Assets/Prefabs/Characters/Player
git commit -m "feat(art): author 48px male hero master"
~~~

### Task 3: Introduce persistent small world modules

**Files:**
- Create: Assets/Scripts/Art/MvpWorldModule.cs
- Create: Assets/Scripts/Editor/MvpSceneModuleAssembler.cs
- Create: Assets/Scripts/Editor/MvpDenseSceneLayouts.cs
- Modify: Assets/Scripts/Editor/PlaySceneAssembler.cs:88-160
- Modify: Assets/Tests/EditMode/MvpSceneWiringTests.cs:330-379

**Interfaces:**
- Consumes: asset path, MvpWorldLayer, world position and sorting order.
- Produces: MvpSceneModuleAssembler.Place(GameObject root, MvpScenePlacement placement) -> SpriteRenderer.

- [ ] **Step 1: Write the failing module test**

~~~csharp
[Test]
public void DemoScenesUsePersistentSmallWorldModulesInsteadOfFullFrameLayers()
{
    foreach (var scenePath in DemoScenePaths)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var modules = Object.FindObjectsByType<MvpWorldModule>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.That(modules, Is.Not.Empty);
        Assert.That(GameObject.Find("[MVP Ground]"), Is.Null);
        Assert.That(GameObject.Find("[MVP Environment]"), Is.Null);
        Assert.That(GameObject.Find("[MVP Foreground]"), Is.Null);
        Assert.That(modules.All(module => module.Sprite.rect.size.x <= 64f), Is.True);
        Assert.That(modules.All(module => AssetDatabase.Contains(module.Sprite)), Is.True);
    }
}
~~~

Also assert the three bands resolve to Default, Environment and Foreground.

- [ ] **Step 2: Verify red**

Run the targeted EditMode fixture. Expected: it sees three 480×270 backdrops and no MvpWorldModule.

- [ ] **Step 3: Implement the fail-fast module assembler**

~~~csharp
public enum MvpWorldLayer { Ground, Environment, Foreground }

public readonly struct MvpScenePlacement
{
    public readonly string AssetPath;
    public readonly Vector2 Position;
    public readonly MvpWorldLayer Layer;
    public readonly int SortingOrder;
    public MvpScenePlacement(string assetPath, Vector2 position,
        MvpWorldLayer layer, int sortingOrder) { /* assign all fields */ }
}
~~~

Place loads its sprite with AssetDatabase.LoadAssetAtPath<Sprite>; it throws InvalidOperationException containing the asset path when absent or larger than 64px. It creates a child of [MVP Visual Root], assigns SpriteRenderer and MvpWorldModule, then uses Default/-100 for Ground, Environment/0 for Environment and Foreground/0 for Foreground. Use explicit Unity == null checks.

Delete CreateMvpSceneLayers and its old image-import helper. PlaySceneAssembler.CreateMvpVisualRoot returns only the root, never a frame-sized renderer.

- [ ] **Step 4: Verify green**

Make MvpDenseSceneLayouts.BuildTown and BuildInn temporarily place one valid 16px ground module, then run the targeted fixture and git diff --check.

- [ ] **Step 5: Commit**

~~~bash
git add Assets/Scripts/Art/MvpWorldModule.cs Assets/Scripts/Editor/MvpSceneModuleAssembler.cs Assets/Scripts/Editor/MvpDenseSceneLayouts.cs Assets/Scripts/Editor/PlaySceneAssembler.cs Assets/Tests/EditMode/MvpSceneWiringTests.cs
git commit -m "refactor(art): assemble MVP worlds from persistent modules"
~~~

### Task 4: Build the smoke-willow town route

**Files:**
- Modify: tools/art_pipeline/mvp_dense_art_builder.py and tools/art_pipeline/tests/test_mvp_dense_art_builder.py
- Create: Assets/ArtSource/MVP/dense_pixel/layouts/town.json
- Create: Assets/ArtSource/MVP/dense_pixel/environment/town/*.png
- Create: Assets/Art/MVP/dense_pixel/environment/town/*.png
- Create: Assets/Resources/Art/MVP/dense_pixel/actors/mvp_bandit_a.png
- Create: Assets/Resources/Art/MVP/dense_pixel/actors/mvp_bandit_b.png
- Create: Assets/Resources/Art/MVP/dense_pixel/actors/mvp_lost_pouch.png
- Modify: MvpDenseSceneLayouts.cs, DemoSceneGenerator.cs:17-135,420-535 and MvpSceneWiringTests.cs

**Interfaces:**
- Consumes: existing town spawn (7.5, 7.6), inn door, riverbank combat locations and pouch location.
- Produces: BuildTown(GameObject root) with role-complete modules and 48px bandits/16px pouch.

- [ ] **Step 1: Write failing town tests**

~~~python
def test_town_modules_cover_required_readability_roles(self):
    layout = load_layout(self.root, "town")
    self.assertEqual(set(layout["roles"]), {
        "road", "water", "shore", "inn_roof", "inn_wall", "inn_door",
        "bridge", "boat", "bollard", "lantern", "foreground_foliage",
    })
    self.assertTrue(all(load_size(self.root, item["asset"]) in ALLOWED_MODULE_SIZES
                        for item in layout["placements"]))
~~~

Add EditMode checks for a module near spawn, inn door, riverbank and pouch, plus the current BFS path contract.

- [ ] **Step 2: Verify red**

Run the Python test and focused EditMode fixture. Expected: layout and assets are absent.

- [ ] **Step 3: Author and integrate town modules**

Create three stone-road variants; three water variants; four shore/canal edges; inn roof/wall/door/sign; bridge deck/arch/rail; boat hull/sail; bollard; lantern; crate/sack; near/far willow; edge-only foreground willow/roof trim. Use the frozen palette roles. Do not fill water with uniform horizontal strokes.

Write asset, world position, layer and sorting order into town.json. Center a multi-module inn on the current door, create a continuous stone route from (7.5, 7.6) to riverbank, and preserve all existing blockers. Place 1–3 warm lantern focal points. Replace three legacy town backdrop constants/call with BuildTown. Bind bandits/pouch through MvpStaticVisual using new IDs.

- [ ] **Step 4: Verify green**

~~~bash
python3 -m unittest tools.art_pipeline.tests.test_mvp_dense_art_builder -v
python3 -m tools.art_pipeline.build --all
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/lhw/code/yuanHaiLu -executeMethod YuanHaiLu.Editor.DemoSceneGenerator.GenerateFromCommandLine -logFile /private/tmp/yuanhailu-dense-town-generate.log
~~~

Run focused EditMode. Inspect regenerated town capture at 1×: hero, door, route, riverbank and two bandits must all be identifiable.

- [ ] **Step 5: Commit**

~~~bash
git add tools/art_pipeline/mvp_dense_art_builder.py tools/art_pipeline/tests/test_mvp_dense_art_builder.py Assets/ArtSource/MVP/dense_pixel Assets/Art/MVP/dense_pixel Assets/Resources/Art/MVP/dense_pixel/actors Assets/Scripts/Editor/MvpDenseSceneLayouts.cs Assets/Scripts/Editor/DemoSceneGenerator.cs Assets/Tests/EditMode/MvpSceneWiringTests.cs Assets/Scenes/Demo_YanLiuTown.unity
git commit -m "feat(art): build dense modular yanliu MVP route"
~~~

### Task 5: Build the functional inn interior

**Files:**
- Modify: mvp_dense_art_builder.py, test_mvp_dense_art_builder.py, MvpDenseSceneLayouts.cs, InnSceneGenerator.cs:17-120,155-166 and MvpSceneWiringTests.cs
- Create: Assets/ArtSource/MVP/dense_pixel/layouts/inn.json
- Create: Assets/ArtSource/MVP/dense_pixel/environment/inn/*.png
- Create: Assets/Art/MVP/dense_pixel/environment/inn/*.png
- Create: Assets/Resources/Art/MVP/dense_pixel/actors/mvp_innkeeper.png

**Interfaces:**
- Consumes: innkeeper (15, 10) and exit trigger (15, 1.8).
- Produces: BuildInn(GameObject root) and persistent 48px dense_pixel/actors/mvp_innkeeper.

- [ ] **Step 1: Write failing interior tests**

~~~python
def test_inn_layout_has_required_roles_and_limited_foreground(self):
    layout = load_layout(self.root, "inn")
    self.assertEqual(set(layout["roles"]), {
        "entrance", "walkway", "counter", "innkeeper_light", "table",
        "kitchen_fire", "stairs", "north_exit", "foreground_beam",
    })
    self.assertLessEqual(sum(item["area"] for item in layout["foreground"]), 19440)
~~~

Add an EditMode BFS assertion that entrance → innkeeper interaction tile → north exit stays clear, every module collider stays outside the path, and innkeeper sprite is persistent 48×48.

- [ ] **Step 2: Verify red**

Run the Python test and focused EditMode fixture. Expected: the layout/assets and corridor assertion fail.

- [ ] **Step 3: Author and integrate inn modules**

Create wood-floor variants, stone entry, beam/wall, counter front/back, ledger/jar, table/chair, stove/fire, stair/landing, window-light, door/rug, narrow foreground curtain/beam. Counter + innkeeper are the primary warm focus; stove/window are secondary. Entrance→counter→exit corridor uses floor modules only; tables, stairs and beams occupy side zones.

Implement BuildInn from the placement array. Replace the old three inn backdrop constants/call. Bind current innkeeper with MvpStaticVisual.ApplyTo(npc, "dense_pixel/actors/mvp_innkeeper") and leave NPCBase, QuestGiver and collider unchanged.

- [ ] **Step 4: Verify green**

~~~bash
python3 -m unittest tools.art_pipeline.tests.test_mvp_dense_art_builder -v
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/lhw/code/yuanHaiLu -executeMethod YuanHaiLu.Editor.InnSceneGenerator.GenerateFromCommandLine -logFile /private/tmp/yuanhailu-dense-inn-generate.log
~~~

Run focused EditMode and inspect 1× inn capture: player, counter, innkeeper, route, stove and stairs are identifiable.

- [ ] **Step 5: Commit**

~~~bash
git add tools/art_pipeline/mvp_dense_art_builder.py tools/art_pipeline/tests/test_mvp_dense_art_builder.py Assets/ArtSource/MVP/dense_pixel Assets/Art/MVP/dense_pixel Assets/Resources/Art/MVP/dense_pixel/actors Assets/Scripts/Editor/MvpDenseSceneLayouts.cs Assets/Scripts/Editor/InnSceneGenerator.cs Assets/Tests/EditMode/MvpSceneWiringTests.cs Assets/Scenes/Demo_Inn.unity
git commit -m "feat(art): build dense modular inn MVP interior"
~~~

### Task 6: Capture 1× review frames and close technical verification

**Files:**
- Modify: Assets/Scripts/Editor/VisualRegressionCapture.cs
- Modify: Assets/Tests/EditMode/MvpSceneWiringTests.cs
- Modify: Assets/Tests/PlayMode/MainFlowPlayModeTests.cs
- Modify: docs/17-mvp-art-integration-rework.md
- Modify: AGENTS.md

**Interfaces:**
- Consumes: rebuilt dense scenes and VisualRegressionCapture.CaptureMvpGameplay(directory).
- Produces: unscaled town-spawn-1x.png, town-riverbank-1x.png, inn-counter-1x.png, fresh XML/logs and Gate-V evidence.

- [ ] **Step 1: Write failing capture/play tests**

~~~csharp
[Test]
public void DenseMvpCaptureCreatesThreeFullLogicalFrames()
{
    var directory = Path.Combine(Path.GetTempPath(), "yuanhailu-dense-mvp-review");
    VisualRegressionCapture.CaptureMvpGameplay(directory);
    foreach (var name in new[] { "town-spawn-1x.png", "town-riverbank-1x.png", "inn-counter-1x.png" })
        Assert.That(new FileInfo(Path.Combine(directory, name)).Length, Is.GreaterThan(2048));
}
~~~

Extend current town→inn→town PlayMode test to assert the 48px player remains bound and two bandits/one pouch still follow their current QuestStageGate stages.

- [ ] **Step 2: Verify red**

Run focused fixtures. Expected: the capture test fails until it targets the dense module scenes and fixed review locations.

- [ ] **Step 3: Implement capture/documentation**

Keep CaptureMvpGameplay(directory) public. Set three target positions to regenerated hero, riverbank and counter locations. It saves native 480×270 PNGs without crop, scale or annotation and restores active scene, camera target, RenderTexture and Canvas in finally.

Replace docs/17’s old backdrop explanation with the module architecture, directories, fixed actor IDs, commands and Gate-V manual criteria. Update AGENTS.md only after final test outputs exist.

- [ ] **Step 4: Run final verification after final implementation commit**

~~~bash
python3 -m unittest discover -s tools/art_pipeline/tests -v
python3 -m tools.art_pipeline.build --all
python3 -m tools.art_pipeline.validate --all
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/lhw/code/yuanHaiLu -runTests -testPlatform EditMode -testResults /private/tmp/yuanhailu-dense-final-editmode.xml -logFile /private/tmp/yuanhailu-dense-final-editmode.log
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/lhw/code/yuanHaiLu -runTests -testPlatform PlayMode -testResults /private/tmp/yuanhailu-dense-final-playmode.xml -logFile /private/tmp/yuanhailu-dense-final-playmode.log
git diff --check main...HEAD
~~~

Capture into /private/tmp/yuanhailu-dense-mvp-review/, inspect each PNG at 1×, and restore Unity’s known unrelated scene serialization churn before status review.

- [ ] **Step 5: Commit evidence and stop at Gate V**

~~~bash
git add Assets/Scripts/Editor/VisualRegressionCapture.cs Assets/Tests/EditMode/MvpSceneWiringTests.cs Assets/Tests/PlayMode/MainFlowPlayModeTests.cs docs/17-mvp-art-integration-rework.md AGENTS.md
git commit -m "test(art): verify dense MVP presentation"
~~~

Do not merge or push. Deliver the three 1× screenshots, XML/log paths, git diff --check result and Gate-V manual checklist for user approval.

## Plan Self-Review

- Design §§1–3 map to Tasks 1–2 (male hero) and 4–5 (town/inn visual scripts).
- Design §4 maps to Task 3 (persistent module architecture) and Tasks 4–5 (placement data).
- Design §5 maps to Tasks 3–6 (fail-fast import plus unchanged gameplay).
- Design §§6–7 map to Task 6 (automated evidence and the user visual gate).
- The plan has no new region, roster, system or ProjectSettings work; each task has a named red/green check and commit boundary.
