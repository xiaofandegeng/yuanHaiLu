"""Strict JSON contracts shared by the character and environment bakers."""

import json
import re
from dataclasses import dataclass
from pathlib import Path

from . import SCHEMA_VERSION


ART_ID_PATTERN = re.compile(r"^[a-z][a-z0-9_]{2,63}$")


class ManifestError(ValueError):
    """Raised when an art manifest violates the production contract."""


def _stable_id(value):
    if not isinstance(value, str) or ART_ID_PATTERN.fullmatch(value) is None:
        raise ManifestError("invalid art id: {!r}".format(value))
    return value


def _module_names(value, owner_id):
    if not isinstance(value, list) or not value:
        raise ManifestError("{} requires at least one module".format(owner_id))
    modules = []
    for module in value:
        if not isinstance(module, str) or not module.strip():
            raise ManifestError("{} contains an invalid module name".format(owner_id))
        modules.append(module.strip())
    return tuple(modules)


@dataclass(frozen=True)
class AnimationRow:
    name: str
    direction: str
    frames: int
    fps: int
    loop: bool
    hit_frames: tuple = ()

    @classmethod
    def from_dict(cls, payload):
        if not isinstance(payload, dict):
            raise ManifestError("animation row must be an object")
        name = payload.get("name")
        direction = payload.get("direction")
        frames = payload.get("frames")
        fps = payload.get("fps")
        loop = payload.get("loop")
        hit_frames = payload.get("hitFrames", [])

        if not isinstance(name, str) or not name:
            raise ManifestError("animation row requires a name")
        if not isinstance(direction, str) or not direction:
            raise ManifestError("animation row requires a direction")
        if not isinstance(frames, int) or frames <= 0 or frames > 6:
            raise ManifestError("animation frames must be between 1 and 6")
        if not isinstance(fps, int) or fps <= 0:
            raise ManifestError("animation fps must be positive")
        if not isinstance(loop, bool):
            raise ManifestError("animation loop must be boolean")
        if not isinstance(hit_frames, list) or any(
            not isinstance(frame, int) or frame < 0 or frame >= frames
            for frame in hit_frames
        ):
            raise ManifestError("animation hitFrames must reference valid frame indexes")

        return cls(name, direction, frames, fps, loop, tuple(hit_frames))


@dataclass(frozen=True)
class CharacterRecipe:
    id: str
    frame_size: int
    modules: tuple
    animations: tuple = ()

    @classmethod
    def from_dict(cls, payload):
        if not isinstance(payload, dict):
            raise ManifestError("character recipe must be an object")
        art_id = _stable_id(payload.get("id"))
        frame_size = payload.get("frameSize")
        if frame_size != 32:
            raise ManifestError("{} frameSize must be 32".format(art_id))
        modules = _module_names(payload.get("modules"), art_id)
        rows = tuple(AnimationRow.from_dict(row) for row in payload.get("animations", []))
        keys = [(row.name, row.direction) for row in rows]
        if len(keys) != len(set(keys)):
            raise ManifestError("{} contains duplicate animation row".format(art_id))
        return cls(art_id, frame_size, modules, rows)


@dataclass(frozen=True)
class LandmarkRecipe:
    id: str
    module: str
    collision: tuple
    foreground_cut: int

    @classmethod
    def from_dict(cls, payload):
        if not isinstance(payload, dict):
            raise ManifestError("landmark recipe must be an object")
        art_id = _stable_id(payload.get("id"))
        module = payload.get("module")
        collision = payload.get("collision")
        foreground_cut = payload.get("foregroundCut")
        if not isinstance(module, str) or not module.strip():
            raise ManifestError("{} landmark requires a module".format(art_id))
        if (
            not isinstance(collision, list)
            or len(collision) != 4
            or any(not isinstance(value, int) or value < 0 for value in collision)
            or collision[2] <= 0
            or collision[3] <= 0
        ):
            raise ManifestError(
                "{} landmark collision must be [x, y, width, height]".format(art_id)
            )
        if not isinstance(foreground_cut, int) or foreground_cut < 0:
            raise ManifestError(
                "{} landmark foregroundCut must be non-negative".format(art_id)
            )
        return cls(art_id, module.strip(), tuple(collision), foreground_cut)


@dataclass(frozen=True)
class EnvironmentRecipe:
    id: str
    tile_size: int
    modules: tuple
    landmarks: tuple = ()
    blocking_tile_roles: tuple = ()

    @classmethod
    def from_dict(cls, payload):
        if not isinstance(payload, dict):
            raise ManifestError("environment recipe must be an object")
        art_id = _stable_id(payload.get("id"))
        tile_size = payload.get("tileSize")
        if tile_size != 16:
            raise ManifestError("{} tileSize must be 16".format(art_id))
        landmarks = tuple(
            LandmarkRecipe.from_dict(item) for item in payload.get("landmarks", [])
        )
        landmark_ids = [landmark.id for landmark in landmarks]
        if len(landmark_ids) != len(set(landmark_ids)):
            raise ManifestError("{} contains duplicate landmark id".format(art_id))
        blocking_roles = payload.get("blockingTileRoles", [])
        if not isinstance(blocking_roles, list) or any(
            not isinstance(role, str) or not role for role in blocking_roles
        ):
            raise ManifestError("{} blockingTileRoles must be an array of role names".format(art_id))
        module_roles = {
            re.sub(r"_[0-9]+$", "", Path(module).stem)
            for module in payload.get("modules", [])
            if isinstance(module, str)
        }
        unknown_blocking = set(blocking_roles) - module_roles
        if unknown_blocking:
            raise ManifestError(
                "{} blocking tile role has no declared sprite role: {}".format(
                    art_id, ", ".join(sorted(unknown_blocking))
                )
            )
        return cls(
            art_id,
            tile_size,
            _module_names(payload.get("modules"), art_id),
            landmarks,
            tuple(blocking_roles),
        )


def _payload_items(payload, key):
    if not isinstance(payload, dict) or payload.get("schemaVersion") != SCHEMA_VERSION:
        raise ManifestError("schemaVersion must be {}".format(SCHEMA_VERSION))
    items = payload.get(key)
    if not isinstance(items, list):
        raise ManifestError("{} must be an array".format(key))
    return items


@dataclass(frozen=True)
class CharacterManifest:
    characters: tuple

    @classmethod
    def from_dict(cls, payload):
        characters = tuple(
            CharacterRecipe.from_dict(item) for item in _payload_items(payload, "characters")
        )
        ids = [character.id for character in characters]
        if len(ids) != len(set(ids)):
            duplicate = next(art_id for art_id in ids if ids.count(art_id) > 1)
            raise ManifestError("duplicate character id: {}".format(duplicate))
        return cls(characters)


@dataclass(frozen=True)
class EnvironmentManifest:
    environments: tuple

    @classmethod
    def from_dict(cls, payload):
        environments = tuple(
            EnvironmentRecipe.from_dict(item)
            for item in _payload_items(payload, "environments")
        )
        ids = [environment.id for environment in environments]
        if len(ids) != len(set(ids)):
            duplicate = next(art_id for art_id in ids if ids.count(art_id) > 1)
            raise ManifestError("duplicate environment id: {}".format(duplicate))
        return cls(environments)


def _load_json(path):
    manifest_path = Path(path)
    try:
        return json.loads(manifest_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ManifestError("cannot load manifest '{}': {}".format(manifest_path, exc)) from exc


def load_character_manifest(path):
    return CharacterManifest.from_dict(_load_json(path))


def load_environment_manifest(path):
    return EnvironmentManifest.from_dict(_load_json(path))
