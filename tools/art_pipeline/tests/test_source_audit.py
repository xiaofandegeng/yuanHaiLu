import unittest
from dataclasses import dataclass
from pathlib import Path
from tempfile import TemporaryDirectory

from PIL import Image

from tools.art_pipeline.source_audit import audit_character_sources


@dataclass(frozen=True)
class FakeAnimation:
    frames: int = 1


@dataclass(frozen=True)
class FakeRecipe:
    id: str
    modules: tuple
    frame_size: int = 32
    animations: tuple = (FakeAnimation(),)


class SourceAuditTests(unittest.TestCase):
    def test_character_source_audit_reports_missing_shared_wrong_sized_and_empty_modules(self):
        with TemporaryDirectory() as temporary:
            root = Path(temporary)
            shared_hair = root / "shared-hair.png"
            wrong_sized = root / "wrong-sized.png"
            empty = root / "empty.png"
            self._write_opaque_png(shared_hair, (32, 32))
            self._write_opaque_png(wrong_sized, (16, 16))
            Image.new("RGBA", (32, 32), (0, 0, 0, 0)).save(empty)

            hero_b = self._module_paths(root, "hero-b", hair=shared_hair)
            hero_c = self._module_paths(root, "hero-c", hair=shared_hair)
            hero_d = self._module_paths(root, "hero-d", weapon=wrong_sized, accessory=empty)
            recipes = (
                FakeRecipe("hero_a", tuple(root / "missing.png" for _ in range(6))),
                FakeRecipe("hero_b", hero_b),
                FakeRecipe("hero_c", hero_c),
                FakeRecipe("hero_d", hero_d),
            )

            errors = audit_character_sources(
                recipes, unique_layers={"hair", "outfit", "weapon", "accessory"}
            )

        self.assertTrue(any("hero_a" in error and "missing" in error for error in errors))
        self.assertTrue(any("hair source shared" in error for error in errors))
        self.assertTrue(any("hero_d weapon source must be 32x32" in error for error in errors))
        self.assertTrue(any("hero_d accessory source has no visible pixels" in error for error in errors))

    def _module_paths(self, root, character_id, **overrides):
        paths = []
        for layer in ("body", "face", "hair", "outfit", "weapon", "accessory"):
            path = overrides.get(layer, root / (character_id + "-" + layer + ".png"))
            if layer not in overrides:
                self._write_opaque_png(path, (32, 32))
            paths.append(path)
        return tuple(paths)

    @staticmethod
    def _write_opaque_png(path, size):
        Image.new("RGBA", size, (255, 255, 255, 255)).save(path)


if __name__ == "__main__":
    unittest.main()
