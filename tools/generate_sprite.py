#!/usr/bin/env python3
"""
像素武侠角色精灵表生成器
角色：凌霜（剑客）— 默认主角外观
规格：48x48 per frame, 4方向 x 4帧 = 16帧
输出：单张精灵表 768x48（横排4帧x4行方向）
"""

from PIL import Image, ImageDraw
import os

# ============================================================
# 调色板 — 像素武侠风
# ============================================================
PALETTE = {
    # 头发（墨黑系）
    'hair_dark':   (30, 30, 40),
    'hair_mid':    (50, 45, 55),
    'hair_light':  (70, 60, 75),

    # 皮肤
    'skin':        (240, 200, 160),
    'skin_shadow': (210, 170, 130),
    'skin_light':  (250, 220, 185),

    # 外衣（青灰色系 — 武侠素雅风）
    'coat_dark':   (60, 70, 85),
    'coat_mid':    (80, 95, 115),
    'coat_light':  (105, 125, 150),

    # 内衬（白色）
    'inner':       (230, 230, 235),
    'inner_shade': (200, 200, 210),

    # 腰带（深红/赭石）
    'belt':        (160, 60, 50),
    'belt_dark':   (120, 40, 35),

    # 裤子
    'pants':       (55, 55, 65),
    'pants_light': (75, 75, 85),

    # 靴子
    'boot':        (45, 35, 30),
    'boot_light':  (65, 50, 40),

    # 剑（银白+青铜护手）
    'blade':       (200, 210, 220),
    'blade_light': (230, 235, 240),
    'guard':       (180, 150, 80),
    'hilt':        (100, 70, 45),

    # 眼睛
    'eye':         (25, 25, 30),
    'eye_white':   (245, 245, 245),

    # 流苏/装饰（暗红）
    'tassel':      (180, 50, 40),
    'tassel_dark': (140, 35, 30),

    # 透明
    'transparent': (0, 0, 0, 0),
}

# ============================================================
# 精灵绘制函数
# ============================================================

def create_sprite_sheet():
    """生成完整精灵表：4方向 x 4帧"""
    TILE = 48
    COLS = 4  # 帧数
    ROWS = 4  # 方向（下、左、右、上）
    sheet = Image.new('RGBA', (TILE * COLS, TILE * ROWS), (0, 0, 0, 0))

    directions = ['down', 'left', 'right', 'up']

    for row, direction in enumerate(directions):
        for frame in range(4):
            sprite = draw_character(direction, frame)
            x = frame * TILE
            y = row * TILE
            sheet.paste(sprite, (x, y), sprite)

    return sheet


def draw_character(direction, frame):
    """绘制单个角色帧"""
    TILE = 48
    img = Image.new('RGBA', (TILE, TILE), (0, 0, 0, 0))
    p = PALETTE

    # 行走动画偏移（腿部/手臂位移）
    # frame: 0=站立, 1=左脚前, 2=站立, 3=右脚前
    walk_offset = [0, -1, 0, 1][frame]  # 身体微微上下
    arm_swing = [0, 1, 0, -1][frame]
    leg_swing = [0, 2, 0, -2][frame]

    if direction == 'down':
        draw_facing_down(img, p, walk_offset, arm_swing, leg_swing, frame)
    elif direction == 'up':
        draw_facing_up(img, p, walk_offset, arm_swing, leg_swing, frame)
    elif direction == 'left':
        draw_facing_left(img, p, walk_offset, arm_swing, leg_swing, frame)
    elif direction == 'right':
        draw_facing_right(img, p, walk_offset, arm_swing, leg_swing, frame)

    return img


def put_pixel(draw, x, y, color, size=1):
    """放置一个或多个像素"""
    if isinstance(color, str):
        color = PALETTE.get(color, (255, 0, 255))
    for dx in range(size):
        for dy in range(size):
            draw.point((x + dx, y + dy), fill=color)


