using UnityEngine;
using System.Collections;


public class CamouflageAbility : ActorAbility
{
    [Header("Camouflage Settings")]
    [Range(0f, 1f)]
    [Tooltip("Alpha when camouflaged")]
    public float camouflageAlpha = 0.2f;

    [Tooltip("Fade transition duration in seconds")]
    public float fadeDuration = 0.4f;

    [Tooltip("How long camouflage stays active in auto-defense mode (seconds)")]
    public float autoDefenseDuration = 4f;

    public override string AbilityName => "Camouflage";
    public override string IconName => "icon_camouflage";

    private Renderer[] renderers;
    private Color[] originalColors;
    private bool isFading = false;
    private Coroutine autoDefenseCoroutine;

    // Config read by ARPlacementController at spawn time
    // These are set via InitializeDefense()
    private float defenseTriggerChance = 0.8f;
    private float stunDuration = 1.5f;
    private float slowDuration = 3f;
    private float slowMultiplier = 0.3f;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        CacheOriginalColors();
    }

  
    // Called by ARPlacementController after attaching this component.
    // Reads defense values from FoodChainConfig.
  
    public void InitializeDefense(float triggerChance, float stun, float slow, float multiplier)
    {
        defenseTriggerChance = triggerChance;
        stunDuration = stun;
        slowDuration = slow;
        slowMultiplier = multiplier;
    }

    

   
    public override void Activate()
    {
        if (isFading) return;
        IsActive = !IsActive;

        if (IsActive)
            StartCoroutine(FadeTo(camouflageAlpha));
        else
            StartCoroutine(FadeToOpaque());
    }

    

    public bool TriggerDefense(GameObject predator)
    {
        // Already active or fading — don't re-trigger
        if (IsActive || isFading) return false;

        // Chance roll
        if (Random.value > defenseTriggerChance)
        {
            Debug.Log($"[Camouflage] {gameObject.name} defense roll FAILED " +
                      $"(chance={defenseTriggerChance:P0})");
            return false;
        }

        Debug.Log($"[Camouflage] {gameObject.name} defense triggered! " +
                  $"Debuffing {predator.name}");

        // Cancel any existing auto-defense timer
        if (autoDefenseCoroutine != null)
            StopCoroutine(autoDefenseCoroutine);

        autoDefenseCoroutine = StartCoroutine(AutoDefenseSequence(predator));
        return true;
    }

    IEnumerator AutoDefenseSequence(GameObject predator)
    {
        // Step 1: Go invisible
        IsActive = true;
        yield return StartCoroutine(FadeTo(camouflageAlpha));

        // Step 2: Debuff the predator
        if (predator != null)
            StartCoroutine(DebuffPredator(predator));

        // Step 3: Stay camouflaged for autoDefenseDuration
        yield return new WaitForSeconds(autoDefenseDuration);

        // Step 4: Fade back to visible
        yield return StartCoroutine(FadeToOpaque());
        IsActive = false;
        autoDefenseCoroutine = null;
    }

    IEnumerator DebuffPredator(GameObject predator)
    {
        FoodConsumer predatorConsumer = predator.GetComponent<FoodConsumer>();
        if (predatorConsumer == null) yield break;

        // Phase 1: STUN — completely stop hunting
        predatorConsumer.SetStunned(true);
        Debug.Log($"[Camouflage] {predator.name} STUNNED for {stunDuration}s");
        yield return new WaitForSeconds(stunDuration);

        // Phase 2: SLOW — reduced speed
        predatorConsumer.SetStunned(false);
        predatorConsumer.ApplySlow(slowMultiplier);
        Debug.Log($"[Camouflage] {predator.name} SLOWED ({slowMultiplier:P0} speed) for {slowDuration}s");
        yield return new WaitForSeconds(slowDuration);

        // Phase 3: Restore normal speed
        predatorConsumer.RemoveSlow();
        Debug.Log($"[Camouflage] {predator.name} debuff expired, normal speed restored");
    }

    public override void Deactivate()
    {
        if (autoDefenseCoroutine != null)
        {
            StopCoroutine(autoDefenseCoroutine);
            autoDefenseCoroutine = null;
        }
        if (IsActive)
        {
            StopAllCoroutines();
            RestoreOpaque();
        }
        base.Deactivate();
    }

    
    // Fade helpers


    void CacheOriginalColors()
    {
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i].material != null)
                originalColors[i] = renderers[i].material.color;
    }

    IEnumerator FadeTo(float targetAlpha)
    {
        isFading = true;
        SetMaterialsTransparent();
        float elapsed = 0f;
        float[] startAlphas = GetCurrentAlphas();

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i].material == null) continue;
                Color c = renderers[i].material.color;
                c.a = Mathf.Lerp(startAlphas[i], targetAlpha, t);
                renderers[i].material.color = c;
            }
            yield return null;
        }
        isFading = false;
    }

    IEnumerator FadeToOpaque()
    {
        isFading = true;
        float elapsed = 0f;
        float[] startAlphas = GetCurrentAlphas();

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i].material == null) continue;
                Color c = renderers[i].material.color;
                c.a = Mathf.Lerp(startAlphas[i], originalColors[i].a, t);
                renderers[i].material.color = c;
            }
            yield return null;
        }
        RestoreOpaque();
        isFading = false;
    }

    void SetMaterialsTransparent()
    {
        foreach (var r in renderers)
        {
            if (r?.material == null) continue;
            SetTransparentMode(r.material);
        }
    }

    void RestoreOpaque()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i]?.material == null) continue;
            SetOpaqueMode(renderers[i].material);
            renderers[i].material.color = originalColors[i];
        }
    }

    float[] GetCurrentAlphas()
    {
        float[] a = new float[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            a[i] = renderers[i]?.material != null ? renderers[i].material.color.a : 1f;
        return a;
    }

    void SetTransparentMode(Material mat)
    {
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }

    void SetOpaqueMode(Material mat)
    {
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 0);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }
        else if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 0);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = -1;
        }
    }
}