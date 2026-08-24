using System;
using System.Collections;
using UnityEngine;

namespace MarineAR.AISpawner.UI
{
    /// <summary>
    /// Dependency-free coroutine tweens for the AISpawner UI. Kept deliberately tiny —
    /// smooth fades, slides and pulses without pulling a tween library into the project.
    /// All tweens are unscaled-time so UI stays responsive if gameplay time is paused.
    /// </summary>
    public static class UITween
    {
        /// <summary>Smoothstep ease — gentle in/out, the house style for this panel.</summary>
        public static float Ease(float t) => t * t * (3f - 2f * t);

        public static IEnumerator Fade(CanvasGroup group, float from, float to, float duration, Action onComplete = null)
        {
            if (group == null)
                yield break;

            float elapsed = 0f;
            group.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.LerpUnclamped(from, to, Ease(Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            group.alpha = to;
            onComplete?.Invoke();
        }

        public static IEnumerator SlideAnchored(RectTransform rect, Vector2 from, Vector2 to, float duration, Action onComplete = null)
        {
            if (rect == null)
                yield break;

            float elapsed = 0f;
            rect.anchoredPosition = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                rect.anchoredPosition = Vector2.LerpUnclamped(from, to, Ease(Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            rect.anchoredPosition = to;
            onComplete?.Invoke();
        }

        /// <summary>Endless soft scale pulse (for the AI button idle state).</summary>
        public static IEnumerator Pulse(Transform target, float amplitude = 0.04f, float period = 2.4f)
        {
            if (target == null)
                yield break;

            Vector3 baseScale = target.localScale;
            float elapsed = 0f;
            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                float wave = (Mathf.Sin(elapsed / period * Mathf.PI * 2f) + 1f) * 0.5f;
                target.localScale = baseScale * (1f + wave * amplitude);
                yield return null;
            }
        }

        /// <summary>Smoothly approaches a moving target value (progress bars).</summary>
        public static float SmoothTowards(float current, float target, float speed = 8f)
        {
            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
        }
    }
}
