import json
import tempfile
import unittest
from pathlib import Path

from PIL import Image

from tools.art_pipeline.build import build_manifest
from tools.art_pipeline.canvas import PixelBoundsError, PixelCanvas
from tools.art_pipeline.character_baker import bake_character
from tools.art_pipeline.schema import AnimationRow, CharacterRecipe, ManifestError
from tools.art_pipeline.validate import validate_outputs


class PixelCanvasTests(unittest.TestCase):
    def test_paste_rejects_modules_outside_canvas(self):
        canvas = PixelCanvas(32, 32)
        module = Image.new("RGBA", (16, 16), (255, 255, 255, 255))

        with self.assertRaisesRegex(PixelBoundsError, "outside 32x32"):
            canvas.paste(module, (24, 24))

    def test_recolor_only_changes_declared_palette_roles(self):
        canvas = PixelCanvas(2, 1)
        module = Image.new("RGBA", (2, 1))
        module.putdata([(10, 20, 30, 255), (1, 2, 3, 255)])

        recolored = canvas.recolor_by_palette_role(
            module, {(10, 20, 30, 255): (90, 80, 70, 255)}
        )

        self.assertEqual(list(recolored.getdata()), [(90, 80, 70, 255), (1, 2, 3, 255)])


class CharacterBakerTests(unittest.TestCase):
    def setUp(self):
        self.temp_dir = tempfile.TemporaryDirectory()
        self.root = Path(self.temp_dir.name)
        self.module_path = self.root / "body.png"
        module = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        for row in range(2):
            for frame in range(2):
                module.putpixel((frame * 32 + 16, row * 32 + 29), (30 + frame, 40 + row, 50, 255))
        module.save(self.module_path)
        self.recipe = CharacterRecipe(
            id="player_male_swordsman",
            frame_size=32,
            modules=(str(self.module_path),),
            animations=(
                AnimationRow("idle", "down", 2, 4, True),
                AnimationRow("walk", "down", 2, 8, True),
            ),
        )

    def tearDown(self):
        self.temp_dir.cleanup()

    def test_character_bake_is_deterministic(self):
        first = bake_character(self.recipe, self.root / "first")
        second = bake_character(self.recipe, self.root / "second")

        self.assertEqual(first.sha256, second.sha256)
        self.assertEqual(first.image.size, (64, 64))
        self.assertEqual(first.image.size[0] % 32, 0)
        self.assertEqual(first.image.size[1] % 32, 0)

    def test_only_fixed_male_player_can_use_a_48_pixel_frame(self):
        payload = {
            "id": "player_male_swordsman",
            "frameSize": 48,
            "modules": [str(self.module_path)],
            "animations": [],
        }

        try:
            fixed_male = CharacterRecipe.from_dict(payload)
        except ManifestError as error:
            self.fail("fixed male player must accept 48px frames: {}".format(error))
        self.assertEqual(fixed_male.frame_size, 48)

        payload["id"] = "player_female_swordsman"
        with self.assertRaisesRegex(ManifestError, "frameSize must be 32"):
            CharacterRecipe.from_dict(payload)

    def test_character_metadata_names_frames_and_bottom_pivots(self):
        baked = bake_character(self.recipe, self.root / "output")
        metadata = json.loads(baked.metadata_path.read_text(encoding="utf-8"))

        self.assertEqual(metadata["sha256"], baked.sha256)
        self.assertEqual(metadata["frameSize"], 32)
        self.assertEqual(metadata["sprites"][0]["name"], "player_male_swordsman__idle__down__0")
        self.assertEqual(metadata["sprites"][0]["pivot"], [0.5, 0.0])
        self.assertEqual(metadata["animations"][1]["fps"], 8)

    def test_build_manifest_writes_character_outputs(self):
        manifest_path = self.root / "characters.json"
        manifest_path.write_text(
            json.dumps(
                {
                    "schemaVersion": 1,
                    "characters": [
                        {
                            "id": "player_male_swordsman",
                            "frameSize": 32,
                            "modules": [str(self.module_path)],
                            "animations": [
                                {"name": "idle", "direction": "down", "frames": 2, "fps": 4, "loop": True},
                                {"name": "walk", "direction": "down", "frames": 2, "fps": 8, "loop": True},
                            ],
                        }
                    ],
                }
            ),
            encoding="utf-8",
        )

        result = build_manifest(manifest_path, self.root / "built")

        self.assertEqual(result.built, 1)
        self.assertEqual(result.skipped, 0)
        self.assertTrue((self.root / "built" / "player_male_swordsman.png").exists())

    def test_validation_detects_tampered_baked_image(self):
        baked = bake_character(self.recipe, self.root / "output")
        baked.image.putpixel((0, 0), (255, 0, 0, 255))
        baked.image.save(baked.image_path)

        errors = validate_outputs(self.root / "output")

        self.assertEqual(len(errors), 1)
        self.assertIn("hash mismatch", errors[0])

    def test_validation_accepts_the_fixed_male_48_pixel_hero_output(self):
        module_path = self.root / "hero_48.png"
        module = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
        for row in range(2):
            for frame in range(2):
                module.putpixel((frame * 48 + 24, row * 48 + 43), (30, 40, 50, 255))
        module.save(module_path)
        hero = CharacterRecipe(
            id="player_male_swordsman",
            frame_size=48,
            modules=(str(module_path),),
            animations=(
                AnimationRow("idle", "down", 2, 4, True),
                AnimationRow("walk", "down", 2, 8, True),
            ),
        )

        bake_character(hero, self.root / "output_48")

        self.assertEqual(validate_outputs(self.root / "output_48"), [])


if __name__ == "__main__":
    unittest.main()
