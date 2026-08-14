# Formal Pixel Art Rebuild Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver rebuildable, visually distinct 32×32 formal characters and fully data-driven 40×24 formal scenes for the existing 2D Unity game.

**Architecture:** Keep the existing deterministic PNG/metadata pipeline, but add committed character/environment design specifications and source builders that produce the editable pixel modules referenced by manifests. Convert every formal layout into data that the Unity scene builder consumes directly; generated scenes, prefabs, controllers and visual captures are derived outputs and never the source of truth.

**Tech Stack:** Unity `6000.4.10f1`, C#, Unity Test Framework, Python 3 `unittest`, Pillow, existing deterministic art pipeline, ImageGen concept references.

## Global Constraints

- Work only on `codex/art-production-rebuild`; merge target remains `main` after independent validation and user approval.
- Keep pure 2D, internal resolution `480×270`, Tile `16×16`, character frames `32×32`, PPU `16`, Point filtering and uncompressed textures.
- Retain `PlayerAppearance.DefaultArtId == "player_female_swordsman"`, `SaveData.saveVersion == 4`, and the `QuestTarget` success-before-lock contract.
- Do not add URP, 3D, a Unity-version change, runtime Texture2D/Sprite art fallbacks, untracked source modules, `Library/`, `Temp/`, logs, `.csproj`, `.sln`, `.vscode`, `.zcode`, or `docs/superpowers/`.
- All shell commands use `rtk`.
- A changed visual baseline is accepted only after a fresh temporary capture and explicit human review; a test must never mark an unreviewed image as approved.

---

## File Structure

| File | Responsibility |
|---|---|
| `Assets/ArtSource/Concepts/` | Non-runtime concept reference sheets generated for the confirmed A direction. |
| `Assets/ArtSource/Characters/Designs/character-designs.json` | One explicit silhouette/palette/prop design record for every stable character ID. |
| `tools/art_pipeline/character_source_builder.py` | Deterministically writes six committed 32px layered source sheets per character from the design records and animation rows. |
| `tools/art_pipeline/source_audit.py` | Rejects a manifest whose referenced source PNG is absent, incorrectly sized, empty, or shared by two characters where the layer requires uniqueness. |
| `Assets/ArtSource/Environment/Designs/environment-designs.json` | Region/interior palette, landmark and tile-role design records, including prologue state variants. |
| `tools/art_pipeline/environment_source_builder.py` | Deterministically writes 16px tile modules and landmark modules from environment design records. |
| `Assets/ArtSource/Environment/Layouts/**/*.json` | Explicit layer cells, collision cells, foreground spans and anchors for all 23 scenes. |
| `tools/art_pipeline/map_layout.py` | Validates the layout schema, reachability and coordinate-only uniqueness. |
| `Assets/Scripts/Editor/Art/RegionSceneBuilder.cs` | Reads layout data and creates persistent Tilemaps, landmark objects, collision objects and scene definitions without structural formula fallbacks. |
| `Assets/Scripts/Art/RegionEnvironmentController.cs` | Applies strict weather and prologue `normal`/`burned` environment variants without changing anchors or collisions. |
| `Assets/Scripts/Editor/Art/CharacterAnimationBuilder.cs` | Builds all directional animation states and reachable runtime combat transitions. |
| `Assets/Scripts/Editor/Art/CharacterShowcaseGenerator.cs` and `CharacterShowcaseWindow.cs` | Provide 97 stable labels plus action and scale preview controls. |
| `Assets/Scripts/Editor/Art/VisualRegressionCapture.cs` | Captures fixed 480×270 scenes while restoring scene/editor/render state in `finally`. |
| `Assets/Tests/EditMode/*ArtTests.cs`, `Assets/Tests/PlayMode/*Tests.cs`, `tools/art_pipeline/tests/` | Verify source completeness, silhouette uniqueness, layout consumption, environment state, runtime animation and visual capture behavior. |

### Task 1: Establish rebuildable source-art contracts

**Files:**
- Create: `tools/art_pipeline/source_audit.py`
- Create: `tools/art_pipeline/tests/test_source_audit.py`
- Modify: `tools/art_pipeline/character_modules.py`
- Modify: `tools/art_pipeline/tests/test_character_roster.py`

**Interfaces:**
- Consumes: `CharacterRecipe.id`, `CharacterRecipe.modules`, animation metadata and project-root-relative PNG paths.
- Produces: `audit_character_sources(recipes) -> list[str]` and `assert_character_sources_complete(recipes) -> None`.

- [ ] **Step 1: Write the source-completeness failure test.**

```python
def test_character_source_audit_reports_missing_and_shared_unique_modules(tmp_path):
    recipes = [
        FakeRecipe("hero_a", (str(tmp_path / "missing.png"),) * 6),
        FakeRecipe("hero_b", (str(tmp_path / "shared.png"),) * 6),
    ]
    errors = audit_character_sources(recipes, unique_layers={"hair", "outfit", "weapon", "accessory"})
    assert any("hero_a" in error and "missing" in error for error in errors)
```

Define the fixture in the same test file so the contract has no Unity dependency:

