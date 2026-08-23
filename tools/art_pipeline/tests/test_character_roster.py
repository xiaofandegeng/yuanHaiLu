import json
import unittest
from pathlib import Path

from PIL import Image

from tools.art_pipeline.character_modules import validate_character_modules
from tools.art_pipeline.source_audit import audit_character_sources
from tools.art_pipeline.character_roster import (
    BOSS_IDS,
    CORE_REGIONS,
    ENEMY_IDS,
    NAMED_IDS,
    NPC_ROLES,
    PLAYER_CLASSES,
    build_boss_recipes,
    build_enemy_recipes,
    build_named_recipes,
    build_npc_recipes,
    build_player_recipes,
    build_roster,
)


class CharacterRosterTests(unittest.TestCase):
    def test_player_roster_contains_two_genders_by_six_classes(self):
        recipes = build_player_recipes()
        self.assertEqual(len(recipes), 12)
        self.assertEqual(
            {recipe.id for recipe in recipes},
            {
                "player_{}_{}".format(gender, profession)
                for gender in ("male", "female")
                for profession in PLAYER_CLASSES
            },
        )

    def test_every_player_has_two_skills_and_three_attacks(self):
        for recipe in build_player_recipes():
            names = {row.name for row in recipe.animations}
            self.assertTrue(
                {"attack_1", "attack_2", "attack_3", "skill_1", "skill_2"}
                <= names
            )

    def test_named_roster_is_exact_and_excludes_antagonist_bosses(self):
        recipes = build_named_recipes()
        self.assertEqual({recipe.id for recipe in recipes}, set(NAMED_IDS))
        self.assertEqual(len(recipes), 15)
        self.assertFalse(
            {"helian_beiming", "liu_hanzhang", "feng_sanniang"}
            & {recipe.id for recipe in recipes}
        )

    def test_npc_roster_has_six_roles_per_core_region(self):
        recipes = build_npc_recipes()
        self.assertEqual(len(recipes), 36)
        for region in CORE_REGIONS:
            self.assertEqual(
                sum(recipe.id.startswith(region + "_") for recipe in recipes),
                len(NPC_ROLES),
            )

    def test_enemy_and_boss_scopes_are_exact(self):
        self.assertEqual({recipe.id for recipe in build_enemy_recipes()}, set(ENEMY_IDS))
        self.assertEqual(len(build_enemy_recipes()), 24)
        self.assertEqual({recipe.id for recipe in build_boss_recipes()}, set(BOSS_IDS))
        self.assertEqual(len(build_boss_recipes()), 10)

    def test_complete_roster_has_97_unique_ids(self):
        recipes = build_roster()
        ids = [recipe.id for recipe in recipes]
        self.assertEqual(len(ids), 97)
        self.assertEqual(len(set(ids)), 97)

    def test_every_recipe_uses_six_visible_editable_modules(self):
        for recipe in build_roster():
            self.assertTrue(validate_character_modules(recipe), recipe.id)

    def test_all_formal_character_sources_are_complete_and_unique(self):
        self.assertEqual(audit_character_sources(build_roster()), [])

    def test_named_and_principal_boss_union_matches_18_story_roles(self):
        principal_bosses = {"helian_beiming", "liu_hanzhang", "feng_sanniang"}
        self.assertEqual(len(set(NAMED_IDS) | principal_bosses), 18)
        self.assertEqual(set(NAMED_IDS) & principal_bosses, set())

    def test_every_roster_entry_has_an_independent_baked_output(self):
        project_root = Path(__file__).resolve().parents[3]
        for recipe in build_roster():
            category = self._category(recipe.id)
            image_path = project_root / "Assets" / "Art" / "Characters" / category / (recipe.id + ".png")
            metadata_path = image_path.with_suffix(".art.json")
            self.assertTrue(image_path.exists(), str(image_path))
            self.assertTrue(metadata_path.exists(), str(metadata_path))
            metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
            self.assertEqual(metadata["id"], recipe.id)
            expected_frame_size = 48 if recipe.id == "player_male_swordsman" else 32
            self.assertEqual(metadata["frameSize"], expected_frame_size)
            self.assertTrue(metadata["sha256"])
            with Image.open(image_path) as source:
                sheet = source.convert("RGBA")
            for sprite in metadata["sprites"]:
                x, y, width, height = sprite["rect"]
                self.assertIsNotNone(
                    sheet.crop((x, y, x + width, y + height)).getbbox(),
                    sprite["name"],
                )

    @staticmethod
    def _category(art_id):
        if art_id.startswith("player_"):
            return "Player"
        if art_id in NAMED_IDS:
            return "Named"
        if art_id in BOSS_IDS:
            return "Bosses"
        if art_id in ENEMY_IDS:
            return "Enemies"
        return "NPC"


if __name__ == "__main__":
    unittest.main()
