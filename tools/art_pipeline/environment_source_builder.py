"""Build distinctive, editable pixel source modules for every formal environment."""

import argparse
import hashlib
import json
import re
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw

from .environment_roster import (
    INTERIOR_IDS,
    MANIFEST_ROOT,
    REGION_IDS,
    build_interior_recipes,
    build_region_recipes,
)


PROJECT_ROOT = Path(__file__).resolve().parents[2]
DESIGN_PATH = (
    PROJECT_ROOT / "Assets" / "ArtSource" / "Environment" / "Designs" / "environment-designs.json"
)
PROLOGUE_NORMAL_SOURCE_DIRECTORY = "Assets/ArtSource/Environment/Regions/prologue_village/formal"
PROLOGUE_BURNED_SOURCE_DIRECTORY = "Assets/ArtSource/Environment/Regions/prologue_village/burned/formal"
PROLOGUE_STATE_VARIANTS = {
    "normal": {
        "tilesetSuffix": "normal",
        "weather": "clear",
        "sourceDirectory": PROLOGUE_NORMAL_SOURCE_DIRECTORY,
    },
    "burned": {
        "tilesetSuffix": "burned",
        "weather": "ember_wind",
        "sourceDirectory": PROLOGUE_BURNED_SOURCE_DIRECTORY,
    },
}


REGION_PROFILES = {
    "tianshu": ("imperial_market", "clear", ((34, 42, 45), (70, 83, 77), (125, 107, 72), (212, 160, 70), (238, 219, 170))),
    "cangyue": ("cliff_stair_temple", "mist", ((45, 58, 64), (90, 111, 109), (135, 148, 137), (186, 177, 143), (222, 224, 210))),
    "yanliu": ("branching_canal_town", "drizzle", ((25, 62, 67), (49, 101, 103), (86, 137, 126), (197, 196, 158), (232, 224, 185))),
    "chisha": ("dune_frontier", "sandstorm", ((77, 52, 34), (128, 83, 47), (181, 124, 61), (219, 169, 89), (242, 216, 149))),
    "youhuang": ("bamboo_maze", "toxic_mist", ((27, 55, 43), (53, 91, 59), (91, 128, 74), (126, 154, 88), (180, 196, 128))),
    "hanyuan": ("snow_ridge_tomb", "snowfall", ((47, 65, 78), (88, 113, 128), (144, 166, 174), (205, 216, 209), (238, 238, 225))),
    "prologue_village": ("ring_road_grain_yard", "clear", ((68, 62, 43), (110, 99, 57), (153, 128, 70), (192, 158, 85), (225, 203, 139))),
    "luoyuan": ("river_ruin_market", "overcast", ((44, 64, 62), (70, 99, 91), (111, 125, 101), (151, 141, 99), (199, 187, 138))),
    "jueyun": ("sect_cliff_walk", "cloudy", ((50, 64, 68), (78, 94, 91), (110, 128, 115), (152, 151, 121), (212, 207, 172))),
    "zhenyue": ("stele_altar_axis", "mountain_wind", ((57, 59, 48), (86, 91, 68), (119, 122, 83), (163, 151, 94), (214, 198, 137))),
}

