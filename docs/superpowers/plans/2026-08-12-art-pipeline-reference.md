# Art Pipeline and Reference Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the deterministic modular art pipeline, Unity catalogs, validation tools, and one approved 32×32 character plus Yanliu Town reference slice that all later art batches reuse.

**Architecture:** Transparent PNG modules and JSON manifests are canonical inputs. A Pillow-based Python baker composes deterministic sprite sheets and tilesets; Unity editor tools import, slice, validate, catalog, and preview those baked outputs. Missing or invalid formal assets fail in the editor and never fall back to runtime color blocks.

**Tech Stack:** Python 3 + Pillow + `unittest`; Unity 6000.4.10f1; C#; Unity Editor APIs; NUnit EditMode tests; built-in 2D renderer.

## Global Constraints

- Rendering is complete top-down 2D pixel art; do not add URP, 3D models, or HD-2D dependencies.
- Native resolution is exactly `480×270`; tile size is `16×16`; character frames are exactly `32×32`; PPU is exactly `16`.
- All sprite textures use Point filtering, no mipmaps, and uncompressed texture import.
- Runtime composition and runtime placeholder `Texture2D` creation are forbidden for formal characters and environments.
- Source modules use transparent PNG plus versioned JSON; baked Unity assets are deterministic and committed.
- Run all repository shell commands through `rtk`.
- Final commits land on `main`.

---

## File Structure

### Python production pipeline

- Create `tools/art_pipeline/__init__.py`: package marker and schema version export.
- Create `tools/art_pipeline/palette.py`: shared named RGBA palettes and palette validation.
- Create `tools/art_pipeline/schema.py`: manifest dataclasses and strict JSON loading.
- Create `tools/art_pipeline/canvas.py`: pixel-safe load, crop, transform, and composite helpers.
- Create `tools/art_pipeline/character_baker.py`: compose 32×32 character sheets from module recipes.
- Create `tools/art_pipeline/environment_baker.py`: compose 16×16 tilesets and large environment sprites.
- Create `tools/art_pipeline/validate.py`: CLI validation and non-zero failure exit.
- Create `tools/art_pipeline/build.py`: deterministic build entry point.
- Create `tools/art_pipeline/tests/test_schema.py`: schema and duplicate-ID tests.
- Create `tools/art_pipeline/tests/test_character_baker.py`: frame, alpha, anchor, and deterministic-output tests.
- Create `tools/art_pipeline/tests/test_environment_baker.py`: tile dimensions, palette, and deterministic-output tests.

### Source and baked assets

- Create `Assets/ArtSource/palettes/yuanhai-v1.json`: canonical global and regional palettes.
- Create `Assets/ArtSource/Characters/Manifests/reference-characters.json`: male/female swordsman reference recipes.
- Create `Assets/ArtSource/Characters/{Bodies,Faces,Hair,Outfits,Weapons,Accessories}/reference/*.png`: visible editable reference modules.
- Create `Assets/ArtSource/Environment/Manifests/yanliu-reference.json`: Yanliu tile and landmark recipes.
- Create `Assets/ArtSource/Environment/{Shared,Regions/yanliu}/**/*.png`: visible editable reference tiles, props, and landmarks.
- Create `Assets/Art/Characters/Player/`: baked reference sheets and previews.
- Create `Assets/Art/Environment/Regions/yanliu/`: baked tileset, landmarks, and previews.

### Unity runtime and editor integration

- Create `Assets/Scripts/Art/ArtAssetId.cs`: stable ID validation and category constants.
- Create `Assets/Scripts/Art/CharacterArtCatalog.cs`: character entry ScriptableObject model and lookup.
- Create `Assets/Scripts/Art/EnvironmentArtCatalog.cs`: region entry ScriptableObject model and lookup.
- Create `Assets/Scripts/Editor/Art/ArtImportRules.cs`: path-aware 32×32 character and 16×16 tile import rules.
- Create `Assets/Scripts/Editor/Art/ArtAssetValidator.cs`: fail-fast catalog, texture, sprite, and animation validation.
- Create `Assets/Scripts/Editor/Art/ArtCatalogBuilder.cs`: create/update catalog assets from baked manifests.
- Create `Assets/Scripts/Editor/Art/ArtReferencePreviewGenerator.cs`: generate the isolated art reference scene.
- Modify `Assets/Scripts/Editor/PixelArtImporter.cs`: route existing menus through `ArtImportRules` and change character slicing from 48×48 to 32×32.
- Modify `Assets/Scripts/Core/GameConfig.cs`: document 32×32 character height and add `CHARACTER_FRAME_SIZE = 32`.
- Create `Assets/Tests/EditMode/ArtPipelineTests.cs`: catalog, import, validation, and preview scene tests.
- Modify `Assets/Tests/EditMode/YuanHaiLu.EditModeTests.asmdef`: reference `YuanHaiLu.Editor` so editor art tools are testable.

