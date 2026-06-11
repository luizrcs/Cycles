using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Shared time-based fade helpers. Durations match the old hand-rolled
// WaitForSeconds step loops so every transition keeps its original timing.
public static class Fades
{
    public static IEnumerator Graphic(Graphic graphic, float from, float to, float duration)
    {
        Color color = graphic.color;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            color.a = Mathf.Lerp(from, to, t / duration);
            graphic.color = color;
            yield return null;
        }

        color.a = to;
        graphic.color = color;
    }

    public static IEnumerator Volume(AudioSource source, float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            source.volume = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        source.volume = to;
    }
}
