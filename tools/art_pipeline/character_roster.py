"""Exact formal-character roster and manifest loading helpers."""

from pathlib import Path

from .schema import load_character_manifest


PROJECT_ROOT = Path(__file__).resolve().parents[2]
MANIFEST_ROOT = PROJECT_ROOT / "Assets" / "ArtSource" / "Characters" / "Manifests"

PLAYER_CLASSES = (
    "swordsman",
    "boxer",
    "hidden_weapon",
    "healer",
    "scholar",
    "mystic",
)
CORE_REGIONS = ("tianshu", "cangyue", "yanliu", "chisha", "youhuang", "hanyuan")
NPC_ROLES = ("official", "merchant", "civilian", "soldier", "religious", "faction")

NAMED_IDS = (
    "shen_ruolan",
    "zhao_wuhen",
    "su_qinghe",
    "xiao_wenyuan",
    "xuan_qingzi",
    "xiao_cangming",
    "du_qiusheng",
    "fengling_taihou",
    "shen_zhenyue",
    "cao_tianlang",
    "xiao_chengying",
    "innkeeper_zhao",
    "su_wanqing",
    "fishing_elder",
    "blacksmith_wang",
)

ENEMY_ARCHETYPES = {
    "tianshu": ("black_guard", "market_thug", "rogue_scholar", "palace_spy"),
    "cangyue": ("mountain_bandit", "traitor_disciple", "cliff_wolf", "sword_puppet"),
    "yanliu": ("river_bandit", "rebel_scout", "poison_smuggler", "marsh_raider"),
    "chisha": ("desert_raider", "beidi_scout", "sand_wolf", "fortress_deserter"),
    "youhuang": ("bamboo_assassin", "poison_cultist", "swamp_beast", "mechanism_puppet"),
    "hanyuan": ("snow_bandit", "ice_wolf", "frost_cultist", "tomb_guard"),
}
ENEMY_IDS = tuple(
    "{}_{}".format(region, archetype)
    for region in CORE_REGIONS
    for archetype in ENEMY_ARCHETYPES[region]
)

BOSS_IDS = (
    "helian_beiming",
    "liu_hanzhang",
    "feng_sanniang",
    "prologue_black_guard",
    "tianshu_black_market_lord",
    "cangyue_traitor_master",
    "yanliu_rebel_gang_lord",
    "chisha_beidi_vanguard",
    "youhuang_forbidden_mage",
    "hanyuan_snow_beast",
)


def _recipes(filename):
    return load_character_manifest(MANIFEST_ROOT / filename).characters


def build_player_recipes():
    return _recipes("player-roster.json")


def build_named_recipes():
    return _recipes("named-roster.json")


def build_npc_recipes():
    return _recipes("npc-roster.json")


def build_enemy_recipes():
    return _recipes("enemy-roster.json")


def build_boss_recipes():
    return _recipes("boss-roster.json")


def build_roster():
    return (
        build_player_recipes()
        + build_named_recipes()
        + build_npc_recipes()
        + build_enemy_recipes()
        + build_boss_recipes()
    )