---

### Task 1: Lock the Manifest Schema and Palette Contract

**Files:**
- Create: `tools/art_pipeline/__init__.py`
- Create: `tools/art_pipeline/palette.py`
- Create: `tools/art_pipeline/schema.py`
- Create: `tools/art_pipeline/tests/__init__.py`
- Create: `tools/art_pipeline/tests/test_schema.py`
- Create: `Assets/ArtSource/palettes/yuanhai-v1.json`

**Interfaces:**
- Produces: `load_character_manifest(path: Path) -> CharacterManifest`, `load_environment_manifest(path: Path) -> EnvironmentManifest`, `validate_palette(colors: dict[str, RGBA]) -> None`.
- Produces IDs matching `^[a-z][a-z0-9_]{2,63}$`; duplicate IDs raise `ManifestError`.

- [ ] **Step 1: Write failing schema tests**

```python
class SchemaTests(unittest.TestCase):
    def test_duplicate_character_ids_are_rejected(self):
        payload = {"schemaVersion": 1, "characters": [
            {"id": "player_male_swordsman", "frameSize": 32},
            {"id": "player_male_swordsman", "frameSize": 32},
        ]}
        with self.assertRaisesRegex(ManifestError, "duplicate character id"):
            CharacterManifest.from_dict(payload)

    def test_character_frame_size_must_be_32(self):
        with self.assertRaisesRegex(ManifestError, "frameSize must be 32"):
            CharacterRecipe.from_dict({"id": "bad_actor", "frameSize": 48})
```

- [ ] **Step 2: Run the tests and verify failure**

Run: `rtk python3 -m unittest tools.art_pipeline.tests.test_schema -v`  
Expected: FAIL because `tools.art_pipeline.schema` does not exist.

- [ ] **Step 3: Implement strict schema types**

Use frozen dataclasses and reject unknown schema versions, malformed IDs, missing module names, duplicate animation names, non-32 character frames, and non-16 tile sizes. Define the canonical animation row object exactly as:

```python
@dataclass(frozen=True)
class AnimationRow:
    name: str
    direction: str
    frames: int
    fps: int
    loop: bool
    hit_frames: tuple[int, ...] = ()
```

- [ ] **Step 4: Add the canonical palette**

`yuanhai-v1.json` must define named groups `ink`, `paper`, `cinnabar`, `jade`, `earth`, `gold`, `mystic`, plus region groups for all ten regions. Each color is an explicit four-integer RGBA array; validation rejects channels outside 0–255 and groups with fewer than four colors.

- [ ] **Step 5: Run schema tests**

Run: `rtk python3 -m unittest tools.art_pipeline.tests.test_schema -v`  
Expected: all schema tests PASS.

- [ ] **Step 6: Commit the schema contract**

```bash
rtk git add tools/art_pipeline Assets/ArtSource/palettes/yuanhai-v1.json
rtk git commit -m "feat: define deterministic art manifests"
```

### Task 2: Implement Deterministic Pixel Baking

**Files:**
- Create: `tools/art_pipeline/canvas.py`
- Create: `tools/art_pipeline/character_baker.py`
- Create: `tools/art_pipeline/environment_baker.py`
- Create: `tools/art_pipeline/build.py`
- Create: `tools/art_pipeline/validate.py`
- Create: `tools/art_pipeline/tests/test_character_baker.py`
- Create: `tools/art_pipeline/tests/test_environment_baker.py`

**Interfaces:**
- Consumes: schema types and palette groups from Task 1.
- Produces: `bake_character(recipe, output_dir) -> BakedCharacter`, `bake_environment(recipe, output_dir) -> BakedEnvironment`, `python3 -m tools.art_pipeline.build --all`.
- Baked outputs include `.png` and adjacent `.art.json` metadata with SHA-256, frame rectangles, pivots, FPS, loop flags, and hit frames.

- [ ] **Step 1: Write failing deterministic-output tests**

