import json
import tempfile
import unittest
from pathlib import Path

from PIL import Image

from tools.art_pipeline.environment_baker import bake_environment
from tools.art_pipeline.schema import EnvironmentRecipe


class EnvironmentBakerTests(unittest.TestCase):
    def setUp(self):
        self.temp_dir = tempfile.TemporaryDirectory()
        self.root = Path(self.temp_dir.name)
        self.module_paths = []
        for name, color in (
            ("grass", (70, 120, 70, 255)),
            ("grass", (80, 135, 76, 255)),
            ("water", (55, 105, 140, 255)),
        ):
            path = self.root / "{}_{}.png".format(name, len(self.module_paths))
            image = Image.new("RGBA", (16, 16), color)
            image.putpixel((8, 8), (255, 255, 255, 255))
            image.save(path)
            self.module_paths.append(str(path))
        self.recipe = EnvironmentRecipe("yanliu", 16, tuple(self.module_paths))

    def tearDown(self):
        self.temp_dir.cleanup()

    def test_environment_tiles_are_16_pixels_and_deterministic(self):
        first = bake_environment(self.recipe, self.root / "first")
        second = bake_environment(self.recipe, self.root / "second")

        self.assertEqual(first.sha256, second.sha256)
        self.assertEqual(first.tile_size, 16)
        self.assertEqual(first.image.size, (48, 16))
        self.assertEqual(first.image.size[0] % 16, 0)
        self.assertEqual(first.image.size[1] % 16, 0)

    def test_environment_metadata_uses_center_pivots_and_stable_names(self):
        baked = bake_environment(self.recipe, self.root / "output")
        metadata = json.loads(baked.metadata_path.read_text(encoding="utf-8"))

        self.assertEqual(metadata["tileSize"], 16)
        self.assertEqual(metadata["sprites"][0]["name"], "yanliu__grass__0")
        self.assertEqual(metadata["sprites"][1]["name"], "yanliu__grass__1")
        self.assertEqual(metadata["sprites"][2]["name"], "yanliu__water__0")
        self.assertEqual(metadata["sprites"][0]["pivot"], [0.5, 0.5])


if __name__ == "__main__":
    unittest.main()