INTERIOR_PROFILES = {
    "inn": ("inn_counter_tables_kitchen", "warm_indoor", ((65, 42, 27), (104, 68, 37), (151, 101, 53), (210, 162, 83), (245, 221, 161))),
    "residence": ("residence_bed_screen", "warm_indoor", ((72, 51, 38), (112, 77, 55), (152, 110, 76), (196, 151, 103), (237, 215, 172))),
    "shop": ("shop_shelves_counter", "warm_indoor", ((57, 46, 35), (94, 71, 45), (141, 101, 54), (207, 163, 79), (239, 219, 166))),
    "pharmacy": ("pharmacy_cabinets_diagnosis", "herbal_air", ((42, 62, 47), (69, 100, 67), (109, 139, 79), (176, 182, 111), (229, 224, 174))),
    "academy": ("academy_desks_scroll_racks", "quiet", ((49, 58, 62), (74, 89, 91), (115, 125, 117), (168, 158, 116), (225, 216, 177))),
    "yamen": ("yamen_bench_cell_desk", "stern", ((54, 52, 48), (84, 77, 64), (119, 100, 69), (157, 123, 72), (220, 199, 149))),
    "palace": ("palace_screen_throne", "ceremonial", ((55, 37, 37), (94, 51, 42), (142, 75, 48), (205, 149, 68), (244, 220, 151))),
    "temple": ("temple_altar_incense", "incense", ((48, 45, 38), (77, 67, 52), (116, 94, 62), (173, 137, 71), (232, 210, 151))),
    "cave": ("cave_crystal_pool", "damp", ((38, 51, 55), (59, 79, 82), (87, 108, 104), (111, 143, 135), (176, 199, 181))),
    "tomb": ("tomb_sarcophagus_steles", "cold", ((46, 51, 58), (70, 79, 85), (101, 111, 111), (139, 143, 129), (204, 198, 171))),
    "dungeon": ("dungeon_cells_chains", "torch", ((41, 42, 45), (66, 63, 57), (100, 84, 60), (157, 109, 60), (237, 174, 83))),
    "military_camp": ("camp_bunks_weapon_rack", "command", ((47, 55, 47), (74, 86, 65), (107, 115, 74), (158, 142, 76), (219, 198, 134))),
    "ship_cabin": ("ship_cabin_hatch_cargo", "sea_air", ((40, 55, 62), (63, 83, 88), (95, 120, 117), (145, 136, 94), (220, 202, 151))),
}


@dataclass(frozen=True)
class EnvironmentDesign:
    id: str
    kind: str
    palette: tuple
    geometry_key: str
    weather_id: str
    tile_roles: tuple
    landmarks: tuple
    blocking_tile_roles: tuple

    @classmethod
    def from_payload(cls, payload):
        required = ("id", "kind", "palette", "geometryKey", "weather", "tileRoles", "landmarks", "blockingTileRoles")
        missing = [field for field in required if field not in payload]
        if missing:
            raise ValueError("environment design missing {}".format(", ".join(missing)))
        palette = tuple(tuple(color) for color in payload["palette"])
        if payload["kind"] not in ("region", "interior") or len(palette) != 5:
            raise ValueError("{} has invalid kind or palette".format(payload["id"]))
        if any(len(color) != 3 or any(not isinstance(channel, int) or not 0 <= channel <= 255 for channel in color) for color in palette):
            raise ValueError("{} palette must contain five RGB colors".format(payload["id"]))
        fields = (payload["geometryKey"], payload["weather"])
        if not all(isinstance(value, str) and value for value in fields):
            raise ValueError("{} requires geometry and weather IDs".format(payload["id"]))
        return cls(
            payload["id"], payload["kind"], palette, payload["geometryKey"], payload["weather"],
            tuple(payload["tileRoles"]), tuple(payload["landmarks"]), tuple(payload["blockingTileRoles"]),
        )


def load_environment_designs(path=DESIGN_PATH):
    payload = json.loads(Path(path).read_text(encoding="utf-8"))
    if payload.get("schemaVersion") != 1 or not isinstance(payload.get("environments"), list):
        raise ValueError("environment designs require schemaVersion 1 and an environments array")
    designs = {item["id"]: EnvironmentDesign.from_payload(item) for item in payload["environments"]}
    if len(designs) != len(payload["environments"]):
        raise ValueError("environment designs contain duplicate IDs")
    return designs


def _seed_design_records():
    records = []
    for art_id in REGION_IDS:
        geometry, weather, palette = REGION_PROFILES[art_id]
        recipe = next(recipe for recipe in build_region_recipes() if recipe.id == art_id)
        records.append({
            "id": art_id, "kind": "region", "palette": [list(color) for color in palette],
            "geometryKey": geometry, "weather": weather,
            "tileRoles": sorted({_role(path) for path in recipe.modules}),
            "landmarks": [landmark.id for landmark in recipe.landmarks],
            "blockingTileRoles": ["wall", "roof"],
            **({"stateVariants": PROLOGUE_STATE_VARIANTS} if art_id == "prologue_village" else {}),
        })
    for art_id in INTERIOR_IDS:
        geometry, weather, palette = INTERIOR_PROFILES[art_id]
        recipe = next(recipe for recipe in build_interior_recipes() if recipe.id == art_id)
        records.append({
            "id": art_id, "kind": "interior", "palette": [list(color) for color in palette],
            "geometryKey": geometry, "weather": weather,
            "tileRoles": sorted({_role(path) for path in recipe.modules}),
            "landmarks": [landmark.id for landmark in recipe.landmarks],
            "blockingTileRoles": ["wall"],
        })
    return {"schemaVersion": 1, "environments": records}


