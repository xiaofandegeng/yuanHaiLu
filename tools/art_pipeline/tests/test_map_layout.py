import unittest
from pathlib import Path

from tools.art_pipeline.environment_roster import INTERIOR_IDS, REGION_IDS
from tools.art_pipeline.map_layout import load_map_layout, reachable_anchor_ids


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


if __name__ == "__main__":
    unittest.main()
