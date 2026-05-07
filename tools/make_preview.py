#!/usr/bin/env python3
"""Render a PNG mock-up of the PictureApp window for README preview.

This isn't a real screenshot of the app (we can't run WPF here on Linux);
it's a faithful re-creation of the XAML layout so users see what to expect.
"""
from __future__ import annotations

import os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_DIR = os.path.join(REPO_ROOT, "docs")
os.makedirs(OUT_DIR, exist_ok=True)
OUT = os.path.join(OUT_DIR, "preview.png")


def find_font(size: int) -> ImageFont.FreeTypeFont:
    candidates = [
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
        "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
    ]
    for p in candidates:
        if os.path.exists(p):
            return ImageFont.truetype(p, size)
    return ImageFont.load_default()


def make_background(w: int, h: int) -> Image.Image:
    """Fake desktop wallpaper to show the glass effect over."""
    bg = Image.new("RGB", (w, h), (90, 105, 130))
    d = ImageDraw.Draw(bg)
    # Soft gradient + a few blobs
    for y in range(h):
        t = y / h
        r = int(70 + 60 * t)
        g = int(110 + 30 * t)
        b = int(160 - 40 * t)
        d.line([(0, y), (w, y)], fill=(r, g, b))
    # blobs
    for cx, cy, rad, color in [
        (w * 0.2, h * 0.3, 220, (255, 180, 90)),
        (w * 0.8, h * 0.7, 260, (90, 200, 230)),
        (w * 0.55, h * 0.2, 180, (200, 130, 230)),
    ]:
        bb = (cx - rad, cy - rad, cx + rad, cy + rad)
        d.ellipse(bb, fill=color)
    return bg.filter(ImageFilter.GaussianBlur(radius=40))


def main() -> None:
    W, H = 1280, 820
    bg = make_background(W, H)
    out = bg.copy()

    # Window box
    pad = 80
    win_box = (pad, pad - 20, W - pad, H - pad)

    # Glass layer: blur the background under the window and tint slightly white
    cropped = bg.crop(win_box).filter(ImageFilter.GaussianBlur(radius=18))
    tint = Image.new("RGBA", cropped.size, (255, 255, 255, 90))
    cropped = Image.alpha_composite(cropped.convert("RGBA"), tint)

    # Rounded mask for the window
    mask = Image.new("L", cropped.size, 0)
    md = ImageDraw.Draw(mask)
    md.rounded_rectangle((0, 0, cropped.size[0] - 1, cropped.size[1] - 1),
                          radius=22, fill=255)
    out.paste(cropped, (win_box[0], win_box[1]), mask)

    d = ImageDraw.Draw(out, "RGBA")

    # Subtle dark stroke on window
    d.rounded_rectangle(win_box, radius=22, outline=(40, 40, 40, 90), width=2)

    # Title bar text (faint)
    title_font = find_font(16)
    d.text((win_box[0] + 24, win_box[1] + 12), "PictureApp",
           fill=(20, 20, 20, 170), font=title_font)

    # Window control hints (top right)
    cx_right = win_box[2] - 20
    for label, dx in [("✕", 0), ("☐", -34), ("‒", -68)]:
        d.text((cx_right + dx - 6, win_box[1] + 8), label,
               fill=(20, 20, 20, 200), font=title_font)

    # Image card
    card_pad_x, card_pad_y = 100, 70
    card_box = (
        win_box[0] + card_pad_x,
        win_box[1] + card_pad_y,
        win_box[2] - card_pad_x,
        win_box[3] - 30,
    )
    d.rounded_rectangle(card_box, radius=18, fill=(255, 255, 255, 255),
                        outline=(43, 43, 43, 220), width=2)

    # "Картинка" placeholder text in the center
    big_font = find_font(56)
    text = "Картинка"
    bbox = d.textbbox((0, 0), text, font=big_font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    cx = (card_box[0] + card_box[2]) / 2 - tw / 2
    cy = (card_box[1] + card_box[3]) / 2 - th / 2 - 8
    d.text((cx, cy), text, fill=(31, 31, 31, 255), font=big_font)

    # Side arrow buttons
    nav_font = find_font(40)
    btn_r = 30
    for ch, x in [("‹", win_box[0] + 50), ("›", win_box[2] - 50)]:
        ay = (win_box[1] + win_box[3]) / 2
        d.ellipse((x - btn_r, ay - btn_r, x + btn_r, ay + btn_r),
                  fill=(255, 255, 255, 200),
                  outline=(43, 43, 43, 200), width=2)
        bbox = d.textbbox((0, 0), ch, font=nav_font)
        cw, ch2 = bbox[2] - bbox[0], bbox[3] - bbox[1]
        d.text((x - cw / 2 - bbox[0], ay - ch2 / 2 - bbox[1] - 2),
               ch, fill=(31, 31, 31, 255), font=nav_font)

    out.convert("RGB").save(OUT, "PNG", optimize=True)
    print(f"Wrote {OUT}")


if __name__ == "__main__":
    main()
