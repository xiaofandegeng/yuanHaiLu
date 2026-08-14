"""Fail-fast validation for committed, rebuildable character source sheets."""

from pathlib import Path

from PIL import Image, UnidentifiedImageError


REQUIRED_LAYERS = ("body", "face", "hair", "outfit", "weapon", "accessory")


def audit_character_sources(recipes, unique_layers=REQUIRED_LAYERS[2:]):
    """Return deterministic, human-readable source-art contract violations.

    The audit is deliberately independent from Unity.  It validates the exact source
    sheets referenced by every recipe, so a stale baked PNG can never conceal a
    missing, incorrectly sized, transparent, or accidentally shared editable module.
    """

    errors = []
    owners = {}
    unique_layers = set(unique_layers)

    for recipe in recipes:
        modules = tuple(getattr(recipe, "modules", ()))
        if len(modules) != len(REQUIRED_LAYERS):
            errors.append(
                "{} must declare exactly {} source modules".format(
                    recipe.id, len(REQUIRED_LAYERS)
                )
            )
            continue

        expected_size = _expected_sheet_size(recipe)
        for layer, raw_path in zip(REQUIRED_LAYERS, modules):
            path = Path(raw_path)
            if not path.is_file():
                errors.append("{} {} source missing: {}".format(recipe.id, layer, path))
                continue

            resolved_path = path.resolve()
            if layer in unique_layers:
                owners.setdefault((layer, resolved_path.as_posix()), []).append(recipe.id)

            try:
                with Image.open(path) as source:
                    image = source.convert("RGBA")
            except (OSError, UnidentifiedImageError):
                errors.append("{} {} source is not a readable PNG: {}".format(recipe.id, layer, path))
                continue

            if image.size != expected_size:
                errors.append(
                    "{} {} source must be {}x{}, got {}x{}: {}".format(
                        recipe.id,
                        layer,
                        expected_size[0],
                        expected_size[1],
                        image.size[0],
                        image.size[1],
                        path,
                    )
                )
            elif image.getbbox() is None:
                errors.append(
                    "{} {} source has no visible pixels: {}".format(recipe.id, layer, path)
                )

    for (layer, path), ids in sorted(owners.items()):
        if len(ids) > 1:
            errors.append(
                "{} source shared by {}: {}".format(layer, ", ".join(sorted(ids)), path)
            )
    return errors


def assert_character_sources_complete(recipes):
    """Raise one actionable error containing every invalid character source."""

    errors = audit_character_sources(recipes)
    if errors:
        raise ValueError("\n".join(errors))


def _expected_sheet_size(recipe):
    animations = tuple(getattr(recipe, "animations", ()))
    frame_size = getattr(recipe, "frame_size", 32)
    columns = max((row.frames for row in animations), default=1)
    rows = max(len(animations), 1)
    return columns * frame_size, rows * frame_size
