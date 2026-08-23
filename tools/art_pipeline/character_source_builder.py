"""Build committed, editable 32px character source sheets from design records."""

import argparse
import hashlib
import json
from dataclasses import asdict, dataclass
from pathlib import Path

from PIL import Image, ImageDraw

from .character_roster import MANIFEST_ROOT, build_roster
from .source_audit import REQUIRED_LAYERS, assert_character_sources_complete


PROJECT_ROOT = Path(__file__).resolve().parents[2]
DESIGN_PATH = (
    PROJECT_ROOT
    / "Assets"
    / "ArtSource"
    / "Characters"
    / "Designs"
    / "character-designs.json"
)
GENERATED_ROOT = PROJECT_ROOT / "Assets" / "ArtSource" / "Characters" / "Generated"
MVP_HERO_ID = "player_male_swordsman"
MANIFEST_NAMES = (
    "player-roster.json",
    "named-roster.json",
    "npc-roster.json",
    "enemy-roster.json",
    "boss-roster.json",
)


PALETTES = {
    "ink_blue": ((23, 33, 49), (54, 81, 110), (115, 155, 184)),
    "paper_white": ((83, 68, 55), (204, 194, 164), (239, 231, 204)),
    "river_blue": ((20, 48, 76), (39, 102, 142), (103, 170, 190)),
    "vermilion": ((73, 22, 25), (151, 48, 38), (217, 92, 56)),
    "deep_purple": ((44, 26, 58), (85, 48, 106), (150, 91, 153)),
    "bamboo_green": ((29, 58, 47), (65, 111, 76), (134, 166, 94)),
    "warm_brown": ((68, 43, 29), (123, 78, 44), (187, 127, 63)),
    "desert_ochre": ((86, 49, 28), (160, 101, 47), (221, 166, 82)),
    "snow_slate": ((42, 56, 74), (94, 125, 146), (186, 211, 213)),
    "jade_green": ((21, 56, 48), (43, 118, 90), (104, 181, 133)),
    "ash_gray": ((41, 43, 47), (91, 91, 88), (153, 143, 125)),
    "gold": ((86, 59, 24), (176, 125, 36), (239, 202, 100)),
    "skin": ((91, 53, 37), (183, 120, 84), (237, 183, 127)),
}


REGION_PALETTES = {
    "tianshu": ("ink_blue", "paper_white", "gold"),
    "cangyue": ("snow_slate", "jade_green", "paper_white"),
    "yanliu": ("river_blue", "bamboo_green", "paper_white"),
    "chisha": ("desert_ochre", "warm_brown", "vermilion"),
    "youhuang": ("jade_green", "deep_purple", "bamboo_green"),
    "hanyuan": ("snow_slate", "paper_white", "river_blue"),
}


@dataclass(frozen=True)
class CharacterDesign:
    id: str
    silhouette: str
    palette: tuple
    hair_style: str
    outfit_style: str
    prop_style: str
    accent_style: str

    @property
    def signature(self):
        return (
            self.silhouette,
            self.palette,
            self.hair_style,
            self.outfit_style,
            self.prop_style,
            self.accent_style,
        )

    @classmethod
    def from_payload(cls, art_id, payload):
        required = ("silhouette", "palette", "hairStyle", "outfitStyle", "propStyle", "accentStyle")
        missing = [field for field in required if field not in payload]
        if missing:
            raise ValueError("{} design missing {}".format(art_id, ", ".join(missing)))
        palette = tuple(payload["palette"])
        if len(palette) != 3 or any(color not in PALETTES for color in palette):
            raise ValueError("{} design palette must name three known colors".format(art_id))
        values = [payload[field] for field in required if field != "palette"]
        if not all(isinstance(value, str) and value for value in values):
            raise ValueError("{} design fields must be non-empty strings".format(art_id))
        return cls(
            art_id,
            payload["silhouette"],
            palette,
            payload["hairStyle"],
            payload["outfitStyle"],
            payload["propStyle"],
            payload["accentStyle"],
        )


