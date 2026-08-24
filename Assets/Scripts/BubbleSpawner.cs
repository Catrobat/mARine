using UnityEngine;

/// <summary>
/// Spawns ascending bubble particles below the water line.
/// Configures the ParticleSystem at runtime for zero-asset setup.
/// Inspired by Marine_Biology_AR_Application BubbleController pattern.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class BubbleSpawner : MonoBehaviour
{
    [Header("Bubble Settings")]
    [SerializeField] private float emissionRate = 15f;
    [SerializeField] private float bubbleSpeed = 0.15f;
    [SerializeField] private float minSize = 0.003f;
    [SerializeField] private float maxSize = 0.012f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private Color bubbleColor = new Color(0.75f, 0.92f, 1f, 0.45f);

    [Header("Spawn Area")]
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(0.3f, 0.01f, 0.15f);
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, -0.1f, 0.4f);
    [SerializeField] private Material bubbleMaterial;

    private ParticleSystem _ps;

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
        ConfigureParticleSystem();
    }

    private void ConfigureParticleSystem()
    {
        if (_ps == null) return;

        // Stop any auto-play
        _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Main module
        var main = _ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.06f); // Make them larger and clearly visible
        main.startColor = Color.white; // Use the texture color directly
        main.maxParticles = 150;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.05f; // float upward naturally
        main.playOnAwake = true;
        main.loop = true;

        // Emission
        var emission = _ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 25f; // More bubbles for full display cover

        // Shape - wide horizontal plane below camera view
        var shape = _ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(2.5f, 0.1f, 2.5f); // Spread all over the screen
        shape.position = new Vector3(0f, -0.8f, 1.5f); // Positioned in front and below camera

        // Velocity over lifetime - upward drift
        var vel = _ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
        vel.y = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);

        // Noise for organic wobble
        var noise = _ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.01f, 0.025f);
        noise.frequency = 1.0f;
        noise.scrollSpeed = 0.15f;

        // Size over lifetime - slight grow then pop
        var sol = _ps.sizeOverLifetime;
        sol.enabled = true;
        var sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.4f);
        sizeCurve.AddKey(0.2f, 1f);
        sizeCurve.AddKey(0.85f, 1.1f);
        sizeCurve.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Color over lifetime - fade in and out
        var col = _ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new(Color.white, 0f),
                new(Color.white, 1f)
            },
            new GradientAlphaKey[]
            {
                new(0f, 0f),
                new(0.6f, 0.15f),
                new(0.5f, 0.8f),
                new(0f, 1f)
            }
        );
        col.color = gradient;

        var renderer = _ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (bubbleMaterial != null)
                renderer.material = bubbleMaterial;
            else
                renderer.material = CreateBubbleMaterial();
        }

        // Start playing
        _ps.Play();
    }

    private Texture2D CreateBubbleTexture()
    {
        int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - radius + 0.5f) / radius;
                float dy = (y - radius + 0.5f) / radius;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                if (d > 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    // Beautiful spherical bubble shading:
                    // 1. Edge ring outline (peak near d = 0.9)
                    float edge = Mathf.Exp(-Mathf.Pow((d - 0.9f) / 0.08f, 2f)) * 0.75f;
                    
                    // 2. Light blue highlight in the top-left
                    float hdx = dx + 0.35f;
                    float hdy = dy + 0.35f;
                    float hd = Mathf.Sqrt(hdx * hdx + hdy * hdy);
                    float spec = Mathf.Exp(-Mathf.Pow(hd / 0.22f, 2f)) * 0.8f;

                    // 3. Subtle overall inner transparency glow
                    float inner = (1f - d) * 0.15f;

                    float alpha = Mathf.Clamp01(edge + spec + inner);
                    Color col = new Color(0.82f, 0.93f, 1f, alpha);
                    tex.SetPixel(x, y, col);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private Material CreateBubbleMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Particles/Standard Unlit")
                  ?? Shader.Find("Unlit/Transparent");

        if (shader == null)
        {
            Debug.LogWarning("[FaceAR] Could not find particle shader");
            return null;
        }

        var mat = new Material(shader);
        var tex = CreateBubbleTexture();

        if (mat.HasProperty("_MainTex"))
            mat.SetTexture("_MainTex", tex);
        else if (mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", tex);

        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f); // Transparent mode

        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return mat;
    }

    /// <summary>Enable or disable bubble emission.</summary>
    public void SetActive(bool active)
    {
        if (_ps == null) return;
        if (active && !_ps.isPlaying) _ps.Play();
        else if (!active && _ps.isPlaying) _ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}
