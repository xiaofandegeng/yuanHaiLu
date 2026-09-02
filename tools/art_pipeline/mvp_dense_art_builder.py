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
    # 鲜明清晰的暗色卡通描边
    "ink_black": (14, 12, 22, 255),
    "ink_deep": (24, 20, 36, 255),
    "ink": (38, 32, 54, 255),
    "ink_light": (58, 50, 78, 255),
    "shadow": (16, 14, 28, 120),
    # 萌系肤色与粉嫩腮红
    "skin_shine": (255, 245, 230, 255),
    "skin_light": (255, 220, 185, 255),
    "skin": (240, 180, 140, 255),
    "skin_shadow": (195, 125, 95, 255),
    "skin_deep": (140, 80, 60, 255),
    "blush": (255, 125, 145, 230),
    "blush_light": (255, 175, 185, 180),
    # 水汪汪大眼
    "eye_dark": (16, 14, 24, 255),
    "eye_pupil": (35, 45, 75, 255),
    "eye_blue": (85, 155, 235, 255),
    "eye_shine": (255, 255, 255, 255),
    # 蓬松Q版黑发与高光
    "hair_shine": (155, 145, 175, 255),
    "hair_light": (95, 85, 110, 255),
    "hair": (48, 40, 58, 255),
    "hair_dark": (26, 20, 32, 255),
    # 明快清爽青灰儒侠小袍
    "cloak_shine": (185, 225, 255, 255),
    "cloak_light": (120, 175, 235, 255),
    "cloak": (65, 118, 188, 255),
    "cloak_dark": (38, 72, 128, 255),
    "cloak_deep": (20, 42, 80, 255),
    "cloak_trim": (155, 205, 255, 255),
    # 白绫素绢
    "paper_shine": (255, 255, 250, 255),
    "paper_light": (250, 245, 230, 255),
    "paper": (235, 225, 205, 255),
    "paper_shadow": (175, 165, 150, 255),
    # 鲜亮朱红飘带与腰封
    "vermilion_shine": (255, 165, 135, 255),
    "vermilion_bright": (255, 75, 65, 255),
    "vermilion": (215, 45, 45, 255),
    "vermilion_dark": (145, 25, 30, 255),
    "vermilion_deep": (85, 15, 20, 255),
    # 碧绿翡翠与水润玉佩
    "jade_shine": (165, 245, 185, 255),
    "jade_light": (105, 205, 135, 255),
    "jade": (58, 148, 92, 255),
    "jade_dark": (32, 88, 58, 255),
    "jade_moss": (28, 68, 46, 255),
    # 闪耀神兵冷钢
    "steel_shine": (255, 255, 255, 255),
    "steel_light": (230, 245, 255, 255),
    "steel": (175, 205, 228, 255),
    "steel_dark": (95, 125, 155, 255),
    # 卡通黄金与铜饰
    "gold_shine": (255, 248, 175, 255),
    "gold_light": (255, 210, 85, 255),
    "gold": (225, 160, 45, 255),
    "gold_dark": (155, 105, 28, 255),
    # 软皮短靴
    "leather_light": (185, 130, 85, 255),
    "leather": (125, 80, 52, 255),
    "leather_dark": (78, 45, 30, 255),
    "leather_deep": (45, 25, 18, 255),
    # 明澈水波
    "water_shine": (185, 245, 250, 255),
    "water_ripple": (125, 215, 225, 255),
    "water_light": (75, 165, 178, 255),
    "water": (38, 108, 125, 255),
    "water_deep": (20, 65, 82, 255),
    "water_abyss": (12, 40, 55, 255),
    # 明亮青石
    "stone_shine": (230, 235, 225, 255),
    "stone_highlight": (205, 210, 195, 255),
    "stone_light": (168, 172, 158, 255),
    "stone": (120, 128, 118, 255),
    "stone_dark": (78, 85, 76, 255),
    # 温暖实木
    "wood_shine": (240, 195, 140, 255),
    "wood_highlight": (210, 155, 105, 255),
    "wood_light": (170, 115, 72, 255),
    "wood": (125, 78, 48, 255),
    "wood_dark": (78, 45, 26, 255),
    "wood_deep": (45, 24, 14, 255),
    # 黛瓦屋顶
    "roof_shine": (155, 185, 205, 255),
    "roof_highlight": (110, 138, 158, 255),
    "roof_light": (72, 95, 112, 255),
    "roof": (42, 58, 74, 255),
    "roof_dark": (24, 34, 46, 255),
    # 暖心灯笼光芒
    "warm_glow": (255, 255, 220, 255),
    "warm_light": (255, 235, 150, 255),
    "warm": (248, 180, 65, 255),
    "warm_dark": (195, 115, 35, 255),
    # 青花瓷
    "porcelain_white": (250, 254, 255, 255),
    "porcelain_shadow": (215, 228, 240, 255),
    "porcelain_blue": (42, 95, 185, 255),
    "porcelain_dark": (24, 52, 110, 255),
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


def _plot(draw, ox, oy, x, y, color):
    draw.point((ox + x, oy + y), fill=color)