def draw_facing_down(img, p, body_y, arm, leg, frame):
    """正面朝下（面向玩家）"""
    d = ImageDraw.Draw(img)

    # ---- 头发 ---- (顶部的发髻/马尾)
    for x in range(18, 30):
        for y in range(4, 8):
            put_pixel(d, x, y, p['hair_dark'])
    # 发髻顶部
    for x in range(20, 28):
        put_pixel(d, x, 3, p['hair_dark'])
    # 两侧头发
    for y in range(8, 16):
        put_pixel(d, 17, y, p['hair_dark'])
        put_pixel(d, 30, y, p['hair_dark'])
    # 刘海
    for x in range(18, 30):
        for y in range(7, 10):
            put_pixel(d, x, y, p['hair_mid'])

    # ---- 脸部 ----
    for x in range(19, 29):
        for y in range(10, 17):
            put_pixel(d, x, y, p['skin'])
    # 脸部阴影
    for x in range(19, 29):
        put_pixel(d, x, 16, p['skin_shadow'])
    # 眼睛
    put_pixel(d, 21, 12, p['eye'])
    put_pixel(d, 26, 12, p['eye'])
    # 嘴
    put_pixel(d, 23, 15, p['skin_shadow'])
    put_pixel(d, 24, 15, p['skin_shadow'])

    # ---- 身体/外衣 ----
    by = 17 + body_y
    for x in range(16, 32):
        for y in range(by, by + 14):
            put_pixel(d, x, y, p['coat_mid'])
    # 外衣深色边缘
    for y in range(by, by + 14):
        put_pixel(d, 16, y, p['coat_dark'])
        put_pixel(d, 31, y, p['coat_dark'])
    # 内衬（V领）
    for x in range(22, 26):
        for y in range(by, by + 4):
            put_pixel(d, x, y, p['inner'])
    # 衣服褶皱
    for y in range(by + 4, by + 10):
        put_pixel(d, 20, y, p['coat_dark'])
        put_pixel(d, 27, y, p['coat_dark'])

    # ---- 腰带 ----
    belt_y = by + 12
    for x in range(16, 32):
        put_pixel(d, x, belt_y, p['belt'])
        put_pixel(d, x, belt_y + 1, p['belt_dark'])

    # ---- 手臂 ----
    # 左臂
    for y in range(by + 2, by + 12):
        put_pixel(d, 14 + min(arm, 0), y, p['coat_mid'])
        put_pixel(d, 15 + min(arm, 0), y, p['coat_mid'])
    put_pixel(d, 14 + arm, by + 12, p['skin'])  # 手
    put_pixel(d, 15 + arm, by + 12, p['skin'])
    # 右臂
    for y in range(by + 2, by + 12):
        put_pixel(d, 32 + max(arm, 0), y, p['coat_mid'])
        put_pixel(d, 33 + max(arm, 0), y, p['coat_mid'])
    put_pixel(d, 32 - arm, by + 12, p['skin'])
    put_pixel(d, 33 - arm, by + 12, p['skin'])

    # ---- 裤子/腿 ----
    pants_y = belt_y + 2
    # 左腿
    for y in range(pants_y, pants_y + 9):
        lx = 19 + (leg if leg > 0 else 0)
        put_pixel(d, lx, y, p['pants'])
        put_pixel(d, lx + 1, y, p['pants'])
        put_pixel(d, lx + 2, y, p['pants_light'])
        put_pixel(d, lx + 3, y, p['pants_light'])
    # 右腿
    for y in range(pants_y, pants_y + 9):
        rx = 25 + (leg if leg < 0 else 0)
        put_pixel(d, rx, y, p['pants_light'])
        put_pixel(d, rx + 1, y, p['pants_light'])
        put_pixel(d, rx + 2, y, p['pants'])
        put_pixel(d, rx + 3, y, p['pants'])

    # ---- 靴子 ----
    boot_y = pants_y + 9
    put_pixel(d, 19 + (leg if leg > 0 else 0), boot_y, p['boot'])
    put_pixel(d, 20 + (leg if leg > 0 else 0), boot_y, p['boot_light'])
    put_pixel(d, 21 + (leg if leg > 0 else 0), boot_y, p['boot_light'])
    put_pixel(d, 22 + (leg if leg > 0 else 0), boot_y, p['boot'])
    put_pixel(d, 25 + (leg if leg < 0 else 0), boot_y, p['boot'])
    put_pixel(d, 26 + (leg if leg < 0 else 0), boot_y, p['boot_light'])
    put_pixel(d, 27 + (leg if leg < 0 else 0), boot_y, p['boot_light'])
    put_pixel(d, 28 + (leg if leg < 0 else 0), boot_y, p['boot'])

    # ---- 剑（背在身后，剑柄露出右肩）----
    put_pixel(d, 34, by - 1, p['guard'])
    put_pixel(d, 34, by - 2, p['guard'])
    for y in range(by - 8, by - 2):
        put_pixel(d, 34, y, p['blade'])
    put_pixel(d, 34, by - 9, p['blade_light'])  # 剑尖高光

    # ---- 流苏（剑柄）----
    put_pixel(d, 35, by - 1, p['tassel'])
    put_pixel(d, 36, by, p['tassel_dark'])
    put_pixel(d, 35, by + 1, p['tassel_dark'])


