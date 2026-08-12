# Full Environment Art Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce ten distinct outdoor region kits, thirteen reusable interior/underground kits, weather/day-night variants, landmark sprites, and walkable generated showcase maps with no placeholder geometry.

**Architecture:** Region and interior JSON recipes declare palettes, tile roles, landmarks, map layouts, collision roles, entrances, exits, foreground spans, and weather layers. Python bakes deterministic tilesets and preview images; Unity imports them into catalogs and builds layered Tilemap scenes from explicit layout data.

**Tech Stack:** Python 3 + Pillow; JSON manifests; Unity 6000.4.10f1; Tilemap/Tile assets; C# Editor APIs; NUnit EditMode/PlayMode tests; built-in 2D renderer.

## Global Constraints

- Tile size is exactly `16×16`, PPU `16`, Point filtered, uncompressed, and mipmap-free.
- Ten outdoor regions are independent visual kits: Tianshu, Cangyue, Yanliu, Chisha, Youhuang, Hanyuan, Prologue Village, Luoyuan, Jueyun, Zhenyue.
- Every region has at least 8 ground variants, 16 decorations, 3 landmarks, one day/night palette pair, one weather treatment, one walkable showcase map, and an associated interior or underground map.
- Shared structural recipes may be reused, but region identity cannot be a whole-image tint.
- Maps use Ground, Water, Lower Environment, Buildings, Character, Foreground, and Effects layers with stable entry/exit/trigger anchors.
- Runtime placeholder textures and full-map background screenshots are forbidden.
- Run shell commands through `rtk`; final commits land on `main`.

---

## File Structure

- Create `tools/art_pipeline/environment_modules.py`: load and validate ground, shore, wall, roof, foliage, prop, landmark, and weather PNG modules.
- Create `tools/art_pipeline/environment_roster.py`: exact region/interior recipes and counts.
- Create `tools/art_pipeline/map_layout.py`: strict tile-layer and anchor layout schema.
- Create `tools/art_pipeline/tests/test_environment_roster.py`.
- Create `tools/art_pipeline/tests/test_map_layout.py`.
- Create `Assets/ArtSource/Environment/Manifests/regions.json`.
- Create `Assets/ArtSource/Environment/Manifests/interiors.json`.
- Create source PNG modules under `Assets/ArtSource/Environment/{Shared,Regions,Interiors}/`.
- Create `Assets/ArtSource/Environment/Layouts/<region-id>.json` for ten outdoor maps.
- Create `Assets/ArtSource/Environment/Layouts/interiors/<interior-id>.json` for thirteen interior maps.
- Create baked outputs under `Assets/Art/Environment/Regions/<region-id>/` and `Assets/Art/Environment/Interiors/<interior-id>/`.
- Create `Assets/Scripts/Art/RegionSceneDefinition.cs`: runtime-safe region layout reference.
- Create `Assets/Scripts/Editor/Art/EnvironmentTileBuilder.cs`: Tile asset creation.
- Create `Assets/Scripts/Editor/Art/RegionSceneBuilder.cs`: layered scene generation.
- Create `Assets/Scripts/Editor/Art/EnvironmentShowcaseGenerator.cs`: overview scene and screenshots.
- Create `Assets/Tests/EditMode/EnvironmentArtTests.cs`.
- Create `Assets/Tests/PlayMode/EnvironmentTraversalPlayModeTests.cs`.

---

### Task 1: Lock the Region and Map Layout Contracts

**Files:**
- Create: `tools/art_pipeline/environment_roster.py`
- Create: `tools/art_pipeline/map_layout.py`
- Create: `tools/art_pipeline/tests/test_environment_roster.py`
- Create: `tools/art_pipeline/tests/test_map_layout.py`
- Create: `Assets/ArtSource/Environment/Manifests/regions.json`
- Create: `Assets/ArtSource/Environment/Manifests/interiors.json`

**Interfaces:**
- Produces: `build_region_recipes() -> tuple[EnvironmentRecipe, ...]`, `build_interior_recipes() -> tuple[EnvironmentRecipe, ...]`, `load_map_layout(path) -> MapLayout`.
- `MapLayout` exposes `layers`, `collisions`, `foreground_spans`, `anchors`, `bounds`, and `required_landmarks`.

- [ ] **Step 1: Write failing scope tests**

```python
def test_region_scope_is_exact(self):
    self.assertEqual(
        {r.id for r in build_region_recipes()},
        {"tianshu", "cangyue", "yanliu", "chisha", "youhuang", "hanyuan",
         "prologue_village", "luoyuan", "jueyun", "zhenyue"})

def test_every_region_meets_minimum_art_counts(self):
    for region in build_region_recipes():
        self.assertGreaterEqual(len(region.ground_variants), 8)
        self.assertGreaterEqual(len(region.decorations), 16)
        self.assertEqual(len(region.landmarks), 3)
```

