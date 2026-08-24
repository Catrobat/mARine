"""MinimapRadar layout, old rules vs new, on identical blip data.
Layout only — says nothing about Unity Text metrics."""
import math, os, random
import numpy as np
from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "preview")
os.makedirs(OUT, exist_ok=True)

R          = 150          # radarPixelRadius, scaled up for legibility
SCAN       = 18.0         # scanRadius, metres
CELL       = 22.0         # clusterCellPixels
BASE       = 10.0
DENSITY_CAP= 8

SPECIES = [((255, 150, 175), 0.42),   # pink  – coral
           ((205, 255, 215), 0.34),   # pale green – seaweed
           ((130, 235, 170), 0.16),   # green – kelp
           ((120, 230, 220), 0.08)]   # cyan  – anemone


def world_props(rng, n=420):
    """Props on the seabed around a swimmer who floats ~3 m above it."""
    out = []
    for _ in range(n):
        a, d = rng.uniform(0, math.tau), math.sqrt(rng.random()) * SCAN * 1.55
        col = rng.choices([c for c, _ in SPECIES], weights=[w for _, w in SPECIES])[0]
        out.append((math.cos(a) * d, math.sin(a) * d, -rng.uniform(2.0, 4.0), col))
    return out


def collect(props, *, clamp_out, bands, layer_h=2.0, layer_range=1, max_blips=None):
    px_per_m = R / SCAN
    cells, blips = {}, []
    for x, z, dy, col in props:
        layer = round(dy / layer_h)
        if abs(layer) > layer_range:
            continue
        fx, fz = x, z
        if fx * fx + fz * fz > SCAN * SCAN:
            if not clamp_out:
                continue
            k = SCAN / math.hypot(fx, fz); fx, fz = fx * k, fz * k
        px, py = fx * px_per_m, fz * px_per_m
        band = "level" if (not bands or layer == 0) else ("above" if layer > 0 else "below")
        key = (round(px / CELL), round(py / CELL), layer)
        if key in cells:
            b = blips[cells[key]]; b["n"] += 1
        else:
            cells[key] = len(blips)
            blips.append({"p": (px, py), "band": band, "c": col, "n": 1})
    if max_blips:
        blips.sort(key=lambda b: b["p"][0] ** 2 + b["p"][1] ** 2)
        blips = blips[:max_blips]
    return blips


def glyph(dr, p, size, colour, band):
    x, y = R + p[0], R - p[1]
    h = size * 0.5
    if band == "level":
        dr.ellipse([x - h * 0.62, y - h * 0.62, x + h * 0.62, y + h * 0.62], fill=colour)
    elif band == "below":
        dr.polygon([(x - h, y - h), (x + h, y - h), (x, y + h)], fill=colour)
    else:
        dr.polygon([(x - h, y + h), (x + h, y + h), (x, y - h)], fill=colour)


def render(blips, *, size_per_extra, white_mix, ring, north, title):
    img = Image.new("RGB", (R * 2 + 40, R * 2 + 70), (58, 122, 150))
    dr = ImageDraw.Draw(img, "RGBA")
    cx = cy = R + 20
    dr.ellipse([cx - R, cy - R, cx + R, cy + R], fill=(30, 70, 92, 235),
               outline=(150, 215, 240, 120), width=2)
    if ring:
        dr.ellipse([cx - R * .5, cy - R * .5, cx + R * .5, cy + R * .5],
                   outline=(255, 255, 255, 40), width=1)

    base = Image.new("RGBA", img.size, (0, 0, 0, 0))
    bd = ImageDraw.Draw(base)
    for b in blips:
        d = min(1.0, (b["n"] - 1) / (DENSITY_CAP - 1))
        px = BASE + size_per_extra * (d * (DENSITY_CAP - 1) if size_per_extra > 2.5 else d)
        col = tuple(int(c + (255 - c) * d * white_mix) for c in b["c"])
        glyph(bd, (b["p"][0] + 20 - R + R, b["p"][1]), px, col + (255,), b["band"])
    img.paste(Image.alpha_composite(img.convert("RGBA"), base).convert("RGB"), (0, 0))
    dr = ImageDraw.Draw(img, "RGBA")

    if north:
        dr.text((cx - 4, cy - R + 6), "N", fill=(255, 255, 255, 190))
    dr.polygon([(cx, cy - 9), (cx - 6, cy + 6), (cx + 6, cy + 6)], fill=(255, 235, 50))
    dr.text((14, R * 2 + 46), title, fill=(255, 255, 255))
    return img


if __name__ == "__main__":
    props = world_props(random.Random(4))
    old = collect(props, clamp_out=True,  bands=True)
    new = collect(props, clamp_out=False, bands=False, max_blips=60)
    print(f"old: {len(old)} blips   new: {len(new)} blips")

    a = render(old, size_per_extra=3.0, white_mix=0.85, ring=False, north=False,
               title="BEFORE  rim-clamped, depth bands, 10-31px, washes to white")
    b = render(new, size_per_extra=3.0, white_mix=0.25, ring=True, north=True,
               title="AFTER  culled, dots, 10-13px, keeps colour, ring + N")
    out = Image.new("RGB", (a.width + b.width + 12, a.height), (24, 40, 52))
    out.paste(a, (0, 0)); out.paste(b, (a.width + 12, 0))
    out.save(os.path.join(OUT, "minimap_before_after.png"))
    print("wrote minimap_before_after.png")