def draw_facing_up(img, p, body_y, arm, leg, frame):
    """背面朝上"""
    d = ImageDraw.Draw(img)

    # ---- 头发（背面，大面积）----
    for x in range(18, 30):
        for y in range(4, 17):
            put_pixel(d, x, y, p['hair_dark'])
    for x in range(19, 29):
        for y in range(5, 16):
            put_pixel(d, x, y, p['hair_mid'])
    # 发髻
    for x in range(20, 28):
        for y in range(2, 5):
            put_pixel(d, x, y, p['hair_dark'])
    # 马尾
    for y in range(5, 22 + body_y):
        put_pixel(d, 28, y, p['hair_dark'])
        put_pixel(d, 29, y, p['hair_mid'])
    # 头发高光
    for x in range(21, 27):
        put_pixel(d, x, 6, p['hair_light'])

    # ---- 身体/外衣 ----
    by = 17 + body_y
    for x in range(16, 32):
        for y in range(by, by + 14):
            put_pixel(d, x, y, p['coat_mid'])
    for y in range(by, by + 14):
        put_pixel(d, 16, y, p['coat_dark'])
        put_pixel(d, 31, y, p['coat_dark'])
    # 背部中线
    for y in range(by, by + 14):
        put_pixel(d, 23, y, p['coat_dark'])
        put_pixel(d, 24, y, p['coat_dark'])

    # ---- 腰带 ----
    belt_y = by + 12
    for x in range(16, 32):
        put_pixel(d, x, belt_y, p['belt'])
        put_pixel(d, x, belt_y + 1, p['belt_dark'])

    # ---- 手臂 ----
    for y in range(by + 2, by + 12):
        put_pixel(d, 14 + min(arm, 0), y, p['coat_mid'])
        put_pixel(d, 15 + min(arm, 0), y, p['coat_mid'])
        put_pixel(d, 32 + max(arm, 0), y, p['coat_mid'])
        put_pixel(d, 33 + max(arm, 0), y, p['coat_mid'])

    # ---- 裤子/腿 ----
    pants_y = belt_y + 2
    for y in range(pants_y, pants_y + 9):
        lx = 19 + (leg if leg > 0 else 0)
        put_pixel(d, lx, y, p['pants'])
        put_pixel(d, lx + 1, y, p['pants'])
        put_pixel(d, lx + 2, y, p['pants_light'])
        put_pixel(d, lx + 3, y, p['pants_light'])
    for y in range(pants_y, pants_y + 9):
        rx = 25 + (leg if leg < 0 else 0)
        put_pixel(d, rx, y, p['pants_light'])
        put_pixel(d, rx + 1, y, p['pants_light'])
        put_pixel(d, rx + 2, y, p['pants'])
        put_pixel(d, rx + 3, y, p['pants'])

    # ---- 靴子 ----
    boot_y = pants_y + 9
    put_pixel(d, 19 + (leg if leg > 0 else 0), boot_y, p['boot'])
    put_pixel(d, 20 + (leg if leg > 0 else 0), boot_y, p['boot_light'])
    put_pixel(d, 21 + (leg if leg > 0 else 0), boot_y, p['boot_light'])
    put_pixel(d, 22 + (leg if leg > 0 else 0), boot_y, p['boot'])
    put_pixel(d, 25 + (leg if leg < 0 else 0), boot_y, p['boot'])
    put_pixel(d, 26 + (leg if leg < 0 else 0), boot_y, p['boot_light'])
    put_pixel(d, 27 + (leg if leg < 0 else 0), boot_y, p['boot_light'])
    put_pixel(d, 28 + (leg if leg < 0 else 0), boot_y, p['boot'])

    # ---- 剑（背面可见完整的剑）----
    for y in range(by - 10, by + 6):
        put_pixel(d, 35, y, p['blade'])
    put_pixel(d, 35, by - 11, p['blade_light'])
    put_pixel(d, 35, by + 5, p['guard'])
    put_pixel(d, 35, by + 6, p['hilt'])

    # 流苏
    put_pixel(d, 36, by + 7, p['tassel'])
    put_pixel(d, 36, by + 8, p['tassel_dark'])