```python
from dataclasses import dataclass

@dataclass(frozen=True)
class FakeRecipe:
    id: str
    modules: tuple
```

- [ ] **Step 2: Run the focused test before implementation.**

Run: `rtk python3 -m unittest tools.art_pipeline.tests.test_source_audit -v`

Expected: FAIL because `source_audit` is not importable.

- [ ] **Step 3: Implement deterministic source validation.**

```python
REQUIRED_LAYERS = ("body", "face", "hair", "outfit", "weapon", "accessory")

def audit_character_sources(recipes, unique_layers=REQUIRED_LAYERS[2:]):
    errors, owners = [], {}
    for recipe in recipes:
        for layer, raw_path in zip(REQUIRED_LAYERS, recipe.modules):
            path = Path(raw_path)
            if not path.is_file():
                errors.append(f"{recipe.id} {layer} source missing: {path}")
                continue
            if layer in unique_layers:
                owners.setdefault((layer, path.as_posix()), []).append(recipe.id)
    for (layer, path), ids in owners.items():
        if len(ids) > 1:
            errors.append(f"{layer} source shared by {', '.join(sorted(ids))}: {path}")
    return errors

def assert_character_sources_complete(recipes):
    errors = audit_character_sources(recipes)
    if errors:
        raise ValueError("\n".join(errors))
```

- [ ] **Step 4: Run the focused Python audit suite.**

Run: `rtk python3 -m unittest tools.art_pipeline.tests.test_source_audit -v`

Expected: PASS. Do not add the full-roster zero-error assertion until Task 2 has written all sources.

- [ ] **Step 5: Commit the contract-only change.**

```bash
rtk git add tools/art_pipeline/source_audit.py tools/art_pipeline/character_modules.py \
  tools/art_pipeline/tests/test_source_audit.py tools/art_pipeline/tests/test_character_roster.py
rtk git commit -m "test: require complete unique character art sources"
```

### Task 2: Produce distinct 32×32 formal character sources

**Files:**
- Create: `Assets/ArtSource/Concepts/characters-a-direction.png`
- Create: `Assets/ArtSource/Characters/Designs/character-designs.json`
- Create: `tools/art_pipeline/character_source_builder.py`
- Create: `tools/art_pipeline/tests/test_character_source_builder.py`
- Modify: all five files under `Assets/ArtSource/Characters/Manifests/`
- Create: `Assets/ArtSource/Characters/Generated/<stable-id>/{body,face,hair,outfit,weapon,accessory}.png` and matching `.meta` files

**Interfaces:**
- Consumes: `CharacterDesign(id, silhouette, palette, hair_style, outfit_style, prop_style, accent_style)` and the recipe animation rows.
- Produces: six non-empty RGBA sheets per stable ID, each exactly `(max animation frames × 32) × (animation rows × 32)`, and manifest module paths under `Generated/<stable-id>/`.

- [ ] **Step 1: Generate and save the approved A-direction character concept sheet.**

Use ImageGen with this prompt: `top-down 2D pixel art character design sheet for a Chinese wuxia RPG, 4-head-tall 32x32 sprite silhouettes, six roles: white-blue swordsman with long sword, red-black boxer with wraps, black-purple hidden-weapon rogue with belt pouch, green-white healer with medicine gourd, blue-brown scholar with scroll, purple-gold mystic with talismans; crisp clusters, selective dark ink outlines, transparent-free parchment presentation, no text, no 3D`.

Save the approved reference as `Assets/ArtSource/Concepts/characters-a-direction.png` with a Unity `.meta` file; this image is reference-only and is never loaded at runtime.

- [ ] **Step 2: Write the failing design-record coverage test.**

```python
def test_every_roster_id_has_a_unique_visual_design_record():
    designs = load_character_designs(DESIGN_PATH)
    roster_ids = {recipe.id for recipe in build_roster()}
    assert set(designs) == roster_ids
    assert len({design.signature for design in designs.values()}) == len(roster_ids)
```

- [ ] **Step 3: Create all 97 explicit design records.**

`character-designs.json` must contain one object for each stable ID, with the fields below. Player profession records use the confirmed role vocabulary; named, NPC, enemy and boss records use their story role, region and a non-duplicated `silhouette`/`propStyle` pair.

```json
{
  "player_female_swordsman": {
    "silhouette": "slender_long_coat",
    "palette": ["ink_blue", "paper_white", "river_blue"],
    "hairStyle": "high_ponytail",
    "outfitStyle": "split_hem_robe",
    "propStyle": "long_sword",
    "accentStyle": "blue_sash"
  }
}
```

- [ ] **Step 4: Implement the pixel source builder.**

```python
def build_character_sources(recipe, design, destination):
    width = max(row.frames for row in recipe.animations) * 32
    height = len(recipe.animations) * 32
    layers = {name: Image.new("RGBA", (width, height), (0, 0, 0, 0)) for name in REQUIRED_LAYERS}
    for row_index, row in enumerate(recipe.animations):
        for frame_index in range(row.frames):
            draw_pose(layers, design, row.name, row.direction, frame_index, row_index * 32)
    write_layer_pngs(layers, destination)
```

