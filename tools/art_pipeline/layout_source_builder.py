"""Materialise authored formal-map geometry into committed layout JSON files.

The editor consumes the resulting cells verbatim.  The helpers below exist
only to keep the source-production pass readable; no route, building or
collision geometry is invented by Unity at scene-generation time.
"""

import argparse
import json
from pathlib import Path

from .environment_roster import INTERIOR_IDS, REGION_IDS
from .map_layout import FORMAL_LAYER_NAMES


PROJECT_ROOT = Path(__file__).resolve().parents[2]
LAYOUT_ROOT = PROJECT_ROOT / "Assets" / "ArtSource" / "Environment" / "Layouts"


OUTDOOR_BLUEPRINTS = {
    "tianshu": {
        "roads": (((2, 2), (8, 2), (8, 8), (20, 8), (20, 21), (37, 21)),
                  ((20, 8), (31, 8), (31, 15))),
        "water": ((0, 16, 12, 4),),
        "houses": ((4, 13, 7, 3), (13, 14, 7, 3), (27, 11, 8, 3)),
        "decor": ((3, 5), (5, 9), (10, 11), (15, 5), (24, 5), (34, 6), (35, 16)),
        "foreground": (2, 19, 14, 19),
    },
    "cangyue": {
        "roads": (((2, 2), (7, 2), (7, 6), (13, 6), (13, 11), (19, 11), (19, 16), (28, 16), (28, 21), (37, 21)),),
        "water": ((23, 2, 5, 10),),
        "houses": ((3, 14, 7, 3), (12, 17, 7, 3), (29, 8, 7, 3)),
        "decor": ((4, 6), (9, 10), (11, 3), (16, 14), (21, 19), (31, 15), (35, 4)),
        "foreground": (24, 3, 38, 3),
    },
    "yanliu": {
        "roads": (((2, 2), (11, 2), (11, 7), (20, 7), (20, 13), (30, 13), (30, 21), (37, 21)),
                  ((20, 7), (20, 4))),
        "water": ((13, 0, 5, 18), (27, 7, 5, 17)),
        "houses": ((4, 12, 7, 3), (21, 16, 6, 3), (32, 12, 6, 3)),
        "decor": ((4, 4), (8, 8), (10, 17), (20, 17), (24, 8), (34, 5), (36, 18)),
        "foreground": (1, 18, 12, 18),
    },
    "chisha": {
        "roads": (((2, 2), (13, 2), (13, 6), (22, 6), (22, 11), (31, 11), (31, 21), (37, 21)),),
        "water": (),
        "houses": ((4, 12, 8, 3), (16, 14, 7, 3), (28, 7, 8, 3)),
        "decor": ((4, 5), (8, 18), (12, 9), (18, 4), (23, 17), (33, 17), (36, 4)),
        "foreground": (3, 20, 18, 20),
    },
    "youhuang": {
        "roads": (((2, 2), (7, 2), (7, 8), (15, 8), (15, 5), (24, 5), (24, 15), (32, 15), (32, 21), (37, 21)),),
        "water": ((10, 10, 6, 4), (25, 8, 4, 6)),
        "houses": ((3, 14, 6, 3), (18, 16, 6, 3), (30, 5, 7, 3)),
        "decor": ((4, 4), (5, 9), (12, 17), (19, 9), (23, 3), (29, 18), (35, 12)),
        "foreground": (9, 15, 18, 15),
    },
    "hanyuan": {
        "roads": (((2, 2), (2, 9), (9, 9), (9, 14), (17, 14), (17, 19), (28, 19), (28, 21), (37, 21)),
                  ((17, 14), (22, 14), (22, 6))),
        "water": ((10, 2, 9, 4), (30, 12, 6, 4)),
        "houses": ((4, 15, 7, 3), (20, 7, 8, 3), (30, 17, 7, 3)),
        "decor": ((4, 5), (7, 11), (14, 7), (18, 3), (24, 17), (34, 6), (35, 10)),
        "foreground": (20, 20, 38, 20),
    },
    "prologue_village": {
        "roads": (((2, 2), (8, 2), (8, 7), (16, 7), (16, 12), (25, 12), (25, 17), (37, 17), (37, 21)),
                  ((16, 12), (16, 4), (20, 4))),
        "water": ((3, 18, 9, 3),),
        "houses": ((3, 10, 7, 3), (13, 14, 7, 3), (27, 10, 8, 3)),
        "decor": ((4, 4), (7, 15), (11, 8), (20, 17), (23, 6), (33, 5), (36, 15)),
        "foreground": (2, 21, 17, 21),
    },
    "luoyuan": {
        "roads": (((2, 2), (11, 2), (11, 11), (19, 11), (19, 6), (29, 6), (29, 16), (37, 16), (37, 21)),),
        "water": ((0, 5, 8, 10), (21, 17, 14, 4)),
        "houses": ((4, 15, 6, 3), (14, 16, 6, 3), (30, 9, 7, 3)),
        "decor": ((3, 7), (6, 18), (13, 5), (17, 14), (24, 10), (32, 4), (35, 18)),
        "foreground": (20, 22, 38, 22),
    },
    "jueyun": {
        "roads": (((2, 2), (6, 2), (6, 7), (12, 7), (12, 12), (20, 12), (20, 17), (29, 17), (29, 21), (37, 21)),),
        "water": ((15, 3, 4, 5),),
        "houses": ((3, 13, 7, 3), (15, 15, 7, 3), (29, 7, 8, 3)),
        "decor": ((4, 5), (8, 10), (11, 18), (21, 4), (24, 16), (33, 14), (35, 4)),
        "foreground": (2, 20, 15, 20),
    },
    "zhenyue": {
        "roads": (((2, 2), (10, 2), (10, 6), (20, 6), (20, 15), (30, 15), (30, 21), (37, 21)),
                  ((20, 15), (20, 4))),
        "water": ((4, 12, 6, 3),),
        "houses": ((3, 16, 7, 3), (15, 8, 8, 3), (30, 11, 7, 3)),
        "decor": ((3, 5), (8, 9), (13, 15), (23, 9), (26, 18), (35, 5), (36, 16)),
        "foreground": (22, 19, 38, 19),
    },
}