def draw_facing_left(img, p, body_y, arm, leg, frame):
    """侧面朝左"""
    d = ImageDraw.Draw(img)

    # ---- 头发 ----
    for x in range(19, 29):
        for y in range(4, 8):
            put_pixel(d, x, y, p['hair_dark'])
    for x in range(20, 28):
        put_pixel(d, x, 3, p['hair_dark'])
    # 侧面头发
    for y in range(8, 16):
        put_pixel(d, 18, y, p['hair_dark'])
        put_pixel(d, 19, y, p['hair_mid'])
    # 马尾向后飘
    for y in range(6, 14 + body_y):
        put_pixel(d, 29, y, p['hair_dark'])
        if y < 10:
            put_pixel(d, 30, y, p['hair_mid'])
    # 刘海
    for x in range(19, 25):
        for y in range(7, 10):
            put_pixel(d, x, y, p['hair_mid'])

    # ---- 脸部（侧面）----
    for x in range(20, 28):
        for y in range(10, 17):
            put_pixel(d, x, y, p['skin'])
    for y in range(10, 17):
        put_pixel(d, 28, y, p['skin_shadow'])
    # 眼睛（侧面一个）
    put_pixel(d, 21, 12, p['eye'])
    # 鼻子
    put_pixel(d, 19, 13, p['skin_shadow'])

    # ---- 身体（侧面较窄）----
    by = 17 + body_y
    for x in range(18, 30):
        for y in range(by, by + 14):
            put_pixel(d, x, y, p['coat_mid'])
    for y in range(by, by + 14):
        put_pixel(d, 18, y, p['coat_dark'])
        put_pixel(d, 29, y, p['coat_dark'])
    # 衣服前襟
    for y in range(by, by + 6):
        put_pixel(d, 19, y, p['inner'])

    # ---- 腰带 ----
    belt_y = by + 12
    for x in range(18, 30):
        put_pixel(d, x, belt_y, p['belt'])
        put_pixel(d, x, belt_y + 1, p['belt_dark'])

    # ---- 前臂（左）----
    for y in range(by + 2, by + 11):
        put_pixel(d, 16 + arm, y, p['coat_mid'])
        put_pixel(d, 17 + arm, y, p['coat_mid'])
    put_pixel(d, 16 + arm, by + 11, p['skin'])
    put_pixel(d, 17 + arm, by + 11, p['skin'])

    # ---- 裤子（侧面）----
    pants_y = belt_y + 2
    front_leg_x = 21 + leg
    back_leg_x = 23 - leg
    for y in range(pants_y, pants_y + 9):
        put_pixel(d, front_leg_x, y, p['pants'])
        put_pixel(d, front_leg_x + 1, y, p['pants'])
        put_pixel(d, front_leg_x + 2, y, p['pants_light'])
    for y in range(pants_y, pants_y + 9):
        put_pixel(d, back_leg_x, y, p['pants_light'])
        put_pixel(d, back_leg_x + 1, y, p['pants'])
        put_pixel(d, back_leg_x + 2, y, p['pants'])

    # ---- 靴子 ----
    boot_y = pants_y + 9
    put_pixel(d, front_leg_x, boot_y, p['boot'])
    put_pixel(d, front_leg_x + 1, boot_y, p['boot_light'])
    put_pixel(d, front_leg_x + 2, boot_y, p['boot'])
    put_pixel(d, back_leg_x, boot_y, p['boot'])
    put_pixel(d, back_leg_x + 1, boot_y, p['boot_light'])

    # ---- 剑 ----
    if frame == 1 or frame == 3:
        # 行走时剑随身体摆动
        for y in range(by - 6, by + 4):
            put_pixel(d, 31, y, p['blade'])
        put_pixel(d, 31, by - 7, p['blade_light'])
        put_pixel(d, 31, by + 4, p['guard'])
    else:
        for y in range(by - 5, by + 5):
            put_pixel(d, 31, y, p['blade'])
        put_pixel(d, 31, by - 6, p['blade_light'])
        put_pixel(d, 31, by + 5, p['guard'])

    put_pixel(d, 32, by + 5, p['tassel'])
    put_pixel(d, 32, by + 6, p['tassel_dark'])


