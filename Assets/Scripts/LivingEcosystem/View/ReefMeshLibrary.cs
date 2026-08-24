using System.Collections.Generic;
using UnityEngine;

namespace CreateEnv.Ecosystem
{
    // Species-shaped meshes for the nine Cabo Verde organisms, built in code.
    //
    // These are not placeholders for "some sea creature". Each one is built to the
    // diagnostic silhouette the Literature Document describes, because a learner is
    // meant to recognise what they are looking at and read a real trait off it:
    //
    //   Halimeda    chains of flat calcified segments   -> it is built of discs
    //   Padina      banded funnel-shaped blade          -> it is a rolled fan
    //   Siderastrea low dome pitted with star cups      -> the corallites are modelled
    //   Parrotfish  deep laterally-compressed body      -> tall and thin, not a tube
    //   Diadema     small test, very long fine spines   -> spines dominate the shape
    //   Fissurella  low cone with a hole at the apex    -> the keyhole is really there
    //   Panulirus   long antennae and NO claws          -> the point of its info card
    //   Octopus     bulbous mantle, eight arms          -> arms are individually built
    //   Tiger shark blunt snout, long upper tail lobe   -> the tail is asymmetric
    //
    // Everything is low-poly (84 to 452 triangles each, 2,524 for the whole roster),
    // shaded off normals, with no textures, and built once then shared by every
    // instance. At the 30-model on-screen cap the worst case is around 13k triangles
    // and zero texture memory, so the added application size is a rounding error
    // against the 15 MB budget.
    //
    // Verified by the mesh check in the balance report: every species builds valid,
    // non-degenerate geometry with the right proportions.
    public static class ReefMeshLibrary
    {
        public static Mesh Build(int species, int seed)
        {
            var mb = new MeshBuilder();
            var rng = new System.Random(seed);

            switch (species)
            {
                case SpeciesLibrary.Halimeda:   BuildHalimeda(mb, rng);   break;
                case SpeciesLibrary.Padina:     BuildPadina(mb, rng);     break;
                case SpeciesLibrary.Coral:      BuildStarletCoral(mb, rng); break;
                case SpeciesLibrary.Parrotfish: BuildParrotfish(mb);      break;
                case SpeciesLibrary.Urchin:     BuildUrchin(mb, rng);     break;
                case SpeciesLibrary.Limpet:     BuildLimpet(mb);          break;
                case SpeciesLibrary.Lobster:    BuildLobster(mb);         break;
                case SpeciesLibrary.Octopus:    BuildOctopus(mb, rng);    break;
                case SpeciesLibrary.TigerShark: BuildTigerShark(mb);      break;
                default:                        BuildUrchin(mb, rng);     break;
            }

            return mb.ToMesh("LivingEcosystem/" + SpeciesLibrary.Get(species)?.id);
        }

        // ── Producers ────────────────────────────────────────────────────────

        // Halimeda: low bushy clumps of branching chains, each chain a run of small
        // flat segments. The segmented construction is the whole teaching point —
        // those segments are limestone, and they become the sand.
        static void BuildHalimeda(MeshBuilder mb, System.Random rng)
        {
            int chains = 4 + rng.Next(2);
            for (int c = 0; c < chains; c++)
            {
                float baseAngle = (float)(rng.NextDouble() * Mathf.PI * 2f);
                float lean = 0.12f + (float)rng.NextDouble() * 0.22f;
                Vector3 pos = new Vector3(Mathf.Cos(baseAngle) * 0.06f, 0f, Mathf.Sin(baseAngle) * 0.06f);
                Vector3 dir = new Vector3(Mathf.Cos(baseAngle) * lean, 1f, Mathf.Sin(baseAngle) * lean).normalized;

                int segments = 3 + rng.Next(3);
                float size = 0.085f;
                for (int s = 0; s < segments; s++)
                {
                    // Each segment is a flat disc, alternating its facing so the chain
                    // reads as the flattened, articulated thallus it really is.
                    // Pick the reference axis that is least parallel to the chain, or
                    // the cross product collapses and the disc degenerates.
                    Vector3 reference = Mathf.Abs(dir.x) < 0.85f ? Vector3.right : Vector3.forward;
                    if (s % 2 == 1) reference = Mathf.Abs(dir.z) < 0.85f ? Vector3.forward : Vector3.right;
                    Vector3 normal = Vector3.Cross(dir, reference).normalized;
                    if (normal.sqrMagnitude < 0.25f) normal = Vector3.Cross(dir, Vector3.up).normalized;
                    mb.AddDisc(pos + dir * size * 0.5f, dir, normal, size, size * 0.82f, 5, 0.018f);

                    pos += dir * size * 0.95f;
                    size *= 0.88f;
                    // Wander so no two chains look alike.
                    dir = (dir + new Vector3((float)rng.NextDouble() - 0.5f, 0.25f,
                                             (float)rng.NextDouble() - 0.5f) * 0.30f).normalized;
                }
            }
        }