`draw_pose` must use integer pixel rectangles, stepped diagonals and palette-role colors only. It draws a unique body mass, hair/hat, outfit hem, weapon/prop and accessory for every design record; `idle`, `walk`, `hurt`, `death`, `attack_1`, `attack_2`, `attack_3`, `skill_1`, `skill_2` and `dash` have distinct frame offsets or poses when declared.

- [ ] **Step 5: Rewrite manifest module paths to per-character generated sources.**

```json
"modules": [
  "Assets/ArtSource/Characters/Generated/player_female_swordsman/body.png",
  "Assets/ArtSource/Characters/Generated/player_female_swordsman/face.png",
  "Assets/ArtSource/Characters/Generated/player_female_swordsman/hair.png",
  "Assets/ArtSource/Characters/Generated/player_female_swordsman/outfit.png",
  "Assets/ArtSource/Characters/Generated/player_female_swordsman/weapon.png",
  "Assets/ArtSource/Characters/Generated/player_female_swordsman/accessory.png"
]
```

- [ ] **Step 6: Add the zero-error production assertion and run source generation.**

Add the assertion in `test_character_roster.py`:

```python
def test_all_formal_character_sources_are_complete_and_unique(self):
    self.assertEqual(audit_character_sources(build_roster()), [])
```

Run: `rtk python3 -m tools.art_pipeline.character_source_builder --all`

- [ ] **Step 7: Run the Python character suite.**

Run: `rtk python3 -m unittest tools.art_pipeline.tests.test_character_source_builder tools.art_pipeline.tests.test_source_audit tools.art_pipeline.tests.test_character_roster -v`

Expected: all 97 design records, 582 module PNGs and their manifest references pass; 97 visual signatures are unique.

- [ ] **Step 8: Bake and validate the formal character output.**

Run: `rtk python3 -m tools.art_pipeline.build --all`

Run: `rtk python3 -m tools.art_pipeline.validate --all`

Expected: both return exit code 0, and a second build reports only skipped outputs.

- [ ] **Step 9: Commit the character source pass.**

```bash
rtk git add Assets/ArtSource/Concepts Assets/ArtSource/Characters tools/art_pipeline Assets/Art/Characters
rtk git commit -m "feat: build distinct formal pixel character sources"
```

### Task 3: Make all required character actions reachable and reviewable

**Files:**
- Modify: `Assets/Scripts/Character/PlayerController.cs`
- Modify: `Assets/Scripts/Character/PlayerCombat.cs`
- Modify: `Assets/Scripts/Editor/Art/CharacterAnimationBuilder.cs`
- Modify: `Assets/Scripts/Editor/Art/CharacterShowcaseGenerator.cs`
- Create: `Assets/Scripts/Editor/Art/CharacterShowcaseWindow.cs` and `.meta`
- Modify: `Assets/Tests/EditMode/CharacterArtTests.cs`
- Modify: `Assets/Tests/PlayMode/CharacterAnimationPlayModeTests.cs`

**Interfaces:**
- Consumes: animator parameters `Facing` (`int` 0=down, 1=left, 2=right, 3=up), `Speed`, `IsDashing`, `IsAttacking`, `AttackIndex`.
- Produces: four-direction idle/walk/dash and attack state transitions; public showcase actions `idle`, `walk`, `dash`, `attack1`, `attack2`, `attack3`, `skill1`, `skill2`, `hurt`, `death`; scale values `1`, `4`, `8`.

- [ ] **Step 1: Write failing animation reachability tests.**

```csharp
[TestCase("attack1", "attack_1_down")]
[TestCase("skill1", "skill_1_left")]
public void ShowcaseActionMapsStableApiToAnimatorState(string actionId, string stateName)
{
    var window = ScriptableObject.CreateInstance<CharacterShowcaseWindow>();
    Assert.That(window.AnimatorStateFor(actionId, 1), Is.EqualTo(stateName));
}

[UnityTest]
public IEnumerator PlayerCombatAttackEntersAttackStateAndRaisesHitEvent()
{
    animator.SetInteger("Facing", 0);
    animator.SetInteger("AttackIndex", 0);
    animator.SetBool("IsAttacking", true);
    animator.Update(0.05f);
    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("attack_1_down"), Is.True);
    combat.OnAttackHitFrame();
    Assert.That(hitCount, Is.EqualTo(1));
    yield return null;
}
```

- [ ] **Step 2: Run the focused Unity tests before implementation.**

Run: `rtk /Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/lhw/code/yuanHaiLu -runTests -testPlatform EditMode -testFilter YuanHaiLu.Tests.EditMode.CharacterArtTests -testResults /private/tmp/yuanhailu-character-red.xml -logFile /private/tmp/yuanhailu-character-red.log`

Expected: FAIL because the showcase window and stable action mapping do not exist.

- [ ] **Step 3: Add a single directional parameter path.**

```csharp
private static int FacingIndex(Vector2 direction)
{
    if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)) return direction.x < 0f ? 1 : 2;
    return direction.y > 0f ? 3 : 0;
}

_anim.SetInteger(Animator.StringToHash("Facing"), FacingIndex(_lastDirection));
```

Add `Facing` in `CharacterAnimationBuilder.AddParameters`; preserve the existing `Speed`, dash and combat parameter names so gameplay scripts remain compatible.

