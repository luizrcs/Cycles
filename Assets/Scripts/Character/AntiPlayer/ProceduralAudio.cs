using UnityEngine;

// Runtime-synthesized audio for the dread systems — no authored clips exist
// for these. Same philosophy as the code-generated detail textures: cheap,
// fully controllable, no new assets to source.
public static class ProceduralAudio
{
    private const int SampleRate = 44100;

    // A looping "broken transmission" bed for the double: low electrical hum,
    // crackle, hard digital stutters and bit-crushed static breaths.
    public static AudioClip MakeGlitchLoop(int seed = 1912)
    {
        int n = SampleRate * 4;
        float[] d = new float[n];
        var rng = new System.Random(seed);

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float wobble = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * t * 0.37f);
            d[i] += (Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.6f
                   + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.25f) * 0.045f * wobble;
        }

        // Crackle: sparse impulses with a fast noisy decay.
        for (int c = 0; c < 380; c++)
        {
            int start = rng.Next(n - 200);
            float amp = ((float)rng.NextDouble() * 2f - 1f) * 0.35f;
            for (int i = 0; i < 180; i++)
                d[start + i] += amp * Mathf.Exp(-i / 22f) * ((float)rng.NextDouble() * 2f - 1f);
        }

        // Digital stutter: a chunk of the signal hard-repeated — the sound of
        // a moment of time caught skipping.
        for (int s = 0; s < 7; s++)
        {
            int len = SampleRate * rng.Next(35, 85) / 1000;
            int src = rng.Next(n - len);
            int reps = rng.Next(4, 8);
            int dst = rng.Next(n - len * reps - 1);
            for (int r = 0; r < reps; r++)
                for (int i = 0; i < len; i++)
                    d[dst + r * len + i] += d[src + i] * 0.8f + Quantize(d[src + i], 5) * 0.4f;
        }

        // Static breaths, bit-crushed so they read as interference, not wind.
        for (int b = 0; b < 12; b++)
        {
            int len = SampleRate * rng.Next(60, 220) / 1000;
            int start = rng.Next(n - len);
            for (int i = 0; i < len; i++)
            {
                float env = Mathf.Sin(Mathf.PI * i / len);
                d[start + i] += Quantize(((float)rng.NextDouble() * 2f - 1f) * 0.16f, 6) * env;
            }
        }

        Normalize(d, 0.8f);
        CrossfadeLoop(d, SampleRate / 10);

        var clip = AudioClip.Create("GlitchLoop", n, 1, SampleRate, false);
        clip.SetData(d, 0);
        return clip;
    }

    // One low heart thump (the lub or the dub — pitch/volume vary per use):
    // a falling 78→40 Hz sine with a fast decay and a soft valve knock.
    public static AudioClip MakeHeartThump()
    {
        int n = (int)(SampleRate * 0.4f);
        float[] d = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float freq = Mathf.Lerp(78f, 40f, Mathf.Clamp01(t * 5f));
            phase += 2f * Mathf.PI * freq / SampleRate;
            float body = Mathf.Sin(phase) * Mathf.Exp(-t * 13f);
            float knock = Mathf.Sin(2f * Mathf.PI * 190f * t) * Mathf.Exp(-t * 70f) * 0.4f;
            d[i] = Saturate((body + knock) * 1.6f);
        }

        var clip = AudioClip.Create("HeartThump", n, 1, SampleRate, false);
        clip.SetData(d, 0);
        return clip;
    }

    // A knock carried through the ship's structure: a cluster of inharmonic
    // damped partials, like a fist on a bulkhead two corridors away.
    public static AudioClip MakeMetalKnock()
    {
        int n = (int)(SampleRate * 0.7f);
        float[] d = new float[n];
        float[] partials = { 168f, 277f, 433f, 689f };
        float[] gains = { 1f, 0.6f, 0.35f, 0.18f };
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float x = 0f;
            for (int p = 0; p < partials.Length; p++)
                x += Mathf.Sin(2f * Mathf.PI * partials[p] * t) * gains[p] * Mathf.Exp(-t * (6f + p * 4f));
            float click = ((i % 37) / 37f - 0.5f) * Mathf.Exp(-t * 300f) * 0.5f;
            d[i] = Saturate((x + click) * 0.9f);
        }
        var clip = AudioClip.Create("MetalKnock", n, 1, SampleRate, false);
        clip.SetData(d, 0);
        return clip;
    }

    // A failing lamp's electrical buzz: mains hum + harmonics + grit. Looping;
    // FlickeringLight rides its volume on the bulb's level.
    private static AudioClip buzzLoop;
    public static AudioClip GetBuzzLoop()
    {
        if (buzzLoop != null) return buzzLoop;
        int n = SampleRate;
        float[] d = new float[n];
        var rng = new System.Random(50);
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float x = Mathf.Sin(2f * Mathf.PI * 100f * t) * 0.5f
                    + Mathf.Sin(2f * Mathf.PI * 200f * t) * 0.28f
                    + Mathf.Sin(2f * Mathf.PI * 300f * t) * 0.12f;
            x += Quantize(((float)rng.NextDouble() * 2f - 1f) * 0.1f, 7); // grit
            d[i] = Saturate(x);
        }
        Normalize(d, 0.5f);
        CrossfadeLoop(d, SampleRate / 20);
        buzzLoop = AudioClip.Create("LampBuzz", n, 1, SampleRate, false);
        buzzLoop.SetData(d, 0);
        return buzzLoop;
    }

    private static float Quantize(float x, int levels)
    {
        return Mathf.Round(x * levels) / levels;
    }

    private static float Saturate(float x)
    {
        return x / (1f + Mathf.Abs(x)) * 1.4f;
    }

    private static void Normalize(float[] d, float peak)
    {
        float max = 0.0001f;
        foreach (float x in d) max = Mathf.Max(max, Mathf.Abs(x));
        float gain = peak / max;
        for (int i = 0; i < d.Length; i++) d[i] *= gain;
    }

    private static void CrossfadeLoop(float[] d, int fade)
    {
        for (int i = 0; i < fade; i++)
        {
            float a = (float)i / fade;
            d[i] = d[i] * a + d[d.Length - fade + i] * (1f - a);
        }
    }
}