def load_character_designs(path=DESIGN_PATH):
    payload = json.loads(Path(path).read_text(encoding="utf-8"))
    if payload.get("schemaVersion") != 1 or not isinstance(payload.get("designs"), dict):
        raise ValueError("character designs require schemaVersion 1 and a designs object")
    return {
        art_id: CharacterDesign.from_payload(art_id, design)
        for art_id, design in payload["designs"].items()
    }


def build_character_sources(recipe, design, destination):
    """Write the six visible RGBA source sheets used by one formal character."""

    if recipe.id == MVP_HERO_ID and recipe.frame_size == 48:
        raise ValueError(
            "player_male_swordsman is authored by mvp_dense_art_builder; "
            "the legacy source builder only owns 32px character sheets")

    columns = max((row.frames for row in recipe.animations), default=1)
    rows = max(len(recipe.animations), 1)
    sheet_size = (columns * recipe.frame_size, rows * recipe.frame_size)
    layers = {
        layer: Image.new("RGBA", sheet_size, (0, 0, 0, 0)) for layer in REQUIRED_LAYERS
    }
    for row_index, row in enumerate(recipe.animations or (None,)):
        frame_count = row.frames if row is not None else 1
        for frame_index in range(frame_count):
            _draw_pose(
                layers,
                design,
                row.name if row is not None else "idle",
                row.direction if row is not None else "down",
                frame_index,
                row_index * recipe.frame_size,
            )

    destination = Path(destination)
    destination.mkdir(parents=True, exist_ok=True)
    paths = {}
    for layer, image in layers.items():
        path = destination / (layer + ".png")
        image.save(path, format="PNG", optimize=False, compress_level=9)
        _write_source_meta(path, recipe.id, layer)
        paths[layer] = path
    return paths


def build_all_character_sources():
    """Materialize the design table once, then make sources and manifests agree."""

    if not DESIGN_PATH.exists():
        _write_seed_designs(DESIGN_PATH, build_roster())
    designs = load_character_designs(DESIGN_PATH)
    recipes = build_roster()
    expected_ids = {recipe.id for recipe in recipes}
    if set(designs) != expected_ids:
        raise ValueError("character design ids must exactly match the formal roster")
    if len({design.signature for design in designs.values()}) != len(designs):
        raise ValueError("character design signatures must be unique")

    _rewrite_manifest_modules()
    recipes = build_roster()
    for recipe in recipes:
        if recipe.id == MVP_HERO_ID:
            continue
        build_character_sources(recipe, designs[recipe.id], GENERATED_ROOT / recipe.id)
    from .mvp_dense_art_builder import build_dense_mvp_art
    build_dense_mvp_art(PROJECT_ROOT)
    assert_character_sources_complete(recipes)
    return len(recipes)


def build_character_source(art_id):
    """Rebuild one authored source sheet without touching the frozen roster."""
    designs = load_character_designs(DESIGN_PATH)
    recipes = {recipe.id: recipe for recipe in build_roster()}
    if art_id not in recipes or art_id not in designs:
        raise ValueError("unknown formal character '{}'".format(art_id))
    if art_id == MVP_HERO_ID:
        from .mvp_dense_art_builder import build_dense_mvp_art
        build_dense_mvp_art(PROJECT_ROOT)
        return {
            layer: GENERATED_ROOT / art_id / (layer + ".png")
            for layer in REQUIRED_LAYERS
        }
    return build_character_sources(
        recipes[art_id], designs[art_id], GENERATED_ROOT / art_id)