- [ ] **Step 4: Build real Animator transitions.**

For each direction, create idle↔walk transitions on `Speed`, Any State→dash on `IsDashing && Facing`, and Any State→`attack_<1..3>_<direction>` on `IsAttacking && AttackIndex && Facing`. Each attack clip retains `OnAttackHitFrame` and `OnAttackAnimationEnd` events; add a state exit transition back to matching idle only when `IsAttacking == false`.

- [ ] **Step 5: Implement the character showcase.**

```csharp
public static readonly string[] SupportedActions =
{ "idle", "walk", "dash", "attack1", "attack2", "attack3", "skill1", "skill2", "hurt", "death" };

public static readonly int[] SupportedScales = { 1, 4, 8 };

public string AnimatorStateFor(string actionId, int facing)
{
    if (facing < 0 || facing > 3) throw new ArgumentOutOfRangeException(nameof(facing));
    if (!actionToState.TryGetValue(actionId, out var state)) throw new ArgumentOutOfRangeException(nameof(actionId));
    return state + "_" + directionNames[facing];
}
```

Instantiate the initial selected catalog prefab in `OnEnable`, add a stable `TextMesh` label below every showcase instance, and destroy preview objects in `OnDisable`.

- [ ] **Step 6: Rebuild character controllers, prefabs and the showcase scene.**

Run: `rtk /Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/lhw/code/yuanHaiLu -executeMethod YuanHaiLu.Editor.CharacterAnimationBuilder.RebuildFromCommandLine -logFile /private/tmp/yuanhailu-character-build.log -quit`

Run: `rtk /Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/lhw/code/yuanHaiLu -executeMethod YuanHaiLu.Editor.CharacterShowcaseGenerator.GenerateFromCommandLine -logFile /private/tmp/yuanhailu-showcase-build.log -quit`

Expected: 97 persistent prefabs/controllers, 97 labels, and reachable directional action states.

- [ ] **Step 7: Run focused tests and commit.**

Run: `rtk /Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/lhw/code/yuanHaiLu -runTests -testPlatform EditMode -testFilter YuanHaiLu.Tests.EditMode.CharacterArtTests -testResults /private/tmp/yuanhailu-character-green.xml -logFile /private/tmp/yuanhailu-character-green.log`

Run: `rtk /Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/lhw/code/yuanHaiLu -runTests -testPlatform PlayMode -testFilter YuanHaiLu.Tests.PlayMode.CharacterAnimationPlayModeTests -testResults /private/tmp/yuanhailu-character-play.xml -logFile /private/tmp/yuanhailu-character-play.log`

```bash
rtk git add Assets/Scripts/Character Assets/Scripts/Editor/Art Assets/Tests Assets/Animations \
  Assets/AnimatorControllers Assets/Prefabs Assets/Scenes/CharacterShowcase.unity
rtk git commit -m "feat: make formal character animation reviewable"
```

### Task 4: Make layout files the complete scene structure source

**Files:**
- Modify: `Assets/ArtSource/Environment/Layouts/*.json`
- Modify: `Assets/ArtSource/Environment/Layouts/interiors/*.json`
- Modify: `tools/art_pipeline/map_layout.py`
- Modify: `tools/art_pipeline/tests/test_map_layout.py`
- Modify: `Assets/Scripts/Editor/Art/RegionSceneBuilder.cs`
- Modify: `Assets/Tests/EditMode/EnvironmentArtTests.cs`

**Interfaces:**
- Consumes: `layers` as an array of `{ "name": string, "cells": array of [x, y, token] triples }`, `collisions`, `foregroundSpans`, `anchors`, `requiredLandmarks`.
- Produces: `MapLayout.coordinate_signature() -> tuple`, `RegionSceneBuilder.ApplyDeclaredLayer(Tilemap, IReadOnlyDictionary<string, Tile>, string, LayoutJson, string)`, and persistent structural/collision content that matches JSON exactly.

- [ ] **Step 1: Write coordinate-only uniqueness and declared-layer coverage tests.**

```python
def test_outdoor_coordinate_signatures_are_unique_without_tokens_or_ids():
    signatures = [layout.coordinate_signature() for layout in load_all_outdoor_layouts()]
    assert len(signatures) == len(set(signatures)) == 10
```

```csharp
foreach (var declared in layout.DeclaredCells("Buildings"))
    Assert.That(buildings.GetTile(new Vector3Int(declared.X, declared.Y, 0)), Is.Not.Null);
Assert.That(FindLayoutCollisionCells(scene), Is.EquivalentTo(layout.CollisionCells));
```

- [ ] **Step 2: Run the focused Python test before implementation.**

Run: `rtk python3 -m unittest tools.art_pipeline.tests.test_map_layout -v`

Expected: FAIL because the present signatures are identical after token/id removal and the C# builder does not parse layers.

- [ ] **Step 3: Migrate the 23 layout files to JsonUtility-readable layer arrays.**

Every file declares exactly the seven named layers. Each outdoor has distinct coordinate geometry; each interior declares function-specific prop cells. Required outdoor anchors/landmarks are:

| Scene | Distinct structural feature |
|---|---|
| `tianshu` | diagonal market street, palace gate and side alley |
| `cangyue` | zigzag cliff stair, pine shelf and temple court |
| `yanliu` | branching canal, two bridges and riverside inn |
| `chisha` | diagonal dunes, gate wall, beacon tower and caravan yard |
| `youhuang` | bamboo maze, stream crossing and poison shrine clearing |
| `hanyuan` | snow ridge, ice crack, hot spring and tomb approach |
| `prologue_village` | ring road, grain yard, village entrance and homes |
| `luoyuan` | broken city blocks, water quay and old market |
| `jueyun` | gate stair, cliff walk and sword school terrace |
| `zhenyue` | altar axis, stele grove, mountain path and overlook |

- [ ] **Step 4: Implement strict layout parsing and direct Tilemap application.**

```csharp
[Serializable] internal sealed class LayoutLayerJson
{
    public string name;
    public LayoutCellJson[] cells;
}

private static void ApplyDeclaredLayer(Tilemap map, IReadOnlyDictionary<string, Tile> tiles,
    string sceneId, LayoutJson layout, string layerName)
{
    var declared = layout.layers.Single(layer => layer.name == layerName).cells;
    var positions = new Vector3Int[declared.Length];
    var values = new TileBase[declared.Length];
    for (var i = 0; i < declared.Length; i++)
    {
        positions[i] = new Vector3Int(declared[i].x, declared[i].y, 0);
        values[i] = ResolveDeclaredTile(tiles, sceneId, declared[i].token);
    }
    map.SetTiles(positions, values);
}
```

Delete `PaintRegionStructure`, `PaintHouse`, `PaintInteriorStructure`, fixed water strips, fixed road loops and formulaic decoration loops. `ResolveDeclaredTile` rejects unknown tokens; collision objects are built from declared collision runs only.

- [ ] **Step 5: Run Python and Unity layout tests.**

Run: `rtk python3 -m unittest tools.art_pipeline.tests.test_map_layout -v`

Run: `rtk /Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/lhw/code/yuanHaiLu -runTests -testPlatform EditMode -testFilter YuanHaiLu.Tests.EditMode.EnvironmentArtTests -testResults /private/tmp/yuanhailu-layout-green.xml -logFile /private/tmp/yuanhailu-layout-green.log`

Expected: 23 layouts validate, all ten coordinate signatures differ, and every declared structure/collision cell appears in the saved scene.

- [ ] **Step 6: Commit the scene-data contract.**

```bash
rtk git add Assets/ArtSource/Environment/Layouts tools/art_pipeline/map_layout.py \
  tools/art_pipeline/tests/test_map_layout.py Assets/Scripts/Editor/Art/RegionSceneBuilder.cs \
  Assets/Tests/EditMode/EnvironmentArtTests.cs
rtk git commit -m "feat: build formal scenes from declared layouts"
```

### Task 5: Produce region and interior pixel environment assets

**Files:**
- Create: `Assets/ArtSource/Concepts/environments-a-direction.png`
- Create: `Assets/ArtSource/Environment/Designs/environment-designs.json`
- Create: `tools/art_pipeline/environment_source_builder.py`
- Create: `tools/art_pipeline/tests/test_environment_source_builder.py`
- Modify: `Assets/ArtSource/Environment/Manifests/regions.json`
- Modify: `tools/art_pipeline/environment_baker.py`
- Modify: `Assets/Scripts/Editor/Art/EnvironmentTileBuilder.cs`
- Modify: `Assets/Scripts/Editor/Art/ArtImportRules.cs`
- Modify: `Assets/Scripts/Editor/Art/ArtAssetValidator.cs`
- Modify: `Assets/Art/Environment/**`, `Assets/Tilemaps/Formal/**`, `Assets/Scenes/Regions/**`, `Assets/Scenes/Interiors/**`

**Interfaces:**
- Consumes: `EnvironmentDesign(id, palette, tile_roles, landmark_shapes, weather_id, state_variants)`.
- Produces: persistent 16px tile modules, landmark sheets, metadata with bottom pivots/collision rectangles, and Tile assets that set `ColliderType.Grid` only for walk-blocking structure roles.

- [ ] **Step 1: Generate and save the approved A-direction environment concept sheet.**

Use ImageGen with this prompt: `top-down 2D pixel art environment concept sheet for a Chinese wuxia RPG, 16x16 tile language, ten distinct zones: imperial market city, misty mountain temple, water town with stone bridge and boats, desert frontier beacon, bamboo poison valley, snow mountain tomb, burned village, ancient river ruins, sword sect cliff, mountain altar; low-saturation ink outlines, warm lantern focal points, readable paths, no 3D, no text`.

Save the selected result as `Assets/ArtSource/Concepts/environments-a-direction.png` with `.meta`; it is visual reference only.

- [ ] **Step 2: Write failing environment design/source tests.**

```python
def test_every_environment_has_unique_palette_landmark_and_required_tile_roles():
    designs = load_environment_designs(DESIGN_PATH)
    assert set(designs) == set(REGION_IDS) | set(INTERIOR_IDS)
    assert len({design.geometry_key for design in designs.values() if design.kind == "region"}) == 10
    for design in designs.values():
        assert {"ground", "road", "wall", "roof", "decor"} <= set(design.tile_roles)
        assert len(design.landmarks) >= 3
```

