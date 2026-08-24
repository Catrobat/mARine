"""
Figure generator for the Procedural Terrain technical report.

Every raster figure is produced by a faithful Python port of the actual C# in
Assets/ProceduralTerrain — Noise.cs, TerrainShaper.cs, BiomeStyles.cs,
ProceduralMeshLibrary.cs (RockStyle), TerrainDetailScatter.cs (Vogel packing),
UnderwaterCommon.hlsl (absorption). Nothing here is hand-drawn approximation.

The only substitution: Unity's Mathf.PerlinNoise is replaced by Ken Perlin's
improved 2D noise (same construction, same [0,1] output range, different
permutation table), so layouts differ from the app but the *character* produced
by the algorithm is identical.
"""
import math, os
import numpy as np
from PIL import Image

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "figs")
os.makedirs(OUT, exist_ok=True)

# ─────────────────────────────────────────────────────────────────────────────
# Perlin backend (stand-in for Mathf.PerlinNoise; returns [0,1])
# ─────────────────────────────────────────────────────────────────────────────
_p = np.arange(256, dtype=np.int32)
np.random.default_rng(20260730).shuffle(_p)
_perm = np.concatenate([_p, _p])

def _fade(t): return t * t * t * (t * (t * 6 - 15) + 10)

def _grad2(h, x, y):
    h = h & 7
    u = np.where(h < 4, x, y)
    v = np.where(h < 4, y, x)
    return (np.where(h & 1, -u, u) + np.where(h & 2, -2.0 * v, 2.0 * v))

def perlin(x, y):
    """Vectorised 2D Perlin in [0,1], matching Mathf.PerlinNoise's range."""
    x = np.asarray(x, dtype=np.float64); y = np.asarray(y, dtype=np.float64)
    xi = np.floor(x).astype(np.int64); yi = np.floor(y).astype(np.int64)
    xf = x - xi; yf = y - yi
    X = xi & 255; Y = yi & 255
    u = _fade(xf); v = _fade(yf)
    A = _perm[X] + Y; B = _perm[X + 1] + Y
    n00 = _grad2(_perm[A], xf, yf)
    n10 = _grad2(_perm[B], xf - 1, yf)
    n01 = _grad2(_perm[A + 1], xf, yf - 1)
    n11 = _grad2(_perm[B + 1], xf - 1, yf - 1)
    x1 = n00 + u * (n10 - n00)
    x2 = n01 + u * (n11 - n01)
    val = x1 + v * (x2 - x1)
    return np.clip(val / 4.0 + 0.5, 0.0, 1.0)

class UnityRandom:
    """System.Random-shaped helper: only Next(lo,hi) / NextDouble() are needed."""
    def __init__(self, seed): self.r = np.random.default_rng(abs(seed) + 1)
    def next_int(self, lo, hi): return int(self.r.integers(lo, hi))
    def next_double(self): return float(self.r.random())

# ─────────────────────────────────────────────────────────────────────────────
# Port of Noise.cs — GenerateNoiseMap, NormalizeMode.Global
# ─────────────────────────────────────────────────────────────────────────────
def generate_noise_map(w, h, seed, scale, octaves, persistance, lacunarity, offset):
    rng = UnityRandom(seed)
    octave_offsets = []
    max_possible = 0.0
    amp = 1.0
    for _ in range(octaves):
        octave_offsets.append((rng.next_int(-100000, 100000), rng.next_int(-100000, 100000)))
        max_possible += amp
        amp *= persistance
    if scale <= 0: scale = 0.0001

    xs = np.arange(w)[None, :]; ys = np.arange(h)[:, None]
    # Same coordinate grouping as the C#: small terms first, offset added per octave.
    base_x = xs - w / 2.0 + offset[0]
    base_y = ys - h / 2.0 - offset[1]

    total = np.zeros((h, w))
    amp = 1.0; freq = 1.0
    for i in range(octaves):
        sx = (base_x + octave_offsets[i][0]) / scale * freq
        sy = (base_y + octave_offsets[i][1]) / scale * freq
        total += (perlin(sx, sy) * 2.0 - 1.0) * amp
        amp *= persistance; freq *= lacunarity
    # Global normalisation
    return np.clip((total + 1.0) / (max_possible / 0.9), 0.0, None)

# ─────────────────────────────────────────────────────────────────────────────
# Port of TerrainShaper.cs — Canyons style
# ─────────────────────────────────────────────────────────────────────────────
def terrace(h, steps, sharpness):
    t = h * steps
    band = np.floor(t)
    f = t - band
    p = np.power(np.clip(f, 1e-9, 1), sharpness)
    q = np.power(np.clip(1 - f, 1e-9, 1), sharpness)
    f2 = p / (p + q)
    return (band + f2) / steps

