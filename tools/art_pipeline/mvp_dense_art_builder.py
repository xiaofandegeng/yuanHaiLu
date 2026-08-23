"""Author the original 48px male-hero source for the MVP vertical slice.

The output is deliberately source-scale pixel art: no resize, smoothing,
external assets, or image-to-image conversion is involved.  The six layers
remain compatible with the established character baker.
"""

import json
from pathlib import Path

from PIL import Image, ImageDraw


HERO_ID = "player_male_swordsman"
FRAME_SIZE = 48
MODULE_NAMES = ("body", "face", "hair", "outfit", "weapon", "accessory")
WEAPON_IDS = ("weapon_sword", "weapon_gauntlets", "weapon_dart")

P = {
    "clear": (0, 0, 0, 0),
    "ink": (24, 26, 42, 255),
    "ink_light": (48, 48, 67, 255),
    "skin_shadow": (134, 83, 63, 255),
    "skin": (213, 158, 120, 255),
    "skin_light": (246, 202, 158, 255),
    "hair": (35, 31, 42, 255),
    "hair_light": (76, 65, 78, 255),
    "cloak_dark": (28, 50, 101, 255),
    "cloak": (48, 82, 150, 255),
    "cloak_light": (82, 121, 188, 255),
    "paper_shadow": (180, 172, 164, 255),
    "paper": (235, 229, 210, 255),
    "paper_light": (255, 247, 224, 255),
    "vermilion_dark": (109, 39, 43, 255),
    "vermilion": (183, 57, 55, 255),
    "vermilion_light": (232, 94, 71, 255),
    "leather": (92, 60, 45, 255),
    "leather_light": (151, 105, 68, 255),
    "steel_dark": (71, 86, 105, 255),
    "steel": (160, 185, 204, 255),
    "steel_light": (228, 238, 239, 255),
    "shadow": (22, 25, 40, 110),
    "gold": (220, 168, 75, 255),
    "water_deep": (20, 57, 72, 255),
    "water": (35, 92, 105, 255),
    "water_light": (83, 149, 150, 255),
    "jade_dark": (37, 71, 64, 255),
    "jade": (67, 108, 78, 255),
    "stone": (104, 110, 104, 255),
    "stone_light": (159, 161, 145, 255),
    "wood_dark": (65, 43, 35, 255),
    "wood": (116, 76, 49, 255),
    "wood_light": (171, 122, 73, 255),
    "roof": (34, 48, 62, 255),
    "roof_light": (70, 91, 102, 255),
    "warm": (234, 172, 79, 255),
    "warm_light": (255, 228, 150, 255),
}

TOWN_ROLES = (
    "road", "water", "shore", "inn_roof", "inn_wall", "inn_door",
    "bridge", "boat", "bollard", "lantern", "foreground_foliage",
)


def _translated(points, ox, oy):
    return tuple((x + ox, y + oy) for x, y in points)


def _rect(draw, ox, oy, box, color):
    left, top, right, bottom = box
    draw.rectangle((ox + left, oy + top, ox + right, oy + bottom), fill=color)


def _line(draw, ox, oy, points, color, width=1):
    draw.line(_translated(points, ox, oy), fill=color, width=width)


def _polygon(draw, ox, oy, points, color):
    draw.polygon(_translated(points, ox, oy), fill=color)


def _motion(animation, frame_index, frame_count):
    if animation == "walk":
        stride = (-2, -1, 1, 2, 1, -1)[frame_index % 6]
        return stride, (0, 1, 0, -1, 0, 1)[frame_index % 6], 0
    if animation == "dash":
        return (0, 2, 3, 1)[frame_index % 4], (0, -1, 0, 0)[frame_index % 4], 1
    if animation.startswith("attack_") or animation.startswith("skill_"):
        progress = frame_index / max(1, frame_count - 1)
        lean = -2 if progress < 0.34 else 3 if progress < 0.72 else 1
        return lean, 0, 2
    return 0, (0, 1, 0, -1)[frame_index % 4], 0


def _direction_layout(direction):
    if direction == "down":
        return {"center": 24, "face": "front", "cloak": 0, "sword": 1, "ribbon": 1}
    if direction == "up":
        return {"center": 24, "face": "back", "cloak": 0, "sword": -1, "ribbon": -1}
    if direction == "left":
        return {"center": 22, "face": "left", "cloak": 1, "sword": 1, "ribbon": 1}
    if direction == "right":
        return {"center": 26, "face": "right", "cloak": -1, "sword": -1, "ribbon": -1}
    raise ValueError("unsupported direction: " + direction)