def ensure_design_records(path=DESIGN_PATH):
    path = Path(path)
    if not path.exists():
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(_seed_design_records(), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    else:
        payload = json.loads(path.read_text(encoding="utf-8"))
        prologue = next(item for item in payload["environments"] if item["id"] == "prologue_village")
        if prologue.get("stateVariants") != PROLOGUE_STATE_VARIANTS:
            prologue["stateVariants"] = PROLOGUE_STATE_VARIANTS
            path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return load_environment_designs(path)


def _role(path):
    return re.sub(r"_[0-9]+$", "", Path(path).stem)


def _variant(path):
    match = re.search(r"_(\d+)$", Path(path).stem)
    return int(match.group(1)) if match else 0


def _dark(color):
    return tuple(max(0, channel - 34) for channel in color)


def _light(color):
    return tuple(min(255, channel + 34) for channel in color)


def _accent(design):
    return design.palette[(sum(ord(char) for char in design.geometry_key) % 3) + 2]


def _draw_tile(design, role, variant):
    ink, base, mid, bright, pale = design.palette
    image = Image.new("RGBA", (16, 16), base + (255,))
    draw = ImageDraw.Draw(image)
    dark = _dark(ink)
    if role in ("ground", "floor"):
        draw.rectangle((0, 0, 15, 15), fill=base + (255,))
        for index in range(6):
            x = (index * 5 + variant * 3) % 15
            y = (index * 7 + variant * 2) % 15
            draw.rectangle((x, y, min(15, x + 1 + index % 2), y), fill=mid + (255,))
        draw.point(((variant * 3 + 2) % 16, (variant * 5 + 4) % 16), fill=dark + (255,))
    elif role == "road":
        draw.rectangle((0, 0, 15, 15), fill=mid + (255,))
        for y in range(2, 16, 5):
            draw.line((0, y, 15, y - (variant % 2)), fill=_dark(base) + (255,))
        for x in range(3 + variant % 3, 16, 6):
            draw.line((x, 0, x - 1, 15), fill=_light(mid) + (255,))
    elif role == "water":
        draw.rectangle((0, 0, 15, 15), fill=ink + (255,))
        for y in (3, 8, 13):
            offset = (variant * 3 + y) % 5
            draw.line((offset, y, min(15, offset + 6), y), fill=mid + (255,))
            draw.point((min(15, offset + 7), y - 1), fill=bright + (255,))
    elif role == "shore":
        draw.rectangle((0, 0, 15, 15), fill=base + (255,))
        draw.line((0, 3 + variant % 3, 15, 3 + variant % 3), fill=bright + (255,), width=2)
        draw.line((0, 6 + variant % 3, 15, 6 + variant % 3), fill=ink + (255,))
    elif role == "wall":
        draw.rectangle((0, 0, 15, 15), fill=mid + (255,))
        for y in range(1, 16, 5):
            draw.line((0, y, 15, y), fill=dark + (255,))
            for x in range((y + variant) % 4, 16, 8):
                draw.line((x, max(0, y - 4), x, y - 1), fill=_dark(base) + (255,))
        draw.line((0, 0, 15, 0), fill=pale + (255,))
    elif role == "roof":
        draw.rectangle((0, 0, 15, 15), fill=ink + (255,))
        for y in range(1, 15, 4):
            draw.line((0, y, 15, y), fill=mid + (255,), width=2)
            for x in range((variant + y) % 5, 16, 5):
                draw.point((x, y - 1), fill=bright + (255,))
        draw.line((0, 15, 15, 15), fill=pale + (255,))
    elif role == "door":
        draw.rectangle((3, 1, 12, 15), fill=dark + (255,))
        draw.rectangle((5, 3, 10, 15), fill=mid + (255,))
        draw.point((9, 9), fill=bright + (255,))
    elif role == "window":
        draw.rectangle((2, 2, 13, 13), fill=dark + (255,))
        draw.rectangle((4, 4, 11, 11), fill=pale + (255,))
        draw.line((7, 4, 7, 11), fill=mid + (255,))
        draw.line((4, 7, 11, 7), fill=mid + (255,))
    elif role == "bridge":
        draw.rectangle((0, 5, 15, 13), fill=mid + (255,))
        draw.line((0, 4, 15, 4), fill=pale + (255,))
        for x in range(2 + variant % 2, 16, 4):
            draw.line((x, 6, x, 12), fill=dark + (255,))
        draw.line((0, 14, 15, 14), fill=dark + (255,))
    elif role == "light":
        image = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
        draw = ImageDraw.Draw(image)
        draw.rectangle((6, 6, 9, 12), fill=dark + (255,))
        draw.rectangle((5, 4, 10, 9), fill=bright + (255,))
        draw.rectangle((6, 5, 9, 8), fill=pale + (255,))
    elif role in ("entry", "exit"):
        draw.rectangle((0, 0, 15, 15), fill=base + (255,))
        draw.rectangle((3, 4, 12, 15), fill=dark + (255,))
        draw.rectangle((5, 5, 10, 15), fill=mid + (255,))
        draw.line((3, 3, 8, 0, 13, 3), fill=bright + (255,), width=2)
        if role == "exit":
            draw.line((7, 7, 7, 13), fill=pale + (255,))
    else:  # decor and prop carry the scene-specific silhouettes.
        image = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
        draw = ImageDraw.Draw(image)
        style = (variant + sum(ord(char) for char in design.geometry_key)) % 5
        if style == 0:
            draw.rectangle((6, 7, 9, 15), fill=dark + (255,))
            draw.polygon(((2, 8), (8, 0), (14, 8)), fill=mid + (255,))
            draw.rectangle((5, 5, 10, 9), fill=bright + (255,))
        elif style == 1:
            draw.rectangle((3, 7, 12, 14), fill=mid + (255,))
            draw.rectangle((5, 3, 10, 8), fill=bright + (255,))
            draw.line((3, 14, 12, 14), fill=dark + (255,))
        elif style == 2:
            draw.rectangle((6, 5, 9, 15), fill=dark + (255,))
            draw.polygon(((7, 1), (13, 7), (9, 10), (3, 6)), fill=mid + (255,))
            draw.point((8, 4), fill=pale + (255,))
        elif style == 3:
            draw.rectangle((4, 9, 11, 15), fill=mid + (255,))
            draw.rectangle((5, 5, 10, 10), fill=bright + (255,))
            draw.line((5, 5, 10, 10), fill=pale + (255,))
        else:
            draw.rectangle((2, 10, 13, 15), fill=dark + (255,))
            draw.rectangle((4, 5, 11, 12), fill=mid + (255,))
            draw.rectangle((6, 2, 9, 7), fill=_accent(design) + (255,))
    return image


def _draw_landmark(design, landmark_id, size):
    width, height = size
    ink, base, mid, bright, pale = design.palette
    image = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    dark = _dark(ink)
    seed = sum(ord(char) for char in landmark_id)
    fingerprint = int.from_bytes(hashlib.sha256(landmark_id.encode("utf-8")).digest()[:4], "big")
    left = 4 + fingerprint % 6
    right = width - 5 - (fingerprint >> 3) % 6
    bottom = height - 4
    if "bridge" in landmark_id:
        arch_top = bottom - 28 - fingerprint % 10
        draw.rectangle((left, bottom - 12, right, bottom - 6), fill=mid + (255,))
        draw.arc((left + 7, arch_top, right - 7, bottom - 2), 180, 360, fill=pale + (255,), width=4)
        draw.arc((left + 11, arch_top + 6, right - 11, bottom + 4), 180, 360, fill=dark + (255,), width=3)
    elif "gate" in landmark_id:
        pillar_width = 8 + fingerprint % 5
        draw.rectangle((left, bottom - 36, left + pillar_width, bottom), fill=mid + (255,))
        draw.rectangle((right - pillar_width, bottom - 36, right, bottom), fill=mid + (255,))
        draw.rectangle((left + pillar_width, bottom - 22, right - pillar_width, bottom - 12), fill=dark + (255,))
        draw.polygon(((left - 4, bottom - 36), (width // 2, bottom - 57 + fingerprint % 7), (right + 4, bottom - 36)), fill=ink + (255,))
        draw.rectangle((width // 2 - 3, bottom - 34, width // 2 + 3, bottom - 24), fill=bright + (255,))
    elif any(word in landmark_id for word in ("tower", "stele", "tree", "beacon")):
        center = width // 2
        tower_height = 34 + fingerprint % 17
        tower_width = 6 + (fingerprint >> 4) % 6
        draw.rectangle((center - tower_width, bottom - tower_height, center + tower_width, bottom), fill=mid + (255,))
        draw.rectangle((center - tower_width + 3, bottom - tower_height + 5, center + tower_width - 3, bottom - 3), fill=base + (255,))
        draw.polygon(((center - tower_width - 7, bottom - tower_height + 3), (center, bottom - tower_height - 13), (center + tower_width + 7, bottom - tower_height + 3)), fill=dark + (255,))
        draw.rectangle((center - 3, bottom - tower_height - 11, center + 3, bottom - tower_height - 2), fill=bright + (255,))
    elif any(word in landmark_id for word in ("market", "caravan", "camp")):
        post = 8 + fingerprint % 8
        draw.rectangle((left + 4, bottom - 28, left + 8, bottom), fill=dark + (255,))
        draw.rectangle((right - 8, bottom - 25, right - 4, bottom), fill=dark + (255,))
        draw.polygon(((left, bottom - 26), (width // 2 - post, bottom - 47), (width // 2 + post, bottom - 26)), fill=bright + (255,))
        draw.polygon(((width // 2 - post + 2, bottom - 26), (right, bottom - 43 + fingerprint % 7), (right, bottom - 24)), fill=mid + (255,))
        draw.rectangle((left + 10, bottom - 17, right - 10, bottom - 6), fill=base + (255,))
    elif any(word in landmark_id for word in ("blacksmith", "pharmacy", "lab")):
        draw.rectangle((left, bottom - 25, right, bottom), fill=mid + (255,))
        draw.rectangle((left + 8, bottom - 19, right - 8, bottom - 3), fill=pale + (255,))
        draw.polygon(((left - 2, bottom - 25), (width // 2, bottom - 50 + fingerprint % 8), (right + 2, bottom - 25)), fill=dark + (255,))
        draw.rectangle((width // 2 - 5, bottom - 16, width // 2 + 5, bottom - 6), fill=_accent(design) + (255,))
        draw.line((right - 8, bottom - 33, right - 8, bottom - 18), fill=pale + (255,), width=2)
    elif any(word in landmark_id for word in ("cave", "tomb", "shrine", "platform")):
        draw.rectangle((left + 5, bottom - 18, right - 5, bottom), fill=dark + (255,))
        draw.rectangle((left + 9, bottom - 28, right - 9, bottom - 17), fill=mid + (255,))
        draw.rectangle((width // 2 - 12, bottom - 41 - fingerprint % 7, width // 2 + 12, bottom - 27), fill=base + (255,))
        draw.polygon(((width // 2 - 16, bottom - 40), (width // 2, bottom - 55 - fingerprint % 8), (width // 2 + 16, bottom - 40)), fill=bright + (255,))
    else:
        roof_height = 40 + fingerprint % 16
        draw.rectangle((left, bottom - 27, right, bottom), fill=mid + (255,))
        draw.rectangle((left + 5, bottom - 23, right - 5, bottom - 2), fill=pale + (255,))
        draw.polygon(((left - 3, bottom - 27), (width // 2, bottom - roof_height), (right + 3, bottom - 27)), fill=dark + (255,))
        draw.polygon(((left + 2, bottom - 29), (width // 2, bottom - roof_height + 5), (right - 2, bottom - 29)), fill=ink + (255,))
        draw.rectangle((width // 2 - 4, bottom - 19, width // 2 + 4, bottom), fill=base + (255,))
    for index in range(3):
        x = 8 + ((seed + index * 17) % max(1, width - 16))
        x = max(left, min(right - 3, x))
        draw.rectangle((x, bottom - 7, x + 3, bottom - 4), fill=_accent(design) + (255,))
    # A few tiny profile-specific silhouettes (signs, rubble, reeds) make the
    # model readable at landmark scale and prevent palette-only duplication.
    for bit in range(16):
        if fingerprint & (1 << bit):
            x = 1 + (bit * 5 + (fingerprint >> 16)) % (width - 2)
            y = bottom - 1 - (bit % 4) * 3
            draw.point((x, y), fill=bright + (255,))
    return image


def _burned_design(normal_design):
    return EnvironmentDesign(
        normal_design.id,
        normal_design.kind,
        ((27, 25, 24), (55, 48, 43), (93, 75, 62), (153, 66, 36), (226, 156, 74)),
        "burned_" + normal_design.geometry_key,
        "ember_wind",
        normal_design.tile_roles,
        normal_design.landmarks,
        normal_design.blocking_tile_roles,
    )


def _draw_burned_tile(design, role, variant):
    image = _draw_tile(_burned_design(design), role, variant)
    draw = ImageDraw.Draw(image)
    ash = (23, 21, 20, 255)
    ember = (226, 112, 45, 255)
    for index in range(3):
        x = (variant * 5 + index * 6) % 16
        y = 3 + (variant * 3 + index * 4) % 12
        draw.rectangle((x, y, min(15, x + 2), y), fill=ash)
    if role in ("roof", "wall", "decor", "prop"):
        draw.point(((variant * 7 + 2) % 16, (variant * 5 + 5) % 16), fill=ember)
    return image


def _draw_burned_landmark(design, landmark_id, size):
    image = _draw_landmark(_burned_design(design), landmark_id, size)
    draw = ImageDraw.Draw(image)
    width, height = size
    for index in range(7):
        x = 6 + (index * 13 + len(landmark_id) * 3) % (width - 12)
        y = height - 12 - (index % 3) * 8
        draw.rectangle((x, y, min(width - 1, x + 3), min(height - 1, y + 2)), fill=(0, 0, 0, 0))
    draw.rectangle((width // 2 - 2, height - 12, width // 2 + 2, height - 7), fill=(226, 112, 45, 255))
    return image


def _write_png(path, image):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, format="PNG", optimize=False, compress_level=9)


def _existing_size(path, fallback):
    if not Path(path).exists():
        return fallback
    with Image.open(path) as source:
        return source.size


def _rewrite_manifest_blocking_roles(designs):
    for filename in ("regions.json", "interiors.json"):
        path = MANIFEST_ROOT / filename
        payload = json.loads(path.read_text(encoding="utf-8"))
        for environment in payload["environments"]:
            environment["blockingTileRoles"] = list(designs[environment["id"]].blocking_tile_roles)
            if environment["id"] == "prologue_village":
                environment["stateVariants"] = PROLOGUE_STATE_VARIANTS
            else:
                environment.pop("stateVariants", None)
        path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def build_environment_sources(designs):
    """Rewrite every manifest-declared 16px module and landmark source image."""
    _rewrite_manifest_blocking_roles(designs)
    recipes = (*build_region_recipes(), *build_interior_recipes())
    expected_ids = {recipe.id for recipe in recipes}
    if set(designs) != expected_ids:
        raise ValueError("environment design IDs must exactly match the formal roster")
    for recipe in recipes:
        design = designs[recipe.id]
        if design.kind != ("region" if recipe.id in REGION_IDS else "interior"):
            raise ValueError("{} has incompatible environment kind".format(recipe.id))
        for module_path in recipe.modules:
            _write_png(PROJECT_ROOT / module_path, _draw_tile(design, _role(module_path), _variant(module_path)))
        for landmark in recipe.landmarks:
            module_path = PROJECT_ROOT / landmark.module
            size = _existing_size(
                module_path,
                (96, 72) if design.kind == "region" else (80, 56),
            )
            _write_png(module_path, _draw_landmark(design, landmark.id, size))
        if recipe.id == "prologue_village":
            for module_path in recipe.modules:
                burned_path = PROJECT_ROOT / PROLOGUE_BURNED_SOURCE_DIRECTORY / Path(module_path).name
                _write_png(burned_path, _draw_burned_tile(design, _role(module_path), _variant(module_path)))
            for landmark in recipe.landmarks:
                normal_path = PROJECT_ROOT / landmark.module
                burned_path = PROJECT_ROOT / PROLOGUE_BURNED_SOURCE_DIRECTORY / Path(landmark.module).name
                _write_png(
                    burned_path,
                    _draw_burned_landmark(
                        design,
                        landmark.id,
                        _existing_size(normal_path, (96, 72)),
                    ),
                )
    return len(recipes)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--all", action="store_true", help="build all formal environment source modules")
    args = parser.parse_args()
    if not args.all:
        raise SystemExit("pass --all to build formal environment sources")
    designs = ensure_design_records()
    print("built={} environment sources".format(build_environment_sources(designs)))


if __name__ == "__main__":
    main()