def draw_facing_right(img, p, body_y, arm, leg, frame):
    """侧面朝右（镜像左）"""
    # 先画朝左，然后水平翻转
    left_img = draw_character('left', frame)
    # 手动镜像
    right_img = left_img.transpose(Image.FLIP_LEFT_RIGHT)
    img.paste(right_img, (0, 0), right_img)


# ============================================================
# 生成附加动画帧
# ============================================================

def draw_idle_animation():
    """待机动画（呼吸/微动）— 4帧"""
    TILE = 48
    sheet = Image.new('RGBA', (TILE * 4, TILE), (0, 0, 0, 0))
    for frame in range(4):
        sprite = Image.new('RGBA', (TILE, TILE), (0, 0, 0, 0))
        d = ImageDraw.Draw(sprite)
        p = PALETTE

        # 微微呼吸（身体上下1px）
        breath = [0, -1, 0, 1][frame]

        # 头发
        for x in range(18, 30):
            for y in range(4, 8):
                put_pixel(d, x, y, p['hair_dark'])
        for x in range(20, 28):
            put_pixel(d, x, 3, p['hair_dark'])
        for y in range(8, 16):
            put_pixel(d, 17, y, p['hair_dark'])
            put_pixel(d, 30, y, p['hair_dark'])
        for x in range(18, 30):
            for y in range(7, 10):
                put_pixel(d, x, y, p['hair_mid'])

        # 脸
        for x in range(19, 29):
            for y in range(10, 17):
                put_pixel(d, x, y, p['skin'])
        put_pixel(d, 21, 12, p['eye'])
        put_pixel(d, 26, 12, p['eye'])

        # 身体
        by = 17 + breath
        for x in range(16, 32):
            for y in range(by, by + 14):
                put_pixel(d, x, y, p['coat_mid'])
        for y in range(by, by + 14):
            put_pixel(d, 16, y, p['coat_dark'])
            put_pixel(d, 31, y, p['coat_dark'])
        for x in range(22, 26):
            for y in range(by, by + 4):
                put_pixel(d, x, y, p['inner'])

        # 腰带
        belt_y = by + 12
        for x in range(16, 32):
            put_pixel(d, x, belt_y, p['belt'])
            put_pixel(d, x, belt_y + 1, p['belt_dark'])

        # 手臂（自然下垂）
        for y in range(by + 2, by + 12):
            put_pixel(d, 14, y, p['coat_mid'])
            put_pixel(d, 15, y, p['coat_mid'])
            put_pixel(d, 32, y, p['coat_mid'])
            put_pixel(d, 33, y, p['coat_mid'])
        put_pixel(d, 14, by + 12, p['skin'])
        put_pixel(d, 15, by + 12, p['skin'])
        put_pixel(d, 32, by + 12, p['skin'])
        put_pixel(d, 33, by + 12, p['skin'])

        # 裤子
        pants_y = belt_y + 2
        for y in range(pants_y, pants_y + 9):
            for x in [19, 20, 21]:
                put_pixel(d, x, y, p['pants'])
            for x in [22, 23]:
                put_pixel(d, x, y, p['pants_light'])
            for x in [25, 26]:
                put_pixel(d, x, y, p['pants_light'])
            for x in [27, 28, 29]:
                put_pixel(d, x, y, p['pants'])

        # 靴子
        boot_y = pants_y + 9
        for x in [19, 20, 21, 22]:
            put_pixel(d, x, boot_y, p['boot_light'] if x in [20, 21] else p['boot'])
        for x in [25, 26, 27, 28]:
            put_pixel(d, x, boot_y, p['boot_light'] if x in [26, 27] else p['boot'])

        # 剑（背在身后）
        for y in range(by - 8, by - 2):
            put_pixel(d, 34, y, p['blade'])
        put_pixel(d, 34, by - 9, p['blade_light'])
        put_pixel(d, 34, by - 1, p['guard'])

        # 流苏随风动
        tassel_offset = [0, 1, 0, -1][frame]
        put_pixel(d, 35, by - 1 + tassel_offset, p['tassel'])
        put_pixel(d, 36, by + tassel_offset, p['tassel_dark'])
        put_pixel(d, 35, by + 1 + tassel_offset, p['tassel_dark'])

        sheet.paste(sprite, (frame * TILE, 0), sprite)

    return sheet


