using UnityEngine;
// ReSharper disable UnusedMember.Global

public static class Easing
{
    public static float InQuad(float time, float start, float end, float duration)
    {
        time /= duration;
        return (end - start) * time * time + start;
    }

    public static float OutQuad(float time, float start, float end, float duration)
    {
        time /= duration;
        return (start - end) * time * (time - 2) + start;
    }

    public static float InOutQuad(float time, float start, float end, float duration)
    {
        time /= duration / 2;
        if (time < 1)
            return (end - start) / 2 * time * time + start;
        time--;
        return (start - end) / 2 * (time * (time - 2) - 1) + start;
    }

    public static float InCubic(float time, float start, float end, float duration)
    {
        time /= duration;
        return (end - start) * time * time * time + start;
    }

    public static float OutCubic(float time, float start, float end, float duration)
    {
        time /= duration;
        time--;
        return (end - start) * (time * time * time + 1) + start;
    }

    public static float InOutCubic(float time, float start, float end, float duration)
    {
        time /= duration / 2;
        if (time < 1) return (end - start) / 2 * time * time * time + start;
        time -= 2;
        return (end - start) / 2 * (time * time * time + 2) + start;
    }

    public static float InQuart(float time, float start, float end, float duration)
    {
        time /= duration;
        return (end - start) * time * time * time * time + start;
    }

    public static float OutQuart(float time, float start, float end, float duration)
    {
        time /= duration;
        time--;
        return (start - end) * (time * time * time * time - 1) + start;
    }

    public static float InOutQuart(float time, float start, float end, float duration)
    {
        time /= duration / 2;
        if (time < 1) return (end - start) / 2 * time * time * time * time + start;
        time -= 2;
        return (start - end) / 2 * (time * time * time * time - 2) + start;
    }

    public static float InQuint(float time, float start, float end, float duration)
    {
        time /= duration;
        return (end - start) * time * time * time * time * time + start;
    }

    public static float OutQuint(float time, float start, float end, float duration)
    {
        time /= duration;
        time--;
        return (end - start) * (time * time * time * time * time + 1) + start;
    }

    public static float InOutQuint(float time, float start, float end, float duration)
    {
        time /= duration / 2;
        if (time < 1) return (end - start) / 2 * time * time * time * time * time + start;
        time -= 2;
        return (end - start) / 2 * (time * time * time * time * time + 2) + start;
    }

    public static float InSine(float time, float start, float end, float duration)
    {
        return (start - end) * Mathf.Cos(time / duration * (Mathf.PI / 2)) + (end - start) + start;
    }

    public static float OutSine(float time, float start, float end, float duration)
    {
        return (end - start) * Mathf.Sin(time / duration * (Mathf.PI / 2)) + start;
    }

    public static float InOutSine(float time, float start, float end, float duration)
    {
        return (start - end) / 2 * (Mathf.Cos(Mathf.PI * time / duration) - 1) + start;
    }

    public static float InExpo(float time, float start, float end, float duration)
    {
        return (end - start) * Mathf.Pow(2, 10 * (time / duration - 1)) + start;
    }

    public static float OutExpo(float time, float start, float end, float duration)
    {
        return (end - start) * (-Mathf.Pow(2, -10 * time / duration) + 1) + start;
    }

    public static float InOutExpo(float time, float start, float end, float duration)
    {
        time /= duration / 2;
        if (time < 1) return (end - start) / 2 * Mathf.Pow(2, 10 * (time - 1)) + start;
        time--;
        return (end - start) / 2 * (-Mathf.Pow(2, -10 * time) + 2) + start;
    }

    public static float InCirc(float time, float start, float end, float duration)
    {
        time /= duration;
        return (start - end) * (Mathf.Sqrt(1 - time * time) - 1) + start;
    }

    public static float OutCirc(float time, float start, float end, float duration)
    {
        time /= duration;
        time--;
        return (end - start) * Mathf.Sqrt(1 - time * time) + start;
    }

    public static float InOutCirc(float time, float start, float end, float duration)
    {
        time /= duration / 2;
        if (time < 1) return (start - end) / 2 * (Mathf.Sqrt(1 - time * time) - 1) + start;
        time -= 2;
        return (end - start) / 2 * (Mathf.Sqrt(1 - time * time) + 1) + start;
    }

    public static float InBounce(float time, float start, float end, float duration)
    {
        return end - OutBounce(duration - time, start, end, duration);
    }

    public static float OutBounce(float time, float start, float end, float duration)
    {
        const float b1 = 4f / 11f;
        const float b2 = 6f / 11f;
        const float b3 = 8f / 11f;
        const float b4 = 3f / 4f;
        const float b5 = 9f / 11f;
        const float b6 = 10f / 11f;
        const float b7 = 15f / 16f;
        const float b8 = 21f / 22f;
        const float b9 = 63f / 64f;
        const float b0 = 1 / b1 / b1;

        var t = time / duration;
        var result = t < b1 ? b0 * t * t : t < b3 ? b0 * (t -= b2) * t + b4 : t < b6 ? b0 * (t -= b5) * t + b7 : b0 * (t -= b8) * t + b9;
        return result * (end - start) + start;
    }

    public static float InOutBounce(float time, float start, float end, float duration)
    {
        var t = time / duration;
        var result = ((t *= 2) <= 1 ? 1 - OutBounce(1 - t, 0, 1, 1) : OutBounce(t - 1, 0, 1, 1) + 1) / 2;
        return result * (end - start) + start;
    }
}
