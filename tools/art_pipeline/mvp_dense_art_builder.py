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
    # 墨线勾边与深影（带冷紫/冷蓝调）
    "ink_black": (10, 11, 18, 255),
    "ink_deep": (18, 20, 32, 255),
    "ink": (28, 30, 48, 255),
    "ink_light": (48, 52, 76, 255),
    "shadow": (14, 16, 28, 130),
    # 儒侠发丝（玄墨发色 + 雅致紫灰高光）
    "hair_shine": (128, 118, 142, 255),
    "hair_light": (82, 72, 94, 255),
    "hair": (42, 36, 50, 255),
    "hair_dark": (24, 20, 30, 255),
    # 东方武侠肤色（细腻 5 阶色相偏移）
    "skin_shine": (255, 235, 210, 255),
    "skin_light": (248, 205, 168, 255),
    "skin": (220, 165, 125, 255),
    "skin_shadow": (168, 105, 78, 255),
    "skin_deep": (120, 68, 52, 255),
    # 儒侠青灰外袍（月白高光 -> 青灰 -> 黛蓝深影）
    "cloak_shine": (160, 200, 245, 255),
    "cloak_light": (105, 150, 208, 255),
    "cloak": (55, 95, 158, 255),
    "cloak_dark": (32, 58, 108, 255),
    "cloak_deep": (18, 34, 68, 255),
    "cloak_trim": (135, 175, 230, 255),
    # 白绫中单与内衬素绢
    "paper_shine": (255, 252, 240, 255),
    "paper_light": (245, 238, 220, 255),
    "paper": (225, 215, 195, 255),
    "paper_shadow": (165, 155, 140, 255),
    "paper_dark": (115, 105, 95, 255),
    # 朱红发带与腰封流苏（鲜活武侠点缀）
    "vermilion_shine": (255, 150, 120, 255),
    "vermilion_bright": (245, 85, 65, 255),
    "vermilion": (195, 50, 48, 255),
    "vermilion_dark": (125, 28, 32, 255),
    "vermilion_deep": (75, 16, 20, 255),
    # 佩玉与青苔翡翠（温润碧玉）
    "jade_shine": (145, 215, 165, 255),
    "jade_light": (95, 165, 115, 255),
    "jade": (55, 115, 78, 255),
    "jade_dark": (32, 72, 50, 255),
    "jade_moss": (24, 52, 38, 255),
    # 冷钢与神兵刃芒
    "steel_shine": (255, 255, 255, 255),
    "steel_light": (225, 242, 255, 255),
    "steel": (165, 192, 215, 255),
    "steel_dark": (85, 110, 138, 255),
    "steel_deep": (42, 58, 80, 255),
    # 青铜与赤金（吞口、门环、铜钱）
    "gold_shine": (255, 242, 160, 255),
    "gold_light": (245, 195, 80, 255),
    "gold": (198, 142, 45, 255),
    "gold_dark": (138, 92, 28, 255),
    "gold_deep": (80, 52, 16, 255),
    # 皮革与长靴
    "leather_light": (165, 115, 75, 255),
    "leather": (110, 70, 48, 255),
    "leather_dark": (68, 40, 28, 255),
    "leather_deep": (40, 22, 16, 255),
    # 江南水波（清澈碧水与深水倒影）
    "water_shine": (165, 230, 235, 255),
    "water_ripple": (115, 195, 205, 255),
    "water_light": (68, 145, 155, 255),
    "water": (32, 92, 105, 255),
    "water_deep": (16, 52, 68, 255),
    "water_abyss": (10, 32, 45, 255),
    # 青石与石板路
    "stone_shine": (215, 218, 205, 255),
    "stone_highlight": (185, 188, 172, 255),
    "stone_light": (148, 152, 138, 255),
    "stone": (105, 112, 102, 255),
    "stone_dark": (68, 74, 66, 255),
    "stone_deep": (40, 44, 38, 255),
    # 温润实木与红木家具
    "wood_shine": (225, 178, 125, 255),
    "wood_highlight": (195, 142, 92, 255),
    "wood_light": (155, 102, 60, 255),
    "wood": (110, 68, 40, 255),
    "wood_dark": (68, 38, 22, 255),
    "wood_deep": (38, 20, 12, 255),
    # 黛瓦屋顶
    "roof_shine": (135, 165, 185, 255),
    "roof_highlight": (95, 122, 140, 255),
    "roof_light": (62, 82, 98, 255),
    "roof": (34, 48, 62, 255),
    "roof_dark": (20, 28, 38, 255),
    # 暖阳烛火与灯笼光晕
    "warm_glow": (255, 250, 210, 255),
    "warm_light": (255, 225, 135, 255),
    "warm": (238, 165, 60, 255),
    "warm_dark": (175, 102, 32, 255),
    "warm_deep": (105, 55, 18, 255),
    # 景德镇青花瓷
    "porcelain_white": (248, 252, 255, 255),
    "porcelain_shadow": (205, 218, 230, 255),
    "porcelain_blue": (38, 80, 155, 255),
    "porcelain_dark": (20, 45, 95, 255),
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
    if animation == "walk":
        stride = (-3, -1, 1, 3, 1, -1)[frame_index % 6]
        bob = (0, 1, 0, -1, 0, 1)[frame_index % 6]
        sway = (-1, 0, 1, 0, -1, 0)[frame_index % 6]
        return stride, bob, sway, 0
    if animation == "dash":
        return (0, 3, 5, 2)[frame_index % 4], (0, -1, 0, 0)[frame_index % 4], 0, 1
    if animation.startswith("attack_") or animation.startswith("skill_"):
        progress = frame_index / max(1, frame_count - 1)
        lean = -2 if progress < 0.34 else 4 if progress < 0.72 else 1
        return lean, 0, 0, 2
    # idle: 4 帧柔和呼吸与微风吹拂
    bob = (0, 1, 0, -1)[frame_index % 4]
    sway = (0, 1, 0, -1)[frame_index % 4]
    return 0, bob, sway, 0


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
    stride, bob, sway, attack = _motion(animation, frame_index, frame_count)
    cx = layout["center"] + (attack if direction == "right" else -attack if direction == "left" else 0)
    cy = bob
    body = draws["body"]
    face = draws["face"]
    hair = draws["hair"]
    outfit = draws["outfit"]
    weapon = draws["weapon"]
    accessory = draws["accessory"]

    # 1. 角色脚下软影（随呼吸微缩）
    shadow_w = 11 + (1 if bob == 0 else 0)
    _polygon(accessory, ox, oy, (
        (cx - shadow_w, 44), (cx + shadow_w, 44),
        (cx + shadow_w - 3, 46), (cx - shadow_w + 3, 46)
    ), P["shadow"])

    left_step = -stride // 2
    right_step = stride // 2

    # 2. 下身修长行裤与乌皮皂靴（靴头微翘 + 金属扣件）
    for leg_idx, (lx, step) in enumerate(((-6, left_step), (4, right_step))):
        # 裤管深浅层次
        _rect(body, ox, oy, (cx + lx + step, 30 + cy, cx + lx + step + 3, 38 + cy), P["cloak_deep"])
        _rect(body, ox, oy, (cx + lx + step + 1, 31 + cy, cx + lx + step + 2, 37 + cy), P["cloak_dark"])
        # 绑腿素绫衬垫
        _rect(body, ox, oy, (cx + lx + step, 37 + cy, cx + lx + step + 3, 39 + cy), P["paper_shadow"])
        _line(body, ox, oy, ((cx + lx + step, 38 + cy), (cx + lx + step + 3, 38 + cy)), P["paper_light"], 1)
        # 乌皮皂靴：靴筒折皱 + 靴头起翘弧线
        _rect(body, ox, oy, (cx + lx + step - 1, 39 + cy, cx + lx + step + 4, 43), P["ink_black"])
        _rect(body, ox, oy, (cx + lx + step, 40 + cy, cx + lx + step + 3, 42), P["leather_dark"])
        _rect(body, ox, oy, (cx + lx + step + 1, 40 + cy, cx + lx + step + 2, 41), P["leather"])
        # 铜扣与靴底
        _plot(body, ox, oy, cx + lx + step + (2 if leg_idx == 0 else 1), 40 + cy, P["gold_light"])
        _line(body, ox, oy, ((cx + lx + step - 1, 43), (cx + lx + step + 4, 43)), P["ink_deep"], 1)

    # 3. 儒侠青灰外袍（三层交领 + 飘逸外袍开衩 + 随身法摆动）
    cloak_shift = layout["cloak"] * (3 + attack) + sway
    # 外袍底衬墨线与背光暗色
    _polygon(outfit, ox, oy, (
        (cx - 10, 18 + cy), (cx + 9, 18 + cy),
        (cx + 14 + cloak_shift, 32 + cy), (cx + 7, 37 + cy),
        (cx, 34 + cy), (cx - 9, 37 + cy), (cx - 14 + cloak_shift, 30 + cy)
    ), P["ink_black"])
    _polygon(outfit, ox, oy, (
        (cx - 9, 19 + cy), (cx + 8, 19 + cy),
        (cx + 12 + cloak_shift, 31 + cy), (cx + 6, 35 + cy),
        (cx, 33 + cy), (cx - 7, 35 + cy), (cx - 12 + cloak_shift, 29 + cy)
    ), P["cloak_deep"])
    _polygon(outfit, ox, oy, (
        (cx - 8, 20 + cy), (cx + 7, 20 + cy),
        (cx + 9 + cloak_shift, 29 + cy), (cx + 4, 33 + cy),
        (cx - 4, 31 + cy), (cx - 8 + cloak_shift, 28 + cy)
    ), P["cloak_dark"])
    _polygon(outfit, ox, oy, (
        (cx - 7, 21 + cy), (cx + 5, 21 + cy),
        (cx + 6 + cloak_shift, 28 + cy), (cx + 2, 31 + cy),
        (cx - 3, 29 + cy), (cx - 6 + cloak_shift, 27 + cy)
    ), P["cloak"])
    # 外袍向光面高光与折边线条
    _line(outfit, ox, oy, ((cx - 7, 22 + cy), (cx - 4, 27 + cy), (cx - 1, 31 + cy)), P["cloak_light"], 1)
    _line(outfit, ox, oy, ((cx - 6, 23 + cy), (cx - 3, 28 + cy)), P["cloak_shine"], 1)

    # 交领右衽：雪白中单内衬与黛蓝折边
    _polygon(outfit, ox, oy, (
        (cx - 5, 20 + cy), (cx + 5, 20 + cy),
        (cx + 7, 32 + cy), (cx, 35 + cy), (cx - 7, 32 + cy)
    ), P["paper_dark"])
    _polygon(outfit, ox, oy, (
        (cx - 4, 21 + cy), (cx + 4, 21 + cy),
        (cx + 5, 31 + cy), (cx, 33 + cy), (cx - 5, 31 + cy)
    ), P["paper_shadow"])
    _polygon(outfit, ox, oy, (
        (cx - 3, 21 + cy), (cx + 3, 21 + cy),
        (cx + 3, 30 + cy), (cx, 32 + cy), (cx - 3, 30 + cy)
    ), P["paper"])
    _line(outfit, ox, oy, ((cx - 2, 21 + cy), (cx + 1, 28 + cy)), P["paper_shine"], 1)
    _line(outfit, ox, oy, ((cx + 2, 21 + cy), (cx - 1, 28 + cy)), P["cloak_trim"], 1)

    # 4. 宽袍大袖、护腕与手掌
    arm_shift = attack * (2 if direction in ("right", "down") else -2)
    for arm_idx, (sx, shift) in enumerate(((-13, 0), (7 + arm_shift, arm_shift))):
        # 宽袖大摆
        _rect(outfit, ox, oy, (cx + sx, 20 + cy, cx + sx + 6, 29 + cy), P["ink_black"])
        _rect(outfit, ox, oy, (cx + sx + 1, 21 + cy, cx + sx + 5, 28 + cy), P["cloak_dark"])
        _rect(outfit, ox, oy, (cx + sx + 2, 22 + cy, cx + sx + 4, 27 + cy), P["cloak"])
        _line(outfit, ox, oy, ((cx + sx + 2, 23 + cy), (cx + sx + 4, 23 + cy)), P["cloak_light"], 1)
        # 熟牛皮护腕（带赤金搭扣）
        _rect(body, ox, oy, (cx + sx + 1, 27 + cy, cx + sx + 5, 30 + cy), P["leather_deep"])
        _rect(body, ox, oy, (cx + sx + 2, 28 + cy, cx + sx + 4, 29 + cy), P["leather"])
        _plot(body, ox, oy, cx + sx + 3, 28 + cy, P["gold_light"])
        # 紧握手掌
        _rect(body, ox, oy, (cx + sx + 1, 30 + cy, cx + sx + 4, 33 + cy), P["skin_shadow"])
        _rect(body, ox, oy, (cx + sx + 2, 30 + cy, cx + sx + 3, 32 + cy), P["skin"])

    # 5. 面容五官、坚毅剑眉星目与束发发髻
    # 头部轮廓与肤色渐变
    _polygon(face, ox, oy, (
        (cx - 6, 7 + cy), (cx + 5, 7 + cy),
        (cx + 6, 14 + cy), (cx + 3, 19 + cy),
        (cx - 4, 19 + cy), (cx - 7, 14 + cy)
    ), P["ink_black"])
    _polygon(face, ox, oy, (
        (cx - 5, 8 + cy), (cx + 4, 8 + cy),
        (cx + 5, 14 + cy), (cx + 2, 18 + cy),
        (cx - 3, 18 + cy), (cx - 6, 14 + cy)
    ), P["skin_shadow"])
    _polygon(face, ox, oy, (
        (cx - 4, 9 + cy), (cx + 3, 9 + cy),
        (cx + 4, 13 + cy), (cx + 1, 16 + cy),
        (cx - 2, 16 + cy), (cx - 5, 13 + cy)
    ), P["skin"])
    _rect(face, ox, oy, (cx - 3, 9 + cy, cx + 2, 13 + cy), P["skin_light"])
    _plot(face, ox, oy, cx - 1, 10 + cy, P["skin_shine"])

    # 五官（前向/侧向精细绘制）
    if layout["face"] == "front":
        # 剑眉星目（黑瞳 + 亮白反光）
        _line(face, ox, oy, ((cx - 4, 11 + cy), (cx - 1, 11 + cy)), P["hair_dark"], 1)
        _line(face, ox, oy, ((cx + 1, 11 + cy), (cx + 4, 11 + cy)), P["hair_dark"], 1)
        _plot(face, ox, oy, cx - 3, 13 + cy, P["ink_black"])
        _plot(face, ox, oy, cx - 2, 13 + cy, P["ink_black"])
        _plot(face, ox, oy, cx - 3, 13 + cy, P["steel_shine"])
        _plot(face, ox, oy, cx + 2, 13 + cy, P["ink_black"])
        _plot(face, ox, oy, cx + 3, 13 + cy, P["ink_black"])
        _plot(face, ox, oy, cx + 2, 13 + cy, P["steel_shine"])
        # 挺拔鼻梁与微抿薄唇
        _plot(face, ox, oy, cx, 14 + cy, P["skin_shadow"])
        _plot(face, ox, oy, cx, 15 + cy, P["skin_shadow"])
        _line(face, ox, oy, ((cx - 1, 17 + cy), (cx + 1, 17 + cy)), P["skin_deep"], 1)
    elif layout["face"] == "left":
        _line(face, ox, oy, ((cx - 5, 11 + cy), (cx - 2, 11 + cy)), P["hair_dark"], 1)
        _plot(face, ox, oy, cx - 4, 13 + cy, P["ink_black"])
        _plot(face, ox, oy, cx - 4, 13 + cy, P["steel_shine"])
        _plot(face, ox, oy, cx - 5, 15 + cy, P["skin_shadow"])
        _plot(face, ox, oy, cx - 3, 17 + cy, P["skin_deep"])
    elif layout["face"] == "right":
        _line(face, ox, oy, ((cx + 2, 11 + cy), (cx + 5, 11 + cy)), P["hair_dark"], 1)
        _plot(face, ox, oy, cx + 4, 13 + cy, P["ink_black"])
        _plot(face, ox, oy, cx + 4, 13 + cy, P["steel_shine"])
        _plot(face, ox, oy, cx + 5, 15 + cy, P["skin_shadow"])
        _plot(face, ox, oy, cx + 3, 17 + cy, P["skin_deep"])

    # 束发发髻、金簪与前额刘海
    _polygon(hair, ox, oy, (
        (cx - 7, 8 + cy), (cx - 4, 2 + cy), (cx + 3, 2 + cy),
        (cx + 6, 8 + cy), (cx + 5, 13 + cy), (cx - 6, 13 + cy)
    ), P["ink_black"])
    _polygon(hair, ox, oy, (
        (cx - 6, 7 + cy), (cx - 3, 3 + cy), (cx + 2, 3 + cy),
        (cx + 5, 7 + cy), (cx + 4, 11 + cy), (cx - 5, 11 + cy)
    ), P["hair_dark"])
    _polygon(hair, ox, oy, (
        (cx - 4, 5 + cy), (cx - 2, 3 + cy), (cx + 2, 3 + cy),
        (cx + 3, 6 + cy), (cx + 1, 8 + cy), (cx - 3, 8 + cy)
    ), P["hair"])
    _line(hair, ox, oy, ((cx - 2, 4 + cy), (cx + 2, 4 + cy)), P["hair_shine"], 1)
    # 头顶发冠与插簪
    _rect(hair, ox, oy, (cx - 3, 0 + cy, cx + 2, 4 + cy), P["ink_black"])
    _rect(hair, ox, oy, (cx - 2, 1 + cy, cx + 1, 3 + cy), P["hair_light"])
    _line(hair, ox, oy, ((cx - 4, 2 + cy), (cx + 3, 2 + cy)), P["gold_light"], 1)
    if layout["face"] == "back":
        _rect(hair, ox, oy, (cx - 6, 8 + cy, cx + 5, 18 + cy), P["hair_dark"])
        _rect(hair, ox, oy, (cx - 4, 9 + cy, cx + 3, 16 + cy), P["hair"])
        _line(hair, ox, oy, ((cx - 1, 9 + cy), (cx - 1, 15 + cy)), P["hair_shine"], 1)
    # 两缕前胸垂鬓发丝
    _line(hair, ox, oy, ((cx - 6, 12 + cy), (cx - 6, 19 + cy)), P["hair_dark"], 1)
    _line(hair, ox, oy, ((cx + 5, 12 + cy), (cx + 5, 19 + cy)), P["hair_dark"], 1)
    _plot(hair, ox, oy, cx - 6, 15 + cy, P["hair_shine"])
    _plot(hair, ox, oy, cx + 5, 15 + cy, P["hair_shine"])

    # 6. 朱红飘逸发带（随步履自然摆动）
    ribbon = layout["ribbon"]
    ribbon_sway = sway + (1 if frame_index % 2 == 1 else 0)
    _rect(accessory, ox, oy, (cx - 2, 2 + cy, cx + 2, 5 + cy), P["vermilion_deep"])
    _polygon(accessory, ox, oy, (
        (cx + ribbon * 2, 3 + cy),
        (cx + ribbon * (9 + ribbon_sway), 6 + cy),
        (cx + ribbon * (12 + ribbon_sway), 12 + cy),
        (cx + ribbon * (6 + ribbon_sway), 11 + cy)
    ), P["vermilion_dark"])
    _polygon(accessory, ox, oy, (
        (cx + ribbon * 3, 4 + cy),
        (cx + ribbon * (8 + ribbon_sway), 7 + cy),
        (cx + ribbon * (10 + ribbon_sway), 11 + cy),
        (cx + ribbon * (6 + ribbon_sway), 10 + cy)
    ), P["vermilion"])
    _line(accessory, ox, oy, (
        (cx + ribbon * 3, 5 + cy), (cx + ribbon * (9 + ribbon_sway), 8 + cy)
    ), P["vermilion_shine"], 1)

    # 7. 朱红回纹双层腰封 + 悬挂青玉环佩与朱红长流苏
    _rect(accessory, ox, oy, (cx - 8, 25 + cy, cx + 7, 29 + cy), P["ink_black"])
    _rect(accessory, ox, oy, (cx - 7, 26 + cy, cx + 6, 28 + cy), P["vermilion_deep"])
    _rect(accessory, ox, oy, (cx - 6, 26 + cy, cx + 5, 27 + cy), P["vermilion"])
    _line(accessory, ox, oy, ((cx - 5, 26 + cy), (cx + 4, 26 + cy)), P["vermilion_shine"], 1)
    # 中心赤金云纹带扣
    _rect(accessory, ox, oy, (cx - 1, 25 + cy, cx + 1, 28 + cy), P["gold_dark"])
    _plot(accessory, ox, oy, cx, 26 + cy, P["gold_shine"])
    # 斜挂温润青玉佩（镂空玉环）
    _polygon(accessory, ox, oy, (
        (cx + 3, 28 + cy), (cx + 7, 31 + cy),
        (cx + 5, 34 + cy), (cx + 1, 31 + cy)
    ), P["jade_dark"])
    _polygon(accessory, ox, oy, (
        (cx + 4, 29 + cy), (cx + 6, 31 + cy),
        (cx + 4, 33 + cy), (cx + 2, 31 + cy)
    ), P["jade"])
    _plot(accessory, ox, oy, cx + 4, 30 + cy, P["jade_shine"])
    # 悬垂鲜红长流苏（随动作向侧后方飘逸）
    tassel_sway = sway // 2
    _line(accessory, ox, oy, (
        (cx + 4, 34 + cy), (cx + 5 + tassel_sway, 39 + cy), (cx + 6 + tassel_sway, 42 + cy)
    ), P["vermilion_bright"], 1)
    _plot(accessory, ox, oy, cx + 5 + tassel_sway, 40 + cy, P["vermilion_shine"])

    # 8. 背负剑鞘与青铜兽首吞口
    scabbard = layout["sword"]
    _line(weapon, ox, oy, (
        (cx + scabbard * 8, 14 + cy), (cx + scabbard * 16, 37 + cy)
    ), P["ink_black"], 4)
    _line(weapon, ox, oy, (
        (cx + scabbard * 8, 14 + cy), (cx + scabbard * 15, 36 + cy)
    ), P["leather_deep"], 2)
    _line(weapon, ox, oy, (
        (cx + scabbard * 7, 15 + cy), (cx + scabbard * 10, 22 + cy)
    ), P["steel_light"], 1)
    # 兽首青铜吞口
    _rect(weapon, ox, oy, (cx + scabbard * 6 - 1, 12 + cy, cx + scabbard * 6 + 3, 17 + cy), P["gold_dark"])
    _rect(weapon, ox, oy, (cx + scabbard * 6, 13 + cy, cx + scabbard * 6 + 2, 16 + cy), P["gold"])
    _plot(weapon, ox, oy, cx + scabbard * 6 + 1, 14 + cy, P["gold_shine"])


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
        # 龙泉冷钢宝剑：神兵刃芒 + 兽首吞口 + 编织防滑剑柄
        # 剑刃暗面与锋芒
        draw.line(((17, 36), (36, 9)), fill=P["ink_black"], width=5)
        draw.line(((17, 36), (36, 9)), fill=P["steel_dark"], width=3)
        draw.line(((18, 34), (35, 10)), fill=P["steel"], width=2)
        draw.line(((19, 32), (36, 9)), fill=P["steel_light"], width=1)
        draw.line(((21, 29), (36, 9)), fill=P["steel_shine"], width=1)
        # 青铜兽首剑格与吞口
        draw.rectangle((13, 29, 23, 34), fill=P["gold_deep"])
        draw.rectangle((14, 30, 22, 33), fill=P["gold"])
        draw.rectangle((16, 31, 20, 32), fill=P["gold_shine"])
        # 剑柄与红丝缠绳
        draw.rectangle((15, 34, 20, 42), fill=P["leather_deep"])
        for y in range(35, 42, 2):
            draw.line(((15, y), (20, y)), fill=P["vermilion_bright"], width=1)
        # 剑首赤金圆环与朱红剑穗
        draw.rectangle((16, 42, 19, 44), fill=P["gold"])
        draw.line(((18, 44), (22, 47)), fill=P["vermilion_bright"], width=1)
    elif weapon_id == "weapon_gauntlets":
        # 玄铁破阵指虎：精钢甲片 + 虎口金铆钉 + 暗纹皮质缠腕
        draw.rectangle((17, 21, 34, 37), fill=P["ink_black"])
        draw.rectangle((19, 22, 32, 35), fill=P["leather_deep"])
        draw.rectangle((20, 23, 31, 34), fill=P["leather"])
        draw.line((21, 25, 30, 25), fill=P["leather_light"], width=1)
        # 四指玄铁合金拳锋
        for x in (19, 22, 25, 28):
            draw.rectangle((x, 17, x + 2, 24), fill=P["steel_dark"])
            draw.line((x + 1, 18, x + 1, 23), fill=P["steel_light"])
            draw.point((x + 1, 18), fill=P["steel_shine"])
            # 拳背赤金铆钉
            draw.rectangle((x, 27, x + 1, 28), fill=P["gold_light"])
            draw.point((x, 27), fill=P["gold_shine"])
        # 腕口血色锁边
        draw.line((19, 36, 32, 36), fill=P["vermilion_dark"], width=1)
    elif weapon_id == "weapon_dart":
        # 追魂透甲流光镖：三棱飞刃 + 镂空血槽 + 赤羽尾带
        draw.rectangle((15, 24, 30, 39), fill=P["ink_black"])
        draw.rectangle((17, 26, 28, 37), fill=P["leather_deep"])
        for y_offset, y in enumerate((15, 21, 27)):
            # 锋刃三棱形
            draw.polygon(((25, y), (42, y + 3), (25, y + 6)), fill=P["ink_black"])
            draw.polygon(((26, y + 1), (40, y + 3), (26, y + 5)), fill=P["steel_dark"])
            draw.polygon(((28, y + 1), (39, y + 3), (28, y + 4)), fill=P["steel_light"])
            draw.line(((29, y + 2), (41, y + 3)), fill=P["steel_shine"], width=1)
            # 镂空血槽与赤羽尾带
            draw.line(((26, y + 3), (32, y + 3)), fill=P["vermilion_deep"], width=1)
            draw.line(((21, y + 3), (25, y + 3)), fill=P["vermilion_bright"], width=1)
            draw.point((20, y + 3), fill=P["vermilion_shine"])
    else:
        raise ValueError("unknown weapon ID: " + weapon_id)
    return image