def terrain_shaper(w, h, seed, scale, octaves, persistance, lacunarity, offset,
                   warp_strength=6.0, warp_scale=2.5, ridge_weight=0.55,
                   ridge_sharpness=2.2, terrace_steps=5, terrace_sharpness=4.0,
                   terrace_strength=0.65, stages=("warp", "ridge", "terrace")):
    rng = UnityRandom(seed)
    octave_offsets = []
    max_possible = 0.0
    amp = 1.0
    for _ in range(octaves):
        octave_offsets.append((rng.next_int(-100000, 100000), rng.next_int(-100000, 100000)))
        max_possible += amp
        amp *= persistance
    warp_a = (rng.next_int(-100000, 100000), rng.next_int(-100000, 100000))
    warp_b = (rng.next_int(-100000, 100000), rng.next_int(-100000, 100000))

    xs = np.arange(w)[None, :]; ys = np.arange(h)[:, None]
    base_x = np.broadcast_to(xs - w / 2.0 + offset[0], (h, w)).astype(np.float64).copy()
    base_y = np.broadcast_to(ys - h / 2.0 - offset[1], (h, w)).astype(np.float64).copy()

    warp_feature = scale * max(warp_scale, 0.01)
    if "warp" in stages and warp_strength > 0:
        wx = perlin((base_x + warp_a[0]) / warp_feature, (base_y + warp_a[1]) / warp_feature) * 2 - 1
        wy = perlin((base_x + warp_b[0]) / warp_feature, (base_y + warp_b[1]) / warp_feature) * 2 - 1
        base_x = base_x + wx * warp_strength
        base_y = base_y + wy * warp_strength

    smooth_sum = np.zeros((h, w)); ridge_sum = np.zeros((h, w))
    amp = 1.0; freq = 1.0
    for i in range(octaves):
        sx = (base_x + octave_offsets[i][0]) / scale * freq
        sy = (base_y + octave_offsets[i][1]) / scale * freq
        n = np.clip(perlin(sx, sy), 0.0, 1.0)          # Clamp01 — NaN guard
        smooth_sum += (n * 2 - 1) * amp
        r = 1.0 - np.abs(n * 2 - 1)                     # ridge fold
        ridge_sum += np.power(r, ridge_sharpness) * amp
        amp *= persistance; freq *= lacunarity

    smooth01 = np.clip((smooth_sum + 1) / (max_possible / 0.9), 0, 1)
    ridge01 = np.clip(ridge_sum / max_possible, 0, 1)
    out = smooth01 if "ridge" not in stages else (smooth01 + (ridge01 - smooth01) * ridge_weight)
    if "terrace" in stages and terrace_steps > 0 and terrace_strength > 0:
        out = out + (terrace(out, terrace_steps, terrace_sharpness) - out) * terrace_strength
    return out

# ─────────────────────────────────────────────────────────────────────────────
# Port of BiomeStyles.BuildHeightCurve — Unity AnimationCurve (cubic Hermite)
# ─────────────────────────────────────────────────────────────────────────────
def eval_curve(keys, t):
    t = np.clip(t, keys[0][0], keys[-1][0])
    out = np.zeros_like(t)
    for i in range(len(keys) - 1):
        t0, v0, _, out0 = keys[i]
        t1, v1, in1, _ = keys[i + 1]
        m = (t >= t0) & (t <= t1)
        dt = t1 - t0
        s = np.where(m, (t - t0) / dt, 0.0)
        s2 = s * s; s3 = s2 * s
        h00 = 2 * s3 - 3 * s2 + 1
        h10 = s3 - 2 * s2 + s
        h01 = -2 * s3 + 3 * s2
        h11 = s3 - s2
        v = h00 * v0 + h10 * out0 * dt + h01 * v1 + h11 * in1 * dt
        out = np.where(m, v, out)
    return out

CURVE_CLASSIC = [(0.0, 0.0, 0.0, 0.6), (0.5, 0.45, 1.0, 1.0), (1.0, 1.0, 1.2, 0.0)]
CURVE_CANYONS = [(0.0, 0.0, 0.0, 0.2), (0.45, 0.25, 1.4, 1.4),
                 (0.7, 0.8, 1.4, 0.5), (1.0, 1.0, 0.2, 0.0)]