def _motion(animation, frame_index, frame_count):
    # Q 版萌系弹跳与晃头晃脑
    if animation == "walk":
        stride = (-4, -2, 2, 4, 2, -2)[frame_index % 6]
        bob = (-1, 1, -1, 1, -1, 1)[frame_index % 6]
        sway = (-2, 0, 2, 0, -2, 0)[frame_index % 6]
        bounce = (0, 1, 0, 1, 0, 1)[frame_index % 6]
        return stride, bob, sway, bounce, 0
    if animation == "dash":
        return (0, 4, 7, 3)[frame_index % 4], (0, -2, 0, 0)[frame_index % 4], 0, 0, 1
    if animation.startswith("attack_") or animation.startswith("skill_"):
        progress = frame_index / max(1, frame_count - 1)
        lean = -3 if progress < 0.34 else 5 if progress < 0.72 else 1
        return lean, 0, 0, 0, 2
    # idle: Q 弹果冻呼吸（DuangDuang 的上下弹跳与发带飘摆）
    bob = (0, 1, 0, -1)[frame_index % 4]
    sway = (0, 1, 0, -1)[frame_index % 4]
    bounce = (0, 1, 0, 0)[frame_index % 4]
    return 0, bob, sway, bounce, 0


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
    stride, bob, sway, bounce, attack = _motion(animation, frame_index, frame_count)
    cx = layout["center"] + (attack if direction == "right" else -attack if direction == "left" else 0)
    cy = bob
    body = draws["body"]
    face = draws["face"]
    hair = draws["hair"]
    outfit = draws["outfit"]
    weapon = draws["weapon"]
    accessory = draws["accessory"]

    # 1. 脚底 Q 弹小阴影（随弹跳微缩放）
    shadow_rx = 10 + (1 if bob < 0 else 0)
    _polygon(accessory, ox, oy, (
        (cx - shadow_rx, 44), (cx + shadow_rx, 44),
        (cx + shadow_rx - 2, 46), (cx - shadow_rx + 2, 46)
    ), P["shadow"])

    left_step = -stride // 2
    right_step = stride // 2

    # 2. Q版短短小腿与萌萌乌皮小黑靴
    for leg_idx, (lx, step) in enumerate(((-5, left_step), (2, right_step))):
        # 胖乎乎小裤腿
        _rect(body, ox, oy, (cx + lx + step, 35 + cy, cx + lx + step + 3, 40 + cy), P["ink_black"])
        _rect(body, ox, oy, (cx + lx + step + 1, 35 + cy, cx + lx + step + 2, 39 + cy), P["cloak_dark"])
        # 圆头乌皮短靴
        _rect(body, ox, oy, (cx + lx + step - 1, 39 + cy, cx + lx + step + 4, 43), P["ink_black"])
        _rect(body, ox, oy, (cx + lx + step, 40 + cy, cx + lx + step + 3, 42), P["leather_dark"])
        _plot(body, ox, oy, cx + lx + step + (2 if leg_idx == 0 else 1), 40 + cy, P["gold_light"])

    # 3. Q版胖乎乎小长袍（三层交领 + 大袖子 + 短小袍摆）
    cloak_sway = layout["cloak"] * (2 + attack) + sway
    # 小袍摆轮廓
    _polygon(outfit, ox, oy, (
        (cx - 9, 23 + cy), (cx + 8, 23 + cy),
        (cx + 12 + cloak_sway, 35 + cy), (cx + 6, 37 + cy),
        (cx, 36 + cy), (cx - 6, 37 + cy), (cx - 12 + cloak_sway, 35 + cy)
    ), P["ink_black"])
    _polygon(outfit, ox, oy, (
        (cx - 8, 24 + cy), (cx + 7, 24 + cy),
        (cx + 10 + cloak_sway, 34 + cy), (cx + 5, 36 + cy),
        (cx, 35 + cy), (cx - 5, 36 + cy), (cx - 10 + cloak_sway, 34 + cy)
    ), P["cloak_dark"])
    _polygon(outfit, ox, oy, (
        (cx - 7, 25 + cy), (cx + 6, 25 + cy),
        (cx + 8 + cloak_sway, 33 + cy), (cx + 3, 35 + cy),
        (cx - 3, 35 + cy), (cx - 8 + cloak_sway, 33 + cy)
    ), P["cloak"])
    _line(outfit, ox, oy, ((cx - 6, 26 + cy), (cx - 3, 32 + cy)), P["cloak_light"], 1)
    _line(outfit, ox, oy, ((cx - 5, 27 + cy), (cx - 2, 31 + cy)), P["cloak_shine"], 1)

    # 交领右衽（雪白内衬）
    _polygon(outfit, ox, oy, (
        (cx - 4, 23 + cy), (cx + 4, 23 + cy),
        (cx + 3, 31 + cy), (cx, 33 + cy), (cx - 3, 31 + cy)
    ), P["ink_black"])
    _polygon(outfit, ox, oy, (
        (cx - 3, 24 + cy), (cx + 3, 24 + cy),
        (cx + 2, 30 + cy), (cx, 32 + cy), (cx - 2, 30 + cy)
    ), P["paper"])
    _line(outfit, ox, oy, ((cx - 2, 24 + cy), (cx + 1, 29 + cy)), P["paper_shine"], 1)

    # 4. Q版圆滚滚小手臂与肉乎乎小手
    arm_shift = attack * (2 if direction in ("right", "down") else -2)
    for arm_idx, (sx, shift) in enumerate(((-12, 0), (6 + arm_shift, arm_shift))):
        # 胖袖筒
        _rect(outfit, ox, oy, (cx + sx, 24 + cy, cx + sx + 6, 31 + cy), P["ink_black"])
        _rect(outfit, ox, oy, (cx + sx + 1, 25 + cy, cx + sx + 5, 30 + cy), P["cloak_dark"])
        _rect(outfit, ox, oy, (cx + sx + 2, 26 + cy, cx + sx + 4, 29 + cy), P["cloak"])
        # 圆圆小肉手（小拳头）
        _ellipse_box = (cx + sx + 1, 30 + cy, cx + sx + 5, 34 + cy)
        outfit.ellipse(_translated(((cx + sx + 1, 30 + cy), (cx + sx + 5, 34 + cy)), ox, oy), fill=P["ink_black"])
        body.ellipse(_translated(((cx + sx + 2, 31 + cy), (cx + sx + 4, 33 + cy)), ox, oy), fill=P["skin"])
        _plot(body, ox, oy, cx + sx + 3, 31 + cy, P["skin_light"])

    # 5. Q版超萌大头（y=3~23）与灵动水汪汪大眼睛
    # 大大圆圆的头脸轮廓（Q版精髓）
    face_rect = (cx - 10, 4 + cy, cx + 9, 23 + cy)
    _polygon(face, ox, oy, (
        (cx - 8, 4 + cy), (cx + 7, 4 + cy),
        (cx + 10, 8 + cy), (cx + 10, 18 + cy),
        (cx + 7, 23 + cy), (cx - 8, 23 + cy),
        (cx - 11, 18 + cy), (cx - 11, 8 + cy)
    ), P["ink_black"])
    _polygon(face, ox, oy, (
        (cx - 7, 5 + cy), (cx + 6, 5 + cy),
        (cx + 9, 9 + cy), (cx + 9, 17 + cy),
        (cx + 6, 22 + cy), (cx - 7, 22 + cy),
        (cx - 10, 17 + cy), (cx - 10, 9 + cy)
    ), P["skin_shadow"])
    _polygon(face, ox, oy, (
        (cx - 6, 6 + cy), (cx + 5, 6 + cy),
        (cx + 8, 9 + cy), (cx + 8, 16 + cy),
        (cx + 5, 21 + cy), (cx - 6, 21 + cy),
        (cx - 9, 16 + cy), (cx - 9, 9 + cy)
    ), P["skin"])
    _rect(face, ox, oy, (cx - 6, 7 + cy, cx + 5, 17 + cy), P["skin_light"])

    # 粉嫩 Q 萌小腮红（两团圆圆红晕）
    _rect(face, ox, oy, (cx - 9, 16 + cy, cx - 7, 18 + cy), P["blush"])
    _rect(face, ox, oy, (cx + 6, 16 + cy, cx + 8, 18 + cy), P["blush"])
    _plot(face, ox, oy, cx - 8, 17 + cy, P["blush_light"])
    _plot(face, ox, oy, cx + 7, 17 + cy, P["blush_light"])

    # 五官（超大水汪汪萌眼 + 灵动眉眼）
    if layout["face"] == "front":
        # 双眼：大大的深色大眼眶 (宽4 x 高6 像素)
        # 左眼
        _rect(face, ox, oy, (cx - 7, 11 + cy, cx - 3, 17 + cy), P["ink_black"])
        _rect(face, ox, oy, (cx - 6, 12 + cy, cx - 4, 16 + cy), P["eye_pupil"])
        _line(face, ox, oy, ((cx - 6, 15 + cy), (cx - 4, 15 + cy)), P["eye_blue"], 1)
        # 亮白双星高光（超有神！）
        _rect(face, ox, oy, (cx - 6, 12 + cy, cx - 5, 13 + cy), P["eye_shine"])
        _plot(face, ox, oy, cx - 4, 15 + cy, P["eye_shine"])
        # 右眼
        _rect(face, ox, oy, (cx + 2, 11 + cy, cx + 6, 17 + cy), P["ink_black"])
        _rect(face, ox, oy, (cx + 3, 12 + cy, cx + 5, 16 + cy), P["eye_pupil"])
        _line(face, ox, oy, ((cx + 3, 15 + cy), (cx + 5, 15 + cy)), P["eye_blue"], 1)
        _rect(face, ox, oy, (cx + 3, 12 + cy, cx + 4, 13 + cy), P["eye_shine"])
        _plot(face, ox, oy, cx + 5, 15 + cy, P["eye_shine"])
        # 灵动小剑眉与俏皮小嘴
        _line(face, ox, oy, ((cx - 6, 9 + cy), (cx - 3, 9 + cy)), P["hair_dark"], 1)
        _line(face, ox, oy, ((cx + 2, 9 + cy), (cx + 5, 9 + cy)), P["hair_dark"], 1)
        _plot(face, ox, oy, cx, 19 + cy, P["skin_deep"])
        _line(face, ox, oy, ((cx - 1, 20 + cy), (cx + 1, 20 + cy)), P["skin_deep"], 1)
    elif layout["face"] == "left":
        # 侧向大萌眼
        _rect(face, ox, oy, (cx - 8, 11 + cy, cx - 4, 17 + cy), P["ink_black"])
        _rect(face, ox, oy, (cx - 7, 12 + cy, cx - 5, 16 + cy), P["eye_pupil"])
        _plot(face, ox, oy, cx - 6, 15 + cy, P["eye_blue"])
        _rect(face, ox, oy, (cx - 7, 12 + cy, cx - 6, 13 + cy), P["eye_shine"])
        _plot(face, ox, oy, cx - 5, 15 + cy, P["eye_shine"])
        _line(face, ox, oy, ((cx - 7, 9 + cy), (cx - 4, 9 + cy)), P["hair_dark"], 1)
        _plot(face, ox, oy, cx - 8, 19 + cy, P["skin_deep"])
    elif layout["face"] == "right":
        _rect(face, ox, oy, (cx + 3, 11 + cy, cx + 7, 17 + cy), P["ink_black"])
        _rect(face, ox, oy, (cx + 4, 12 + cy, cx + 6, 16 + cy), P["eye_pupil"])
        _plot(face, ox, oy, cx + 5, 15 + cy, P["eye_blue"])
        _rect(face, ox, oy, (cx + 4, 12 + cy, cx + 5, 13 + cy), P["eye_shine"])
        _plot(face, ox, oy, cx + 6, 15 + cy, P["eye_shine"])
        _line(face, ox, oy, ((cx + 3, 9 + cy), (cx + 6, 9 + cy)), P["hair_dark"], 1)
        _plot(face, ox, oy, cx + 7, 19 + cy, P["skin_deep"])

    # 6. Q版蓬松发型与萌态呆毛
    # 顶部刘海与两鬓圆发
    _polygon(hair, ox, oy, (
        (cx - 10, 8 + cy), (cx - 6, 2 + cy), (cx + 5, 2 + cy),
        (cx + 9, 8 + cy), (cx + 9, 13 + cy), (cx - 10, 13 + cy)
    ), P["ink_black"])
    _polygon(hair, ox, oy, (
        (cx - 9, 7 + cy), (cx - 5, 3 + cy), (cx + 4, 3 + cy),
        (cx + 8, 7 + cy), (cx + 8, 11 + cy), (cx - 9, 11 + cy)
    ), P["hair_dark"])
    _polygon(hair, ox, oy, (
        (cx - 7, 5 + cy), (cx - 3, 3 + cy), (cx + 3, 3 + cy),
        (cx + 6, 6 + cy), (cx + 3, 8 + cy), (cx - 5, 8 + cy)
    ), P["hair"])
    _line(hair, ox, oy, ((cx - 4, 4 + cy), (cx + 3, 4 + cy)), P["hair_shine"], 1)

    # 萌系呆毛（Ahoge）！自头顶俏皮挑起
    _line(hair, ox, oy, ((cx - 1, 2 + cy), (cx - 3, -1 + cy), (cx - 2, -2 + cy)), P["ink_black"], 2)
    _plot(hair, ox, oy, cx - 2, -2 + cy, P["hair_shine"])

    # 头顶圆圆发髻与金发簪
    _rect(hair, ox, oy, (cx - 3, -1 + cy, cx + 2, 3 + cy), P["ink_black"])
    _rect(hair, ox, oy, (cx - 2, 0 + cy, cx + 1, 2 + cy), P["hair_light"])
    _line(hair, ox, oy, ((cx - 4, 1 + cy), (cx + 3, 1 + cy)), P["gold_light"], 1)

    # 背向发型
    if layout["face"] == "back":
        _rect(hair, ox, oy, (cx - 10, 8 + cy, cx + 9, 21 + cy), P["hair_dark"])
        _rect(hair, ox, oy, (cx - 8, 9 + cy, cx + 7, 19 + cy), P["hair"])
        _line(hair, ox, oy, ((cx - 2, 10 + cy), (cx - 2, 18 + cy)), P["hair_shine"], 1)

    # 7. Q版大大的朱红飘带（随风弹跳晃动）
    ribbon = layout["ribbon"]
    ribbon_sway = sway * 2 + (1 if frame_index % 2 == 1 else 0)
    _rect(accessory, ox, oy, (cx - 3, 1 + cy, cx + 2, 4 + cy), P["vermilion_deep"])
    _polygon(accessory, ox, oy, (
        (cx + ribbon * 2, 2 + cy),
        (cx + ribbon * (11 + ribbon_sway), 5 + cy),
        (cx + ribbon * (15 + ribbon_sway), 12 + cy),
        (cx + ribbon * (7 + ribbon_sway), 11 + cy)
    ), P["ink_black"])
    _polygon(accessory, ox, oy, (
        (cx + ribbon * 3, 3 + cy),
        (cx + ribbon * (10 + ribbon_sway), 6 + cy),
        (cx + ribbon * (13 + ribbon_sway), 11 + cy),
        (cx + ribbon * (7 + ribbon_sway), 10 + cy)
    ), P["vermilion"])
    _line(accessory, ox, oy, (
        (cx + ribbon * 4, 4 + cy), (cx + ribbon * (11 + ribbon_sway), 7 + cy)
    ), P["vermilion_shine"], 1)

    # 8. Q版朱红大腰带与圆滚滚大青玉佩
    _rect(accessory, ox, oy, (cx - 7, 28 + cy, cx + 6, 32 + cy), P["ink_black"])
    _rect(accessory, ox, oy, (cx - 6, 29 + cy, cx + 5, 31 + cy), P["vermilion"])
    _line(accessory, ox, oy, ((cx - 5, 29 + cy), (cx + 4, 29 + cy)), P["vermilion_shine"], 1)
    # 中心金扣
    _rect(accessory, ox, oy, (cx - 1, 28 + cy, cx + 1, 31 + cy), P["gold_light"])
    # 圆圆的青玉佩（大青绿宝石）
    _ellipse_box_jade = (cx + 2, 31 + cy, cx + 7, 36 + cy)
    accessory.ellipse(_translated(((cx + 2, 31 + cy), (cx + 7, 36 + cy)), ox, oy), fill=P["ink_black"])
    accessory.ellipse(_translated(((cx + 3, 32 + cy), (cx + 6, 35 + cy)), ox, oy), fill=P["jade"])
    _plot(accessory, ox, oy, cx + 4, 33 + cy, P["jade_shine"])
    # 晃动的小流苏
    _line(accessory, ox, oy, ((cx + 4, 36 + cy), (cx + 5 + sway, 41 + cy)), P["vermilion_bright"], 2)

    # 9. Q版小宝剑/剑鞘斜跨身后
    scabbard = layout["sword"]
    _line(weapon, ox, oy, ((cx + scabbard * 7, 16 + cy), (cx + scabbard * 14, 38 + cy)), P["ink_black"], 4)
    _line(weapon, ox, oy, ((cx + scabbard * 7, 16 + cy), (cx + scabbard * 13, 37 + cy)), P["leather_deep"], 2)
    _line(weapon, ox, oy, ((cx + scabbard * 6, 17 + cy), (cx + scabbard * 9, 24 + cy)), P["steel_light"], 1)
    _rect(weapon, ox, oy, (cx + scabbard * 5 - 1, 14 + cy, cx + scabbard * 5 + 3, 19 + cy), P["gold_light"])


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
    # Q 版卡通神兵武器：夸张饱满比例 + 鲜明轮廓与光芒
    image = Image.new("RGBA", (FRAME_SIZE, FRAME_SIZE), P["clear"])
    draw = ImageDraw.Draw(image)
    if weapon_id == "weapon_sword":
        # Q 版宝剑：短阔锐利的冷钢剑刃 + 超大青铜兽首金格 + 飘扬朱红长剑穗
        # 宽短锐利大剑身
        draw.line(((16, 35), (37, 10)), fill=P["ink_black"], width=7)
        draw.line(((16, 35), (37, 10)), fill=P["steel_dark"], width=5)
        draw.line(((17, 33), (36, 11)), fill=P["steel"], width=3)
        draw.line(((18, 31), (37, 10)), fill=P["steel_light"], width=2)
        draw.line(((20, 28), (37, 10)), fill=P["steel_shine"], width=1)
        # 超大赤金兽首剑格
        draw.rectangle((11, 27, 24, 35), fill=P["ink_black"])
        draw.rectangle((12, 28, 23, 34), fill=P["gold"])
        draw.rectangle((14, 29, 21, 33), fill=P["gold_light"])
        draw.point((17, 31), fill=P["gold_shine"])
        # 剑柄与大剑穗
        draw.rectangle((13, 35, 19, 43), fill=P["ink_black"])
        draw.rectangle((14, 36, 18, 42), fill=P["vermilion_deep"])
        draw.line((14, 38, 18, 38), fill=P["gold_light"], width=1)
        draw.line((14, 41, 18, 41), fill=P["gold_light"], width=1)
        # 飘扬大红剑穗
        draw.polygon(((16, 43), (23, 47), (20, 44)), fill=P["vermilion_bright"])
    elif weapon_id == "weapon_gauntlets":
        # Q 版玄铁猫爪拳套：超大浑圆拳锋 + 锋利精钢爪刃 + 赤金猫爪肉垫
        draw.rectangle((15, 18, 35, 38), fill=P["ink_black"])
        draw.rectangle((17, 20, 33, 36), fill=P["steel_dark"])
        draw.rectangle((18, 21, 32, 35), fill=P["steel"])
        draw.line((19, 22, 31, 22), fill=P["steel_light"], width=2)
        # 三道锐利合金爪刃
        for x in (19, 24, 29):
            draw.polygon(((x, 15), (x + 2, 19), (x - 1, 19)), fill=P["ink_black"])
            draw.polygon(((x, 16), (x + 1, 19), (x, 19)), fill=P["steel_shine"])
        # 正面金色肉垫/虎头浮雕
        draw.ellipse((22, 26, 28, 32), fill=P["gold_light"])
        draw.point((25, 29), fill=P["gold_shine"])
    elif weapon_id == "weapon_dart":
        # Q 版超级四芒手里剑：大大的旋转四角星镖 + 中心红宝石
        cx, cy = 26, 26
        # 四角飞刃
        draw.polygon(((cx, cy - 14), (cx + 5, cy - 4), (cx + 14, cy), (cx + 4, cy + 5),
                      (cx, cy + 14), (cx - 5, cy + 4), (cx - 14, cy), (cx - 4, cy - 5)), fill=P["ink_black"])
        draw.polygon(((cx, cy - 12), (cx + 4, cy - 3), (cx + 12, cy), (cx + 3, cy + 4),
                      (cx, cy + 12), (cx - 4, cy + 3), (cx - 12, cy), (cx - 3, cy - 4)), fill=P["steel_dark"])
        draw.polygon(((cx, cy - 10), (cx + 3, cy - 2), (cx + 10, cy), (cx + 2, cy + 3),
                      (cx, cy + 10), (cx - 3, cy + 2), (cx - 10, cy), (cx - 2, cy - 3)), fill=P["steel_light"])
        draw.line(((cx, cy - 10), (cx, cy + 10)), fill=P["steel_shine"], width=1)
        draw.line(((cx - 10, cy), (cx + 10, cy)), fill=P["steel_shine"], width=1)
        # 中心红宝石
        draw.ellipse((cx - 3, cy - 3, cx + 3, cy + 3), fill=P["vermilion_bright"])
        draw.point((cx - 1, cy - 1), fill=P["paper_shine"])
    else:
        raise ValueError("unknown weapon ID: " + weapon_id)
    return image


