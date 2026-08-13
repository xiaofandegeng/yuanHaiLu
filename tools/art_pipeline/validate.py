"""Validate hashes and pixel contracts for baked formal art."""

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image


PROJECT_ROOT = Path(__file__).resolve().parents[2]


def _image_hash(image):
    digest = hashlib.sha256()
    digest.update("{}x{}:RGBA".format(*image.size).encode("ascii"))
    digest.update(image.tobytes())
    return digest.hexdigest()


def validate_outputs(root):
    errors = []
    root_path = Path(root)
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
    errors = validate_outputs(args.root)
    if errors:
        for error in errors:
            print(error)
        return 1
    print("validated {}".format(args.root))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
