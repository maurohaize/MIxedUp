"""
Hand-drawn full-screen frames for the box effects (transparent centre, drawn in the style of the icy frame that comes
from the old Mixed_Up project): flames licking in from the edges for HOT boxes and dripping green slime for TOXIC ones.

    python Tools/generate_overlay_art.py

The game crops the bottom 30% of each image (EffectHud.OverlayBottomCrop), so everything is drawn in the top 70%.
"""
import math
import os
import random

from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "MixedUp", "Assets", "_Project", "Art", "UI", "Overlays")

W, H = 1026, 572
VISIBLE_H = int(H * 0.7)
SS = 2  # supersampling


def new_layer():
    return Image.new("RGBA", (W * SS, H * SS), (0, 0, 0, 0))


def edge_wash(color, strength, falloff=0.5):
    """A soft coloured haze that is strongest at the screen edges and empty in the middle."""
    img = Image.new("L", (W, VISIBLE_H), 0)
    px = img.load()
    cx, cy = W / 2.0, VISIBLE_H / 2.0
    for y in range(VISIBLE_H):
        for x in range(W):
            dx = abs(x - cx) / cx
            dy = abs(y - cy) / cy
            d = max(dx, dy * 0.9)
            t = max(0.0, (d - falloff) / (1.0 - falloff))
            px[x, y] = int(255 * min(1.0, t * t) * strength)
    full = Image.new("L", (W, H), 0)
    full.paste(img, (0, 0))
    layer = Image.new("RGBA", (W, H), color + (0,))
    layer.putalpha(full)
    return layer.resize((W * SS, H * SS), Image.BILINEAR)


def wobble(points, amount, rnd):
    return [(x + rnd.uniform(-amount, amount), y + rnd.uniform(-amount, amount)) for x, y in points]


def scribble(draw, points, color, width):
    """A slightly shaky pen line, like the dark sketch strokes of the frozen frame."""
    draw.line(points, fill=color, width=width, joint="curve")


def flame_outline(base_x, base_y, height, width, angle, bend, steps=18):
    """Outline of one curled flame tongue: a wide base narrowing to a tip that leans to one side."""
    a = math.radians(angle)
    ux, uy = math.sin(a), -math.cos(a)           # direction the flame grows in
    px, py = -uy, ux                              # sideways
    left, right = [], []
    for i in range(steps + 1):
        t = i / steps
        w = width * 0.5 * (1.0 - t) ** 0.8 * (1.0 + 0.35 * math.sin(t * 7.0))
        spine = bend * height * t ** 2.1
        cx = base_x + ux * height * t + px * spine
        cy = base_y + uy * height * t + py * spine
        left.append((cx - px * w, cy - py * w))
        right.append((cx + px * w, cy + py * w))
    return left + right[::-1]


def flame(draw, base_x, base_y, height, width, angle, rnd, outline, body, core):
    """A cluster: a tall tongue with two smaller ones beside it, plus a bright core."""
    bend = rnd.choice([-1, 1]) * rnd.uniform(0.18, 0.4)
    parts = [(0.0, 1.0, 1.0, bend), (-0.32, 0.62, 0.55, -bend * 0.8), (0.34, 0.5, 0.5, bend * 1.1)]
    for offset, hk, wk, bk in parts:
        a = math.radians(angle)
        px, py = -math.cos(a), -math.sin(a)
        bx, by = base_x + px * width * offset * -1.0, base_y + py * width * offset * -1.0
        pts = wobble(flame_outline(bx, by, height * hk, width * wk, angle, bk), width * 0.012, rnd)
        draw.polygon(pts, fill=body)
        scribble(draw, pts + [pts[0]], outline, max(3, int(width * 0.04)))
    # bright core inside the main tongue
    inner = flame_outline(base_x, base_y, height * 0.62, width * 0.5, angle, bend * 0.8)
    draw.polygon(inner, fill=core)
    tip = flame_outline(base_x, base_y, height * 0.34, width * 0.26, angle, bend * 0.6)
    draw.polygon(tip, fill=(255, 235, 150, 245))


