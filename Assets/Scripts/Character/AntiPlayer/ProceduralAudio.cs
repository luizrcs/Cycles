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

    // The hull under a big wave: a deep stress groan that swells, cracks into
    // metallic complaint partway, and dies down. One-shot (~7 s).
    public static AudioClip MakeGroanSwell()
    {
        int n = SampleRate * 7;
        float[] d = new float[n];
        var rng = new System.Random(417);
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float k = t / 7f;
            // swell envelope: slow rise, hold, slow fall
            float env = Mathf.Sin(Mathf.PI * Mathf.Pow(k, 0.8f));

            float freq = 26f + 16f * Mathf.Sin(Mathf.PI * k) + 3f * Mathf.Sin(t * 1.7f);
            phase += 2f * Mathf.PI * freq / SampleRate;
            float body = Mathf.Sin(phase) * 0.7f + Mathf.Sin(phase * 2.02f) * 0.25f;

            // metal stress voices join near the peak
            float stress = (Mathf.Sin(2f * Mathf.PI * 93f * t + Mathf.Sin(t * 3.1f) * 4f) * 0.5f
                          + Mathf.Sin(2f * Mathf.PI * 131f * t) * 0.3f)
                          * Mathf.Clamp01(env - 0.45f) * 1.4f;

            float grit = ((float)rng.NextDouble() * 2f - 1f) * 0.06f * env;
            d[i] = Saturate((body * env + stress + grit) * 1.1f);
        }
        Normalize(d, 0.85f);
        var clip = AudioClip.Create("GroanSwell", n, 1, SampleRate, false);
        clip.SetData(d, 0);
        return clip;
    }

    // A wave breaking against the hull at a porthole: a deep boom under a
    // splash of noise that hisses out. One-shot (~1.8 s).
    public static AudioClip MakeWaveSlap()
    {
        int n = (int)(SampleRate * 1.8f);
        float[] d = new float[n];
        var rng = new System.Random(86);
        float previous = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float boom = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(52f, 30f, Mathf.Clamp01(t * 4f)) * t)
                       * Mathf.Exp(-t * 5.5f);

            // splash: brightened noise (first difference) with a fast attack
            float white = (float)rng.NextDouble() * 2f - 1f;
            float bright = white - previous * 0.6f;
            previous = white;
            float splash = bright * Mathf.Clamp01(t * 30f) * Mathf.Exp(-t * 3.2f) * 0.5f;

            d[i] = Saturate(boom * 1.2f + splash);
        }
        Normalize(d, 0.8f);
        var clip = AudioClip.Create("WaveSlap", n, 1, SampleRate, false);
        clip.SetData(d, 0);
        return clip;
    }

    // The gramophone: a slow minor waltz in a music-box timbre, with the
    // surface crackle of a worn shellac record baked in. Loops (~17 s);
    // wow/flutter is applied live by the Gramophone component via pitch.
    public static AudioClip MakeWaltz()
    {
        const float beat = 60f / 84f; // 84 bpm, 3/4
        int bars = 8;
        int n = (int)(SampleRate * beat * 3f * bars);
        float[] d = new float[n];

        // (bar, beat, frequency, beats held, gain). Melody an octave up —
        // music boxes sing high; bass anchors the oom of each bar.
        float A2 = 110f, E2 = 82.4f, F2 = 87.3f, D3 = 146.8f;
        float E4 = 329.6f, F4 = 349.2f, GS4 = 415.3f, A4 = 440f, B4 = 493.9f;
        float C5 = 523.3f, D5 = 587.3f, E5 = 659.3f;
        var notes = new (int bar, float beatIn, float freq, float held, float gain)[]
        {
            (0,0,A2,3,0.5f), (1,0,A2,3,0.5f), (2,0,D3,3,0.5f), (3,0,E2,3,0.5f),
            (4,0,A2,3,0.5f), (5,0,F2,3,0.5f), (6,0,E2,3,0.5f), (7,0,A2,3,0.5f),
            (0,0,E5*2,2,1f), (0,2,C5*2,1,0.85f),
            (1,0,B4*2,2,1f), (1,2,E4*2,1,0.8f),
            (2,0,F4*2,1,0.9f), (2,1,A4*2,1,0.9f), (2,2,D5*2,1,0.95f),
            (3,0,B4*2,3,1f),
            (4,0,C5*2,2,1f), (4,2,E5*2,1,0.9f),
            (5,0,A4*2,2,0.95f), (5,2,C5*2,1,0.85f),
            (6,0,B4*2,1,0.9f), (6,1,GS4*2,1,0.85f), (6,2,B4*2,1,0.9f),
            (7,0,A4*2,3,1f),
        };

        foreach (var note in notes)
        {
            int start = (int)(SampleRate * beat * (note.bar * 3 + note.beatIn));
            int length = (int)(SampleRate * beat * note.held);
            bool isBass = note.freq < 200f;
            for (int i = 0; i < length && start + i < n; i++)
            {
                float t = (float)i / SampleRate;
                float decay = Mathf.Exp(-t * (isBass ? 5f : 3.2f));
                float x = Mathf.Sin(2f * Mathf.PI * note.freq * t)
                        + Mathf.Sin(2f * Mathf.PI * note.freq * 2.003f * t) * 0.35f
                        + Mathf.Sin(2f * Mathf.PI * note.freq * 4.01f * t) * 0.1f;
                d[start + i] += x * decay * note.gain * (isBass ? 0.16f : 0.12f);
            }
        }

        // shellac surface: steady soft hiss + sparse crackle, louder than hi-fi
        var rng = new System.Random(1921);
        for (int i = 0; i < n; i++)
            d[i] += ((float)rng.NextDouble() * 2f - 1f) * 0.012f;
        for (int c = 0; c < 260; c++)
        {
            int start = rng.Next(n - 120);
            float amp = ((float)rng.NextDouble() * 2f - 1f) * 0.16f;
            for (int i = 0; i < 100; i++)
                d[start + i] += amp * Mathf.Exp(-i / 14f) * ((float)rng.NextDouble() * 2f - 1f);
        }

        Normalize(d, 0.7f);
        CrossfadeLoop(d, SampleRate / 8);
        var clip = AudioClip.Create("GramophoneWaltz", n, 1, SampleRate, false);
        clip.SetData(d, 0);
        return clip;
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
