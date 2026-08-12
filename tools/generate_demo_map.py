#!/usr/bin/env python3
"""
「烟柳镇」示范地图 — 使用 tileset 拼接一个20x15的场景
展示水乡布局: 河流/桥梁/建筑/柳树/荷花
"""
from PIL import Image, ImageDraw
import os

T = 16
COLS, ROWS = 30, 20  # 地图格数

# 地图数据 — 每格为 (row, col) 指向 tileset 中的瓦片
# 格式: 'G'=草地 'g'=深草 'D'=泥土 'S'=石板 'W'=水 'w'=深水 's'=闪光水
#        'ST'=岸上 'SB'=岸下 'SL'=岸左 'SR'=岸右
#        'BW'=白墙 'BB'=石基白墙 'WO'=木墙 'WI'=窗 'DR'=门 'DO'=开着的门
#        'RF'=屋顶 'RE'=飞檐 'RL'=屋顶左 'RR'=屋顶右 'RD'=屋脊
#        'WL'=柳树 'LT'=荷花 'LN'=灯笼 'FL'=旗帜 'BR'=桶 'CR'=箱
#        'BG'=桥 'BA'=桥栏 'SS'=踏石 'BT'=船 'DK'=码头
#        '.'=透明/碰撞

# 简化: 用字符串数组表示
MAP = [
    # 0         1         2
    # 0123456789012345678901234567890
    "WWWWWWWWWWWWWWWWWWWWWWWWWWWWWW", # 0
    "WwWwWWsWWwWwWWsWWwWwWWsWWWWWW", # 1
    "WWWWWWWWWWWWWWWWWWWWWWWWWWWWWW", # 2
    "WWWWWWWWWGGGGGWWWWWWGGGGGWWWWW", # 3
    "WWWWWSTSTSTSTSTSWWWSTSTSTSTSWW", # 4
    "GGGGGGGGGGGGGGGGGGGGGGGGGGGGGG", # 5  岸线
    "GGDDSSGGGGDDGGGGDDSSGGGGDDGGGG", # 6  泥路+石板
    "GGBBGGGGBRBBGGGGBBGGGGBRBBGGGG", # 7  桥区
    "GGBAGGGGBABAGGGGBAGGGGBABAGGGG", # 8  桥栏
    "GGBBGGGGBRBBGGGGBBGGGGBRBBGGGG", # 9  桥
    "GGDDGGGGDDGGGGGGDDGGGGDDGGGGGG", # 10
    "GGGGGGGGGGGGGGGGGGGGGGGGGGGGGG", # 11
    "GGBWBBWBWBBWBBBWGGBWBBWBWBBWWG", # 12 墙+窗
    "GGBWBBWIWBBWDRBWWGBWBBWIWBBWWG", # 13 墙+窗+门
    "GGRFRFRFRFRFRLGGGGRFRFRFRFRFRR", # 14 屋顶
    "GGREREREREREREGGGGRERERERERERR", # 15 飞檐
    "GGRDRDRDRDRDRGGGGGRDRDRDRDRDGG", # 16 屋脊
    "GGLNWLGGGGLNWLGWLGLNWLGGGGLNWW", # 17 柳树+灯笼
    "GGDDGGGGGGDDGGGGGGDDGGGGGGDDGG", # 18
    "GGSDDGGGGSDDGGGGGSDDGGGGSDDGGG", # 19
]

# 每个字符对应的 tileset 坐标 (tile_col, tile_row)
TILE_MAP = {
    'G': (0, 0), 'g': (6, 0), 'D': (10, 0), 'S': (14, 0),
    'W': (0, 1), 'w': (8, 1), 's': (12, 1),
    'T': (0, 2),  # shore top
    'B': (0, 10), # bridge
    'A': (8, 10), # bridge rail
    'R': (0, 6),  # roof
    'E': (8, 6),  # eave
    'L': (4, 8),  # willow
    'N': (8, 8),  # lantern
    'F': (12, 8), # flag
    'C': (20, 8), # crate
    'I': (16, 4), # window
}

def main():
    base = os.path.dirname(os.path.dirname(__file__))
    tileset_path = os.path.join(base, 'assets', 'tilesets', 'yanliu_town_tileset.png')
    tileset = Image.open(tileset_path)
    out = os.path.join(base, 'assets', 'tilesets')

    map_w, map_h = COLS * T, ROWS * T
    map_img = Image.new('RGBA', (map_w, map_h), (0, 0, 0, 0))

    print("🏘️  生成烟柳镇示范地图...")
    print(f"   地图大小: {map_w}x{map_h} ({COLS}x{ROWS} tiles)")

    for row_idx, row_str in enumerate(MAP):
        for col_idx, ch in enumerate(row_str):
            if ch in TILE_MAP:
                tc, tr = TILE_MAP[ch]
                tile = tileset.crop((tc*T, tr*T, (tc+1)*T, (tr+1)*T))
                map_img.paste(tile, (col_idx*T, row_idx*T), tile)
            elif ch == '.':
                pass  # transparent

    path = os.path.join(out, 'yanliu_town_demo.png')
    map_img.save(path)
    print(f"   ✓ 保存: {path} ({os.path.getsize(path)} bytes)")

    # 放大预览
    for scale in [2, 4, 8]:
        big = map_img.resize((map_w*scale, map_h*scale), Image.NEAREST)
        p = os.path.join(out, f'yanliu_town_demo_{scale}x.png')
        big.save(p)
        print(f"   ✓ 预览 {scale}x: {p}")

    print("\n✅ 示范地图生成完成！")

if __name__ == '__main__':
    main()
