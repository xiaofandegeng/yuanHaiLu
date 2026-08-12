import unittest

from tools.art_pipeline.environment_roster import (
    INTERIOR_IDS,
    REGION_IDS,
    build_interior_recipes,
    build_region_recipes,
)


EXPECTED_LANDMARKS = {
    "tianshu": {"city_gate", "imperial_avenue", "academy"},
    "cangyue": {"mountain_temple", "cloud_bridge", "sword_platform"},
    "yanliu": {"inn", "arched_bridge", "pharmacy"},
    "chisha": {"fortress_gate", "beacon_tower", "caravan_inn"},
    "youhuang": {"bamboo_shrine", "poison_marsh_lab", "hidden_camp"},
    "hanyuan": {"hot_spring_inn", "ice_lake_tomb", "hunter_village"},
    "prologue_village": {"blacksmith", "ancestral_tree", "village_gate"},
    "luoyuan": {"east_city_gate", "canal_market", "escape_alley"},
    "jueyun": {"sword_sect_gate", "chain_bridge", "summit_platform"},
    "zhenyue": {"stele_forest", "ritual_altar", "mountain_garrison"},
}


class EnvironmentRosterTests(unittest.TestCase):
    def test_region_scope_is_exact(self):
        self.assertEqual({recipe.id for recipe in build_region_recipes()}, set(REGION_IDS))
        self.assertEqual(len(build_region_recipes()), 10)

    def test_every_region_meets_minimum_art_counts(self):
        for region in build_region_recipes():
            grounds = [module for module in region.modules if "/ground_" in module]
            decorations = [module for module in region.modules if "/decor_" in module]
            self.assertGreaterEqual(len(grounds), 8, region.id)
            self.assertGreaterEqual(len(decorations), 16, region.id)
            self.assertEqual(len(region.landmarks), 3, region.id)
            self.assertEqual(
                {landmark.id for landmark in region.landmarks},
                EXPECTED_LANDMARKS[region.id],
            )

    def test_interior_scope_is_exact_and_complete(self):
        recipes = build_interior_recipes()
        self.assertEqual({recipe.id for recipe in recipes}, set(INTERIOR_IDS))
        self.assertEqual(len(recipes), 13)
        for interior in recipes:
            self.assertGreaterEqual(
                len([module for module in interior.modules if "/floor_" in module]),
                4,
                interior.id,
            )
            self.assertGreaterEqual(
                len([module for module in interior.modules if "/wall_" in module]),
                4,
                interior.id,
            )
            self.assertGreaterEqual(
                len([module for module in interior.modules if "/prop_" in module]),
                8,
                interior.id,
            )


if __name__ == "__main__":
    unittest.main()
