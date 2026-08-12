"""Visible source-module validation for formal environments."""

from pathlib import Path

from PIL import Image


def validate_environment_modules(recipe):
    for module_name in recipe.modules:
        path = Path(module_name)
        if not path.exists():
            raise ValueError("{} is missing module {}".format(recipe.id, path))
        with Image.open(path) as source:
            image = source.convert("RGBA")
        if image.size != (16, 16):
            raise ValueError("{} module {} is not 16x16".format(recipe.id, path))
        if image.getbbox() is None:
            raise ValueError("{} module {} has no pixels".format(recipe.id, path))
    for landmark in recipe.landmarks:
        path = Path(landmark.module)
        if not path.exists():
            raise ValueError("{} is missing landmark {}".format(recipe.id, path))
        with Image.open(path) as source:
            if source.convert("RGBA").getbbox() is None:
                raise ValueError("{} landmark {} has no pixels".format(recipe.id, path))
    return True
