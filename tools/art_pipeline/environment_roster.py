"""Exact formal environment scope and manifest helpers."""

from pathlib import Path

from .schema import load_environment_manifest


PROJECT_ROOT = Path(__file__).resolve().parents[2]
MANIFEST_ROOT = PROJECT_ROOT / "Assets" / "ArtSource" / "Environment" / "Manifests"

REGION_IDS = (
    "tianshu",
    "cangyue",
    "yanliu",
    "chisha",
    "youhuang",
    "hanyuan",
    "prologue_village",
    "luoyuan",
    "jueyun",
    "zhenyue",
)
INTERIOR_IDS = (
    "inn",
    "residence",
    "shop",
    "pharmacy",
    "academy",
    "yamen",
    "palace",
    "temple",
    "cave",
    "tomb",
    "dungeon",
    "military_camp",
    "ship_cabin",
)


def build_region_recipes():
    return load_environment_manifest(MANIFEST_ROOT / "regions.json").environments


def build_interior_recipes():
    return load_environment_manifest(MANIFEST_ROOT / "interiors.json").environments
