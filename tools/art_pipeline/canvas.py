"""Pixel-safe operations for composing editable transparent PNG modules."""

from pathlib import Path

from PIL import Image, ImageOps


class PixelBoundsError(ValueError):
    """Raised when a source module would be clipped by composition."""


class PixelCanvas:
    def __init__(self, width, height):
        if not isinstance(width, int) or not isinstance(height, int) or width <= 0 or height <= 0:
            raise ValueError("canvas dimensions must be positive integers")
        self.image = Image.new("RGBA", (width, height), (0, 0, 0, 0))

    @property
    def size(self):
        return self.image.size

    def load_module(self, path):
        module_path = Path(path)
        try:
            with Image.open(module_path) as source:
                return source.convert("RGBA")
        except OSError as exc:
            raise ValueError("cannot load PNG module '{}': {}".format(module_path, exc)) from exc

    def crop(self, bounds):
        left, top, right, bottom = bounds
        if left < 0 or top < 0 or right > self.size[0] or bottom > self.size[1] or right <= left or bottom <= top:
            raise PixelBoundsError("crop {} is outside {}x{} canvas".format(bounds, *self.size))
        return self.image.crop(bounds)

    def assert_inside(self, module, position):
        x, y = position
        width, height = module.size
        canvas_width, canvas_height = self.size
        if x < 0 or y < 0 or x + width > canvas_width or y + height > canvas_height:
            raise PixelBoundsError(
                "module {}x{} at ({}, {}) is outside {}x{} canvas".format(
                    width, height, x, y, canvas_width, canvas_height
                )
            )

    def paste(self, module, position=(0, 0)):
        rgba_module = module.convert("RGBA")
        self.assert_inside(rgba_module, position)
        overlay = Image.new("RGBA", self.size, (0, 0, 0, 0))
        overlay.paste(rgba_module, position, rgba_module)
        self.image = Image.alpha_composite(self.image, overlay)
        return self

    def mirror_x(self, module):
        return ImageOps.mirror(module.convert("RGBA"))

    def recolor_by_palette_role(self, module, replacements):
        rgba_module = module.convert("RGBA")
        recolored = [replacements.get(pixel, pixel) for pixel in rgba_module.getdata()]
        result = Image.new("RGBA", rgba_module.size)
        result.putdata(recolored)
        return result