def _draw_dense_actor(actor_id):
    size = 16 if actor_id == "mvp_lost_pouch" else 48
    image = Image.new("RGBA", (size, size), P["clear"])
    draw = ImageDraw.Draw(image)
    if actor_id == "mvp_lost_pouch":
        # 16x16 极精细金丝云纹锦囊：朱红绸缎 + 抽绳金结 + 翡翠玉珠流苏
        # 锦囊外轮廓
        draw.ellipse((2, 5, 13, 15), fill=P["ink_black"])
        draw.ellipse((3, 6, 12, 14), fill=P["vermilion_deep"])
        draw.ellipse((4, 7, 11, 13), fill=P["vermilion"])
        draw.arc((4, 7, 11, 13), 180, 360, fill=P["vermilion_shine"])
        # 抽绳与金丝如意绣纹
        draw.line((3, 6, 12, 6), fill=P["gold_dark"], width=2)
        draw.line((4, 6, 11, 6), fill=P["gold_shine"], width=1)
        draw.line((6, 2, 9, 6), fill=P["gold_light"], width=1)
        draw.point((7, 3), fill=P["gold_shine"])
        # 锦囊中央金线云纹
        draw.point((7, 9), fill=P["gold_shine"])
        draw.point((8, 9), fill=P["gold_light"])
        draw.point((7, 10), fill=P["gold_light"])
        # 下方悬挂翡翠珠与赤红丝穗
        draw.rectangle((7, 13, 8, 14), fill=P["jade"])
        draw.point((7, 13), fill=P["jade_shine"])
        draw.line((7, 15, 8, 16), fill=P["vermilion_bright"], width=1)
        return image
    if actor_id == "mvp_innkeeper":
        # 48x48 掌柜老赵：文生圆顶软帽 + 酱色福字锦袍 + 青灰算账围裙 + 青花提梁茶壶 + 铜钥匙
        # 地面阴影
        draw.ellipse((8, 39, 40, 47), fill=P["shadow"])
        # 裤腿与长靴
        draw.rectangle((13, 28, 21, 42), fill=P["ink_black"])
        draw.rectangle((27, 28, 35, 42), fill=P["ink_black"])
        draw.rectangle((14, 38, 20, 42), fill=P["leather_deep"])
        draw.rectangle((28, 38, 34, 42), fill=P["leather_deep"])
        # 酱色员外长袍与青灰围裙
        draw.rectangle((6, 18, 41, 36), fill=P["ink_black"])
        draw.rectangle((8, 19, 39, 35), fill=P["wood_deep"])
        draw.rectangle((10, 20, 37, 34), fill=P["wood_dark"])
        # 青灰细布围裙（带折痕与侧口袋）
        draw.rectangle((12, 21, 35, 33), fill=P["paper_dark"])
        draw.rectangle((14, 22, 33, 31), fill=P["paper_shadow"])
        draw.rectangle((16, 23, 31, 30), fill=P["paper"])
        draw.line((14, 25, 33, 25), fill=P["paper_light"], width=1)
        # 腰封与掌柜铜钥匙圈
        draw.rectangle((9, 22, 38, 24), fill=P["leather_deep"])
        draw.ellipse((10, 25, 16, 31), fill=P["gold_dark"])
        draw.ellipse((11, 26, 15, 30), fill=P["gold_shine"])
        draw.point((13, 28), fill=P["ink_black"])
        # 头面部与文生软帽（带帽正碧玉）
        draw.rectangle((11, 7, 36, 22), fill=P["ink_black"])
        draw.rectangle((13, 9, 34, 20), fill=P["skin_shadow"])
        draw.rectangle((15, 10, 32, 17), fill=P["skin"])
        draw.rectangle((17, 10, 30, 14), fill=P["skin_light"])
        # 软帽弧度与帽正翠玉
        draw.polygon(((10, 9), (13, 4), (34, 4), (37, 9)), fill=P["ink_black"])
        draw.polygon(((12, 8), (14, 5), (33, 5), (35, 8)), fill=P["roof_dark"])
        draw.line((15, 6, 32, 6), fill=P["roof_light"], width=1)
        draw.rectangle((23, 6, 25, 8), fill=P["jade_shine"])
        # 慈眉善目：弯弯笑眼与八字胡须
        draw.line(((17, 12), (20, 13)), fill=P["ink_black"], width=1)
        draw.line(((27, 13), (30, 12)), fill=P["ink_black"], width=1)
        draw.point((19, 13), fill=P["steel_shine"])
        draw.point((28, 13), fill=P["steel_shine"])
        draw.point((24, 15), fill=P["skin_shadow"])
        # 和善八字胡
        draw.line(((20, 17), (23, 16)), fill=P["hair_dark"], width=1)
        draw.line(((25, 16), (28, 17)), fill=P["hair_dark"], width=1)
        draw.line(((22, 18), (26, 18)), fill=P["skin_deep"], width=1)
        # 右手托着的景德镇青花瓷提梁茶壶
        draw.ellipse((32, 22, 40, 29), fill=P["ink_black"])
        draw.ellipse((33, 23, 39, 28), fill=P["porcelain_white"])
        draw.arc((33, 23, 39, 28), 0, 180, fill=P["porcelain_shadow"])
        draw.line((35, 25, 37, 25), fill=P["porcelain_blue"], width=1)
        draw.arc((34, 19, 38, 24), 180, 360, fill=P["wood_dark"], width=1)
        return image
    if actor_id not in ("mvp_bandit_a", "mvp_bandit_b"):
        raise ValueError("unknown dense actor: " + actor_id)
    # 48x48 河岸水匪：粗犷肌肉劲装 + 煞气头巾 + 环首九环大刀 (A) / 开山双刃阔斧 (B)
    headband = P["vermilion_bright"] if actor_id == "mvp_bandit_a" else P["jade_light"]
    headband_dark = P["vermilion_deep"] if actor_id == "mvp_bandit_a" else P["jade_dark"]
    # 地面阴影
    draw.ellipse((9, 39, 39, 47), fill=P["shadow"])
    # 麻绳绑腿与粗麻草鞋
    draw.rectangle((12, 29, 21, 42), fill=P["ink_black"])
    draw.rectangle((27, 29, 36, 42), fill=P["ink_black"])
    draw.rectangle((13, 30, 20, 38), fill=P["leather_deep"])
    draw.rectangle((28, 30, 35, 38), fill=P["leather_deep"])
    for y in (32, 35):
        draw.line((13, y, 20, y), fill=P["paper_shadow"], width=1)
        draw.line((28, y, 35, y), fill=P["paper_shadow"], width=1)
    # 敞襟短打与古铜健硕肌肉
    draw.rectangle((7, 18, 41, 36), fill=P["ink_black"])
    draw.polygon(((10, 19), (37, 19), (42, 33), (25, 39), (6, 33)), fill=headband_dark)
    draw.polygon(((12, 20), (35, 20), (39, 31), (25, 36), (9, 31)), fill=P["leather_deep"])
    # 袒胸肌肉线条阴影
    draw.polygon(((18, 19), (30, 19), (24, 30)), fill=P["skin_deep"])
    draw.polygon(((19, 20), (29, 20), (24, 28)), fill=P["skin_shadow"])
    draw.polygon(((20, 20), (28, 20), (24, 26)), fill=P["skin"])
    draw.line((24, 21, 24, 27), fill=P["skin_shadow"], width=1)
    # 头面部与煞气头巾
    draw.rectangle((12, 7, 36, 22), fill=P["ink_black"])
    draw.rectangle((14, 9, 34, 20), fill=P["skin_shadow"])
    draw.rectangle((16, 10, 32, 17), fill=P["skin"])
    # 头巾与飘尾
    draw.polygon(((10, 8), (13, 3), (35, 3), (38, 8)), fill=P["ink_black"])
    draw.polygon(((12, 7), (14, 4), (34, 4), (36, 7)), fill=headband_dark)
    draw.line((14, 5, 34, 5), fill=headband, width=1)
    draw.line((34, 7, 41, 14), fill=headband, width=2)
    # 凶煞怒目与络腮虬髯
    draw.line(((17, 11), (21, 13)), fill=P["ink_black"], width=2)
    draw.line(((27, 13), (31, 11)), fill=P["ink_black"], width=2)
    draw.point((19, 13), fill=P["steel_shine"])
    draw.point((29, 13), fill=P["steel_shine"])
    # 络腮大胡
    draw.rectangle((15, 17, 33, 21), fill=P["ink_black"])
    draw.line((16, 18, 32, 18), fill=P["hair_dark"], width=1)
    # 专属武器：九环大刀 (A) / 开山双刃阔斧 (B)
    if actor_id == "mvp_bandit_a":
        # 环首九环大刀：厚背寒光 + 刀背赤金环扣
        draw.line(((33, 30), (46, 11)), fill=P["ink_black"], width=5)
        draw.line(((33, 30), (46, 11)), fill=P["steel_dark"], width=3)
        draw.line(((34, 29), (45, 12)), fill=P["steel_light"], width=2)
        draw.line(((36, 27), (45, 13)), fill=P["steel_shine"], width=1)
        # 刀背 3 枚赤金环扣
        for hx, hy in ((39, 19), (42, 15), (44, 12)):
            draw.rectangle((hx, hy, hx + 1, hy + 1), fill=P["gold_light"])
            draw.point((hx, hy), fill=P["gold_shine"])
    else:
        # 开山双刃阔斧：沉木长柄 + 精钢大斧刃
        draw.line(((33, 32), (43, 12)), fill=P["wood_deep"], width=3)
        draw.line(((34, 31), (42, 13)), fill=P["wood_dark"], width=1)
        # 斧头两侧弧刃
        draw.polygon(((38, 10), (47, 7), (45, 20), (37, 18)), fill=P["ink_black"])
        draw.polygon(((39, 11), (46, 8), (44, 19), (38, 17)), fill=P["steel_dark"])
        draw.polygon(((40, 12), (45, 9), (43, 18), (39, 16)), fill=P["steel_light"])
        draw.line(((45, 8), (44, 18)), fill=P["steel_shine"], width=1)
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
    """Draw one authored, composable town module with pixel-art masterwork shading."""
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
        # 16x16 江南青石路面：错落青石板 + 石缝青苔 + 斑驳石光
        draw.rectangle((0, 0, 15, 15), fill=P["ink_black"])
        draw.rectangle((1, 1, 14, 14), fill=P["stone"])
        # 逐块石板细密勾勒
        if name == "road_a":
            # 横向青石铺地
            draw.line((1, 5, 14, 5), fill=P["stone_dark"], width=1)
            draw.line((1, 10, 14, 10), fill=P["stone_dark"], width=1)
            draw.line((7, 1, 7, 5), fill=P["stone_dark"], width=1)
            draw.line((11, 6, 11, 10), fill=P["stone_dark"], width=1)
            draw.line((5, 11, 5, 14), fill=P["stone_dark"], width=1)
            # 石板顶沿亮线高光
            draw.line((1, 1, 14, 1), fill=P["stone_highlight"], width=1)
            draw.line((1, 6, 14, 6), fill=P["stone_light"], width=1)
            draw.line((1, 11, 14, 11), fill=P["stone_light"], width=1)
            # 石缝偶见微苔
            draw.point((7, 5), fill=P["jade_moss"])
            draw.point((11, 10), fill=P["jade_moss"])
        elif name == "road_b":
            # 纵向青石铺地
            draw.line((5, 1, 5, 14), fill=P["stone_dark"], width=1)
            draw.line((10, 1, 10, 14), fill=P["stone_dark"], width=1)
            draw.line((1, 7, 5, 7), fill=P["stone_dark"], width=1)
            draw.line((6, 11, 10, 11), fill=P["stone_dark"], width=1)
            draw.line((11, 6, 14, 6), fill=P["stone_dark"], width=1)
            # 亮面高光
            draw.line((1, 1, 5, 1), fill=P["stone_highlight"], width=1)
            draw.line((6, 1, 10, 1), fill=P["stone_light"], width=1)
            draw.line((11, 1, 14, 1), fill=P["stone_highlight"], width=1)
            draw.point((5, 7), fill=P["jade_moss"])
        else:
            # 弯道交界石阶
            draw.arc((-6, -6, 22, 22), 0, 90, fill=P["stone_highlight"], width=2)
            draw.arc((-4, -4, 20, 20), 0, 90, fill=P["stone_light"], width=1)
            draw.arc((-2, -2, 18, 18), 0, 90, fill=P["stone_dark"], width=1)
        return image
    if name.startswith("water"):
        # 16x16 碧波微漾与波光倒影（逐行平滑水纹）
        base = P["water_deep"] if name == "water_deep" else P["water"]
        draw.rectangle((0, 0, 15, 15), fill=base)
        # 深浅水流微澜
        if name == "water_flow":
            draw.line((1, 3, 5, 3, 9, 4, 14, 4), fill=P["water_ripple"], width=1)
            draw.line((2, 4, 6, 4), fill=P["water_shine"], width=1)
            draw.line((0, 9, 4, 8, 10, 8, 15, 9), fill=P["water_light"], width=1)
            draw.line((5, 9, 8, 9), fill=P["water_ripple"], width=1)
            draw.line((3, 13, 8, 13, 13, 14), fill=P["water_light"], width=1)
        elif name == "water_reflection":
            # 黛瓦倒影与红灯笼碎金星芒
            draw.line((2, 1, 5, 5, 1, 9), fill=P["roof_dark"], width=1)
            draw.line((3, 2, 6, 6), fill=P["roof_light"], width=1)
            # 水面灯火星光
            draw.line((9, 4, 14, 9), fill=P["warm"], width=1)
            draw.line((10, 5, 13, 8), fill=P["warm_light"], width=1)
            draw.point((11, 6), fill=P["warm_glow"])
            draw.point((12, 7), fill=P["warm_glow"])
        else:
            draw.line((2, 6, 7, 6, 13, 7), fill=P["water_light"], width=1)
            draw.line((4, 7, 6, 7), fill=P["water_ripple"], width=1)
            draw.line((0, 12, 5, 11, 11, 11, 15, 12), fill=P["water_light"], width=1)
        return image
    if name.startswith("shore"):
        # 16x16 驳岸青苔与石质阶梯
        draw.rectangle((0, 0, 15, 15), fill=P["jade_moss"])
        draw.polygon(((0, 8), (5, 6), (10, 7), (15, 4), (15, 15), (0, 15)), fill=P["ink_black"])
        draw.polygon(((0, 9), (5, 7), (10, 8), (15, 5), (15, 15), (0, 15)),
                     fill=P["stone_dark"] if name == "shore_stone" else P["water_deep"])
        draw.polygon(((0, 10), (5, 8), (10, 9), (15, 6), (15, 15), (0, 15)),
                     fill=P["stone"] if name == "shore_stone" else P["water"])
        draw.line((0, 9, 5, 7, 10, 8, 15, 5), fill=P["stone_highlight"], width=1)
        # 岸边青草聚簇
        draw.line((2, 3, 3, 1), fill=P["jade_light"], width=1)
        draw.line((7, 4, 8, 2), fill=P["jade_light"], width=1)
        draw.line((12, 3, 13, 1), fill=P["jade_shine"], width=1)
        return image
    if name == "inn_roof":
        # 64x64 歇山顶黛瓦层叠飞檐（瓦当勾头 + 翘角飞檐 + 脊兽）
        draw.polygon(((0, 40), (14, 8), (49, 2), (63, 34), (58, 48), (5, 48)), fill=P["ink_black"])
        draw.polygon(((5, 38), (16, 11), (48, 6), (59, 33), (55, 43), (8, 43)), fill=P["roof_dark"])
        draw.polygon(((7, 36), (17, 13), (47, 8), (57, 32), (53, 41), (10, 41)), fill=P["roof"])
        # 瓦垄层次逐层递进
        for y in range(12, 42, 5):
            draw.line((10, y, 56, y - 6), fill=P["roof_light"], width=2)
            draw.line((11, y - 1, 55, y - 7), fill=P["roof_highlight"], width=1)
            draw.line((12, y - 2, 54, y - 8), fill=P["roof_shine"], width=1)
        # 滴水瓦当与飞檐金翘角
        draw.line((2, 41, 62, 41), fill=P["gold_dark"], width=2)
        draw.line((1, 40, 6, 37), fill=P["roof_shine"], width=2)
        draw.line((63, 36, 58, 38), fill=P["roof_shine"], width=2)
        return image
    if name == "inn_wall":
        # 64x64 粉墙黛瓦 + 立体雕花木窗 + 青石踢脚线
        draw.rectangle((1, 12, 62, 63), fill=P["ink_black"])
        draw.rectangle((5, 16, 58, 60), fill=P["paper_dark"])
        draw.rectangle((8, 19, 55, 58), fill=P["paper_shadow"])
        draw.rectangle((10, 20, 53, 57), fill=P["paper"])
        # 实木立柱与横梁
        for x in (11, 31, 51):
            draw.rectangle((x, 16, x + 3, 58), fill=P["wood_deep"])
            draw.line((x + 1, 17, x + 1, 57), fill=P["wood"], width=1)
            draw.line((x + 2, 17, x + 2, 57), fill=P["wood_light"], width=1)
        # 两扇透光雕花万字木窗（暖阳倾洒）
        for wx in (16, 36):
            draw.rectangle((wx, 26, wx + 11, 40), fill=P["ink_black"])
            draw.rectangle((wx + 1, 27, wx + 10, 39), fill=P["wood_deep"])
            draw.rectangle((wx + 2, 28, wx + 9, 38), fill=P["warm"])
            draw.rectangle((wx + 3, 29, wx + 8, 37), fill=P["warm_light"])
            draw.rectangle((wx + 4, 30, wx + 7, 36), fill=P["warm_glow"])
            # 十字花格窗棂
            draw.line((wx + 5, 27, wx + 5, 39), fill=P["wood_dark"], width=1)
            draw.line((wx + 2, 33, wx + 9, 33), fill=P["wood_dark"], width=1)
        # 墙底青石踢脚线
        draw.rectangle((5, 57, 58, 61), fill=P["stone_dark"])
        draw.rectangle((5, 58, 58, 60), fill=P["stone"])
        draw.line((5, 58, 58, 58), fill=P["stone_highlight"], width=1)
        return image
    if name == "inn_door":
        # 32x32 红木雕花大门 + 黄铜兽首门环
        draw.rectangle((1, 0, 30, 31), fill=P["ink_black"])
        draw.rectangle((4, 3, 27, 31), fill=P["wood_deep"])
        draw.rectangle((6, 4, 25, 30), fill=P["wood_dark"])
        draw.rectangle((7, 5, 24, 29), fill=P["wood"])
        # 双扇门中缝与包边
        draw.line((15, 3, 15, 31), fill=P["wood_deep"], width=2)
        draw.line((16, 4, 16, 30), fill=P["wood_light"], width=1)
        # 黄铜门环与乳钉
        for hy in (11, 19):
            draw.rectangle((9, hy, 12, hy + 2), fill=P["gold_dark"])
            draw.rectangle((10, hy, 11, hy + 2), fill=P["gold_shine"])
            draw.rectangle((18, hy, 21, hy + 2), fill=P["gold_dark"])
            draw.rectangle((19, hy, 20, hy + 2), fill=P["gold_shine"])
        # 门槛青石
        draw.rectangle((0, 28, 31, 31), fill=P["stone"])
        draw.line((0, 28, 31, 28), fill=P["stone_highlight"], width=1)
        return image
    if name == "inn_sign":
        # 32x32 “悦来客栈”朱红酒幌招牌 + 铜钩木架
        draw.rectangle((1, 2, 30, 29), fill=P["ink_black"])
        draw.rectangle((3, 4, 28, 26), fill=P["wood_deep"])
        draw.rectangle((5, 5, 26, 24), fill=P["wood_dark"])
        draw.rectangle((7, 7, 24, 11), fill=P["paper"])
        draw.line((8, 9, 23, 9), fill=P["ink_black"], width=1)
        # 飘扬朱红酒旗
        draw.polygon(((8, 12), (23, 12), (20, 24), (10, 24)), fill=P["vermilion_deep"])
        draw.polygon(((9, 13), (22, 13), (19, 23), (11, 23)), fill=P["vermilion"])
        draw.line(((9, 13), (21, 13)), fill=P["vermilion_shine"], width=1)
        # 金黄“酒”字草书印
        draw.rectangle((13, 15, 17, 20), fill=P["paper_shine"])
        draw.point((15, 17), fill=P["ink_black"])
        return image
    if name == "bridge":
        # 48x48 江南青石拱桥（半圆拱券石 + 错落石阶 + 望柱抱鼓石栏杆）
        draw.polygon(((0, 25), (6, 11), (41, 11), (47, 25), (44, 40), (3, 40)), fill=P["ink_black"])
        draw.polygon(((2, 25), (8, 13), (39, 13), (45, 25), (42, 36), (5, 36)), fill=P["stone_dark"])
        draw.polygon(((3, 26), (9, 14), (38, 14), (44, 26), (40, 34), (7, 34)), fill=P["stone"])
        # 半圆拱券石与阴影青苔
        draw.arc((11, 21, 36, 43), 180, 360, fill=P["ink_black"], width=5)
        draw.arc((12, 22, 35, 42), 180, 360, fill=P["jade_moss"], width=3)
        draw.arc((13, 23, 34, 41), 180, 360, fill=P["water_abyss"], width=1)
        # 石桥板缝与石栏杆
        for x in (8, 17, 27, 37):
            draw.line((x, 12, x - 2, 35), fill=P["stone_dark"], width=2)
            draw.line((x + 1, 12, x - 1, 35), fill=P["stone_light"], width=1)
            draw.line((x + 2, 12, x, 35), fill=P["stone_highlight"], width=1)
            # 望柱抱鼓石圆雕
            draw.rectangle((x - 1, 8, x + 2, 13), fill=P["stone_highlight"])
            draw.point((x, 9), fill=P["stone_shine"])
        draw.line((3, 14, 44, 14), fill=P["stone_highlight"], width=1)
        return image
    if name == "boat":
        # 48x48 摇橹乌篷船（竹篾圆拱船篷 + 尖翘木板船身 + 木橹与白幔）
        draw.polygon(((1, 28), (45, 28), (39, 41), (7, 41)), fill=P["ink_black"])
        draw.polygon(((4, 30), (42, 30), (37, 38), (9, 38)), fill=P["wood_deep"])
        draw.polygon(((5, 31), (41, 31), (36, 37), (10, 37)), fill=P["wood"])
        draw.line((5, 31, 41, 31), fill=P["wood_highlight"], width=1)
        # 拱形深色竹篾圆篷（细密编织纹）
        draw.arc((15, 15, 33, 33), 180, 360, fill=P["ink_black"], width=4)
        draw.arc((16, 16, 32, 32), 180, 360, fill=P["wood_deep"], width=3)
        draw.arc((17, 17, 31, 31), 180, 360, fill=P["wood_dark"], width=1)
        # 船尾木橹与船头白幔
        draw.line((22, 30, 22, 5), fill=P["wood_light"], width=2)
        draw.polygon(((24, 7), (41, 18), (24, 24)), fill=P["paper_shadow"])
        draw.polygon(((25, 8), (40, 18), (25, 23)), fill=P["paper"])
        draw.line((25, 9, 39, 18), fill=P["paper_shine"], width=1)
        return image
    if name == "bollard":
        # 16x16 沿河系缆沉木桩
        draw.rectangle((3, 2, 12, 15), fill=P["ink_black"])
        draw.rectangle((4, 3, 11, 14), fill=P["wood_deep"])
        draw.rectangle((5, 4, 10, 13), fill=P["wood"])
        draw.rectangle((3, 1, 12, 4), fill=P["wood_light"])
        draw.rectangle((4, 2, 11, 3), fill=P["wood_highlight"])
        return image
    if name == "lantern":
        # 16x16 悬挂八角红木灯笼 + 暖黄烛光晕染 + 鲜红流苏
        draw.rectangle((7, 0, 8, 15), fill=P["wood_deep"])
        draw.rectangle((1, 3, 14, 14), fill=P["ink_black"])
        draw.rectangle((3, 4, 12, 13), fill=P["vermilion_deep"])
        draw.rectangle((4, 5, 11, 12), fill=P["warm_dark"])
        draw.rectangle((5, 6, 10, 11), fill=P["warm"])
        draw.rectangle((6, 7, 9, 10), fill=P["warm_light"])
        draw.rectangle((7, 8, 8, 9), fill=P["warm_glow"])
        # 下方悬垂朱红流苏
        draw.line((7, 14, 8, 15), fill=P["vermilion_bright"], width=1)
        return image
    if name == "crate":
        # 16x16 码头实木货箱 + 铜包角加固
        draw.rectangle((0, 1, 15, 15), fill=P["ink_black"])
        draw.rectangle((1, 2, 14, 14), fill=P["wood_deep"])
        draw.rectangle((2, 3, 13, 13), fill=P["wood"])
        draw.line((2, 3, 13, 13), fill=P["wood_light"], width=1)
        draw.line((13, 3, 2, 13), fill=P["wood_light"], width=1)
        draw.rectangle((6, 7, 9, 10), fill=P["gold_light"])
        draw.point((7, 8), fill=P["gold_shine"])
        return image
    if name in ("willow_near", "willow_far"):
        # 烟雨垂柳：苍劲皴裂老树干 + 5 团细腻柳叶簇 + 柔韧飘曳柳丝
        draw.line((size // 2, 0, size // 2 - 8, size - 4), fill=P["ink_black"], width=6)
        draw.line((size // 2, 0, size // 2 - 8, size - 4), fill=P["wood_deep"], width=4)
        draw.line((size // 2 + 1, 0, size // 2 - 7, size - 4), fill=P["wood"], width=2)
        draw.line((size // 2 + 2, 0, size // 2 - 6, size - 4), fill=P["wood_highlight"], width=1)
        leaf_dark = P["jade_moss"] if name == "willow_near" else P["jade_dark"]
        leaf_mid = P["jade"] if name == "willow_near" else P["jade_light"]
        leaf_light = P["jade_shine"]
        for index, y in enumerate(range(5, size - 5, 7)):
            offset = 12 + (index % 3) * 6
            # 左侧柳叶球形聚簇与柳丝
            draw.polygon(((size // 2 - 4, y), (size // 2 - offset, y + 7),
                          (size // 2 - 5, y + 16)), fill=leaf_dark)
            draw.polygon(((size // 2 - 3, y + 1), (size // 2 - offset + 2, y + 7),
                          (size // 2 - 4, y + 13)), fill=leaf_mid)
            draw.line(((size // 2 - 2, y + 2), (size // 2 - offset + 4, y + 8)), fill=leaf_light, width=1)
            # 右侧柳叶球形聚簇与柳丝
            draw.polygon(((size // 2 + 2, y + 2), (size // 2 + offset, y + 8),
                          (size // 2 + 4, y + 17)), fill=leaf_mid)
            draw.polygon(((size // 2 + 3, y + 3), (size // 2 + offset - 2, y + 8),
                          (size // 2 + 5, y + 14)), fill=leaf_light)
        return image
    if name == "roof_trim":
        # 32x32 前景飞檐瓦当
        draw.polygon(((0, 0), (31, 0), (31, 13), (19, 9), (10, 15), (0, 10)), fill=P["ink_black"])
        draw.polygon(((2, 1), (29, 1), (29, 10), (19, 7), (10, 12), (2, 8)), fill=P["roof"])
        draw.line((2, 2, 29, 2), fill=P["roof_shine"], width=1)
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
    """Draw interior modules with pixel-art masterwork shading and warm atmospheric pools."""
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
        # 16x16 温润拼花红木地板（木纹嵌条 + 榫卯缝线）
        draw.rectangle((0, 0, 15, 15), fill=P["wood_deep"])
        draw.rectangle((1, 1, 14, 14), fill=P["wood_dark"])
        draw.rectangle((2, 2, 13, 13), fill=P["wood"])
        y = 4 if name == "floor_wood_a" else 9
        draw.line((1, y, 14, y), fill=P["wood_light"], width=1)
        draw.line((1, y + 1, 14, y + 1), fill=P["wood_highlight"], width=1)
        draw.line((7, 1, 7, 14), fill=P["wood_deep"], width=1)
        return image
    if name == "entry_stone":
        # 16x16 玄关青石板
        draw.rectangle((0, 0, 15, 15), fill=P["ink_black"])
        draw.rectangle((1, 1, 14, 14), fill=P["stone"])
        draw.line((1, 7, 14, 7), fill=P["stone_highlight"], width=1)
        draw.line((7, 1, 7, 14), fill=P["stone_dark"], width=1)
        return image
    if name == "rug":
        # 16x16 迎宾金丝祥云地毯（朱红织锦 + 金线如意回纹）
        draw.rectangle((0, 0, 15, 15), fill=P["vermilion_deep"])
        draw.rectangle((1, 1, 14, 14), fill=P["vermilion"])
        draw.rectangle((3, 3, 12, 12), fill=P["gold_dark"])
        draw.rectangle((4, 4, 11, 11), fill=P["gold"])
        draw.rectangle((5, 5, 10, 10), fill=P["gold_shine"])
        return image
    if name == "counter":
        # 64x64 掌柜红木柜台（雕花回纹裙板 + 翻开线装账本 + 算盘 + 青花毛笔筒）
        draw.rectangle((0, 18, 63, 59), fill=P["ink_black"])
        draw.rectangle((3, 21, 60, 40), fill=P["wood_light"])
        draw.rectangle((3, 41, 60, 56), fill=P["wood"])
        # 雕花实木立柱与立面回纹
        for x in range(8, 59, 10):
            draw.line((x, 23, x, 55), fill=P["wood_deep"], width=2)
            draw.line((x + 1, 23, x + 1, 55), fill=P["wood_highlight"], width=1)
        draw.line((3, 21, 60, 21), fill=P["wood_shine"], width=1)
        # 台面翻开的线装账本
        draw.rectangle((17, 11, 41, 21), fill=P["ink_black"])
        draw.rectangle((19, 13, 39, 19), fill=P["paper"])
        draw.line((20, 15, 38, 15), fill=P["ink_black"], width=1)
        draw.line((20, 17, 38, 17), fill=P["ink_black"], width=1)
        # 青花毛笔筒（插着狼毫毛笔）
        draw.rectangle((44, 13, 51, 21), fill=P["porcelain_white"])
        draw.rectangle((45, 15, 50, 19), fill=P["porcelain_blue"])
        draw.line((46, 7, 46, 13), fill=P["wood_deep"], width=1)
        draw.line((49, 9, 49, 13), fill=P["wood_deep"], width=1)
        return image
    if name == "counter_lantern":
        # 32x32 柜台八角暖光灯笼 + 柔和光晕渐变
        draw.line((16, 0, 16, 7), fill=P["wood_deep"], width=2)
        draw.rectangle((6, 6, 26, 27), fill=P["ink_black"])
        draw.rectangle((8, 8, 24, 25), fill=P["vermilion_deep"])
        draw.rectangle((10, 10, 22, 23), fill=P["warm_dark"])
        draw.rectangle((12, 12, 20, 21), fill=P["warm"])
        draw.rectangle((14, 14, 18, 19), fill=P["warm_light"])
        draw.rectangle((15, 15, 17, 18), fill=P["warm_glow"])
        return image
    if name == "table":
        # 48x48 沉木八仙圆桌 + 景德镇青花提梁茶壶 + 双茶盏
        draw.ellipse((1, 9, 47, 37), fill=P["ink_black"])
        draw.ellipse((4, 12, 44, 34), fill=P["wood_deep"])
        draw.ellipse((6, 14, 42, 32), fill=P["wood"])
        draw.line((9, 17, 39, 17), fill=P["wood_highlight"], width=2)
        # 桌腿与阴影
        for x in (10, 36):
            draw.rectangle((x, 30, x + 5, 44), fill=P["wood_deep"])
            draw.line((x + 1, 30, x + 1, 43), fill=P["wood_light"])
        # 青花瓷提梁茶壶（壶嘴出水弧度 + 青花釉色）
        draw.ellipse((20, 18, 28, 26), fill=P["porcelain_white"])
        draw.ellipse((21, 19, 27, 25), fill=P["porcelain_blue"])
        draw.line((24, 15, 24, 18), fill=P["wood_deep"], width=1)
        # 两只白瓷茶盏
        draw.rectangle((14, 21, 17, 24), fill=P["porcelain_white"])
        draw.rectangle((31, 21, 34, 24), fill=P["porcelain_white"])
        return image
    if name == "stove":
        # 48x48 厨房青石火灶 + 柴火暖焰
        draw.rectangle((2, 3, 45, 45), fill=P["ink_black"])
        draw.rectangle((5, 6, 42, 42), fill=P["stone_dark"])
        draw.rectangle((6, 7, 41, 41), fill=P["stone"])
        draw.line((6, 7, 41, 7), fill=P["stone_highlight"], width=1)
        draw.rectangle((11, 18, 36, 42), fill=P["ink_black"])
        # 柴火熊熊火焰
        draw.polygon(((17, 36), (24, 15), (31, 36)), fill=P["vermilion_bright"])
        draw.polygon(((19, 35), (24, 20), (29, 35)), fill=P["warm"])
        draw.polygon(((21, 34), (24, 25), (27, 34)), fill=P["warm_glow"])
        draw.rectangle((8, 8, 16, 12), fill=P["wood_deep"])
        return image
    if name == "stairs":
        # 64x64 实木楼梯台阶 + 扶手雕花立柱
        draw.rectangle((1, 3, 63, 63), fill=P["ink_black"])
        for index in range(7):
            y = 8 + index * 7
            draw.rectangle((5 + index * 3, y, 58, y + 6), fill=P["wood"])
            draw.line((5 + index * 3, y, 58, y), fill=P["wood_highlight"], width=1)
            draw.line((5 + index * 3, y + 6, 58, y + 6), fill=P["wood_deep"], width=1)
        return image
    if name == "kitchen_wall":
        # 64x64 厨房木格背景墙 + 悬挂腊味干货与红辣椒
        draw.rectangle((0, 0, 63, 63), fill=P["ink_black"])
        draw.rectangle((4, 4, 59, 59), fill=P["wood_deep"])
        for y in (12, 26, 40):
            draw.line((6, y, 57, y), fill=P["wood"], width=2)
        # 悬挂腊肉干货与红辣椒串
        draw.rectangle((8, 8, 22, 23), fill=P["paper_shadow"])
        draw.rectangle((40, 8, 53, 21), fill=P["vermilion_deep"])
        for py in (9, 13, 17):
            draw.point((46, py), fill=P["vermilion_bright"])
        return image
    if name == "window_light":
        # 32x32 江南镂空雕花木窗 + 暖阳斜照光斑
        draw.rectangle((0, 0, 31, 31), fill=P["ink_black"])
        draw.rectangle((3, 3, 28, 28), fill=P["warm_dark"])
        draw.rectangle((5, 5, 26, 26), fill=P["warm"])
        draw.rectangle((7, 7, 24, 24), fill=P["warm_light"])
        draw.rectangle((9, 9, 22, 22), fill=P["warm_glow"])
        # 万字木格窗棂
        draw.line((15, 3, 15, 28), fill=P["wood_deep"], width=2)
        draw.line((3, 15, 28, 15), fill=P["wood_deep"], width=2)
        draw.rectangle((7, 7, 24, 24), outline=P["wood_deep"])
        return image
    if name == "north_door":
        # 32x32 客栈后院实木小门 + 黄铜搭扣
        draw.rectangle((1, 0, 30, 31), fill=P["ink_black"])
        draw.rectangle((4, 2, 27, 31), fill=P["wood_deep"])
        draw.rectangle((5, 3, 26, 30), fill=P["wood"])
        draw.line((15, 3, 15, 31), fill=P["wood_light"])
        draw.rectangle((18, 16, 21, 19), fill=P["gold_light"])
        draw.point((19, 17), fill=P["gold_shine"])
        return image
    if name == "shelf":
        # 32x32 博古酒架（多宝格 + 红布封泥酒坛 + 青花酒壶）
        draw.rectangle((1, 1, 30, 31), fill=P["ink_black"])
        draw.rectangle((3, 3, 28, 29), fill=P["wood_deep"])
        for y in (5, 13, 21):
            draw.line((3, y, 28, y), fill=P["wood_highlight"], width=2)
        # 红布封泥酒坛（“绍兴花雕”、“女儿红”、“竹叶青”）
        for x, y, color in ((6, 6, P["vermilion"]), (16, 6, P["jade"]),
                            (9, 14, P["vermilion_bright"]), (19, 14, P["gold_light"])):
            draw.rectangle((x, y, x + 5, y + 6), fill=P["ink_black"])
            draw.rectangle((x + 1, y + 1, x + 4, y + 5), fill=color)
            draw.line((x + 1, y + 1, x + 4, y + 1), fill=P["paper_shine"], width=1)
        return image
    if name == "foreground_beam":
        # 64x64 客栈前景挑高实木立柱与雕花横梁
        draw.rectangle((0, 0, 14, 63), fill=P["ink_black"])
        draw.rectangle((2, 0, 11, 63), fill=P["wood_deep"])
        draw.line((4, 0, 4, 63), fill=P["wood_light"], width=1)
        draw.polygon(((10, 0), (63, 0), (63, 13), (22, 13)), fill=P["ink_black"])
        draw.polygon(((12, 1), (63, 1), (63, 11), (24, 11)), fill=P["wood_deep"])
        draw.line((24, 11, 63, 11), fill=P["wood_highlight"], width=1)
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
