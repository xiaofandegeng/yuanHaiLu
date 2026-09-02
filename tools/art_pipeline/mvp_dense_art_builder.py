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
    "ink_deep": (14, 15, 24, 255),
    "ink": (24, 26, 42, 255),
    "ink_light": (48, 48, 67, 255),
    "skin_shadow": (134, 83, 63, 255),
    "skin": (213, 158, 120, 255),
    "skin_light": (246, 202, 158, 255),
    "hair": (32, 28, 38, 255),
    "hair_light": (72, 62, 75, 255),
    "hair_shine": (115, 100, 120, 255),
    "cloak_dark": (26, 45, 90, 255),
    "cloak": (46, 78, 142, 255),
    "cloak_light": (82, 121, 188, 255),
    "cloak_trim": (120, 160, 220, 255),
    "paper_shadow": (175, 168, 158, 255),
    "paper": (235, 229, 210, 255),
    "paper_light": (255, 247, 224, 255),
    "vermilion_dark": (109, 39, 43, 255),
    "vermilion": (183, 57, 55, 255),
    "vermilion_light": (235, 88, 68, 255),
    "vermilion_bright": (255, 110, 85, 255),
    "leather_dark": (60, 38, 28, 255),
    "leather": (92, 60, 45, 255),
    "leather_light": (151, 105, 68, 255),
    "steel_dark": (60, 75, 95, 255),
    "steel": (150, 175, 195, 255),
    "steel_light": (215, 230, 238, 255),
    "steel_shine": (245, 252, 255, 255),
    "shadow": (18, 20, 35, 120),
    "gold_dark": (160, 115, 45, 255),
    "gold": (220, 168, 75, 255),
    "gold_light": (250, 215, 125, 255),
    "water_deep": (18, 52, 68, 255),
    "water": (32, 85, 98, 255),
    "water_light": (78, 142, 145, 255),
    "water_ripple": (115, 185, 192, 255),
    "jade_dark": (32, 65, 58, 255),
    "jade": (62, 102, 75, 255),
    "jade_light": (95, 145, 110, 255),
    "jade_moss": (42, 78, 52, 255),
    "stone_dark": (75, 80, 76, 255),
    "stone": (104, 110, 104, 255),
    "stone_light": (159, 161, 145, 255),
    "stone_highlight": (195, 198, 182, 255),
    "wood_dark": (58, 38, 30, 255),
    "wood": (110, 72, 46, 255),
    "wood_light": (168, 118, 70, 255),
    "wood_highlight": (205, 155, 105, 255),
    "roof_dark": (26, 38, 48, 255),
    "roof": (34, 48, 62, 255),
    "roof_light": (70, 91, 102, 255),
    "roof_highlight": (105, 130, 145, 255),
    "warm_dark": (180, 115, 45, 255),
    "warm": (234, 172, 79, 255),
    "warm_light": (255, 228, 150, 255),
    "warm_glow": (255, 245, 190, 255),
    "porcelain_blue": (45, 85, 145, 255),
    "porcelain_white": (240, 246, 252, 255),
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

    # 1. 角色地面投影与步履
    _polygon(accessory, ox, oy, ((cx - 12, 43), (cx + 12, 43), (cx + 9, 46),
                                  (cx - 9, 46)), P["shadow"])

    left_step = -stride // 2
    right_step = stride // 2
    # 裤腿与长靴
    _rect(body, ox, oy, (cx - 10 + left_step, 31 + cy, cx - 3 + left_step, 41), P["ink_deep"])
    _rect(body, ox, oy, (cx + 3 + right_step, 31 + cy, cx + 10 + right_step, 41), P["ink_deep"])
    _rect(body, ox, oy, (cx - 8 + left_step, 32 + cy, cx - 4 + left_step, 38), P["paper_shadow"])
    _rect(body, ox, oy, (cx + 4 + right_step, 32 + cy, cx + 8 + right_step, 38), P["paper_shadow"])
    # 皂靴与铜扣
    _rect(body, ox, oy, (cx - 11 + left_step, 39, cx - 2 + left_step, 44), P["ink"])
    _rect(body, ox, oy, (cx + 2 + right_step, 39, cx + 11 + right_step, 44), P["ink"])
    _rect(body, ox, oy, (cx - 9 + left_step, 40, cx - 3 + left_step, 43), P["leather"])
    _rect(body, ox, oy, (cx + 3 + right_step, 40, cx + 9 + right_step, 43), P["leather"])
    _rect(body, ox, oy, (cx - 5 + left_step, 40, cx - 4 + left_step, 41), P["gold_light"])
    _rect(body, ox, oy, (cx + 4 + right_step, 40, cx + 5 + right_step, 41), P["gold_light"])

    # 2. 儒侠青灰外袍与开衩层次
    cloak_shift = layout["cloak"] * (3 + attack)
    _polygon(outfit, ox, oy, ((cx - 11, 18 + cy), (cx + 10, 18 + cy),
                               (cx + 15 + cloak_shift, 33), (cx + 8, 37),
                               (cx, 34), (cx - 10, 37), (cx - 15 + cloak_shift, 31)), P["ink_deep"])
    _polygon(outfit, ox, oy, ((cx - 9, 19 + cy), (cx + 8, 19 + cy),
                               (cx + 13 + cloak_shift, 32), (cx + 6, 35),
                               (cx, 32), (cx - 8, 35), (cx - 13 + cloak_shift, 30)), P["cloak_dark"])
    _polygon(outfit, ox, oy, ((cx - 7, 20 + cy), (cx + 6, 20 + cy),
                               (cx + 9 + cloak_shift, 30), (cx + 3, 32),
                               (cx - 5, 30), (cx - 9 + cloak_shift, 29)), P["cloak"])
    _line(outfit, ox, oy, ((cx - 8, 22 + cy), (cx - 4, 27), (cx - 1, 31)), P["cloak_light"], 2)
    _line(outfit, ox, oy, ((cx - 7, 23 + cy), (cx - 3, 28)), P["cloak_trim"], 1)

    # 白绫中单与交领
    _polygon(outfit, ox, oy, ((cx - 6, 21 + cy), (cx + 6, 21 + cy),
                               (cx + 8, 33), (cx, 35), (cx - 8, 33)), P["paper_shadow"])
    _polygon(outfit, ox, oy, ((cx - 4, 21 + cy), (cx + 4, 21 + cy),
                               (cx + 5, 32), (cx, 33), (cx - 5, 32)), P["paper"])
    _line(outfit, ox, oy, ((cx, 22 + cy), (cx, 32)), P["paper_light"], 1)

    # 3. 双臂、护腕与手掌
    arm_shift = attack * (2 if direction in ("right", "down") else -2)
    _rect(outfit, ox, oy, (cx - 14, 21 + cy, cx - 8, 30 + cy), P["ink_deep"])
    _rect(outfit, ox, oy, (cx + 8, 21 + cy, cx + 14, 30 + cy), P["ink_deep"])
    _rect(outfit, ox, oy, (cx - 12, 22 + cy, cx - 8, 28 + cy), P["cloak"])
    _rect(outfit, ox, oy, (cx + 8, 22 + cy, cx + 12, 28 + cy), P["cloak"])
    # 皮革护腕与手部
    _rect(body, ox, oy, (cx - 13, 27 + cy, cx - 9, 30 + cy), P["leather_dark"])
    _rect(body, ox, oy, (cx + 9 + arm_shift, 27 + cy, cx + 13 + arm_shift, 30 + cy), P["leather_dark"])
    _rect(body, ox, oy, (cx - 12, 28 + cy, cx - 10, 29 + cy), P["gold"])
    _rect(body, ox, oy, (cx + 10 + arm_shift, 28 + cy, cx + 12 + arm_shift, 29 + cy), P["gold"])
    _rect(body, ox, oy, (cx - 13, 30 + cy, cx - 9, 33 + cy), P["skin_shadow"])
    _rect(body, ox, oy, (cx + 9 + arm_shift, 30 + cy, cx + 13 + arm_shift, 33 + cy), P["skin"])

    # 4. 面容、眼神高光与束发
    _rect(face, ox, oy, (cx - 7, 7 + cy, cx + 7, 20 + cy), P["ink_deep"])
    _rect(face, ox, oy, (cx - 5, 8 + cy, cx + 5, 18 + cy), P["skin"])
    _rect(face, ox, oy, (cx - 4, 9 + cy, cx + 4, 14 + cy), P["skin_light"])
    if layout["face"] == "front":
        _rect(face, ox, oy, (cx - 3, 13 + cy, cx - 2, 15 + cy), P["ink_deep"])
        _rect(face, ox, oy, (cx + 2, 13 + cy, cx + 3, 15 + cy), P["ink_deep"])
        _rect(face, ox, oy, (cx - 3, 13 + cy, cx - 3, 13 + cy), P["steel_shine"])
        _rect(face, ox, oy, (cx + 2, 13 + cy, cx + 2, 13 + cy), P["steel_shine"])
        _rect(face, ox, oy, (cx - 1, 17 + cy, cx + 1, 17 + cy), P["skin_shadow"])
    elif layout["face"] == "left":
        _rect(face, ox, oy, (cx - 5, 13 + cy, cx - 4, 15 + cy), P["ink_deep"])
        _rect(face, ox, oy, (cx - 5, 13 + cy, cx - 5, 13 + cy), P["steel_shine"])
    elif layout["face"] == "right":
        _rect(face, ox, oy, (cx + 4, 13 + cy, cx + 5, 15 + cy), P["ink_deep"])
        _rect(face, ox, oy, (cx + 4, 13 + cy, cx + 4, 13 + cy), P["steel_shine"])

    # 发髻与垂鬓
    _polygon(hair, ox, oy, ((cx - 8, 8 + cy), (cx - 5, 2 + cy), (cx + 4, 2 + cy),
                             (cx + 8, 9 + cy), (cx + 6, 14 + cy), (cx - 6, 13 + cy)), P["ink_deep"])
    _polygon(hair, ox, oy, ((cx - 6, 7 + cy), (cx - 3, 3 + cy), (cx + 4, 4 + cy),
                             (cx + 7, 10 + cy), (cx + 2, 8 + cy), (cx - 4, 10 + cy)), P["hair"])
    _line(hair, ox, oy, ((cx - 2, 4 + cy), (cx + 3, 5 + cy)), P["hair_shine"], 1)
    _rect(hair, ox, oy, (cx - 3, 0 + cy, cx + 3, 4 + cy), P["ink_deep"])
    _rect(hair, ox, oy, (cx - 1, 0 + cy, cx + 2, 2 + cy), P["hair_light"])
    if layout["face"] == "back":
        _rect(hair, ox, oy, (cx - 6, 10 + cy, cx + 6, 19 + cy), P["hair"])
        _line(hair, ox, oy, ((cx, 10 + cy), (cx, 18 + cy)), P["hair_shine"], 1)
    # 两缕垂鬓
    _line(hair, ox, oy, ((cx - 6, 12 + cy), (cx - 6, 18 + cy)), P["hair"], 1)
    _line(hair, ox, oy, ((cx + 6, 12 + cy), (cx + 6, 18 + cy)), P["hair"], 1)

    # 飘逸朱红发带
    ribbon = layout["ribbon"]
    ribbon_sway = 1 if frame_index % 2 == 1 else 0
    _rect(accessory, ox, oy, (cx - 2, 3 + cy, cx + 3, 5 + cy), P["vermilion_dark"])
    _polygon(accessory, ox, oy, ((cx + ribbon * 2, 4 + cy),
                                 (cx + ribbon * (9 + ribbon_sway), 7 + cy),
                                 (cx + ribbon * (6 + ribbon_sway), 12 + cy)), P["vermilion"])
    _line(accessory, ox, oy, ((cx + ribbon * 3, 6 + cy),
                              (cx + ribbon * (8 + ribbon_sway), 9 + cy)), P["vermilion_bright"], 1)

    # 5. 朱红与黛蓝双层腰封 + 青玉佩流苏
    _rect(accessory, ox, oy, (cx - 9, 26 + cy, cx + 9, 30 + cy), P["ink_deep"])
    _rect(accessory, ox, oy, (cx - 8, 27 + cy, cx + 8, 28 + cy), P["cloak_dark"])
    _rect(accessory, ox, oy, (cx - 7, 28 + cy, cx + 7, 29 + cy), P["vermilion"])
    _rect(accessory, ox, oy, (cx - 1, 27 + cy, cx + 1, 29 + cy), P["gold_light"])
    # 青玉佩与流苏垂带
    _polygon(accessory, ox, oy, ((cx + 4, 29 + cy), (cx + 8, 33 + cy), (cx + 6, 35 + cy),
                                 (cx + 2, 30 + cy)), P["jade"])
    _line(accessory, ox, oy, ((cx + 6, 34 + cy), (cx + 7, 39 + cy)), P["vermilion_bright"], 1)

    # 6. 背负剑鞘与兽首吞口
    scabbard = layout["sword"]
    _line(weapon, ox, oy, ((cx + scabbard * 9, 15 + cy), (cx + scabbard * 16, 37)), P["ink_deep"], 4)
    _line(weapon, ox, oy, ((cx + scabbard * 8, 15 + cy), (cx + scabbard * 15, 36)), P["leather_dark"], 2)
    _line(weapon, ox, oy, ((cx + scabbard * 8, 15 + cy), (cx + scabbard * 11, 23 + cy)), P["steel_shine"], 1)
    _rect(weapon, ox, oy, (cx + scabbard * 6 - 1, 13 + cy, cx + scabbard * 6 + 3, 18 + cy), P["gold"])
    _rect(weapon, ox, oy, (cx + scabbard * 6, 14 + cy, cx + scabbard * 6 + 2, 17 + cy), P["gold_light"])


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
        # 冷钢宝剑：兽首剑格 + 刃光高光 + 剑柄缠绳
        draw.line(((18, 35), (36, 10)), fill=P["ink_deep"], width=5)
        draw.line(((18, 35), (36, 10)), fill=P["steel_dark"], width=3)
        draw.line(((19, 33), (35, 11)), fill=P["steel_light"], width=2)
        draw.line(((21, 31), (35, 12)), fill=P["steel_shine"], width=1)
        # 青铜剑格与剑柄
        draw.rectangle((15, 30, 24, 34), fill=P["gold_dark"])
        draw.rectangle((16, 31, 23, 33), fill=P["gold_light"])
        draw.rectangle((16, 35, 21, 43), fill=P["leather_dark"])
        draw.line((17, 36, 20, 42), fill=P["vermilion"], width=1)
        draw.rectangle((17, 43, 20, 45), fill=P["gold"])
    elif weapon_id == "weapon_gauntlets":
        # 玄铁指虎：金属光泽 + 铆钉 + 暗纹护腕
        draw.rectangle((18, 22, 33, 36), fill=P["ink_deep"])
        draw.rectangle((20, 23, 31, 34), fill=P["leather_dark"])
        draw.rectangle((21, 22, 31, 26), fill=P["leather_light"])
        for x in (20, 23, 26, 29):
            draw.rectangle((x, 18, x + 2, 24), fill=P["steel_dark"])
            draw.line((x, 19, x + 1, 21), fill=P["steel_shine"])
            draw.rectangle((x + 1, 27, x + 2, 28), fill=P["gold_light"])
    elif weapon_id == "weapon_dart":
        # 流线型透甲飞镖：三棱银刃 + 尾羽系绳
        draw.rectangle((16, 25, 29, 38), fill=P["ink_deep"])
        draw.rectangle((18, 27, 27, 36), fill=P["leather_dark"])
        for index, y in enumerate((16, 21, 26)):
            draw.polygon(((26, y), (42, y + 3), (26, y + 6)), fill=P["steel_dark"])
            draw.polygon(((28, y + 1), (40, y + 3), (28, y + 5)), fill=P["steel_light"])
            draw.line(((29, y + 3), (41, y + 3)), fill=P["steel_shine"], width=1)
            draw.line(((22, y + 3), (25, y + 3)), fill=P["vermilion_bright"], width=1)
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
        # 青石路面：咬合石纹 + 斑驳石光
        draw.rectangle((0, 0, 15, 15), fill=P["ink_deep"])
        draw.rectangle((1, 1, 14, 14), fill=P["stone"])
        if name == "road_a":
            draw.line((1, 4, 14, 3), fill=P["stone_light"], width=1)
            draw.line((1, 5, 14, 5), fill=P["stone_highlight"], width=1)
            draw.line((3, 11, 12, 12), fill=P["stone_dark"], width=1)
            draw.rectangle((6, 7, 9, 9), fill=P["stone_highlight"])
        elif name == "road_b":
            draw.line((4, 1, 3, 14), fill=P["stone_light"], width=1)
            draw.line((5, 1, 5, 14), fill=P["stone_highlight"], width=1)
            draw.line((11, 3, 12, 12), fill=P["stone_dark"], width=1)
            draw.rectangle((7, 5, 9, 8), fill=P["stone_highlight"])
        else:
            draw.arc((-6, -6, 22, 22), 0, 90, fill=P["stone_highlight"], width=2)
            draw.arc((-4, -4, 20, 20), 0, 90, fill=P["stone_light"], width=1)
        return image
    if name.startswith("water"):
        # 碧波微漾与波光倒影
        base = P["water_deep"] if name == "water_deep" else P["water"]
        draw.rectangle((0, 0, 15, 15), fill=base)
        if name == "water_flow":
            draw.line((1, 4, 8, 3, 14, 5), fill=P["water_ripple"], width=1)
            draw.line((0, 11, 6, 10, 12, 12), fill=P["water_light"], width=1)
            draw.rectangle((9, 10, 10, 11), fill=P["water_ripple"])
        elif name == "water_reflection":
            # 黛瓦倒影与红灯笼金暖碎影
            draw.line((3, 1, 6, 5, 2, 9), fill=P["roof_dark"], width=1)
            draw.line((4, 2, 7, 6), fill=P["roof_light"], width=1)
            draw.line((10, 6, 14, 10), fill=P["warm"], width=1)
            draw.line((11, 7, 13, 9), fill=P["warm_light"], width=1)
        else:
            draw.line((2, 8, 12, 8), fill=P["water_light"], width=1)
            draw.line((4, 9, 8, 9), fill=P["water_ripple"], width=1)
        return image
    if name.startswith("shore"):
        # 驳岸青苔与石质阶梯
        draw.rectangle((0, 0, 15, 15), fill=P["jade_moss"])
        draw.polygon(((0, 9), (5, 7), (10, 8), (15, 5), (15, 15), (0, 15)), fill=P["ink_deep"])
        draw.polygon(((0, 10), (5, 8), (10, 9), (15, 6), (15, 15), (0, 15)),
                     fill=P["stone"] if name == "shore_stone" else P["water"])
        draw.line((0, 10, 5, 8, 10, 9, 15, 6), fill=P["stone_highlight"], width=1)
        draw.rectangle((6, 4, 8, 6), fill=P["jade_light"])
        return image
    if name == "inn_roof":
        # 歇山顶黛瓦层叠飞檐
        draw.polygon(((1, 39), (15, 9), (48, 3), (63, 35), (57, 47), (6, 47)), fill=P["ink_deep"])
        draw.polygon(((6, 37), (17, 12), (47, 7), (58, 34), (54, 42), (9, 42)), fill=P["roof"])
        for y in range(14, 40, 6):
            draw.line((11, y, 55, y - 6), fill=P["roof_light"], width=2)
            draw.line((12, y - 1, 54, y - 7), fill=P["roof_highlight"], width=1)
        # 飞檐翘角与滴水瓦
        draw.line((3, 41, 61, 41), fill=P["gold_dark"], width=2)
        draw.line((2, 40, 5, 38), fill=P["roof_highlight"], width=2)
        draw.line((62, 37, 59, 39), fill=P["roof_highlight"], width=2)
        return image
    if name == "inn_wall":
        # 粉墙黛瓦与镂空雕花木窗
        draw.rectangle((2, 13, 61, 63), fill=P["ink_deep"])
        draw.rectangle((6, 17, 57, 60), fill=P["paper_shadow"])
        draw.rectangle((10, 20, 53, 58), fill=P["paper"])
        for x in (11, 31, 51):
            draw.rectangle((x, 17, x + 3, 60), fill=P["wood_dark"])
            draw.line((x + 1, 18, x + 1, 59), fill=P["wood_light"])
        # 两扇透光木格花窗
        for wx in (17, 37):
            draw.rectangle((wx, 27, wx + 10, 39), fill=P["ink_deep"])
            draw.rectangle((wx + 2, 29, wx + 8, 37), fill=P["warm"])
            draw.rectangle((wx + 3, 30, wx + 7, 36), fill=P["warm_light"])
            draw.line((wx + 5, 29, wx + 5, 37), fill=P["wood_dark"], width=1)
            draw.line((wx + 2, 33, wx + 8, 33), fill=P["wood_dark"], width=1)
        # 墙底青石踢脚线
        draw.rectangle((6, 58, 57, 61), fill=P["stone"])
        draw.line((6, 58, 57, 58), fill=P["stone_highlight"], width=1)
        return image
    if name == "inn_door":
        # 红木雕花客栈大门 + 黄铜门环
        draw.rectangle((2, 1, 29, 31), fill=P["ink_deep"])
        draw.rectangle((5, 4, 26, 31), fill=P["wood_dark"])
        draw.rectangle((6, 5, 25, 30), fill=P["wood"])
        draw.line((15, 4, 15, 31), fill=P["wood_light"], width=2)
        # 门环与铜钉
        for hy in (12, 20):
            draw.rectangle((9, hy, 12, hy + 2), fill=P["gold_light"])
            draw.rectangle((18, hy, 21, hy + 2), fill=P["gold_light"])
        draw.rectangle((0, 28, 31, 31), fill=P["stone"])
        return image
    if name == "inn_sign":
        # “悦来客栈”朱红酒幌招牌
        draw.rectangle((1, 3, 30, 28), fill=P["ink_deep"])
        draw.rectangle((4, 5, 27, 25), fill=P["wood_dark"])
        draw.rectangle((7, 8, 24, 11), fill=P["paper"])
        # 朱红酒字大旗
        draw.polygon(((9, 13), (22, 13), (20, 23), (11, 23)), fill=P["vermilion"])
        draw.rectangle((13, 15, 18, 20), fill=P["paper_light"])
        draw.line((0, 1, 31, 1), fill=P["wood_light"], width=2)
        return image
    if name == "bridge":
        # 江南青石拱桥 + 抱鼓石栏杆
        draw.polygon(((0, 26), (6, 12), (41, 12), (47, 26), (43, 39), (4, 39)), fill=P["ink_deep"])
        draw.polygon(((3, 26), (9, 15), (38, 15), (44, 26), (40, 34), (7, 34)), fill=P["stone"])
        # 桥拱阴影与苔痕
        draw.arc((12, 22, 35, 42), 180, 360, fill=P["ink_deep"], width=4)
        draw.arc((13, 23, 34, 41), 180, 360, fill=P["jade_moss"], width=2)
        # 石桥板缝与石栏杆
        for x in (8, 17, 27, 37):
            draw.line((x, 13, x - 2, 35), fill=P["stone_light"], width=2)
            draw.line((x + 1, 13, x - 1, 35), fill=P["stone_highlight"], width=1)
            # 望柱抱鼓石
            draw.rectangle((x - 1, 9, x + 2, 14), fill=P["stone_highlight"])
        draw.line((4, 15, 43, 15), fill=P["stone_highlight"], width=1)
        return image
    if name == "boat":
        # 摇橹木质乌篷船 + 竹篾篷顶
        draw.polygon(((2, 29), (44, 29), (38, 40), (8, 40)), fill=P["ink_deep"])
        draw.polygon(((5, 31), (41, 31), (35, 37), (10, 37)), fill=P["wood"])
        draw.line((6, 31, 40, 31), fill=P["wood_light"], width=1)
        # 拱形竹篾船篷
        draw.arc((16, 16, 32, 32), 180, 360, fill=P["ink_deep"], width=3)
        draw.arc((17, 17, 31, 31), 180, 360, fill=P["wood_dark"], width=2)
        # 船橹与白帆/布幔
        draw.line((22, 31, 22, 6), fill=P["wood_light"], width=2)
        draw.polygon(((24, 8), (41, 19), (24, 24)), fill=P["paper_shadow"])
        draw.polygon(((25, 9), (39, 19), (25, 23)), fill=P["paper"])
        draw.line((25, 10, 38, 19), fill=P["paper_light"], width=1)
        return image
    if name == "bollard":
        # 沿河系缆青石/沉木桩
        draw.rectangle((4, 3, 11, 15), fill=P["ink_deep"])
        draw.rectangle((5, 4, 10, 14), fill=P["wood_dark"])
        draw.rectangle((6, 5, 9, 13), fill=P["wood"])
        draw.rectangle((3, 1, 12, 5), fill=P["wood_light"])
        draw.rectangle((4, 2, 11, 4), fill=P["wood_highlight"])
        return image
    if name == "lantern":
        # 挂檐八角红木灯笼 + 暖黄烛光
        draw.rectangle((7, 0, 8, 15), fill=P["wood_dark"])
        draw.rectangle((2, 4, 13, 14), fill=P["ink_deep"])
        draw.rectangle((4, 5, 11, 13), fill=P["vermilion_dark"])
        draw.rectangle((5, 6, 10, 12), fill=P["warm"])
        draw.rectangle((6, 7, 9, 11), fill=P["warm_light"])
        draw.rectangle((7, 8, 8, 10), fill=P["warm_glow"])
        # 灯笼流苏
        draw.line((7, 14, 8, 15), fill=P["vermilion_bright"], width=1)
        return image
    if name == "crate":
        # 码头货箱 + 铜锁扣
        draw.rectangle((0, 1, 15, 15), fill=P["ink_deep"])
        draw.rectangle((2, 3, 13, 14), fill=P["wood_dark"])
        draw.rectangle((3, 4, 12, 13), fill=P["wood"])
        draw.line((3, 4, 12, 13), fill=P["wood_light"], width=1)
        draw.line((12, 4, 3, 13), fill=P["wood_light"], width=1)
        draw.rectangle((6, 7, 9, 10), fill=P["gold_light"])
        return image
    if name in ("willow_near", "willow_far"):
        # 烟雨垂柳：虬曲老树干 + 多层翠绿柔韧柳丝
        draw.line((size // 2, 0, size // 2 - 8, size - 4), fill=P["ink_deep"], width=5)
        draw.line((size // 2, 0, size // 2 - 8, size - 4), fill=P["wood_dark"], width=3)
        draw.line((size // 2 + 1, 0, size // 2 - 7, size - 4), fill=P["wood_light"], width=1)
        leaf_dark = P["jade_dark"] if name == "willow_near" else P["jade"]
        leaf_light = P["jade"] if name == "willow_near" else P["jade_light"]
        for index, y in enumerate(range(6, size - 6, 8)):
            offset = 12 + (index % 3) * 6
            # 左侧垂柳带
            draw.polygon(((size // 2 - 4, y), (size // 2 - offset, y + 8),
                          (size // 2 - 5, y + 15)), fill=leaf_dark)
            draw.line(((size // 2 - 4, y), (size // 2 - offset + 2, y + 8)), fill=leaf_light, width=1)
            # 右侧垂柳带
            draw.polygon(((size // 2 + 2, y + 2), (size // 2 + offset, y + 9),
                          (size // 2 + 4, y + 16)), fill=leaf_light)
            draw.line(((size // 2 + 2, y + 2), (size // 2 + offset - 2, y + 9)), fill=leaf_dark, width=1)
        return image
    if name == "roof_trim":
        # 前景飞檐瓦当
        draw.polygon(((0, 0), (31, 0), (31, 12), (19, 8), (10, 14), (0, 9)), fill=P["ink_deep"])
        draw.polygon(((2, 1), (29, 1), (29, 9), (19, 6), (10, 11), (2, 7)), fill=P["roof"])
        draw.line((2, 2, 29, 2), fill=P["roof_highlight"], width=1)
        return image
    raise AssertionError("unreachable module: " + name)


def _draw_dense_actor(actor_id):
    size = 16 if actor_id == "mvp_lost_pouch" else 48
    image = Image.new("RGBA", (size, size), P["clear"])
    draw = ImageDraw.Draw(image)
    if actor_id == "mvp_lost_pouch":
        # 精致云纹锦囊：金线锁边 + 朱红结扣 + 青玉流苏
        draw.ellipse((2, 6, 13, 15), fill=P["ink_deep"])
        draw.ellipse((3, 7, 12, 14), fill=P["vermilion"])
        draw.ellipse((4, 8, 11, 13), fill=P["vermilion_bright"])
        draw.line((3, 7, 12, 7), fill=P["gold_light"], width=2)
        draw.rectangle((6, 2, 9, 6), fill=P["gold"])
        draw.rectangle((7, 3, 8, 5), fill=P["jade_light"])
        draw.line((7, 14, 8, 16), fill=P["jade"], width=1)
        return image
    if actor_id == "mvp_innkeeper":
        # 掌柜老赵：文生方巾 + 酱色员外长衫 + 青灰围裙 + 铜钥匙算盘
        draw.ellipse((8, 39, 40, 47), fill=P["shadow"])
        # 长靴与裤脚
        draw.rectangle((13, 29, 21, 42), fill=P["ink_deep"])
        draw.rectangle((28, 29, 36, 42), fill=P["ink_deep"])
        # 酱色员外长衫与青灰围裙
        draw.rectangle((7, 19, 41, 36), fill=P["ink_deep"])
        draw.rectangle((9, 20, 39, 35), fill=P["wood_dark"])
        draw.rectangle((12, 21, 36, 33), fill=P["paper_shadow"])
        draw.rectangle((14, 22, 34, 31), fill=P["paper"])
        # 腰封与掌柜铜钥匙圈
        draw.rectangle((10, 23, 38, 25), fill=P["leather_dark"])
        draw.ellipse((12, 26, 17, 31), fill=P["gold_light"])
        draw.ellipse((13, 27, 16, 30), fill=P["ink_deep"])
        # 头面部与文生方巾软帽
        draw.rectangle((12, 7, 37, 22), fill=P["ink_deep"])
        draw.rectangle((15, 9, 34, 20), fill=P["skin"])
        draw.rectangle((16, 10, 33, 15), fill=P["skin_light"])
        draw.rectangle((10, 4, 39, 11), fill=P["ink_deep"])
        draw.rectangle((13, 5, 36, 8), fill=P["roof"])
        draw.rectangle((15, 5, 34, 6), fill=P["roof_highlight"])
        # 和善五官与胡须
        draw.rectangle((17, 13, 19, 15), fill=P["ink_deep"])
        draw.rectangle((29, 13, 31, 15), fill=P["ink_deep"])
        draw.line((21, 17, 27, 17), fill=P["ink_deep"], width=2)
        # 手中青花瓷茶壶
        draw.rectangle((32, 24, 38, 29), fill=P["porcelain_white"])
        draw.rectangle((34, 25, 36, 27), fill=P["porcelain_blue"])
        return image
    if actor_id not in ("mvp_bandit_a", "mvp_bandit_b"):
        raise ValueError("unknown dense actor: " + actor_id)
    # 河岸水匪：粗布短打劲装 + 头巾 + 肌肉阴影 + 环首九环大刀 / 阔刃斧
    accent = P["vermilion_bright"] if actor_id == "mvp_bandit_a" else P["jade_light"]
    accent_dark = P["vermilion_dark"] if actor_id == "mvp_bandit_a" else P["jade_dark"]
    draw.ellipse((9, 39, 39, 47), fill=P["shadow"])
    # 绑腿与草鞋
    draw.rectangle((12, 30, 21, 42), fill=P["ink_deep"])
    draw.rectangle((27, 30, 36, 42), fill=P["ink_deep"])
    draw.line((13, 33, 20, 33), fill=P["paper_shadow"], width=1)
    draw.line((28, 33, 35, 33), fill=P["paper_shadow"], width=1)
    # 短打上衣与敞襟肌肉
    draw.rectangle((8, 19, 40, 36), fill=P["ink_deep"])
    draw.polygon(((11, 20), (36, 20), (41, 33), (25, 39), (7, 33)), fill=accent_dark)
    draw.polygon(((13, 21), (34, 21), (38, 31), (25, 36), (10, 31)), fill=accent)
    draw.polygon(((20, 20), (28, 20), (24, 29)), fill=P["skin_shadow"])
    draw.polygon(((21, 21), (27, 21), (24, 27)), fill=P["skin"])
    # 面容与煞气头巾
    draw.rectangle((13, 7, 36, 22), fill=P["ink_deep"])
    draw.rectangle((16, 9, 33, 20), fill=P["skin"])
    draw.rectangle((11, 4, 38, 11), fill=P["ink_deep"])
    draw.rectangle((14, 5, 35, 8), fill=accent)
    draw.line((33, 8, 39, 13), fill=accent, width=2)
    # 凶悍眼神与络腮胡
    draw.rectangle((17, 13, 20, 15), fill=P["ink_deep"])
    draw.rectangle((28, 13, 31, 15), fill=P["ink_deep"])
    draw.line((16, 17, 32, 17), fill=P["ink_deep"], width=2)
    # 专属武器：九环大刀 (A) / 阔刃斧 (B)
    if actor_id == "mvp_bandit_a":
        draw.line(((34, 28), (46, 12)), fill=P["ink_deep"], width=4)
        draw.line(((35, 27), (45, 13)), fill=P["steel_light"], width=2)
        draw.line(((37, 25), (45, 14)), fill=P["steel_shine"], width=1)
        draw.rectangle((41, 15, 43, 17), fill=P["gold_light"])
    else:
        draw.line(((34, 30), (42, 14)), fill=P["wood_dark"], width=3)
        draw.polygon(((39, 13), (46, 10), (45, 20), (38, 18)), fill=P["steel_light"])
        draw.line(((45, 11), (44, 19)), fill=P["steel_shine"], width=1)
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
        # 温润拼花红木地板
        draw.rectangle((0, 0, 15, 15), fill=P["wood_dark"])
        draw.rectangle((1, 1, 14, 14), fill=P["wood"])
        y = 4 if name == "floor_wood_a" else 9
        draw.line((1, y, 14, y), fill=P["wood_light"], width=1)
        draw.line((1, y + 1, 14, y + 1), fill=P["wood_highlight"], width=1)
        draw.line((7, 1, 7, 14), fill=P["ink_deep"], width=1)
        return image
    if name == "entry_stone":
        # 玄关青石板
        draw.rectangle((0, 0, 15, 15), fill=P["ink_deep"])
        draw.rectangle((1, 1, 14, 14), fill=P["stone"])
        draw.line((1, 7, 14, 7), fill=P["stone_highlight"], width=1)
        draw.line((7, 1, 7, 14), fill=P["stone_dark"], width=1)
        return image
    if name == "rug":
        # 迎宾金丝祥云地毯
        draw.rectangle((0, 0, 15, 15), fill=P["vermilion_dark"])
        draw.rectangle((1, 1, 14, 14), fill=P["vermilion"])
        draw.rectangle((4, 4, 11, 11), fill=P["gold_dark"])
        draw.rectangle((5, 5, 10, 10), fill=P["gold_light"])
        return image
    if name == "counter":
        # 掌柜红木柜台 + 账本 + 青花笔筒
        draw.rectangle((1, 19, 62, 58), fill=P["ink_deep"])
        draw.rectangle((4, 22, 59, 40), fill=P["wood_light"])
        draw.rectangle((4, 41, 59, 55), fill=P["wood"])
        for x in range(9, 58, 10):
            draw.line((x, 24, x, 54), fill=P["wood_dark"], width=2)
            draw.line((x + 1, 24, x + 1, 54), fill=P["wood_highlight"], width=1)
        draw.line((4, 22, 59, 22), fill=P["wood_highlight"], width=1)
        # 台面账本与青花瓷茶具
        draw.rectangle((18, 12, 42, 21), fill=P["ink_deep"])
        draw.rectangle((20, 14, 40, 19), fill=P["paper"])
        draw.line((21, 16, 39, 16), fill=P["ink_deep"], width=1)
        draw.rectangle((45, 14, 50, 20), fill=P["porcelain_white"])
        draw.rectangle((46, 16, 49, 18), fill=P["porcelain_blue"])
        return image
    if name == "counter_lantern":
        # 柜台暖光八角灯笼
        draw.line((16, 0, 16, 7), fill=P["wood_dark"], width=2)
        draw.rectangle((7, 7, 25, 26), fill=P["ink_deep"])
        draw.rectangle((9, 9, 23, 24), fill=P["vermilion_dark"])
        draw.rectangle((11, 11, 21, 22), fill=P["warm"])
        draw.rectangle((13, 13, 19, 20), fill=P["warm_light"])
        draw.rectangle((15, 15, 17, 18), fill=P["warm_glow"])
        return image
    if name == "table":
        # 沉木八仙圆桌 + 青瓷茶壶茶盏
        draw.ellipse((2, 10, 46, 36), fill=P["ink_deep"])
        draw.ellipse((5, 13, 43, 33), fill=P["wood_dark"])
        draw.ellipse((7, 15, 41, 31), fill=P["wood"])
        draw.line((10, 18, 38, 18), fill=P["wood_highlight"], width=2)
        # 桌腿与阴影
        for x in (11, 35):
            draw.rectangle((x, 31, x + 5, 44), fill=P["wood_dark"])
            draw.line((x + 1, 31, x + 1, 43), fill=P["wood_light"])
        # 青瓷茶壶与茶盏
        draw.ellipse((21, 19, 27, 25), fill=P["porcelain_white"])
        draw.ellipse((22, 20, 26, 24), fill=P["porcelain_blue"])
        draw.rectangle((15, 22, 18, 25), fill=P["porcelain_white"])
        return image
    if name == "stove":
        # 厨房青石火灶 + 柴火暖焰
        draw.rectangle((3, 4, 44, 44), fill=P["ink_deep"])
        draw.rectangle((6, 7, 41, 41), fill=P["stone"])
        draw.line((6, 7, 41, 7), fill=P["stone_highlight"], width=1)
        draw.rectangle((12, 19, 35, 41), fill=P["ink_deep"])
        # 柴火火焰
        draw.polygon(((18, 36), (24, 16), (30, 36)), fill=P["vermilion_bright"])
        draw.polygon(((20, 35), (24, 21), (28, 35)), fill=P["warm"])
        draw.polygon(((22, 34), (24, 26), (26, 34)), fill=P["warm_glow"])
        draw.rectangle((9, 9, 16, 12), fill=P["wood_dark"])
        return image
    if name == "stairs":
        # 实木楼梯台阶 + 扶手立柱
        draw.rectangle((2, 4, 62, 62), fill=P["ink_deep"])
        for index in range(7):
            y = 9 + index * 7
            draw.rectangle((6 + index * 3, y, 57, y + 6), fill=P["wood"])
            draw.line((6 + index * 3, y, 57, y), fill=P["wood_highlight"], width=1)
            draw.line((6 + index * 3, y + 6, 57, y + 6), fill=P["wood_dark"], width=1)
        return image
    if name == "kitchen_wall":
        # 厨房木格背景墙 + 悬挂蒜头红椒
        draw.rectangle((1, 1, 62, 62), fill=P["ink_deep"])
        draw.rectangle((5, 5, 58, 58), fill=P["wood_dark"])
        for y in (13, 27, 41):
            draw.line((7, y, 56, y), fill=P["wood"], width=2)
        # 挂腊味干货
        draw.rectangle((9, 9, 21, 22), fill=P["paper_shadow"])
        draw.rectangle((41, 9, 52, 20), fill=P["vermilion_dark"])
        return image
    if name == "window_light":
        # 江南镂空雕花木窗 + 暖阳斜照
        draw.rectangle((1, 1, 30, 30), fill=P["ink_deep"])
        draw.rectangle((4, 4, 27, 27), fill=P["warm"])
        draw.rectangle((6, 6, 25, 25), fill=P["warm_light"])
        draw.rectangle((9, 9, 22, 22), fill=P["warm_glow"])
        # 万字花格窗棂
        draw.line((15, 4, 15, 27), fill=P["wood_dark"], width=2)
        draw.line((4, 15, 27, 15), fill=P["wood_dark"], width=2)
        draw.rectangle((8, 8, 23, 23), outline=P["wood_dark"])
        return image
    if name == "north_door":
        # 客栈通往后院实木小门
        draw.rectangle((2, 0, 29, 31), fill=P["ink_deep"])
        draw.rectangle((5, 3, 26, 31), fill=P["wood_dark"])
        draw.rectangle((6, 4, 25, 30), fill=P["wood"])
        draw.line((15, 4, 15, 31), fill=P["wood_light"])
        draw.rectangle((18, 17, 21, 19), fill=P["gold_light"])
        return image
    if name == "shelf":
        # 博古酒架：红布封泥酒坛 + 瓷盘
        draw.rectangle((2, 2, 29, 31), fill=P["ink_deep"])
        draw.rectangle((4, 4, 27, 29), fill=P["wood_dark"])
        for y in (6, 14, 22):
            draw.line((4, y, 27, y), fill=P["wood_highlight"], width=2)
        # 红布封泥酒坛（女儿红、竹叶青）
        for x, y, color in ((7, 7, P["vermilion"]), (16, 7, P["jade"]),
                            (10, 15, P["vermilion_bright"]), (19, 15, P["gold_light"])):
            draw.rectangle((x, y, x + 4, y + 6), fill=color)
            draw.line((x, y, x + 4, y), fill=P["paper_light"], width=1)
        return image
    if name == "foreground_beam":
        # 客栈前景挑高立柱与雕花横梁
        draw.rectangle((0, 0, 13, 63), fill=P["ink_deep"])
        draw.rectangle((2, 0, 10, 63), fill=P["wood_dark"])
        draw.line((4, 0, 4, 63), fill=P["wood_light"], width=1)
        draw.polygon(((10, 0), (63, 0), (63, 12), (22, 12)), fill=P["ink_deep"])
        draw.polygon(((12, 1), (63, 1), (63, 10), (24, 10)), fill=P["wood_dark"])
        draw.line((24, 10, 63, 10), fill=P["wood_highlight"], width=1)
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
