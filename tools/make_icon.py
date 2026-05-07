#!/usr/bin/env python3
"""Generate a minimalist app icon (.ico) for PictureApp.

Design (matches the app's UI language):
- Soft rounded square card with a thin dark stroke (the "image frame").
- A small mountain + sun motif inside the frame (recognisable image-viewer cue).
- Subtle "<" and ">" hints on the sides at larger sizes.

Outputs Resources/app.ico with multiple sizes (16, 24, 32, 48, 64, 128, 256).
Run from repo root: python3 tools/make_icon.py
"""
from __future__ import annotations

import os
from PIL import Image, ImageDraw

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(REPO_ROOT, "src", "PictureApp", "Resources", "app.ico")
os.makedirs(os.path.dirname(OUT), exist_ok=True)

SIZES = [16, 24, 32, 48, 64, 128, 256]

# Palette
CARD_FILL = (255, 255, 255, 255)
CARD_STROKE = (43, 43, 43, 220)
SKY = (210, 226, 240, 255)
SUN = (255, 196, 96, 255)
MOUNT_FRONT = (90, 110, 130, 255)
MOUNT_BACK = (140, 160, 180, 255)
ARROW = (43, 43, 43, 235)


def render(size: int) -> Image.Image:
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # Card geometry: leave room for arrows on the sides at larger sizes
    pad = max(1, size // 16)
    side_margin = max(0, size // 8) if size >= 48 else pad
    card_box = (side_margin, pad, size - side_margin - 1, size - pad - 1)
    radius = max(1, size // 8)
    stroke_w = max(1, size // 32)

    # Card fill + stroke
    d.rounded_rectangle(card_box, radius=radius, fill=CARD_FILL,
                        outline=CARD_STROKE, width=stroke_w)

    # Inner content area
    cx0, cy0, cx1, cy1 = card_box
    inner_pad = max(1, size // 16)
    ix0, iy0 = cx0 + inner_pad, cy0 + inner_pad
    ix1, iy1 = cx1 - inner_pad, cy1 - inner_pad

    # Sky band (top portion of the card)
    sky_box = (ix0, iy0, ix1, iy0 + (iy1 - iy0) * 2 // 3)
    d.rectangle(sky_box, fill=SKY)

    # Sun
    sun_r = max(1, (iy1 - iy0) // 5)
    sun_cx = ix0 + (ix1 - ix0) * 3 // 4
    sun_cy = iy0 + (iy1 - iy0) // 3
    d.ellipse((sun_cx - sun_r, sun_cy - sun_r, sun_cx + sun_r, sun_cy + sun_r),
              fill=SUN)

    # Mountains: back triangle + front triangle
    base_y = iy1
    back_apex = (ix0 + (ix1 - ix0) * 2 // 5,
                 iy0 + (iy1 - iy0) * 1 // 3)
    front_apex = (ix0 + (ix1 - ix0) * 2 // 3,
                  iy0 + (iy1 - iy0) * 2 // 5)

    d.polygon([(ix0, base_y), back_apex, (ix1, base_y)], fill=MOUNT_BACK)
    d.polygon([(ix0 + (ix1 - ix0) // 4, base_y),
               front_apex,
               (ix1, base_y)], fill=MOUNT_FRONT)

    # Side arrow hints (only when we have side margin to draw in)
    if side_margin >= 4:
        arrow_w = max(1, size // 48)
        # Left arrow "<"
        ax = side_margin // 2
        ay = size // 2
        ah = max(2, size // 8)
        d.line([(ax + ah // 2, ay - ah), (ax - ah // 2, ay), (ax + ah // 2, ay + ah)],
               fill=ARROW, width=arrow_w, joint="curve")
        # Right arrow ">"
        bx = size - side_margin // 2 - 1
        d.line([(bx - ah // 2, ay - ah), (bx + ah // 2, ay), (bx - ah // 2, ay + ah)],
               fill=ARROW, width=arrow_w, joint="curve")

    return img


def main() -> None:
    # Render each size separately so detail isn't lost when downscaling.
    # PIL's ICO writer supports `append_images=[...]` to embed multiple
    # pre-rendered frames (Pillow >= 9.0).
    layers = [render(s) for s in SIZES]
    largest = max(layers, key=lambda im: im.size[0])
    others = [im for im in layers if im is not largest]
    largest.save(
        OUT,
        format="ICO",
        sizes=[im.size for im in layers],
        append_images=others,
    )
    print(f"Wrote {OUT} ({os.path.getsize(OUT)} bytes)")


if __name__ == "__main__":
    main()
