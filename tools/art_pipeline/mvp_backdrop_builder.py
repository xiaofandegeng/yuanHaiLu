"""Build the two fixed 480×270 gameplay backdrops used by the single-hero MVP.

The source images are art-direction originals.  This module keeps Unity-facing
outputs deterministic and avoids runtime texture creation: each source is
center-cropped to 16:9 and reduced with nearest-neighbour sampling.
"""

from pathlib import Path

from PIL import Image


LOGICAL_SIZE = (480, 270)
SOURCE_TO_OUTPUT = (
    ("yanliu_mvp_concept_v1.png", "mvp_yanliu_backdrop.png"),
    ("inn_mvp_concept_v1.png", "mvp_inn_backdrop.png"),
)


def _crop_to_logical_aspect(image):
    target_ratio = LOGICAL_SIZE[0] / LOGICAL_SIZE[1]
    source_ratio = image.width / image.height
    if source_ratio > target_ratio:
        crop_width = int(image.height * target_ratio)
        left = (image.width - crop_width) // 2
        return image.crop((left, 0, left + crop_width, image.height))
    crop_height = int(image.width / target_ratio)
    top = (image.height - crop_height) // 2
    return image.crop((0, top, image.width, top + crop_height))


def build_mvp_backdrops(source_directory, output_directory):
    """Bake both named source concepts into 480×270 RGB PNGs and return paths."""
    source_directory = Path(source_directory)
    output_directory = Path(output_directory)
    output_directory.mkdir(parents=True, exist_ok=True)
    built = []
    for source_name, output_name in SOURCE_TO_OUTPUT:
        source_path = source_directory / source_name
        if not source_path.is_file():
            raise FileNotFoundError("MVP backdrop source is missing: {}".format(source_path))
        with Image.open(source_path) as image:
            cropped = _crop_to_logical_aspect(image.convert("RGB"))
            baked = cropped.resize(LOGICAL_SIZE, Image.Resampling.NEAREST)
            output_path = output_directory / output_name
            baked.save(output_path, format="PNG", optimize=False)
            built.append(output_path)
    return tuple(built)


def main():
    project_root = Path(__file__).resolve().parents[2]
    source = project_root / "Assets" / "ArtSource" / "Environment" / "MVP"
    output = project_root / "Assets" / "Art" / "Environment" / "MVP"
    for path in build_mvp_backdrops(source, output):
        print("built={}".format(path))


if __name__ == "__main__":
    main()
