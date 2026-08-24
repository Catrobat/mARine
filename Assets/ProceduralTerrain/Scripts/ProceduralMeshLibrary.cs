using UnityEngine;
using System.Collections.Generic;

// Builds the low-poly prop meshes the biomes scatter around: kelp, seagrass, glow
// anemones, sea fans and coral nubs.
public static class ProceduralMeshLibrary
{
    // Values are explicit and must stay put:
    public enum FeatureKind
    {
        KelpPlant   = 5, // fan of tall ribbons (sways in the vertex shader)
        GlowAnemone = 6, // squat dome with emissive tentacle tips
        SeaFan      = 7, // branching gorgonian fan (the classic reef silhouette)
        CoralNub    = 8, // finger-sized colony, used as ground/coral detail
        SeagrassTuft= 9  // short arcing blades; scattered densely it reads as a bed
    }

    // Nominal size ~1 m tall; the scatter rescales via renderer bounds.
    public static Mesh Build(FeatureKind kind, int seed, int jointSeed = 0)
    {
        var rng = new System.Random(seed);
        if (jointSeed == 0) jointSeed = seed;
        switch (kind)
        {
            case FeatureKind.KelpPlant:   return BuildKelpPlant(rng, seed);
            case FeatureKind.GlowAnemone: return BuildGlowAnemone(rng, seed);
            case FeatureKind.SeaFan:      return BuildSeaFan(rng, seed);
            case FeatureKind.CoralNub:    return BuildCoralNub(rng, seed);
            case FeatureKind.SeagrassTuft:return BuildSeagrassTuft(rng, seed, jointSeed);
            default:
                // Retired rock ids land here. Fall back rather than return null,
                // which the scatter would instantiate as an empty renderer.
                Debug.LogWarning($"ProceduralMeshLibrary: unknown FeatureKind "
                                 + $"{(int)kind}; rocks were removed. Using CoralNub.");
                return BuildCoralNub(rng, seed);
        }
    }

    // ── Reef life ────────────────────────────────────────────────────────────

    // A clump of soft cushions, merged into one mesh so hundreds cost a few dozen tris.
    static Mesh BuildCoralNub(System.Random rng, int seed)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();

        // Triangle count is multiplied by hundreds of colonies per rock, so it is the most
        // expensive number in this file.
        int cushions = 1 + rng.Next(2);
        const int segs = 6;
        const int rings = 2;

