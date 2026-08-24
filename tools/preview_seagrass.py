"""Offline preview for BuildSeagrassTuft. Renders one tuft and a meadow patch —
a tuft can look right alone and still read as shuttlecocks when repeated."""
import math, os, sys, random
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import figs  # noqa: F401  (kept so the port stays beside the harness)

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "preview")
os.makedirs(OUT, exist_ok=True)


def seagrass_tuft(rng, blades=None, current=None):
    """One shoot: several arcing ribbons from a small common base.

    Differs from kelp deliberately. Kelp is a few tall S-curved straps from a wide
    ring; seagrass is many short blades from a near-point base, each arcing over in
    roughly (not exactly) the same direction, because they all lean with the same
    current. That shared lean is what makes a meadow read as a surface with a grain
    rather than as scattered individual plants.
    """
    verts, tris = [], []
    n = blades if blades is not None else rng.randint(9, 13)

    # The current is a property of the *meadow*, not the shoot. Every tuft leans broadly
    # the same way with only a little jitter, which is what gives a bed its grain — and
    lean_dir = (rng.uniform(0, math.tau) if current is None
                else current + rng.uniform(-0.45, 0.45))
    segs = 5                                   # 5 segments = 10 tris a blade

    for b in range(n):
        # Blades stay in a coherent sheaf. Wide splay made the tuft a shuttlecock.
        yaw = lean_dir + rng.uniform(-0.5, 0.5)
        # Long blades dominate: they overlap their neighbours and close the gaps
        # between tufts, which is what turns separate shoots into a bed.
        h = 0.70 + 0.30 * (rng.random() ** 1.2)
        # Real lean, not a gentle nod. Below ~0.5 the blades read as wire.
        bend = rng.uniform(0.55, 1.05)
        # Roughly double the first attempt. Narrow ribbons rasterise to hairlines
        # and the whole patch turns spiky.
        w0 = rng.uniform(0.038, 0.058)

        right = np.array([math.cos(yaw), 0.0, math.sin(yaw)])
        side = np.array([-math.sin(yaw), 0.0, math.cos(yaw)])
        # Blades leave the sediment from slightly different spots, not one point.
        base = (right * rng.uniform(0.0, 0.055) + side * rng.uniform(-0.055, 0.055))

        prev = None
        for i in range(segs + 1):
            t = i / segs
            # Arc: lateral lean grows faster than linear while height compresses,
            # which keeps the blade roughly the same arc-length as it bends over.
            y = h * (t - 0.30 * t ** 3 * bend)
            # A pure power curve leaves the sediment with zero slope, so every blade starts dead
            # vertical and a dense tuft becomes a block of pillars. The linear term gives the
            lat = h * bend * (0.28 * t + 0.72 * t ** 1.7)
            # Narrow where it leaves the sediment, widest around a third up, then a long taper.
            # Widest-at-base is what made the bed's bottom edge read as a row of rectangles.
            swell = math.sin(min(t, 0.30) / 0.30 * 1.57)
            width = w0 * (0.42 + 0.62 * swell - 0.46 * t)

            centre = base + right * lat + np.array([0.0, y, 0.0])
            a = centre - side * width
            c = centre + side * width

            if prev is not None:
                i0 = len(verts)
                verts.extend([prev[0], prev[1], c, a])
                # Double-sided: the real shader is Cull Off, and a one-sided ribbon
                # would simply vanish from behind in this rasteriser.
                tris.extend([(i0, i0 + 1, i0 + 2), (i0, i0 + 2, i0 + 3),
                             (i0 + 2, i0 + 1, i0), (i0 + 3, i0 + 2, i0)])
            prev = (a, c)

    return verts, tris