def draw_attack_animation():
    """攻击动画（剑劈）— 6帧，面向下"""
    TILE = 48
    sheet = Image.new('RGBA', (TILE * 6, TILE), (0, 0, 0, 0))
    p = PALETTE

    # 6帧：蓄力→举剑→劈下→到位→收回→恢复
    for frame in range(6):
        sprite = Image.new('RGBA', (TILE, TILE), (0, 0, 0, 0))
        d = ImageDraw.Draw(sprite)

        # 头发
        for x in range(18, 30):
            for y in range(4, 8):
                put_pixel(d, x, y, p['hair_dark'])
        for x in range(20, 28):
            put_pixel(d, x, 3, p['hair_dark'])
        for y in range(8, 16):
            put_pixel(d, 17, y, p['hair_dark'])
            put_pixel(d, 30, y, p['hair_dark'])
        for x in range(18, 30):
            for y in range(7, 10):
                put_pixel(d, x, y, p['hair_mid'])

        # 脸
        for x in range(19, 29):
            for y in range(10, 17):
                put_pixel(d, x, y, p['skin'])
        # 不同帧表情变化
        if frame == 2:  # 劈下瞬间，眼睛更锐利
            put_pixel(d, 21, 12, p['eye'])
            put_pixel(d, 22, 12, p['eye'])
            put_pixel(d, 26, 12, p['eye'])
            put_pixel(d, 27, 12, p['eye'])
        else:
            put_pixel(d, 21, 12, p['eye'])
            put_pixel(d, 26, 12, p['eye'])

        # 身体（攻击时微倾）
        body_offset = [0, -1, 1, 2, 1, 0][frame]
        by = 17 + body_offset
        for x in range(16, 32):
            for y in range(by, by + 14):
                put_pixel(d, x, y, p['coat_mid'])
        for y in range(by, by + 14):
            put_pixel(d, 16, y, p['coat_dark'])
            put_pixel(d, 31, y, p['coat_dark'])
        for x in range(22, 26):
            for y in range(by, by + 4):
                put_pixel(d, x, y, p['inner'])

        # 腰带
        belt_y = by + 12
        for x in range(16, 32):
            put_pixel(d, x, belt_y, p['belt'])
            put_pixel(d, x, belt_y + 1, p['belt_dark'])

        # 裤子
        pants_y = belt_y + 2
        for y in range(pants_y, pants_y + 9):
            for x in [19, 20, 21, 22]:
                put_pixel(d, x, y, p['pants'] if x < 21 else p['pants_light'])
            for x in [25, 26, 27, 28]:
                put_pixel(d, x, y, p['pants_light'] if x < 27 else p['pants'])
        boot_y = pants_y + 9
        for x in [19, 20, 21, 22]:
            put_pixel(d, x, boot_y, p['boot_light'] if x in [20, 21] else p['boot'])
        for x in [25, 26, 27, 28]:
            put_pixel(d, x, boot_y, p['boot_light'] if x in [26, 27] else p['boot'])

        # ---- 剑动画 ----
        # 左手臂持剑
        if frame == 0:  # 蓄力：剑在身后
            for y in range(by - 8, by - 2):
                put_pixel(d, 14, y, p['blade'])
            put_pixel(d, 14, by - 9, p['blade_light'])
            put_pixel(d, 14, by - 1, p['guard'])
        elif frame == 1:  # 举剑
            for x in range(12, 20):
                put_pixel(d, x, by - 6, p['blade'])
            put_pixel(d, 12, by - 6, p['blade_light'])
            put_pixel(d, 19, by - 6, p['guard'])
            put_pixel(d, 14, by - 3, p['hilt'])
        elif frame == 2:  # 劈下！（剑在身前）
            for x in range(10, 22):
                put_pixel(d, x, by + 8, p['blade'])
            put_pixel(d, 10, by + 8, p['blade_light'])
            put_pixel(d, 21, by + 7, p['guard'])
            # 剑光特效
            put_pixel(d, 9, by + 8, (255, 255, 255, 180))
            put_pixel(d, 22, by + 8, (255, 255, 255, 180))
        elif frame == 3:  # 到位
            for x in range(14, 24):
                put_pixel(d, x, by + 10, p['blade'])
            put_pixel(d, 14, by + 10, p['blade_light'])
            put_pixel(d, 23, by + 9, p['guard'])
        elif frame == 4:  # 收回
            for y in range(by - 4, by + 2):
                put_pixel(d, 13, y, p['blade'])
            put_pixel(d, 13, by - 5, p['blade_light'])
            put_pixel(d, 13, by + 2, p['guard'])
        else:  # 恢复
            for y in range(by - 6, by - 2):
                put_pixel(d, 14, y, p['blade'])
            put_pixel(d, 14, by - 7, p['blade_light'])
            put_pixel(d, 14, by - 1, p['guard'])

        # 持剑手
        if frame in [0, 4, 5]:
            put_pixel(d, 14, by + 10, p['skin'])
            put_pixel(d, 15, by + 10, p['skin'])
        elif frame == 1:
            put_pixel(d, 16, by - 2, p['skin'])
            put_pixel(d, 17, by - 2, p['skin'])
        elif frame == 2:
            put_pixel(d, 20, by + 5, p['skin'])
            put_pixel(d, 21, by + 5, p['skin'])
        elif frame == 3:
            put_pixel(d, 22, by + 8, p['skin'])
            put_pixel(d, 23, by + 8, p['skin'])

        # 右臂
        for y in range(by + 2, by + 12):
            put_pixel(d, 32, y, p['coat_mid'])
            put_pixel(d, 33, y, p['coat_mid'])
        put_pixel(d, 32, by + 12, p['skin'])
        put_pixel(d, 33, by + 12, p['skin'])

        sheet.paste(sprite, (frame * TILE, 0), sprite)

    return sheet