        for (int c = 0; c < cushions; c++)
        {
            // Cushions after the first sit beside the leader and slightly lower, so a
            // clump reads as one spreading colony rather than as separate balls.
            float offAng = Lerp(rng, 0f, Mathf.PI * 2f);
            float offDist = c == 0 ? 0f : Lerp(rng, 0.35f, 0.75f);
            Vector3 centre = new Vector3(Mathf.Cos(offAng) * offDist, c == 0 ? 0f : Lerp(rng, -0.12f, 0f),
                                         Mathf.Sin(offAng) * offDist);
            float width = c == 0 ? 1f : Lerp(rng, 0.55f, 0.85f);
            float height = width * Lerp(rng, 0.38f, 0.55f);   // squat, never a spike

            int baseIndex = verts.Count;
            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings;
                // Quarter-sine profile: full width at the rock, doming over to the top.
                float radius = width * Mathf.Cos(t * Mathf.PI * 0.5f);
                float y = height * Mathf.Sin(t * Mathf.PI * 0.5f);
                for (int s = 0; s < segs; s++)
                {
                    float sa = s / (float)segs * Mathf.PI * 2f;
                    // A little per-vertex wobble so a field of cushions is not a field
                    // of identical domes.
                    float wob = 1f + 0.16f * Fbm(new Vector3(Mathf.Cos(sa) * 2.1f, t * 1.7f,
                                                             Mathf.Sin(sa) * 2.1f), 2, seed + c * 331);
                    verts.Add(centre + new Vector3(Mathf.Cos(sa) * radius * wob, y,
                                                   Mathf.Sin(sa) * radius * wob));
                }
            }
            int apex = verts.Count;
            verts.Add(centre + new Vector3(0f, height, 0f));

            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segs; s++)
                {
                    int s1 = (s + 1) % segs;
                    int a0 = baseIndex + r * segs + s;
                    int a1 = baseIndex + r * segs + s1;
                    int b0 = baseIndex + (r + 1) * segs + s;
                    int b1 = baseIndex + (r + 1) * segs + s1;
                    tris.Add(a0); tris.Add(b0); tris.Add(b1);
                    tris.Add(a0); tris.Add(b1); tris.Add(a1);
                }
            for (int s = 0; s < segs; s++)
            {
                int s1 = (s + 1) % segs;
                tris.Add(baseIndex + rings * segs + s);
                tris.Add(apex);
                tris.Add(baseIndex + rings * segs + s1);
            }
        }

        // AO darkens toward the base so a clump reads as sitting in a shared mass
        // rather than as separate objects balanced on the rock.
        float species = (float)rng.NextDouble();
        return FinishSmooth(verts, tris, seed,
            ao: p => Mathf.Clamp01(0.55f + p.y * 0.9f), species: species);
    }

    // ── Life ─────────────────────────────────────────────────────────────────

    static Mesh BuildKelpPlant(System.Random rng, int seed)
    {
        var d = new Draft();
        int blades = 3 + rng.Next(3);
        int segsY = 7;

        for (int b = 0; b < blades; b++)
        {
            float yaw = (b / (float)blades) * Mathf.PI * 2f + Lerp(rng, -0.3f, 0.3f);
            float bladeH = Lerp(rng, 0.75f, 1f);
            float w0 = Lerp(rng, 0.05f, 0.08f);
            float drift = Lerp(rng, 0.06f, 0.16f);
            Vector3 right = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
            Vector3 fwd = new Vector3(-Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
            float hue = Lerp(rng, -0.5f, 0.5f);
            Color tint = new Color(1f + hue * 0.12f, 1f, 1f - hue * 0.1f, 1f);

            var prev = new Vector3[2];
            var prevUV = new Vector2[2];
            for (int i = 0; i <= segsY; i++)
            {
                float t = i / (float)segsY;
                float width = w0 * (1f - t * 0.55f);
                // Gentle S-curve so blades aren't rigid poles even at rest.
                Vector3 centre = fwd * (drift * Mathf.Sin(t * 3.1f + b)) + right * (drift * 0.4f * Mathf.Sin(t * 5.3f + seed))
                               + Vector3.up * (t * bladeH);
                Vector3 a = centre - right * width;
                Vector3 c = centre + right * width;
                var uvA = new Vector2(0f, t);
                var uvC = new Vector2(1f, t);

                if (i > 0)
                {
                    d.QuadUV(prev[0], prev[1], c, a, prevUV[0], prevUV[1], uvC, uvA, tint);
                }
                prev[0] = a; prev[1] = c;
                prevUV[0] = uvA; prevUV[1] = uvC;
            }
        }

        // Smooth normals suit ribbons; uv.y drives sway + colour ramp in shader.
        return d.ToSmoothMesh();
    }

    // A seagrass shoot: many short blades arcing over from a near-point base.
    // ~110 triangles, because the effect is density — see tools/preview_seagrass.py.
    static Mesh BuildSeagrassTuft(System.Random rng, int seed, int jointSeed)
    {
        var d = new Draft();
        int blades = 9 + rng.Next(5);
        int segsY = 5;

        // The current belongs to the patch, not the shoot.
        float current = (float)(new System.Random(jointSeed).NextDouble()) * Mathf.PI * 2f;
        float leanDir = current + Lerp(rng, -0.45f, 0.45f);

        for (int b = 0; b < blades; b++)
        {
            float yaw = leanDir + Lerp(rng, -0.5f, 0.5f);
            // Long blades dominate so they overlap and close the gaps between shoots.
            float t01 = (float)rng.NextDouble();
            float bladeH = 0.70f + 0.30f * Mathf.Pow(t01, 1.2f);
            float bend = Lerp(rng, 0.55f, 1.05f);
            float w0 = Lerp(rng, 0.038f, 0.058f);

            Vector3 lean = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
            Vector3 side = new Vector3(-Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
            Vector3 root = lean * Lerp(rng, 0f, 0.055f) + side * Lerp(rng, -0.055f, 0.055f);

            float hue = Lerp(rng, -0.5f, 0.5f);
            Color tint = new Color(1f + hue * 0.10f, 1f, 1f - hue * 0.12f, 1f);

            var prev = new Vector3[2];
            var prevUV = new Vector2[2];
            for (int i = 0; i <= segsY; i++)
            {
                float t = i / (float)segsY;
                float y = bladeH * (t - 0.30f * t * t * t * bend);
                float lat = bladeH * bend * (0.28f * t + 0.72f * Mathf.Pow(t, 1.7f));
                float swell = Mathf.Sin(Mathf.Min(t, 0.30f) / 0.30f * 1.5708f);
                float width = w0 * (0.42f + 0.62f * swell - 0.46f * t);

                Vector3 centre = root + lean * lat + Vector3.up * y;
                Vector3 a = centre - side * width;
                Vector3 c = centre + side * width;
                var uvA = new Vector2(0f, t);
                var uvC = new Vector2(1f, t);

                if (i > 0)
                    d.QuadUV(prev[0], prev[1], c, a, prevUV[0], prevUV[1], uvC, uvA, tint);

                prev[0] = a; prev[1] = c;
                prevUV[0] = uvA; prevUV[1] = uvC;
            }
        }

        // Same contract as kelp: uv.y drives sway and the root-to-tip ramp, so this
        // renders on UnderwaterKelp unchanged.
        return d.ToSmoothMesh();
    }

    static Mesh BuildGlowAnemone(System.Random rng, int seed)
    {
        List<Vector3> verts; List<int> tris;
        Icosphere(1, out verts, out tris);

        var d = new Draft();
        for (int i = 0; i < tris.Count; i += 3)
        {
            Vector3 a = Squash(verts[tris[i]]), b = Squash(verts[tris[i + 1]]), c = Squash(verts[tris[i + 2]]);
            d.Tri(a, b, c);
        }

        // Tentacle spikes on the upper half; their tips carry the glow mask.
        int spikes = 8 + rng.Next(7);
        for (int s = 0; s < spikes; s++)
        {
            float yaw = Lerp(rng, 0f, Mathf.PI * 2f);
            float pitch = Lerp(rng, 0.35f, 1.35f);
            Vector3 dir = new Vector3(
                Mathf.Cos(yaw) * Mathf.Cos(pitch), Mathf.Sin(pitch), Mathf.Sin(yaw) * Mathf.Cos(pitch)).normalized;
            Vector3 baseP = Squash(dir) * 0.92f;
            Vector3 tip = baseP + dir * Lerp(rng, 0.22f, 0.42f);
            Vector3 side = Vector3.Cross(dir, Vector3.up).normalized * 0.035f;
            if (side.sqrMagnitude < 1e-6f) side = Vector3.right * 0.035f;
            Vector3 side2 = Vector3.Cross(dir, side).normalized * 0.035f;

            // Tiny 3-sided pyramid per tentacle; glow mask 1 at the tip.
            d.TriGlow(baseP + side, baseP - side * 0.5f + side2, tip, 0.1f, 0.1f, 1f);
            d.TriGlow(baseP - side * 0.5f + side2, baseP - side * 0.5f - side2, tip, 0.1f, 0.1f, 1f);
            d.TriGlow(baseP - side * 0.5f - side2, baseP + side, tip, 0.1f, 0.1f, 1f);
        }

        return d.ToFlatMesh(seed, ao: p => 1f, glowIsBaked: true);

        Vector3 Squash(Vector3 v)
        {
            Vector3 r = v.normalized * 0.5f;
            r.y *= 0.55f;
            if (r.y < 0f) r.y *= 0.35f; // sit nearly flat on the ground
            return r;
        }
    }

    // Gorgonian sea fan: a mostly-planar recursive branch structure, like the pink fans
    // silhouetted against the water in every reef shot.
    static Mesh BuildSeaFan(System.Random rng, int seed)
    {
        var d = new Draft();
        int trunks = 3 + rng.Next(3);
        float spread = Lerp(rng, 0.55f, 0.85f);

        void Branch(Vector3 pos, float angle, float len, float width, int depth)
        {
            if (depth == 0 || width < 0.004f) return;

            Vector3 dir = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f);
            Vector3 end = pos + dir * len;
            end.z += Lerp(rng, -0.025f, 0.025f);  // slight out-of-plane wobble

            Vector3 side = new Vector3(-dir.y, dir.x, 0f) * width;
            Vector3 sideEnd = side * 0.6f;        // taper toward the tip
            d.Quad(pos - side, pos + side, end + sideEnd, end - sideEnd);
            d.Quad(end - sideEnd, end + sideEnd, pos + side, pos - side); // back face

            int kids = 2 + (rng.Next(100) < 35 ? 1 : 0);
            for (int k = 0; k < kids; k++)
            {
                float t = kids == 1 ? 0f : (k / (float)(kids - 1)) * 2f - 1f;
                float childAngle = angle + t * spread * Lerp(rng, 0.6f, 1f);
                Branch(end, childAngle, len * Lerp(rng, 0.58f, 0.72f), width * 0.62f, depth - 1);
            }
        }

        for (int b = 0; b < trunks; b++)
        {
            float t = trunks == 1 ? 0f : (b / (float)(trunks - 1)) * 2f - 1f;
            Branch(new Vector3(t * 0.03f, 0f, 0f), t * spread * 0.8f,
                   Lerp(rng, 0.3f, 0.38f), Lerp(rng, 0.02f, 0.03f), 5);
        }

        // Fans brighten toward the tips, like light shining through the mesh.
        return d.ToFlatMesh(seed, ao: p => Mathf.Clamp01(0.55f + p.y * 0.5f));
    }

    // Converts an indexed mesh (e.g. a displaced icosphere) into a flat-shaded
    // Draft and bakes it.
    static Mesh FinishSmooth(List<Vector3> verts, List<int> tris, int seed,
                             System.Func<Vector3, float> ao, float species)
    {
        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < verts.Count; i++)
        {
            if (verts[i].y < minY) minY = verts[i].y;
            if (verts[i].y > maxY) maxY = verts[i].y;
        }
        float span = Mathf.Max(1e-4f, maxY - minY);

        var uv = new Vector2[verts.Count];
        var cols = new Color[verts.Count];
        for (int i = 0; i < verts.Count; i++)
        {
            float h = (verts[i].y - minY) / span;
            uv[i] = new Vector2(species, h);
            cols[i] = new Color(1f, 1f, 1f, Mathf.Clamp01(ao(verts[i])));
        }

        var mesh = new Mesh { name = "SmoothFeature" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, new List<Vector2>(uv));
        mesh.SetColors(new List<Color>(cols));
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ── Draft: triangle-soup builder ─────────────────────────────────────────

    class Draft
    {
        public readonly List<Vector3> V = new List<Vector3>();
        public readonly List<int> T = new List<int>();
        public readonly List<Vector2> UV = new List<Vector2>();
        public readonly List<Color> C = new List<Color>();

        public void Tri(Vector3 a, Vector3 b, Vector3 c)
        {
            int i = V.Count;
            V.Add(a); V.Add(b); V.Add(c);
            UV.Add(Vector2.zero); UV.Add(Vector2.zero); UV.Add(Vector2.zero);
            C.Add(Color.white); C.Add(Color.white); C.Add(Color.white);
            T.Add(i); T.Add(i + 1); T.Add(i + 2);
        }

        public void TriGlow(Vector3 a, Vector3 b, Vector3 c, float ga, float gb, float gc)
        {
            int i = V.Count;
            V.Add(a); V.Add(b); V.Add(c);
            UV.Add(Vector2.zero); UV.Add(Vector2.zero); UV.Add(Vector2.zero);
            C.Add(new Color(1f, 1f, 1f, ga)); C.Add(new Color(1f, 1f, 1f, gb)); C.Add(new Color(1f, 1f, 1f, gc));
            T.Add(i); T.Add(i + 1); T.Add(i + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Tri(a, b, c);
            Tri(a, c, d);
        }

        public void QuadUV(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                           Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD, Color col)
        {
            int i = V.Count;
            V.Add(a); V.Add(b); V.Add(c); V.Add(d);
            UV.Add(uvA); UV.Add(uvB); UV.Add(uvC); UV.Add(uvD);
            C.Add(col); C.Add(col); C.Add(col); C.Add(col);
            T.Add(i); T.Add(i + 1); T.Add(i + 2);
            T.Add(i); T.Add(i + 2); T.Add(i + 3);
        }

        // Flat-shaded output: verts already duplicated per triangle, so face normals come
        // free. AO callback bakes into vertex alpha;
        public Mesh ToFlatMesh(int seed, System.Func<Vector3, float> ao, bool glowIsBaked = false)
        {
            var normals = new Vector3[V.Count];
            var colors = new Color[V.Count];

            for (int i = 0; i < T.Count; i += 3)
            {
                Vector3 n = Vector3.Cross(V[T[i + 1]] - V[T[i]], V[T[i + 2]] - V[T[i]]).normalized;
                normals[T[i]] = normals[T[i + 1]] = normals[T[i + 2]] = n;
            }

            for (int i = 0; i < V.Count; i++)
            {
                float tint = 1f + 0.08f * Fbm(V[i] * 3.7f, 2, seed + 91);
                float a = glowIsBaked ? C[i].a : Mathf.Clamp01(ao(V[i]));
                colors[i] = new Color(C[i].r * tint, C[i].g * tint, C[i].b * tint, a);
            }

            return Bake(normals, colors);
        }

        public Mesh ToSmoothMesh()
        {
            var mesh = Bake(null, C.ToArray());
            mesh.RecalculateNormals();
            return mesh;
        }

        Mesh Bake(Vector3[] normals, Color[] colors)
        {
            var mesh = new Mesh();
            mesh.SetVertices(V);
            mesh.SetTriangles(T, 0);
            mesh.SetUVs(0, UV);
            mesh.SetColors(colors);
            if (normals != null) mesh.SetNormals(normals);
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    // ── Primitives & noise ───────────────────────────────────────────────────

    static void Icosphere(int subdivisions, out List<Vector3> verts, out List<int> tris)
    {
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        verts = new List<Vector3>
        {
            new Vector3(-1,  t, 0).normalized, new Vector3( 1,  t, 0).normalized,
            new Vector3(-1, -t, 0).normalized, new Vector3( 1, -t, 0).normalized,
            new Vector3(0, -1,  t).normalized, new Vector3(0,  1,  t).normalized,
            new Vector3(0, -1, -t).normalized, new Vector3(0,  1, -t).normalized,
            new Vector3( t, 0, -1).normalized, new Vector3( t, 0,  1).normalized,
            new Vector3(-t, 0, -1).normalized, new Vector3(-t, 0,  1).normalized,
        };
        tris = new List<int>
        {
            0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
            1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
            4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1,
        };

        for (int s = 0; s < subdivisions; s++)
        {
            var midCache = new Dictionary<long, int>();
            var newTris = new List<int>(tris.Count * 4);
            for (int i = 0; i < tris.Count; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                int ab = Midpoint(verts, midCache, a, b);
                int bc = Midpoint(verts, midCache, b, c);
                int ca = Midpoint(verts, midCache, c, a);
                newTris.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            tris = newTris;
        }
    }

    static int Midpoint(List<Vector3> verts, Dictionary<long, int> cache, int a, int b)
    {
        long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        if (cache.TryGetValue(key, out int idx)) return idx;
        verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
        cache[key] = verts.Count - 1;
        return verts.Count - 1;
    }

    // Deterministic lattice value noise (independent of UnityEngine.Random).
    static float Hash(int x, int y, int z, int seed)
    {
        unchecked
        {
            int h = seed;
            h = h * 374761393 + x * 668265263;
            h = h * 374761393 + y * unchecked((int)2246822519);
            h = h * 374761393 + z * unchecked((int)3266489917);
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0x7fffffff) / (float)int.MaxValue;
        }
    }

    static float ValueNoise(Vector3 p, int seed)
    {
        int x0 = Mathf.FloorToInt(p.x), y0 = Mathf.FloorToInt(p.y), z0 = Mathf.FloorToInt(p.z);
        float fx = SmoothStep(p.x - x0), fy = SmoothStep(p.y - y0), fz = SmoothStep(p.z - z0);

        float v000 = Hash(x0, y0, z0, seed),     v100 = Hash(x0 + 1, y0, z0, seed);
        float v010 = Hash(x0, y0 + 1, z0, seed), v110 = Hash(x0 + 1, y0 + 1, z0, seed);
        float v001 = Hash(x0, y0, z0 + 1, seed), v101 = Hash(x0 + 1, y0, z0 + 1, seed);
        float v011 = Hash(x0, y0 + 1, z0 + 1, seed), v111 = Hash(x0 + 1, y0 + 1, z0 + 1, seed);

        float x00 = Mathf.Lerp(v000, v100, fx), x10 = Mathf.Lerp(v010, v110, fx);
        float x01 = Mathf.Lerp(v001, v101, fx), x11 = Mathf.Lerp(v011, v111, fx);
        return Mathf.Lerp(Mathf.Lerp(x00, x10, fy), Mathf.Lerp(x01, x11, fy), fz);
    }

    // Signed fBm in roughly [-1, 1].
    static float Fbm(Vector3 p, int octaves, int seed)
    {
        float sum = 0f, amp = 1f, freq = 1f, norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += (ValueNoise(p * freq, seed + i * 131) * 2f - 1f) * amp;
            norm += amp;
            amp *= 0.5f;
            freq *= 2.1f;
        }
        return sum / norm;
    }

    static float SmoothStep(float t) => t * t * (3f - 2f * t);
    static float Lerp(System.Random rng, float a, float b) => Mathf.Lerp(a, b, (float)rng.NextDouble());

}
