"""Strict, flood-fillable layout contract for generated environment scenes."""

import json
from collections import deque
from dataclasses import dataclass
from pathlib import Path


FORMAL_LAYER_NAMES = (
    "Ground",
    "Water",
    "Lower Environment",
    "Buildings",
    "Character",
    "Foreground",
    "Effects",
)


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

    def coordinate_signature(self):
        """Return authored geometry without IDs or decorative tile tokens.

        This is deliberately based on cell positions only.  Two regions that
        merely swap a palette, an ID, or tile names must not pass as distinct
        layouts.
        """
        cells = tuple(
            sorted(
                (layer, x, y)
                for layer, entries in self.layers.items()
                for x, y, _ in entries
            )
        )
        return cells, tuple(sorted(self.collisions)), self.foreground_spans


def _normalise_layers(value, width, height):
    if isinstance(value, dict):
        source_layers = value.items()
    elif isinstance(value, list):
        source_layers = (
            (item.get("name"), item.get("cells"))
            for item in value
            if isinstance(item, dict)
        )
    else:
        raise MapLayoutError("layers must be an object or an array")

    result = {}
    for name, cells in source_layers:
        if name not in FORMAL_LAYER_NAMES:
            raise MapLayoutError("unknown formal layer {}".format(name))
        if not isinstance(cells, list):
            raise MapLayoutError("layer {} requires a cell array".format(name))
        normalised = []
        for cell in cells:
            if isinstance(cell, dict):
                x, y, token = cell.get("x"), cell.get("y"), cell.get("token")
            elif isinstance(cell, (list, tuple)) and len(cell) == 3:
                x, y, token = cell
            else:
                raise MapLayoutError("layer {} has an invalid cell".format(name))
            if not isinstance(x, int) or not isinstance(y, int) or not isinstance(token, str):
                raise MapLayoutError("layer {} has an invalid cell value".format(name))
            if not (0 <= x < width and 0 <= y < height):
                raise MapLayoutError("layer {} cell is out of bounds".format(name))
            normalised.append((x, y, token))
        if name in result:
            raise MapLayoutError("duplicate layer {}".format(name))
        result[name] = tuple(normalised)

    missing = set(FORMAL_LAYER_NAMES) - set(result)
    if missing:
        raise MapLayoutError("layout is missing formal layers {}".format(sorted(missing)))
    return result


def _normalise_collisions(value, width, height):
    if not isinstance(value, list):
        raise MapLayoutError("collisions must be an array of declared runs")
    cells = set()
    for run in value:
        if isinstance(run, dict):
            x, y = run.get("x"), run.get("y")
            run_width = run.get("width")
            run_height = run.get("height")
        elif isinstance(run, (list, tuple)) and len(run) == 2:
            x, y = run
            run_width = run_height = 1
        else:
            raise MapLayoutError("collision requires x, y, width and height")
        if not all(isinstance(item, int) for item in (x, y, run_width, run_height)):
            raise MapLayoutError("collision values must be integers")
        if run_width <= 0 or run_height <= 0 or x < 0 or y < 0 or x + run_width > width or y + run_height > height:
            raise MapLayoutError("collision run is out of bounds")
        cells.update(
            (cell_x, cell_y)
            for cell_x in range(x, x + run_width)
            for cell_y in range(y, y + run_height)
        )
    return frozenset(cells)


def _normalise_foreground_spans(value):
    if not isinstance(value, list):
        raise MapLayoutError("foregroundSpans must be an array")
    normalised = []
    for span in value:
        if isinstance(span, dict):
            fields = (span.get("fromX"), span.get("fromY"), span.get("toX"), span.get("toY"), span.get("token"))
        elif isinstance(span, (list, tuple)) and len(span) == 4:
            fields = (*span, "")
        else:
            raise MapLayoutError("foreground span must declare endpoints and token")
        if not all(isinstance(item, int) for item in fields[:4]) or not isinstance(fields[4], str):
            raise MapLayoutError("foreground span values are invalid")
        normalised.append(fields)
    return tuple(normalised)


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
    layers = _normalise_layers(payload.get("layers"), width, height)
    collisions = _normalise_collisions(payload.get("collisions", []), width, height)
    return MapLayout(
        payload["id"],
        kind,
        width,
        height,
        layers,
        collisions,
        _normalise_foreground_spans(payload.get("foregroundSpans", [])),
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
