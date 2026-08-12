# Full Character Art Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce, import, animate, catalog, and visually verify all 12 player combinations, current named characters, 36 NPCs, 24 enemies, and 10 Bosses as formal 32×32 pixel resources.

**Architecture:** Extend the approved modular recipes and animation-row contract from the art pipeline plan. Each recipe bakes to an independent sheet and Unity Animator Override Controller; a generated character showcase scene provides one deterministic visual and runtime QA surface for the entire roster.

**Tech Stack:** Python 3 + Pillow; JSON manifests; Unity 6000.4.10f1; C# Editor APIs; AnimationClip/AnimatorOverrideController; NUnit EditMode and PlayMode tests.

## Global Constraints

- All in-world character frames are exactly `32×32`, PPU `16`, Point filtered, uncompressed, and mipmap-free.
- Player set is exactly 12 combinations: two genders × swordsman, boxer, hidden-weapon, healer, scholar, mystic.
- Runtime modular assembly and runtime placeholder textures are forbidden; each roster ID resolves to an independent baked sheet.
- Non-combat NPCs receive idle, walk, talk, and emote only; combat actors receive the combat rows required by their role.
- Feet, hands, weapons, frame names, direction order, and hit frames follow the reference contract from `2026-08-12-art-pipeline-reference.md`.
- Run all repository shell commands through `rtk`; final commits land on `main`.

---

## File Structure

- Create `Assets/ArtSource/Characters/Manifests/player-roster.json`: 12 player recipes.
- Create `Assets/ArtSource/Characters/Manifests/named-roster.json`: 15 current non-Boss named recipes; the three principal antagonists live in the Boss manifest.
- Create `Assets/ArtSource/Characters/Manifests/npc-roster.json`: 36 regional NPC recipes.
- Create `Assets/ArtSource/Characters/Manifests/enemy-roster.json`: 24 enemy recipes.
- Create `Assets/ArtSource/Characters/Manifests/boss-roster.json`: 10 Boss recipes.
- Create source PNG modules under `Assets/ArtSource/Characters/{Bodies,Faces,Hair,Outfits,Weapons,Accessories}/`.
- Create `tools/art_pipeline/character_modules.py`: load and validate body, face, hair, outfit, weapon, accessory, and pose PNG modules.
- Create `tools/art_pipeline/character_roster.py`: exact roster constants and recipe expansion.
- Create `tools/art_pipeline/tests/test_character_roster.py`: count, ID, row, silhouette, anchor, and palette tests.
- Create baked outputs under `Assets/Art/Characters/{Player,Named,NPC,Enemies,Bosses}/`.
- Create `Assets/Scripts/Editor/Art/CharacterAnimationBuilder.cs`: clips and override controllers from `.art.json`.
- Create `Assets/Scripts/Editor/Art/CharacterShowcaseGenerator.cs`: full roster preview scene.
- Create `Assets/Scripts/Art/CharacterVisual.cs`: bind an art ID to renderer and animator.
- Create `Assets/Tests/EditMode/CharacterArtTests.cs`.
- Create `Assets/Tests/PlayMode/CharacterAnimationPlayModeTests.cs`.
- Create `Assets/Scenes/CharacterShowcase.unity` through the editor generator.

---

### Task 1: Expand the Player Module Library

**Files:**
- Create: `tools/art_pipeline/character_modules.py`
- Create: `tools/art_pipeline/character_roster.py`
- Create: `tools/art_pipeline/tests/test_character_roster.py`
- Create: `Assets/ArtSource/Characters/Manifests/player-roster.json`

**Interfaces:**
- Consumes: `PixelCanvas`, palette, schema, and reference anchors from the pipeline plan.
- Produces: `build_player_recipes() -> tuple[CharacterRecipe, ...]` with exactly 12 unique recipes.

- [ ] **Step 1: Write failing player-roster tests**

```python
def test_player_roster_contains_two_genders_by_six_classes(self):
    recipes = build_player_recipes()
    self.assertEqual(len(recipes), 12)
    self.assertEqual(
        {r.id for r in recipes},
        {f"player_{g}_{c}" for g in ("male", "female") for c in
         ("swordsman", "boxer", "hidden_weapon", "healer", "scholar", "mystic")})

def test_every_player_has_two_skills_and_three_attacks(self):
    for recipe in build_player_recipes():
        names = {row.name for row in recipe.animations}
        self.assertTrue({"attack_1", "attack_2", "attack_3", "skill_1", "skill_2"} <= names)
```

- [ ] **Step 2: Run the test and verify failure**

Run: `rtk python3 -m unittest tools.art_pipeline.tests.test_character_roster -v`  
Expected: FAIL because roster modules do not exist.