```python
def test_character_bake_is_deterministic(self):
    first = bake_character(self.recipe, self.first_dir)
    second = bake_character(self.recipe, self.second_dir)
    self.assertEqual(first.sha256, second.sha256)
    self.assertEqual(first.image.size[0] % 32, 0)
    self.assertEqual(first.image.size[1] % 32, 0)

def test_environment_tiles_are_16_pixels(self):
    baked = bake_environment(self.recipe, self.output_dir)
    self.assertEqual(baked.tile_size, 16)
    self.assertEqual(baked.image.size[0] % 16, 0)
    self.assertEqual(baked.image.size[1] % 16, 0)
```

- [ ] **Step 2: Run baker tests and verify failure**

Run: `rtk python3 -m unittest tools.art_pipeline.tests.test_character_baker tools.art_pipeline.tests.test_environment_baker -v`  
Expected: FAIL because baker modules do not exist.

- [ ] **Step 3: Implement pixel-safe canvas operations**

`PixelCanvas` must expose `load_module`, `crop`, `paste`, `mirror_x`, `recolor_by_palette_role`, and `assert_inside`. All coordinates are integers; alpha compositing uses Pillow `Image.alpha_composite`; out-of-bounds composition raises `PixelBoundsError` instead of clipping silently. The baker may recolor declared palette-role pixels and transform modules, but must not synthesize formal anatomy, clothing, buildings, foliage, or props from geometric primitives.

- [ ] **Step 4: Implement character sheet layout**

Use a fixed maximum row width of six 32×32 frames. Rows are packed in manifest order. Metadata names every sprite `<character-id>__<animation>__<direction>__<frame-index>` and uses bottom-center pivot `(0.5, 0.0)`.

- [ ] **Step 5: Implement environment baking and metadata**

Tiles are named `<region-id>__<tile-role>__<variant-index>` with center pivot `(0.5, 0.5)`. Large landmarks use bottom-center pivots and store their pixel bounds separately from collision bounds.

- [ ] **Step 6: Implement build and validation CLIs**

`build.py --all` loads every manifest under `Assets/ArtSource/**/Manifests`, writes only changed outputs, and prints built/skipped counts. `validate.py --all` recomputes hashes, validates dimensions and alpha, and exits 1 on the first invalid asset set.

- [ ] **Step 7: Run the Python suite**

Run: `rtk python3 -m unittest discover -s tools/art_pipeline/tests -v`  
Expected: all tests PASS.

- [ ] **Step 8: Commit the baker**

```bash
rtk git add tools/art_pipeline
rtk git commit -m "feat: add deterministic pixel art baker"
```

### Task 3: Produce the Reference Character and Yanliu Art Slice

**Files:**
- Create: `Assets/ArtSource/Characters/Manifests/reference-characters.json`
- Create: `Assets/ArtSource/Characters/Bodies/reference/*.png`
- Create: `Assets/ArtSource/Characters/Faces/reference/*.png`
- Create: `Assets/ArtSource/Characters/Hair/reference/*.png`
- Create: `Assets/ArtSource/Characters/Outfits/reference/*.png`
- Create: `Assets/ArtSource/Characters/Weapons/reference/*.png`
- Create: `Assets/ArtSource/Characters/Accessories/reference/*.png`
- Create: `Assets/ArtSource/Environment/Manifests/yanliu-reference.json`
- Create: `Assets/ArtSource/Environment/Shared/reference/*.png`
- Create: `Assets/ArtSource/Environment/Regions/yanliu/reference/*.png`
- Create: `Assets/Art/Characters/Player/player_male_swordsman.png`
- Create: `Assets/Art/Characters/Player/player_female_swordsman.png`
- Create: `Assets/Art/Environment/Regions/yanliu/yanliu_tileset.png`
- Create: `Assets/Art/Environment/Regions/yanliu/yanliu_landmarks.png`
- Create: `Assets/Art/Environment/Regions/yanliu/yanliu_reference.png`

**Interfaces:**
- Consumes: Task 2 baker CLI.
- Produces: approved visual grammar used by all character and environment recipes in later plans.

- [ ] **Step 1: Add manifest tests for the reference slice**

Assert exact IDs `player_male_swordsman`, `player_female_swordsman`, and `yanliu`; assert the swordsman reference includes `idle`, `walk`, `dash`, `attack_1`, `attack_2`, `attack_3`, `skill_1`, `skill_2`, `hurt`, `dodge`, `down`, and `death` rows for required directions.

- [ ] **Step 2: Run the manifest tests and verify failure**

Run: `rtk python3 -m unittest tools.art_pipeline.tests.test_schema -v`  
Expected: FAIL because reference manifests do not exist.

