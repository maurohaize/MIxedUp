"""Draws the hand-made UI kit: paper cards, wooden sign planks, brush sliders, swatches, halftone, tape...
Everything is procedurally "hand drawn": wobbly outlines, double sketch strokes, paper grain.
It also crops the art that was drawn by the game's author in the old Mixed_Up prototype (logo, bar, game over / win drawings).

Run:  python Tools/generate_ui_art.py [path to the Mixed_Up project]
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "MixedUp", "Assets", "_Project", "Art", "UI", "Hand")
OLD = sys.argv[1] if len(sys.argv) > 1 else r"C:\Users\mauro\Mixed_Up"
os.makedirs(OUT, exist_ok=True)

SS = 3  # supersampling factor

INK = (43, 29, 18)
BRUSH = (90, 61, 18)
PAPER = (251, 241, 220)
PLANK = (201, 157, 109)
PLANK_DARK = (150, 108, 66)
POST = (128, 88, 52)
BRICK = (184, 115, 85)
SAND = (255, 210, 143)


# ---------------------------------------------------------------- noise helpers

def noise1d(n, cells, amp, seed, closed=True):
    """Smooth 1D noise with n samples; wraps seamlessly when closed."""
    r = np.random.default_rng(seed)
    pts = r.uniform(-1, 1, cells + 3)
    if closed:
        pts[-3:] = pts[:3]
    x = np.linspace(0, cells, n, endpoint=False)
    i = np.floor(x).astype(int)
    t = x - i
    p0, p1, p2, p3 = pts[i], pts[i + 1], pts[i + 2], pts[i + 3]
    v = 0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t ** 2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3)
    return v * amp


def resample_closed(points, step):
    pts = np.array(points, dtype=float)
    closed = np.vstack([pts, pts[0]])
    seg = np.hypot(*(closed[1:] - closed[:-1]).T)
    cum = np.concatenate([[0], np.cumsum(seg)])
    n = max(8, int(cum[-1] / step))
    s = np.linspace(0, cum[-1], n, endpoint=False)
    x = np.interp(s, cum, closed[:, 0])
    y = np.interp(s, cum, closed[:, 1])
    return np.stack([x, y], axis=1)


def chaikin(points, iterations=2):
    pts = [np.array(p, dtype=float) for p in points]
    for _ in range(iterations):
        out = []
        for i in range(len(pts)):
            a, b = pts[i], pts[(i + 1) % len(pts)]
            out.append(a * 0.75 + b * 0.25)
            out.append(a * 0.25 + b * 0.75)
        pts = out
    return pts


def wobble(points, amp, cells, seed, step=3.0):
    """Resamples a closed outline and pushes it along its normals with smooth noise."""
    pts = resample_closed(points, step)
    n = len(pts)
    tangent = np.roll(pts, -1, axis=0) - np.roll(pts, 1, axis=0)
    tangent /= np.maximum(np.hypot(tangent[:, 0], tangent[:, 1]), 1e-6)[:, None]
    normal = np.stack([-tangent[:, 1], tangent[:, 0]], axis=1)
    d = noise1d(n, max(3, cells), amp, seed) + noise1d(n, max(6, cells * 3), amp * 0.35, seed + 91)
    return pts + normal * d[:, None]


def rounded_rect(x0, y0, x1, y1, r, steps=10):
    pts = []
    for cx, cy, a0 in ((x1 - r, y0 + r, -90), (x1 - r, y1 - r, 0), (x0 + r, y1 - r, 90), (x0 + r, y0 + r, 180)):
        for k in range(steps + 1):
            a = math.radians(a0 + 90 * k / steps)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


def rounded_polygon(points, radii, samples=7):
    """Polygon whose corners are rounded with quadratic curves; radii may be one number or one per vertex."""
    n = len(points)
    if not isinstance(radii, (list, tuple)):
        radii = [radii] * n
    out = []
    for i in range(n):
        p0 = np.array(points[i - 1], dtype=float)
        p1 = np.array(points[i], dtype=float)
        p2 = np.array(points[(i + 1) % n], dtype=float)
        v1, v2 = p0 - p1, p2 - p1
        l1, l2 = np.hypot(*v1), np.hypot(*v2)
        d1, d2 = min(radii[i], l1 / 2), min(radii[i], l2 / 2)
        a = p1 + v1 / l1 * d1
        b = p1 + v2 / l2 * d2
        for k in range(samples + 1):
            t = k / samples
            out.append(((1 - t) ** 2) * a + 2 * (1 - t) * t * p1 + (t ** 2) * b)
    return [tuple(p) for p in out]


def rounded_rect_var(x0, y0, x1, y1, radii):
    """radii: top-left, top-right, bottom-right, bottom-left."""
    return rounded_polygon([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], list(radii), samples=10)


def ellipse_path(cx, cy, rx, ry, n=64):
    return [(cx + rx * math.cos(2 * math.pi * k / n), cy + ry * math.sin(2 * math.pi * k / n)) for k in range(n)]


# ----------------------------------------------------------------- drawing

def new_layer(w, h):
    return Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))


def scaled(points):
    return [(float(x) * SS, float(y) * SS) for x, y in points]


def stroke(draw, pts, width, color, seed=0, jitter=0.25, closed=True, start=0.0, end=1.0):
    """Pencil-like stroke through pts (supersampled coordinates), width varies slowly along the path."""
    pts = np.asarray(pts, dtype=float)
    n = len(pts)
    if closed:
        w = width * (1 + noise1d(n, max(3, n // 40), jitter, seed))
    else:
        w = width * (1 + noise1d(n, max(3, n // 40), jitter, seed, closed=False))
    lo, hi = int(n * start), int(n * end)
    for i in range(lo, hi):
        x, y = pts[i]
        r = max(0.5, w[i] / 2)
        draw.ellipse([x - r, y - r, x + r, y + r], fill=color)


def sketch_outline(layer, path, width, color=INK, seed=1):
    """A bold stroke plus a thin, slightly offset second pass over part of the shape: the 'redrawn' look of a sketch."""
    draw = ImageDraw.Draw(layer)
    main = wobble(path, 1.2 * SS, 5, seed, step=2.0 * SS)
    stroke(draw, main, width * SS, color, seed, closed=True)
    second = wobble(path, 2.4 * SS, 4, seed + 5, step=2.0 * SS)
    offset = np.array([0.8 * SS, 0.6 * SS])
    stroke(draw, second + offset, width * 0.4 * SS, color, seed + 2, jitter=0.5, closed=False, start=0.05, end=0.62)


def finish(layer, name, size=None):
    img = layer.resize((layer.width // SS, layer.height // SS), Image.LANCZOS)
    img.save(os.path.join(OUT, name))
    print("wrote", name, img.size)
    return img


def texture_noise(w, h, seed, strength):
    r = np.random.default_rng(seed)
    n = r.normal(0, 1, (h, w))
    img = Image.fromarray(((n - n.min()) / (n.max() - n.min()) * 255).astype(np.uint8))
    img = img.filter(ImageFilter.GaussianBlur(0.8))
    a = np.asarray(img).astype(float) / 255 - 0.5
    return a * strength


def fibres(w, h, seed, count, length, colour, alpha):
    r = np.random.default_rng(seed)
    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for _ in range(count):
        x, y = r.uniform(0, w), r.uniform(0, h)
        a = r.uniform(0, math.pi)
        l = r.uniform(length * 0.3, length)
        d.line([(x, y), (x + math.cos(a) * l, y + math.sin(a) * l)], fill=colour + (int(alpha * r.uniform(0.5, 1.0)),), width=1)
    return layer


def fill_with(layer_mask, base, w, h, seed, grain=10, edge_shade=0.12):
    """Colours a mask with a base colour, paper grain and a darker rim (inner shadow)."""
    mask = layer_mask.convert("L")
    small = mask.resize((w, h), Image.LANCZOS)
    arr = np.asarray(small).astype(float) / 255
    blur = np.asarray(small.filter(ImageFilter.GaussianBlur(max(6, min(w, h) * 0.05)))).astype(float) / 255
    shade = 1 - edge_shade * np.clip(1 - blur, 0, 1) ** 1.5 * 2.0
    noise = texture_noise(w, h, seed, grain)
    rgb = np.zeros((h, w, 3))
    for k in range(3):
        rgb[..., k] = np.clip(base[k] * shade + noise, 0, 255)
    out = np.dstack([rgb, arr * 255]).astype(np.uint8)
    return Image.fromarray(out, "RGBA")


# ------------------------------------------------------------------ sprites

def paper(name="paper.png", size=512, margin=12, seed=3):
    w = h = size
    # uneven corners and a few corners nudged out of square: it should look cut by hand
    path = rounded_polygon([(margin + 3, margin + 6), (w - margin - 5, margin), (w - margin + 1, h - margin - 8), (margin - 2, h - margin + 1)],
                           [44, 30, 52, 26], samples=12)
    path = [(x * SS, y * SS) for x, y in path]
    outline_pts = wobble(path, 4.2 * SS, 5, seed, step=3.0 * SS)

    # soft drop shadow so the card floats above whatever is behind it
    shadow = Image.new("L", (w * SS, h * SS), 0)
    ImageDraw.Draw(shadow).polygon([(x + 4 * SS, y + 6 * SS) for x, y in outline_pts], fill=110)
    shadow = shadow.filter(ImageFilter.GaussianBlur(4 * SS))

    mask = Image.new("L", (w * SS, h * SS), 0)
    ImageDraw.Draw(mask).polygon([tuple(p) for p in outline_pts], fill=255)
    colour = fill_with(mask.resize((w, h), Image.LANCZOS), PAPER, w, h, seed, grain=7)
    fib = fibres(w, h, seed, 280, 18, (170, 140, 100), 40)
    fib.putalpha(ImageChops.multiply(fib.getchannel("A"), colour.getchannel("A")))
    colour.alpha_composite(fib)

    big = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
    shade_layer = Image.new("RGBA", big.size, (60, 40, 20, 0))
    shade_layer.putalpha(shadow)
    big.alpha_composite(shade_layer)
    big.alpha_composite(colour.resize((w * SS, h * SS), Image.LANCZOS))

    draw = ImageDraw.Draw(big)
    stroke(draw, outline_pts, 5.6 * SS, INK, seed, closed=True, jitter=0.35)
    second = wobble(path, 5.0 * SS, 4, seed + 7, step=3.0 * SS) + np.array([2.2 * SS, 1.6 * SS])
    stroke(draw, second, 2.2 * SS, INK, seed + 3, jitter=0.6, closed=False, start=0.08, end=0.5)
    third = wobble(path, 5.0 * SS, 4, seed + 13, step=3.0 * SS) + np.array([-1.8 * SS, 1.4 * SS])
    stroke(draw, third, 1.6 * SS, INK, seed + 9, jitter=0.6, closed=False, start=0.6, end=0.9)
    finish(big, name)


def plank(name, w, h, arrow, seed, mirror=False):
    """Wooden sign: a plank with an arrow tip on the right (or a flat end when arrow == 0)."""
    m = 14
    tip = arrow
    if tip:
        pts = [(m + 4, m + 10), (w - m - tip, m + 2), (w - m, h / 2 + 2), (w - m - tip - 3, h - m - 1), (m, h - m - 8)]
        radii = [10, 8, 5, 8, 10]
    else:
        pts = [(m + 5, m + 6), (w - m - 2, m + 1), (w - m + 1, h - m - 7), (m, h - m)]
        radii = [10, 10, 10, 10]
    soft = rounded_polygon(pts, radii, samples=8)
    outline = wobble(soft, 2.4, 6, seed, step=3.0)
    big_pts = [(x * SS, y * SS) for x, y in outline]

    mask = Image.new("L", (w * SS, h * SS), 0)
    ImageDraw.Draw(mask).polygon(big_pts, fill=255)
    base = fill_with(mask.resize((w, h), Image.LANCZOS), PLANK, w, h, seed, grain=9, edge_shade=0.16)

    # wood grain: long wavy darker lines
    grain = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
    gd = ImageDraw.Draw(grain)
    r = np.random.default_rng(seed)
    for k in range(9):
        y0 = (m + 10 + (h - 2 * m - 20) * (k + r.uniform(0.1, 0.9)) / 9) * SS
        xs = np.linspace(m * SS, (w - m - tip * 0.5) * SS, 90)
        ys = y0 + noise1d(len(xs), 6, 3.5 * SS, seed + k, closed=False)
        a = int(r.uniform(35, 80))
        gd.line(list(zip(xs, ys)), fill=PLANK_DARK + (a,), width=int(r.uniform(1.2, 2.6) * SS), joint="curve")
    # knot
    kx, ky = r.uniform(w * 0.2, w * 0.6) * SS, r.uniform(h * 0.3, h * 0.7) * SS
    for rad, a in ((13, 60), (8, 90), (4, 120)):
        gd.ellipse([kx - rad * SS, ky - rad * 0.6 * SS, kx + rad * SS, ky + rad * 0.6 * SS], outline=PLANK_DARK + (a,), width=int(1.6 * SS))
    grain = grain.resize((w, h), Image.LANCZOS)
    grain.putalpha(ImageChops.multiply(grain.getchannel("A"), base.getchannel("A")))
    base.alpha_composite(grain)

    big = base.resize((w * SS, h * SS), Image.LANCZOS)
    draw = ImageDraw.Draw(big)
    stroke(draw, np.array(big_pts), 5.0 * SS, INK, seed, closed=True)
    second = np.array(big_pts) * 1.0 + np.array([1.2 * SS, 1.4 * SS])
    stroke(draw, second, 1.9 * SS, INK, seed + 3, jitter=0.5, closed=False, start=0.15, end=0.7)
    if mirror:
        big = ImageOps_mirror(big)
    finish(big, name)


def ImageOps_mirror(img):
    return img.transpose(Image.FLIP_LEFT_RIGHT)


def post(name="post.png", w=128, h=1024, seed=21):
    m = 26
    pts = [(m, 12), (w - m, 6), (w - m - 3, h - 8), (m + 2, h - 12)]
    soft = rounded_polygon(pts, 12, samples=8)
    outline = wobble(soft, 2.6, 9, seed, step=3.0)
    big_pts = [(x * SS, y * SS) for x, y in outline]
    mask = Image.new("L", (w * SS, h * SS), 0)
    ImageDraw.Draw(mask).polygon(big_pts, fill=255)
    base = fill_with(mask.resize((w, h), Image.LANCZOS), POST, w, h, seed, grain=9, edge_shade=0.2)

    grain = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
    gd = ImageDraw.Draw(grain)
    r = np.random.default_rng(seed)
    for k in range(7):
        x0 = (m + 8 + (w - 2 * m - 16) * (k + r.uniform(0.2, 0.8)) / 7) * SS
        ys = np.linspace(14 * SS, (h - 14) * SS, 220)
        xs = x0 + noise1d(len(ys), 10, 2.4 * SS, seed + k, closed=False)
        gd.line(list(zip(xs, ys)), fill=(70, 44, 22, int(r.uniform(60, 120))), width=int(r.uniform(1.5, 3) * SS), joint="curve")
    grain = grain.resize((w, h), Image.LANCZOS)
    grain.putalpha(ImageChops.multiply(grain.getchannel("A"), base.getchannel("A")))
    base.alpha_composite(grain)

    big = base.resize((w * SS, h * SS), Image.LANCZOS)
    draw = ImageDraw.Draw(big)
    stroke(draw, np.array(big_pts), 5.0 * SS, INK, seed, closed=True)
    finish(big, name)


def brush_line(name, w, h, colour, seed, taper=False, rough=1.0):
    """A horizontal brush stroke with ragged edges."""
    layer = new_layer(w, h)
    d = ImageDraw.Draw(layer)
    xs = np.linspace(h * 0.4, w - h * 0.4, 400)
    cy = h / 2
    r = np.random.default_rng(seed)
    thick = noise1d(len(xs), 14, h * 0.07 * rough, seed, closed=False)
    mid = noise1d(len(xs), 8, h * 0.05 * rough, seed + 1, closed=False)
    for i, x in enumerate(xs):
        t = i / (len(xs) - 1)
        th = h * 0.36 + thick[i]
        if taper:
            th *= 0.35 + 0.65 * math.sin(math.pi * min(1, t * 1.05 + 0.02))
        y = cy + mid[i]
        d.ellipse([(x - th * 0.9) * SS, (y - th) * SS, (x + th * 0.9) * SS, (y + th) * SS], fill=colour + (255,))
    # dry-brush streaks
    for _ in range(int(w / 6)):
        x = r.uniform(h * 0.4, w - h * 0.4)
        y = cy + r.uniform(-h * 0.3, h * 0.3)
        d.line([(x * SS, y * SS), ((x + r.uniform(12, 40)) * SS, (y + r.uniform(-0.6, 0.6)) * SS)], fill=(0, 0, 0, 0), width=int(r.uniform(1, 2.2) * SS))
    return finish(layer, name)


def knob(name="slider_knob.png", size=72, seed=5):
    layer = new_layer(size, size)
    path = ellipse_path(size / 2, size / 2, size / 2 - 8, size / 2 - 8)
    outline = wobble(path, 1.4, 4, seed, step=1.5)
    d = ImageDraw.Draw(layer)
    d.polygon([(x * SS, y * SS) for x, y in outline], fill=PAPER + (255,))
    # soft shading on the lower right
    shade = new_layer(size, size)
    sd = ImageDraw.Draw(shade)
    sd.ellipse([(size * 0.38) * SS, (size * 0.38) * SS, (size - 10) * SS, (size - 10) * SS], fill=(190, 150, 100, 70))
    mask = Image.new("L", layer.size, 0)
    ImageDraw.Draw(mask).polygon([(x * SS, y * SS) for x, y in outline], fill=255)
    shade.putalpha(ImageChops.multiply(shade.getchannel("A"), mask))
    layer.alpha_composite(shade)
    stroke(d, np.array([(x * SS, y * SS) for x, y in outline]), 5.0 * SS, INK, seed, closed=True)
    d.ellipse([size / 2 * SS - 5 * SS, size / 2 * SS - 5 * SS, size / 2 * SS + 5 * SS, size / 2 * SS + 5 * SS], fill=BRICK + (255,))
    finish(layer, name)


def swatch(name="swatch.png", size=112, seed=9):
    layer = new_layer(size, size)
    path = ellipse_path(size / 2, size / 2, size / 2 - 10, size / 2 - 10)
    outline = [(x * SS, y * SS) for x, y in wobble(path, 1.6, 5, seed, step=1.5)]
    d = ImageDraw.Draw(layer)
    d.polygon(outline, fill=(255, 255, 255, 255))
    # a lighter blob and a darker crescent give the flat colour some hand-painted body
    hl = new_layer(size, size)
    hd = ImageDraw.Draw(hl)
    hd.ellipse([size * 0.26 * SS, size * 0.2 * SS, size * 0.5 * SS, size * 0.4 * SS], fill=(255, 255, 255, 0))
    mask = Image.new("L", layer.size, 0)
    ImageDraw.Draw(mask).polygon(outline, fill=255)
    crescent = new_layer(size, size)
    cd = ImageDraw.Draw(crescent)
    cd.ellipse([size * 0.18 * SS, size * 0.1 * SS, size * 0.95 * SS, size * 0.92 * SS], fill=(0, 0, 0, 40))
    cd.ellipse([size * 0.1 * SS, size * 0.02 * SS, size * 0.85 * SS, size * 0.82 * SS], fill=(0, 0, 0, 0))
    crescent.putalpha(ImageChops.multiply(crescent.getchannel("A"), mask))
    layer.alpha_composite(crescent)
    stroke(d, np.array(outline), 5.0 * SS, INK, seed, closed=True)
    finish(layer, name)


def ring(name="swatch_ring.png", size=144, seed=14):
    layer = new_layer(size, size)
    d = ImageDraw.Draw(layer)
    for k, (rad, wd) in enumerate(((size / 2 - 9, 6.0), (size / 2 - 14, 3.0))):
        path = ellipse_path(size / 2, size / 2, rad, rad * 0.97, n=140)
        pts = np.array([(x * SS, y * SS) for x, y in wobble(path, 2.2, 5, seed + k * 3, step=1.5)])
        stroke(d, pts, wd * SS, INK if k == 0 else BRICK, seed + k, closed=False, start=0.0, end=0.97 - 0.3 * k)
    finish(layer, name)


def checkbox(seed=31):
    size = 72
    layer = new_layer(size, size)
    path = rounded_rect(9, 9, size - 9, size - 9, 10)
    outline = [(x * SS, y * SS) for x, y in wobble(path, 1.6, 5, seed, step=1.5)]
    d = ImageDraw.Draw(layer)
    d.polygon(outline, fill=PAPER + (255,))
    stroke(d, np.array(outline), 5.0 * SS, INK, seed, closed=True)
    finish(layer, "checkbox.png")

    layer = new_layer(size, size)
    d = ImageDraw.Draw(layer)
    pts = np.array([(18, 38), (30, 52), (56, 18)], dtype=float)
    dense = []
    for a, b in zip(pts[:-1], pts[1:]):
        for t in np.linspace(0, 1, 40, endpoint=False):
            dense.append(a * (1 - t) + b * t)
    dense.append(pts[-1])
    dense = np.array(dense) * SS
    dense += np.stack([noise1d(len(dense), 6, 1.5 * SS, seed, closed=False), noise1d(len(dense), 6, 1.5 * SS, seed + 2, closed=False)], axis=1)
    stroke(d, dense, 9.0 * SS, BRICK, seed, closed=False)
    finish(layer, "checkmark.png")


def highlight(name="highlight.png", size=256, seed=40):
    layer = new_layer(size, size)
    d = ImageDraw.Draw(layer)
    path = rounded_rect(14, 14, size - 14, size - 14, 38)
    for k, (amp, wd, col) in enumerate(((3.0, 13.0, (255, 200, 58)), (4.5, 6.0, (255, 160, 40)))):
        pts = np.array([(x * SS, y * SS) for x, y in wobble(path, amp, 6, seed + k * 4, step=2.0)])
        stroke(d, pts, wd * SS, col, seed + k, closed=False, start=0.0, end=1.0 - 0.25 * k)
    finish(layer, name)


def tape(name="tape.png", w=200, h=64, seed=50):
    layer = new_layer(w, h)
    d = ImageDraw.Draw(layer)
    r = np.random.default_rng(seed)
    top, bottom = [], []
    n = 7
    for i in range(n):
        top.append((18 + r.uniform(-4, 4) + (0 if i % 2 else 5), 6 + (h - 12) * i / (n - 1)))
        bottom.append((w - 18 + r.uniform(-4, 4) - (5 if i % 2 else 0), 6 + (h - 12) * i / (n - 1)))
    poly = [(x * SS, y * SS) for x, y in top + bottom[::-1]]
    d.polygon(poly, fill=(238, 214, 150, 190))
    for _ in range(30):
        x = r.uniform(24, w - 24) * SS
        y = r.uniform(10, h - 10) * SS
        d.line([(x, y), (x + r.uniform(10, 30) * SS, y)], fill=(255, 245, 210, 90), width=SS)
    img = finish(layer, name)


def halftone(name="halftone.png", size=512):
    s = size * 2
    yy, xx = np.mgrid[0:s, 0:s].astype(float)
    cx = cy = s / 2
    dist = np.hypot(xx - cx, yy - cy) / (s * 0.72)
    base = 150 + (1 - np.clip(dist, 0, 1)) * 40
    # rotated dot grid
    ang = math.radians(35)
    u = (xx * math.cos(ang) + yy * math.sin(ang)) / 18
    v = (-xx * math.sin(ang) + yy * math.cos(ang)) / 18
    fu, fv = u - np.round(u), v - np.round(v)
    d = np.hypot(fu, fv)
    radius = 0.14 + 0.38 * np.clip(dist, 0, 1) ** 1.3
    dots = (d < radius).astype(float)
    rgb = np.where(dots > 0, 24, base)
    img = Image.fromarray(np.clip(rgb, 0, 255).astype(np.uint8), "L").resize((size, size), Image.LANCZOS).convert("RGB")
    img.save(os.path.join(OUT, name))
    print("wrote", name)


def sketch_frame(name="sketch_frame.png", w=1024, h=576, seed=60):
    """Faint pencil hatching in the corners and a darker rim: laid over the 3D main-menu scene."""
    r = np.random.default_rng(seed)
    layer = new_layer(w, h)
    d = ImageDraw.Draw(layer)
    for corner in ((0, 0), (w, 0), (0, h), (w, h)):
        for _ in range(70):
            dist = abs(r.normal(0, 0.11))
            ox = (corner[0] + (1 if corner[0] == 0 else -1) * r.uniform(0, w * 0.2) * (1 - dist * 2))
            oy = (corner[1] + (1 if corner[1] == 0 else -1) * r.uniform(0, h * 0.28) * (1 - dist * 2))
            ang = math.radians(r.uniform(-50, -25) if (corner[0] == 0) == (corner[1] == 0) else r.uniform(25, 50))
            l = r.uniform(30, 120)
            d.line([(ox * SS, oy * SS), ((ox + math.cos(ang) * l) * SS, (oy + math.sin(ang) * l) * SS)],
                   fill=(40, 28, 18, int(r.uniform(35, 90))), width=int(r.uniform(1, 2.4) * SS))
    for side in range(4):
        for _ in range(26):
            t = r.uniform(0.04, 0.96)
            l = r.uniform(40, 160)
            if side < 2:
                x, y = t * w, (r.uniform(2, 14) if side == 0 else h - r.uniform(2, 14))
                d.line([(x * SS, y * SS), ((x + l) * SS, (y + r.uniform(-2, 2)) * SS)], fill=(40, 28, 18, 70), width=int(1.6 * SS))
            else:
                x, y = (r.uniform(2, 14) if side == 2 else w - r.uniform(2, 14)), t * h
                d.line([(x * SS, y * SS), ((x + r.uniform(-2, 2)) * SS, (y + l * 0.6) * SS)], fill=(40, 28, 18, 70), width=int(1.6 * SS))
    finish(layer, name)


# ------------------------------------------------------ the author's own art

def crop_sand(src, box, name, tolerance=34, clear=()):
    """Crops a region of a drawing on a sand background and turns the background transparent."""
    im = Image.open(os.path.join(OLD, "Assets", "Images", src)).convert("RGBA").crop(box)
    arr = np.asarray(im).astype(float)
    bg = np.array(SAND, dtype=float)
    corner = arr[2, 2, :3]
    if np.abs(corner - bg).sum() < 90:
        bg = corner
    dist = np.sqrt(((arr[..., :3] - bg) ** 2).sum(axis=2))
    alpha = np.clip((dist - tolerance * 0.5) / tolerance, 0, 1)
    arr[..., 3] = np.minimum(arr[..., 3], alpha * 255)
    for x0, y0, x1, y1 in clear:
        arr[y0:y1, x0:x1, 3] = 0
    out = Image.fromarray(arr.astype(np.uint8), "RGBA")
    bbox = out.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
    if bbox:
        out = out.crop(bbox)
    out.save(os.path.join(OUT, name))
    print("wrote", name, out.size)


def copy_trim(src, name):
    im = Image.open(os.path.join(OLD, "Assets", "Images", src)).convert("RGBA")
    bbox = im.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
    if bbox:
        im = im.crop(bbox)
    im.save(os.path.join(OUT, name))
    print("wrote", name, im.size)


def main():
    paper()
    plank("plank_right.png", 640, 168, 92, 11)
    plank("plank_left.png", 640, 168, 92, 11, mirror=True)
    plank("plank_plain.png", 512, 144, 0, 17)
    post()
    brush_line("slider_track.png", 512, 48, BRUSH, 71, rough=1.2)
    brush_line("underline.png", 512, 30, (170, 40, 30), 72, taper=True)
    brush_line("brush_white.png", 512, 48, (255, 255, 255), 73, rough=0.8)
    knob()
    swatch()
    ring()
    checkbox()
    highlight()
    tape()
    halftone()
    sketch_frame()

    if os.path.isdir(OLD):
        copy_trim(os.path.join("ui", "logo.png"), "logo.png")
        copy_trim(os.path.join("ui", "barra 1.png"), "bar_brick.png")
        crop_sand(os.path.join("Menus", "gameover.png"), (440, 20, 1010, 572), "art_gameover.png", clear=[(0, 225, 30, 295)])
        crop_sand(os.path.join("Menus", "win.png"), (385, 40, 1010, 520), "art_win.png", clear=[(0, 0, 118, 280)])
    else:
        print("Mixed_Up project not found, skipping the author's drawings:", OLD)


if __name__ == "__main__":
    main()