- [ ] **Step 2: Run tests and verify failure**

Run: `rtk python3 -m unittest tools.art_pipeline.tests.test_environment_roster tools.art_pipeline.tests.test_map_layout -v`  
Expected: FAIL because environment roster and map layout modules do not exist.

- [ ] **Step 3: Implement strict region/interior recipe expansion**

Define explicit structural sets, palette groups, tile roles, decoration IDs, landmark IDs, weather ID, day/night palettes, and associated interior IDs. Reject missing roles, duplicate IDs, fewer-than-required counts, or region recipes containing only a global tint transform.

- [ ] **Step 4: Implement strict map layout loading**

Layer cells are integer `(x,y,tileId)` records. Anchors use stable IDs and types `entry`, `exit`, `interior`, `quest`, `spawn`, or `camera`. Validation rejects out-of-bounds cells, missing entry/exit, overlapping solid entrance cells, unknown tile IDs, and missing required landmarks.

- [ ] **Step 5: Run contract tests**

Expected: all environment contract tests PASS.

- [ ] **Step 6: Commit contracts**

```bash
rtk git add tools/art_pipeline Assets/ArtSource/Environment/Manifests
rtk git commit -m "feat: define region and map art contracts"
```

### Task 2: Build the Shared Environment Module Library

**Files:**
- Create: `tools/art_pipeline/environment_modules.py`
- Modify: `tools/art_pipeline/environment_baker.py`
- Modify: `tools/art_pipeline/tests/test_environment_baker.py`

**Interfaces:**
- Produces tile families for ground, road, shore, cliff, wall, roof, doors, windows, bridges, foliage, props, water, landmarks, and weather overlays.

- [ ] **Step 1: Write failing tile-family tests**

Assert every edge-capable family provides north/south/east/west edges plus four corners; every roof family provides body/eave/ridge/left/right/corners; every animated water family provides exactly eight frames.

- [ ] **Step 2: Run tests and verify failure**

Expected: FAIL because shared environment modules are absent.

- [ ] **Step 3: Author pixel-cluster source modules**

Draw and commit reusable transparent PNG module families for stone, timber, plaster, tile roof, sand, snow, ice, water, bamboo, broadleaf, pine, scorch, smoke, fog, rain, snow, petals, and lantern glow. `environment_modules.py` loads, validates, transforms, and palette-maps these modules; it must not synthesize final tiles or landmarks from geometric primitives.

- [ ] **Step 4: Implement structural families and collision metadata**

Each baked tile metadata record includes `role`, `solid`, `foreground`, and `animationGroup`. Landmark metadata stores sprite rect, foot pivot, collision polygon, and foreground cut line.

- [ ] **Step 5: Run baker tests and commit**

Run the full Python test suite. Expected: PASS. Commit shared environment modules and updated metadata logic.

### Task 3: Produce the Six Core Region Kits

**Files:**
- Modify: `Assets/ArtSource/Environment/Manifests/regions.json`
- Create: `Assets/Art/Environment/Regions/{tianshu,cangyue,yanliu,chisha,youhuang,hanyuan}/**`
- Modify: `tools/art_pipeline/tests/test_environment_roster.py`

**Interfaces:**
- Produces one tileset, landmark sheet, decoration sheet, weather sheet, palette preview, and scene preview per core region.

- [ ] **Step 1: Add exact landmark tests**

Assert these landmark triplets:

```text
tianshu: city_gate, imperial_avenue, academy
cangyue: mountain_temple, cloud_bridge, sword_platform
yanliu: inn, arched_bridge, pharmacy
chisha: fortress_gate, beacon_tower, caravan_inn
youhuang: bamboo_shrine, poison_marsh_lab, hidden_camp
hanyuan: hot_spring_inn, ice_lake_tomb, hunter_village
```

- [ ] **Step 2: Run tests and verify missing landmarks fail**

Expected: FAIL until all six region recipes contain the exact triplets.

- [ ] **Step 3: Author region-specific structures and vegetation**

Tianshu uses vermilion walls and axial street structures; Cangyue uses cliff/stair/cloud motifs; Yanliu uses white walls, gray roofs, waterways, willow, and lotus; Chisha uses sandstone, wind erosion, fortification, and cloth awnings; Youhuang uses layered bamboo, wet stone, poison pools, and mechanisms; Hanyuan uses snow caps, ice cracks, timber/fur buildings, and steam.

- [ ] **Step 4: Bake six kits and generate contact sheets**

