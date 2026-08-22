#!/usr/bin/env python3
"""docs/16 阶段 D 构图缩样生成器（E.1：三张 ≤160×90、无 UI、非最终像素资源）。

用途：向用户提案烟柳出生/主路、烟柳河岸、客栈掌柜三个镜头的焦点、路线与
明度层级；批准后才进入 Tile/烘焙返工。坐标全部来自两个 Demo 场景的真实
玩法坐标（出生点/客栈门/水匪/荷包/掌柜/出入口）与 yanliu/inn 布局的水体
轮廓，缩放比例 1 游戏像素 = 1/3 缩样像素（480×270 → 160×90）。

色块约定（亮度即路线）：
  亮暖主路 > 可走地面 > 岸线 > 地标木色 > 建筑深木 > 水面(最暗)
  主焦点 = 暖光池；男主深色轮廓小人；敌人暗红；荷包金色；NPC 灰蓝
"""

from PIL import Image, ImageDraw

SIZE = (160, 90)          # 缩样尺寸（≤160×90）
VIEW_W, VIEW_H = 30.0, 16.875   # 480×270 @ PPU16 的世界视口

# --- 调色板（水乡低饱和） ---
WALK = (196, 188, 160)
ROAD = (226, 206, 162)
SHORE = (214, 190, 140)
WATER = (74, 104, 120)
WATER_DEEP = (58, 84, 100)
WOOD = (96, 76, 58)
WOOD_DARK = (56, 44, 36)
LANDMARK = (190, 142, 86)
WARM = (255, 214, 140)
HERO_BODY = (38, 34, 32)
HERO_HEAD = (232, 198, 162)
ENEMY = (122, 52, 44)
NPC = (108, 110, 124)
POUCH = (234, 182, 72)
WILLOW = (74, 100, 74)


def world_to_px(cx, cy):
    """返回世界坐标 → 缩样像素的映射函数（y 翻转）。"""
    sx = SIZE[0] / VIEW_W
    sy = SIZE[1] / VIEW_H

    def map_(wx, wy):
        return (int((wx - cx + VIEW_W / 2) * sx), int((cy + VIEW_H / 2 - wy) * sy))
    return map_


def rect_world(draw, m, x0, y0, x1, y1, color, outline=None):
    """世界坐标矩形（x0<x1, y0<y1，y 向上）。"""
    px0, py0 = m(x0, y1)
    px1, py1 = m(x1, y0)
    draw.rectangle([px0, py0, px1, py1], fill=color, outline=outline)


