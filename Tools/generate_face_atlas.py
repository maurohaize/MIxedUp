"""Draws the character's facial expressions as one atlas (4 x 4 cells, transparent background).

Every cell uses the same angular layout as Tools/generate_character_art.py (x covers -65..+65 degrees of azimuth, y +50 top
.. -50 bottom of the head sphere), so the game can show any expression by moving the texture window. Cell order (row by row,
from the top-left): see EXPRESSIONS below; it must match MixedUp.FaceExpression.

Run:  python Tools/generate_face_atlas.py
"""
import math
import os

from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "MixedUp", "Assets", "_Project", "Art", "Characters")
os.makedirs(OUT, exist_ok=True)

AZ, EL = 130.0, 100.0
PPD = 12
W, H = int(AZ * PPD), int(EL * PPD)
CELL_W, CELL_H = 416, 320
COLS, ROWS = 4, 4

INK = (14, 12, 12, 255)
WHITE = (255, 255, 255, 255)
RED = (222, 58, 78, 255)
BLUE = (96, 168, 232, 255)
GREEN = (118, 172, 58, 255)
PINK = (244, 128, 140, 150)
YELLOW = (255, 214, 64, 255)

EXPRESSIONS = ["neutral", "blink", "happy", "hurt", "dizzy", "scared", "cold", "hot",
               "shocked", "sick", "love", "effort", "dead", "surprised", "worried", "wink"]


def px(az, el):
    return ((az + AZ / 2) * PPD, (EL / 2 - el) * PPD)


def ellipse(draw, az, el, rx, ry, fill):
    cx, cy = px(az, el)
    draw.ellipse([cx - rx * PPD, cy - ry * PPD, cx + rx * PPD, cy + ry * PPD], fill=fill)


def polygon(draw, points, fill):
    draw.polygon([px(a, e) for a, e in points], fill=fill)


def stroke(draw, points, width, fill=INK, steps=30, taper=None):
    pts = [points[0]] + list(points) + [points[-1]]
    path = []
    for i in range(1, len(pts) - 2):
        p0, p1, p2, p3 = pts[i - 1], pts[i], pts[i + 1], pts[i + 2]
        for s in range(steps):
            t = s / steps
            t2, t3 = t * t, t * t * t
            path.append(tuple(
                0.5 * ((2 * p1[k]) + (-p0[k] + p2[k]) * t + (2 * p0[k] - 5 * p1[k] + 4 * p2[k] - p3[k]) * t2
                       + (-p0[k] + 3 * p1[k] - 3 * p2[k] + p3[k]) * t3) for k in (0, 1)))
    path.append(points[-1])
    for i, (az, el) in enumerate(path):
        t = i / max(1, len(path) - 1)
        w = width * (taper(t) if taper else 1.0)
        ellipse(draw, az, el, w / 2, w / 2, fill)


def arc(draw, cx, cy, rx, ry, a0, a1, width, fill=INK, steps=18):
    pts = [(cx + math.cos(math.radians(a0 + (a1 - a0) * i / steps)) * rx, cy + math.sin(math.radians(a0 + (a1 - a0) * i / steps)) * ry)
           for i in range(steps + 1)]
    stroke(draw, pts, width, fill)


EYE_Y = -9.0
EYE_X = 26.0


def open_eye(draw, side, pupil=12.5, look=(-1.5, -0.5), lashes=True, rim=2.0):
    cx, cy = side * EYE_X, EYE_Y
    rx, ry = 17.5, 16.0
    ellipse(draw, cx, cy, rx + rim, ry + rim, INK)
    ellipse(draw, cx, cy, rx, ry, WHITE)
    pcx = cx + side * look[0]
    ellipse(draw, pcx, cy + look[1], pupil, pupil * 1.08, INK)
    if pupil > 6:
        ellipse(draw, pcx - side * 4.0, cy + 5.5, min(4.6, pupil * 0.37), min(4.6, pupil * 0.37), WHITE)
        ellipse(draw, pcx + side * 4.5, cy - 5.0, 2.0, 2.0, WHITE)
    lid = [(cx - side * 17.0, cy + 7.0), (cx - side * 9.0, cy + 15.5), (cx + side * 2.0, cy + 18.0),
           (cx + side * 12.0, cy + 15.5), (cx + side * 19.5, cy + 9.0)]
    stroke(draw, lid, 4.6, taper=lambda t: 0.55 + 0.45 * math.sin(math.pi * min(1.0, t * 1.15)))
    if lashes:
        stroke(draw, [(cx + side * 17.0, cy + 9.0), (cx + side * 22.0, cy + 12.0), (cx + side * 25.5, cy + 10.5)], 3.4, taper=lambda t: 0.9 - 0.7 * t)
        stroke(draw, [(cx + side * 17.5, cy + 3.0), (cx + side * 23.5, cy + 3.8), (cx + side * 27.0, cy + 1.2)], 3.2, taper=lambda t: 0.9 - 0.7 * t)