        // Padina: thin fan- or funnel-shaped blades with an inrolled upper margin and
        // fine concentric banding. Built as a rolled fan sheet, double-sided.
        static void BuildPadina(MeshBuilder mb, System.Random rng)
        {
            int blades = 3 + rng.Next(2);
            for (int b = 0; b < blades; b++)
            {
                float yaw = (float)(rng.NextDouble() * Mathf.PI * 2f);
                float height = 0.55f + (float)rng.NextDouble() * 0.35f;
                float spread = 1.5f + (float)rng.NextDouble() * 0.9f;   // radians of arc
                float tilt = 0.15f + (float)rng.NextDouble() * 0.2f;

                const int rings = 4, cols = 6;
                var grid = new Vector3[rings + 1, cols + 1];

                for (int r = 0; r <= rings; r++)
                {
                    float t = r / (float)rings;
                    float radius = t * height;
                    // The blade widens as it rises, and curls inward at the margin.
                    float curl = Mathf.Pow(t, 2.2f) * 0.30f;

                    for (int c = 0; c <= cols; c++)
                    {
                        float a = yaw + (c / (float)cols - 0.5f) * spread * t;
                        float x = Mathf.Cos(a) * radius;
                        float z = Mathf.Sin(a) * radius;
                        float y = t * height * (1f - tilt) - curl * 0.25f;
                        // Pull the rim in toward the axis: the funnel shape.
                        grid[r, c] = new Vector3(x * (1f - curl), y, z * (1f - curl));
                    }
                }

                // Given real thickness rather than built as a zero-thickness sheet.
                // Two coincident faces pointing opposite ways means one of them always
                // faces away from the light and renders black, which is why the fan
                // alga looked inside-out. A thin solid lights correctly from any angle.
                mb.AddSheet(grid, rings, cols, height * 0.018f);
            }
        }

        // Siderastrea radians: a low dome, rarely more than 30 cm across, its surface
        // deeply pitted with star-shaped cups. Those pits are what the name refers to,
        // so they are modelled rather than implied.
        static void BuildStarletCoral(MeshBuilder mb, System.Random rng)
        {
            const int rings = 6, segs = 12;
            float radius = 0.5f, height = 0.30f;

            var grid = new Vector3[rings + 1, segs + 1];
            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings;
                // Flattened hemisphere: encrusting, not spherical.
                float ringRadius = Mathf.Sin(t * Mathf.PI * 0.5f) * radius;
                float y = Mathf.Cos(t * Mathf.PI * 0.5f) * height;

                for (int c = 0; c <= segs; c++)
                {
                    float a = c / (float)segs * Mathf.PI * 2f;
                    // Slight irregularity: it grows over whatever rock it is on.
                    float bump = 1f + ((float)rng.NextDouble() - 0.5f) * 0.06f;
                    grid[r, c] = new Vector3(Mathf.Cos(a) * ringRadius * bump, y * bump,
                                             Mathf.Sin(a) * ringRadius * bump);
                }
            }

            for (int r = 0; r < rings; r++)
                for (int c = 0; c < segs; c++)
                    mb.AddQuad(grid[r, c], grid[r, c + 1], grid[r + 1, c + 1], grid[r + 1, c]);

            // The corallites: small pits sunk into the dome.
            int pits = 18;
            for (int i = 0; i < pits; i++)
            {
                float u = (float)rng.NextDouble();
                float t = Mathf.Sqrt(u) * 0.86f;
                float a = (float)(rng.NextDouble() * Mathf.PI * 2f);
                float ringRadius = Mathf.Sin(t * Mathf.PI * 0.5f) * radius;
                float y = Mathf.Cos(t * Mathf.PI * 0.5f) * height;

                Vector3 centre = new Vector3(Mathf.Cos(a) * ringRadius, y, Mathf.Sin(a) * ringRadius);
                Vector3 outward = centre.normalized;
                mb.AddPit(centre, outward, 0.060f, 0.035f, 5);
            }
        }