- [ ] **Step 3: Create 23 environment design records.**

Each record declares a regional palette, distinct role modules, three named landmarks, weather and functional props. `inn`, `pharmacy`, `academy`, `yamen`, `palace`, `temple`, `cave`, `tomb`, `dungeon`, `military_camp`, `ship_cabin`, `shop` and `residence` must each name the exact prop silhouettes that distinguish their room function.

- [ ] **Step 4: Implement the environment source builder.**

```python
def build_environment_sources(design, destination):
    for role, variant in design.tile_roles:
        image = draw_tile(role, variant, design.palette, design.geometry_key)
        write_png(destination / f"{role}_{variant:02d}.png", image)
    for landmark in design.landmarks:
        write_png(destination / f"{landmark.id}.png", draw_landmark(landmark, design.palette))
```

`draw_tile` must produce non-empty 16×16 RGBA tiles with cluster-level texture; `draw_landmark` must produce a bottom-pivoted, non-empty silhouette whose declared collision rectangle remains within the image bounds.

- [ ] **Step 5: Make tile collision roles explicit.**

```csharp
tile.colliderType = metadata.BlockingTileRoles.Contains(spriteRole)
    ? Tile.ColliderType.Grid
    : Tile.ColliderType.None;
```

`EnvironmentTileBuilder` obtains `spriteRole` from the metadata name and never marks ground, water, road, decor or effects as blocking. `RegionSceneBuilder` retains layout collision runs for non-tile obstacles.

Add `string[] blockingTileRoles` to both environment `.art.json` output and `ArtMetadata`; `ArtImportRules.ReadMetadataAtPath` must reject a blocking role that has no corresponding sprite role, and `ArtAssetValidator` must report an environment metadata file that omits this field.

- [ ] **Step 6: Build assets and scenes twice for determinism.**

Run: `rtk python3 -m tools.art_pipeline.environment_source_builder --all`

Run: `rtk python3 -m tools.art_pipeline.build --all`

Run: `rtk python3 -m tools.art_pipeline.validate --all`

Run: `rtk /Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/lhw/code/yuanHaiLu -executeMethod YuanHaiLu.Editor.RegionSceneBuilder.BuildAll -logFile /private/tmp/yuanhailu-scene-build-1.log -quit`

Run the same scene build command a second time, then run `rtk git status --short`.

Expected: 23 formal environments, all landmark/Tile imports valid, and the second build introduces no unexpected source or scene differences.

- [ ] **Step 7: Commit formal environment assets and rebuilt scenes.**

```bash
rtk git add Assets/ArtSource/Concepts Assets/ArtSource/Environment tools/art_pipeline \
  Assets/Art/Environment Assets/Tilemaps/Formal Assets/Scenes/Regions Assets/Scenes/Interiors \
  Assets/Scripts/Editor/Art/EnvironmentTileBuilder.cs
rtk git commit -m "feat: build distinct formal pixel environments"
```

### Task 6: Add the prologue normal/burned environment state

**Files:**
- Modify: `Assets/ArtSource/Environment/Designs/environment-designs.json`
- Modify: `Assets/ArtSource/Environment/Manifests/regions.json`
- Modify: `tools/art_pipeline/schema.py`
- Modify: `tools/art_pipeline/environment_baker.py`
- Create: `Assets/Scripts/Art/RegionEnvironmentController.cs` and `.meta`
- Modify: `Assets/Scripts/Art/RegionSceneDefinition.cs`
- Modify: `Assets/Scripts/Editor/Art/RegionSceneBuilder.cs`
- Modify: `Assets/Tests/EditMode/EnvironmentArtTests.cs`
- Modify: `Assets/Tests/PlayMode/RuntimePresentationPlayModeTests.cs`

**Interfaces:**
- Consumes: `stateVariants.normal`, `stateVariants.burned`, each with environment asset references and weather ID.
- Produces: `public string CurrentEnvironmentState { get; }` and `public void SetEnvironmentState(string stateId)` accepting only `normal` or `burned`.

- [ ] **Step 1: Write the state-preservation tests.**

```csharp
[Test]
public void BurnedProloguePreservesAnchorsAndCollisionButChangesArt()
{
    var beforeAnchors = definition.Anchors.Select(anchor => (anchor.Id, anchor.Cell)).ToArray();
    var beforeCollision = LayoutCollisionCells(root).ToArray();
    controller.SetEnvironmentState("burned");
    Assert.That(controller.CurrentEnvironmentState, Is.EqualTo("burned"));
    CollectionAssert.AreEqual(beforeAnchors, definition.Anchors.Select(anchor => (anchor.Id, anchor.Cell)).ToArray());
    CollectionAssert.AreEqual(beforeCollision, LayoutCollisionCells(root).ToArray());
    Assert.That(CurrentEnvironmentSpriteIds(root), Is.Not.EqualTo(normalSpriteIds));
}
```

- [ ] **Step 2: Run the focused test before implementation.**

