"""Draws the character's face (eyes, eyebrows, chin mark) as a transparent texture.

The texture is mapped onto a patch of the head sphere by azimuth (u) and elevation (v), so everything here
is drawn in angles: x covers -65..+65 degrees, y covers +50 (top) .. -50 (bottom).
Run:  python Tools/generate_character_art.py
"""
import math
import os

from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(__file__), "..", "MixedUp", "Assets", "_Project", "Art", "Characters")
os.makedirs(OUT, exist_ok=True)

AZ, EL = 130.0, 100.0         # degrees covered by the texture
PPD = 16                      # pixels per degree while drawing (2x supersampling of the final 8 px/degree)
W, H = int(AZ * PPD), int(EL * PPD)

INK = (14, 12, 12, 255)
WHITE = (255, 255, 255, 255)


def px(az, el):
    return ((az + AZ / 2) * PPD, (EL / 2 - el) * PPD)


def ellipse(draw, az, el, rx, ry, fill):
    cx, cy = px(az, el)
    draw.ellipse([cx - rx * PPD, cy - ry * PPD, cx + rx * PPD, cy + ry * PPD], fill=fill)


def stroke(draw, points, width, fill=INK, steps=40, taper=None):
    """Smooth stroke through control points (Catmull-Rom), drawn as overlapping discs. taper(t) scales the width."""
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


def eye(draw, side):
    """side = -1 for the character's left eye, +1 for the right one (as seen from the front, mirrored in azimuth)."""
    cx, cy = side * 26.0, -9.0
    rx, ry = 17.5, 16.0

    # sclera with a dark rim
    ellipse(draw, cx, cy, rx + 2.0, ry + 2.0, INK)
    ellipse(draw, cx, cy, rx, ry, WHITE)

    # big pupil looking slightly towards the nose
    pcx = cx - side * 1.5
    ellipse(draw, pcx, cy - 0.5, 12.5, 13.5, INK)

    # highlights: one large, one small
    ellipse(draw, pcx - side * 4.0, cy + 5.5, 4.6, 4.6, WHITE)
    ellipse(draw, pcx + side * 4.5, cy - 5.0, 2.0, 2.0, WHITE)

    # heavy upper lid
    lid = [(cx - side * 17.0, cy + 7.0), (cx - side * 9.0, cy + 15.5), (cx + side * 2.0, cy + 18.0),
           (cx + side * 12.0, cy + 15.5), (cx + side * 19.5, cy + 9.0)]
    stroke(draw, lid, 4.6, taper=lambda t: 0.55 + 0.45 * math.sin(math.pi * min(1.0, t * 1.15)))

    # lash flicks at the outer corner and a couple of small ones underneath
    outer = side * 19.5
    stroke(draw, [(cx + side * 17.0, cy + 9.0), (cx + side * 22.0, cy + 12.0), (cx + side * 25.5, cy + 10.5)], 3.4,
           taper=lambda t: 0.9 - 0.7 * t)
    stroke(draw, [(cx + side * 17.5, cy + 3.0), (cx + side * 23.5, cy + 3.8), (cx + side * 27.0, cy + 1.2)], 3.2,
           taper=lambda t: 0.9 - 0.7 * t)
    stroke(draw, [(cx + side * 15.5, cy - 7.0), (cx + side * 20.5, cy - 10.0)], 2.6, taper=lambda t: 0.9 - 0.6 * t)

    # eyebrow: heavy and slightly furrowed (inner end lower)
    brow = [(cx - side * 13.0, cy + 22.0), (cx - side * 3.0, cy + 26.0), (cx + side * 9.0, cy + 27.5), (cx + side * 17.0, cy + 24.5)]
    stroke(draw, brow, 5.4, taper=lambda t: 0.7 + 0.3 * math.sin(math.pi * t))


def chin(draw):
    stroke(draw, [(-7.0, -43.0), (-2.5, -44.4), (3.0, -44.4), (7.5, -43.0)], 1.6)


def main():
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    eye(draw, -1)
    eye(draw, +1)
    chin(draw)
    img = img.resize((W // 2, H // 2), Image.LANCZOS)
    path = os.path.join(OUT, "face.png")
    img.save(path)
    print("wrote", os.path.abspath(path), img.size)


if __name__ == "__main__":
    main()