def hot_frame():
    rnd = random.Random(11)
    layer = edge_wash((226, 78, 32), 0.55, 0.42)
    flames = new_layer()
    d = ImageDraw.Draw(flames)
    outline = (70, 28, 12, 255)

    def burn(x, y, height, width, angle):
        shade = rnd.uniform(0.85, 1.0)
        body = (int(235 * shade), int(92 * shade), int(36 * shade), 245)
        core = (255, int(190 * shade), 70, 250)
        flame(d, x * SS, y * SS, height * SS, width * SS, angle, rnd, outline, body, core)

    bottom = VISIBLE_H + 6
    # a wall of fire along the bottom edge, the tallest tongues near the corners
    for i in range(22):
        x = rnd.uniform(-30, W + 30)
        edge_dist = min(x, W - x) / (W / 2.0)
        height = rnd.uniform(70, 130) * (1.5 - 0.8 * edge_dist)
        burn(x, bottom, height, rnd.uniform(70, 120), rnd.uniform(-14, 14))
    # tongues up both sides
    for side in (0, 1):
        for i in range(10):
            y = rnd.uniform(40, VISIBLE_H)
            x = -8 if side == 0 else W + 8
            height = rnd.uniform(90, 190) * (1.0 - 0.35 * (VISIBLE_H - y) / VISIBLE_H)
            burn(x, y, height, rnd.uniform(60, 100), (80 if side == 0 else -80) + rnd.uniform(-16, 16))
    # small ones licking down from the top corners
    for side in (0, 1):
        for i in range(5):
            x = rnd.uniform(0, 190) if side == 0 else W - rnd.uniform(0, 190)
            burn(x, -6, rnd.uniform(60, 120), rnd.uniform(46, 76), 180 + rnd.uniform(-20, 20))

    # embers
    for i in range(46):
        side = rnd.random()
        x = rnd.uniform(0, W) if side < 0.5 else (rnd.uniform(0, 260) if side < 0.75 else W - rnd.uniform(0, 260))
        y = rnd.uniform(40, VISIBLE_H - 40)
        r = rnd.uniform(3, 8)
        d.ellipse([(x - r) * SS, (y - r) * SS, (x + r) * SS, (y + r) * SS], fill=(255, rnd.randint(170, 230), 70, 235))

    layer = Image.alpha_composite(layer, flames)
    return layer.resize((W, H), Image.LANCZOS)


def blob(draw, cx, cy, rx, ry, fill, outline, width, rnd, lobes=9):
    pts = []
    n = 36
    phase = rnd.uniform(0, 6)
    for i in range(n):
        a = i / n * 2 * math.pi
        k = 1.0 + 0.12 * math.sin(a * lobes + phase)
        pts.append((cx + math.cos(a) * rx * k, cy + math.sin(a) * ry * k))
    draw.polygon(pts, fill=fill)
    scribble(draw, pts + [pts[0]], outline, width)


