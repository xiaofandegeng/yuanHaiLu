"""Command-line entry point for deterministic formal-art builds."""

import argparse
import json
from dataclasses import dataclass
from pathlib import Path

from .character_baker import bake_character
from .environment_baker import bake_environment
from .mvp_scene_layer_builder import build_mvp_scene_art_with_change_count
from .schema import load_character_manifest, load_environment_manifest


PROJECT_ROOT = Path(__file__).resolve().parents[2]


@dataclass(frozen=True)
class BuildResult:
    built: int = 0
    skipped: int = 0

    def plus(self, changed):
        return BuildResult(self.built + int(changed), self.skipped + int(not changed))


def _resolve_output(raw_payload, explicit_output, kind):
    if explicit_output is not None:
        return Path(explicit_output)
    configured = raw_payload.get("outputDirectory")
    if configured:
        configured_path = Path(configured)
        return configured_path if configured_path.is_absolute() else PROJECT_ROOT / configured_path
    suffix = "Characters/Generated" if kind == "characters" else "Environment/Generated"
    return PROJECT_ROOT / "Assets" / "Art" / suffix


def build_manifest(manifest_path, output_dir=None):
    manifest_path = Path(manifest_path)
    payload = json.loads(manifest_path.read_text(encoding="utf-8"))
    result = BuildResult()

    if "characters" in payload:
        destination = _resolve_output(payload, output_dir, "characters")
        for recipe in load_character_manifest(manifest_path).characters:
            baked = bake_character(recipe, destination)
            result = result.plus(baked.changed)
        return result

    if "environments" in payload:
        destination = _resolve_output(payload, output_dir, "environments")
        for recipe in load_environment_manifest(manifest_path).environments:
            recipe_destination = (
                destination / recipe.id
                if payload.get("perRecipeDirectory") is True
                else destination
            )
            baked = bake_environment(recipe, recipe_destination)
            result = result.plus(baked.changed)
            for variant in recipe.state_variants:
                if variant.id == "normal":
                    continue
                baked_variant = bake_environment(recipe.variant_recipe(variant.id), recipe_destination)
                result = result.plus(baked_variant.changed)
        return result

    raise ValueError("manifest '{}' contains neither characters nor environments".format(manifest_path))


def _all_manifests():
    source_root = PROJECT_ROOT / "Assets" / "ArtSource"
    manifests = sorted(source_root.glob("**/Manifests/*.json"))
    full_player_roster = source_root / "Characters" / "Manifests" / "player-roster.json"
    if full_player_roster.exists():
        manifests = [
            path for path in manifests if path.name != "reference-characters.json"
        ]
    full_region_roster = source_root / "Environment" / "Manifests" / "regions.json"
    if full_region_roster.exists():
        manifests = [
            path for path in manifests if path.name != "yanliu-reference.json"
        ]
    return manifests


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    selection = parser.add_mutually_exclusive_group(required=True)
    selection.add_argument("--manifest", type=Path)
    selection.add_argument("--all", action="store_true")
    parser.add_argument("--output-dir", type=Path)
    args = parser.parse_args(argv)

    manifests = _all_manifests() if args.all else [args.manifest]
    total = BuildResult()
    for manifest in manifests:
        result = build_manifest(manifest, args.output_dir)
        total = BuildResult(total.built + result.built, total.skipped + result.skipped)
    if args.all:
        _, changed = build_mvp_scene_art_with_change_count(
            PROJECT_ROOT / "Assets" / "ArtSource" / "Environment" / "MVP" / "v2",
            PROJECT_ROOT / "Assets" / "Art" / "Environment" / "MVP" / "v2",
            PROJECT_ROOT / "Assets" / "ArtSource" / "Characters" / "MVP",
            PROJECT_ROOT / "Assets" / "Resources" / "Art" / "MVP")
        total = BuildResult(total.built + changed, total.skipped + 10 - changed)
    print("built={} skipped={}".format(total.built, total.skipped))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