# ─────────────────────────────────────────────────────────────────────────────
# Hillshade renderer (stands in for the lit mesh)
# ─────────────────────────────────────────────────────────────────────────────
def hillshade(height, z_scale, water_rgb=(0.10, 0.42, 0.52), sand_rgb=(0.93, 0.86, 0.68),
              rock_rgb=(0.42, 0.44, 0.46), fog=0.55):
    h = height * z_scale
    gy, gx = np.gradient(h)
    nz = np.ones_like(h)
    n = np.stack([-gx, nz, -gy], axis=-1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    L = np.array([0.35, 0.86, 0.36]); L /= np.linalg.norm(L)
    lam = np.clip((n @ L) * 0.5 + 0.5, 0, 1)           # wrapped diffuse, as in the shaders

    slope = np.degrees(np.arccos(np.clip(n[..., 1], -1, 1)))
    rockness = np.clip((slope - 22.0) / 22.0, 0, 1)     # steep faces read as rock
    base = (np.array(sand_rgb)[None, None, :] * (1 - rockness[..., None])
            + np.array(rock_rgb)[None, None, :] * rockness[..., None])

    col = base * lam[..., None]
    # depth tint + medium, mirroring ApplyUnderwaterMedium's structure
    depth = np.clip(1.0 - height, 0, 1)[..., None]
    col = col * (1 - 0.45 * depth) + np.array(water_rgb)[None, None, :] * (0.45 * depth)
    vign = np.linspace(-1, 1, h.shape[0])[:, None] ** 2 + np.linspace(-1, 1, h.shape[1])[None, :] ** 2
    t = np.clip(vign / 2.0, 0, 1)[..., None] * fog
    col = col * (1 - t) + np.array(water_rgb)[None, None, :] * t
    return (np.clip(col, 0, 1) * 255).astype(np.uint8)

def render_heightfield(height, height_multiplier, size=(760, 400), ss=2,
                       scale=0.25, water_rgb=(0.10, 0.42, 0.52), fog_end=26.0):
    """
    Oblique 3D render of the streamed seabed. Geometry uses the app's real
    proportions: sample spacing = EndlessTerrain.Scale (0.25 m) and
    y = curve(h) * meshHeightMultiplier * Scale. Shading mirrors the
    UnderwaterSand/Rock path: wrapped diffuse, slope-based sand/rock blend,
    then the exp-squared medium toward the water colour.
    """
    from PIL import ImageDraw
    H, W = height.shape
    SW, SH = size[0] * ss, size[1] * ss
    img = Image.new("RGB", (SW, SH), tuple((np.array(water_rgb) * 255).astype(int)))
    dr = ImageDraw.Draw(img)

    ys = (np.arange(H) - H / 2) * scale
    xs = (np.arange(W) - W / 2) * scale
    Y = height * height_multiplier * scale
    P = np.stack([np.broadcast_to(xs[None, :], (H, W)),
                  Y,
                  np.broadcast_to(ys[:, None], (H, W))], axis=-1)

    # Camera: eye height 2.2 m above the mean floor, looking along +z, 22 deg down.
    pitch = math.radians(26.0)
    eye = np.array([0.0, float(Y.mean()) + 3.0, -(H / 2) * scale - 1.2])
    cp, sp = math.cos(pitch), math.sin(pitch)
    R = np.array([[1, 0, 0], [0, cp, sp], [0, -sp, cp]])
    Q = (P - eye) @ R.T
    f = SW * 0.62
    zc = np.maximum(Q[..., 2], 1e-3)
    px = SW / 2 + Q[..., 0] / zc * f
    py = SH * 0.46 - Q[..., 1] / zc * f

    L = np.array([0.35, 0.86, 0.36]); L /= np.linalg.norm(L)
    sand = np.array([0.93, 0.86, 0.68]); rock = np.array([0.44, 0.45, 0.46])
    wcol = np.array(water_rgb)

    faces = []
    for j in range(H - 1):
        for i in range(W - 1):
            faces.append((j, i))
    # Painter's algorithm: far rows first.
    faces.sort(key=lambda ji: -float(Q[ji[0], ji[1], 2]))

    for (j, i) in faces:
        z = float(Q[j, i, 2])
        if z <= 0.05 or z > fog_end * 1.4:
            continue
        quad = [(px[j, i], py[j, i]), (px[j, i + 1], py[j, i + 1]),
                (px[j + 1, i + 1], py[j + 1, i + 1]), (px[j + 1, i], py[j + 1, i])]
        v0 = P[j, i]; v1 = P[j, i + 1]; v2 = P[j + 1, i]
        n = np.cross(v1 - v0, v2 - v0)
        ln = np.linalg.norm(n)
        if ln < 1e-12:
            continue
        n = n / ln
        if n[1] < 0: n = -n
        slope = math.degrees(math.acos(max(-1.0, min(1.0, float(n[1])))))
        rockness = min(1.0, max(0.0, (slope - 24.0) / 20.0))
        base = sand * (1 - rockness) + rock * rockness
        lam = 0.30 + 0.70 * max(0.0, float(np.dot(n, L)) * 0.5 + 0.5)
        col = base * lam
        # exp-squared medium, matching UnderwaterFog()
        t = 1.0 - math.exp(-((z * (1.0 / (fog_end * 0.52))) ** 2))
        t = min(1.0, max(0.0, t))
        col = col * (1 - t) + wcol * t
        dr.polygon(quad, fill=tuple((np.clip(col, 0, 1) * 255).astype(int)))

    return np.array(img.resize(size, Image.LANCZOS))


def save_png(arr, name):
    Image.fromarray(arr).save(os.path.join(OUT, name))
    print("wrote", name, arr.shape)

# ═════════════════════════════════════════════════════════════════════════════
# FIG 1 — Classic vs Canyons terrain (heightmap + lit render)
# ═════════════════════════════════════════════════════════════════════════════
def fig_terrain():
    N = 420
    classic = generate_noise_map(N, N, seed=0, scale=45, octaves=5,
                                 persistance=0.5, lacunarity=2.0, offset=(0, 0))
    classic_c = eval_curve(CURVE_CLASSIC, np.clip(classic, 0, 1))

    canyon = terrain_shaper(N, N, seed=7, scale=60, octaves=6, persistance=0.5,
                            lacunarity=2.1, offset=(0, 0), warp_strength=6, warp_scale=2.5,
                            ridge_weight=0.6, ridge_sharpness=2.4, terrace_steps=5,
                            terrace_sharpness=4.0, terrace_strength=0.65)
    canyon_c = eval_curve(CURVE_CANYONS, np.clip(canyon, 0, 1))

    def gray(a):
        a = (a - a.min()) / max(float(np.ptp(a)), 1e-6)
        return np.repeat((a * 255).astype(np.uint8)[..., None], 3, axis=2)

    save_png(gray(classic_c), "f1a_classic_height.png")
    save_png(gray(canyon_c), "f1c_canyon_height.png")

    # Oblique 3D views at the app's real proportions (160 x 110 samples = 40 x 27 m)
    cl = generate_noise_map(160, 110, seed=0, scale=45, octaves=5,
                            persistance=0.5, lacunarity=2.0, offset=(0, 0))
    save_png(render_heightfield(eval_curve(CURVE_CLASSIC, np.clip(cl, 0, 1)),
                                height_multiplier=18.0), "f1b_classic_lit.png")

    cn = terrain_shaper(160, 110, seed=7, scale=60, octaves=6, persistance=0.5,
                        lacunarity=2.1, offset=(0, 0), warp_strength=6, warp_scale=2.5,
                        ridge_weight=0.6, ridge_sharpness=2.4, terrace_steps=5,
                        terrace_sharpness=4.0, terrace_strength=0.65)
    save_png(render_heightfield(eval_curve(CURVE_CANYONS, np.clip(cn, 0, 1)),
                                height_multiplier=40.0), "f1d_canyon_lit.png")

# ═════════════════════════════════════════════════════════════════════════════
# FIG 2 — Canyons operator ablation: fBm → +warp → +ridge → +terrace
# ═════════════════════════════════════════════════════════════════════════════
def fig_ablation():
    kw = dict(w=300, h=300, seed=7, scale=60, octaves=6, persistance=0.5,
              lacunarity=2.1, offset=(0, 0), ridge_weight=0.6, ridge_sharpness=2.4,
              terrace_steps=5, terrace_sharpness=4.0, terrace_strength=0.65)
    combos = [("f2a_fbm.png", ()),
              ("f2b_warp.png", ("warp",)),
              ("f2c_ridge.png", ("warp", "ridge")),
              ("f2d_terrace.png", ("warp", "ridge", "terrace"))]
    for name, stages in combos:
        h = terrain_shaper(stages=stages, **kw)
        # Heightmaps, not 3D: warp meanders, ridge crests and terrace banding are
        # all *height-field* structure and read far more clearly in plan view.
        a = np.clip(h, 0, 1)
        lo_p, hi_p = np.percentile(a, 2), np.percentile(a, 98)
        a = np.clip((a - lo_p) / max(hi_p - lo_p, 1e-6), 0, 1)
        # shaded relief overlay so the bands and crests have relief cues too
        gy, gx = np.gradient(a * 26.0)
        n = np.stack([-gx, np.ones_like(a), -gy], axis=-1)
        n /= np.linalg.norm(n, axis=-1, keepdims=True)
        L = np.array([0.4, 0.78, 0.48]); L /= np.linalg.norm(L)
        lam = np.clip((n @ L) * 0.42 + 0.62, 0, 1)
        # hypsometric ramp: deep basin -> mid slope -> lit plateau top
        c0 = np.array([0.05, 0.17, 0.26]); c1 = np.array([0.28, 0.48, 0.50])
        c2 = np.array([0.86, 0.80, 0.62]); c3 = np.array([1.00, 0.98, 0.93])
        t = a[..., None]
        ramp = np.where(t < 0.45, c0 + (c1 - c0) * (t / 0.45),
                np.where(t < 0.78, c1 + (c2 - c1) * ((t - 0.45) / 0.33),
                                   c2 + (c3 - c2) * ((t - 0.78) / 0.22)))
        col = ramp * lam[..., None]
        save_png((np.clip(col, 0, 1) * 255).astype(np.uint8), name)

# ═════════════════════════════════════════════════════════════════════════════
# FIG 3 — Port of ProceduralMeshLibrary.RockStyle, rendered flat-shaded
# ═════════════════════════════════════════════════════════════════════════════
def _hash3(x, y, z, seed):
    h = np.int64(seed)
    h = (h * 374761393 + np.int64(x) * 668265263) & 0xFFFFFFFF
    h = (h * 374761393 + np.int64(y) * 2246822519) & 0xFFFFFFFF
    h = (h * 374761393 + np.int64(z) * 3266489917) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    h ^= h >> 16
    return (h & 0x7FFFFFFF) / 2147483647.0

def _smoothstep(t): return t * t * (3 - 2 * t)

def _value_noise3(p, seed):
    x0, y0, z0 = int(math.floor(p[0])), int(math.floor(p[1])), int(math.floor(p[2]))
    fx, fy, fz = _smoothstep(p[0] - x0), _smoothstep(p[1] - y0), _smoothstep(p[2] - z0)
    def H(dx, dy, dz): return _hash3(x0 + dx, y0 + dy, z0 + dz, seed)
    x00 = H(0,0,0) + fx * (H(1,0,0) - H(0,0,0)); x10 = H(0,1,0) + fx * (H(1,1,0) - H(0,1,0))
    x01 = H(0,0,1) + fx * (H(1,0,1) - H(0,0,1)); x11 = H(0,1,1) + fx * (H(1,1,1) - H(0,1,1))
    a = x00 + fy * (x10 - x00); b = x01 + fy * (x11 - x01)
    return a + fz * (b - a)

def _fbm3(p, octaves, seed):
    s = 0.0; amp = 1.0; freq = 1.0; norm = 0.0
    for i in range(octaves):
        s += (_value_noise3((p[0]*freq, p[1]*freq, p[2]*freq), seed + i*131) * 2 - 1) * amp
        norm += amp; amp *= 0.5; freq *= 2.1
    return s / norm

def _ridged3(p, octaves, seed):
    s = 0.0; amp = 1.0; freq = 1.0; norm = 0.0
    for i in range(octaves):
        n = _value_noise3((p[0]*freq, p[1]*freq, p[2]*freq), seed + i*131) * 2 - 1
        s += (1 - abs(n)) * amp
        norm += amp; amp *= 0.55; freq *= 2.3
    return s / norm

def icosphere(subdiv):
    t = (1 + math.sqrt(5)) / 2
    raw = [(-1,t,0),(1,t,0),(-1,-t,0),(1,-t,0),(0,-1,t),(0,1,t),
           (0,-1,-t),(0,1,-t),(t,0,-1),(t,0,1),(-t,0,-1),(-t,0,1)]
    V = [np.array(v, float) / np.linalg.norm(v) for v in raw]
    T = [(0,11,5),(0,5,1),(0,1,7),(0,7,10),(0,10,11),(1,5,9),(5,11,4),(11,10,2),
         (10,7,6),(7,1,8),(3,9,4),(3,4,2),(3,2,6),(3,6,8),(3,8,9),(4,9,5),
         (2,4,11),(6,2,10),(8,6,7),(9,8,1)]
    for _ in range(subdiv):
        cache = {}; newT = []
        def mid(a, b):
            k = (min(a,b), max(a,b))
            if k in cache: return cache[k]
            v = (V[a] + V[b]) / 2; V.append(v / np.linalg.norm(v))
            cache[k] = len(V) - 1; return cache[k]
        for (a,b,c) in T:
            ab, bc, ca = mid(a,b), mid(b,c), mid(c,a)
            newT += [(a,ab,ca),(b,bc,ab),(c,ca,bc),(ab,bc,ca)]
        T = newT
    return V, T

class RockStyle:
    """Faithful port of ProceduralMeshLibrary.RockStyle."""
    def __init__(self, rng, seed, flatness=0.2):
        L = lambda a, b: a + (b - a) * rng.next_double()
        self.seed = seed
        wide = L(0.85, 1.35)
        tall = L(0.7, 1.15) + (L(0.45, 0.7) - L(0.7, 1.15)) * flatness
        self.axes = np.array([wide, tall, L(0.8, 1.3) * wide])
        self.broad_freq = L(1.0, 1.7);  self.broad_amp = L(0.26, 0.40)
        self.detail_freq = L(5, 8);     self.detail_amp = L(0.06, 0.11)
        self.crease_freq = L(2.0, 3.4); self.crease_amp = L(0.10, 0.17)
        self.strata_count = L(5, 10) * (1 + 0.6 * flatness)
        self.strata_amp = L(0.09, 0.16) * (1 + 0.7 * flatness)
        dip = L(0.03, 0.16); yaw = L(0, 2 * math.pi)
        bu = np.array([math.cos(yaw) * dip, 1.0, math.sin(yaw) * dip])
        self.bedding_up = bu / np.linalg.norm(bu)
        self.fractures = self._build_fractures(rng, 6 + rng.next_int(0, 5))

    def _build_fractures(self, rng, count):
        planes = []
        for _ in range(count):
            n = np.array([rng.next_double()*2-1, rng.next_double()*2-0.6, rng.next_double()*2-1])
            if np.dot(n, n) < 1e-5: n = np.array([1.0, 0, 0])
            n /= np.linalg.norm(n)
            extent = np.linalg.norm(self.axes * n)
            planes.append((n, extent * (0.52 + (0.86 - 0.52) * rng.next_double())))
        return planes

    def strata_push(self, p, outward):
        if self.strata_amp <= 0: return np.zeros(3)
        h = float(np.dot(p, self.bedding_up)) * self.strata_count
        band = math.floor(h); f = h - band
        hardness = _hash3(int(round(band)), 17, 3, self.seed) * 2 - 1
        lip = 0.35 + 0.65 * (1 - f)
        lat = np.array([outward[0], 0.0, outward[2]])
        if np.dot(lat, lat) < 1e-6: lat = outward
        lat = lat / np.linalg.norm(lat)
        return lat * (self.strata_amp * hardness * lip)

    def fracture(self, p):
        for n, w in self.fractures:
            over = float(np.dot(p, n)) - w
            if over > 0: p = p - n * over
        return p

    def shape(self, d, use_disp=True, use_strata=True, use_fract=True):
        p = d * self.axes
        if use_disp:
            r = (1.0
                 + self.broad_amp * _fbm3(tuple(d * self.broad_freq), 3, self.seed)
                 + self.detail_amp * _fbm3(tuple(d * self.detail_freq), 2, self.seed + 401)
                 + self.crease_amp * (_ridged3(tuple(d * self.crease_freq), 3, self.seed + 907) - 0.55))
            p = p * max(0.25, r)
        if use_strata: p = p + self.strata_push(p, d)
        if use_fract:  p = self.fracture(p)
        return p

    @staticmethod
    def seat_base(p, y=-0.18, flare=0.14):
        if p[1] >= y: return p
        under = y - p[1]
        p = p.copy(); p[1] = y - under * 0.15
        lat = np.array([p[0], 0.0, p[2]])
        if np.dot(lat, lat) > 1e-6:
            p = p + lat / np.linalg.norm(lat) * (flare * under)
        return p

def render_mesh(verts, tris, size=460, ss=3, yaw=0.5, pitch=0.35,
                bg=(232, 238, 240), ao_fn=None):
    """Flat-shaded painter's-algorithm rasteriser (mirrors the flat-shaded meshes)."""
    from PIL import ImageDraw
    S = size * ss
    img = Image.new("RGB", (S, S), bg)
    dr = ImageDraw.Draw(img)

    cy, sy = math.cos(yaw), math.sin(yaw)
    cp, sp = math.cos(pitch), math.sin(pitch)
    Ry = np.array([[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]])
    Rx = np.array([[1, 0, 0], [0, cp, -sp], [0, sp, cp]])
    R = Rx @ Ry

    P = np.array([R @ v for v in verts])
    scale = S * 0.36 / max(np.abs(P[:, :3]).max(), 1e-6)
    L = np.array([0.4, 0.75, 0.5]); L /= np.linalg.norm(L)

    faces = []
    for (a, b, c) in tris:
        pa, pb, pc = P[a], P[b], P[c]
        n = np.cross(pb - pa, pc - pa)
        ln = np.linalg.norm(n)
        if ln < 1e-12: continue
        n /= ln
        if n[2] < 0: continue                        # backface cull
        depth = (pa[2] + pb[2] + pc[2]) / 3
        faces.append((depth, pa, pb, pc, n, (a, b, c)))
    faces.sort(key=lambda f: f[0])

    for depth, pa, pb, pc, n, idx in faces:
        # Wrapped diffuse + a dim fill from below, as in Custom/UnderwaterRock.
        key = max(0.0, float(np.dot(n, L)))
        fill = max(0.0, float(np.dot(n, np.array([-0.4, -0.2, 0.5]))))
        lam = 0.34 + 0.62 * key + 0.14 * fill
        ao = 1.0
        if ao_fn is not None:
            mid = (verts[idx[0]] + verts[idx[1]] + verts[idx[2]]) / 3
            ao = 0.55 + 0.45 * ao_fn(mid)
        base = np.array([0.82, 0.80, 0.76])
        col = np.clip(base * lam * ao, 0, 1)
        pts = [(S/2 + p[0]*scale, S/2 - p[1]*scale) for p in (pa, pb, pc)]
        dr.polygon(pts, fill=tuple((col * 255).astype(int)))

    return np.array(img.resize((size, size), Image.LANCZOS))

def fig_rock():
    V, T = icosphere(3)
    rng = UnityRandom(4242)
    style = RockStyle(rng, seed=4242, flatness=0.2)
    ao = lambda p: min(1.0, max(0.0, 0.6 + p[1] * 0.9))

    stages = [
        ("f3a_ellipsoid.png", dict(use_disp=False, use_strata=False, use_fract=False)),
        ("f3b_displaced.png", dict(use_disp=True,  use_strata=False, use_fract=False)),
        ("f3c_bedded.png",    dict(use_disp=True,  use_strata=True,  use_fract=False)),
        ("f3d_fractured.png", dict(use_disp=True,  use_strata=True,  use_fract=True)),
    ]
    for name, flags in stages:
        verts = [RockStyle.seat_base(style.shape(np.array(d), **flags) * 0.5) for d in V]
        save_png(render_mesh(verts, T, ao_fn=ao), name)

# ═════════════════════════════════════════════════════════════════════════════
# FIG 4 — Vogel/sunflower mound packing (TerrainDetailScatter.TryBuildMound)
# ═════════════════════════════════════════════════════════════════════════════
def fig_vogel():
    GOLDEN = 2.39996323
    rng = UnityRandom(1337)
    count, R, dome = 16, 1.6, 0.5
    ang_jit = rng.next_double() * math.pi * 2
    W = 520; pad = 40
    parts = [f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {W}" width="{W}" height="{W}">',
             f'<rect width="{W}" height="{W}" fill="#f7f9fa"/>']
    cx = cy = W / 2
    px_per_m = (W / 2 - pad) / R
    parts.append(f'<circle cx="{cx}" cy="{cy}" r="{R*px_per_m:.1f}" fill="#e8eef0" stroke="#b9c6cb" stroke-dasharray="5 4"/>')
    for i in range(count):
        t = (i + 0.5) / count
        rr = R * math.sqrt(t) * (0.82 + 0.18 * rng.next_double())
        a = i * GOLDEN + ang_jit
        ox, oz = math.cos(a) * rr, math.sin(a) * rr
        edge = rr / R
        lift = dome * (1 - edge * edge)
        boost = 1.4 + (1.0 - 1.4) * edge
        x = cx + ox * px_per_m; y = cy + oz * px_per_m
        rad = 7 * boost + 5 * lift
        shade = int(90 + 90 * edge)
        parts.append(f'<circle cx="{x:.1f}" cy="{y:.1f}" r="{rad:.1f}" '
                     f'fill="rgb({shade},{min(255,shade+70)},{shade+40})" fill-opacity="0.82" stroke="#2c5f57" stroke-width="1"/>')
        parts.append(f'<text x="{x:.1f}" y="{y+3.5:.1f}" font-family="Helvetica" font-size="9" '
                     f'fill="#12302c" text-anchor="middle">{i}</text>')
    parts.append(f'<text x="{cx}" y="{W-14}" font-family="Helvetica" font-size="12" fill="#4a5f66" '
                 f'text-anchor="middle">r = R&#8730;t , &#952; = i&#183;2.39996 rad — radius {R} m, {count} members</text>')
    parts.append('</svg>')
    open(os.path.join(OUT, "f4_vogel.svg"), "w").write("\n".join(parts))
    print("wrote f4_vogel.svg")

# ═════════════════════════════════════════════════════════════════════════════
# FIG 5 — Curve plots: terrace gain, ridge fold, height curves
# ═════════════════════════════════════════════════════════════════════════════
def _plot(fn_list, name, xlabel, ylabel, title, W=520, H=300, xr=(0, 1), yr=(0, 1), legend='right'):
    pad_l, pad_b, pad_t, pad_r = 52, 44, 30, 14
    pw, ph = W - pad_l - pad_r, H - pad_b - pad_t
    def X(v): return pad_l + (v - xr[0]) / (xr[1] - xr[0]) * pw
    def Y(v): return pad_t + ph - (v - yr[0]) / (yr[1] - yr[0]) * ph
    p = [f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}">',
         f'<rect width="{W}" height="{H}" fill="#ffffff"/>']
    for i in range(6):
        gv = yr[0] + (yr[1] - yr[0]) * i / 5
        p.append(f'<line x1="{pad_l}" y1="{Y(gv):.1f}" x2="{pad_l+pw}" y2="{Y(gv):.1f}" stroke="#e6ebee"/>')
        p.append(f'<text x="{pad_l-8}" y="{Y(gv)+4:.1f}" font-family="Helvetica" font-size="10" fill="#7b8b92" text-anchor="end">{gv:.2g}</text>')
    for i in range(6):
        gx = xr[0] + (xr[1] - xr[0]) * i / 5
        p.append(f'<text x="{X(gx):.1f}" y="{pad_t+ph+16}" font-family="Helvetica" font-size="10" fill="#7b8b92" text-anchor="middle">{gx:.2g}</text>')
    p.append(f'<line x1="{pad_l}" y1="{pad_t+ph}" x2="{pad_l+pw}" y2="{pad_t+ph}" stroke="#9aa8ae"/>')
    p.append(f'<line x1="{pad_l}" y1="{pad_t}" x2="{pad_l}" y2="{pad_t+ph}" stroke="#9aa8ae"/>')
    xs = np.linspace(xr[0], xr[1], 400)
    legend = []
    for (fn, colour, label) in fn_list:
        ys = fn(xs)
        pts = " ".join(f"{X(a):.1f},{Y(b):.1f}" for a, b in zip(xs, np.clip(ys, yr[0], yr[1])))
        p.append(f'<polyline points="{pts}" fill="none" stroke="{colour}" stroke-width="2.2"/>')
        legend.append((colour, label))
    for i, (colour, label) in enumerate(legend):
        ly = pad_t + 6 + i * 16
        lx = (pad_l + pw - 116) if legend == 'right' else (pad_l + 12)
        p.append(f'<rect x="{lx-4}" y="{ly-8}" width="128" height="15" fill="#ffffff" fill-opacity="0.82"/>')
        p.append(f'<line x1="{lx}" y1="{ly}" x2="{lx+20}" y2="{ly}" stroke="{colour}" stroke-width="2.2"/>')
        p.append(f'<text x="{lx+26}" y="{ly+4}" font-family="Helvetica" font-size="10.5" fill="#33474f">{label}</text>')
    p.append(f'<text x="{W/2}" y="{16}" font-family="Helvetica" font-size="12.5" font-weight="bold" fill="#22343b" text-anchor="middle">{title}</text>')
    p.append(f'<text x="{W/2}" y="{H-6}" font-family="Helvetica" font-size="11" fill="#5b6f77" text-anchor="middle">{xlabel}</text>')
    p.append(f'<text x="14" y="{pad_t+ph/2}" font-family="Helvetica" font-size="11" fill="#5b6f77" text-anchor="middle" transform="rotate(-90 14 {pad_t+ph/2})">{ylabel}</text>')
    p.append('</svg>')
    open(os.path.join(OUT, name), "w").write("\n".join(p))
    print("wrote", name)

