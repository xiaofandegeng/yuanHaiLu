"""Strict, flood-fillable layout contract for generated environment scenes."""

import json
from collections import deque
from dataclasses import dataclass
from pathlib import Path


class MapLayoutError(ValueError):
    pass


@dataclass(frozen=True)
class MapAnchor:
    id: str
    type: str
    x: int
    y: int


@dataclass(frozen=True)
class MapLayout:
    id: str
    kind: str
    width: int
    height: int
    layers: dict
    collisions: frozenset
    foreground_spans: tuple
    anchors: tuple
    required_landmarks: tuple


def load_map_layout(path):
    layout_path = Path(path)
    try:
        payload = json.loads(layout_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise MapLayoutError("cannot load {}: {}".format(layout_path, exc)) from exc
    width = payload.get("width")
    height = payload.get("height")
    if not isinstance(width, int) or not isinstance(height, int) or width <= 0 or height <= 0:
        raise MapLayoutError("layout requires positive dimensions")
    kind = payload.get("kind")
    if kind not in ("region", "interior"):
        raise MapLayoutError("layout kind must be region or interior")
    anchors = []
    for item in payload.get("anchors", []):
        anchor = MapAnchor(item["id"], item["type"], item["x"], item["y"])
        if anchor.type not in {"entry", "exit", "interior", "quest", "spawn", "camera", "landmark"}:
            raise MapLayoutError("unknown anchor type {}".format(anchor.type))
        if not (0 <= anchor.x < width and 0 <= anchor.y < height):
            raise MapLayoutError("anchor {} is out of bounds".format(anchor.id))
        anchors.append(anchor)
    if not any(anchor.type == "entry" for anchor in anchors) or not any(anchor.type == "exit" for anchor in anchors):
        raise MapLayoutError("layout requires entry and exit anchors")
    collisions = frozenset(tuple(cell) for cell in payload.get("collisions", []))
    return MapLayout(
        payload["id"],
        kind,
        width,
        height,
        payload.get("layers", {}),
        collisions,
        tuple(tuple(span) for span in payload.get("foregroundSpans", [])),
        tuple(anchors),
        tuple(payload.get("requiredLandmarks", [])),
    )


def reachable_anchor_ids(layout):
    entry = next(anchor for anchor in layout.anchors if anchor.type == "entry")
    queue = deque([(entry.x, entry.y)])
    visited = {(entry.x, entry.y)}
    while queue:
        x, y = queue.popleft()
        for neighbor in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            nx, ny = neighbor
            if 0 <= nx < layout.width and 0 <= ny < layout.height and neighbor not in layout.collisions and neighbor not in visited:
                visited.add(neighbor)
                queue.append(neighbor)
    return {
        anchor.id
        for anchor in layout.anchors
        if (anchor.x, anchor.y) in visited
    }