def _draw_pose(layers, design, action, direction, frame, row_y):
    """Draw one 32px top-down pose using only integer pixel clusters."""

    draw = {name: ImageDraw.Draw(image) for name, image in layers.items()}
    frame_x = frame * 32
    outline, primary, highlight = _design_colors(design)
    skin_outline, skin_mid, skin_highlight = _palette("skin")
    marker = _stable_number(design.accent_style)
    center = frame_x + 16 + _horizontal_motion(action, frame)
    top = row_y + _vertical_motion(action, frame)
    if "beast" in design.silhouette or "quadruped" in design.silhouette:
        _draw_beast_pose(draw, design, action, direction, frame_x, row_y, marker)
        return

    width = _silhouette_width(design.silhouette, marker)
    body_left = center - width // 2
    body_right = center + width // 2
    head_left = center - 4
    head_right = center + 4
    head_top = top + 6
    head_bottom = top + 13

    if action == "death":
        _draw_death_pose(draw, center, row_y, outline, primary, highlight, skin_mid, marker)
        return

    lean = -2 if action.startswith("attack") and direction == "left" else 2 if action.startswith("attack") and direction == "right" else 0
    body_left += lean
    body_right += lean
    head_left += lean
    head_right += lean
    
    # Body mass and legs: the silhouette controls the width and coat shape.
    torso_top = top + 13
    torso_bottom = top + 24
    _rect(draw["body"], (body_left - 1, torso_top, body_right + 1, torso_bottom), outline)
    _rect(draw["body"], (body_left, torso_top + 1, body_right, torso_bottom - 2), primary)
    hem = _hem_width(design.outfit_style, marker)
    _rect(draw["body"], (body_left - hem, torso_bottom - 2, body_right + hem, torso_bottom + 2), outline)
    _rect(draw["body"], (body_left - hem + 1, torso_bottom - 2, body_right + hem - 1, torso_bottom + 1), primary)
    stride = _stride(action, frame)
    _rect(draw["body"], (center - 4 + stride, top + 25, center - 1 + stride, top + 29), outline)
    _rect(draw["body"], (center + 1 - stride, top + 25, center + 4 - stride, top + 29), outline)
    _rect(draw["body"], (center - 4 + stride, top + 27, center - 1 + stride, top + 28), highlight)
    _rect(draw["body"], (center + 1 - stride, top + 27, center + 4 - stride, top + 28), highlight)

    # Face and hair are separate editable layers so identity survives recolors.
    _rect(draw["face"], (head_left, head_top, head_right, head_bottom), skin_outline)
    _rect(draw["face"], (head_left + 1, head_top + 1, head_right - 1, head_bottom - 1), skin_mid)
    if direction != "up":
        eye_shift = -1 if direction == "left" else 1 if direction == "right" else 0
        _rect(draw["face"], (center - 2 + eye_shift, top + 10, center - 1 + eye_shift, top + 10), outline)
        _rect(draw["face"], (center + 1 + eye_shift, top + 10, center + 2 + eye_shift, top + 10), outline)
        _rect(draw["face"], (center, top + 12, center, top + 12), skin_highlight)
    hair_height = 3 + marker % 3
    _rect(draw["hair"], (head_left - 1, head_top - 2, head_right + 1, head_top + hair_height), outline)
    _rect(draw["hair"], (head_left, head_top - 1, head_right, head_top + hair_height - 1), primary)
    _draw_hair_style(draw["hair"], design.hair_style, center, top, head_left, head_right, head_bottom, outline, primary, highlight)

    # Garments use a collar, sash, sleeves and a unique accent patch.
    _rect(draw["outfit"], (body_left - 2, torso_top + 2, body_right + 2, torso_bottom - 1), outline)
    _rect(draw["outfit"], (body_left - 1, torso_top + 3, body_right + 1, torso_bottom - 2), primary)
    _rect(draw["outfit"], (center - 1, torso_top + 1, center + 1, torso_bottom - 3), highlight)
    sleeve = _sleeve_width(design.outfit_style)
    _rect(draw["outfit"], (body_left - sleeve, torso_top + 4, body_left - 1, torso_bottom - 3), outline)
    _rect(draw["outfit"], (body_right + 1, torso_top + 4, body_right + sleeve, torso_bottom - 3), outline)
    sash_y = torso_top + 6 + marker % 3
    _rect(draw["outfit"], (body_left, sash_y, body_right, sash_y + 1), outline)
    _rect(draw["outfit"], (body_left + 1, sash_y, body_right - 1, sash_y), highlight)
    _rect(draw["outfit"], (body_left + (marker % max(1, width - 1)), torso_top + 3, body_left + (marker % max(1, width - 1)) + 1, torso_top + 4), highlight)

    # Props differ by action and direction: weapons, scrolls, gourds and talismans all keep a readable cluster.
    _draw_prop(draw["weapon"], design, action, direction, center, top, outline, primary, highlight, marker)
    _draw_accessory(draw["accessory"], design, center, top, outline, primary, highlight, marker)


