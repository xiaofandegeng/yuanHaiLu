import unittest
from pathlib import Path

from tools.art_pipeline.environment_roster import INTERIOR_IDS, REGION_IDS
from tools.art_pipeline.map_layout import (
    FORMAL_LAYER_NAMES,
    load_map_layout,
    reachable_anchor_ids,
)


class MapLayoutTests(unittest.TestCase):
    def test_all_twenty_three_layouts_exist_and_required_anchors_are_reachable(self):
        project_root = Path(__file__).resolve().parents[3]
        layout_root = project_root / "Assets" / "ArtSource" / "Environment" / "Layouts"
        paths = [layout_root / (region + ".json") for region in REGION_IDS]
        paths += [layout_root / "interiors" / (interior + ".json") for interior in INTERIOR_IDS]
        self.assertEqual(len(paths), 23)

        for path in paths:
            layout = load_map_layout(path)
            reachable = reachable_anchor_ids(layout)
            required = {
                anchor.id
                for anchor in layout.anchors
                if anchor.type in {"entry", "exit", "interior"}
            } | set(layout.required_landmarks)
            self.assertEqual(required - reachable, set(), str(path))
            self.assertGreaterEqual(layout.width, 30 if layout.kind == "region" else 12)
            self.assertGreaterEqual(layout.height, 20 if layout.kind == "region" else 10)
            self.assertEqual(tuple(layout.layers), FORMAL_LAYER_NAMES)

    def test_outdoor_layouts_have_unique_authored_coordinate_geometry(self):
        project_root = Path(__file__).resolve().parents[3]
        layout_root = project_root / "Assets" / "ArtSource" / "Environment" / "Layouts"
        signatures = {
            region: load_map_layout(layout_root / (region + ".json")).coordinate_signature()
            for region in REGION_IDS
        }
        self.assertEqual(
            len(set(signatures.values())),
            len(REGION_IDS),
            "outdoor maps must differ by authored coordinates, not merely palette or IDs",
        )

    def test_outdoor_landmarks_use_unique_spatial_compositions(self):
        project_root = Path(__file__).resolve().parents[3]
        layout_root = project_root / "Assets" / "ArtSource" / "Environment" / "Layouts"
        compositions = {}
        for region in REGION_IDS:
            layout = load_map_layout(layout_root / (region + ".json"))
            compositions[region] = tuple(
                (anchor.x, anchor.y)
                for anchor in layout.anchors
                if anchor.id in layout.required_landmarks
            )
        self.assertEqual(
            len(set(compositions.values())),
            len(REGION_IDS),
            "landmarks must be staged differently in each outdoor region",
        )


if __name__ == "__main__":
    unittest.main()
