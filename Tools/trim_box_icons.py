"""Trims the transparent margins of the box drawings so the icons fill their frames in the HUD and the menus.

The drawings in Art/UI/Boxes are 1026 x 572 with the box in the middle, which makes the icons look tiny when they are shown
in a small square. This writes tightly cropped copies (with a little padding, on a square-ish canvas) to Art/UI/Boxes/Trimmed.

    python Tools/trim_box_icons.py
"""
import glob
import os

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "MixedUp", "Assets", "_Project", "Art", "UI", "Boxes")
DST = os.path.join(SRC, "Trimmed")
PADDING = 0.05


def main():
    os.makedirs(DST, exist_ok=True)
    for path in sorted(glob.glob(os.path.join(SRC, "*.png"))):
        image = Image.open(path).convert("RGBA")
        box = image.getchannel("A").getbbox()
        if box is None:
            continue
        crop = image.crop(box)
        side = int(max(crop.size) * (1 + 2 * PADDING))
        canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        canvas.paste(crop, ((side - crop.width) // 2, (side - crop.height) // 2), crop)
        canvas = canvas.resize((384, 384), Image.LANCZOS)
        out = os.path.join(DST, os.path.basename(path))
        canvas.save(out)
        print("wrote", out, "from", image.size, "box", box)


if __name__ == "__main__":
    main()