def warm_pool(img, m, wx, wy, radius, peak=110):
    """主焦点暖光池：径向渐变叠加。"""
    cx, cy = m(wx, wy)
    r = int(radius * SIZE[0] / VIEW_W)
    overlay = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    od = ImageDraw.Draw(overlay)
    for i in range(r, 0, -1):
        alpha = int(peak * (1 - i / r))
        od.ellipse([cx - i, cy - i * 2 // 3, cx + i, cy + i * 2 // 3],
                   fill=WARM + (alpha,))
    return Image.alpha_composite(img.convert("RGBA"), overlay).convert("RGB")


def actor(draw, m, wx, wy, body, head=HERO_HEAD, facing="down"):
    """约 10px 高的小人：头/身/朝向点。"""
    x, y = m(wx, wy)
    draw.rectangle([x - 1, y - 4, x + 1, y + 1], fill=body)      # 身
    draw.rectangle([x - 1, y - 7, x + 1, y - 5], fill=head)      # 头
    dx, dy = {"down": (0, 2), "up": (0, -6), "left": (-3, -2), "right": (3, -2)}[facing]
    draw.point((x + dx, y + dy), fill=POUCH if body == HERO_BODY else body)


def base_frame():
    img = Image.new("RGB", SIZE, WALK)
    return img, ImageDraw.Draw(img)


def vignette(img):
    """前景遮挡只压画面边缘，不遮焦点/路线（D.1）。"""
    overlay = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    od = ImageDraw.Draw(overlay)
    w, h = SIZE
    od.rectangle([0, 0, w, 3], fill=(20, 24, 22, 90))
    od.rectangle([0, 0, 2, h], fill=(20, 24, 22, 70))
    od.rectangle([w - 3, 0, w, h], fill=(20, 24, 22, 70))
    od.rectangle([0, h - 2, w, h], fill=(20, 24, 22, 60))
    return Image.alpha_composite(img.convert("RGBA"), overlay).convert("RGB")


# ---------------------------------------------------------------- T1 烟柳出生/主路
def thumbnail_town_spawn():
    img, d = base_frame()
    m = world_to_px(7.5, 8.6)

    # 水面：视口右上（yanliu 水体 x13+、y7+），深处更暗
    rect_world(d, m, 13.0, 9.5, 22.6, 17.1, WATER)
    rect_world(d, m, 14.5, 11.0, 22.6, 17.1, WATER_DEEP)
    # 岸线：水陆交界的暖沙窄带
    rect_world(d, m, 12.2, 9.5, 13.0, 17.1, SHORE)
    # 主路：客栈门向南 → 折向东去河岸（全程最亮）
    rect_world(d, m, 6.6, 5.2, 8.4, 12.0, ROAD)
    rect_world(d, m, 6.6, 4.4, 22.6, 6.2, ROAD)
    # 客栈建筑：左上深木块 + 门框（主入口，南面开门）
    rect_world(d, m, 4.0, 12.0, 9.0, 15.0, WOOD, WOOD_DARK)
    rect_world(d, m, 6.9, 11.2, 8.1, 12.0, WOOD_DARK)          # 门洞
    d.rectangle([m(6.0, 11.6)[0], m(6.0, 11.6)[1], m(6.5, 11.1)[0], m(6.5, 11.1)[1]],
                fill=LANDMARK)                                   # 门口招牌
    # 拱桥：水上转向点地标（单一地标提示转向，D.2）
    rect_world(d, m, 17.6, 11.3, 21.0, 12.4, LANDMARK, WOOD_DARK)
    # 柳树：岸线两簇大轮廓，不抢焦点
    for wx, wy in ((11.6, 10.2), (21.8, 13.6)):
        x, y = m(wx, wy)
        d.ellipse([x - 3, y - 3, x + 3, y + 3], fill=WILLOW, outline=WOOD_DARK)

    img = warm_pool(img, m, 7.5, 11.4, 3.4)     # 主焦点：客栈门暖光
    img = warm_pool(img, m, 19.3, 11.8, 1.6, 60)  # 次焦点：桥
    di = ImageDraw.Draw(img)
    actor(di, m, 7.5, 7.6, HERO_BODY, facing="up")   # 男主出生，面朝客栈门
    return vignette(img)


# ---------------------------------------------------------------- T2 烟柳河岸
def thumbnail_town_riverbank():
    img, d = base_frame()
    m = world_to_px(15.0, 4.2)

    # 水面：视口上部横带（水体 y7+），东端更宽更深
    rect_world(d, m, 12.2, 7.0, 30.1, 12.9, WATER)
    rect_world(d, m, 20.0, 7.8, 30.1, 12.9, WATER_DEEP)
    # 岸线：南岸暖沙带 + 岸阶（可战斗陆地北缘）
    rect_world(d, m, 0.0, 6.2, 30.1, 7.0, SHORE)
    # 主路（官道）：沿南岸东西向，最亮
    rect_world(d, m, 0.0, 4.4, 30.1, 6.2, ROAD)
    # 岸灯：转向/战斗点地标（单一地标，D.2）
    lamp_x, lamp_y = m(12.5, 6.8)
    d.rectangle([lamp_x - 1, lamp_y - 6, lamp_x + 1, lamp_y], fill=LANDMARK)
    d.rectangle([lamp_x - 2, lamp_y - 8, lamp_x + 2, lamp_y - 6], fill=WARM)
    # 芦苇/柳簇：水缘两簇
    for wx, wy in ((19.0, 7.4), (27.6, 7.4)):
        x, y = m(wx, wy)
        d.ellipse([x - 3, y - 3, x + 3, y + 2], fill=WILLOW, outline=WOOD_DARK)

    img = warm_pool(img, m, 15.5, 4.0, 3.8)       # 主焦点：两名水匪的战斗区
    img = warm_pool(img, m, 24.0, 3.0, 1.8, 70)   # 次焦点：荷包
    di = ImageDraw.Draw(img)
    actor(di, m, 15.5, 4.2, HERO_BODY, facing="up")       # 男主自官道迎向水匪
    actor(di, m, 14.0, 3.2, ENEMY, (188, 150, 128), "down")
    actor(di, m, 17.0, 2.6, ENEMY, (188, 150, 128), "down")
    px, py = m(24.0, 3.0)                                   # 荷包
    di.rectangle([px - 2, py - 2, px + 2, py + 2], fill=POUCH, outline=WOOD_DARK)
    return vignette(img)


# ---------------------------------------------------------------- T3 客栈掌柜
def thumbnail_inn_counter():
    img, d = base_frame()
    m = world_to_px(9.5, 6.6)

    # 室内分区：大轮廓区分（D.3），地板基色 = 可走
    rect_world(d, m, 0.0, 0.0, 16.0, 12.0, WALK)
    # 墙体：北墙 + 东西墙，深木
    rect_world(d, m, 0.0, 11.0, 16.0, 12.0, WOOD, WOOD_DARK)
    rect_world(d, m, 0.0, 0.0, 1.0, 11.0, WOOD, WOOD_DARK)
    rect_world(d, m, 15.0, 0.0, 16.0, 11.0, WOOD, WOOD_DARK)
    # 柜台：主焦点（画面中北），横木台 + 掌柜站台后
    rect_world(d, m, 6.4, 7.4, 10.6, 8.3, WOOD, WOOD_DARK)
    # 后厨/灶火：左上次光区（暗轮廓）
    rect_world(d, m, 1.0, 8.3, 4.2, 11.0, WOOD_DARK)
    # 桌席：右侧两桌（次区剪影，不填满）
    for wx in (12.2, 13.8):
        rect_world(d, m, wx, 5.6, wx + 1.2, 6.8, WOOD, WOOD_DARK)
    # 楼梯/暗口：右上暗轮廓
    rect_world(d, m, 13.2, 9.0, 15.0, 11.0, WOOD_DARK)
    # 主通道：入口 → 柜台 → 出口（最亮路线）
    rect_world(d, m, 10.6, 1.8, 12.4, 7.4, ROAD)
    rect_world(d, m, 8.0, 4.6, 10.6, 6.4, ROAD)

    img = warm_pool(img, m, 8.5, 8.0, 4.2)        # 主光：柜台
    img = warm_pool(img, m, 2.6, 9.2, 1.8, 80)    # 次光：灶火
    img = warm_pool(img, m, 12.6, 9.2, 1.4, 55)   # 次光：窗光
    di = ImageDraw.Draw(img)
    actor(di, m, 8.5, 8.9, NPC, (214, 196, 170), "down")  # 掌柜老赵（柜台后）
    actor(di, m, 11.5, 2.5, HERO_BODY, facing="up")       # 男主入口入场
    return vignette(img)


def main():
    import os
    out = os.path.dirname(os.path.abspath(__file__))
    for name, fn in (("town-spawn-thumb.png", thumbnail_town_spawn),
                     ("town-riverbank-thumb.png", thumbnail_town_riverbank),
                     ("inn-counter-thumb.png", thumbnail_inn_counter)):
        path = os.path.join(out, name)
        fn().save(path)
        print("wrote", path)


if __name__ == "__main__":
    main()