def _draw_death_pose(draw, center, row_y, outline, primary, highlight, skin, marker):
    y = row_y + 22
    _rect(draw["body"], (center - 11, y - 3, center + 8, y + 3), outline)
    _rect(draw["body"], (center - 10, y - 2, center + 7, y + 2), primary)
    _rect(draw["face"], (center + 7, y - 4, center + 12, y + 1), skin)
    _rect(draw["hair"], (center + 7, y - 5, center + 12, y - 3), outline)
    _rect(draw["outfit"], (center - 8, y - 1, center + 4, y + 2), highlight)
    _rect(draw["weapon"], (center - 14, y + 3, center + 3, y + 4), outline)
    _rect(draw["accessory"], (center - 2 + marker % 4, y - 5, center + marker % 4, y - 3), highlight)


def _draw_beast_pose(draw, design, action, direction, frame_x, row_y, marker):
    outline, primary, highlight = _design_colors(design)
    center = frame_x + 16 + _horizontal_motion(action, marker)
    top = row_y + 14 + _vertical_motion(action, marker)
    _rect(draw["body"], (center - 10, top, center + 10, top + 8), outline)
    _rect(draw["body"], (center - 9, top + 1, center + 9, top + 7), primary)
    _rect(draw["body"], (center - 8, top + 7, center - 5, top + 12), outline)
    _rect(draw["body"], (center + 5, top + 7, center + 8, top + 12), outline)
    _rect(draw["face"], (center + 7, top - 2, center + 12, top + 4), outline)
    _rect(draw["face"], (center + 8, top - 1, center + 11, top + 3), highlight)
    _rect(draw["hair"], (center - 6, top - 4, center + 4, top + 1), outline)
    _rect(draw["hair"], (center - 5, top - 3, center + 3, top), primary)
    _rect(draw["outfit"], (center - 2, top + 2, center + 5, top + 7), highlight)
    _rect(draw["weapon"], (center + 10, top + 3, center + 14, top + 4), outline)
    _rect(draw["accessory"], (center - 3 + marker % 4, top + 8, center - 1 + marker % 4, top + 10), highlight)


def _silhouette_width(silhouette, marker):
    if any(token in silhouette for token in ("broad", "wide", "lamellar")):
        return 15
    if any(token in silhouette for token in ("slender", "lean", "narrow", "short_cloak")):
        return 8
    if any(token in silhouette for token in ("round", "soft", "travel")):
        return 12
    if "tall" in silhouette:
        return 11
    return 10 + marker % 3


def _hem_width(outfit_style, marker):
    if any(token in outfit_style for token in ("robe", "cloak", "vestment")):
        return 4
    if any(token in outfit_style for token in ("lamellar", "uniform", "wrap")):
        return 2
    return 2 + marker % 2


def _sleeve_width(outfit_style):
    if any(token in outfit_style for token in ("wide", "robe", "vestment")):
        return 4
    if any(token in outfit_style for token in ("lamellar", "sleeveless", "wrap")):
        return 2
    return 3