Run the region manifest build. Generate one labeled contact sheet per region containing all tile roles, decorations, landmarks, day/night palette, and weather frames.

- [ ] **Step 5: Perform cross-region visual acceptance**

View contact sheets in grayscale and color. Reject any pair of regions that can only be distinguished by hue. Check seamless roads, shores, walls, roofs, water loops, and foreground cut lines.

- [ ] **Step 6: Validate and commit core region art**

Run global Python validation; commit source recipes and six baked region folders.

### Task 4: Produce the Four Extended Region Kits

**Files:**
- Modify: `Assets/ArtSource/Environment/Manifests/regions.json`
- Create: `Assets/Art/Environment/Regions/{prologue_village,luoyuan,jueyun,zhenyue}/**`
- Modify: `tools/art_pipeline/tests/test_environment_roster.py`

**Interfaces:**
- Produces four independent kits and a second state `prologue_village_burned` using the same layout anchors.

- [ ] **Step 1: Add exact landmark/state tests**

```text
prologue_village: blacksmith, ancestral_tree, village_gate + burned state
luoyuan: east_city_gate, canal_market, escape_alley
jueyun: sword_sect_gate, chain_bridge, summit_platform
zhenyue: stele_forest, ritual_altar, mountain_garrison
```

- [ ] **Step 2: Run tests and verify failure**

Expected: FAIL until the four kits and burned state exist.

- [ ] **Step 3: Author and bake extended kits**

The burned village state replaces roof/wall/foliage variants with charred and flame-damaged structures while preserving collision and anchor IDs. Luoyuan is denser and more mercantile than Tianshu; Jueyun uses rope/chain and narrow cliff architecture; Zhenyue uses monumental stone and military ritual structures.

- [ ] **Step 4: Validate visual and structural independence**

Reject direct recolors of core-region landmark silhouettes. Verify the normal/burned village layouts share exact anchor coordinates.

- [ ] **Step 5: Commit extended region art**

Commit source manifests and four baked region folders after global validation exits 0.

### Task 5: Produce Thirteen Interior and Underground Kits

**Files:**
- Modify: `Assets/ArtSource/Environment/Manifests/interiors.json`
- Create: `Assets/Art/Environment/Interiors/{inn,residence,shop,pharmacy,academy,yamen,palace,temple,cave,tomb,dungeon,military_camp,ship_cabin}/**`
- Modify: `tools/art_pipeline/tests/test_environment_roster.py`

**Interfaces:**
- Produces exactly 13 interior kits with floor, wall, door, furniture/props, foreground, light source, and entry/exit tile roles.

- [ ] **Step 1: Write exact interior scope tests**

Assert the 13 IDs above and require at least four floor variants, four wall variants, eight props, one light source, one entrance, and one exit per kit.

- [ ] **Step 2: Run and verify failure**

Expected: FAIL because interior recipes are incomplete.

- [ ] **Step 3: Author interior-specific props and structure**

Create role-appropriate visual structure: counters and tables for inns/shops, cabinets and herb drawers for pharmacies, desks/screens for academy/yamen, throne screens for palace, altar/incense for temple, rock/ore for cave, sarcophagus/seals for tomb, bars/chains for dungeon, tents/racks for camp, and curved timber/cargo for ship cabin.

- [ ] **Step 4: Bake, preview, and validate interiors**

Generate a 13-panel contact sheet and reject kits distinguishable only by labels. Run global validation.

- [ ] **Step 5: Commit interior art**

Commit source and baked interior folders.

### Task 6: Author Ten Outdoor and Thirteen Interior Map Layouts

**Files:**
- Create: `Assets/ArtSource/Environment/Layouts/*.json`
- Create: `Assets/ArtSource/Environment/Layouts/interiors/*.json`
- Modify: `tools/art_pipeline/tests/test_map_layout.py`

**Interfaces:**
- Produces 23 validated `MapLayout` documents consumed by Unity `RegionSceneBuilder`.

- [ ] **Step 1: Write failing layout coverage and path tests**

For every layout, flood-fill walkable cells from the `entry` anchor and assert all `exit`/`interior` anchors and all three landmarks are reachable. Assert map bounds are at least 30×20 outdoor and 12×10 indoor tiles.

- [ ] **Step 2: Run and verify failure**

Expected: FAIL listing 23 missing layouts.

- [ ] **Step 3: Author outdoor layouts**

Each outdoor layout includes one readable main route, at least one optional loop, three landmarks, collision boundaries, foreground spans, spawn anchors, quest anchors, entry, exit, and interior anchors. Water/cliff obstacles must not isolate required anchors.

- [ ] **Step 4: Author interior layouts**

