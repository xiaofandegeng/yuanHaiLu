"""Validation helpers for visible editable character module PNGs."""

from pathlib import Path

from PIL import Image


REQUIRED_LAYERS = ("body", "face", "hair", "outfit", "weapon", "accessory")


def validate_character_modules(recipe):
    if len(recipe.modules) != len(REQUIRED_LAYERS):
        raise ValueError(
            "{} must declare exactly {} visible layers".format(
                recipe.id, len(REQUIRED_LAYERS)
            )
        )
    columns = max((row.frames for row in recipe.animations), default=1)
    expected_size = (columns * recipe.frame_size, len(recipe.animations) * recipe.frame_size)
    visible_layers = 0
    for layer, module_path in zip(REQUIRED_LAYERS, recipe.modules):
        path = Path(module_path)
        if not path.exists():
            raise ValueError("{} {} module is missing: {}".format(recipe.id, layer, path))
        with Image.open(path) as source:
            image = source.convert("RGBA")
        if image.size != expected_size:
            raise ValueError(
                "{} {} module must be {}x{}, got {}x{}".format(
                    recipe.id, layer, *expected_size, *image.size
                )
            )
        if image.getbbox() is not None:
            visible_layers += 1
        elif layer == "body":
            raise ValueError("{} {} module has no visible pixels".format(recipe.id, layer))
    if visible_layers < 3:
        raise ValueError("{} requires at least three visible module layers".format(recipe.id))
    return True