- [ ] **Step 3: Implement shared bodies and face/hair modules**

Draw transparent PNG modules for two body silhouettes, four skin-role masks, twelve hairstyles, six face details, and directional shadow/highlight masks. Every module fits the 32×32 frame and uses the same foot anchor `(16, 29)`. `character_modules.py` loads and validates these files; it does not draw substitute anatomy.

- [ ] **Step 4: Implement six profession outfit and weapon modules**

Draw transparent PNG pose modules for white/blue sword robes and sword, red/orange wraps and gauntlets, black/purple light armor and darts, green/white healer robes and needles/gourd, blue/brown scholar robes and brush/scroll, purple/gold mystic robes and talisman/compass. Manifests may substitute palette roles but may not request procedural rectangles, circles, or anatomy.

- [ ] **Step 5: Implement the canonical player pose rows**

Build four-direction idle, walk, dash, three attacks, two skills, hurt, dodge, down, and death. Walk uses six distinct locomotion poses; hit frames are `[2]`, `[3]`, and `[3]` for attack 1–3 unless the profession manifest explicitly overrides them.

- [ ] **Step 6: Run roster and baker tests**

Run: `rtk python3 -m unittest discover -s tools/art_pipeline/tests -v`  
Expected: all tests PASS and player count equals 12.

- [ ] **Step 7: Commit the player module library**

```bash
rtk git add tools/art_pipeline Assets/ArtSource/Characters/Manifests/player-roster.json
rtk git commit -m "feat: define twelve player art recipes"
```

### Task 2: Bake and Visually Gate All 12 Player Sheets

**Files:**
- Create: `Assets/Art/Characters/Player/player_*.png`
- Create: `Assets/Art/Characters/Player/player_*.art.json`
- Create: `Assets/Art/Characters/Player/previews/player-roster.png`

**Interfaces:**
- Consumes: player recipes from Task 1.
- Produces: 12 independent sheets plus a 4×3 labeled preview grid.

- [ ] **Step 1: Add expected-output tests**

Assert every player output has SHA metadata, width `192`, height equal to `32 × animation-row-count`, alpha content in every required frame, and no opaque pixels on frame borders except allowed weapon arcs.

- [ ] **Step 2: Run tests and verify missing outputs fail**

Run: `rtk python3 -m unittest tools.art_pipeline.tests.test_character_roster -v`  
Expected: FAIL listing the 12 missing baked outputs.

- [ ] **Step 3: Bake the player roster**

Run: `rtk python3 -m tools.art_pipeline.build --manifest Assets/ArtSource/Characters/Manifests/player-roster.json`

- [ ] **Step 4: Generate the roster preview**

Preview each combination at 6× nearest-neighbor scale in down-facing idle, walk contact, attack hit, skill, and hurt frames. Labels use stable IDs, not localized display names.

- [ ] **Step 5: Perform visual acceptance**

Reject any pair whose gender silhouettes are identical, any professions indistinguishable without reading labels, duplicated walk frames, weapon-hand separation, or isolated-noise pixels. Correct the source module rather than painting the baked sheet.

- [ ] **Step 6: Rebuild and validate**

Run: `rtk python3 -m tools.art_pipeline.validate --all`  
Expected: validation exits 0.

- [ ] **Step 7: Commit player outputs**

```bash
rtk git add Assets/Art/Characters/Player tools/art_pipeline Assets/ArtSource/Characters
rtk git commit -m "feat: bake full player character roster"
```

### Task 3: Produce the 15 Current Non-Boss Named Characters

**Files:**
- Create: `Assets/ArtSource/Characters/Manifests/named-roster.json`
- Create: `Assets/Art/Characters/Named/*.png`
- Modify: `tools/art_pipeline/character_roster.py`
- Modify: `tools/art_pipeline/tests/test_character_roster.py`

**Interfaces:**
- Produces 15 stable IDs for current named non-Boss roles. Together with the three principal antagonists produced in Task 5, the union matches all 18 names in the approved design spec.

- [ ] **Step 1: Write the exact named-roster test**

The expected ID set is:

```python
EXPECTED_NON_BOSS_NAMED = {
 "shen_ruolan", "zhao_wuhen", "su_qinghe", "xiao_wenyuan", "xuan_qingzi",
 "xiao_cangming", "du_qiusheng",
 "fengling_taihou", "shen_zhenyue", "cao_tianlang", "xiao_chengying",
 "innkeeper_zhao", "su_wanqing", "fishing_elder", "blacksmith_wang"
}
```

Assert the five companions have combat rows; the remaining ten have their role-appropriate row set. The three antagonist IDs must be absent here so the catalog cannot receive duplicates.