def fig_curves():
    _plot([(lambda x: terrace(x, 5, s), c, f"sharpness = {s}")
           for s, c in [(1.0, "#c2ccd1"), (2.0, "#5aa7c4"), (4.0, "#1f6f8b"), (8.0, "#0d3b4c")]],
          "f5a_terrace.svg", "input height h", "terraced height",
          "Terrace gain curve — 5 steps (TerrainShaper.Terrace)", legend='left')

    _plot([(lambda n: 1 - np.abs(n * 2 - 1), "#c2ccd1", "s = 1 (fold only)"),
           (lambda n: np.power(1 - np.abs(n * 2 - 1), 2.2), "#1f6f8b", "s = 2.2 (shipped)"),
           (lambda n: np.power(1 - np.abs(n * 2 - 1), 4.0), "#0d3b4c", "s = 4.0")],
          "f5b_ridge.svg", "raw Perlin value n", "ridge contribution",
          "Ridge fold (1-|2n-1|)^s — the crest former")

    _plot([(lambda t: eval_curve(CURVE_CLASSIC, t), "#d98c3f", "Classic (dunes)"),
           (lambda t: eval_curve(CURVE_CANYONS, t), "#1f6f8b", "Canyons (mesas)")],
          "f5c_heightcurves.svg", "normalised noise", "height multiplier fraction",
          "BiomeStyles height remap curves")

    # Per-channel transmittance, UnderwaterCommon.hlsl: T = exp(-(d*a)^2)
    water = np.array([0.015, 0.46, 0.595])
    peak = water.max()
    hue = np.clip(water / peak, 0, 1)
    def T(ch, tint):
        a = 0.055 * (1 + tint * (1 - hue[ch]))
        return lambda d: np.exp(-np.power(d * a, 2.0))
    _plot([(T(0, 2.0), "#c0392b", "red   (absorbTint 2)"),
           (T(1, 2.0), "#27834a", "green (absorbTint 2)"),
           (T(2, 2.0), "#1f6f8b", "blue  (absorbTint 2)"),
           (T(0, 0.0), "#b9c6cb", "all channels (absorbTint 0)")],
          "f5d_absorption.svg", "distance d (m)", "transmittance T",
          "Per-channel absorption — red dies ~3x faster than blue",
          xr=(0, 30), yr=(0, 1))

if __name__ == "__main__":
    fig_terrain()
    fig_ablation()
    fig_rock()
    fig_vogel()
    fig_curves()
    print("\nall figures in", OUT)