# ============================================================
# 生成调色板参考图
# ============================================================

def draw_palette_reference():
    """生成调色板色板参考"""
    colors = [
        ('hair_dark', '头发深'), ('hair_mid', '头发中'), ('hair_light', '头发亮'),
        ('skin', '皮肤'), ('skin_shadow', '皮肤阴影'), ('skin_light', '皮肤亮'),
        ('coat_dark', '外衣深'), ('coat_mid', '外衣中'), ('coat_light', '外衣亮'),
        ('inner', '内衬'), ('inner_shade', '内衬阴影'),
        ('belt', '腰带'), ('belt_dark', '腰带深'),
        ('pants', '裤子'), ('pants_light', '裤子亮'),
        ('boot', '靴子'), ('boot_light', '靴子亮'),
        ('blade', '剑身'), ('blade_light', '剑身高光'), ('guard', '护手'), ('hilt', '剑柄'),
        ('eye', '眼睛'),
        ('tassel', '流苏'), ('tassel_dark', '流苏深'),
    ]

    TILE = 24
    cols = 6
    rows = (len(colors) + cols - 1) // cols
    img = Image.new('RGBA', (TILE * cols, TILE * rows + 20), (40, 40, 50))
    d = ImageDraw.Draw(img)

    for i, (name, label) in enumerate(colors):
        col = i % cols
        row = i // cols
        x = col * TILE + 2
        y = row * TILE + 18
        color = PALETTE[name]
        d.rectangle([x, y, x + TILE - 4, y + TILE - 4], fill=color[:3])
        d.rectangle([x, y, x + TILE - 4, y + TILE - 4], outline=(200, 200, 200))

    return img


