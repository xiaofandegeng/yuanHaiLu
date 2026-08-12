"""Palette loading and validation for formal pixel-art assets."""

import json
from pathlib import Path


class PaletteError(ValueError):
    """Raised when a palette cannot be used by the art pipeline."""


def validate_palette(colors):
    if not isinstance(colors, dict) or not colors:
        raise PaletteError("palette must contain named color groups")

    for group_name, group_colors in colors.items():
        if not isinstance(group_name, str) or not group_name:
            raise PaletteError("palette group names must be non-empty strings")
        if not isinstance(group_colors, list) or len(group_colors) < 4:
            raise PaletteError(
                "palette group '{}' must contain at least four colors".format(group_name)
            )
        for color in group_colors:
            if not isinstance(color, list) or len(color) != 4:
                raise PaletteError(
                    "palette group '{}' colors must be RGBA arrays".format(group_name)
                )
            if any(not isinstance(channel, int) or channel < 0 or channel > 255 for channel in color):
                raise PaletteError(
                    "palette group '{}' channels must stay in 0..255".format(group_name)
                )


def load_palette(path):
    palette_path = Path(path)
    try:
        payload = json.loads(palette_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise PaletteError("cannot load palette '{}': {}".format(palette_path, exc)) from exc

    groups = payload.get("groups") if isinstance(payload, dict) else None
    validate_palette(groups)
    return groups

