import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from PIL import Image

from tools.art_pipeline.character_roster import build_roster
from tools.art_pipeline.character_source_builder import (
    CharacterDesign,
    build_character_sources,
    load_character_designs,
)
from tools.art_pipeline.schema import AnimationRow, CharacterRecipe


DESIGN_PATH = (
    Path(__file__).resolve().parents[3]
    / "Assets"
    / "ArtSource"
    / "Characters"
    / "Designs"
    / "character-designs.json"
)


class CharacterSourceBuilderTests(unittest.TestCase):
    def test_every_roster_id_has_a_unique_visual_design_record(self):
        designs = load_character_designs(DESIGN_PATH)
        roster_ids = {recipe.id for recipe in build_roster()}
        self.assertEqual(set(designs), roster_ids)
        self.assertEqual(
            len({design.signature for design in designs.values()}), len(roster_ids)
        )

    def test_builder_writes_six_visible_sheets_matching_animation_dimensions(self):
        recipe = CharacterRecipe(
            "test_wuxia_hero",
            32,
            (),
            (
                AnimationRow("idle", "down", 4, 4, True),
                AnimationRow("attack_1", "left", 6, 12, False, (2,)),
            ),
        )
        design = CharacterDesign(
            "test_wuxia_hero",
            "broad_armored",
            ("ink_blue", "paper_white", "river_blue"),
            "braided_topknot",
            "lamellar_coat",
            "long_sword",
            "jade_guard",
        )
        with TemporaryDirectory() as temporary:
            destination = Path(temporary)
            paths = build_character_sources(recipe, design, destination)

            self.assertEqual(set(paths), {"body", "face", "hair", "outfit", "weapon", "accessory"})
            for path in paths.values():
                with Image.open(path) as source:
                    sheet = source.convert("RGBA")
                self.assertEqual(sheet.size, (192, 64))
                self.assertIsNotNone(sheet.getbbox())

    def test_semantic_silhouette_and_prop_styles_change_the_pixel_masks(self):
        recipe = CharacterRecipe(
            "test_style_compare",
            32,
            (),
            (AnimationRow("idle", "down", 1, 4, True),),
        )
        broad = CharacterDesign(
            "broad", "broad_lamellar", ("vermilion", "warm_brown", "gold"),
            "helmet", "lamellar_coat", "long_sword", "rank_banner",
        )
        slender = CharacterDesign(
            "slender", "slender_cloak", ("deep_purple", "ash_gray", "vermilion"),
            "high_ponytail", "split_hem_robe", "wrapped_fists", "jade_ribbon",
        )
        with TemporaryDirectory() as temporary:
            root = Path(temporary)
            broad_paths = build_character_sources(recipe, broad, root / "broad")
            slender_paths = build_character_sources(recipe, slender, root / "slender")
            broad_body = self._bbox(broad_paths["body"])
            slender_body = self._bbox(slender_paths["body"])
            broad_weapon = self._bbox(broad_paths["weapon"])
            slender_weapon = self._bbox(slender_paths["weapon"])

        self.assertGreater(broad_body[2] - broad_body[0], slender_body[2] - slender_body[0])
        self.assertGreater(broad_weapon[3] - broad_weapon[1], slender_weapon[3] - slender_weapon[1])

    def test_legacy_32px_source_builder_refuses_the_48px_mvp_hero(self):
        recipe = next(
            value for value in build_roster()
            if value.id == "player_male_swordsman"
        )
        design = load_character_designs(DESIGN_PATH)[recipe.id]
        with TemporaryDirectory() as temporary:
            with self.assertRaisesRegex(ValueError, "mvp_dense_art_builder"):
                build_character_sources(recipe, design, Path(temporary))

    @staticmethod
    def _bbox(path):
        with Image.open(path) as source:
            return source.convert("RGBA").getbbox()


if __name__ == "__main__":
    unittest.main()
