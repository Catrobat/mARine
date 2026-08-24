using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class World
{
    public static string CreatureTag = "Actor";
    public static string PropTag     = "Obstacle";
    public static float  DefaultRadius = 15f;
    public static int    MaxCreatures  = 12;
    public static int    MaxProps      = 10;
    public static float  DefaultWaterLevel = 8f;

    [Serializable] public struct PlayerState
    { public float depthMeters, headingDegrees; public string headingCompass; public float x, y, z; }

    [Serializable] public struct EnvironmentData
    { public string biome; public float depthMeters, seafloorDistanceMeters, waterTemperatureC, visibilityMeters; public string lightLevel, waterColorHint; }

    [Serializable] public struct CreatureInfo
    { public string name; public float distanceMeters; public string direction, verticalPosition; }

    [Serializable] public struct PropInfo
    { public string name, category; public float distanceMeters; public string direction; }

    [Serializable] public class Snapshot
    { public PlayerState player; public EnvironmentData environment; public CreatureInfo[] creatures; public PropInfo[] props; public string summary; }

    public static PlayerState GetPlayer()
    {
        var cam = Cam;
        Vector3 p = cam != null ? cam.transform.position : Vector3.zero;
        float surface = WaterLevel();
        float heading = cam != null ? cam.transform.eulerAngles.y : 0f;
        return new PlayerState
        {
            x = R(p.x), y = R(p.y), z = R(p.z),
            depthMeters = R(Mathf.Max(0f, surface - p.y)),
            headingDegrees = R(heading),
            headingCompass = Compass(heading),
        };
    }

    public static EnvironmentData GetEnvironment()
    {
        var cam = Cam;
        Vector3 p = cam != null ? cam.transform.position : Vector3.zero;
        float depth = Mathf.Max(0f, WaterLevel() - p.y);

        float seafloor = -1f;
        if (Physics.Raycast(p, Vector3.down, out RaycastHit hit, 200f))
            seafloor = R(hit.distance);

        var uw = UnityEngine.Object.FindFirstObjectByType<UnderwaterEnvironment>();
        return new EnvironmentData
        {
            biome = EstimateBiome(depth, seafloor),
            depthMeters = R(depth),
            seafloorDistanceMeters = seafloor,
            visibilityMeters = R(uw != null ? uw.fadeEnd : 20f),
            lightLevel = LightLevel(depth),
            waterColorHint = uw != null ? ColorHint(uw.waterColor) : "blue",
        };
    }

    public static List<CreatureInfo> GetCreatures(float radius = -1f)
    {
        if (radius <= 0f) radius = DefaultRadius;
        var list = new List<CreatureInfo>();
        var cam = Cam;
        if (cam == null) return list;
        Vector3 p = cam.transform.position;

        foreach (var o in FindByTag(CreatureTag))
        {
            float d = Vector3.Distance(p, o.transform.position);
            if (d > radius) continue;
            list.Add(new CreatureInfo
            {
                name = Clean(o.name),
                distanceMeters = R(d),
                direction = RelativeDir(cam, o.transform.position),
                verticalPosition = Vertical(p.y, o.transform.position.y),
            });
        }
        list.Sort((a, b) => a.distanceMeters.CompareTo(b.distanceMeters));
        if (list.Count > MaxCreatures) list.RemoveRange(MaxCreatures, list.Count - MaxCreatures);
        return list;
    }

    public static List<PropInfo> GetProps(float radius = -1f)
    {
        if (radius <= 0f) radius = DefaultRadius;
        var list = new List<PropInfo>();
        var cam = Cam;
        if (cam == null) return list;
        Vector3 p = cam.transform.position;

        foreach (var o in FindByTag(PropTag))
        {
            float d = Vector3.Distance(p, o.transform.position);
            if (d > radius) continue;
            list.Add(new PropInfo
            {
                name = Clean(o.name),
                category = Categorize(o.name),
                distanceMeters = R(d),
                direction = RelativeDir(cam, o.transform.position),
            });
        }
        list.Sort((a, b) => a.distanceMeters.CompareTo(b.distanceMeters));
        if (list.Count > MaxProps) list.RemoveRange(MaxProps, list.Count - MaxProps);
        return list;
    }

    public static Snapshot GetSnapshot(float radius = -1f)
    {
        if (radius <= 0f) radius = DefaultRadius;
        var s = new Snapshot
        {
            player = GetPlayer(),
            environment = GetEnvironment(),
            creatures = GetCreatures(radius).ToArray(),
            props = GetProps(radius).ToArray(),
        };
        s.summary = BuildSummary(s);
        return s;
    }

    public static string ToJson(float radius = -1f) => JsonUtility.ToJson(GetSnapshot(radius), true);

    public static string Describe(float radius = -1f)
    {
        if (Cam == null) return "";
        var snap = GetSnapshot(radius);
        bool oceanScene = UnityEngine.Object.FindFirstObjectByType<UnderwaterEnvironment>() != null
                       || UnityEngine.Object.FindFirstObjectByType<WaterSurface>() != null;
        if (!oceanScene && snap.creatures.Length == 0) return "";
        return snap.summary;
    }

    static Camera Cam => Camera.main;

    static float WaterLevel()
    {
        var uw = UnityEngine.Object.FindFirstObjectByType<UnderwaterEnvironment>();
        if (uw != null) return uw.waterLevel;
        var ws = UnityEngine.Object.FindFirstObjectByType<WaterSurface>();
        if (ws != null) return ws.waterLevel;
        return DefaultWaterLevel;
    }

    static GameObject[] FindByTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return Array.Empty<GameObject>();
        try { return GameObject.FindGameObjectsWithTag(tag); }
        catch { return Array.Empty<GameObject>(); }
    }

    static string RelativeDir(Camera cam, Vector3 target)
    {
        Vector3 to = target - cam.transform.position; to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return "right here";
        Vector3 fwd = cam.transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        float ang = Vector3.SignedAngle(fwd.normalized, to.normalized, Vector3.up);
        float a = Mathf.Abs(ang);
        string side = ang >= 0 ? "right" : "left";
        if (a < 25f) return "ahead";
        if (a < 70f) return "ahead-" + side;
        if (a < 110f) return side;
        if (a < 155f) return "behind-" + side;
        return "behind";
    }

    static string Vertical(float playerY, float targetY)
    {
        float d = targetY - playerY;
        if (d > 1f) return "above you";
        if (d < -1f) return "below you";
        return "same depth";
    }

    static string Compass(float deg)
    {
        string[] pts = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        return pts[Mathf.RoundToInt(Mathf.Repeat(deg, 360f) / 45f) % 8];
    }

    static float  EstimateTemp(float depth) => Mathf.Clamp(26f - depth * 0.4f, 4f, 30f);

    static string EstimateBiome(float depth, float seafloor)
    {
        if (seafloor >= 0f && seafloor < 2f) return "close to the seabed";
        if (depth < 3f)  return "sunlit shallows";
        if (depth < 8f)  return "reef zone";
        if (depth < 20f) return "open midwater";
        return "deep seabed";
    }

    static string LightLevel(float depth) => depth < 5f ? "bright" : depth < 15f ? "dim" : "dark";

    static string ColorHint(Color c) => c.g > c.b ? "green" : c.b > 0.4f ? "teal" : "deep blue";

    static string Categorize(string raw)
    {
        string n = raw.ToLowerInvariant();
        if (n.Contains("coral")) return "coral";
        if (n.Contains("weed") || n.Contains("kelp") || n.Contains("plant")) return "seaweed";
        if (n.Contains("rock") || n.Contains("stone")) return "rock";
        return "feature";
    }

    static string BuildSummary(Snapshot s)
    {
        var sb = new StringBuilder();
        var e = s.environment;
        sb.Append($"You are about {e.depthMeters}m deep in the {e.biome}");
        if (e.seafloorDistanceMeters >= 0f) sb.Append($", {e.seafloorDistanceMeters}m above the seabed");
        sb.Append($". Water is ~{e.waterTemperatureC}°C, {e.lightLevel}, visibility ~{e.visibilityMeters}m.");

        if (s.creatures.Length == 0) sb.Append(" No creatures within range.");
        else
        {
            sb.Append($" {s.creatures.Length} creature(s) nearby: ");
            int show = Mathf.Min(4, s.creatures.Length);
            for (int i = 0; i < show; i++)
            {
                var c = s.creatures[i];
                if (i > 0) sb.Append(", ");
                sb.Append($"a {c.name} {c.direction} ({c.distanceMeters}m, {c.verticalPosition})");
            }
            if (s.creatures.Length > show) sb.Append(", and more");
            sb.Append('.');
        }
        if (s.props.Length > 0) sb.Append($" {s.props.Length} nearby feature(s) (corals/seaweed/rocks).");
        return sb.ToString();
    }

    static float R(float v) => Mathf.Round(v * 10f) / 10f;

    static string Clean(string name)
    {
        if (string.IsNullOrEmpty(name)) return "creature";
        name = name.Replace("(Clone)", "").Trim();
        int paren = name.IndexOf('(');
        if (paren > 0) name = name.Substring(0, paren).Trim();
        return name.Length == 0 ? "creature" : name;
    }
}