Run: `rtk /Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/lhw/code/yuanHaiLu -runTests -testPlatform EditMode -testFilter YuanHaiLu.Tests.EditMode.EnvironmentArtTests -testResults /private/tmp/yuanhailu-prologue-red.xml -logFile /private/tmp/yuanhailu-prologue-red.log`

Expected: FAIL because no state API or burned assets exist.

- [ ] **Step 3: Extend the manifest and baker schema.**

```json
"stateVariants": {
  "normal": { "tilesetSuffix": "normal", "weather": "clear" },
  "burned": { "tilesetSuffix": "burned", "weather": "ember_wind" }
}
```

Require both keys for `prologue_village`, reject them for all other regions in the current scope, and bake a separate burned Tile/landmark sheet with charred walls, broken roofs, ash, damaged props and ember particles.

- [ ] **Step 4: Implement strict runtime swapping.**

```csharp
public void SetEnvironmentState(string stateId)
{
    if (stateId != "normal" && stateId != "burned")
        throw new ArgumentException("Expected normal or burned.", nameof(stateId));
    ApplyVariant(variants[stateId]);
    CurrentEnvironmentState = stateId;
}
```

`ApplyVariant` replaces only Tilemap/SpriteRenderer assets and weather tint/velocity; it must never recreate `RegionSceneDefinition`, anchors, `LayoutCollision` or travel trigger objects.

- [ ] **Step 5: Rebuild, run state tests and commit.**

Run: `rtk python3 -m tools.art_pipeline.build --all`

Run: `rtk python3 -m tools.art_pipeline.validate --all`

Run: `rtk /Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/lhw/code/yuanHaiLu -runTests -testPlatform PlayMode -testFilter YuanHaiLu.Tests.PlayMode.RuntimePresentationPlayModeTests -testResults /private/tmp/yuanhailu-prologue-play.xml -logFile /private/tmp/yuanhailu-prologue-play.log`

```bash
rtk git add Assets/ArtSource/Environment Assets/Art/Environment Assets/Scripts/Art \
  Assets/Scripts/Editor/Art/RegionSceneBuilder.cs tools/art_pipeline Assets/Tests
rtk git commit -m "feat: add burned prologue environment state"
```

### Task 7: Capture visual evidence and publish truthful project memory

**Files:**
- Create: `Assets/Scripts/Editor/Art/VisualRegressionCapture.cs` and `.meta`
- Create: `Assets/Tests/EditMode/VisualRegressionTests.cs` and `.meta`
- Modify after approval only: `Assets/Tests/VisualBaselines/MainMenu.png` and ten outdoor PNGs
- Modify: `docs/01-art-style-guide.md`
- Modify: `docs/03-art-production-handoff.md`
- Modify: `AGENTS.md`, `README.md`, `SETUP_GUIDE.md`

**Interfaces:**
- Consumes: `CaptureScene(string sceneId, string outputPath)`, `CaptureMainMenu(string outputPath)`, `ChangedPixelRatio(string expectedPath, string actualPath)`.
- Produces: fixed `480×270` PNGs, restored editor state, `CaptureTemporaryReviewFromCommandLine()`, approved baseline records and actual test counts/evidence paths.

- [ ] **Step 1: Write the capture state restoration tests.**

```csharp
[Test]
public void CaptureRestoresPreviousOpenSceneAndRenderState()
{
    var before = CaptureEditorState.Read();
    VisualRegressionCapture.CaptureScene("yanliu", Path.Combine(Path.GetTempPath(), "yanliu.png"));
    Assert.That(CaptureEditorState.Read(), Is.EqualTo(before));
}
```

The state equality includes the set of open scene paths, active scene path, Canvas fields, camera target texture, `RenderTexture.active` and `QualitySettings.antiAliasing`.

- [ ] **Step 2: Run the focused test before implementation.**

Run: `rtk /Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/lhw/code/yuanHaiLu -runTests -testPlatform EditMode -testFilter YuanHaiLu.Tests.EditMode.VisualRegressionTests -testResults /private/tmp/yuanhailu-visual-red.xml -logFile /private/tmp/yuanhailu-visual-red.log`

Expected: FAIL because fixed 480×270 regression capture and complete editor-state restoration do not exist.

- [ ] **Step 3: Implement capture with a single restoration boundary.**

```csharp
var before = CaptureEditorState.Read();
try
{
    OpenTargetSceneAdditively(scenePath);
    WriteCameraPng(camera, outputPath, width: 480, height: 270);
}
finally
{
    before.Restore();
}
```

`CaptureEditorState.Restore()` closes capture-loaded scenes, reloads any scene that was closed, restores active scene and restores every render/UI field recorded by `Read()` even if PNG writing throws.

- [ ] **Step 4: Capture temporary review images before changing baselines.**

Run: `rtk mkdir -p /private/tmp/yuanhailu-art-review`

Implement `CaptureTemporaryReviewFromCommandLine()` to call `CaptureMainMenu` and `CaptureScene` for `prologue_village`, `luoyuan`, `tianshu`, `yanliu`, `cangyue`, `jueyun`, `chisha`, `youhuang`, `hanyuan` and `zhenyue`, writing only beneath `/private/tmp/yuanhailu-art-review`. Run it with `-executeMethod YuanHaiLu.Editor.VisualRegressionCapture.CaptureTemporaryReviewFromCommandLine`.