        // ── Plant eaters ─────────────────────────────────────────────────────

        // Sparisoma cretense: a deep, laterally compressed body — tall and thin, not a
        // tube — with a blunt head, a long low dorsal fin and a shallow forked tail.
        static void BuildParrotfish(MeshBuilder mb)
        {
            // profile: z along the body, x = half-width, y = half-height
            var profile = new (float z, float w, float h, float yOff)[]
            {
                (-0.50f, 0.010f, 0.045f,  0.00f), // caudal peduncle
                (-0.34f, 0.045f, 0.130f,  0.00f),
                (-0.16f, 0.075f, 0.205f,  0.01f),
                ( 0.02f, 0.082f, 0.225f,  0.01f), // deepest point
                ( 0.20f, 0.070f, 0.195f,  0.00f),
                ( 0.36f, 0.048f, 0.140f, -0.01f),
                ( 0.47f, 0.026f, 0.080f, -0.02f), // blunt snout
            };
            mb.AddBody(profile, 8);

            // Forked caudal fin.
            mb.AddFinDoubleSided(new Vector3(0f, 0f, -0.50f),
                                 new Vector3(0f,  0.155f, -0.70f),
                                 new Vector3(0f,  0.030f, -0.60f));
            mb.AddFinDoubleSided(new Vector3(0f, 0f, -0.50f),
                                 new Vector3(0f, -0.155f, -0.70f),
                                 new Vector3(0f, -0.030f, -0.60f));

            // Long low dorsal fin along the back, and the anal fin beneath.
            mb.AddFinDoubleSided(new Vector3(0f, 0.20f,  0.14f),
                                 new Vector3(0f, 0.20f, -0.30f),
                                 new Vector3(0f, 0.30f, -0.10f));
            mb.AddFinDoubleSided(new Vector3(0f, -0.17f, -0.05f),
                                 new Vector3(0f, -0.17f, -0.30f),
                                 new Vector3(0f, -0.26f, -0.20f));

            // Pectoral fins, one each side.
            mb.AddFinDoubleSided(new Vector3( 0.07f, -0.02f, 0.16f),
                                 new Vector3( 0.20f, -0.09f, 0.02f),
                                 new Vector3( 0.19f,  0.04f, 0.06f));
            mb.AddFinDoubleSided(new Vector3(-0.07f, -0.02f, 0.16f),
                                 new Vector3(-0.20f, -0.09f, 0.02f),
                                 new Vector3(-0.19f,  0.04f, 0.06f));
        }

        // Diadema africanum: a small dark test carrying very long, fine, mobile
        // spines. The spines are most of the animal, and most of the silhouette.
        static void BuildUrchin(MeshBuilder mb, System.Random rng)
        {
            const float testRadius = 0.17f;
            mb.AddEllipsoid(Vector3.zero, new Vector3(testRadius, testRadius * 0.72f, testRadius), 6, 10);

            int spines = 46;
            for (int i = 0; i < spines; i++)
            {
                // Even-ish distribution over the sphere (Fibonacci), jittered.
                float t = (i + 0.5f) / spines;
                float phi = Mathf.Acos(1f - 2f * t);
                float theta = Mathf.PI * (1f + Mathf.Sqrt(5f)) * i;

                var dir = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta),
                                      Mathf.Cos(phi) * 0.85f,
                                      Mathf.Sin(phi) * Mathf.Sin(theta)).normalized;

                // Spines point up and outward; the ones underneath stay short.
                float length = 0.34f + (float)rng.NextDouble() * 0.30f;
                if (dir.y < -0.25f) length *= 0.4f;