def closed_eye(draw, side, curve=-1):
    """curve -1: relaxed lid (a smile turned down, like a blink); +1: a happy arch (^)."""
    cx, cy = side * EYE_X, EYE_Y
    if curve > 0:
        arc(draw, cx, cy - 6, 15.5, 13, 200, 340, 5.2)
    else:
        arc(draw, cx, cy + 6, 15.5, 9, 20, 160, 5.2)
    # lashes
    stroke(draw, [(cx + side * 15, cy + (1 if curve < 0 else 3)), (cx + side * 21, cy + 4), (cx + side * 24.5, cy + 2)], 3.0, taper=lambda t: 0.9 - 0.7 * t)


def squeezed_eye(draw, side):
    """The > < of pain."""
    cx, cy = side * EYE_X, EYE_Y
    stroke(draw, [(cx - side * 13, cy + 11), (cx + side * 11, cy), (cx - side * 13, cy - 11)], 5.4)


def x_eye(draw, side):
    cx, cy = side * EYE_X, EYE_Y
    stroke(draw, [(cx - 12, cy + 12), (cx + 12, cy - 12)], 5.2)
    stroke(draw, [(cx - 12, cy - 12), (cx + 12, cy + 12)], 5.2)


def spiral_eye(draw, side):
    cx, cy = side * EYE_X, EYE_Y
    ellipse(draw, cx, cy, 19.5, 18, INK)
    ellipse(draw, cx, cy, 17.5, 16, WHITE)
    pts = []
    for i in range(0, 70):
        t = i / 69
        a = t * 4.6 * math.pi * side
        r = 2 + t * 13
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r * 0.92))
    stroke(draw, pts, 2.8)


def heart_eye(draw, side):
    cx, cy = side * EYE_X, EYE_Y
    ellipse(draw, cx, cy, 19.5, 18, INK)
    ellipse(draw, cx, cy, 17.5, 16, WHITE)
    pts = []
    for i in range(0, 60):
        t = i / 59 * 2 * math.pi
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((cx + x * 0.78, cy + y * 0.8 + 1))
    polygon(draw, pts, RED)
    ellipse(draw, cx - 5, cy + 5, 2.6, 2.6, WHITE)


def star_eye(draw, side):
    cx, cy = side * EYE_X, EYE_Y
    ellipse(draw, cx, cy, 19.5, 18, INK)
    ellipse(draw, cx, cy, 17.5, 16, WHITE)
    pts = []
    for i in range(10):
        r = 15 if i % 2 == 0 else 6
        a = math.radians(90 + i * 36)
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    polygon(draw, pts, YELLOW)
    stroke(draw, pts + [pts[0]], 1.8)


def brow(draw, side, inner=0.0, outer=0.0, lift=0.0, width=5.4):
    """Eyebrow with the inner end raised by `inner` and the outer end by `outer` (degrees up), `lift` raises both."""
    cx, cy = side * EYE_X, EYE_Y
    pts = [(cx - side * 13.0, cy + 22.0 + lift + inner), (cx - side * 3.0, cy + 26.0 + lift + inner * 0.6 + outer * 0.2),
           (cx + side * 9.0, cy + 27.5 + lift + outer * 0.6), (cx + side * 17.0, cy + 24.5 + lift + outer)]
    stroke(draw, pts, width, taper=lambda t: 0.7 + 0.3 * math.sin(math.pi * t))


def mouth_smile(draw, width=10.0, depth=3.0, y=-43.0, thick=1.8):
    stroke(draw, [(-width, y + depth * 0.5), (-width * 0.4, y - depth * 0.9), (width * 0.4, y - depth * 0.9), (width, y + depth * 0.5)], thick)


