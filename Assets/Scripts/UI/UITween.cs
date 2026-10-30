using System;
using System.Collections;
using UnityEngine;

namespace RataBlanca.UI
{
    /// <summary>
    /// Animaciones simples por código (sin paquetes externos).
    /// Todas usan tiempo sin escala, así funcionan aunque se pause el juego.
    /// </summary>
    public static class UITween
    {
        public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        public static float EaseInOutCubic(float t) =>
            t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

        /// <summary>
        /// Ejecuta step(k) cada frame durante 'duration' segundos, con k de 0 a 1
        /// ya suavizado por la curva (o EaseInOutCubic si no se da curva).
        /// </summary>
        public static IEnumerator Run(float duration, Action<float> step, AnimationCurve curve = null)
        {
            if (duration <= 0f)
            {
                step(1f);
                yield break;
            }
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.unscaledDeltaTime / duration);
                step(curve != null ? curve.Evaluate(t) : EaseInOutCubic(t));
                yield return null;
            }
        }

        public static IEnumerator Fade(CanvasGroup group, float to, float duration, AnimationCurve curve = null)
        {
            float from = group.alpha;
            yield return Run(duration, k => group.alpha = Mathf.LerpUnclamped(from, to, k), curve);
        }
    }
}
