#!/usr/bin/env python3
"""Create the three docs/16 composition thumbnails from the shipped MVP art.

These are intentionally no-UI review reductions of the same persistent 480×270
backdrops that Demo_YanLiuTown and Demo_Inn render.  They replace the former
schematic colour-block proposal: the review document now shows the composition
that will actually enter Unity.
"""

from pathlib import Path

from PIL import Image


SIZE = (160, 90)
ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets" / "Art" / "Environment" / "MVP"
OUTPUT = Path(__file__).resolve().parent


def _reduce(asset_name, crop=None):
    with Image.open(ART / asset_name) as source:
        image = source.convert("RGB")
        if crop is not None:
            image = image.crop(crop)
        return image.resize(SIZE, Image.Resampling.NEAREST)


def build_all():
    # The full town establishes the inn door → bridge → river route. The river
    # crop shifts visual weight to the boat, open combat clearing and bank.
    outputs = {
        "town-spawn-thumb.png": _reduce("mvp_yanliu_backdrop.png"),
        "town-riverbank-thumb.png": _reduce(
            "mvp_yanliu_backdrop.png", (160, 45, 480, 225)),
        "inn-counter-thumb.png": _reduce("mvp_inn_backdrop.png"),
    }
    for name, image in outputs.items():
        path = OUTPUT / name
        image.save(path, format="PNG", optimize=False, compress_level=9)
        print("wrote", path)


if __name__ == "__main__":
    build_all()
