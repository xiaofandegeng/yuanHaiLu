"""Regression tests for the two persistent MVP gameplay backdrops."""

from pathlib import Path
from tempfile import TemporaryDirectory
import unittest

from PIL import Image

from tools.art_pipeline.mvp_backdrop_builder import build_mvp_backdrops


class MvpBackdropBuilderTests(unittest.TestCase):
    def test_builds_crisp_480_by_270_backgrounds_from_the_named_sources(self):
        """Removing a source, changing its mapping, or producing a non-logical frame fails."""
        with TemporaryDirectory() as temporary:
            root = Path(temporary)
            source = root / "source"
            output = root / "output"
            source.mkdir()
            # A non-16:9 hard-pixel fixture proves the builder crops to the logical frame
            # and preserves a literal source pixel instead of smoothing it.
            for name, color in (("yanliu_mvp_concept_v1.png", (12, 90, 140)),
                                ("inn_mvp_concept_v1.png", (180, 80, 24))):
                image = Image.new("RGB", (1672, 941), color)
                image.putpixel((0, 0), (255, 255, 255))
                image.save(source / name)

            built = build_mvp_backdrops(source, output)

            self.assertEqual(
                built,
                (output / "mvp_yanliu_backdrop.png", output / "mvp_inn_backdrop.png"),
            )
            for path, color in zip(built, ((12, 90, 140), (180, 80, 24))):
                with Image.open(path) as image:
                    self.assertEqual(image.size, (480, 270))
                    self.assertEqual(image.mode, "RGB")
                    self.assertEqual(image.getpixel((240, 135)), color)


if __name__ == "__main__":
    unittest.main()