def mouth_open_d(draw, y=-40.0, w=13.0, h=10.0):
    pts = []
    for i in range(0, 31):
        t = math.pi * i / 30
        pts.append((-w * math.cos(t), y + 4 - h * math.sin(t) * 0.0 - h * (math.sin(t)) * -1 * 0))
    # a D shape: flat top, rounded bottom
    shape = [(-w, y + 3)] + [(-w * math.cos(math.pi * i / 24), y + 3 - h * math.sin(math.pi * i / 24)) for i in range(0, 25)] + [(w, y + 3)]
    polygon(draw, shape, INK)
    tongue = [(-w * 0.55 * math.cos(math.pi * i / 24), y + 2 - h * 0.6 * math.sin(math.pi * i / 24)) for i in range(0, 25)]
    polygon(draw, tongue, RED)


def mouth_o(draw, y=-40.0, rx=5.0, ry=7.0):
    ellipse(draw, 0, y, rx + 1.2, ry + 1.2, INK)
    ellipse(draw, 0, y, rx - 0.6, ry - 0.8, (80, 24, 30, 255))


def mouth_wavy(draw, y=-42.0, w=12.0, amp=2.2):
    pts = [(-w + i * (2 * w) / 8, y + (amp if i % 2 else -amp)) for i in range(9)]
    stroke(draw, pts, 1.9)


def mouth_frown(draw, y=-43.0, w=9.0):
    stroke(draw, [(-w, y - 1.5), (-w * 0.4, y + 1.6), (w * 0.4, y + 1.6), (w, y - 1.5)], 1.9)


def mouth_flat(draw, y=-43.0, w=7.0):
    stroke(draw, [(-w, y), (w, y)], 1.9)


def mouth_gritted(draw, y=-41.0, w=11.0):
    polygon(draw, [(-w, y + 4), (w, y + 4), (w * 0.85, y - 3.5), (-w * 0.85, y - 3.5)], WHITE)
    stroke(draw, [(-w, y + 4), (w, y + 4), (w * 0.85, y - 3.5), (-w * 0.85, y - 3.5), (-w, y + 4)], 1.5)
    for k in range(-2, 3):
        stroke(draw, [(k * w * 0.36, y + 4), (k * w * 0.34, y - 3.5)], 1.0)
    stroke(draw, [(-w * 0.95, y + 0.2), (w * 0.95, y + 0.2)], 1.0)


def sweat(draw, az, el, size=1.0):
    pts = [(az, el + 6 * size), (az - 3.2 * size, el), (az - 3.6 * size, el - 3 * size), (az, el - 6 * size), (az + 3.6 * size, el - 3 * size), (az + 3.2 * size, el)]
    polygon(draw, pts, BLUE)
    stroke(draw, pts + [pts[0]], 1.3)
    ellipse(draw, az - 1.2 * size, el - 2.5 * size, 1.1 * size, 1.1 * size, WHITE)


def blush(draw):
    for side in (-1, 1):
        ellipse(draw, side * 40.0, -27.0, 7.5, 4.2, PINK)


def bolt(draw, az, el, size=1.0):
    pts = [(az, el + 8 * size), (az - 4 * size, el), (az - 0.5 * size, el), (az - 3 * size, el - 8 * size), (az + 4.5 * size, el + 1 * size), (az + 0.8 * size, el + 1 * size)]
    polygon(draw, pts, YELLOW)
    stroke(draw, pts + [pts[0]], 1.5)


