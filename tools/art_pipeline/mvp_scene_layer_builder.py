"""Author the two MVP scenes as coherent native-resolution pixel layers.

These are deliberately not downscaled concept paintings.  Every mark is made
on the game's 480×270 grid, then split into ground/environment/foreground so
characters occupy a genuine middle layer in Unity.
"""

from pathlib import Path

from PIL import Image, ImageDraw


LOGICAL_SIZE = (480, 270)
TILE = 16
LAYER_NAMES = ("ground", "environment", "foreground")

P = {
    "ink": (24, 31, 40, 255),
    "slate": (48, 62, 70, 255),
    "stone": (91, 101, 99, 255),
    "stone_light": (145, 147, 132, 255),
    "paper": (221, 207, 169, 255),
    "water_deep": (22, 61, 75, 255),
    "water": (37, 94, 105, 255),
    "water_light": (78, 139, 142, 255),
    "jade_dark": (42, 79, 71, 255),
    "jade": (72, 113, 83, 255),
    "wood_dark": (61, 42, 34, 255),
    "wood": (113, 73, 47, 255),
    "wood_light": (167, 119, 73, 255),
    "roof": (31, 45, 57, 255),
    "roof_light": (65, 83, 92, 255),
    "warm": (232, 171, 79, 255),
    "warm_light": (255, 224, 142, 255),
    "vermilion": (176, 65, 48, 255),
    "vermilion_dark": (109, 42, 39, 255),
    "sand": (159, 132, 87, 255),
    "cloth": (115, 81, 62, 255),
}


def _new(transparent=False):
    return Image.new("RGBA", LOGICAL_SIZE, (0, 0, 0, 0) if transparent else P["water_deep"])


def _tile_noise(draw, box, base, detail, seed, step=8):
    left, top, right, bottom = box
    for y in range(top + 3, bottom, step):
        for x in range(left + 2, right, step):
            phase = (x * 17 + y * 11 + seed * 23) % 7
            if phase < 3:
                draw.rectangle((x, y, min(right, x + 2), min(bottom, y + 1)), fill=detail)
            elif phase == 4:
                draw.point((x, y), fill=base)


def _water_marks(draw, box, seed=0):
    left, top, right, bottom = box
    for y in range(top + 8, bottom, 15):
        for x in range(left + ((y + seed * 13) % 19), right, 29):
            draw.line((x, y, min(right, x + 10), y), fill=P["water_light"], width=1)
            draw.point((min(right, x + 12), y - 1), fill=P["water"])