def _draw_dense_actor(actor_id):
    size = 16 if actor_id == "mvp_lost_pouch" else 48
    image = Image.new("RGBA", (size, size), P["clear"])
    draw = ImageDraw.Draw(image)
    if actor_id == "mvp_lost_pouch":
        # 16x16 Q 版圆滚滚胖锦囊：像果冻布丁一样可爱的大红小福袋
        draw.ellipse((1, 4, 14, 15), fill=P["ink_black"])
        draw.ellipse((2, 5, 13, 14), fill=P["vermilion"])
        draw.ellipse((3, 6, 12, 13), fill=P["vermilion_bright"])
        draw.point((4, 7), fill=P["vermilion_shine"])
        # 大大金色蝴蝶结抽绳
        draw.rectangle((4, 3, 11, 6), fill=P["gold_light"])
        draw.point((5, 4), fill=P["gold_shine"])
        draw.point((10, 4), fill=P["gold_shine"])
        # 挂着圆圆的大绿翡翠宝石珠
        draw.ellipse((6, 12, 9, 15), fill=P["jade_shine"])
        return image
    if actor_id == "mvp_innkeeper":
        # 48x48 Q 版福态老赵掌柜：圆滚滚大笑脸 + 弯弯眯眯眼(^^) + 胖嘟嘟青花大茶壶
        # 脚底软影
        draw.ellipse((10, 40, 38, 47), fill=P["shadow"])
        # 胖乎乎小短腿
        draw.rectangle((15, 36, 21, 42), fill=P["ink_black"])
        draw.rectangle((27, 36, 33, 42), fill=P["ink_black"])
        draw.rectangle((16, 39, 20, 42), fill=P["leather_deep"])
        draw.rectangle((28, 39, 32, 42), fill=P["leather_deep"])
        # 圆滚滚酱色员外肚皮与小围裙
        draw.ellipse((8, 20, 40, 38), fill=P["ink_black"])
        draw.ellipse((9, 21, 39, 37), fill=P["wood_dark"])
        # 白净小围裙
        draw.ellipse((14, 23, 34, 36), fill=P["paper"])
        draw.line((15, 27, 33, 27), fill=P["paper_shadow"], width=1)
        # 腰间一串金色大铜钱钥匙
        draw.ellipse((10, 27, 16, 33), fill=P["gold_light"])
        draw.point((13, 30), fill=P["ink_black"])
        # 胖乎乎大圆脸（超级和蔼生动！）
        draw.ellipse((10, 5, 38, 24), fill=P["ink_black"])
        draw.ellipse((11, 6, 37, 23), fill=P["skin"])
        draw.ellipse((12, 7, 36, 21), fill=P["skin_light"])
        # 圆圆员外软帽 + 亮绿大翡翠帽正
        draw.polygon(((11, 9), (16, 2), (32, 2), (37, 9)), fill=P["ink_black"])
        draw.polygon(((13, 8), (17, 3), (31, 3), (35, 8)), fill=P["roof_dark"])
        draw.line((18, 4, 30, 4), fill=P["roof_shine"], width=1)
        draw.ellipse((22, 4, 26, 8), fill=P["jade_shine"])
        # 弯弯的月牙眯眯笑眼 (^^)
        draw.arc((15, 11, 21, 16), 180, 360, fill=P["ink_black"], width=2)
        draw.arc((27, 11, 33, 16), 180, 360, fill=P["ink_black"], width=2)
        # 大大红苹果脸蛋（两团可爱腮红）
        draw.ellipse((12, 15, 17, 19), fill=P["blush"])
        draw.ellipse((31, 15, 36, 19), fill=P["blush"])
        # 喜气洋洋的八字胡与笑开怀的小嘴
        draw.line(((19, 17), (22, 16)), fill=P["hair_dark"], width=2)
        draw.line(((26, 16), (29, 17)), fill=P["hair_dark"], width=2)
        draw.arc((21, 17, 27, 21), 0, 180, fill=P["vermilion_dark"], width=2)
        # 手捧圆滚滚景德镇青花大茶壶
        draw.ellipse((30, 21, 42, 31), fill=P["ink_black"])
        draw.ellipse((31, 22, 41, 30), fill=P["porcelain_white"])
        draw.line((33, 26, 39, 26), fill=P["porcelain_blue"], width=2)
        draw.arc((33, 17, 39, 23), 180, 360, fill=P["wood_dark"], width=2)
        return image
    if actor_id not in ("mvp_bandit_a", "mvp_bandit_b"):
        raise ValueError("unknown dense actor: " + actor_id)
    # 48x48 Q 版奶凶水匪小头目：大头巾 + 气鼓鼓怒眼 + 扛着超大九环大刀 (A) / 开山大板斧 (B)
    headband_color = P["vermilion_bright"] if actor_id == "mvp_bandit_a" else P["jade_light"]
    # 地面软影
    draw.ellipse((10, 40, 38, 47), fill=P["shadow"])
    # 短短小粗腿与草鞋
    draw.rectangle((14, 36, 20, 42), fill=P["ink_black"])
    draw.rectangle((28, 36, 34, 42), fill=P["ink_black"])
    draw.rectangle((15, 38, 19, 42), fill=P["paper_shadow"])
    draw.rectangle((29, 38, 33, 42), fill=P["paper_shadow"])
    # 敞襟小短打
    draw.ellipse((10, 22, 38, 38), fill=P["ink_black"])
    draw.ellipse((11, 23, 37, 37), fill=P["leather_deep"])
    draw.polygon(((18, 23), (30, 23), (24, 33)), fill=P["skin"])
    # 奶凶大圆脑袋
    draw.ellipse((10, 5, 38, 24), fill=P["ink_black"])
    draw.ellipse((11, 6, 37, 23), fill=P["skin"])
    draw.ellipse((12, 7, 36, 21), fill=P["skin_light"])
    # 大大头巾与侧边飘扬大结
    draw.polygon(((10, 9), (14, 2), (34, 2), (38, 9)), fill=P["ink_black"])
    draw.polygon(((12, 8), (15, 3), (33, 3), (36, 8)), fill=headband_color)
    draw.polygon(((34, 4), (43, 1), (40, 8)), fill=headband_color)
    draw.polygon(((35, 6), (44, 10), (37, 10)), fill=headband_color)
    # 奶凶怒眉与大圆眼 (> < / 气鼓鼓)
    draw.line(((14, 10), (19, 13)), fill=P["ink_black"], width=2)
    draw.line(((29, 13), (34, 10)), fill=P["ink_black"], width=2)
    # 大眼睛
    draw.ellipse((16, 12, 21, 17), fill=P["ink_black"])
    draw.ellipse((27, 12, 32, 17), fill=P["ink_black"])
    draw.point((18, 13), fill=P["steel_shine"])
    draw.point((29, 13), fill=P["steel_shine"])
    # 气鼓鼓腮帮子与小怒嘴
    draw.ellipse((12, 16, 16, 19), fill=P["blush"])
    draw.ellipse((32, 16, 36, 19), fill=P["blush"])
    draw.line(((22, 19), (26, 19)), fill=P["ink_black"], width=2)
    # 专属超大 Q 版武器：扛在肩上的超大九环大刀 (A) / 巨大双刃开山斧 (B)
    if actor_id == "mvp_bandit_a":
        # 夸张超大九环刀
        draw.line(((30, 32), (45, 6)), fill=P["ink_black"], width=7)
        draw.line(((30, 32), (45, 6)), fill=P["steel_dark"], width=5)
        draw.line(((31, 30), (44, 7)), fill=P["steel_light"], width=3)
        draw.line(((33, 28), (44, 8)), fill=P["steel_shine"], width=1)
        # 刀背 3 个大大的金色闪亮圆环
        for hx, hy in ((37, 17), (40, 12), (43, 7)):
            draw.ellipse((hx - 2, hy - 2, hx + 2, hy + 2), fill=P["ink_black"])
            draw.ellipse((hx - 1, hy - 1, hx + 1, hy + 1), fill=P["gold_light"])
            draw.point((hx, hy), fill=P["gold_shine"])
    else:
        # 夸张超大双刃板斧
        draw.line(((30, 35), (42, 8)), fill=P["wood_deep"], width=4)
        draw.polygon(((34, 6), (46, 2), (44, 18), (32, 15)), fill=P["ink_black"])
        draw.polygon(((35, 7), (45, 3), (43, 17), (33, 14)), fill=P["steel"])
        draw.line(((45, 3), (43, 17)), fill=P["steel_shine"], width=2)
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