def _draw_hair_style(draw, style, center, top, head_left, head_right, head_bottom, outline, primary, highlight):
    if any(token in style for token in ("ponytail", "tail")):
        _rect(draw, (head_right, top + 8, head_right + 4, head_bottom), outline)
        _rect(draw, (head_right + 1, top + 9, head_right + 3, head_bottom - 1), primary)
    elif any(token in style for token in ("braid", "double_bun")):
        _rect(draw, (head_left - 3, top + 7, head_left - 1, top + 13), outline)
        _rect(draw, (head_right + 1, top + 7, head_right + 3, top + 13), outline)
    elif any(token in style for token in ("hood", "helmet", "cap")):
        _rect(draw, (head_left - 2, top + 4, head_right + 2, top + 9), outline)
        _rect(draw, (head_left - 1, top + 5, head_right + 1, top + 8), primary)
    elif any(token in style for token in ("crown", "ornament", "topknot", "bun", "knot")):
        _rect(draw, (center - 2, top + 2, center + 2, top + 6), outline)
        _rect(draw, (center - 1, top + 1, center + 1, top + 5), highlight)
    else:
        _rect(draw, (head_left - 2, top + 8, head_left, head_bottom - 2), outline)
    _rect(draw, (center - 1, top + 3, center + 1, top + 5), highlight)


def _draw_prop(draw, design, action, direction, center, top, outline, primary, highlight, marker):
    long_prop = any(token in design.prop_style for token in ("sword", "spear", "staff", "blade"))
    attack = action.startswith("attack") or action.startswith("skill")
    if long_prop:
        if direction == "left":
            end_x, end_y = center - (14 if attack else 9), top + (8 if attack else 19)
        elif direction == "right":
            end_x, end_y = center + (14 if attack else 9), top + (8 if attack else 19)
        elif direction == "up":
            end_x, end_y = center + (5 if attack else 3), top + (2 if attack else 10)
        else:
            end_x, end_y = center + (5 if attack else 3), top + (30 if attack else 8)
        start_x, start_y = center + (marker % 3 - 1), top + 19
        _stepped_line(draw, start_x, start_y, end_x, end_y, outline)
        _stepped_line(draw, start_x + (1 if end_x >= start_x else -1), start_y, end_x, end_y, highlight)
    elif "wrapped_fists" in design.prop_style:
        _rect(draw, (center - 9, top + 18, center - 5, top + 21), outline)
        _rect(draw, (center + 5, top + 18, center + 9, top + 21), outline)
        _rect(draw, (center - 8, top + 18, center - 6, top + 20), highlight)
        _rect(draw, (center + 6, top + 18, center + 8, top + 20), highlight)
    elif "gourd" in design.prop_style:
        _rect(draw, (center + 6, top + 18, center + 10, top + 24), outline)
        _rect(draw, (center + 7, top + 19, center + 9, top + 23), highlight)
    elif "scroll" in design.prop_style:
        _rect(draw, (center + 5, top + 17, center + 9, top + 23), outline)
        _rect(draw, (center + 6, top + 18, center + 8, top + 22), highlight)
    elif "talisman" in design.prop_style:
        _rect(draw, (center + 6, top + 14, center + 8, top + 20), outline)
        _rect(draw, (center + 7, top + 15, center + 7, top + 19), highlight)
    else:
        _rect(draw, (center + 5, top + 19, center + 9, top + 22), outline)
        _rect(draw, (center + 6, top + 19, center + 8, top + 21), primary)


def _draw_accessory(draw, design, center, top, outline, primary, highlight, marker):
    slot = marker % 5
    _rect(draw, (center - 5 + slot, top + 20, center - 3 + slot, top + 23), outline)
    _rect(draw, (center - 4 + slot, top + 21, center - 3 + slot, top + 22), highlight)
    if "banner" in design.accent_style or "ribbon" in design.accent_style:
        _rect(draw, (center - 7, top + 14, center - 5, top + 20), primary)
    elif "jade" in design.accent_style or marker % 3 == 0:
        _rect(draw, (center + 3, top + 15, center + 4, top + 16), highlight)


def _stepped_line(draw, x0, y0, x1, y1, color):
    steps = max(abs(x1 - x0), abs(y1 - y0), 1)
    for step in range(steps + 1):
        x = round(x0 + (x1 - x0) * step / steps)
        y = round(y0 + (y1 - y0) * step / steps)
        draw.point((x, y), fill=color)


