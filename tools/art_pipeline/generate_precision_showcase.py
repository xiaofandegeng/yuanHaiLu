"""Generate high-resolution (4x scaled) visual showcase for handcrafted pixel art."""

from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

PROJECT_ROOT = Path(__file__).resolve().parents[2]
OUTPUT_DIR = PROJECT_ROOT / "Assets/Art/MVP/previews"
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

SCALE = 4


def load_scaled(path, scale=SCALE):
    with Image.open(path) as img:
        img = img.convert("RGBA")
        return img.resize((img.width * scale, img.height * scale), Image.Resampling.NEAREST)


def build_precision_master_showcase():
    actors_dir = PROJECT_ROOT / "Assets/Resources/Art/MVP/dense_pixel/actors"
    env_town_dir = PROJECT_ROOT / "Assets/Art/MVP/dense_pixel/environment/town"
    env_inn_dir = PROJECT_ROOT / "Assets/Art/MVP/dense_pixel/environment/inn"
    weapons_dir = PROJECT_ROOT / "Assets/Resources/Art/MVP"

    # 画布大小
    W, H = 1000, 720
    canvas = Image.new("RGBA", (W, H), (20, 18, 28, 255))
    draw = ImageDraw.Draw(canvas)

    # 标题背景装饰
    draw.rectangle((0, 0, W, 64), fill=(35, 30, 48, 255))
    draw.line((0, 64, W, 64), fill=(215, 160, 45, 255), width=2)

    # 1. 角色与 NPC 区域 (y: 80~320)
    draw.rectangle((20, 80, 980, 310), fill=(28, 24, 38, 255), outline=(60, 52, 78, 255), width=1)
    
    # 加载角色
    pouch = load_scaled(actors_dir / "mvp_lost_pouch.png", scale=4)  # 64x64
    innkeeper = load_scaled(actors_dir / "mvp_innkeeper.png", scale=4)  # 192x192
    bandit_a = load_scaled(actors_dir / "mvp_bandit_a.png", scale=4)  # 192x192
    bandit_b = load_scaled(actors_dir / "mvp_bandit_b.png", scale=4)  # 192x192

    # 放置
    canvas.alpha_composite(innkeeper, (50, 95))
    canvas.alpha_composite(bandit_a, (280, 95))
    canvas.alpha_composite(bandit_b, (510, 95))
    canvas.alpha_composite(pouch, (760, 160))

    # 2. 三神兵武器区域 (y: 330~510)
    draw.rectangle((20, 330, 980, 510), fill=(28, 24, 38, 255), outline=(60, 52, 78, 255), width=1)
    sword = load_scaled(weapons_dir / "weapon_sword.png", scale=3)  # 144x144
    gauntlets = load_scaled(weapons_dir / "weapon_gauntlets.png", scale=3)
    dart = load_scaled(weapons_dir / "weapon_dart.png", scale=3)

    canvas.alpha_composite(sword, (80, 345))
    canvas.alpha_composite(gauntlets, (410, 345))
    canvas.alpha_composite(dart, (740, 345))

    # 3. 场景小模块 (y: 530~920)
    W, H = 1000, 940
    # 重新初始化更大画布
    canvas_large = Image.new("RGBA", (W, H), (20, 18, 28, 255))
    canvas_large.paste(canvas.crop((0, 0, 1000, 520)), (0, 0))
    draw_l = ImageDraw.Draw(canvas_large)
    draw_l.rectangle((20, 530, 980, 910), fill=(28, 24, 38, 255), outline=(60, 52, 78, 255), width=1)

    bridge = load_scaled(env_town_dir / "bridge.png", scale=3)  # 144x144
    boat = load_scaled(env_town_dir / "boat.png", scale=3)
    table = load_scaled(env_inn_dir / "table.png", scale=3)
    shelf = load_scaled(env_inn_dir / "shelf.png", scale=3)
    stove = load_scaled(env_inn_dir / "stove.png", scale=3)
    inn_door = load_scaled(env_town_dir / "inn_door.png", scale=3)
    inn_sign = load_scaled(env_town_dir / "inn_sign.png", scale=3)
    window = load_scaled(env_inn_dir / "window_light.png", scale=3)
    lantern = load_scaled(env_town_dir / "lantern.png", scale=4)

    # 第一排
    canvas_large.alpha_composite(bridge, (50, 545))
    canvas_large.alpha_composite(boat, (240, 545))
    canvas_large.alpha_composite(table, (430, 545))
    canvas_large.alpha_composite(stove, (620, 545))
    canvas_large.alpha_composite(lantern, (820, 570))

    # 第二排
    canvas_large.alpha_composite(inn_door, (60, 740))
    canvas_large.alpha_composite(inn_sign, (240, 740))
    canvas_large.alpha_composite(shelf, (430, 740))
    canvas_large.alpha_composite(window, (620, 740))

    out_path = OUTPUT_DIR / "precision_chibi_master_showcase.png"
    canvas_large.save(out_path)
    print("Saved:", out_path)


def build_hero_action_showcase():
    hero_dir = PROJECT_ROOT / "Assets/Art/Characters/Player/Generated/player_male_swordsman"
    # 加载已烘焙主角图
    sheet_path = PROJECT_ROOT / "Assets/ArtSource/Characters/Generated/player_male_swordsman/body.png"
    # 从 6 个源图层合成完整帧
    layers = []
    for name in ("accessory", "body", "outfit", "face", "hair", "weapon"):
        p = PROJECT_ROOT / "Assets/ArtSource/Characters/Generated/player_male_swordsman" / (name + ".png")
        layers.append(Image.open(p).convert("RGBA"))

    base = Image.new("RGBA", layers[0].size, (0, 0, 0, 0))
    for l in layers:
        base.alpha_composite(l)

    # 放大 4 倍
    scaled = base.resize((base.width * 3, base.height * 3), Image.Resampling.NEAREST)
    out_path = OUTPUT_DIR / "precision_hero_action_sheet.png"
    scaled.save(out_path)
    print("Saved:", out_path)


if __name__ == "__main__":
    build_precision_master_showcase()
    build_hero_action_showcase()
