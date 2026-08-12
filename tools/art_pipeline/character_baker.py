"""Compose independent character sheets from transparent PNG modules."""

import hashlib
import json
from dataclasses import dataclass
from pathlib import Path

from PIL import Image

from .canvas import PixelCanvas


@dataclass(frozen=True)
class BakedCharacter:
    image: Image.Image
    image_path: Path
    metadata_path: Path
    sha256: str
    changed: bool


def _image_hash(image):
    digest = hashlib.sha256()
    digest.update("{}x{}:RGBA".format(*image.size).encode("ascii"))
    digest.update(image.tobytes())
    return digest.hexdigest()


def _write_image_if_changed(image, path, sha256):
    metadata_path = path.with_suffix(".art.json")
    if path.exists() and metadata_path.exists():
        try:
            metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
            if metadata.get("sha256") == sha256:
                with Image.open(path) as current:
                    if _image_hash(current.convert("RGBA")) == sha256:
                        return False
        except (OSError, json.JSONDecodeError):
            pass
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, format="PNG", optimize=False, compress_level=9)
    return True


def bake_character(recipe, output_dir):
    frame_size = recipe.frame_size
    animations = recipe.animations
    columns = max((row.frames for row in animations), default=1)
    rows = max(len(animations), 1)
    sheet_size = (columns * frame_size, rows * frame_size)
    canvas = PixelCanvas(*sheet_size)

    for module_name in recipe.modules:
        module = canvas.load_module(module_name)
        if module.size != sheet_size:
            raise ValueError(
                "character module '{}' must be {}x{}, got {}x{}".format(
                    module_name, *sheet_size, *module.size
                )
            )
        canvas.paste(module)

    sha256 = _image_hash(canvas.image)
    output_path = Path(output_dir) / "{}.png".format(recipe.id)
    metadata_path = output_path.with_suffix(".art.json")
    changed = _write_image_if_changed(canvas.image, output_path, sha256)

    sprites = []
    animation_metadata = []
    for row_index, animation in enumerate(animations):
        frame_names = []
        for frame_index in range(animation.frames):
            name = "{}__{}__{}__{}".format(
                recipe.id, animation.name, animation.direction, frame_index
            )
            frame_names.append(name)
            sprites.append(
                {
                    "name": name,
                    "rect": [frame_index * frame_size, row_index * frame_size, frame_size, frame_size],
                    "pivot": [0.5, 0.0],
                }
            )
        animation_metadata.append(
            {
                "name": animation.name,
                "direction": animation.direction,
                "frames": frame_names,
                "fps": animation.fps,
                "loop": animation.loop,
                "hitFrames": list(animation.hit_frames),
            }
        )

    metadata = {
        "schemaVersion": 1,
        "kind": "character",
        "id": recipe.id,
        "image": output_path.name,
        "sha256": sha256,
        "width": canvas.image.width,
        "height": canvas.image.height,
        "frameSize": frame_size,
        "pivot": [0.5, 0.0],
        "sprites": sprites,
        "animations": animation_metadata,
    }
    encoded = json.dumps(metadata, ensure_ascii=False, indent=2, sort_keys=True) + "\n"
    if not metadata_path.exists() or metadata_path.read_text(encoding="utf-8") != encoded:
        metadata_path.write_text(encoded, encoding="utf-8")
        changed = True

    return BakedCharacter(canvas.image, output_path, metadata_path, sha256, changed)