- [ ] **Step 3: Author the male and female swordsman recipes**

Draw and commit transparent 32×32 body, face, hair, outfit, sword, and accessory PNG modules for every referenced direction/pose. Use distinct silhouettes and hairstyles with shared feet, hands, and weapon anchors. Recipes name those PNG modules and palette-role substitutions; they contain no embedded shape drawing commands. Use `ink`, `paper`, and blue-jade groups; reserve cinnabar for the belt and hit accents. The six-frame walk cycle must include two contact, two passing, and two recoil poses rather than duplicated idle frames.

- [ ] **Step 4: Author the Yanliu reference recipe**

Draw and commit transparent 16×16 tile modules and bottom-pivoted larger landmark modules. Include grass, dirt, stone path, shallow/deep water, all four shore edges, four shore corners, white wall, stone base, wood wall, window, door, roof body/eave/ridge/corners, willow, lotus, lantern, flag, barrel, crate, bridge, dock, boat, well, sign, and three landmarks: inn, pharmacy, and arched bridge. The environment recipe composes these PNG modules but does not replace them with generated rectangles.

- [ ] **Step 5: Bake and validate the reference slice**

Run: `rtk python3 -m tools.art_pipeline.build --manifest Assets/ArtSource/Characters/Manifests/reference-characters.json`  
Run: `rtk python3 -m tools.art_pipeline.build --manifest Assets/ArtSource/Environment/Manifests/yanliu-reference.json`  
Run: `rtk python3 -m tools.art_pipeline.validate --all`  
Expected: two character sheets and the Yanliu tileset/landmarks/reference image are built; validation exits 0.

- [ ] **Step 6: Inspect the baked reference images**

Open `player_male_swordsman.png`, `player_female_swordsman.png`, and `yanliu_reference.png` at nearest-neighbor 6× zoom. Reject isolated single-pixel noise, flat color-block anatomy, indistinguishable gender silhouettes, broken roof seams, and water/shore gaps.

- [ ] **Step 7: Commit the approved reference slice**

```bash
rtk git add Assets/ArtSource Assets/Art/Characters/Player Assets/Art/Environment/Regions/yanliu
rtk git commit -m "feat: add reference character and Yanliu art"
```

### Task 4: Add Unity Catalogs and Stable Art IDs

**Files:**
- Create: `Assets/Scripts/Art/ArtAssetId.cs`
- Create: `Assets/Scripts/Art/CharacterArtCatalog.cs`
- Create: `Assets/Scripts/Art/EnvironmentArtCatalog.cs`
- Create: `Assets/Tests/EditMode/ArtPipelineTests.cs`

**Interfaces:**
- Produces: `CharacterArtCatalog.LoadDefault()`, `bool CharacterArtCatalog.TryGet(string id, out CharacterArtEntry entry)`, `EnvironmentArtCatalog.LoadDefault()`, and `bool EnvironmentArtCatalog.TryGet(string id, out EnvironmentArtEntry entry)`.
- `CharacterArtEntry` stores ID, category, sheet, controller, prefab, and preview; `EnvironmentArtEntry` stores region ID, tileset, landmarks, preview, and scene configuration ID.
- Test-only editor interfaces are `CharacterArtCatalog.SetEntriesForEditor(IEnumerable<CharacterArtEntry> entries)` and `CharacterArtEntry.ForTest(string id)`.

- [ ] **Step 1: Write failing catalog tests**

```csharp
[Test]
public void CharacterCatalogRejectsDuplicateStableIds()
{
    var catalog = ScriptableObject.CreateInstance<CharacterArtCatalog>();
    catalog.SetEntriesForEditor(new[] {
        CharacterArtEntry.ForTest("player_male_swordsman"),
        CharacterArtEntry.ForTest("player_male_swordsman")
    });
    Assert.That(() => catalog.RebuildLookup(), Throws.InvalidOperationException);
}
```

- [ ] **Step 2: Run EditMode tests and verify failure**

Run the Unity EditMode command from `README.md` with `-testFilter YuanHaiLu.Tests.EditMode.ArtPipelineTests`.  
Expected: compile/test failure because catalog types do not exist.

- [ ] **Step 3: Implement stable ID validation and catalog lookups**

Use serialized lists for Unity inspection and non-serialized dictionaries for lookup. `OnEnable` rebuilds lookups. Empty IDs, malformed IDs, duplicates, or null formal textures throw `InvalidOperationException` with the offending ID.

- [ ] **Step 4: Run the focused EditMode tests**

