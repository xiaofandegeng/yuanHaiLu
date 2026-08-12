import json
import tempfile
import unittest
from pathlib import Path

from tools.art_pipeline.palette import PaletteError, load_palette, validate_palette
from tools.art_pipeline.schema import (
    CharacterManifest,
    CharacterRecipe,
    EnvironmentManifest,
    ManifestError,
    load_character_manifest,
    load_environment_manifest,
)


class SchemaTests(unittest.TestCase):
    def test_duplicate_character_ids_are_rejected(self):
        payload = {
            "schemaVersion": 1,
            "characters": [
                {"id": "player_male_swordsman", "frameSize": 32, "modules": ["body"]},
                {"id": "player_male_swordsman", "frameSize": 32, "modules": ["body"]},
            ],
        }

        with self.assertRaisesRegex(ManifestError, "duplicate character id"):
            CharacterManifest.from_dict(payload)

    def test_character_frame_size_must_be_32(self):
        with self.assertRaisesRegex(ManifestError, "frameSize must be 32"):
            CharacterRecipe.from_dict(
                {"id": "bad_actor", "frameSize": 48, "modules": ["body"]}
            )

    def test_character_id_must_be_stable_snake_case(self):
        with self.assertRaisesRegex(ManifestError, "invalid art id"):
            CharacterRecipe.from_dict(
                {"id": "Bad Actor", "frameSize": 32, "modules": ["body"]}
            )

    def test_character_requires_at_least_one_named_module(self):
        with self.assertRaisesRegex(ManifestError, "at least one module"):
            CharacterRecipe.from_dict(
                {"id": "bad_actor", "frameSize": 32, "modules": []}
            )

    def test_duplicate_animation_rows_are_rejected(self):
        with self.assertRaisesRegex(ManifestError, "duplicate animation row"):
            CharacterRecipe.from_dict(
                {
                    "id": "bad_actor",
                    "frameSize": 32,
                    "modules": ["body"],
                    "animations": [
                        {"name": "idle", "direction": "down", "frames": 4, "fps": 4, "loop": True},
                        {"name": "idle", "direction": "down", "frames": 4, "fps": 4, "loop": True},
                    ],
                }
            )

    def test_environment_tile_size_must_be_16(self):
        payload = {
            "schemaVersion": 1,
            "environments": [
                {"id": "yanliu", "tileSize": 32, "modules": ["grass"]}
            ],
        }

        with self.assertRaisesRegex(ManifestError, "tileSize must be 16"):
            EnvironmentManifest.from_dict(payload)

    def test_file_loaders_return_validated_manifests(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            character_path = root / "characters.json"
            environment_path = root / "environment.json"
            character_path.write_text(
                json.dumps(
                    {
                        "schemaVersion": 1,
                        "characters": [
                            {
                                "id": "player_male_swordsman",
                                "frameSize": 32,
                                "modules": ["body"],
                            }
                        ],
                    }
                ),
                encoding="utf-8",
            )
            environment_path.write_text(
                json.dumps(
                    {
                        "schemaVersion": 1,
                        "environments": [
                            {"id": "yanliu", "tileSize": 16, "modules": ["grass"]}
                        ],
                    }
                ),
                encoding="utf-8",
            )

            self.assertEqual(load_character_manifest(character_path).characters[0].id, "player_male_swordsman")
            self.assertEqual(load_environment_manifest(environment_path).environments[0].id, "yanliu")

    def test_reference_manifests_cover_both_swordsmen_and_yanliu(self):
        project_root = Path(__file__).resolve().parents[3]
        characters = load_character_manifest(
            project_root
            / "Assets"
            / "ArtSource"
            / "Characters"
            / "Manifests"
            / "reference-characters.json"
        )
        environments = load_environment_manifest(
            project_root
            / "Assets"
            / "ArtSource"
            / "Environment"
            / "Manifests"
            / "yanliu-reference.json"
        )

        self.assertEqual(
            {recipe.id for recipe in characters.characters},
            {"player_male_swordsman", "player_female_swordsman"},
        )
        self.assertEqual([recipe.id for recipe in environments.environments], ["yanliu"])
        self.assertEqual(
            {landmark.id for landmark in environments.environments[0].landmarks},
            {"inn", "pharmacy", "arched_bridge"},
        )

    def test_reference_swordsmen_have_all_four_directions_for_required_actions(self):
        project_root = Path(__file__).resolve().parents[3]
        manifest = load_character_manifest(
            project_root
            / "Assets"
            / "ArtSource"
            / "Characters"
            / "Manifests"
            / "reference-characters.json"
        )
        required_actions = {
            "idle", "walk", "dash", "attack_1", "attack_2", "attack_3",
            "skill_1", "skill_2", "hurt", "dodge", "down", "death",
        }
        directions = {"down", "left", "right", "up"}

        for recipe in manifest.characters:
            self.assertEqual({row.name for row in recipe.animations}, required_actions)
            for action in required_actions:
                self.assertEqual(
                    {row.direction for row in recipe.animations if row.name == action},
                    directions,
                )


class PaletteTests(unittest.TestCase):
    def test_palette_group_requires_four_colors(self):
        with self.assertRaisesRegex(PaletteError, "at least four colors"):
            validate_palette({"ink": [[1, 2, 3, 255]]})

    def test_palette_channels_stay_in_byte_range(self):
        with self.assertRaisesRegex(PaletteError, "0..255"):
            validate_palette(
                {"ink": [[0, 0, 0, 255], [1, 1, 1, 255], [2, 2, 2, 255], [300, 3, 3, 255]]}
            )

    def test_canonical_palette_contains_global_and_ten_region_groups(self):
        palette_path = (
            Path(__file__).resolve().parents[3]
            / "Assets"
            / "ArtSource"
            / "palettes"
            / "yuanhai-v1.json"
        )
        palette = load_palette(palette_path)
        required = {
            "ink", "paper", "cinnabar", "jade", "earth", "gold", "mystic",
            "tianshu", "cangyue", "yanliu", "chisha", "youhuang", "hanyuan",
            "prologue_village", "luoyuan", "jueyun", "zhenyue",
        }

        self.assertEqual(required - palette.keys(), set())


if __name__ == "__main__":
    unittest.main()