def _new_module(size, opaque=False):
    return Image.new("RGBA", (size, size), P["jade_moss"] if opaque else P["clear"])


def _draw_town_module(name):
    """Draw one authored, composable town module with chibi stylized wuxia aesthetics."""
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
        # 16x16 Q版卡通青石路面：圆角大块石板 + 萌萌小青苔点 + 清晰明亮石纹
        draw.rectangle((0, 0, 15, 15), fill=P["stone"])
        if name == "road_a":
            # 横向大块卡通石板
            draw.line((0, 7, 15, 7), fill=P["ink_black"], width=1)
            draw.line((7, 0, 7, 7), fill=P["ink_black"], width=1)
            draw.line((11, 7, 11, 15), fill=P["ink_black"], width=1)
            # 石板顶沿亮线
            draw.line((1, 1, 6, 1), fill=P["stone_shine"], width=1)
            draw.line((8, 1, 14, 1), fill=P["stone_shine"], width=1)
            draw.line((1, 8, 10, 8), fill=P["stone_shine"], width=1)
            # 萌系小青苔球
            draw.ellipse((6, 6, 8, 8), fill=P["jade_light"])
        elif name == "road_b":
            # 纵向大块卡通石板
            draw.line((7, 0, 7, 15), fill=P["ink_black"], width=1)
            draw.line((0, 7, 7, 7), fill=P["ink_black"], width=1)
            draw.line((7, 11, 15, 11), fill=P["ink_black"], width=1)
            draw.line((1, 1, 1, 6), fill=P["stone_shine"], width=1)
            draw.line((8, 1, 8, 10), fill=P["stone_shine"], width=1)
            draw.ellipse((6, 10, 8, 12), fill=P["jade_light"])
        else:
            # 弯道交界石板
            draw.arc((-4, -4, 20, 20), 0, 90, fill=P["stone_shine"], width=2)
            draw.arc((-2, -2, 18, 18), 0, 90, fill=P["ink_black"], width=1)
        return image
    if name.startswith("water"):
        # 16x16 Q版卡通碧波水面：明亮青绿碧水 + 可爱圆润水波纹
        base = P["water_deep"] if name == "water_deep" else P["water"]
        draw.rectangle((0, 0, 15, 15), fill=base)
        if name == "water_flow":
            # 萌系圆弧水浪
            draw.arc((1, 2, 7, 6), 0, 180, fill=P["water_shine"], width=1)
            draw.arc((8, 8, 14, 12), 0, 180, fill=P["water_shine"], width=1)
            draw.arc((3, 11, 9, 15), 0, 180, fill=P["water_light"], width=1)
        elif name == "water_reflection":
            # 倒映的暖金小光斑
            draw.ellipse((4, 4, 8, 8), fill=P["warm_glow"])
            draw.ellipse((9, 9, 13, 13), fill=P["warm_light"])
        else:
            draw.arc((2, 5, 8, 9), 0, 180, fill=P["water_light"], width=1)
            draw.arc((7, 10, 13, 14), 0, 180, fill=P["water_ripple"], width=1)
        return image
    if name.startswith("shore"):
        # 16x16 驳岸青草与石阶
        draw.rectangle((0, 0, 15, 15), fill=P["jade_moss"])
        draw.polygon(((0, 8), (15, 4), (15, 15), (0, 15)), fill=P["ink_black"])
        draw.polygon(((0, 9), (15, 5), (15, 15), (0, 15)),
                     fill=P["stone"] if name == "shore_stone" else P["water"])
        draw.line((0, 9, 15, 5), fill=P["stone_shine"], width=1)
        # 萌萌圆圆的小草芽
        draw.ellipse((2, 2, 5, 5), fill=P["jade_shine"])
        draw.ellipse((8, 1, 11, 4), fill=P["jade_shine"])
        return image
    if name == "inn_roof":
        # 64x64 Q版卡通大飞檐屋顶：圆润歇山顶 + 饱满翘角 + 鲜明瓦当
        draw.polygon(((2, 38), (16, 6), (48, 6), (62, 38), (56, 46), (8, 46)), fill=P["ink_black"])
        draw.polygon(((5, 36), (18, 9), (46, 9), (59, 36), (54, 43), (10, 43)), fill=P["roof"])
        # 圆润的大瓦垄
        for y in range(12, 40, 6):
            draw.line((10, y, 54, y), fill=P["roof_light"], width=2)
            draw.line((10, y - 1, 54, y - 1), fill=P["roof_shine"], width=1)
        # 飞檐大金角
        draw.line((2, 38, 8, 35), fill=P["gold_shine"], width=2)
        draw.line((62, 38, 56, 35), fill=P["gold_shine"], width=2)
        return image
    if name == "inn_wall":
        # 64x64 Q版卡通白墙：明亮暖白墙面 + 大大的圆角雕花暖窗
        draw.rectangle((2, 14, 61, 62), fill=P["ink_black"])
        draw.rectangle((5, 17, 58, 59), fill=P["paper"])
        # 两扇大大的暖光卡通木窗
        for wx in (12, 36):
            draw.rectangle((wx, 25, wx + 15, 43), fill=P["ink_black"])
            draw.rectangle((wx + 2, 27, wx + 13, 41), fill=P["wood_dark"])
            draw.rectangle((wx + 3, 28, wx + 12, 40), fill=P["warm_light"])
            draw.rectangle((wx + 5, 30, wx + 10, 38), fill=P["warm_glow"])
            # 大十字窗格
            draw.line((wx + 7, 27, wx + 7, 41), fill=P["wood_deep"], width=2)
            draw.line((wx + 2, 34, wx + 13, 34), fill=P["wood_deep"], width=2)
        # 墙脚大石块
        draw.rectangle((5, 54, 58, 59), fill=P["stone"])
        draw.line((5, 54, 58, 54), fill=P["stone_shine"], width=1)
        return image
    if name == "inn_door":
        # 32x32 Q版卡通客栈大门：饱满圆润红木门 + 大大的金色兽首门环
        draw.rectangle((1, 0, 30, 31), fill=P["ink_black"])
        draw.rectangle((4, 3, 27, 30), fill=P["wood"])
        draw.line((15, 3, 15, 30), fill=P["ink_black"], width=2)
        # 大大金色圆门环
        for hx in (8, 20):
            draw.ellipse((hx, 13, hx + 4, 17), fill=P["gold_light"])
            draw.point((hx + 2, 15), fill=P["gold_shine"])
        return image
    if name == "inn_sign":
        # 32x32 Q版卡通招牌：圆角酒幌木架 + 飘扬大红“酒”字小旗
        draw.rectangle((2, 2, 29, 29), fill=P["ink_black"])
        draw.rectangle((4, 4, 27, 10), fill=P["wood"])
        # 大红酒旗
        draw.polygon(((6, 11), (25, 11), (21, 26), (10, 26)), fill=P["vermilion"])
        draw.line(((6, 11), (25, 11)), fill=P["vermilion_shine"], width=2)
        # 卡通大白圈“酒”
        draw.ellipse((12, 14, 19, 21), fill=P["paper_shine"])
        draw.point((15, 17), fill=P["ink_black"])
        return image
    if name == "bridge":
        # 48x48 Q版卡通青石拱桥：圆滚滚大彩虹拱桥 + 圆球望柱 + 萌萌石板
        draw.polygon(((0, 23), (8, 9), (39, 9), (47, 23), (44, 38), (3, 38)), fill=P["ink_black"])
        draw.polygon(((3, 23), (10, 11), (37, 11), (44, 23), (41, 35), (6, 35)), fill=P["stone"])
        # 大大的圆半月桥拱
        draw.arc((10, 18, 37, 44), 180, 360, fill=P["ink_black"], width=6)
        draw.arc((12, 20, 35, 42), 180, 360, fill=P["water_abyss"], width=3)
        # 桥面亮线与圆球石雕望柱
        draw.line((8, 11, 39, 11), fill=P["stone_shine"], width=2)
        for x in (8, 18, 28, 38):
            # 圆滚滚卡通石球望柱
            draw.ellipse((x - 2, 5, x + 3, 10), fill=P["ink_black"])
            draw.ellipse((x - 1, 6, x + 2, 9), fill=P["stone_shine"])
        return image
    if name == "boat":
        # 48x48 Q版卡通乌篷船：圆滚滚小胖木船 + 可爱圆弧竹篾篷 + 小白旗
        draw.ellipse((2, 26, 45, 42), fill=P["ink_black"])
        draw.ellipse((4, 28, 43, 40), fill=P["wood"])
        draw.line((5, 29, 42, 29), fill=P["wood_shine"], width=1)
        # 圆滚滚胖胖竹席篷
        draw.ellipse((14, 12, 34, 32), fill=P["ink_black"])
        draw.ellipse((16, 14, 32, 30), fill=P["wood_deep"])
        draw.line((18, 20, 30, 20), fill=P["wood_light"], width=2)
        # 船头小旗
        draw.polygon(((33, 12), (43, 17), (33, 22)), fill=P["paper_shine"])
        return image
    if name == "bollard":
        # 16x16 圆滚滚小木桩
        draw.ellipse((3, 2, 12, 14), fill=P["ink_black"])
        draw.ellipse((4, 3, 11, 13), fill=P["wood"])
        draw.ellipse((4, 3, 11, 6), fill=P["wood_shine"])
        return image
    if name == "lantern":
        # 16x16 Q版圆滚滚大红灯笼：像红苹果一样饱满可爱 + 发散暖光
        draw.ellipse((1, 2, 14, 13), fill=P["ink_black"])
        draw.ellipse((2, 3, 13, 12), fill=P["vermilion"])
        draw.ellipse((4, 5, 11, 10), fill=P["warm_light"])
        draw.ellipse((6, 6, 9, 9), fill=P["warm_glow"])
        # 小红流苏
        draw.line((7, 13, 8, 15), fill=P["vermilion_bright"], width=2)
        return image
    if name == "crate":
        # 16x16 卡通实木宝箱/货箱
        draw.rectangle((1, 2, 14, 14), fill=P["ink_black"])
        draw.rectangle((2, 3, 13, 13), fill=P["wood"])
        draw.rectangle((5, 6, 10, 10), fill=P["gold_light"])
        draw.point((7, 8), fill=P["gold_shine"])
        return image
    if name in ("willow_near", "willow_far"):
        # 蓬松棉花糖般的 Q 版垂柳（鲜亮翠绿 + 层次分明大叶球）
        draw.line((size // 2, 2, size // 2 - 4, size - 4), fill=P["wood_deep"], width=4)
        # 3 团圆滚滚蓬松大柳叶球
        for ox, oy, r, color in (
            (size // 2 - 8, 14, 10, P["jade_light"]),
            (size // 2 + 8, 16, 11, P["jade_shine"]),
            (size // 2, 8, 9, P["jade_shine"]),
        ):
            draw.ellipse((ox - r, oy - r, ox + r, oy + r), fill=P["ink_black"])
            draw.ellipse((ox - r + 1, oy - r + 1, ox + r - 1, oy + r - 1), fill=color)
        # 柔柔飘垂的小柳丝
        for lx in range(size // 2 - 14, size // 2 + 15, 6):
            draw.line((lx, 20, lx + 1, size - 8), fill=P["jade_shine"], width=1)
        return image
    if name == "roof_trim":
        # 32x32 前景大飞檐
        draw.polygon(((0, 0), (31, 0), (31, 14), (0, 14)), fill=P["ink_black"])
        draw.polygon(((2, 1), (29, 1), (29, 11), (2, 11)), fill=P["roof"])
        draw.line((2, 2, 29, 2), fill=P["roof_shine"], width=2)
        return image
    raise AssertionError("unreachable module: " + name)


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
    """Draw interior modules with cute chibi stylized wuxia aesthetics."""
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
        # 16x16 温暖明亮实木地板
        draw.rectangle((0, 0, 15, 15), fill=P["wood"])
        draw.line((0, 0, 15, 0), fill=P["wood_shine"], width=1)
        draw.line((0, 15, 15, 15), fill=P["ink_black"], width=1)
        draw.line((7, 0, 7, 15), fill=P["wood_dark"], width=1)
        return image
    if name == "entry_stone":
        draw.rectangle((0, 0, 15, 15), fill=P["stone"])
        draw.line((0, 0, 15, 0), fill=P["stone_shine"], width=1)
        draw.line((0, 15, 15, 15), fill=P["ink_black"], width=1)
        return image
    if name == "rug":
        # 16x16 迎宾大红金丝小地毯
        draw.rectangle((0, 0, 15, 15), fill=P["vermilion"])
        draw.rectangle((2, 2, 13, 13), fill=P["gold_light"])
        draw.rectangle((4, 4, 11, 11), fill=P["vermilion_bright"])
        return image
    if name == "counter":
        # 64x64 Q版掌柜红木大柜台：翻开的大账本 + 圆滚滚青花笔筒
        draw.rectangle((2, 16, 61, 58), fill=P["ink_black"])
        draw.rectangle((5, 19, 58, 38), fill=P["wood_shine"])
        draw.rectangle((5, 39, 58, 55), fill=P["wood"])
        # 翻开的大账本
        draw.rectangle((16, 10, 42, 21), fill=P["ink_black"])
        draw.rectangle((18, 12, 40, 19), fill=P["paper_shine"])
        draw.line((20, 14, 38, 14), fill=P["ink_black"], width=1)
        draw.line((20, 17, 38, 17), fill=P["ink_black"], width=1)
        # 圆圆青花毛笔筒
        draw.ellipse((45, 11, 53, 21), fill=P["porcelain_white"])
        draw.line((46, 16, 52, 16), fill=P["porcelain_blue"], width=2)
        return image
    if name == "counter_lantern":
        # 32x32 柜台大暖光灯笼
        draw.ellipse((6, 6, 25, 25), fill=P["ink_black"])
        draw.ellipse((8, 8, 23, 23), fill=P["vermilion"])
        draw.ellipse((11, 11, 20, 20), fill=P["warm_light"])
        draw.ellipse((13, 13, 18, 18), fill=P["warm_glow"])
        return image
    if name == "table":
        # 48x48 Q版圆滚滚实木大圆桌 + 胖胖青花大茶壶与茶碗
        draw.ellipse((2, 8, 45, 36), fill=P["ink_black"])
        draw.ellipse((4, 10, 43, 34), fill=P["wood"])
        draw.line((8, 14, 39, 14), fill=P["wood_shine"], width=2)
        # 桌面大大的胖青花茶壶
        draw.ellipse((19, 16, 29, 26), fill=P["porcelain_white"])
        draw.line((21, 21, 27, 21), fill=P["porcelain_blue"], width=2)
        # 两只可爱白瓷小茶碗
        draw.ellipse((12, 20, 17, 25), fill=P["porcelain_white"])
        draw.ellipse((31, 20, 36, 25), fill=P["porcelain_white"])
        return image
    if name == "stove":
        # 48x48 厨房青石火灶 + 旺盛暖焰
        draw.rectangle((3, 4, 44, 44), fill=P["ink_black"])
        draw.rectangle((6, 7, 41, 41), fill=P["stone"])
        draw.rectangle((12, 18, 35, 41), fill=P["ink_black"])
        # 熊熊大火苗
        draw.polygon(((16, 36), (24, 15), (32, 36)), fill=P["vermilion_bright"])
        draw.polygon(((19, 35), (24, 21), (29, 35)), fill=P["warm_glow"])
        return image
    if name == "stairs":
        # 64x64 实木楼梯
        draw.rectangle((2, 4, 62, 62), fill=P["ink_black"])
        for index in range(6):
            y = 8 + index * 8
            draw.rectangle((6 + index * 3, y, 58, y + 7), fill=P["wood"])
            draw.line((6 + index * 3, y, 58, y), fill=P["wood_shine"], width=1)
        return image
    if name == "kitchen_wall":
        # 64x64 厨房木墙 + 挂着可爱大红辣椒串
        draw.rectangle((1, 1, 62, 62), fill=P["ink_black"])
        draw.rectangle((4, 4, 59, 59), fill=P["wood_dark"])
        # 大红辣椒串
        for py in (12, 18, 24, 30):
            draw.ellipse((43, py, 49, py + 5), fill=P["vermilion_bright"])
        return image
    if name == "window_light":
        # 32x32 雕花木窗与暖阳
        draw.rectangle((1, 1, 30, 30), fill=P["ink_black"])
        draw.rectangle((4, 4, 27, 27), fill=P["warm_light"])
        draw.rectangle((7, 7, 24, 24), fill=P["warm_glow"])
        draw.line((15, 4, 15, 27), fill=P["wood_dark"], width=2)
        draw.line((4, 15, 27, 15), fill=P["wood_dark"], width=2)
        return image
    if name == "north_door":
        # 32x32 通往后院实木小门
        draw.rectangle((2, 1, 29, 31), fill=P["ink_black"])
        draw.rectangle((5, 4, 26, 30), fill=P["wood"])
        draw.ellipse((18, 16, 22, 20), fill=P["gold_light"])
        return image
    if name == "shelf":
        # 32x32 博古酒架：整整齐齐圆滚滚的红布封泥胖酒坛
        draw.rectangle((2, 2, 29, 30), fill=P["ink_black"])
        draw.rectangle((4, 4, 27, 28), fill=P["wood"])
        draw.line((4, 15, 27, 15), fill=P["wood_shine"], width=2)
        # 上下两排圆滚滚的小胖酒坛
        for x, y, color in ((7, 7, P["vermilion"]), (17, 7, P["jade"]),
                            (10, 18, P["vermilion_bright"]), (20, 18, P["gold_light"])):
            draw.ellipse((x, y, x + 6, y + 6), fill=P["ink_black"])
            draw.ellipse((x + 1, y + 1, x + 5, y + 5), fill=color)
            draw.line((x + 1, y + 1, x + 5, y + 1), fill=P["paper_shine"], width=1)
        return image
    if name == "foreground_beam":
        # 64x64 前景雕花实木大横梁
        draw.rectangle((0, 0, 14, 63), fill=P["ink_black"])
        draw.rectangle((2, 0, 11, 63), fill=P["wood_deep"])
        draw.polygon(((10, 0), (63, 0), (63, 14), (22, 14)), fill=P["ink_black"])
        draw.polygon(((12, 1), (63, 1), (63, 11), (24, 11)), fill=P["wood"])
        draw.line((24, 11, 63, 11), fill=P["wood_shine"], width=1)
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