def _draw_frame(layers, ox, oy, direction, animation, frame_index, frame_count):
    draws = {name: ImageDraw.Draw(image) for name, image in layers.items()}
    layout = _direction_layout(direction)
    stride, bob, attack = _motion(animation, frame_index, frame_count)
    cx = layout["center"] + (attack if direction == "right" else -attack if direction == "left" else 0)
    cy = bob
    body = draws["body"]
    face = draws["face"]
    hair = draws["hair"]
    outfit = draws["outfit"]
    weapon = draws["weapon"]
    accessory = draws["accessory"]

    # The foot shadow fixes the visual/collision anchor at the bottom pivot.
    _polygon(accessory, ox, oy, ((cx - 11, 43), (cx + 11, 43), (cx + 8, 46),
                                  (cx - 8, 46)), P["shadow"])

    left_step = -stride // 2
    right_step = stride // 2
    _rect(body, ox, oy, (cx - 10 + left_step, 31 + cy, cx - 3 + left_step, 42), P["ink"])
    _rect(body, ox, oy, (cx + 3 + right_step, 31 + cy, cx + 10 + right_step, 42), P["ink"])
    _rect(body, ox, oy, (cx - 8 + left_step, 33 + cy, cx - 4 + left_step, 39), P["paper_shadow"])
    _rect(body, ox, oy, (cx + 4 + right_step, 33 + cy, cx + 8 + right_step, 39), P["paper_shadow"])
    _rect(body, ox, oy, (cx - 11 + left_step, 40, cx - 2 + left_step, 44), P["ink"])
    _rect(body, ox, oy, (cx + 2 + right_step, 40, cx + 11 + right_step, 44), P["ink"])
    _rect(body, ox, oy, (cx - 9 + left_step, 40, cx - 3 + left_step, 42), P["leather"])
    _rect(body, ox, oy, (cx + 3 + right_step, 40, cx + 9 + right_step, 42), P["leather"])

    # The robe is a large silhouette before its small material highlights.
    cloak_shift = layout["cloak"] * (3 + attack)
    _polygon(outfit, ox, oy, ((cx - 11, 18 + cy), (cx + 10, 18 + cy),
                               (cx + 15 + cloak_shift, 33), (cx + 8, 37),
                               (cx, 34), (cx - 10, 37), (cx - 15 + cloak_shift, 31)), P["ink"])
    _polygon(outfit, ox, oy, ((cx - 9, 19 + cy), (cx + 8, 19 + cy),
                               (cx + 12 + cloak_shift, 31), (cx + 6, 34),
                               (cx, 31), (cx - 8, 34), (cx - 12 + cloak_shift, 29)), P["cloak_dark"])
    _polygon(outfit, ox, oy, ((cx - 7, 20 + cy), (cx + 6, 20 + cy),
                               (cx + 8 + cloak_shift, 29), (cx + 3, 31),
                               (cx - 5, 29), (cx - 9 + cloak_shift, 28)), P["cloak"])
    _line(outfit, ox, oy, ((cx - 7, 22 + cy), (cx - 3, 26), (cx - 1, 30)), P["cloak_light"], 2)
    _polygon(outfit, ox, oy, ((cx - 6, 22 + cy), (cx + 6, 22 + cy),
                               (cx + 9, 34), (cx, 36), (cx - 9, 34)), P["paper_shadow"])
    _polygon(outfit, ox, oy, ((cx - 4, 22 + cy), (cx + 4, 22 + cy),
                               (cx + 5, 33), (cx, 34), (cx - 5, 33)), P["paper"])
    _line(outfit, ox, oy, ((cx, 23 + cy), (cx, 33)), P["paper_light"], 1)

    # Sleeves and hands are deliberately asymmetric in side views.
    arm_shift = attack * (1 if direction in ("right", "down") else -1)
    _rect(outfit, ox, oy, (cx - 14, 22 + cy, cx - 8, 30 + cy), P["ink"])
    _rect(outfit, ox, oy, (cx + 8, 22 + cy, cx + 14, 30 + cy), P["ink"])
    _rect(outfit, ox, oy, (cx - 12, 23 + cy, cx - 8, 28 + cy), P["cloak"])
    _rect(outfit, ox, oy, (cx + 8, 23 + cy, cx + 12, 28 + cy), P["cloak"])
    _rect(body, ox, oy, (cx - 13, 29 + cy, cx - 9, 32 + cy), P["skin_shadow"])
    _rect(body, ox, oy, (cx + 9 + arm_shift, 29 + cy, cx + 13 + arm_shift, 32 + cy), P["skin"])

    # Head/face/hair use separate source modules so the established baker remains valid.
    _rect(face, ox, oy, (cx - 7, 7 + cy, cx + 7, 20 + cy), P["ink"])
    _rect(face, ox, oy, (cx - 5, 8 + cy, cx + 5, 18 + cy), P["skin"])
    _rect(face, ox, oy, (cx - 4, 9 + cy, cx + 4, 13 + cy), P["skin_light"])
    if layout["face"] == "front":
        _rect(face, ox, oy, (cx - 3, 14 + cy, cx - 2, 15 + cy), P["ink"])
        _rect(face, ox, oy, (cx + 2, 14 + cy, cx + 3, 15 + cy), P["ink"])
    elif layout["face"] == "left":
        _rect(face, ox, oy, (cx - 5, 14 + cy, cx - 4, 15 + cy), P["ink"])
    elif layout["face"] == "right":
        _rect(face, ox, oy, (cx + 4, 14 + cy, cx + 5, 15 + cy), P["ink"])

    _polygon(hair, ox, oy, ((cx - 8, 8 + cy), (cx - 5, 3 + cy), (cx + 4, 3 + cy),
                             (cx + 8, 9 + cy), (cx + 5, 13 + cy), (cx - 6, 12 + cy)), P["ink"])
    _polygon(hair, ox, oy, ((cx - 5, 7 + cy), (cx - 2, 4 + cy), (cx + 4, 5 + cy),
                             (cx + 6, 10 + cy), (cx + 1, 8 + cy), (cx - 4, 10 + cy)), P["hair"])
    _rect(hair, ox, oy, (cx - 3, 0 + cy, cx + 4, 5 + cy), P["ink"])
    _rect(hair, ox, oy, (cx - 1, 0 + cy, cx + 2, 3 + cy), P["hair_light"])
    if layout["face"] == "back":
        _rect(hair, ox, oy, (cx - 6, 10 + cy, cx + 6, 18 + cy), P["hair"])
    ribbon = layout["ribbon"]
    _rect(accessory, ox, oy, (cx - 2, 4 + cy, cx + 3, 6 + cy), P["vermilion"])
    _polygon(accessory, ox, oy, ((cx + ribbon * 2, 5 + cy), (cx + ribbon * 10, 8 + cy),
                                 (cx + ribbon * 6, 11 + cy)), P["vermilion"])
    _line(accessory, ox, oy, ((cx + ribbon * 3, 7 + cy), (cx + ribbon * 9, 10 + cy)), P["vermilion_light"], 1)

    # Waist sash, pouch and scabbard keep the protagonist readable when idle.
    _rect(accessory, ox, oy, (cx - 9, 27 + cy, cx + 9, 30 + cy), P["vermilion_dark"])
    _rect(accessory, ox, oy, (cx - 6, 27 + cy, cx + 7, 28 + cy), P["vermilion"])
    _polygon(accessory, ox, oy, ((cx + 5, 29 + cy), (cx + 11, 34), (cx + 8, 36),
                                 (cx + 3, 30 + cy)), P["vermilion"])
    scabbard = layout["sword"]
    _line(weapon, ox, oy, ((cx + scabbard * 9, 16 + cy), (cx + scabbard * 15, 36)), P["ink"], 4)
    _line(weapon, ox, oy, ((cx + scabbard * 8, 16 + cy), (cx + scabbard * 14, 35)), P["leather"], 2)
    _rect(weapon, ox, oy, (cx + scabbard * 6 - 1, 14 + cy, cx + scabbard * 6 + 2, 19 + cy), P["gold"])