def draw_expression(name):
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    both = (-1, 1)

    if name == "neutral":
        for s in both:
            open_eye(d, s); brow(d, s)
        mouth_smile(d, 5.5, 1.2, thick=1.6)
    elif name == "blink":
        for s in both:
            closed_eye(d, s, -1); brow(d, s)
        mouth_smile(d, 5.5, 1.2, thick=1.6)
    elif name == "happy":
        for s in both:
            closed_eye(d, s, +1); brow(d, s, outer=2, lift=1.5)
        mouth_open_d(d); blush(d)
    elif name == "hurt":
        for s in both:
            squeezed_eye(d, s); brow(d, s, inner=7, outer=-3)
        mouth_open_d(d, y=-39, w=11, h=9)
    elif name == "dizzy":
        for s in both:
            spiral_eye(d, s); brow(d, s, inner=3, outer=-1)
        mouth_wavy(d)
        for az, el in ((-52, 22), (50, 26)):
            ellipse(d, az, el, 2.4, 2.4, YELLOW)
    elif name == "scared":
        for s in both:
            open_eye(d, s, pupil=4.2, look=(0, 0), lashes=False); brow(d, s, inner=9, outer=-2, lift=2)
        mouth_wavy(d, y=-41, w=10, amp=2.8)
        sweat(d, -58, 12, 1.1); sweat(d, 57, 6, 0.9)
    elif name == "cold":
        for s in both:
            open_eye(d, s, pupil=9.0, look=(0, -2.0)); brow(d, s, inner=6, outer=-2)
        mouth_wavy(d, y=-41, w=11, amp=3.2)
        for az, el in ((-56, -26), (56, -26)):
            stroke(d, [(az - 3, el + 4), (az, el), (az - 3, el - 4), (az, el - 8)], 1.4, fill=BLUE)
        stroke(d, [(-46, -27), (-41, -29), (-36, -27)], 1.2, fill=BLUE)
    elif name == "hot":
        for s in both:
            open_eye(d, s, pupil=8.0, look=(0, -3.0)); brow(d, s, inner=-3, outer=3)
        mouth_open_d(d, y=-40, w=9, h=8)
        sweat(d, -55, 18, 1.3); sweat(d, 56, 10, 1.1); sweat(d, -50, -12, 0.9)
        for s in both:
            ellipse(d, s * 41.0, -27.0, 8, 4.5, (240, 90, 80, 150))
    elif name == "shocked":
        for s in both:
            star_eye(d, s); brow(d, s, inner=8, outer=1, lift=2)
        mouth_open_d(d, y=-40, w=11, h=10)
        bolt(d, -57, 14, 1.1); bolt(d, 57, 18, 1.0)
    elif name == "sick":
        for s in both:
            open_eye(d, s, pupil=7.0, look=(0, -3.5), lashes=False); brow(d, s, inner=7, outer=-3)
        mouth_wavy(d, y=-42, w=12, amp=1.8)
        for s in both:
            ellipse(d, s * 41.0, -26.0, 9, 5, (136, 190, 70, 170))
        ellipse(d, 50, -8, 0.1, 0.1, GREEN)
    elif name == "love":
        for s in both:
            heart_eye(d, s); brow(d, s, outer=2, lift=1.5)
        mouth_smile(d, 9, 3.5, thick=2.0); blush(d)
    elif name == "effort":
        for s in both:
            open_eye(d, s, pupil=10.5, look=(0, 0)); brow(d, s, inner=-9, outer=2)
        mouth_gritted(d)
    elif name == "dead":
        for s in both:
            x_eye(d, s); brow(d, s, inner=4, outer=-3)
        stroke(d, [(-9, -40), (-5, -43), (-1, -39), (3, -43), (7, -39), (11, -42)], 1.8)
    elif name == "surprised":
        for s in both:
            open_eye(d, s, pupil=8.0, look=(0, 0), lashes=False); brow(d, s, inner=4, outer=2, lift=6)
        mouth_o(d)
    elif name == "worried":
        for s in both:
            open_eye(d, s, pupil=11.0, look=(0, -1.0)); brow(d, s, inner=8, outer=-2)
        mouth_frown(d)
    elif name == "wink":
        open_eye(d, -1); brow(d, -1, lift=1)
        closed_eye(d, +1, +1); brow(d, +1, outer=2, lift=2)
        mouth_open_d(d, y=-41, w=9, h=7)
        blush(d)
    return img.resize((CELL_W, CELL_H), Image.LANCZOS)


def main():
    atlas = Image.new("RGBA", (CELL_W * COLS, CELL_H * ROWS), (0, 0, 0, 0))
    for index, name in enumerate(EXPRESSIONS):
        atlas.paste(draw_expression(name), ((index % COLS) * CELL_W, (index // COLS) * CELL_H))
    path = os.path.join(OUT, "face_atlas.png")
    atlas.save(path)
    print("wrote", os.path.abspath(path), atlas.size)


if __name__ == "__main__":
    main()
