using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The reel is old and badly preserved — but WHERE the damage shows follows the
// game's effect taxonomy:
//
//  CONSTANT (environment style): faded-print grade (lifted blacks, teal-rotten
//  shadows, warm highlights) and typewriter wear on every TMP glyph — always,
//  every scene.
//
//  MENUS (everything before/after play): the projection itself is failing,
//  HEAVY and constant — luminance flutter, scratches with shaded edges, hairs,
//  dust, splice jumps, and the film ROLL (the frame line slipping through the
//  picture). The menus may be far heavier than gameplay: the player is not
//  trying to survive there.
//
//  IN-GAME (dying effects): all of that damage exists only as a function of
//  DANGER — it scales with DreadController's intensity (the double close,
//  chasing, being stared at, gaze penalty) and with the endgame paradox ramp.
//  At rest the game shows no film damage at all, only the constant grade.
//
// Self-bootstrapping (no scene edits); the overlay sorts above the HUD so the
// objectives checklist inherits every defect. Also ages the checklist itself:
// stained paper, a slight skew, pencil-scrawl checkmarks.
public class FilmDamage : MonoBehaviour
{
    public static FilmDamage Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("FilmDamage");
        DontDestroyOnLoad(go);
        go.AddComponent<FilmDamage>();
    }

    // Danger systems (DreadController, ParadoxBleed) report each frame; the
    // strongest voice wins, then it resets for the next frame.
    private float reportedDanger;
    public static void ReportDanger(float danger)
    {
        if (Instance != null)
            Instance.reportedDanger = Mathf.Max(Instance.reportedDanger, Mathf.Clamp01(danger));
    }

    private float intensity; // smoothed working level

    private RawImage flutter;
    private RawImage splice;
    private RawImage roll;
    private RawImage[] scratches;
    private RawImage hair;
    private RawImage[] dust;

    private float spliceAt;
    private float spliceUntil = -1f;
    private float dipUntil = -1f;
    private readonly float[] scratchUntil = new float[3];
    private float hairUntil = -1f;
    private float rollAt;
    private float rollStarted = -1f;
    private const float RollDuration = 0.38f;

    private bool isGame;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        BuildOverlay();
        BuildGrade();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        StartCoroutine(WearExistingTexts());
        spliceAt = Time.time + Random.Range(8f, 20f);
        rollAt = Time.time + Random.Range(4f, 12f);
    }

    void OnDestroy()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isGame = scene.name == "Game";
        StartCoroutine(WearExistingTexts());
        if (isGame) StartCoroutine(WearChecklist());
    }

    void Update()
    {
        float t = Time.time;

        // Menus: the projector is simply in this state. In game: damage is a
        // dying effect — it follows danger and vanishes at rest.
        float target = isGame ? reportedDanger : 0.85f;
        reportedDanger = 0f;
        intensity = target > intensity
            ? Mathf.MoveTowards(intensity, target, 3f * Time.deltaTime)
            : Mathf.MoveTowards(intensity, target, 0.8f * Time.deltaTime);

        if (intensity < 0.02f)
        {
            SetAlpha(flutter, 0f);
            SetAlpha(splice, 0f);
            SetAlpha(roll, 0f);
            foreach (var s in scratches) SetAlpha(s, 0f);
            SetAlpha(hair, 0f);
            foreach (var d in dust) SetAlpha(d, 0f);
            return;
        }

        float a = Mathf.PerlinNoise(t * 9f, 3.7f) * 0.06f * intensity;
        if (t < dipUntil) a += 0.11f * intensity;
        else if (Random.value < 0.002f * intensity) dipUntil = t + Random.Range(0.06f, 0.18f);
        SetAlpha(flutter, a);

        UpdateSplice(t);
        UpdateRoll(t);
        UpdateScratches(t);
        UpdateHair(t);
        UpdateDust();
    }

    private void UpdateSplice(float t)
    {
        if (t > spliceAt)
        {
            if (Random.value < intensity) // weak danger rarely earns a splice
            {
                spliceUntil = t + 0.07f;
                dipUntil = t + Random.Range(0.15f, 0.3f);
            }
            spliceAt = t + Random.Range(10f, 35f);
        }
        SetAlpha(splice, t < spliceUntil ? 0.16f * intensity : 0f);
    }

    // The frame line slips through the picture: a dark band with a bright
    // sliver sweeping top to bottom, with the image fluttering under it.
    private void UpdateRoll(float t)
    {
        if (rollStarted > 0f)
        {
            float k = (t - rollStarted) / RollDuration;
            if (k >= 1f)
            {
                rollStarted = -1f;
                SetAlpha(roll, 0f);
            }
            else
            {
                var rect = roll.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f - k * 1.4f);
                rect.anchorMax = new Vector2(1f, 1f - k * 1.4f + 0.22f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                SetAlpha(roll, 0.55f * intensity);
            }
            return;
        }

        if (t > rollAt)
        {
            // menus roll often; in game only when death is close
            if (!isGame || intensity > 0.45f) rollStarted = t;
            rollAt = t + (isGame ? Random.Range(6f, 14f) : Random.Range(7f, 18f));
        }
    }

    private void UpdateScratches(float t)
    {
        for (int i = 0; i < scratches.Length; i++)
        {
            if (t < scratchUntil[i])
            {
                var rect = scratches[i].rectTransform;
                Vector2 p = rect.anchoredPosition;
                p.x += (Mathf.PerlinNoise(t * 17f, i * 9f) - 0.5f) * 2.2f;
                rect.anchoredPosition = p;
            }
            else
            {
                SetAlpha(scratches[i], 0f);
                if (Random.value < 0.012f * intensity)
                {
                    scratchUntil[i] = t + Random.Range(0.15f, 1.4f);
                    var rect = scratches[i].rectTransform;
                    rect.anchorMin = new Vector2(Random.value, 0f);
                    rect.anchorMax = rect.anchorMin + new Vector2(0f, 1f);
                    rect.sizeDelta = new Vector2(Random.Range(2f, 5f), 0f);
                    rect.anchoredPosition = Vector2.zero;
                    SetAlpha(scratches[i], Random.Range(0.06f, 0.2f) * intensity);
                }
            }
        }
    }

    private void UpdateHair(float t)
    {
        if (t < hairUntil) return;
        SetAlpha(hair, 0f);
        if (Random.value < 0.003f * intensity)
        {
            hairUntil = t + Random.Range(0.4f, 2.2f);
            var rect = hair.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(Random.value, Random.value);
            rect.sizeDelta = new Vector2(Random.Range(40f, 90f), Random.Range(120f, 280f));
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-40f, 40f));
            SetAlpha(hair, Random.Range(0.12f, 0.3f) * intensity);
        }
    }

    private void UpdateDust()
    {
        foreach (var speck in dust)
        {
            if (Random.value < 0.08f * intensity)
            {
                var rect = speck.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(Random.value, Random.value);
                float s = Random.Range(2f, 9f);
                rect.sizeDelta = new Vector2(s, s);
                SetAlpha(speck, Random.Range(0.06f, 0.22f) * intensity);
            }
            else SetAlpha(speck, 0f);
        }
    }

    // ------------------------------------------------------------ overlay --

    private void BuildOverlay()
    {
        var canvasGO = new GameObject("FilmOverlay");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        flutter = MakeLayer(canvas.transform, "Flutter", null, Color.black, true);
        splice = MakeLayer(canvas.transform, "Splice", null, Color.white, true);
        roll = MakeLayer(canvas.transform, "Roll", MakeRollTexture(), Color.white, true);

        scratches = new RawImage[3];
        for (int i = 0; i < scratches.Length; i++)
            scratches[i] = MakeLayer(canvas.transform, "Scratch_" + i, MakeScratchTexture(), Color.white, false);

        hair = MakeLayer(canvas.transform, "Hair", MakeHairTexture(), Color.white, false);

        dust = new RawImage[6];
        var dustTexture = MakeDustTexture();
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

    private static void SetAlpha(RawImage image, float alpha)
    {
        if (image == null) return;
        Color c = image.color;
        if (Mathf.Approximately(c.a, alpha)) return;
        c.a = alpha;
        image.color = c;
    }

    // -------------------------------------------------------------- grade --

    // Constant in every scene (environment category): faded print.
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

    // Texts that generated BEFORE this object existed (the first scene's
    // menus) never fired TEXT_CHANGED for us — sweep them explicitly.
    private IEnumerator WearExistingTexts()
    {
        yield return null; // let the scene's canvases build first
        foreach (var text in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            text.ForceMeshUpdate(true, true); // regenerates → wear hook fires
    }

    private static void OnTextChanged(Object obj)
    {
        if (applyingWear) return;
        var text = obj as TMP_Text;
        if (text == null) return;

        applyingWear = true;
        try { ApplyWear(text); }
        finally { applyingWear = false; }
    }

    // Deterministic per glyph+slot: each character sits slightly off its
    // baseline, tilted, unevenly inked — worn type bars on damp paper.
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

            byte ink = (byte)(195 + rng.Next(0, 61));
            for (int k = 0; k < 4; k++)
            {
                Color32 c = colors[v + k];
                c.a = (byte)(c.a * ink / 255);
                colors[v + k] = c;
            }
        }

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
    }

    // ------------------------------------------------------ checklist wear --

    // The objectives panel: skewed like a pinned note, stained like it spent
    // a century at sea, checkmarks replaced by rough pencil scrawls.
    private IEnumerator WearChecklist()
    {
        yield return null;

        var objectives = FindFirstObjectByType<ObjectivesController>();
        if (objectives == null) yield break;

        var rect = objectives.GetComponent<RectTransform>();
        if (rect != null) rect.localRotation = Quaternion.Euler(0f, 0f, -1.7f);

        var stains = new GameObject("PaperStains");
        stains.transform.SetParent(objectives.transform, false);
        var image = stains.AddComponent<RawImage>();
        image.texture = MakeStainTexture();
        image.color = new Color(1f, 1f, 1f, 0.55f);
        image.raycastTarget = false;
        var stainRect = image.rectTransform;
        stainRect.anchorMin = Vector2.zero;
        stainRect.anchorMax = Vector2.one;
        stainRect.offsetMin = Vector2.zero;
        stainRect.offsetMax = Vector2.zero;

        var scrawl = MakeScrawlSprite();
        if (objectives.CheckMarks != null)
            foreach (var mark in objectives.CheckMarks)
            {
                var markImage = mark != null ? mark.GetComponent<Image>() : null;
                if (markImage != null) markImage.sprite = scrawl;
            }
    }

    // ----------------------------------------------------------- textures --

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
                if (x == 3 || x == 4) { c = 1f; a = strength; }
                else if (x == 2 || x == 5) { c = 0f; a = strength * 0.45f; }
                else { c = 0f; a = 0f; }
                tex.SetPixel(x, y, new Color(c, c * 0.97f, c * 0.9f, a));
            }
        }
        tex.Apply();
        return tex;
    }

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

    // The frame divider: dark band, one bright overexposed sliver at its edge.
    private static Texture2D MakeRollTexture()
    {
        const int h = 64;
        var tex = new Texture2D(1, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < h; y++)
        {
            float k = (float)y / h;
            float a;
            float c;
            if (k > 0.93f) { c = 1f; a = 0.85f; }                    // bright sliver
            else { c = 0f; a = Mathf.Sin(k * Mathf.PI) * 0.95f; }    // soft dark band
            tex.SetPixel(0, y, new Color(c, c * 0.97f, c * 0.92f, a));
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D MakeStainTexture()
    {
        const int s = 128;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[s * s];
        var rng = new System.Random(74);

        // edge darkening: paper that aged from its borders inward
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float ex = Mathf.Min(x, s - 1 - x) / (s * 0.5f);
                float ey = Mathf.Min(y, s - 1 - y) / (s * 0.5f);
                float edge = 1f - Mathf.Clamp01(Mathf.Min(ex, ey) * 3f);
                pixels[y * s + x] = new Color(0.25f, 0.18f, 0.1f, edge * 0.5f);
            }

        // blotches: rings and soaks
        for (int b = 0; b < 7; b++)
        {
            float cx = (float)rng.NextDouble() * s;
            float cy = (float)rng.NextDouble() * s;
            float r = 8f + (float)rng.NextDouble() * 22f;
            float strength = 0.1f + (float)rng.NextDouble() * 0.2f;
            bool ring = rng.NextDouble() < 0.5;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) / r;
                    if (d > 1.2f) continue;
                    float a = ring
                        ? Mathf.Clamp01(1f - Mathf.Abs(d - 1f) * 6f) * strength
                        : Mathf.Clamp01(1f - d) * strength * 0.7f;
                    int idx = y * s + x;
                    var p = pixels[idx];
                    pixels[idx] = new Color(0.3f, 0.2f, 0.1f, Mathf.Clamp01(p.a + a));
                }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // A pencil-scrawled check: two rough strokes, uneven pressure.
    private static Sprite MakeScrawlSprite()
    {
        const int s = 48;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[s * s];
        var rng = new System.Random(7);

        void Stroke(Vector2 from, Vector2 to)
        {
            int steps = 40;
            for (int i = 0; i <= steps; i++)
            {
                float k = (float)i / steps;
                Vector2 p = Vector2.Lerp(from, to, k);
                p += new Vector2((float)rng.NextDouble() - 0.5f, ((float)rng.NextDouble() - 0.5f)) * 1.6f;
                float press = 0.55f + (float)rng.NextDouble() * 0.45f;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int x = Mathf.Clamp((int)p.x + dx, 0, s - 1);
                        int y = Mathf.Clamp((int)p.y + dy, 0, s - 1);
                        float fall = (dx == 0 && dy == 0) ? 1f : 0.45f;
                        int idx = y * s + x;
                        float a = Mathf.Clamp01(pixels[idx].a + press * fall * 0.8f);
                        pixels[idx] = new Color(0.12f, 0.1f, 0.09f, a);
                    }
            }
        }

        Stroke(new Vector2(8f, 26f), new Vector2(19f, 12f));
        Stroke(new Vector2(19f, 12f), new Vector2(40f, 38f));

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
    }
}