def _new_sheet(size):
    return {name: Image.new("RGBA", size, P["clear"]) for name in MODULE_NAMES}


def _write_if_changed(image, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        with Image.open(path) as existing:
            if existing.convert("RGBA").size == image.size and existing.convert("RGBA").tobytes() == image.tobytes():
                return False
    image.save(path, format="PNG", optimize=False, compress_level=9)
    return True


def _write_text_if_changed(path, text):
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists() and path.read_text(encoding="utf-8") == text:
        return False
    path.write_text(text, encoding="utf-8")
    return True


def _draw_weapon_layer(weapon_id):
    image = Image.new("RGBA", (FRAME_SIZE, FRAME_SIZE), P["clear"])
    draw = ImageDraw.Draw(image)
    if weapon_id == "weapon_sword":
        draw.line(((19, 34), (35, 11)), fill=P["ink"], width=5)
        draw.line(((19, 34), (35, 11)), fill=P["steel_dark"], width=3)
        draw.line(((20, 32), (34, 12)), fill=P["steel_light"], width=1)
        draw.rectangle((16, 31, 23, 34), fill=P["gold"])
        draw.rectangle((17, 35, 21, 42), fill=P["leather"])
    elif weapon_id == "weapon_gauntlets":
        draw.rectangle((19, 23, 32, 35), fill=P["ink"])
        draw.rectangle((21, 24, 30, 33), fill=P["leather"])
        draw.rectangle((22, 23, 30, 26), fill=P["leather_light"])
        for x in (21, 24, 27, 30):
            draw.rectangle((x, 19, x + 2, 25), fill=P["steel"])
    elif weapon_id == "weapon_dart":
        draw.rectangle((17, 26, 28, 37), fill=P["ink"])
        draw.rectangle((19, 28, 26, 35), fill=P["leather"])
        for index, y in enumerate((17, 21, 25)):
            draw.polygon(((27, y), (40, y + 3), (27, y + 6)), fill=P["steel_dark"])
            draw.line(((28, y + 3), (38, y + 3)), fill=P["steel_light"], width=1)
    else:
        raise ValueError("unknown weapon ID: " + weapon_id)
    return image


def _new_module(size, opaque=False):
    return Image.new("RGBA", (size, size), P["jade_dark"] if opaque else P["clear"])


def _draw_town_module(name):
    """Draw one authored, composable town module at native source scale."""
    if name in ("road_a", "road_b", "road_turn", "water_deep", "water_flow",
                "water_reflection", "shore_grass", "shore_stone", "bollard", "lantern",
                "crate"):
        image = _new_module(16, opaque=name.startswith(("road", "water", "shore")))
    elif name in ("inn_door", "inn_sign", "roof_trim"):
        image = _new_module(32)
    elif name in ("bridge", "boat", "willow_far"):
        image = _new_module(48)
    elif name in ("inn_roof", "inn_wall", "willow_near"):
        image = _new_module(64)
    else:
        raise ValueError("unknown dense town module: " + name)

    draw = ImageDraw.Draw(image)
    size = image.width
    if name.startswith("road"):
        draw.rectangle((0, 0, 15, 15), fill=P["ink"])
        draw.rectangle((1, 1, 14, 14), fill=P["stone"])
        if name == "road_a":
            draw.line((1, 5, 14, 4), fill=P["stone_light"])
            draw.line((3, 12, 12, 13), fill=P["ink_light"])
        elif name == "road_b":
            draw.line((5, 1, 4, 14), fill=P["stone_light"])
            draw.line((12, 3, 13, 12), fill=P["ink_light"])
        else:
            draw.arc((-6, -6, 22, 22), 0, 90, fill=P["stone_light"], width=2)
        return image
    if name.startswith("water"):
        base = P["water_deep"] if name == "water_deep" else P["water"]
        draw.rectangle((0, 0, 15, 15), fill=base)
        if name == "water_flow":
            draw.line((1, 5, 9, 4, 14, 6), fill=P["water_light"])
            draw.line((0, 12, 6, 11), fill=P["water_light"])
        elif name == "water_reflection":
            draw.line((3, 2, 5, 6, 2, 10), fill=P["roof_light"], width=1)
            draw.line((11, 7, 14, 11), fill=P["warm"], width=1)
        else:
            draw.line((2, 9, 10, 9), fill=P["water_light"])
        return image
    if name.startswith("shore"):
        draw.rectangle((0, 0, 15, 15), fill=P["jade_dark"])
        draw.polygon(((0, 10), (5, 8), (9, 9), (15, 6), (15, 15), (0, 15)), fill=P["ink"])
        draw.polygon(((0, 11), (5, 9), (10, 10), (15, 7), (15, 15), (0, 15)),
                     fill=P["stone"] if name == "shore_stone" else P["water"])
        draw.line((0, 11, 5, 9, 10, 10, 15, 7), fill=P["stone_light"], width=1)
        return image
    if name == "inn_roof":
        draw.polygon(((2, 38), (16, 10), (47, 4), (62, 36), (56, 47), (7, 47)), fill=P["ink"])
        draw.polygon(((7, 36), (18, 13), (46, 8), (57, 35), (53, 41), (10, 41)), fill=P["roof"])
        for y in range(16, 40, 7):
            draw.line((12, y, 54, y - 6), fill=P["roof_light"], width=2)
        draw.line((5, 42, 59, 42), fill=P["ink_light"], width=2)
        return image
    if name == "inn_wall":
        draw.rectangle((3, 14, 60, 62), fill=P["ink"])
        draw.rectangle((7, 18, 56, 59), fill=P["paper_shadow"])
        draw.rectangle((11, 21, 52, 57), fill=P["paper"])
        for x in (12, 31, 50):
            draw.rectangle((x, 18, x + 3, 59), fill=P["wood_dark"])
        draw.rectangle((18, 28, 27, 38), fill=P["ink"])
        draw.rectangle((20, 30, 25, 36), fill=P["warm"])
        draw.rectangle((38, 28, 47, 38), fill=P["ink"])
        draw.rectangle((40, 30, 45, 36), fill=P["warm"])
        draw.line((8, 59, 55, 59), fill=P["wood_light"], width=2)
        return image
    if name == "inn_door":
        draw.rectangle((3, 2, 28, 31), fill=P["ink"])
        draw.rectangle((6, 5, 25, 31), fill=P["wood"])
        draw.line((15, 5, 15, 31), fill=P["wood_light"], width=2)
        draw.rectangle((18, 18, 20, 20), fill=P["gold"])
        draw.rectangle((0, 28, 31, 31), fill=P["warm"])
        return image
    if name == "inn_sign":
        draw.rectangle((2, 4, 29, 27), fill=P["ink"])
        draw.rectangle((5, 6, 26, 24), fill=P["wood"])
        draw.rectangle((8, 10, 23, 12), fill=P["paper"])
        draw.rectangle((11, 15, 20, 19), fill=P["vermilion"])
        draw.line((1, 2, 29, 2), fill=P["wood_light"], width=2)
        return image
    if name == "bridge":
        draw.polygon(((0, 27), (7, 13), (40, 13), (47, 27), (42, 38), (5, 38)), fill=P["ink"])
        draw.polygon(((4, 27), (10, 17), (37, 17), (43, 27), (39, 33), (8, 33)), fill=P["stone"])
        for x in (9, 18, 29, 39):
            draw.line((x, 14, x - 2, 35), fill=P["stone_light"], width=2)
        draw.line((5, 18, 42, 18), fill=P["paper"], width=1)
        return image
    if name == "boat":
        draw.polygon(((3, 30), (43, 30), (37, 39), (10, 39)), fill=P["ink"])
        draw.polygon(((7, 32), (39, 32), (34, 36), (12, 36)), fill=P["wood"])
        draw.line((24, 31, 24, 8), fill=P["wood_light"], width=2)
        draw.polygon(((26, 10), (41, 20), (26, 25)), fill=P["paper_shadow"])
        draw.line((26, 12, 38, 20), fill=P["paper_light"], width=1)
        return image
    if name == "bollard":
        draw.rectangle((5, 4, 10, 15), fill=P["ink"])
        draw.rectangle((6, 5, 9, 14), fill=P["wood"])
        draw.rectangle((4, 2, 11, 6), fill=P["wood_light"])
        return image
    if name == "lantern":
        draw.rectangle((7, 0, 8, 15), fill=P["wood_dark"])
        draw.rectangle((3, 5, 12, 13), fill=P["ink"])
        draw.rectangle((5, 6, 10, 11), fill=P["warm"])
        draw.rectangle((6, 7, 9, 10), fill=P["warm_light"])
        return image
    if name == "crate":
        draw.rectangle((1, 2, 14, 15), fill=P["ink"])
        draw.rectangle((3, 4, 12, 14), fill=P["wood"])
        draw.line((3, 5, 12, 13), fill=P["wood_light"], width=1)
        draw.line((12, 5, 3, 13), fill=P["wood_light"], width=1)
        return image
    if name in ("willow_near", "willow_far"):
        draw.line((size // 2, 0, size // 2 - 8, size - 5), fill=P["ink"], width=5)
        draw.line((size // 2, 0, size // 2 - 8, size - 5), fill=P["wood_dark"], width=3)
        leaf_color = P["jade_dark"] if name == "willow_near" else P["jade"]
        for index, y in enumerate(range(7, size - 6, 9)):
            offset = 10 + (index % 3) * 5
            draw.polygon(((size // 2 - 4, y), (size // 2 - offset, y + 7),
                          (size // 2 - 6, y + 12)), fill=leaf_color)
            draw.polygon(((size // 2 + 1, y + 3), (size // 2 + offset, y + 8),
                          (size // 2 + 3, y + 14)), fill=P["jade"])
        return image
    if name == "roof_trim":
        draw.polygon(((0, 0), (31, 0), (31, 11), (20, 8), (11, 13), (0, 9)), fill=P["ink"])
        draw.polygon(((2, 2), (29, 2), (29, 8), (20, 6), (11, 10), (2, 7)), fill=P["roof"])
        return image
    raise AssertionError("unreachable module: " + name)


def _draw_dense_actor(actor_id):
    size = 16 if actor_id == "mvp_lost_pouch" else 48
    image = Image.new("RGBA", (size, size), P["clear"])
    draw = ImageDraw.Draw(image)
    if actor_id == "mvp_lost_pouch":
        draw.rectangle((3, 7, 12, 15), fill=P["ink"])
        draw.rectangle((4, 8, 11, 14), fill=P["wood"])
        draw.rectangle((5, 9, 10, 13), fill=P["wood_light"])
        draw.line((3, 7, 12, 7), fill=P["vermilion"], width=2)
        draw.rectangle((7, 3, 8, 8), fill=P["paper"])
        return image
    if actor_id == "mvp_innkeeper":
        draw.ellipse((9, 40, 39, 47), fill=P["shadow"])
        draw.rectangle((13, 29, 21, 42), fill=P["ink"])
        draw.rectangle((28, 29, 36, 42), fill=P["ink"])
        draw.rectangle((8, 20, 40, 35), fill=P["ink"])
        draw.rectangle((11, 21, 37, 34), fill=P["wood"])
        draw.rectangle((13, 22, 19, 32), fill=P["wood_light"])
        draw.rectangle((28, 23, 35, 33), fill=P["paper_shadow"])
        draw.rectangle((13, 8, 36, 22), fill=P["ink"])
        draw.rectangle((16, 10, 33, 20), fill=P["skin"])
        draw.rectangle((11, 5, 38, 12), fill=P["ink"])
        draw.rectangle((14, 6, 35, 9), fill=P["roof_light"])
        draw.rectangle((18, 16, 20, 17), fill=P["ink"])
        draw.rectangle((29, 16, 31, 17), fill=P["ink"])
        draw.rectangle((23, 25, 26, 27), fill=P["gold"])
        return image
    if actor_id not in ("mvp_bandit_a", "mvp_bandit_b"):
        raise ValueError("unknown dense actor: " + actor_id)
    accent = P["vermilion"] if actor_id == "mvp_bandit_a" else P["jade"]
    draw.ellipse((10, 40, 38, 47), fill=P["shadow"])
    draw.rectangle((12, 30, 21, 42), fill=P["ink"])
    draw.rectangle((27, 30, 36, 42), fill=P["ink"])
    draw.rectangle((9, 20, 39, 35), fill=P["ink"])
    draw.polygon(((12, 21), (35, 21), (40, 33), (25, 39), (8, 33)), fill=accent)
    draw.rectangle((16, 22, 33, 31), fill=P["vermilion_dark"] if actor_id == "mvp_bandit_a" else P["cloak_dark"])
    draw.rectangle((14, 8, 35, 22), fill=P["ink"])
    draw.rectangle((17, 10, 32, 20), fill=P["skin"])
    draw.rectangle((12, 5, 37, 12), fill=P["ink"])
    draw.rectangle((15, 6, 34, 9), fill=accent)
    draw.rectangle((18, 16, 20, 17), fill=P["ink"])
    draw.rectangle((29, 16, 31, 17), fill=P["ink"])
    draw.line((35, 27, 46, 15), fill=P["ink"], width=3)
    draw.line((36, 26, 45, 16), fill=P["steel_light"], width=1)
    return image


def _town_layout():
    """World coordinates retain the existing spawn, door, riverbank and pouch contract."""
    placements = []

    def add(asset, x, y, layer, order, role):
        placements.append({
            "asset": "town/" + asset + ".png",
            "x": x,
            "y": y,
            "layer": layer,
            "sortingOrder": order,
            "role": role,
        })

    for x, y, asset in (
            (7.5, 7.6, "road_a"), (8.5, 7.2, "road_turn"), (9.5, 6.7, "road_a"),
            (10.5, 6.2, "road_b"), (11.5, 5.7, "road_a"), (12.5, 5.2, "road_b"),
            (13.5, 4.7, "road_a"), (14.5, 4.2, "road_b"), (15.5, 3.7, "road_a"),
            (16.5, 3.2, "road_b"), (17.5, 3.0, "road_a")):
        add(asset, x, y, "Ground", -100, "road")
    for x, y, asset in (
            (19, 2, "water_deep"), (20, 2, "water_flow"), (21, 2, "water_reflection"),
            (22, 2, "water_deep"), (19, 4, "water_flow"), (20, 4, "water_deep"),
            (21, 4, "water_reflection"), (22, 4, "water_flow"), (23, 4, "water_deep"),
            (20, 6, "water_flow"), (21, 6, "water_deep"), (22, 6, "water_reflection")):
        add(asset, x, y, "Ground", -100, "water")
    for x, y, asset in ((18, 2, "shore_stone"), (18, 4, "shore_grass"),
                         (18, 6, "shore_stone"), (23, 3, "shore_grass")):
        add(asset, x, y, "Environment", -2, "shore")
    add("inn_roof", 7.5, 14.0, "Environment", 2, "inn_roof")
    add("inn_wall", 7.5, 11.35, "Environment", 3, "inn_wall")
    add("inn_door", 7.5, 9.95, "Environment", 5, "inn_door")
    add("inn_sign", 4.95, 11.45, "Environment", 6, "inn_wall")
    add("bridge", 18.5, 7.2, "Environment", 4, "bridge")
    add("boat", 23.7, 3.9, "Environment", 3, "boat")
    for x, y in ((19.0, 4.3), (20.5, 3.8), (22.4, 4.3)):
        add("bollard", x, y, "Environment", 5, "bollard")
    for x, y in ((6.0, 9.5), (12.0, 5.9), (18.0, 6.8)):
        add("lantern", x, y, "Environment", 8, "lantern")
    add("crate", 13.2, 4.4, "Environment", 3, "shore")
    add("willow_far", 25.0, 10.6, "Environment", 1, "foreground_foliage")
    add("willow_near", 28.0, 12.0, "Foreground", 4, "foreground_foliage")
    add("roof_trim", 1.0, 16.0, "Foreground", 3, "foreground_foliage")
    return {"schemaVersion": 1, "scene": "town", "roles": list(TOWN_ROLES), "placements": placements}


def _build_dense_town(project_root):
    source_root = project_root / "Assets/ArtSource/MVP/dense_pixel"
    output_root = project_root / "Assets/Art/MVP/dense_pixel"
    module_names = (
        "road_a", "road_b", "road_turn", "water_deep", "water_flow", "water_reflection",
        "shore_grass", "shore_stone", "inn_roof", "inn_wall", "inn_door", "inn_sign",
        "bridge", "boat", "bollard", "lantern", "crate", "willow_far", "willow_near", "roof_trim",
    )
    written = []
    changed = 0
    for name in module_names:
        image = _draw_town_module(name)
        relative = Path("environment/town") / (name + ".png")
        source_path = source_root / relative
        output_path = output_root / relative
        changed += int(_write_if_changed(image, source_path))
        changed += int(_write_if_changed(image, output_path))
        written.extend((source_path, output_path))

    layout_path = source_root / "layouts/town.json"
    layout_text = json.dumps(_town_layout(), ensure_ascii=False, indent=2) + "\n"
    changed += int(_write_text_if_changed(layout_path, layout_text))
    written.append(layout_path)

    for actor_id in ("mvp_bandit_a", "mvp_bandit_b", "mvp_lost_pouch"):
        image = _draw_dense_actor(actor_id)
        source_path = source_root / "actors" / (actor_id + ".png")
        resource_path = project_root / "Assets/Resources/Art/MVP/dense_pixel/actors" / (actor_id + ".png")
        changed += int(_write_if_changed(image, source_path))
        changed += int(_write_if_changed(image, resource_path))
        written.extend((source_path, resource_path))
    return tuple(written), changed


def _draw_inn_module(name):
    """Draw interior modules with local warm pools and a clear central aisle."""
    if name in ("floor_wood_a", "floor_wood_b", "entry_stone", "rug"):
        image = _new_module(16, opaque=True)
    elif name in ("window_light", "north_door", "counter_lantern", "shelf"):
        image = _new_module(32)
    elif name in ("table", "stove"):
        image = _new_module(48)
    elif name in ("counter", "stairs", "kitchen_wall", "foreground_beam"):
        image = _new_module(64)
    else:
        raise ValueError("unknown dense inn module: " + name)
    draw = ImageDraw.Draw(image)
    size = image.width
    if name.startswith("floor_wood"):
        draw.rectangle((0, 0, 15, 15), fill=P["wood_dark"])
        draw.rectangle((1, 1, 14, 14), fill=P["wood"])
        y = 5 if name == "floor_wood_a" else 10
        draw.line((2, y, 13, y), fill=P["wood_light"], width=1)
        draw.line((8, 1, 8, 14), fill=P["ink_light"], width=1)
        return image
    if name == "entry_stone":
        draw.rectangle((0, 0, 15, 15), fill=P["ink"])
        draw.rectangle((1, 1, 14, 14), fill=P["stone"])
        draw.line((1, 8, 14, 8), fill=P["stone_light"])
        draw.line((8, 1, 8, 14), fill=P["ink_light"])
        return image
    if name == "rug":
        draw.rectangle((0, 0, 15, 15), fill=P["vermilion_dark"])
        draw.rectangle((2, 2, 13, 13), fill=P["vermilion"])
        draw.rectangle((5, 5, 10, 10), fill=P["gold"])
        return image
    if name == "counter":
        draw.rectangle((2, 20, 61, 57), fill=P["ink"])
        draw.rectangle((5, 23, 58, 39), fill=P["wood_light"])
        draw.rectangle((5, 40, 58, 54), fill=P["wood"])
        for x in range(10, 57, 11):
            draw.line((x, 25, x, 53), fill=P["wood_dark"], width=2)
        draw.line((5, 23, 58, 23), fill=P["paper_light"], width=1)
        draw.rectangle((20, 13, 44, 22), fill=P["ink"])
        draw.rectangle((22, 15, 42, 20), fill=P["paper_shadow"])
        return image
    if name == "counter_lantern":
        draw.line((16, 0, 16, 8), fill=P["wood_dark"], width=2)
        draw.rectangle((8, 8, 24, 25), fill=P["ink"])
        draw.rectangle((10, 10, 22, 23), fill=P["vermilion_dark"])
        draw.rectangle((12, 12, 20, 21), fill=P["warm"])
        draw.rectangle((14, 14, 18, 19), fill=P["warm_light"])
        return image
    if name == "table":
        draw.ellipse((3, 11, 45, 35), fill=P["ink"])
        draw.ellipse((7, 14, 41, 31), fill=P["wood"])
        draw.line((11, 18, 37, 18), fill=P["wood_light"], width=2)
        for x in (12, 35):
            draw.rectangle((x, 31, x + 5, 43), fill=P["wood_dark"])
        draw.rectangle((17, 5, 28, 12), fill=P["paper_shadow"])
        return image
    if name == "stove":
        draw.rectangle((4, 5, 43, 43), fill=P["ink"])
        draw.rectangle((7, 8, 40, 40), fill=P["stone"])
        draw.rectangle((13, 20, 34, 40), fill=P["ink"])
        draw.polygon(((19, 35), (24, 18), (30, 35)), fill=P["vermilion"])
        draw.polygon(((22, 34), (25, 23), (28, 34)), fill=P["warm"])
        draw.rectangle((10, 10, 17, 13), fill=P["wood_dark"])
        return image
    if name == "stairs":
        draw.rectangle((3, 5, 61, 61), fill=P["ink"])
        for index in range(7):
            y = 10 + index * 7
            draw.rectangle((7 + index * 3, y, 56, y + 6), fill=P["wood"])
            draw.line((7 + index * 3, y, 56, y), fill=P["wood_light"])
        return image
    if name == "kitchen_wall":
        draw.rectangle((2, 2, 61, 61), fill=P["ink"])
        draw.rectangle((6, 6, 57, 57), fill=P["wood_dark"])
        for y in (14, 28, 42):
            draw.line((8, y, 55, y), fill=P["wood"])
        draw.rectangle((10, 10, 22, 23), fill=P["paper_shadow"])
        draw.rectangle((42, 10, 53, 21), fill=P["paper_shadow"])
        return image
    if name == "window_light":
        draw.rectangle((2, 2, 29, 29), fill=P["ink"])
        draw.rectangle((5, 5, 26, 26), fill=P["warm"])
        draw.rectangle((7, 7, 24, 24), fill=P["warm_light"])
        draw.line((15, 5, 15, 26), fill=P["wood_dark"], width=2)
        draw.line((5, 15, 26, 15), fill=P["wood_dark"], width=2)
        return image
    if name == "north_door":
        draw.rectangle((3, 1, 28, 31), fill=P["ink"])
        draw.rectangle((6, 4, 25, 31), fill=P["wood"])
        draw.line((15, 5, 15, 31), fill=P["wood_light"])
        draw.rectangle((18, 18, 20, 20), fill=P["gold"])
        return image
    if name == "shelf":
        draw.rectangle((3, 3, 28, 30), fill=P["ink"])
        for y in (7, 15, 23):
            draw.line((5, y, 26, y), fill=P["wood_light"], width=2)
        for x, y, color in ((8, 8, P["vermilion"]), (17, 8, P["jade"]),
                            (11, 16, P["paper"]), (21, 16, P["gold"])):
            draw.rectangle((x, y, x + 3, y + 5), fill=color)
        return image
    if name == "foreground_beam":
        draw.rectangle((0, 0, 12, 63), fill=P["ink"])
        draw.rectangle((3, 0, 9, 63), fill=P["wood_dark"])
        draw.line((5, 0, 5, 63), fill=P["wood_light"], width=1)
        draw.polygon(((10, 0), (63, 0), (63, 10), (24, 10)), fill=P["ink"])
        return image
    raise AssertionError("unreachable inn module: " + name)


def _inn_layout():
    roles = (
        "entrance", "walkway", "counter", "innkeeper_light", "table",
        "kitchen_fire", "stairs", "north_exit", "foreground_beam",
    )
    placements = []

    def add(asset, x, y, layer, order, role):
        placements.append({
            "asset": "inn/" + asset + ".png",
            "x": x,
            "y": y,
            "layer": layer,
            "sortingOrder": order,
            "role": role,
        })

    for x, y, asset in (
            (14.5, 2.0, "entry_stone"), (15.5, 2.0, "rug"),
            (14.5, 3.0, "floor_wood_a"), (15.5, 3.0, "floor_wood_b"),
            (14.5, 4.0, "floor_wood_b"), (15.5, 4.0, "floor_wood_a"),
            (14.5, 5.0, "floor_wood_a"), (15.5, 5.0, "floor_wood_b"),
            (14.5, 6.0, "floor_wood_b"), (15.5, 6.0, "floor_wood_a"),
            (14.5, 7.0, "floor_wood_a"), (15.5, 7.0, "floor_wood_b"),
            (14.5, 8.0, "floor_wood_b"), (15.5, 8.0, "floor_wood_a"),
            (14.5, 9.0, "floor_wood_a"), (15.5, 9.0, "floor_wood_b")):
        add(asset, x, y, "Ground", -100, "entrance" if y == 2.0 else "walkway")
    add("counter", 15.0, 11.4, "Environment", 2, "counter")
    add("counter_lantern", 15.0, 13.8, "Environment", 8, "innkeeper_light")
    add("window_light", 7.2, 12.6, "Environment", 1, "innkeeper_light")
    add("window_light", 22.8, 12.6, "Environment", 1, "innkeeper_light")
    add("kitchen_wall", 3.8, 12.6, "Environment", 1, "kitchen_fire")
    add("stove", 5.0, 10.5, "Environment", 4, "kitchen_fire")
    add("shelf", 7.4, 12.4, "Environment", 3, "kitchen_fire")
    add("shelf", 1.9, 12.2, "Environment", 3, "kitchen_fire")
    add("stairs", 25.5, 12.6, "Environment", 3, "stairs")
    add("table", 22.8, 8.6, "Environment", 4, "table")
    add("table", 22.3, 5.1, "Environment", 4, "table")
    add("north_door", 15.0, 16.0, "Environment", 5, "north_exit")
    add("foreground_beam", 1.0, 13.5, "Foreground", 2, "foreground_beam")
    add("foreground_beam", 29.0, 13.5, "Foreground", 2, "foreground_beam")
    foreground = [
        {"asset": "inn/foreground_beam.png", "area": 4096},
        {"asset": "inn/foreground_beam.png", "area": 4096},
    ]
    return {"schemaVersion": 1, "scene": "inn", "roles": list(roles),
            "placements": placements, "foreground": foreground}


def _build_dense_inn(project_root):
    source_root = project_root / "Assets/ArtSource/MVP/dense_pixel"
    output_root = project_root / "Assets/Art/MVP/dense_pixel"
    module_names = (
        "floor_wood_a", "floor_wood_b", "entry_stone", "rug", "counter", "counter_lantern",
        "table", "stove", "stairs", "kitchen_wall", "window_light", "north_door", "shelf",
        "foreground_beam",
    )
    written = []
    changed = 0
    for name in module_names:
        image = _draw_inn_module(name)
        relative = Path("environment/inn") / (name + ".png")
        source_path = source_root / relative
        output_path = output_root / relative
        changed += int(_write_if_changed(image, source_path))
        changed += int(_write_if_changed(image, output_path))
        written.extend((source_path, output_path))
    layout_path = source_root / "layouts/inn.json"
    changed += int(_write_text_if_changed(
        layout_path, json.dumps(_inn_layout(), ensure_ascii=False, indent=2) + "\n"))
    written.append(layout_path)
    image = _draw_dense_actor("mvp_innkeeper")
    source_path = source_root / "actors/mvp_innkeeper.png"
    resource_path = project_root / "Assets/Resources/Art/MVP/dense_pixel/actors/mvp_innkeeper.png"
    changed += int(_write_if_changed(image, source_path))
    changed += int(_write_if_changed(image, resource_path))
    written.extend((source_path, resource_path))
    return tuple(written), changed


def build_dense_mvp_art(project_root):
    """Write deterministic source assets and return (written paths, change count)."""
    project_root = Path(project_root)
    roster_path = project_root / "Assets/ArtSource/Characters/Manifests/player-roster.json"
    roster = json.loads(roster_path.read_text(encoding="utf-8"))
    hero = next(item for item in roster["characters"] if item["id"] == HERO_ID)
    hero["frameSize"] = FRAME_SIZE
    animations = hero["animations"]
    columns = max(animation["frames"] for animation in animations)
    sheet_size = (columns * FRAME_SIZE, len(animations) * FRAME_SIZE)
    sheets = _new_sheet(sheet_size)
    for row_index, animation in enumerate(animations):
        for frame_index in range(animation["frames"]):
            _draw_frame(
                sheets,
                frame_index * FRAME_SIZE,
                row_index * FRAME_SIZE,
                animation["direction"],
                animation["name"],
                frame_index,
                animation["frames"],
            )

    written = []
    changed = 0
    source_directory = project_root / "Assets/ArtSource/Characters/Generated" / HERO_ID
    for module_name, image in sheets.items():
        path = source_directory / (module_name + ".png")
        changed += int(_write_if_changed(image, path))
        written.append(path)

    encoded_roster = json.dumps(roster, ensure_ascii=False, indent=2) + "\n"
    changed += int(_write_text_if_changed(roster_path, encoded_roster))
    written.append(roster_path)

    resource_directory = project_root / "Assets/Resources/Art/MVP"
    for weapon_id in WEAPON_IDS:
        path = resource_directory / (weapon_id + ".png")
        changed += int(_write_if_changed(_draw_weapon_layer(weapon_id), path))
        written.append(path)

    town_written, town_changed = _build_dense_town(project_root)
    written.extend(town_written)
    changed += town_changed
    inn_written, inn_changed = _build_dense_inn(project_root)
    written.extend(inn_written)
    changed += inn_changed
    return tuple(written), changed


if __name__ == "__main__":
    root = Path(__file__).resolve().parents[2]
    _, count = build_dense_mvp_art(root)
    print("changed={}".format(count))