def _stone_path(draw, points, width):
    draw.line(points, fill=P["ink"], width=width + 4, joint="curve")
    draw.line(points, fill=P["stone"], width=width, joint="curve")
    for index, (x, y) in enumerate(points[1:-1]):
        offset = (index % 2) * 5
        draw.line((x - width // 3, y + offset, x + width // 3, y + offset), fill=P["stone_light"], width=1)


def _town_house(draw, left, top, width, height):
    right = left + width
    bottom = top + height
    wall_top = top + 42
    draw.rectangle((left + 6, wall_top, right - 6, bottom), fill=P["wood_dark"])
    draw.rectangle((left + 10, wall_top + 4, right - 10, bottom - 3), fill=P["paper"])
    draw.line((left + 10, wall_top + 4, right - 10, wall_top + 4), fill=P["stone_light"], width=2)
    # Layered roof reads as a silhouette before it reads as texture.
    draw.polygon(((left, wall_top + 7), (left + 18, top + 6),
                  (right - 18, top + 6), (right, wall_top + 7)), fill=P["ink"])
    draw.polygon(((left + 5, wall_top + 5), (left + 21, top + 11),
                  (right - 21, top + 11), (right - 5, wall_top + 5)), fill=P["roof"])
    for y in range(top + 18, wall_top + 2, 8):
        draw.line((left + 10, y, right - 10, y), fill=P["roof_light"], width=2)
    draw.line((left + 4, wall_top + 8, right - 4, wall_top + 8), fill=P["ink"], width=3)
    # Door is deliberately centred on the real AreaTrigger x=120 / y≈112.
    door_x = left + width // 2
    draw.rectangle((door_x - 15, bottom - 37, door_x + 15, bottom), fill=P["ink"])
    draw.rectangle((door_x - 11, bottom - 33, door_x + 11, bottom), fill=P["wood"])
    draw.line((door_x, bottom - 33, door_x, bottom), fill=P["wood_light"], width=2)
    draw.rectangle((left + 20, wall_top + 16, left + 35, wall_top + 30), fill=P["ink"])
    draw.rectangle((left + 23, wall_top + 18, left + 32, wall_top + 27), fill=P["warm"])
    draw.rectangle((right - 35, wall_top + 16, right - 20, wall_top + 30), fill=P["ink"])
    draw.rectangle((right - 32, wall_top + 18, right - 23, wall_top + 27), fill=P["warm"])
    # Warm entrance light is on Environment, below character but above the road.
    draw.rectangle((door_x - 24, bottom + 1, door_x + 24, bottom + 7), fill=P["warm"])
    draw.rectangle((door_x - 15, bottom + 2, door_x + 15, bottom + 5), fill=P["warm_light"])


def _draw_yanliu():
    ground = _new()
    env = _new(True)
    foreground = _new(True)
    g = ImageDraw.Draw(ground)
    e = ImageDraw.Draw(env)
    f = ImageDraw.Draw(foreground)

    # West town ground and eastern canal use two values before any decoration.
    g.rectangle((0, 0, 286, 269), fill=P["jade_dark"])
    g.rectangle((0, 154, 196, 269), fill=P["water"])
    g.rectangle((286, 0, 479, 269), fill=P["water_deep"])
    _water_marks(g, (0, 164, 196, 269), 1)
    _water_marks(g, (286, 0, 479, 269), 2)
    _tile_noise(g, (0, 0, 286, 154), P["jade_dark"], P["jade"], 3)
    # A single continuous route: inn door -> bend -> riverbank arena.
    _stone_path(g, [(121, 156), (136, 175), (153, 191), (205, 209), (252, 218)], 42)
    g.ellipse((184, 184, 294, 267), fill=P["ink"])
    g.ellipse((188, 187, 291, 269), fill=P["sand"])
    _tile_noise(g, (194, 194, 287, 266), P["sand"], P["wood_light"], 7, 10)

    _town_house(e, 36, 22, 170, 116)
    # A distinct bridge joins the road to the water-side route.
    e.polygon(((262, 101), (290, 86), (321, 101), (321, 157),
               (290, 171), (262, 157)), fill=P["ink"])
    e.polygon(((267, 104), (290, 92), (316, 104), (316, 154),
               (290, 165), (267, 154)), fill=P["stone"])
    for y in (112, 127, 142):
        e.line((270, y, 313, y - 2), fill=P["stone_light"], width=2)
    e.line((268, 104, 268, 156), fill=P["paper"], width=2)
    e.line((315, 104, 315, 156), fill=P["paper"], width=2)
    # River boat, bollards and shore create a readable destination, not a texture field.
    e.polygon(((359, 172), (425, 172), (411, 192), (370, 192)), fill=P["ink"])
    e.polygon(((364, 175), (420, 175), (407, 188), (374, 188)), fill=P["wood"])
    e.line((390, 174, 390, 143), fill=P["wood_light"], width=2)
    e.polygon(((392, 145), (415, 158), (392, 164)), fill=P["cloth"])
    for x in (306, 334, 447):
        e.rectangle((x, 179, x + 3, 205), fill=P["wood_dark"])
        e.rectangle((x - 2, 177, x + 5, 182), fill=P["wood_light"])
    # Shore lantern is both a visual waypoint and a readable warm anchor.
    e.rectangle((205, 154, 208, 182), fill=P["wood_dark"])
    e.rectangle((200, 159, 213, 174), fill=P["ink"])
    e.rectangle((203, 162, 210, 171), fill=P["warm"])
    e.rectangle((205, 164, 208, 169), fill=P["warm_light"])

    # Foreground only clips edges, preserving the route and all interaction points.
    f.polygon(((0, 0), (50, 0), (80, 18), (64, 31), (0, 22)), fill=P["ink"])
    for base_x, base_y in ((450, 0), (462, 20), (16, 232)):
        f.line((base_x, base_y, base_x - 16, base_y + 52), fill=P["jade_dark"], width=4)
        for leaf in range(5):
            y = base_y + 12 + leaf * 8
            f.polygon(((base_x - 15, y), (base_x - 42, y + 8),
                       (base_x - 16, y + 13)), fill=P["jade"])
            f.polygon(((base_x - 17, y + 3), (base_x + 8, y + 10),
                       (base_x - 12, y + 15)), fill=P["jade_dark"])
    return {"ground": ground, "environment": env, "foreground": foreground}


def _draw_inn():
    ground = _new()
    env = _new(True)
    foreground = _new(True)
    g = ImageDraw.Draw(ground)
    e = ImageDraw.Draw(env)
    f = ImageDraw.Draw(foreground)

    g.rectangle((0, 0, 479, 269), fill=P["wood_dark"])
    # Door-to-counter walkway is stone, so it is unmistakably walkable.
    g.rectangle((178, 118, 302, 269), fill=P["ink"])
    g.rectangle((183, 122, 297, 269), fill=P["stone"])
    for y in range(128, 270, 16):
        g.line((185, y, 295, y), fill=P["stone_light"], width=1)
        for x in range(185 + ((y // 16) % 2) * 18, 296, 36):
            g.line((x, y - 12, x, y), fill=P["slate"], width=1)
    for y in range(4, 270, 16):
        g.line((4, y, 476, y), fill=P["wood"], width=1)
    for x in range(12, 480, 32):
        g.line((x, 0, x, 269), fill=P["wood"], width=1)

    # Counter: dominant warm focus, but a clear front side for the keeper and player.
    e.rectangle((104, 82, 376, 126), fill=P["ink"])
    e.rectangle((109, 87, 371, 112), fill=P["wood_light"])
    e.rectangle((109, 113, 371, 124), fill=P["wood"])
    for x in range(122, 367, 28):
        e.line((x, 90, x, 123), fill=P["wood_dark"], width=2)
    e.rectangle((228, 34, 252, 76), fill=P["ink"])
    e.rectangle((232, 39, 248, 70), fill=P["vermilion"])
    e.rectangle((235, 43, 245, 64), fill=P["warm"])
    e.rectangle((238, 46, 242, 60), fill=P["warm_light"])
    # Kitchen is a dark large silhouette with an isolated fire glow.
    e.rectangle((20, 54, 145, 162), fill=P["ink"])
    e.rectangle((26, 61, 139, 157), fill=P["wood"])
    e.rectangle((35, 82, 126, 150), fill=P["wood_dark"])
    e.rectangle((53, 112, 95, 150), fill=P["ink"])
    e.rectangle((60, 120, 88, 148), fill=P["vermilion_dark"])
    e.rectangle((66, 128, 82, 148), fill=P["warm"])
    e.rectangle((70, 134, 78, 146), fill=P["warm_light"])
    # Stairs make a right-side vertical destination without covering the corridor.
    e.rectangle((368, 46, 463, 154), fill=P["ink"])
    for step in range(7):
        y = 64 + step * 12
        e.rectangle((377 + step * 4, y, 452, y + 8), fill=P["wood"])
        e.line((377 + step * 4, y, 452, y), fill=P["wood_light"], width=1)
    # Tables are compact side islands, not a repeated floor texture.
    for x, y in ((332, 180), (358, 222)):
        e.rectangle((x - 33, y - 14, x + 33, y + 14), fill=P["ink"])
        e.rectangle((x - 29, y - 10, x + 29, y + 10), fill=P["wood"])
        e.rectangle((x - 23, y - 7, x + 23, y - 5), fill=P["wood_light"])
        e.rectangle((x - 18, y + 12, x - 12, y + 20), fill=P["wood_dark"])
        e.rectangle((x + 12, y + 12, x + 18, y + 20), fill=P["wood_dark"])
    # Door and rug at south edge correspond to the real return trigger.
    e.rectangle((217, 232, 263, 269), fill=P["ink"])
    e.rectangle((224, 236, 256, 269), fill=P["wood"])
    e.rectangle((190, 237, 290, 266), fill=P["vermilion_dark"])
    e.rectangle((198, 242, 282, 266), outline=P["vermilion"], width=2)

    # Curtain/beam foreground creates depth but never obscures doorway or counter front.
    f.rectangle((0, 0, 18, 269), fill=P["ink"])
    f.rectangle((462, 0, 479, 269), fill=P["ink"])
    f.rectangle((18, 0, 462, 13), fill=P["ink"])
    for x in (76, 410):
        f.rectangle((x, 0, x + 9, 72), fill=P["wood_dark"])
        f.rectangle((x + 2, 0, x + 6, 72), fill=P["wood_light"])
    return {"ground": ground, "environment": env, "foreground": foreground}


def _draw_actor(kind):
    image = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    if kind == "lost_pouch":
        draw.rectangle((8, 11, 23, 28), fill=P["ink"])
        draw.rectangle((10, 13, 21, 27), fill=P["wood"])
        draw.rectangle((11, 15, 20, 24), fill=P["wood_light"])
        draw.line((8, 12, 23, 12), fill=P["vermilion"], width=2)
        draw.rectangle((14, 6, 17, 13), fill=P["ink"])
        draw.rectangle((15, 7, 16, 11), fill=P["paper"])
        return image
    # All actors share a grounded 32px silhouette and the same ink outline.
    draw.rectangle((5, 30, 26, 31), fill=P["ink"])
    if kind == "innkeeper":
        draw.rectangle((8, 24, 14, 30), fill=P["ink"])
        draw.rectangle((18, 24, 24, 30), fill=P["ink"])
        draw.rectangle((5, 15, 27, 26), fill=P["ink"])
        draw.rectangle((7, 16, 25, 25), fill=P["wood"])
        draw.rectangle((9, 17, 12, 24), fill=P["wood_light"])
        draw.rectangle((19, 18, 24, 24), fill=P["cloth"])
        draw.rectangle((9, 5, 23, 16), fill=P["ink"])
        draw.rectangle((11, 7, 21, 15), fill=(189, 138, 99, 255))
        draw.rectangle((8, 4, 24, 9), fill=P["slate"])
        draw.rectangle((10, 3, 22, 6), fill=P["ink"])
        draw.rectangle((11, 13, 13, 14), fill=P["ink"])
        draw.rectangle((19, 13, 21, 14), fill=P["ink"])
    else:
        accent = P["vermilion"] if kind == "bandit_a" else P["slate"]
        draw.rectangle((7, 24, 13, 30), fill=P["ink"])
        draw.rectangle((19, 24, 25, 30), fill=P["ink"])
        draw.rectangle((4, 15, 28, 26), fill=P["ink"])
        draw.rectangle((6, 16, 26, 25), fill=accent)
        draw.rectangle((8, 18, 24, 25), fill=P["vermilion_dark"] if kind == "bandit_a" else P["jade_dark"])
        draw.rectangle((5, 18, 8, 23), fill=P["ink"])
        draw.rectangle((24, 17, 28, 22), fill=P["ink"])
        draw.line((25, 20, 31, 13), fill=P["ink"], width=2)
        draw.line((26, 19, 30, 14), fill=P["paper"], width=1)
        draw.rectangle((8, 5, 24, 16), fill=P["ink"])
        draw.rectangle((10, 8, 22, 15), fill=(176, 112, 83, 255))
        draw.rectangle((7, 4, 25, 9), fill=P["ink"])
        draw.rectangle((9, 5, 23, 7), fill=accent)
        draw.rectangle((11, 12, 13, 13), fill=P["ink"])
        draw.rectangle((19, 12, 21, 13), fill=P["ink"])
    return image


def _save_if_changed(image, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        with Image.open(path) as existing:
            if existing.convert("RGBA").tobytes() == image.tobytes():
                return False
    image.save(path, format="PNG", optimize=False)
    return True


def build_mvp_scene_art(source_directory, output_directory, actor_source_directory, resource_directory):
    """Write all native-resolution scene layers and static MVP actor sprites."""
    outputs, _ = _build_mvp_scene_art(
        source_directory, output_directory, actor_source_directory, resource_directory)
    return outputs


def build_mvp_scene_art_with_change_count(
        source_directory, output_directory, actor_source_directory, resource_directory):
    """Build the MVP slice and return its output paths plus baked-file changes."""
    return _build_mvp_scene_art(
        source_directory, output_directory, actor_source_directory, resource_directory)


def _build_mvp_scene_art(source_directory, output_directory, actor_source_directory, resource_directory):
    """Internal implementation shared by direct and aggregate pipeline builds."""
    source_directory = Path(source_directory)
    output_directory = Path(output_directory)
    actor_source_directory = Path(actor_source_directory)
    resource_directory = Path(resource_directory)
    scenes = {"yanliu": _draw_yanliu(), "inn": _draw_inn()}
    written = []
    changed = 0
    for scene_id, layers in scenes.items():
        for layer_name, image in layers.items():
            filename = "mvp_{}_{}_v2.png".format(scene_id, layer_name)
            _save_if_changed(image, source_directory / filename)
            changed += int(_save_if_changed(image, output_directory / filename))
            written.append(output_directory / filename)
    for actor_id in ("innkeeper", "bandit_a", "bandit_b", "lost_pouch"):
        image = _draw_actor(actor_id)
        filename = "mvp_{}.png".format(actor_id)
        _save_if_changed(image, actor_source_directory / filename)
        changed += int(_save_if_changed(image, resource_directory / filename))
        written.append(resource_directory / filename)
    return tuple(written), changed


def main():
    root = Path(__file__).resolve().parents[2]
    for path in build_mvp_scene_art(
            root / "Assets/ArtSource/Environment/MVP/v2",
            root / "Assets/Art/Environment/MVP/v2",
            root / "Assets/ArtSource/Characters/MVP",
            root / "Assets/Resources/Art/MVP"):
        print("built={}".format(path))


if __name__ == "__main__":
    main()