def _rect(draw, bounds, color):
    draw.rectangle(bounds, fill=color)


def _design_colors(design):
    base_outline, base_primary, base_highlight = _palette(design.palette[0])
    _, secondary, accent = _palette(design.palette[1])
    _, _, sparkle = _palette(design.palette[2])
    marker = _stable_number(design.outfit_style)
    return (
        base_outline,
        secondary if marker % 2 else base_primary,
        sparkle if marker % 3 else accent,
    )


def _palette(name):
    return PALETTES[name]


def _stable_number(value):
    return int(hashlib.sha256(value.encode("utf-8")).hexdigest()[:8], 16)


def _horizontal_motion(action, frame):
    if action == "walk":
        return (-1, 0, 1, 0, -1, 1)[frame % 6]
    if action == "dash":
        return (0, 2, 3, 4)[frame % 4]
    if action.startswith("attack"):
        return (-1, 0, 1, 2, 1, 0)[frame % 6]
    return 0


def _vertical_motion(action, frame):
    if action == "walk":
        return (0, -1, 0, 1, 0, -1)[frame % 6]
    if action.startswith("skill"):
        return (-1, -2, -1, 0, 1, 0)[frame % 6]
    if action == "hurt":
        return (1, 0, 1, 0)[frame % 4]
    return 0


def _stride(action, frame):
    if action == "walk":
        return (-1, 0, 1, 0, -1, 1)[frame % 6]
    if action == "dash":
        return -2 + frame % 3
    return 0


def _write_source_meta(path, art_id, layer):
    guid = hashlib.sha256(("yuanhailu-source:" + art_id + ":" + layer).encode("utf-8")).hexdigest()[:32]
    meta_path = path.with_suffix(path.suffix + ".meta")
    encoded = (
        "fileFormatVersion: 2\n"
        "guid: {}\n"
        "TextureImporter:\n"
        "  externalObjects: {{}}\n"
        "  serializedVersion: 13\n"
        "  mipmaps:\n"
        "    mipMapMode: 0\n"
        "    enableMipMap: 0\n"
        "  textureSettings:\n"
        "    serializedVersion: 2\n"
        "    filterMode: 0\n"
        "    aniso: 0\n"
        "  textureType: 0\n"
        "  spriteMode: 0\n"
        "  spritePixelsToUnits: 16\n"
        "  alphaIsTransparency: 1\n"
        "  platformSettings: []\n"
    ).format(guid)
    if not meta_path.exists() or meta_path.read_text(encoding="utf-8") != encoded:
        meta_path.write_text(encoded, encoding="utf-8")