def toxic_frame():
    rnd = random.Random(23)
    layer = edge_wash((110, 170, 40), 0.5, 0.4)
    art = new_layer()
    d = ImageDraw.Draw(art)
    outline = (28, 62, 14, 255)
    slime = (146, 214, 52, 250)
    slime_dark = (96, 168, 40, 250)
    shine = (222, 255, 150, 235)

    # the slime ceiling: a wavy band along the top with drips of different lengths
    band = []
    x = -20
    while x < W + 40:
        band.append((x, 36 + math.sin(x * 0.018) * 14 + rnd.uniform(-5, 5)))
        x += 24
    top_poly = [(-20 * SS, -20 * SS)] + [(px * SS, py * SS) for px, py in band] + [((W + 40) * SS, -20 * SS)]
    d.polygon(top_poly, fill=slime)
    scribble(d, [(px * SS, py * SS) for px, py in band], outline, 7)

    drip_xs = sorted(rnd.uniform(20, W - 20) for _ in range(11))
    for dx in drip_xs:
        length = rnd.uniform(60, 250) * (1.0 + 0.5 * (abs(dx - W / 2) / (W / 2)))
        width = rnd.uniform(24, 44)
        base = 40 + math.sin(dx * 0.018) * 14
        pts = []
        for t in [i / 12 for i in range(13)]:
            w = width * 0.5 * (1.0 - 0.45 * t)
            pts.append(((dx - w) * SS, (base + length * t * 0.92) * SS))
        end_y = base + length
        # round tip
        for k in range(1, 9):
            a = math.pi * k / 9
            pts.append(((dx - math.cos(a) * width * 0.3) * SS, (end_y - 6 + math.sin(a) * width * 0.34) * SS))
        for t in [i / 12 for i in range(12, -1, -1)]:
            w = width * 0.5 * (1.0 - 0.45 * t)
            pts.append(((dx + w) * SS, (base + length * t * 0.92) * SS))
        d.polygon(pts, fill=slime)
        scribble(d, pts, outline, 7)
        d.line([((dx - width * 0.18) * SS, (base + 10) * SS), ((dx - width * 0.18) * SS, (base + length * 0.6) * SS)], fill=shine, width=6)
        # a falling droplet below the longer drips
        if length > 140:
            r = width * 0.2
            yy = end_y + rnd.uniform(14, 40)
            blob(d, dx * SS, yy * SS, r * SS, r * 1.25 * SS, slime, outline, 5, rnd, 3)

    # slime piling up in the bottom corners
    for side in (0, 1):
        for i in range(5):
            cx = rnd.uniform(0, 210) if side == 0 else W - rnd.uniform(0, 210)
            cy = VISIBLE_H - rnd.uniform(0, 36)
            blob(d, cx * SS, cy * SS, rnd.uniform(46, 90) * SS, rnd.uniform(30, 60) * SS, slime_dark, outline, 6, rnd)

    # bubbles rising along the sides
    for i in range(34):
        side = rnd.random()
        x = rnd.uniform(14, 250) if side < 0.5 else W - rnd.uniform(14, 250)
        y = rnd.uniform(120, VISIBLE_H - 30)
        r = rnd.uniform(8, 30) * (1.0 - 0.4 * (y / VISIBLE_H) * 0.0)
        d.ellipse([(x - r) * SS, (y - r) * SS, (x + r) * SS, (y + r) * SS], fill=(190, 240, 90, 70), outline=outline, width=5)
        d.arc([(x - r * 0.6) * SS, (y - r * 0.6) * SS, (x + r * 0.6) * SS, (y + r * 0.6) * SS], 200, 280, fill=shine, width=5)

    # drifting gas wisps (soft, no outline)
    gas = new_layer()
    gd = ImageDraw.Draw(gas)
    for i in range(18):
        side = rnd.random()
        x = rnd.uniform(0, 240) if side < 0.5 else W - rnd.uniform(0, 240)
        y = rnd.uniform(80, VISIBLE_H)
        r = rnd.uniform(40, 90)
        gd.ellipse([(x - r) * SS, (y - r * 0.7) * SS, (x + r) * SS, (y + r * 0.7) * SS], fill=(140, 220, 70, 90))
    gas = gas.filter(ImageFilter.GaussianBlur(14 * SS))

    out = Image.alpha_composite(layer.resize((W * SS, H * SS), Image.BILINEAR), gas)
    out = Image.alpha_composite(out, art)
    return out.resize((W, H), Image.LANCZOS)


def main():
    os.makedirs(OUT, exist_ok=True)
    hot_frame().save(os.path.join(OUT, "overlay_hot_frame.png"))
    toxic_frame().save(os.path.join(OUT, "overlay_toxic_frame.png"))
    print("overlays written to", OUT)


if __name__ == "__main__":
    main()
