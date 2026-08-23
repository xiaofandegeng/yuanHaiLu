import hashlib
import json
import shutil
from pathlib import Path
from tempfile import TemporaryDirectory
import unittest

from PIL import Image


PROJECT_ROOT = Path(__file__).resolve().parents[3]
ROSTER_PATH = Path("Assets/ArtSource/Characters/Manifests/player-roster.json")
HERO_MODULE_NAMES = ("body", "face", "hair", "outfit", "weapon", "accessory")
WEAPON_IDS = ("weapon_sword", "weapon_gauntlets", "weapon_dart")
ALLOWED_MODULE_SIZES = {(16, 16), (32, 32), (48, 48), (64, 64)}
TOWN_ROLES = {
    "road", "water", "shore", "inn_roof", "inn_wall", "inn_door",
    "bridge", "boat", "bollard", "lantern", "foreground_foliage",
}
INN_ROLES = {
    "entrance", "walkway", "counter", "innkeeper_light", "table",
    "kitchen_fire", "stairs", "north_exit", "foreground_beam",
}


class MvpDenseArtBuilderTests(unittest.TestCase):
    def test_dense_hero_has_48_pixel_nonmirrored_directions_and_weapon_layers(self):
        with TemporaryDirectory() as temporary:
            root = Path(temporary)
            roster_path = root / ROSTER_PATH
            roster_path.parent.mkdir(parents=True)
            shutil.copy2(PROJECT_ROOT / ROSTER_PATH, roster_path)

            try:
                from tools.art_pipeline.mvp_dense_art_builder import build_dense_mvp_art
            except ImportError as error:
                self.fail("dense MVP art builder is required: {}".format(error))

            _, changed = build_dense_mvp_art(root)
            roster = json.loads(roster_path.read_text(encoding="utf-8"))
            hero = roster["characters"][0]
            self.assertEqual(hero["id"], "player_male_swordsman")
            self.assertEqual(hero["frameSize"], 48)

            expected_size = (
                max(animation["frames"] for animation in hero["animations"]) * 48,
                len(hero["animations"]) * 48,
            )
            for module_name in HERO_MODULE_NAMES:
                module_path = root / "Assets/ArtSource/Characters/Generated/player_male_swordsman" / (
                    module_name + ".png")
                with Image.open(module_path) as module:
                    self.assertEqual(module.size, expected_size)
                    self.assertGreater(sum(pixel[3] > 0 for pixel in module.getdata()), 20)

            for animation_name in ("idle", "walk", "attack_1"):
                hashes = {
                    self._composited_frame_hash(root, hero, animation_name, direction)
                    for direction in ("down", "left", "right", "up")
                }
                self.assertEqual(len(hashes), 4, animation_name + " directions must be independently drawn")

            weapon_hashes = []
            for weapon_id in WEAPON_IDS:
                weapon_path = root / "Assets/Resources/Art/MVP" / (weapon_id + ".png")
                with Image.open(weapon_path) as weapon:
                    self.assertEqual(weapon.size, (48, 48))
                    self.assertGreater(sum(pixel[3] > 0 for pixel in weapon.getdata()), 50)
                    weapon_hashes.append(hashlib.sha256(weapon.convert("RGBA").tobytes()).hexdigest())
            self.assertEqual(len(set(weapon_hashes)), 3)

            _, second_changed = build_dense_mvp_art(root)
            self.assertGreater(changed, 0)
            self.assertEqual(second_changed, 0)

    def test_town_modules_are_small_persistent_assets_with_all_navigation_roles(self):
        with TemporaryDirectory() as temporary:
            root = Path(temporary)
            roster_path = root / ROSTER_PATH
            roster_path.parent.mkdir(parents=True)
            shutil.copy2(PROJECT_ROOT / ROSTER_PATH, roster_path)

            from tools.art_pipeline.mvp_dense_art_builder import build_dense_mvp_art
            build_dense_mvp_art(root)

            layout_path = root / "Assets/ArtSource/MVP/dense_pixel/layouts/town.json"
            self.assertTrue(layout_path.is_file(), "town placement layout must be authored")
            layout = json.loads(layout_path.read_text(encoding="utf-8"))
            self.assertEqual(set(layout["roles"]), TOWN_ROLES)
            self.assertGreaterEqual(len(layout["placements"]), 32)

            for placement in layout["placements"]:
                self.assertIn(placement["layer"], {"Ground", "Environment", "Foreground"})
                relative_asset = Path(placement["asset"])
                source_path = root / "Assets/ArtSource/MVP/dense_pixel/environment" / relative_asset
                output_path = root / "Assets/Art/MVP/dense_pixel/environment" / relative_asset
                with Image.open(source_path) as source, Image.open(output_path) as output:
                    source_rgba = source.convert("RGBA")
                    output_rgba = output.convert("RGBA")
                    self.assertIn(source_rgba.size, ALLOWED_MODULE_SIZES, str(source_path))
                    self.assertEqual(output_rgba.size, source_rgba.size)
                    self.assertEqual(output_rgba.tobytes(), source_rgba.tobytes())
                    self.assertIsNotNone(source_rgba.getbbox(), str(source_path))

            for actor_id in ("mvp_bandit_a", "mvp_bandit_b"):
                actor_path = root / "Assets/Resources/Art/MVP/dense_pixel/actors" / (actor_id + ".png")
                with Image.open(actor_path) as actor:
                    self.assertEqual(actor.size, (48, 48))
                    self.assertGreater(sum(pixel[3] > 0 for pixel in actor.getdata()), 120)
            pouch_path = root / "Assets/Resources/Art/MVP/dense_pixel/actors/mvp_lost_pouch.png"
            with Image.open(pouch_path) as pouch:
                self.assertEqual(pouch.size, (16, 16))
                self.assertGreater(sum(pixel[3] > 0 for pixel in pouch.getdata()), 30)

    def test_inn_modules_keep_the_counter_route_clear_and_the_foreground_limited(self):
        with TemporaryDirectory() as temporary:
            root = Path(temporary)
            roster_path = root / ROSTER_PATH
            roster_path.parent.mkdir(parents=True)
            shutil.copy2(PROJECT_ROOT / ROSTER_PATH, roster_path)

            from tools.art_pipeline.mvp_dense_art_builder import build_dense_mvp_art
            build_dense_mvp_art(root)

            layout_path = root / "Assets/ArtSource/MVP/dense_pixel/layouts/inn.json"
            self.assertTrue(layout_path.is_file(), "inn placement layout must be authored")
            layout = json.loads(layout_path.read_text(encoding="utf-8"))
            self.assertEqual(set(layout["roles"]), INN_ROLES)
            self.assertGreaterEqual(len(layout["placements"]), 30)
            self.assertLessEqual(sum(item["area"] for item in layout["foreground"]), 19440)

            for placement in layout["placements"]:
                relative_asset = Path(placement["asset"])
                source_path = root / "Assets/ArtSource/MVP/dense_pixel/environment" / relative_asset
                output_path = root / "Assets/Art/MVP/dense_pixel/environment" / relative_asset
                with Image.open(source_path) as source, Image.open(output_path) as output:
                    source_rgba = source.convert("RGBA")
                    output_rgba = output.convert("RGBA")
                    self.assertIn(source_rgba.size, ALLOWED_MODULE_SIZES, str(source_path))
                    self.assertEqual(output_rgba.tobytes(), source_rgba.tobytes())
                    self.assertIsNotNone(source_rgba.getbbox(), str(source_path))

            innkeeper_path = root / "Assets/Resources/Art/MVP/dense_pixel/actors/mvp_innkeeper.png"
            with Image.open(innkeeper_path) as innkeeper:
                self.assertEqual(innkeeper.size, (48, 48))
                self.assertGreater(sum(pixel[3] > 0 for pixel in innkeeper.getdata()), 140)

    @staticmethod
    def _composited_frame_hash(root, hero, animation_name, direction):
        row_index = next(
            index for index, animation in enumerate(hero["animations"])
            if animation["name"] == animation_name and animation["direction"] == direction)
        frame = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
        for module_name in HERO_MODULE_NAMES:
            module_path = root / "Assets/ArtSource/Characters/Generated/player_male_swordsman" / (
                module_name + ".png")
            with Image.open(module_path) as module:
                cropped = module.convert("RGBA").crop((0, row_index * 48, 48, (row_index + 1) * 48))
                frame.alpha_composite(cropped)
        return hashlib.sha256(frame.tobytes()).hexdigest()


if __name__ == "__main__":
    unittest.main()
