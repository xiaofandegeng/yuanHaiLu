import unittest
from pathlib import Path

from PIL import Image

from tools.art_pipeline.environment_roster import INTERIOR_IDS, REGION_IDS
from tools.art_pipeline.environment_roster import build_interior_recipes, build_region_recipes
from tools.art_pipeline.environment_source_builder import (
    DESIGN_PATH,
    build_environment_sources,
    load_environment_designs,
)


class EnvironmentSourceBuilderTests(unittest.TestCase):
    def test_every_formal_environment_has_distinct_design_record_and_required_roles(self):
        designs = load_environment_designs(DESIGN_PATH)

        self.assertEqual(set(designs), set(REGION_IDS) | set(INTERIOR_IDS))
        regions = [designs[art_id] for art_id in REGION_IDS]
        self.assertEqual(len({design.geometry_key for design in regions}), len(REGION_IDS))
        self.assertEqual(len({design.palette for design in regions}), len(REGION_IDS))
        for design in regions:
            self.assertTrue(
                {"ground", "road", "wall", "roof", "decor"} <= set(design.tile_roles),
                design.id,
            )
            self.assertEqual(len(design.landmarks), 3, design.id)
            self.assertEqual(design.blocking_tile_roles, ("wall", "roof"))
        for art_id in INTERIOR_IDS:
            design = designs[art_id]
            self.assertTrue(
                {"floor", "wall", "prop", "entry", "exit"} <= set(design.tile_roles),
                art_id,
            )
            self.assertEqual(len(design.landmarks), 1, art_id)
            self.assertEqual(design.blocking_tile_roles, ("wall",))

    def test_source_builder_rewrites_every_declared_environment_module(self):
        written = build_environment_sources(load_environment_designs(DESIGN_PATH))
        self.assertEqual(written, 23)

    def test_every_landmark_has_a_unique_silhouette_mask(self):
        masks = {}
        for recipe in (*build_region_recipes(), *build_interior_recipes()):
            for landmark in recipe.landmarks:
                with Image.open(Path(landmark.module)) as source:
                    masks[(recipe.id, landmark.id)] = source.convert("RGBA").getchannel("A").tobytes()
        self.assertEqual(
            len(set(masks.values())),
            len(masks),
            "landmarks must not be identical silhouettes with only recolored pixels",
        )


if __name__ == "__main__":
    unittest.main()