Each interior layout includes entry and exit anchors, a clear interaction route, solid wall/furniture cells, and foreground spans for tall walls or canopies.

- [ ] **Step 5: Run path validation and commit layouts**

Run map layout tests and global validator. Expected: all 23 layouts PASS reachability and bounds checks. Commit the layout JSON files.

### Task 7: Build Unity Tiles, Scene Definitions, and Showcase Maps

**Files:**
- Create: `Assets/Scripts/Art/RegionSceneDefinition.cs`
- Create: `Assets/Scripts/Editor/Art/EnvironmentTileBuilder.cs`
- Create: `Assets/Scripts/Editor/Art/RegionSceneBuilder.cs`
- Create: `Assets/Tests/EditMode/EnvironmentArtTests.cs`
- Create generated assets under `Assets/Tilemaps/Formal/` and `Assets/Scenes/Regions/`.

**Interfaces:**
- Produces: `EnvironmentTileBuilder.RebuildAll()`, `RegionSceneBuilder.Build(string regionId)`, `RegionSceneBuilder.BuildAll()`.

- [ ] **Step 1: Write failing Unity generation tests**

```csharp
[TestCase("yanliu")]
[TestCase("tianshu")]
[TestCase("prologue_village")]
public void RegionSceneContainsRequiredFormalLayers(string regionId)
{
    var scene = RegionSceneBuilder.BuildForTest(regionId);
    Assert.That(scene.LayerNames, Is.EquivalentTo(new[] {
        "Ground", "Water", "Lower Environment", "Buildings", "Character", "Foreground", "Effects" }));
    Assert.That(scene.RuntimeCreatedTextureCount, Is.Zero);
}
```

- [ ] **Step 2: Run focused EditMode tests and verify failure**

Expected: compile/test failure before builder types exist.

- [ ] **Step 3: Implement Tile asset and scene-definition generation**

Read `.art.json` and layout JSON, create named Tile assets, collision roles, foreground objects, region definitions, and catalog entries. Unknown tile IDs or missing landmarks abort before scene save.

- [ ] **Step 4: Implement layered scene generation**

Create Grid, named Tilemaps, CompositeCollider2D for solids, stable anchor GameObjects, day/night tint controller data, and weather renderer data. Do not add gameplay managers yet.

- [ ] **Step 5: Build all 23 scenes and run EditMode tests**

Expected: all scenes save, reopen, and resolve catalog references with zero errors.

- [ ] **Step 6: Commit Unity environment assets**

Commit runtime/editor code, Tile assets, region definitions, and generated region/interior scenes.

### Task 8: Verify Traversal, Foreground, Weather, and Day/Night

**Files:**
- Create: `Assets/Scripts/Editor/Art/EnvironmentShowcaseGenerator.cs`
- Create: `Assets/Scenes/EnvironmentShowcase.unity`
- Create: `Assets/Tests/PlayMode/EnvironmentTraversalPlayModeTests.cs`

**Interfaces:**
- Produces a region selector showcase and automated traversal probes for all 23 maps.

- [ ] **Step 1: Write failing PlayMode traversal tests**

Load each generated map additively, place a probe at entry, follow the validated path to each required anchor, and assert the probe never intersects a solid collider. Toggle day/night and weather states and assert formal renderers remain enabled and no missing sprite warnings occur.

- [ ] **Step 2: Run focused PlayMode tests and verify failure**

Expected: FAIL before showcase controller and traversal fixtures exist.

- [ ] **Step 3: Generate the environment showcase**

Provide deterministic cameras for each region at day, night, and weather state. Include labels outside gameplay view and no placeholder geometry.

- [ ] **Step 4: Run automated and manual validation**

Run Python, EditMode, and PlayMode suites. Walk each outdoor map and its associated interior. Inspect roof/tree/bridge foreground transitions, shore seams, collision edges, landmark readability, weather loops, and night visibility.

- [ ] **Step 5: Commit the complete environment phase**

```bash
rtk git add Assets/Scenes Assets/Scripts/Art Assets/Scripts/Editor/Art Assets/Tests Assets/Art/Environment Assets/Tilemaps/Formal
rtk git commit -m "test: verify complete environment art library"
```

## Plan Completion Gate

- Catalog contains exactly 10 outdoor region entries and 13 interior entries.
- Every outdoor region has at least 8 ground variants, 16 decorations, exactly 3 required landmarks, day/night data, weather data, and a reachable showcase map.
- All 23 maps pass reachability, collision, save/reopen, and missing-reference checks.
- Every region is recognizable by structure and silhouette in grayscale, not only by color.
- No formal environment relies on runtime `Texture2D` placeholders or a flat full-map background image.