Expected: catalog tests PASS.

- [ ] **Step 5: Commit runtime catalog contracts**

```bash
rtk git add Assets/Scripts/Art Assets/Tests/EditMode/ArtPipelineTests.cs
rtk git commit -m "feat: add stable art catalogs"
```

### Task 5: Import, Slice, Validate, and Preview Formal Assets in Unity

**Files:**
- Create: `Assets/Scripts/Editor/Art/ArtImportRules.cs`
- Create: `Assets/Scripts/Editor/Art/ArtAssetValidator.cs`
- Create: `Assets/Scripts/Editor/Art/ArtCatalogBuilder.cs`
- Create: `Assets/Scripts/Editor/Art/ArtReferencePreviewGenerator.cs`
- Modify: `Assets/Scripts/Editor/PixelArtImporter.cs`
- Modify: `Assets/Scripts/Core/GameConfig.cs`
- Modify: `Assets/Tests/EditMode/ArtPipelineTests.cs`
- Modify: `Assets/Tests/EditMode/YuanHaiLu.EditModeTests.asmdef`
- Create: `Assets/Scenes/ArtReference.unity` through the editor generator.

**Interfaces:**
- Consumes: baked `.art.json` metadata and runtime catalog contracts.
- Produces: `ArtImportRules.Apply(string assetPath)`, `ArtValidationReport ValidateAll()`, `ArtCatalogBuilder.RebuildAll()`, and menu `Tools/渊海录/美术/生成参考预览场景`.

- [ ] **Step 1: Write failing importer and validator tests**

```csharp
[Test]
public void FormalCharacterTextureUsesExactPixelImportSettings()
{
    ArtImportRules.Apply(TestCharacterPath);
    var importer = (TextureImporter)AssetImporter.GetAtPath(TestCharacterPath);
    Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(16f));
    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
    Assert.That(importer.mipmapEnabled, Is.False);
    Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
}
```

Add `"YuanHaiLu.Editor"` to `YuanHaiLu.EditModeTests.asmdef` references in the same failing-test change so the test assembly can compile against editor art APIs once they exist.

- [ ] **Step 2: Run focused EditMode tests and verify failure**

Expected: compile/test failure because editor art tools do not exist.

- [ ] **Step 3: Implement path-aware import rules**

Character sheets under `Assets/Art/Characters` use Multiple mode, 32×32 rectangles from metadata, and bottom-center pivots. Region tilesets use Multiple mode, 16×16 rectangles, and center pivots. Landmark sheets use metadata rectangles and bottom-center pivots.

- [ ] **Step 4: Replace the 48×48 slicing menu**

Rename it to `Tools/渊海录/切分角色精灵表 (32x32)` and route slicing through metadata when present. Add `GameConfig.CHARACTER_FRAME_SIZE = 32` and update the `CHARACTER_PPU` comment.

- [ ] **Step 5: Implement validator and catalog builder**

`ValidateAll()` reports missing files, hash mismatch, invalid imports, missing sprite names, duplicate IDs, null references, and missing preview images. `RebuildAll()` refuses to write catalog assets if validation has errors.

- [ ] **Step 6: Generate the reference preview scene**

The scene shows male and female swordsman animation rows beside the Yanliu inn, pharmacy, bridge, willow, water, and shore transitions. It contains no gameplay managers and no runtime-created textures.

- [ ] **Step 7: Run Python and Unity validation**

Run: `rtk python3 -m unittest discover -s tools/art_pipeline/tests -v`  
Run: `rtk python3 -m tools.art_pipeline.validate --all`  
Run the full EditMode suite from `README.md`.  
Expected: all Python and Unity tests PASS; validator reports zero errors.

- [ ] **Step 8: Commit the Unity art foundation**

```bash
rtk git add Assets/Scripts/Art Assets/Scripts/Editor Assets/Scripts/Core/GameConfig.cs Assets/Tests/EditMode Assets/Scenes/ArtReference.unity Assets/Art
rtk git commit -m "feat: integrate validated pixel art pipeline"
```

## Plan Completion Gate

Do not start the full character batch until all of the following are true:

- Reference sheets are visibly 32×32 pixel art rather than colored primitives.
- Yanliu reference image contains readable white-wall/gray-roof architecture, bridge, water, willow, and lotus motifs.
- Python build is deterministic and validation exits 0.
- Unity import and catalog tests pass.
- `Assets/Scenes/ArtReference.unity` opens with zero Console errors and contains no generated placeholder textures.