- [ ] **Step 2: Run the test and verify failure**

Expected: FAIL because named recipes are absent.

- [ ] **Step 3: Author unique silhouettes and palettes**

Each named role must specify a unique hair/outfit/accessory tuple. Demo NPCs retain readable innkeeper, medicine, fishing, and smith tools.

- [ ] **Step 4: Bake, preview, and validate named roles**

Run the named manifest build and global validator. Generate a labeled named-roster preview with idle, walk, talk or attack, and hurt frames.

- [ ] **Step 5: Commit named character art**

```bash
rtk git add Assets/ArtSource/Characters/Manifests/named-roster.json Assets/Art/Characters/Named tools/art_pipeline
rtk git commit -m "feat: add named character art roster"
```

### Task 4: Produce 36 Regional NPCs

**Files:**
- Create: `Assets/ArtSource/Characters/Manifests/npc-roster.json`
- Create: `Assets/Art/Characters/NPC/*.png`
- Modify: `tools/art_pipeline/character_roster.py`
- Modify: `tools/art_pipeline/tests/test_character_roster.py`

**Interfaces:**
- Produces IDs `<region>_<role>_<variant>` for six core regions × six roles: official, merchant, civilian, soldier, religious, faction.

- [ ] **Step 1: Write count and coverage tests**

```python
def test_npc_roster_has_six_roles_per_core_region(self):
    recipes = build_npc_recipes()
    self.assertEqual(len(recipes), 36)
    for region in CORE_REGIONS:
        self.assertEqual(sum(r.region == region for r in recipes), 6)
```

- [ ] **Step 2: Run and verify failure**

Expected: FAIL because NPC recipes are absent.

- [ ] **Step 3: Author region-specific occupation modules**

Use structure changes, not whole-image tinting: hats and layered robes for TianShu, mountain wraps and prayer accessories for Cangyue, water-town aprons and rain capes for Yanliu, scarves and armor plates for Chisha, bamboo/medicine accessories for Youhuang, and fur/snow gear for Hanyuan.

- [ ] **Step 4: Bake, preview, validate, and commit**

Generate a 6×6 preview grid. Reject region rows that remain indistinguishable in grayscale silhouette. Run global validation, then commit source and baked NPC outputs.

### Task 5: Produce 24 Enemies and 10 Bosses

**Files:**
- Create: `Assets/ArtSource/Characters/Manifests/enemy-roster.json`
- Create: `Assets/ArtSource/Characters/Manifests/boss-roster.json`
- Create: `Assets/Art/Characters/Enemies/*.png`
- Create: `Assets/Art/Characters/Bosses/*.png`
- Modify: `tools/art_pipeline/character_roster.py`
- Modify: `tools/art_pipeline/tests/test_character_roster.py`

**Interfaces:**
- Produces exactly the 24 enemy families and 10 Boss IDs fixed in the design spec.

- [ ] **Step 1: Write exact roster tests**

Assert four enemies for each core region and exact Boss IDs `helian_beiming`, `liu_hanzhang`, `feng_sanniang`, `prologue_black_guard`, `tianshu_black_market_lord`, `cangyue_traitor_master`, `yanliu_rebel_gang_lord`, `chisha_beidi_vanguard`, `youhuang_forbidden_mage`, `hanyuan_snow_beast`.
Also assert the union of `build_named_recipes()` and `build_boss_recipes()` contains the full 18-person named-role set from the design spec exactly once.

- [ ] **Step 2: Run and verify failure**

Expected: FAIL with missing enemy and Boss recipes.

- [ ] **Step 3: Author combat silhouettes and effects anchors**

Humanoid and creature recipes all fit 32×32. The three principal antagonists use the reserved silver-cloak, smiling-scholar, and blood-red-assassin silhouettes. Bosses use capes, weapons, horns, or aura anchors but keep feet inside the common bottom row. Every combat actor exposes `weapon_tip`, `projectile_origin`, `hurt_center`, and `ground_center` metadata.

- [ ] **Step 4: Bake and validate all combat sheets**

Generate region-labeled enemy previews and a 5×2 Boss preview. Reject bosses indistinguishable from their base enemy at 1× scale.

- [ ] **Step 5: Commit combat art**

```bash
rtk git add Assets/ArtSource/Characters/Manifests Assets/Art/Characters/Enemies Assets/Art/Characters/Bosses tools/art_pipeline
rtk git commit -m "feat: add regional enemies and bosses"
```

### Task 6: Build Unity Animations, Prefabs, and Catalog Entries