INTERIOR_PROFILES = {
    "inn": ((5, 5), ((5, 7), (10, 7)), ((3, 8), (12, 8))),
    "residence": ((8, 5), ((5, 4), (6, 4), (11, 8)), ((3, 8), (12, 3))),
    "shop": ((8, 5), ((4, 7), (6, 7), (9, 7), (11, 7)), ((3, 9), (12, 8))),
    "pharmacy": ((8, 5), ((4, 4), (5, 4), (11, 4), (12, 4)), ((3, 8), (12, 8))),
    "academy": ((8, 6), ((4, 8), (5, 8), (10, 8), (11, 8)), ((3, 4), (12, 4))),
    "yamen": ((8, 6), ((4, 4), (5, 4), (10, 4), (11, 4)), ((3, 8), (12, 8))),
    "palace": ((8, 7), ((4, 5), (5, 5), (10, 5), (11, 5)), ((3, 9), (12, 9))),
    "temple": ((8, 6), ((4, 4), (5, 4), (10, 4), (11, 4)), ((3, 8), (12, 8))),
    "cave": ((8, 6), ((4, 7), (5, 7), (10, 4), (11, 4)), ((3, 4), (12, 8))),
    "tomb": ((8, 6), ((4, 4), (5, 4), (10, 8), (11, 8)), ((3, 8), (12, 4))),
    "dungeon": ((8, 6), ((4, 4), (4, 5), (11, 7), (11, 8)), ((3, 9), (12, 3))),
    "military_camp": ((8, 6), ((4, 8), (5, 8), (10, 8), (11, 8)), ((3, 4), (12, 4))),
    "ship_cabin": ((8, 6), ((4, 4), (5, 4), (10, 4), (11, 4)), ((3, 8), (12, 8))),
}


def _add_cell(layers, layer, x, y, token):
    if 0 <= x < 40 and 0 <= y < 24:
        layers[layer][(x, y)] = token


def _line(points):
    result = []
    for start, end in zip(points, points[1:]):
        x, y = start
        target_x, target_y = end
        step_x = 0 if target_x == x else (1 if target_x > x else -1)
        while x != target_x:
            result.append((x, y))
            x += step_x
        step_y = 0 if target_y == y else (1 if target_y > y else -1)
        while y != target_y:
            result.append((x, y))
            y += step_y
        result.append((x, y))
    return result


def _rect(x, y, width, height):
    return [(cell_x, cell_y) for cell_y in range(y, y + height) for cell_x in range(x, x + width)]


def _border(width, height):
    return sorted(
        {(x, 0) for x in range(width)}
        | {(x, height - 1) for x in range(width)}
        | {(0, y) for y in range(height)}
        | {(width - 1, y) for y in range(height)}
    )


def _border_runs(width, height):
    """Four explicit wall runs; Unity turns only these records into colliders."""
    return (
        {"x": 0, "y": 0, "width": width, "height": 1},
        {"x": 0, "y": height - 1, "width": width, "height": 1},
        {"x": 0, "y": 1, "width": 1, "height": height - 2},
        {"x": width - 1, "y": 1, "width": 1, "height": height - 2},
    )


def _serialise_layers(layers):
    return [
        {
            "name": name,
            "cells": [
                {"x": x, "y": y, "token": token}
                for (x, y), token in sorted(layers[name].items(), key=lambda item: (item[0][1], item[0][0]))
            ],
        }
        for name in FORMAL_LAYER_NAMES
    ]