def _rewrite_manifest_modules():
    for name in MANIFEST_NAMES:
        path = MANIFEST_ROOT / name
        payload = json.loads(path.read_text(encoding="utf-8"))
        for character in payload["characters"]:
            art_id = character["id"]
            character["modules"] = [
                "Assets/ArtSource/Characters/Generated/{}/{}.png".format(art_id, layer)
                for layer in REQUIRED_LAYERS
            ]
        path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def _write_seed_designs(path, recipes):
    designs = {}
    for index, recipe in enumerate(recipes):
        silhouette, palette, hair, outfit, prop = _design_fields(recipe.id, index)
        designs[recipe.id] = {
            "silhouette": silhouette,
            "palette": list(palette),
            "hairStyle": hair,
            "outfitStyle": outfit,
            "propStyle": prop,
            "accentStyle": "{}-signature-{:02d}".format(_accent_for(recipe.id), index + 1),
        }
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps({"schemaVersion": 1, "designs": designs}, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


def _design_fields(art_id, index):
    player = {
        "player_male_swordsman": ("tall_split_hem", ("ink_blue", "paper_white", "river_blue"), "tied_topknot", "river_guard_robes", "long_sword"),
        "player_male_boxer": ("broad_wrapped", ("vermilion", "warm_brown", "gold"), "short_spike", "sleeveless_wraps", "wrapped_fists"),
        "player_male_hidden_weapon": ("lean_short_coat", ("deep_purple", "ash_gray", "vermilion"), "hood_tied", "belt_pouch_coat", "throwing_blade"),
        "player_male_healer": ("round_travel_robe", ("bamboo_green", "paper_white", "jade_green"), "loose_bun", "herbal_apron", "medicine_gourd"),
        "player_male_scholar": ("narrow_long_robe", ("ink_blue", "warm_brown", "paper_white"), "scholar_cap", "layered_study_robe", "bamboo_scroll"),
        "player_male_mystic": ("wide_sleeve_robe", ("deep_purple", "gold", "paper_white"), "crown_knot", "talisman_robe", "flying_talisman"),
        "player_female_swordsman": ("slender_long_coat", ("ink_blue", "paper_white", "river_blue"), "high_ponytail", "split_hem_robe", "long_sword"),
        "player_female_boxer": ("athletic_wrap_coat", ("vermilion", "warm_brown", "gold"), "braided_tail", "sleeveless_wraps", "wrapped_fists"),
        "player_female_hidden_weapon": ("slender_cloak", ("deep_purple", "ash_gray", "vermilion"), "side_bun", "belt_pouch_coat", "throwing_blade"),
        "player_female_healer": ("soft_apron_robe", ("bamboo_green", "paper_white", "jade_green"), "double_bun", "herbal_apron", "medicine_gourd"),
        "player_female_scholar": ("ribbon_long_robe", ("ink_blue", "warm_brown", "paper_white"), "ribbon_knot", "layered_study_robe", "bamboo_scroll"),
        "player_female_mystic": ("wide_sleeve_robe", ("deep_purple", "gold", "paper_white"), "ornament_bun", "talisman_robe", "flying_talisman"),
    }
    if art_id in player:
        return player[art_id]
    if art_id in REGION_PALETTES:
        return "regal_heir", REGION_PALETTES[art_id], "crown_knot", "court_robe", "long_sword"

    region = next((name for name in REGION_PALETTES if art_id.startswith(name + "_")), None)
    palette = REGION_PALETTES.get(region, ("ink_blue", "warm_brown", "gold"))
    role = art_id.split("_")[-2] if art_id.endswith("_01") else art_id
    silhouettes = ("slender_robed", "broad_lamellar", "short_cloak", "tall_cape", "round_travel_coat", "beast_quadruped")
    hairs = ("topknot", "hood", "braids", "shaved_crown", "fur_mane", "helmet")
    outfits = ("travel_robe", "lamellar_coat", "patched_cloak", "priest_vestment", "fur_wrap", "guard_uniform")
    props = ("long_sword", "short_blade", "bamboo_scroll", "medicine_gourd", "flying_talisman", "banner_staff")
    return (
        silhouettes[index % len(silhouettes)] + "_" + role,
        palette,
        hairs[(index // 2) % len(hairs)] + "_" + role,
        outfits[(index // 3) % len(outfits)] + "_" + role,
        props[(index // 5) % len(props)] + "_" + role,
    )


def _accent_for(art_id):
    if "swordsman" in art_id or "sword" in art_id:
        return "sword_ribbon"
    if "healer" in art_id or "pharmacy" in art_id:
        return "jade_gourd"
    if "wolf" in art_id or "beast" in art_id:
        return "fang_mark"
    if "guard" in art_id or "soldier" in art_id:
        return "rank_banner"
    return "jade_seal"


def _parse_args():
    parser = argparse.ArgumentParser(description=__doc__)
    selection = parser.add_mutually_exclusive_group(required=True)
    selection.add_argument("--all", action="store_true", help="build every formal character source")
    selection.add_argument("--id", help="build one formal character source")
    return parser.parse_args()


def main():
    args = _parse_args()
    if args.all:
        count = build_all_character_sources()
        print("built={} character sources".format(count))
        return
    build_character_source(args.id)
    print("built=1 character source ({})".format(args.id))


if __name__ == "__main__":
    main()
