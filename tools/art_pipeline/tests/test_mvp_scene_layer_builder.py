"""Contract tests for the native-resolution layered MVP scene art."""

from pathlib import Path
from tempfile import TemporaryDirectory
import unittest

from PIL import Image

from tools.art_pipeline.mvp_scene_layer_builder import (
    LOGICAL_SIZE,
    P,
    build_mvp_scene_art,
    build_mvp_scene_art_with_change_count,
)


class MvpSceneLayerBuilderTests(unittest.TestCase):
    def test_builds_opaque_ground_transparent_foreground_and_persistent_actor_sprites(self):
        with TemporaryDirectory() as temporary:
            root = Path(temporary)
            output = root / "output"
            actors = root / "actors"
            built = build_mvp_scene_art(
                root / "source", output, root / "actor-source", actors)

            self.assertEqual(len(built), 10)
            for scene in ("yanliu", "inn"):
                with Image.open(output / "mvp_{}_ground_v2.png".format(scene)) as ground:
                    self.assertEqual(ground.size, LOGICAL_SIZE)
                    self.assertEqual(ground.mode, "RGBA")
                    self.assertTrue(all(pixel[3] == 255 for pixel in ground.getdata()))
                with Image.open(output / "mvp_{}_foreground_v2.png".format(scene)) as foreground:
                    self.assertEqual(foreground.size, LOGICAL_SIZE)
                    self.assertGreater(
                        sum(pixel[3] == 0 for pixel in foreground.getdata()),
                        LOGICAL_SIZE[0] * LOGICAL_SIZE[1] // 2,
                    )

            for actor in ("innkeeper", "bandit_a", "bandit_b", "lost_pouch"):
                with Image.open(actors / "mvp_{}.png".format(actor)) as sprite:
                    self.assertEqual(sprite.size, (32, 32))
                    self.assertIn(P["ink"], sprite.getdata())
                    self.assertGreater(sum(pixel[3] > 0 for pixel in sprite.getdata()), 300)

            _, changed = build_mvp_scene_art_with_change_count(
                root / "source", output, root / "actor-source", actors)
            self.assertEqual(changed, 0, "a second deterministic build must not rewrite outputs")


if __name__ == "__main__":
    unittest.main()