def _outdoor_layout(payload, blueprint, index):
    width, height = payload["width"], payload["height"]
    layers = {name: {} for name in FORMAL_LAYER_NAMES}
    for x, y in _rect(0, 0, width, height):
        layers["Ground"][(x, y)] = "ground__{}".format((x * 3 + y * 5 + index) % 8)
    for route in blueprint["roads"]:
        for x, y in _line(route):
            layers["Ground"][(x, y)] = "road__{}".format((x + y + index) % 2)
    for water_x, water_y, water_width, water_height in blueprint["water"]:
        for x, y in _rect(water_x, water_y, water_width, water_height):
            layers["Water"][(x, y)] = "water__{}".format((x + y + index) % 2)
        for x in range(water_x - 1, water_x + water_width + 1):
            for y in (water_y - 1, water_y + water_height):
                _add_cell(layers, "Lower Environment", x, y, "shore__0")
        for y in range(water_y, water_y + water_height):
            _add_cell(layers, "Lower Environment", water_x - 1, y, "shore__0")
            _add_cell(layers, "Lower Environment", water_x + water_width, y, "shore__0")
    for house_index, (left, bottom, house_width, house_height) in enumerate(blueprint["houses"]):
        for x in range(left, left + house_width):
            layers["Buildings"][(x, bottom + house_height - 1)] = "roof__{}".format((x + house_index) % 2)
            layers["Buildings"][(x, bottom + house_height - 2)] = "wall__{}".format((x + house_index) % 2)
            layers["Buildings"][(x, bottom)] = "door__0" if x == left + house_width // 2 else "wall__{}".format((x + house_index + 1) % 2)
        layers["Buildings"][(left + 1, bottom)] = "window__0"
        layers["Buildings"][(left + house_width - 2, bottom)] = "window__0"
    for decor_index, (x, y) in enumerate(blueprint["decor"]):
        layers["Lower Environment"][(x, y)] = "decor__{}".format((decor_index + index) % 16)
        layers["Lower Environment"][(x + 1, y)] = "decor__{}".format((decor_index + index + 4) % 16)
    foreground_token = "decor__{}".format((index * 3) % 16)
    start_x, start_y, end_x, end_y = blueprint["foreground"]
    result = dict(payload)
    result["layers"] = _serialise_layers(layers)
    result["collisions"] = _border_runs(width, height)
    result["foregroundSpans"] = [{
        "fromX": start_x, "fromY": start_y, "toX": end_x, "toY": end_y, "token": foreground_token,
    }]
    return result


def _interior_layout(payload, profile, index):
    width, height = payload["width"], payload["height"]
    feature, props, lights = profile
    layers = {name: {} for name in FORMAL_LAYER_NAMES}
    for x, y in _rect(0, 0, width, height):
        layers["Ground"][(x, y)] = "floor__{}".format((x * 3 + y * 5 + index) % 4)
    for x, y in _border(width, height):
        layers["Buildings"][(x, y)] = "wall__{}".format((x + y + index) % 4)
    for anchor in payload["anchors"]:
        if anchor["type"] == "entry":
            layers["Buildings"][(anchor["x"], anchor["y"])] = "entry__0"
        elif anchor["type"] == "exit":
            layers["Buildings"][(anchor["x"], anchor["y"])] = "exit__0"
    for prop_index, (x, y) in enumerate(props):
        layers["Lower Environment"][(x, y)] = "prop__{}".format((prop_index + index) % 8)
    for x, y in lights:
        layers["Effects"][(x, y)] = "light__0"
    feature_x, feature_y = feature
    layers["Lower Environment"][(feature_x, feature_y)] = "prop__{}".format((index + 6) % 8)
    result = dict(payload)
    result["layers"] = _serialise_layers(layers)
    result["collisions"] = _border_runs(width, height)
    result["foregroundSpans"] = [{
        "fromX": 2, "fromY": 10, "toX": 13, "toY": 10, "token": "prop__{}".format((index + 2) % 8),
    }]
    return result


def _read_payload(path):
    return json.loads(Path(path).read_text(encoding="utf-8"))


def _write_payload(path, payload):
    Path(path).write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def build_all_layout_sources():
    """Rewrite all 23 layout sources with their complete authored geometry."""
    written = 0
    for index, region_id in enumerate(REGION_IDS):
        path = LAYOUT_ROOT / (region_id + ".json")
        _write_payload(path, _outdoor_layout(_read_payload(path), OUTDOOR_BLUEPRINTS[region_id], index))
        written += 1
    for index, interior_id in enumerate(INTERIOR_IDS):
        path = LAYOUT_ROOT / "interiors" / (interior_id + ".json")
        _write_payload(path, _interior_layout(_read_payload(path), INTERIOR_PROFILES[interior_id], index))
        written += 1
    return written


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--all", action="store_true", help="build all formal layout sources")
    args = parser.parse_args()
    if not args.all:
        raise SystemExit("pass --all to build formal layout sources")
    print("built={} layout sources".format(build_all_layout_sources()))


if __name__ == "__main__":
    main()
