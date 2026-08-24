using UnityEngine;

namespace CreateEnv
{
    // The "locked" half of a biome. Users pick a style index;
    public static class BiomeStyles
    {
        public static readonly string[] Names = { "Classic (dunes)", "Canyons" };

        // Height remap applied per vertex in MeshGenerator. Kept monotonic and in
        // [0,1] so the collider never desyncs and heights never invert.
        public static AnimationCurve BuildHeightCurve(int styleIndex)
        {
            if (styleIndex == 1) // Canyons: flat basins, steep walls, flat mesa tops
                return new AnimationCurve(
                    new Keyframe(0f,   0f,   0f,   0.2f),
                    new Keyframe(0.45f,0.25f,1.4f, 1.4f),
                    new Keyframe(0.7f, 0.8f, 1.4f, 0.5f),
                    new Keyframe(1f,   1f,   0.2f, 0f));

            // Classic: gentle rolling dunes.
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0.6f),
                new Keyframe(0.5f, 0.45f, 1f, 1f),
                new Keyframe(1f, 1f, 1.2f, 0f));
        }

        // Fills a TerrainShaper.Settings from the profile. Classic ignores the
        // canyon sliders entirely (they're hidden in the UI too).
        public static TerrainShaper.Settings BuildShaper(EnvironmentProfile p)
        {
            var s = new TerrainShaper.Settings();
            if (p.styleIndex == 1)
            {
                s.style           = TerrainShaper.Style.Canyons;
                s.warpStrength    = p.warpStrength;
                s.warpScale       = p.warpScale;
                s.ridgeWeight     = p.ridgeWeight;
                s.ridgeSharpness  = p.ridgeSharpness;
                s.terraceSteps    = p.terraceSteps;
                s.terraceSharpness= p.terraceSharpness;
                s.terraceStrength = p.terraceStrength;
            }
            else
            {
                s.style = TerrainShaper.Style.Classic;
            }
            return s;
        }
    }
}