**Files:**
- Create: `Assets/Scripts/Editor/Art/CharacterAnimationBuilder.cs`
- Create: `Assets/Scripts/Art/CharacterVisual.cs`
- Create: `Assets/Tests/EditMode/CharacterArtTests.cs`
- Create generated assets under `Assets/Animations/Characters/`, `Assets/AnimatorControllers/Characters/`, and `Assets/Prefabs/Characters/`.

**Interfaces:**
- Produces: `CharacterAnimationBuilder.RebuildAll()`, `CharacterVisual.Apply(string artId)`, and one prefab/controller/catalog entry per baked character.

- [ ] **Step 1: Write failing Unity generation tests**

```csharp
[TestCase("player_male_swordsman")]
[TestCase("shen_ruolan")]
[TestCase("hanyuan_snow_beast")]
public void FormalCharacterHasSheetControllerAndPrefab(string id)
{
    Assert.That(CharacterArtCatalog.LoadDefault().TryGet(id, out var entry), Is.True);
    Assert.That(entry.SpriteSheet, Is.Not.Null);
    Assert.That(entry.AnimatorController, Is.Not.Null);
    Assert.That(entry.Prefab, Is.Not.Null);
}
```

- [ ] **Step 2: Run focused EditMode tests and verify failure**

Expected: compile/test failure because animation builder and visual component do not exist.

- [ ] **Step 3: Implement clip and override generation**

Create clips from named sprites and metadata FPS/loop settings. Add `PlayerCombat.OnAttackHitFrame` events on manifest hit frames and attack-finished events on final frames. Reuse the shared parameter set `MoveX`, `MoveY`, `Speed`, `IsDashing`, `IsAttacking`, and `AttackIndex`.

- [ ] **Step 4: Implement character prefabs and catalog updates**

Each prefab has SpriteRenderer, Animator, and `CharacterVisual`; player/enemy/NPC gameplay components are not added by the art builder. `CharacterVisual.Apply` changes formal assets only and throws for an unknown ID in editor/development builds.

- [ ] **Step 5: Run all EditMode tests**

Expected: generated-asset tests PASS for every roster ID.

- [ ] **Step 6: Commit Unity character integration**

```bash
rtk git add Assets/Scripts/Art Assets/Scripts/Editor/Art Assets/Animations/Characters Assets/AnimatorControllers/Characters Assets/Prefabs/Characters Assets/Tests/EditMode
rtk git commit -m "feat: build full character animation library"
```

### Task 7: Generate and Run the Full Character Showcase

**Files:**
- Create: `Assets/Scripts/Editor/Art/CharacterShowcaseGenerator.cs`
- Create: `Assets/Scenes/CharacterShowcase.unity`
- Create: `Assets/Tests/PlayMode/CharacterAnimationPlayModeTests.cs`

**Interfaces:**
- Produces menu `Tools/渊海录/美术/生成角色总览场景` and deterministic rows by roster category.

- [ ] **Step 1: Write failing PlayMode animation tests**

Instantiate one player, one NPC, one enemy, and one Boss prefab. Assert each Animator has a controller, transitions from idle to walk when `Speed=1`, and combat actors invoke their attack hit event exactly once.

- [ ] **Step 2: Run focused PlayMode tests and verify failure**

Expected: FAIL before showcase and generated controllers exist.

- [ ] **Step 3: Generate the showcase scene**

Lay out labeled rows for 12 players, 18 named roles, 36 NPCs, 24 enemies, and 10 Bosses. Add editor-only controls for animation selection and 1×/4×/8× camera zoom. No gameplay managers or placeholder sprites are present.

- [ ] **Step 4: Run full automated validation**

Run Python tests and validator, Unity EditMode tests, then Unity PlayMode tests. Expected: all PASS and Console has zero errors.

- [ ] **Step 5: Perform manual visual pass**

Play every animation category at 1× and 6× preview. Check foot anchoring, frame pacing, direction, weapon attachment, silhouette, palette separation, hit frame timing, and death final pose.

- [ ] **Step 6: Commit the complete character phase**

```bash
rtk git add Assets/Scenes/CharacterShowcase.unity Assets/Scripts/Editor/Art/CharacterShowcaseGenerator.cs Assets/Tests/PlayMode Assets/Art/Characters
rtk git commit -m "test: verify complete character art roster"
```

## Plan Completion Gate

- Catalog count is exactly 97 unique formal character entries: 12 player + 15 non-Boss named + 36 NPC + 24 enemy + 10 Boss. The three principal antagonists are included in the Boss group and complete the approved 18-person named-role union.
- Every entry resolves to an independent sheet, controller, prefab, and preview.
- Player professions and genders are readable at actual game scale.
- All Python, EditMode, and PlayMode tests pass; Unity Console has zero errors.
- No formal character relies on runtime composition or generated color-block fallback.
