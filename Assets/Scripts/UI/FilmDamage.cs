using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// The whole game is an old, badly preserved reel — every scene, menus
// included. One persistent object (self-bootstrapping, no scene edits) layers:
//
//  - luminance flutter: the whole frame breathes like lamp-driven projection,
//    with occasional deeper dips,
//  - vertical scratches (bright, with a dark shaded edge — real defects have
//    shading, not clean outlines) and dark hairs that live for a moment,
//  - dust specks flickering frame to frame at random spots,
//  - rare splice jumps: a 1–2 frame white flash, then a dark beat,
//  - a faded-print grade: lifted blacks, teal-rotten shadows, warm highlights
//    (runtime volume on top of each scene's own grade),
//  - typewriter wear on ALL TMP text everywhere: each glyph sits slightly
//    offset, tilted and unevenly inked — the type itself is defective, not
//    noise composited over perfect glyphs.
//
// The overlay canvas sorts above the HUD, so the objectives checklist and
// every menu inherit the damage automatically.
public class FilmDamage : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<FilmDamage>() != null) return;
        var go = new GameObject("FilmDamage");
        DontDestroyOnLoad(go);
        go.AddComponent<FilmDamage>();
    }

    private RawImage flutter;     // full-screen black, alpha breathes
    private RawImage splice;      // full-screen white, fires on splice jumps
    private RawImage[] scratches;
    private RawImage hair;
    private RawImage[] dust;

    private Texture2D scratchTexture;
    private Texture2D hairTexture;
    private Texture2D dustTexture;

    private float spliceAt;
    private float spliceUntil = -1f;
    private float dipUntil = -1f;
    private readonly float[] scratchUntil = new float[3];
    private float hairUntil = -1f;

    void Start()
    {
        BuildOverlay();
        BuildGrade();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        spliceAt = Time.time + Random.Range(18f, 45f);
    }

    void OnDestroy()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    // ------------------------------------------------------------ overlay --

    private void BuildOverlay()
    {
        var canvasGO = new GameObject("FilmOverlay");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90; // above the HUD: the damage covers the UI too

        scratchTexture = MakeScratchTexture();
        hairTexture = MakeHairTexture();
        dustTexture = MakeDustTexture();

        flutter = MakeLayer(canvas.transform, "Flutter", null, Color.black, true);
        splice = MakeLayer(canvas.transform, "Splice", null, Color.white, true);

        scratches = new RawImage[3];
        for (int i = 0; i < scratches.Length; i++)
            scratches[i] = MakeLayer(canvas.transform, "Scratch_" + i, scratchTexture, Color.white, false);

        hair = MakeLayer(canvas.transform, "Hair", hairTexture, Color.white, false);

        dust = new RawImage[6];
        for (int i = 0; i < dust.Length; i++)
            dust[i] = MakeLayer(canvas.transform, "Dust_" + i, dustTexture, Color.white, false);
    }

    private static RawImage MakeLayer(Transform parent, string name, Texture2D tex, Color color, bool fullScreen)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<RawImage>();
        image.texture = tex;
        color.a = 0f;
        image.color = color;
        image.raycastTarget = false;
        var rect = image.rectTransform;
        if (fullScreen)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        return image;
    }

    void Update()
    {
        float t = Time.time;

        // Projection flutter: low-amplitude luminance breathing + rare dips.
        float a = Mathf.PerlinNoise(t * 9f, 3.7f) * 0.045f;
        if (t < dipUntil) a += 0.10f;
        else if (Random.value < 0.002f) dipUntil = t + Random.Range(0.06f, 0.18f);
        SetAlpha(flutter, a);

        UpdateSplice(t);
        UpdateScratches(t);
        UpdateHair(t);
        UpdateDust();
    }

    private void UpdateSplice(float t)
    {
        if (t > spliceAt)
        {
            spliceUntil = t + 0.07f;             // ~2 frames of white
            dipUntil = t + Random.Range(0.15f, 0.3f); // then a dark beat
            spliceAt = t + Random.Range(18f, 55f);
        }
        SetAlpha(splice, t < spliceUntil ? 0.16f : 0f);
    }

    private void UpdateScratches(float t)
    {
        for (int i = 0; i < scratches.Length; i++)
        {
            if (t < scratchUntil[i])
            {
                // alive: tremble sideways a little
                var rect = scratches[i].rectTransform;
                Vector2 p = rect.anchoredPosition;
                p.x += (Mathf.PerlinNoise(t * 17f, i * 9f) - 0.5f) * 2.2f;
                rect.anchoredPosition = p;
            }
            else
            {
                SetAlpha(scratches[i], 0f);
                if (Random.value < 0.004f) // born
                {
                    scratchUntil[i] = t + Random.Range(0.15f, 1.4f);
                    var rect = scratches[i].rectTransform;
                    rect.anchorMin = new Vector2(Random.value, 0f);
                    rect.anchorMax = rect.anchorMin + new Vector2(0f, 1f);
                    rect.sizeDelta = new Vector2(Random.Range(2f, 5f), 0f);
                    rect.anchoredPosition = Vector2.zero;
                    SetAlpha(scratches[i], Random.Range(0.05f, 0.16f));
                }
            }
        }
    }

    private void UpdateHair(float t)
    {
        if (t < hairUntil) return;
        SetAlpha(hair, 0f);
        if (Random.value < 0.0012f)
        {
            hairUntil = t + Random.Range(0.4f, 2.2f);
            var rect = hair.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(Random.value, Random.value);
            rect.sizeDelta = new Vector2(Random.Range(40f, 90f), Random.Range(120f, 280f));
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-40f, 40f));
            SetAlpha(hair, Random.Range(0.12f, 0.3f));
        }
    }

    private void UpdateDust()
    {
        foreach (var speck in dust)
        {
            // most frames off; brief lives at random screen spots
            if (Random.value < 0.06f)
            {
                var rect = speck.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(Random.value, Random.value);
                float s = Random.Range(2f, 9f);
                rect.sizeDelta = new Vector2(s, s);
                SetAlpha(speck, Random.Range(0.06f, 0.22f));
            }
            else SetAlpha(speck, 0f);
        }
    }

    private static void SetAlpha(RawImage image, float alpha)
    {
        Color c = image.color;
        if (Mathf.Approximately(c.a, alpha)) return;
        c.a = alpha;
        image.color = c;
    }

    // -------------------------------------------------------------- grade --

    // Faded print on top of each scene's own grade: lifted blacks, the teal
    // rot old positives drift toward in the shadows, warmth in the highlights.
    private void BuildGrade()
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        var lift = profile.Add<LiftGammaGain>();
        lift.lift.Override(new Vector4(0.02f, 0.025f, 0.022f, 0.012f));

        var split = profile.Add<SplitToning>();
        split.shadows.Override(new Color(0.29f, 0.36f, 0.35f));
        split.highlights.Override(new Color(0.78f, 0.66f, 0.42f));
        split.balance.Override(-15f);

        var go = new GameObject("FilmGrade");
        go.transform.SetParent(transform, false);
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 90f;
        volume.profile = profile;
        volume.weight = 0.65f;
    }

    // ----------------------------------------------------- typewriter wear --

    private static bool applyingWear;

    private static void OnTextChanged(Object obj)
    {
        if (applyingWear) return;
        var text = obj as TMP_Text;
        if (text == null) return;

        applyingWear = true;
        try { ApplyWear(text); }
        finally { applyingWear = false; }
    }

    // Deterministic per glyph+slot so the damage never crawls: each character
    // sits slightly off its baseline, slightly tilted, unevenly inked — like
    // a typewriter with worn type bars striking damp paper.
    private static void ApplyWear(TMP_Text text)
    {
        var info = text.textInfo;
        if (info == null) return;

        for (int i = 0; i < info.characterCount; i++)
        {
            var ch = info.characterInfo[i];
            if (!ch.isVisible) continue;

            var mesh = info.meshInfo[ch.materialReferenceIndex];
            var verts = mesh.vertices;
            var colors = mesh.colors32;
            int v = ch.vertexIndex;
            if (verts == null || v + 3 >= verts.Length) continue;

            var rng = new System.Random(ch.character * 7919 + i * 31);
            float size = text.fontSize;
            var offset = new Vector3(
                ((float)rng.NextDouble() - 0.5f) * size * 0.035f,
                ((float)rng.NextDouble() - 0.5f) * size * 0.05f, 0f);
            float tilt = ((float)rng.NextDouble() - 0.5f) * 0.045f;

            Vector3 mid = (verts[v] + verts[v + 2]) / 2f;
            float cos = Mathf.Cos(tilt);
            float sin = Mathf.Sin(tilt);
            for (int k = 0; k < 4; k++)
            {
                Vector3 p = verts[v + k] - mid;
                verts[v + k] = new Vector3(p.x * cos - p.y * sin, p.x * sin + p.y * cos, p.z) + mid + offset;
            }

            byte ink = (byte)(195 + rng.Next(0, 61)); // uneven strike pressure
            for (int k = 0; k < 4; k++)
            {
                Color32 c = colors[v + k];
                c.a = (byte)(c.a * ink / 255);
                colors[v + k] = c;
            }
        }

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
    }

    // ----------------------------------------------------------- textures --

    // A bright emulsion scratch with a dark shaded edge and gaps — defects
    // have shading and irregularity, never clean outlines.
    private static Texture2D MakeScratchTexture()
    {
        const int w = 8, h = 256;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var rng = new System.Random(1912);
        for (int y = 0; y < h; y++)
        {
            float gap = rng.NextDouble() < 0.18 ? 0f : 1f;
            float strength = (0.5f + (float)rng.NextDouble() * 0.5f) * gap;
            for (int x = 0; x < w; x++)
            {
                float c;
                float a;
                if (x == 3 || x == 4) { c = 1f; a = strength; }                    // bright core
                else if (x == 2 || x == 5) { c = 0f; a = strength * 0.45f; }       // dark shaded edge
                else { c = 0f; a = 0f; }
                tex.SetPixel(x, y, new Color(c, c * 0.97f, c * 0.9f, a));
            }
        }
        tex.Apply();
        return tex;
    }

    // A dark hair caught in the gate: a wandering line.
    private static Texture2D MakeHairTexture()
    {
        const int w = 64, h = 256;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[w * h];
        var rng = new System.Random(31);
        float x0 = w / 2f;
        for (int y = 0; y < h; y++)
        {
            x0 += ((float)rng.NextDouble() - 0.5f) * 2.2f;
            x0 = Mathf.Clamp(x0, 4f, w - 4f);
            for (int x = 0; x < w; x++)
            {
                float d = Mathf.Abs(x - x0);
                float a = Mathf.Clamp01(1.4f - d) * 0.9f;
                pixels[y * w + x] = new Color(0.03f, 0.025f, 0.02f, a);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D MakeDustTexture()
    {
        const int s = 16;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(s / 2f, s / 2f)) / (s / 2f);
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(0.05f, 0.04f, 0.03f, a * a));
            }
        tex.Apply();
        return tex;
    }
}
