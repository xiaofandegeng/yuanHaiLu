"""Validate hashes and pixel contracts for baked formal art."""

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image

from .character_roster import BOSS_IDS, ENEMY_IDS, NAMED_IDS, build_roster
from .environment_roster import INTERIOR_IDS, REGION_IDS


PROJECT_ROOT = Path(__file__).resolve().parents[2]


def _image_hash(image):
    digest = hashlib.sha256()
    digest.update("{}x{}:RGBA".format(*image.size).encode("ascii"))
    digest.update(image.tobytes())
    return digest.hexdigest()


def expected_metadata_scope():
    expected = {}
    for recipe in build_roster():
        if recipe.id.startswith("player_"):
            category = "Player"
        elif recipe.id in NAMED_IDS:
            category = "Named"
        elif recipe.id in BOSS_IDS:
            category = "Bosses"
        elif recipe.id in ENEMY_IDS:
            category = "Enemies"
        else:
            category = "NPC"
        expected["Characters/{}/{}.art.json".format(category, recipe.id)] = recipe.id
    for art_id in REGION_IDS:
        expected[
            "Environment/Regions/{0}/{0}_tileset.art.json".format(art_id)
        ] = art_id
    for art_id in INTERIOR_IDS:
        expected[
            "Environment/Interiors/{0}/{0}_tileset.art.json".format(art_id)
        ] = art_id
    return expected


def validate_output_scope(root, expected=None):
    root_path = Path(root)
    expected = expected or expected_metadata_scope()
    actual = {
        path.relative_to(root_path).as_posix(): path
        for path in root_path.rglob("*.art.json")
    }
    errors = []
    for relative_path in sorted(set(expected) - set(actual)):
        errors.append("missing formal metadata: {}".format(relative_path))
    for relative_path in sorted(set(actual) - set(expected)):
        errors.append("unexpected formal metadata: {}".format(relative_path))
    for relative_path in sorted(set(actual) & set(expected)):
        try:
            metadata = json.loads(actual[relative_path].read_text(encoding="utf-8"))
            if metadata.get("id") != expected[relative_path]:
                errors.append(
                    "{}: expected id {}, got {}".format(
                        relative_path,
                        expected[relative_path],
                        metadata.get("id"),
                    )
                )
        except (OSError, json.JSONDecodeError) as exc:
            errors.append("{}: {}".format(relative_path, exc))
    return errors


def validate_outputs(root, enforce_scope=False):
    errors = []
    root_path = Path(root)
    if enforce_scope:
        errors.extend(validate_output_scope(root_path))
    for metadata_path in sorted(root_path.rglob("*.art.json")):
        try:
            metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
            image_path = metadata_path.parent / metadata["image"]
            with Image.open(image_path) as source:
                image = source.convert("RGBA")
            actual_hash = _image_hash(image)
            if actual_hash != metadata.get("sha256"):
                errors.append("{}: hash mismatch".format(image_path))
                continue
            if image.getbbox() is None:
                errors.append("{}: image has no opaque pixels".format(image_path))
                continue
            unit = metadata.get("frameSize", metadata.get("tileSize"))
            if unit not in (16, 32) or image.width % unit != 0 or image.height % unit != 0:
                errors.append("{}: dimensions violate {}px unit".format(image_path, unit))
            landmark_name = metadata.get("landmarkImage")
            if landmark_name:
                landmark_path = metadata_path.parent / landmark_name
                with Image.open(landmark_path) as source:
                    landmark_image = source.convert("RGBA")
                if _image_hash(landmark_image) != metadata.get("landmarkSha256"):
                    errors.append("{}: hash mismatch".format(landmark_path))
                elif landmark_image.getbbox() is None:
                    errors.append("{}: image has no opaque pixels".format(landmark_path))
        except (OSError, KeyError, json.JSONDecodeError, ValueError) as exc:
            errors.append("{}: {}".format(metadata_path, exc))
    return errors


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--all", action="store_true", required=True)
    parser.add_argument("--root", type=Path, default=PROJECT_ROOT / "Assets" / "Art")
    args = parser.parse_args(argv)
    errors = validate_outputs(args.root, enforce_scope=True)
    if errors:
        for error in errors:
            print(error)
        return 1
    print("validated {}".format(args.root))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