def meadow(rng, count=110, spread=2.2):
    """A patch, viewed low. This is the test that matters — density is the effect."""
    verts, tris = [], []
    current = rng.uniform(0, math.tau)          # one current for the whole bed
    for _ in range(count):
        v, t = seagrass_tuft(rng, current=current)
        off = np.array([rng.uniform(-spread, spread), 0.0, rng.uniform(-spread, spread)])
        s = rng.uniform(0.75, 1.25)
        i0 = len(verts)
        verts.extend([np.asarray(p) * s + off for p in v])
        tris.extend([(a + i0, b + i0, c + i0) for (a, b, c) in t])
    return verts, tris


def render_ribbons(verts, tris, size=760, ss=3, yaw=0.5, pitch=0.10,
                   bg=(232, 238, 240), root=(0.14, 0.30, 0.20), tip=(0.52, 0.72, 0.38)):
    """Mirrors UnderwaterKelp.shader rather than the flat-shaded rock renderer.

    Three things in that shader change the read completely and a plain flat-shaded
    pass gets all three wrong: backface normals are flipped (IS_FRONT_VFACE), so a
    leaning bed is not a dark mat; lighting is half-Lambert (dot*0.5+0.5), so the
    darkest a blade gets is half-lit, not black; and albedo ramps root-to-tip by
    uv.y. Judging blade shape under the wrong lighting is how you 'fix' a shape
    problem that was never there.
    """
    from PIL import ImageDraw
    S = size * ss
    img = Image.new("RGB", (S, S), bg)
    dr = ImageDraw.Draw(img)

    cy, sy = math.cos(yaw), math.sin(yaw)
    cp, sp = math.cos(pitch), math.sin(pitch)
    R = (np.array([[1, 0, 0], [0, cp, -sp], [0, sp, cp]]) @
         np.array([[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]]))

    V = np.array(verts)
    P = V @ R.T
    scale = S * 0.40 / max(np.abs(P[:, :3]).max(), 1e-6)
    L = np.array([0.4, 0.75, 0.5]); L /= np.linalg.norm(L)
    ymax = max(V[:, 1].max(), 1e-6)

    faces = []
    for (a, b, c) in tris:
        pa, pb, pc = P[a], P[b], P[c]
        n = np.cross(pb - pa, pc - pa)
        ln = np.linalg.norm(n)
        if ln < 1e-12:
            continue
        faces.append(((pa[2] + pb[2] + pc[2]) / 3, pa, pb, pc, n / ln,
                      (V[a, 1] + V[b, 1] + V[c, 1]) / 3))
    faces.sort(key=lambda f: f[0])

    root_c, tip_c = np.array(root), np.array(tip)
    for _, pa, pb, pc, n, hmid in faces:
        if n[2] < 0:                       # Cull Off + flipped normal, as in-shader
            n = -n
        half = float(np.dot(n, L)) * 0.5 + 0.5      # half-Lambert, never black
        albedo = root_c + (tip_c - root_c) * np.clip(hmid / ymax, 0, 1)
        col = np.clip(albedo * (0.30 + 0.85 * half), 0, 1)
        pts = [(S / 2 + p[0] * scale, S * 0.72 - p[1] * scale) for p in (pa, pb, pc)]
        dr.polygon(pts, fill=tuple((col * 255).astype(int)))

    return np.array(img.resize((size, size), Image.LANCZOS))


def save(arr, name):
    Image.fromarray(arr.astype(np.uint8)).save(os.path.join(OUT, name))
    print("wrote", name)


if __name__ == "__main__":
    rng = random.Random(7)
    v, t = seagrass_tuft(rng)
    print(f"single tuft: {len(v)} verts, {len(t)//2} tris (one-sided)")
    save(render_ribbons(v, t, size=460, yaw=0.6, pitch=0.18), "seagrass_a_tuft.png")

    rng = random.Random(11)
    v, t = meadow(rng)
    print(f"meadow: {len(v)} verts, {len(t)//2} tris (one-sided)")
    save(render_ribbons(v, t, size=760, yaw=0.5, pitch=0.30), "seagrass_b_meadow.png")
