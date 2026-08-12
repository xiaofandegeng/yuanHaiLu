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
    changed = True
    if output_path.exists() and metadata_path.exists():
        try:
            old_metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
            with Image.open(output_path) as current:
                changed = old_metadata.get("sha256") != sha256 or _image_hash(current.convert("RGBA")) != sha256
        except (OSError, json.JSONDecodeError):
            changed = True
    output_path.parent.mkdir(parents=True, exist_ok=True)
    if changed:
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
        "landmarks": [],
    }
    encoded = json.dumps(metadata, ensure_ascii=False, indent=2, sort_keys=True) + "\n"
    if not metadata_path.exists() or metadata_path.read_text(encoding="utf-8") != encoded:
        metadata_path.write_text(encoded, encoding="utf-8")
        changed = True

    return BakedEnvironment(
        canvas.image, output_path, metadata_path, sha256, tile_size, changed
    )