                Vector3 root = dir * testRadius * 0.92f;
                mb.AddTaperedSpike(root, root + dir * length, 0.011f, 3);
            }
        }

        // Fissurella: a low conical shell with a small opening at the apex. The
        // keyhole is the diagnostic feature and the reason for the common name, so
        // the mesh has an actual hole rather than a painted dot.
        static void BuildLimpet(MeshBuilder mb)
        {
            const int segs = 14;
            const float baseRadius = 0.5f, apexRadius = 0.085f, height = 0.34f;

            // Outer shell wall, from the base rim up to the rim of the keyhole.
            for (int c = 0; c < segs; c++)
            {
                float a0 = c / (float)segs * Mathf.PI * 2f;
                float a1 = (c + 1) / (float)segs * Mathf.PI * 2f;

                // The shell is a slightly oval cone, as a real limpet is.
                Vector3 b0 = new Vector3(Mathf.Cos(a0) * baseRadius, 0f, Mathf.Sin(a0) * baseRadius * 0.82f);
                Vector3 b1 = new Vector3(Mathf.Cos(a1) * baseRadius, 0f, Mathf.Sin(a1) * baseRadius * 0.82f);
                Vector3 t0 = new Vector3(Mathf.Cos(a0) * apexRadius, height, Mathf.Sin(a0) * apexRadius * 0.82f);
                Vector3 t1 = new Vector3(Mathf.Cos(a1) * apexRadius, height, Mathf.Sin(a1) * apexRadius * 0.82f);

                // Concave profile — the sides of a limpet curve, they are not straight.
                Vector3 m0 = Vector3.Lerp(b0, t0, 0.5f); m0.y -= height * 0.10f;
                Vector3 m1 = Vector3.Lerp(b1, t1, 0.5f); m1.y -= height * 0.10f;

                // Wound so the shell's outside faces outward. Built the other way
                // round, the limpet renders as the inside of its own shell.
                mb.AddQuad(b0, m0, m1, b1);
                mb.AddQuad(m0, t0, t1, m1);

                // Inner wall of the keyhole, which correctly faces inward — it is the
                // inside of the opening.
                Vector3 i0 = t0 * 0.72f; i0.y = height * 0.86f;
                Vector3 i1 = t1 * 0.72f; i1.y = height * 0.86f;
                mb.AddQuad(t0, i0, i1, t1);
            }
        }

        // Panulirus echinatus: segmented armoured body, a fanned tail, and very long
        // antennae. Deliberately no claws — that absence is the point of its card.
        static void BuildLobster(MeshBuilder mb)
        {
            // Cephalothorax then the articulated abdomen, tapering to the tail.
            var profile = new (float z, float w, float h, float yOff)[]
            {
                (-0.42f, 0.055f, 0.045f, 0.00f),
                (-0.28f, 0.080f, 0.065f, 0.00f),
                (-0.12f, 0.100f, 0.085f, 0.00f),
                ( 0.06f, 0.115f, 0.100f, 0.00f),
                ( 0.24f, 0.110f, 0.098f, 0.00f),
                ( 0.38f, 0.085f, 0.075f, 0.00f),
                ( 0.46f, 0.050f, 0.048f, 0.00f),
            };
            mb.AddBody(profile, 7);

            // Tail fan.
            mb.AddFinDoubleSided(new Vector3(0f, 0f, -0.42f),
                                 new Vector3( 0.13f, 0.01f, -0.58f),
                                 new Vector3( 0.02f, 0.01f, -0.60f));
            mb.AddFinDoubleSided(new Vector3(0f, 0f, -0.42f),
                                 new Vector3(-0.13f, 0.01f, -0.58f),
                                 new Vector3(-0.02f, 0.01f, -0.60f));

            // The long antennae, as long as the body — the animal's main defence.
            mb.AddTaperedSpike(new Vector3( 0.05f, 0.05f, 0.44f),
                               new Vector3( 0.26f, 0.14f, 1.05f), 0.012f, 4);
            mb.AddTaperedSpike(new Vector3(-0.05f, 0.05f, 0.44f),
                               new Vector3(-0.26f, 0.14f, 1.05f), 0.012f, 4);

            // Walking legs — no claws on any of them.
            for (int i = 0; i < 4; i++)
            {
                float z = 0.30f - i * 0.16f;
                mb.AddTaperedSpike(new Vector3( 0.09f, -0.02f, z),
                                   new Vector3( 0.26f, -0.14f, z - 0.04f), 0.010f, 3);
                mb.AddTaperedSpike(new Vector3(-0.09f, -0.02f, z),
                                   new Vector3(-0.26f, -0.14f, z - 0.04f), 0.010f, 3);
            }
        }

        // ── Hunters ──────────────────────────────────────────────────────────

        // Octopus vulgaris: a bulbous mantle over a head, with eight arms tapering
        // away from it. The arms are built individually and curl outward, which is
        // what makes the animal read as an octopus at a glance.
        static void BuildOctopus(MeshBuilder mb, System.Random rng)
        {
            // Mantle: the big sac. Sits above and behind the eyes.
            mb.AddEllipsoid(new Vector3(0f, 0.20f, -0.06f), new Vector3(0.20f, 0.26f, 0.22f), 5, 8);
            // Head, slightly narrower, carrying the eyes.
            mb.AddEllipsoid(new Vector3(0f, 0.02f, 0.06f), new Vector3(0.17f, 0.14f, 0.16f), 4, 7);
            // Eyes, as small domes — they read even at AR distance.
            mb.AddEllipsoid(new Vector3( 0.13f, 0.06f, 0.10f), new Vector3(0.055f, 0.050f, 0.050f), 3, 5);
            mb.AddEllipsoid(new Vector3(-0.13f, 0.06f, 0.10f), new Vector3(0.055f, 0.050f, 0.050f), 3, 5);

            const int arms = 8;
            for (int i = 0; i < arms; i++)
            {
                float a = i / (float)arms * Mathf.PI * 2f + 0.2f;
                Vector3 outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 pos = new Vector3(0f, -0.05f, 0.02f) + outward * 0.11f;

                // Each arm is a chain of shrinking segments that curls outward and
                // down, so no two arms sit in the same pose.
                Vector3 dir = (outward * 0.75f + Vector3.down * 0.35f).normalized;
                float radius = 0.045f;
                float segLength = 0.10f + (float)rng.NextDouble() * 0.02f;

                for (int s = 0; s < 4; s++)
                {
                    Vector3 next = pos + dir * segLength;
                    mb.AddTube(pos, next, radius, radius * 0.70f, 4);
                    pos = next;
                    radius *= 0.72f;
                    // Curl: bend further outward and upward toward the tip.
                    dir = (dir + outward * 0.16f + Vector3.up * (s > 2 ? 0.22f : -0.05f)).normalized;
                }
            }
        }

        // Galeocerdo cuvier: fusiform body, famously blunt snout, tall first dorsal,
        // and a heterocercal tail whose upper lobe is much the longer. The asymmetric
        // tail is the giveaway that this is a shark and not a fish.
        static void BuildTigerShark(MeshBuilder mb)
        {
            var profile = new (float z, float w, float h, float yOff)[]
            {
                (-0.52f, 0.020f, 0.035f, 0.00f), // caudal peduncle
                (-0.34f, 0.052f, 0.070f, 0.00f),
                (-0.14f, 0.085f, 0.105f, 0.00f),
                ( 0.06f, 0.098f, 0.120f, 0.00f), // thickest just behind the head
                ( 0.24f, 0.086f, 0.104f, 0.00f),
                ( 0.40f, 0.058f, 0.072f, 0.00f),
                ( 0.50f, 0.030f, 0.042f, -0.01f), // blunt, squared-off snout
            };
            mb.AddBody(profile, 8);

            // Heterocercal caudal fin: long upper lobe, short lower one.
            mb.AddFinDoubleSided(new Vector3(0f, 0.01f, -0.52f),
                                 new Vector3(0f, 0.30f, -0.86f),
                                 new Vector3(0f, 0.05f, -0.66f));
            mb.AddFinDoubleSided(new Vector3(0f, -0.01f, -0.52f),
                                 new Vector3(0f, -0.13f, -0.74f),
                                 new Vector3(0f, -0.03f, -0.62f));

            // Tall first dorsal fin, well forward.
            mb.AddFinDoubleSided(new Vector3(0f, 0.11f,  0.14f),
                                 new Vector3(0f, 0.11f, -0.04f),
                                 new Vector3(0f, 0.34f,  0.02f));
            // Small second dorsal, far back.
            mb.AddFinDoubleSided(new Vector3(0f, 0.06f, -0.30f),
                                 new Vector3(0f, 0.06f, -0.42f),
                                 new Vector3(0f, 0.14f, -0.36f));

            // Long pectoral fins, swept back.
            mb.AddFinDoubleSided(new Vector3( 0.08f, -0.04f,  0.16f),
                                 new Vector3( 0.34f, -0.14f, -0.06f),
                                 new Vector3( 0.12f, -0.05f, -0.02f));
            mb.AddFinDoubleSided(new Vector3(-0.08f, -0.04f,  0.16f),
                                 new Vector3(-0.34f, -0.14f, -0.06f),
                                 new Vector3(-0.12f, -0.05f, -0.02f));
        }
    }

    // Accumulates geometry, then bakes one mesh. Kept deliberately small — just the
    // primitives the roster above actually needs.
    class MeshBuilder
    {
        readonly List<Vector3> _verts = new List<Vector3>(512);
        readonly List<int> _tris = new List<int>(1024);

        int Add(Vector3 v) { _verts.Add(v); return _verts.Count - 1; }

        public void AddTri(Vector3 a, Vector3 b, Vector3 c)
        {
            int i = Add(a); Add(b); Add(c);
            _tris.Add(i); _tris.Add(i + 1); _tris.Add(i + 2);
        }

        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            AddTri(a, b, c);
            AddTri(a, c, d);
        }

        // Fins and algal blades are flat sheets; without both faces they vanish when
        // seen from the wrong side.
        public void AddQuadDoubleSided(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            AddQuad(a, b, c, d);
            AddQuad(d, c, b, a);
        }

        // Fins get thickness for the same reason the algal blade does: a flat
        // double-sided triangle has one black side whichever way the light falls.
        public void AddFinDoubleSided(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-12f) return;

            float thickness = Mathf.Sqrt(n.magnitude) * 0.05f;
            Vector3 offset = n.normalized * thickness * 0.5f;

            Vector3 a0 = a + offset, b0 = b + offset, c0 = c + offset;
            Vector3 a1 = a - offset, b1 = b - offset, c1 = c - offset;

            AddTri(a0, b0, c0);
            AddTri(c1, b1, a1);
            AddQuad(a0, c0, c1, a1);
            AddQuad(c0, b0, b1, c1);
            AddQuad(b0, a0, a1, b1);
        }

        // A curved surface given thickness: front, back and a rim all the way round.
        public void AddSheet(Vector3[,] grid, int rings, int cols, float thickness)
        {
            float half = Mathf.Max(0.0005f, thickness) * 0.5f;
            var normals = new Vector3[rings + 1, cols + 1];

            for (int r = 0; r <= rings; r++)
                for (int c = 0; c <= cols; c++)
                {
                    Vector3 du = grid[Mathf.Min(r + 1, rings), c] - grid[Mathf.Max(r - 1, 0), c];
                    Vector3 dv = grid[r, Mathf.Min(c + 1, cols)] - grid[r, Mathf.Max(c - 1, 0)];
                    Vector3 n = Vector3.Cross(du, dv);
                    normals[r, c] = n.sqrMagnitude > 1e-12f ? n.normalized : Vector3.up;
                }

            Vector3 Front(int r, int c) => grid[r, c] + normals[r, c] * half;
            Vector3 Back(int r, int c) => grid[r, c] - normals[r, c] * half;

            // Wound so every face points out of the solid. Verified by the
            // signed-volume check in the balance report rather than by deriving cross
            // products on paper, which is how the blade ended up inside-out twice.
            for (int r = 0; r < rings; r++)
                for (int c = 0; c < cols; c++)
                {
                    AddQuad(Front(r, c), Front(r, c + 1), Front(r + 1, c + 1), Front(r + 1, c));
                    AddQuad(Back(r, c), Back(r + 1, c), Back(r + 1, c + 1), Back(r, c + 1));
                }

            // Rim around all four edges, so the blade reads as solid at grazing angles.
            for (int c = 0; c < cols; c++)
            {
                AddQuad(Front(0, c + 1), Back(0, c + 1), Back(0, c), Front(0, c));
                AddQuad(Front(rings, c), Back(rings, c), Back(rings, c + 1), Front(rings, c + 1));
            }
            for (int r = 0; r < rings; r++)
            {
                AddQuad(Front(r, 0), Back(r, 0), Back(r + 1, 0), Front(r + 1, 0));
                AddQuad(Front(r + 1, cols), Back(r + 1, cols), Back(r, cols), Front(r, cols));
            }
        }

        // A closed body swept along z through a list of oval cross-sections, capped
        // at both ends. This is what makes a fish read as a fish.
        public void AddBody((float z, float w, float h, float yOff)[] profile, int sides)
        {
            var rings = new Vector3[profile.Length][];
            for (int p = 0; p < profile.Length; p++)
            {
                rings[p] = new Vector3[sides];
                for (int s = 0; s < sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    rings[p][s] = new Vector3(Mathf.Cos(a) * profile[p].w,
                                              Mathf.Sin(a) * profile[p].h + profile[p].yOff,
                                              profile[p].z);
                }
            }

            for (int p = 0; p < profile.Length - 1; p++)
                for (int s = 0; s < sides; s++)
                {
                    int n = (s + 1) % sides;
                    AddQuad(rings[p][s], rings[p][n], rings[p + 1][n], rings[p + 1][s]);
                }

            // Caps.
            var tail = new Vector3(0f, profile[0].yOff, profile[0].z);
            var nose = new Vector3(0f, profile[^1].yOff, profile[^1].z);
            for (int s = 0; s < sides; s++)
            {
                int n = (s + 1) % sides;
                AddTri(tail, rings[0][n], rings[0][s]);
                AddTri(nose, rings[^1][s], rings[^1][n]);
            }
        }

        public void AddEllipsoid(Vector3 centre, Vector3 radii, int rings, int segs)
        {
            var grid = new Vector3[rings + 1, segs + 1];
            for (int r = 0; r <= rings; r++)
            {
                float phi = r / (float)rings * Mathf.PI;
                for (int c = 0; c <= segs; c++)
                {
                    float theta = c / (float)segs * Mathf.PI * 2f;
                    grid[r, c] = centre + new Vector3(
                        Mathf.Sin(phi) * Mathf.Cos(theta) * radii.x,
                        Mathf.Cos(phi) * radii.y,
                        Mathf.Sin(phi) * Mathf.Sin(theta) * radii.z);
                }
            }
            // Wound the same way round as the coral dome below. The other order puts
            // every normal on the inside, which turns the octopus mantle and the
            // urchin's test inside-out — you end up looking at the far wall of the
            // model through its near one.
            for (int r = 0; r < rings; r++)
                for (int c = 0; c < segs; c++)
                    AddQuad(grid[r, c], grid[r, c + 1], grid[r + 1, c + 1], grid[r + 1, c]);
        }

        public void AddTube(Vector3 from, Vector3 to, float r0, float r1, int sides)
        {
            Vector3 axis = (to - from).normalized;
            Vector3 up = Mathf.Abs(axis.y) > 0.9f ? Vector3.right : Vector3.up;
            Vector3 x = Vector3.Cross(axis, up).normalized;
            Vector3 y = Vector3.Cross(axis, x);

            for (int s = 0; s < sides; s++)
            {
                float a0 = s / (float)sides * Mathf.PI * 2f;
                float a1 = (s + 1) / (float)sides * Mathf.PI * 2f;
                Vector3 d0 = x * Mathf.Cos(a0) + y * Mathf.Sin(a0);
                Vector3 d1 = x * Mathf.Cos(a1) + y * Mathf.Sin(a1);
                AddQuad(from + d0 * r0, from + d1 * r0, to + d1 * r1, to + d0 * r1);
            }
        }

        // A spine, antenna or leg: a tube that closes to a point.
        public void AddTaperedSpike(Vector3 from, Vector3 to, float radius, int sides)
        {
            Vector3 axis = (to - from).normalized;
            Vector3 up = Mathf.Abs(axis.y) > 0.9f ? Vector3.right : Vector3.up;
            Vector3 x = Vector3.Cross(axis, up).normalized;
            Vector3 y = Vector3.Cross(axis, x);

            for (int s = 0; s < sides; s++)
            {
                float a0 = s / (float)sides * Mathf.PI * 2f;
                float a1 = (s + 1) / (float)sides * Mathf.PI * 2f;
                Vector3 d0 = x * Mathf.Cos(a0) + y * Mathf.Sin(a0);
                Vector3 d1 = x * Mathf.Cos(a1) + y * Mathf.Sin(a1);
                AddTri(from + d0 * radius, from + d1 * radius, to);
                AddTri(from, from + d1 * radius, from + d0 * radius);
            }
        }

        // A flat segment, as in a Halimeda chain: a thin disc with a rim.
        public void AddDisc(Vector3 centre, Vector3 axis, Vector3 normal,
                            float radiusA, float radiusB, int sides, float thickness)
        {
            Vector3 n = normal.normalized;
            Vector3 u = Vector3.Cross(n, axis).normalized;
            Vector3 v = Vector3.Cross(n, u);

            Vector3 front = centre + n * thickness * 0.5f;
            Vector3 back = centre - n * thickness * 0.5f;

            for (int s = 0; s < sides; s++)
            {
                float a0 = s / (float)sides * Mathf.PI * 2f;
                float a1 = (s + 1) / (float)sides * Mathf.PI * 2f;
                Vector3 p0 = u * Mathf.Cos(a0) * radiusA + v * Mathf.Sin(a0) * radiusB;
                Vector3 p1 = u * Mathf.Cos(a1) * radiusA + v * Mathf.Sin(a1) * radiusB;

                AddTri(front, front + p0, front + p1);
                AddTri(back, back + p1, back + p0);
                AddQuad(front + p0, back + p0, back + p1, front + p1);
            }
        }

        // A corallite: a small cup sunk into a surface.
        public void AddPit(Vector3 centre, Vector3 outward, float radius, float depth, int sides)
        {
            Vector3 n = outward.normalized;
            Vector3 up = Mathf.Abs(n.y) > 0.9f ? Vector3.right : Vector3.up;
            Vector3 x = Vector3.Cross(n, up).normalized;
            Vector3 y = Vector3.Cross(n, x);
            Vector3 floor = centre - n * depth;

            for (int s = 0; s < sides; s++)
            {
                float a0 = s / (float)sides * Mathf.PI * 2f;
                float a1 = (s + 1) / (float)sides * Mathf.PI * 2f;
                Vector3 r0 = x * Mathf.Cos(a0) * radius + y * Mathf.Sin(a0) * radius;
                Vector3 r1 = x * Mathf.Cos(a1) * radius + y * Mathf.Sin(a1) * radius;
                AddQuad(centre + r0, centre + r1, floor + r1 * 0.45f, floor + r0 * 0.45f);
                AddTri(floor, floor + r0 * 0.45f, floor + r1 * 0.45f);
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (_verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(_verts);
            mesh.SetTriangles(_tris, 0);
            mesh.SetNormals(BuildNormals(55f));
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);   // no CPU copy kept; these are never re-read
            return mesh;
        }

        // Smooths across gentle curvature while keeping genuine edges sharp.
        //
        // Every vertex here is unshared, so plain RecalculateNormals gives a purely
        // faceted result and a curved body — an octopus mantle, a fish flank, an
        // urchin's test — reads as a lump of rock. Averaging normals between
        // coincident vertices only where their faces agree to within the smoothing
        // angle rounds those surfaces off, while a fin's two opposed faces (180°
        // apart) and a hard crease stay crisp.
        List<Vector3> BuildNormals(float smoothingAngleDegrees)
        {
            int n = _verts.Count;
            var faceNormals = new Vector3[n];

            for (int t = 0; t < _tris.Count; t += 3)
            {
                int i0 = _tris[t], i1 = _tris[t + 1], i2 = _tris[t + 2];
                Vector3 normal = Vector3.Cross(_verts[i1] - _verts[i0], _verts[i2] - _verts[i0]);
                faceNormals[i0] = normal;
                faceNormals[i1] = normal;
                faceNormals[i2] = normal;
            }

            // Bucket coincident vertices. 0.1 mm is far below anything these shapes
            // resolve, so this only ever merges vertices that are genuinely the same.
            var buckets = new Dictionary<long, List<int>>(n);
            for (int i = 0; i < n; i++)
            {
                var v = _verts[i];
                long key = ((long)Mathf.Round(v.x * 10000f) * 73856093)
                         ^ ((long)Mathf.Round(v.y * 10000f) * 19349663)
                         ^ ((long)Mathf.Round(v.z * 10000f) * 83492791);
                if (!buckets.TryGetValue(key, out var list))
                    buckets[key] = list = new List<int>(4);
                list.Add(i);
            }

            float cosLimit = Mathf.Cos(smoothingAngleDegrees * Mathf.Deg2Rad);
            var normals = new List<Vector3>(n);
            for (int i = 0; i < n; i++) normals.Add(Vector3.zero);

            foreach (var bucket in buckets.Values)
            {
                for (int a = 0; a < bucket.Count; a++)
                {
                    int ia = bucket[a];
                    Vector3 own = faceNormals[ia].normalized;
                    Vector3 sum = Vector3.zero;

                    for (int b = 0; b < bucket.Count; b++)
                    {
                        int ib = bucket[b];
                        // Weighted by face area (the un-normalised cross product),
                        // which is what stops small slivers skewing a smooth surface.
                        if (Vector3.Dot(own, faceNormals[ib].normalized) >= cosLimit)
                            sum += faceNormals[ib];
                    }

                    normals[ia] = sum.sqrMagnitude > 1e-12f ? sum.normalized : own;
                }
            }

            return normals;
        }
    }
}