# ============================================================
# 主函数
# ============================================================

def main():
    output_dir = os.path.join(os.path.dirname(os.path.dirname(__file__)), 'assets', 'sprites')
    os.makedirs(output_dir, exist_ok=True)

    print("🗡️  生成剑客「凌霜」像素精灵表...")
    print(f"   输出目录: {output_dir}")

    # 1. 行走精灵表
    print("   [1/4] 行走动画 (4方向 x 4帧)")
    walk_sheet = create_sprite_sheet()
    walk_path = os.path.join(output_dir, 'hero_lingshuang_walk.png')
    walk_sheet.save(walk_path)
    print(f"   ✓ 保存: {walk_path} ({walk_sheet.size[0]}x{walk_sheet.size[1]})")

    # 2. 待机动画
    print("   [2/4] 待机动画 (4帧)")
    idle_sheet = draw_idle_animation()
    idle_path = os.path.join(output_dir, 'hero_lingshuang_idle.png')
    idle_sheet.save(idle_path)
    print(f"   ✓ 保存: {idle_path} ({idle_sheet.size[0]}x{idle_sheet.size[1]})")

    # 3. 攻击动画
    print("   [3/4] 攻击动画 (6帧)")
    attack_sheet = draw_attack_animation()
    attack_path = os.path.join(output_dir, 'hero_lingshuang_attack.png')
    attack_sheet.save(attack_path)
    print(f"   ✓ 保存: {attack_path} ({attack_sheet.size[0]}x{attack_sheet.size[1]})")

    # 4. 调色板参考
    print("   [4/4] 调色板参考")
    palette_img = draw_palette_reference()
    palette_path = os.path.join(output_dir, 'palette_reference.png')
    palette_img.save(palette_path)
    print(f"   ✓ 保存: {palette_path}")

    # 放大版本（便于预览）
    print("\n   生成放大预览版 (3x)...")
    scale = 3
    for name in ['hero_lingshuang_walk', 'hero_lingshuang_idle', 'hero_lingshuang_attack']:
        src = os.path.join(output_dir, f'{name}.png')
        img = Image.open(src)
        big = img.resize((img.width * scale, img.height * scale), Image.NEAREST)
        preview_dir = os.path.join(output_dir, 'preview')
        os.makedirs(preview_dir, exist_ok=True)
        big.save(os.path.join(preview_dir, f'{name}_3x.png'))
        print(f"   ✓ {name}_3x.png ({big.size[0]}x{big.size[1]})")

    print("\n✅ 全部完成！")
    print(f"   精灵表目录: {output_dir}")
    print(f"   预览目录:   {os.path.join(output_dir, 'preview')}")
    print("\n   📋 精灵表规格:")
    print("   行走: 48x48 per frame | 4方向 x 4帧 = 192x192")
    print("   待机: 48x48 per frame | 4帧 = 192x48")
    print("   攻击: 48x48 per frame | 6帧 = 288x48")
    print("   调色板: 24色武侠风格")


if __name__ == '__main__':
    main()
