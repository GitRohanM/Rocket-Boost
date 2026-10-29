using System;
using System.Collections;
using UnityEngine;

namespace RocketBoost.UI
{
    /// <summary>Easing curves used by <see cref="UITween"/>; matches DOTween's familiar names.</summary>
    public enum UIEase
    {
        Linear,
        InQuad,
        OutQuad,
        InOutQuad,
        OutCubic,
        InCubic,
        OutBack,
        OutExpo,
        OutBounce
    }

    /// <summary>
    /// Tiny coroutine tween helper. Every UI animation runs on unscaled time so menus keep
    /// animating while <c>Time.timeScale</c> is zero. Keeps the UI independent of DOTween,
    /// whose optional modules are not required by this project.
    /// </summary>
    public static class UITween
    {
        private static MonoBehaviour runner;

        private static MonoBehaviour Runner
        {
            get
            {
                if (runner != null)
                {
                    return runner;
                }

                GameObject host = new GameObject("[UI Tween Runner]");
                UnityEngine.Object.DontDestroyOnLoad(host);
                runner = host.AddComponent<UITweenRunner>();
                return runner;
            }
        }

        public static Coroutine Run(IEnumerator routine) => Runner.StartCoroutine(routine);

        /// <summary>Stops a tween on the shared runner; safe to call with a stale handle.</summary>
        public static void Stop(ref Coroutine routine)
        {
            if (routine == null)
            {
                return;
            }

            if (runner != null)
            {
                runner.StopCoroutine(routine);
            }

            routine = null;
        }

        public static Coroutine Fade(CanvasGroup group, float to, float duration, Action onComplete = null) =>
            Run(FadeRoutine(group, to, duration, onComplete));

        public static Coroutine Scale(Transform target, Vector3 to, float duration, UIEase ease, Action onComplete = null) =>
            Run(ScaleRoutine(target, to, duration, ease, onComplete));

        public static Coroutine Move(RectTransform target, Vector2 to, float duration, UIEase ease, Action onComplete = null) =>
            Run(MoveRoutine(target, to, duration, ease, onComplete));

        public static Coroutine Value(float from, float to, float duration, Action<float> apply, UIEase ease = UIEase.Linear) =>
            Run(ValueRoutine(from, to, duration, apply, ease));

        public static Coroutine Shake(RectTransform target, float magnitude, float duration) =>
            Run(ShakeRoutine(target, magnitude, duration));

        /// <summary>Quick elastic punch used on stars, badges and confirmations.</summary>
        public static Coroutine Punch(Transform target, float strength, float duration) =>
            Run(PunchRoutine(target, strength, duration));

        public static IEnumerator FadeRoutine(CanvasGroup group, float to, float duration, Action onComplete)
        {
            if (group == null)
            {
                yield break;
            }

            float start = group.alpha;
            float elapsed = 0f;

            while (elapsed < duration && group != null)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, to, Ease(UIEase.OutQuad, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            if (group != null)
            {
                group.alpha = to;
            }

            onComplete?.Invoke();
        }

        private static IEnumerator ScaleRoutine(Transform target, Vector3 to, float duration, UIEase ease, Action onComplete)
        {
            if (target == null)
            {
                yield break;
            }

            Vector3 start = target.localScale;
            float elapsed = 0f;

            while (elapsed < duration && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                target.localScale = Vector3.LerpUnclamped(start, to, Ease(ease, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            if (target != null)
            {
                target.localScale = to;
            }

            onComplete?.Invoke();
        }

        private static IEnumerator MoveRoutine(RectTransform target, Vector2 to, float duration, UIEase ease, Action onComplete)
        {
            if (target == null)
            {
                yield break;
            }

            Vector2 start = target.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                target.anchoredPosition = Vector2.LerpUnclamped(start, to, Ease(ease, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            if (target != null)
            {
                target.anchoredPosition = to;
            }

            onComplete?.Invoke();
        }

        private static IEnumerator ValueRoutine(float from, float to, float duration, Action<float> apply, UIEase ease)
        {
            if (apply == null)
            {
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                apply(Mathf.LerpUnclamped(from, to, Ease(ease, Mathf.Clamp01(elapsed / duration))));
                yield return null;
            }

            apply(to);
        }

        private static IEnumerator ShakeRoutine(RectTransform target, float magnitude, float duration)
        {
            if (target == null)
            {
                yield break;
            }

            Vector2 origin = target.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float decay = 1f - Mathf.Clamp01(elapsed / duration);
                target.anchoredPosition = origin + UnityEngine.Random.insideUnitCircle * (magnitude * decay);
                yield return null;
            }

            if (target != null)
            {
                target.anchoredPosition = origin;
            }
        }

        private static IEnumerator PunchRoutine(Transform target, float strength, float duration)
        {
            if (target == null)
            {
                yield break;
            }

            Vector3 origin = target.localScale;
            float elapsed = 0f;

            while (elapsed < duration && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI) * (1f - t);
                target.localScale = origin * (1f + strength * pulse);
                yield return null;
            }

            if (target != null)
            {
                target.localScale = origin;
            }
        }

        /// <summary>Evaluates an easing curve in the 0..1 domain.</summary>
        public static float Ease(UIEase ease, float t)
        {
            switch (ease)
            {
                case UIEase.InQuad: return t * t;
                case UIEase.OutQuad: return 1f - (1f - t) * (1f - t);
                case UIEase.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
                case UIEase.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case UIEase.InCubic: return t * t * t;
                case UIEase.OutExpo: return Mathf.Approximately(t, 1f) ? 1f : 1f - Mathf.Pow(2f, -10f * t);
                case UIEase.OutBack:
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                case UIEase.OutBounce: return OutBounce(t);
                default: return t;
            }
        }

        private static float OutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
            {
                return n1 * t * t;
            }

            if (t < 2f / d1)
            {
                t -= 1.5f / d1;
                return n1 * t * t + 0.75f;
            }

            if (t < 2.5f / d1)
            {
                t -= 2.25f / d1;
                return n1 * t * t + 0.9375f;
            }

            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }
    }
}
