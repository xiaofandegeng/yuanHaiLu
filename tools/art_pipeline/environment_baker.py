"""Compose independent environment tilesets from transparent PNG modules."""

import hashlib
import json
import re
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path

from PIL import Image

from .canvas import PixelCanvas


@dataclass(frozen=True)
class BakedEnvironment:
    image: Image.Image
    image_path: Path
    landmark_image_path: object
    metadata_path: Path
    sha256: str
    tile_size: int
    changed: bool


def _image_hash(image):
    digest = hashlib.sha256()
    digest.update("{}x{}:RGBA".format(*image.size).encode("ascii"))
    digest.update(image.tobytes())
    return digest.hexdigest()


def _tile_role(path):
    return re.sub(r"_[0-9]+$", "", Path(path).stem)


def _file_matches(path, expected_hash):
    if not path.exists():
        return False
    try:
        with Image.open(path) as current:
            return _image_hash(current.convert("RGBA")) == expected_hash
    except OSError:
        return False


def _bake_landmarks(recipe, output_dir):
    if not recipe.landmarks:
        return None, None, [], False

    source_images = []
    module_loader = PixelCanvas(1, 1)
    for landmark in recipe.landmarks:
        module = module_loader.load_module(landmark.module)
        if module.width <= 0 or module.height <= 0:
            raise ValueError("landmark '{}' has invalid dimensions".format(landmark.module))
        collision_x, collision_y, collision_width, collision_height = landmark.collision
        if (
            collision_x + collision_width > module.width
            or collision_y + collision_height > module.height
            or landmark.foreground_cut > module.height
        ):
            raise ValueError(
                "landmark '{}' metadata exceeds its {}x{} canvas".format(
                    landmark.module, module.width, module.height
                )
            )
        source_images.append((landmark, module))

    sheet = PixelCanvas(
        sum(module.width for _, module in source_images),
        max(module.height for _, module in source_images),
    )
    entries = []
    cursor_x = 0
    for landmark, module in source_images:
        y = sheet.image.height - module.height
        sheet.paste(module, (cursor_x, y))
        entries.append(
            {
                "name": "{}__landmark__{}".format(recipe.id, landmark.id),
                "id": landmark.id,
                "rect": [cursor_x, y, module.width, module.height],
                "pivot": [0.5, 0.0],
                "collision": list(landmark.collision),
                "foregroundCut": landmark.foreground_cut,
            }
        )
        cursor_x += module.width

    landmark_hash = _image_hash(sheet.image)
    landmark_path = Path(output_dir) / "{}_landmarks.png".format(recipe.id)
    changed = not _file_matches(landmark_path, landmark_hash)
    landmark_path.parent.mkdir(parents=True, exist_ok=True)
    if changed:
        sheet.image.save(landmark_path, format="PNG", optimize=False, compress_level=9)
    return landmark_path, landmark_hash, entries, changed


def bake_environment(recipe, output_dir):
    tile_size = recipe.tile_size
    columns = max(len(recipe.modules), 1)
    canvas = PixelCanvas(columns * tile_size, tile_size)
    roles = defaultdict(int)
    sprites = []

    for index, module_name in enumerate(recipe.modules):
        module = canvas.load_module(module_name)
        if module.size != (tile_size, tile_size):
            raise ValueError(
                "environment tile '{}' must be {}x{}, got {}x{}".format(
                    module_name, tile_size, tile_size, *module.size
                )
            )
        canvas.paste(module, (index * tile_size, 0))
        role = _tile_role(module_name)
        variant = roles[role]
        roles[role] += 1
        sprites.append(
            {
                "name": "{}__{}__{}".format(recipe.id, role, variant),
                "role": role,
                "variant": variant,
                "rect": [index * tile_size, 0, tile_size, tile_size],
                "pivot": [0.5, 0.5],
            }
        )

    sha256 = _image_hash(canvas.image)
    output_path = Path(output_dir) / "{}_tileset.png".format(recipe.id)
    metadata_path = output_path.with_suffix(".art.json")
    landmark_path, landmark_hash, landmarks, landmark_changed = _bake_landmarks(
        recipe, output_dir
    )
    changed = not _file_matches(output_path, sha256) or landmark_changed
    output_path.parent.mkdir(parents=True, exist_ok=True)
    if not _file_matches(output_path, sha256):
        canvas.image.save(output_path, format="PNG", optimize=False, compress_level=9)

    metadata = {
        "schemaVersion": 1,
        "kind": "environment",
        "id": recipe.id,
        "image": output_path.name,
        "sha256": sha256,
        "width": canvas.image.width,
        "height": canvas.image.height,
        "tileSize": tile_size,
        "sprites": sprites,
        "landmarkImage": landmark_path.name if landmark_path else None,
        "landmarkSha256": landmark_hash,
        "landmarks": landmarks,
    }
    encoded = json.dumps(metadata, ensure_ascii=False, indent=2, sort_keys=True) + "\n"
    if not metadata_path.exists() or metadata_path.read_text(encoding="utf-8") != encoded:
        metadata_path.write_text(encoded, encoding="utf-8")
        changed = True

    return BakedEnvironment(
        canvas.image,
        output_path,
        landmark_path,
        metadata_path,
        sha256,
        tile_size,
        changed,
    )