At 1× and 4× inspect: readable player silhouette, non-identical routes/buildings, valid foreground order, weather matching region, no missing sprites or sprite-sheet corruption, and visibly different normal/burned prologue images.

- [ ] **Step 5: Approve or reject captures explicitly.**

Only if every image passes human review, copy the reviewed images to `Assets/Tests/VisualBaselines/` and record the date, Unity version, reviewer, paths and checklist in `docs/03-art-production-handoff.md`. If one image fails, do not update any baseline and return to its producing task.

- [ ] **Step 6: Align long-term documentation with reality.**

Update `docs/01-art-style-guide.md` to state pure 2D, 32×32 formal frames and the confirmed A direction; update `AGENTS.md`, `README.md` and `SETUP_GUIDE.md` only with actual counts from current test XML. Preserve `docs/04-external-ai-development-handoff.md` and `docs/05-post-development-review-plan.md` as historical/review records.

- [ ] **Step 7: Run full verification and commit evidence.**

Run: `rtk python3 -m unittest discover -s tools/art_pipeline/tests -v`

Run: `rtk python3 -m tools.art_pipeline.build --all`

Run: `rtk python3 -m tools.art_pipeline.validate --all`

Run EditMode and PlayMode full suites with separate XML/log files under `/private/tmp/yuanhailu-final-*`.

Run: `rtk git diff --check main...HEAD`

```bash
rtk git add Assets/Scripts/Editor/Art/VisualRegressionCapture.cs Assets/Tests/VisualBaselines \
  Assets/Tests/EditMode/VisualRegressionTests.cs docs AGENTS.md README.md SETUP_GUIDE.md
rtk git commit -m "test: approve formal art visual evidence"
```

### Task 8: Independent pre-merge evidence package

**Files:**
- Modify: `docs/03-art-production-handoff.md`
- Modify: `AGENTS.md`

**Interfaces:**
- Consumes: current branch HEAD, `main` merge-base, Python output, Unity EditMode XML, Unity PlayMode XML, visual screenshot paths and visible-window QA results.
- Produces: a review-ready report with no unverified completion claims.

- [ ] **Step 1: Record the fixed review range and clean status.**

```bash
rtk git rev-parse main
rtk git rev-parse HEAD
rtk git merge-base main HEAD
rtk git status --short --branch
rtk git diff --stat main...HEAD
```

- [ ] **Step 2: Re-run every independent gate from a clean worktree.**

```bash
rtk python3 -m unittest discover -s tools/art_pipeline/tests -v
rtk python3 -m tools.art_pipeline.build --all
rtk python3 -m tools.art_pipeline.validate --all
rtk /Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode \
  -projectPath /Users/lhw/code/yuanHaiLu -runTests -testPlatform EditMode \
  -testResults /private/tmp/yuanhailu-final-editmode.xml \
  -logFile /private/tmp/yuanhailu-final-editmode.log
rtk /Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode \
  -projectPath /Users/lhw/code/yuanHaiLu -runTests -testPlatform PlayMode \
  -testResults /private/tmp/yuanhailu-final-playmode.xml \
  -logFile /private/tmp/yuanhailu-final-playmode.log
```

- [ ] **Step 3: Perform visible Unity QA.**

From `MainMenu.unity`, verify selection confirm/cancel, menu→Demo, movement, J three-hit combo, dash, NPC interaction, pause, smoke test through an interior and back, retained player state/input, direct-open formal scene, and prologue normal/burned switching. Record pass/fail and screenshot paths; a licensing or GUI failure is recorded as `not completed`, never `passed`.

- [ ] **Step 4: Write the evidence record.**

The handoff contains branch/HEAD/baseline, exact test totals from XML, all result/log paths, the eleven visual image paths, visible QA checklist, unresolved P0/P1 list and `git diff --check` result. Do not state that the project is complete if any evidence is absent.

- [ ] **Step 5: Commit the evidence record and stop for independent review.**

```bash
rtk git add docs/03-art-production-handoff.md AGENTS.md
rtk git commit -m "docs: record formal art rebuild verification"
```

Do not merge into `main` in this task. Submit the branch and evidence to the independent review procedure in `docs/05-post-development-review-plan.md`.

## Plan Self-Review

- **Spec coverage:** Tasks 1–2 make all character sources rebuildable and visually distinct; Task 3 makes their runtime actions and review surface reachable; Tasks 4–5 remove formulaic layouts and produce all 23 environment assets; Task 6 adds the prologue burn state; Task 7 adds fresh visual review; Task 8 performs independent evidence collection and preserves the merge gate.
- **Placeholder scan:** The plan contains no deferred implementation markers. All declared file paths, public methods, action identifiers, layer names and verification commands are stated explicitly.
- **Type consistency:** Character source audit uses `CharacterRecipe` module paths throughout; layout parsing uses the declared `LayoutLayerJson` array in Python and C#; prologue state is consistently `normal`/`burned`; showcase public IDs map to underscore Animator state names.
