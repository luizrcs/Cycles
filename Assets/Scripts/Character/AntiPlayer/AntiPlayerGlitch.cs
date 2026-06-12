using System.Collections.Generic;
using UnityEngine;

// Makes the double read WRONG without changing the model: each of its
// SkinnedMeshRenderers gets a shell renderer driven by Cycles/GlitchShell
// (slice tearing, red/cyan chromatic ghosts, time-snapped — think the
// Abstractions in The Amazing Digital Circus, kept human), and the whole
// model snaps sideways in brief erratic displacement bursts. Intensity rises
// with proximity, while it chases, and while the player stares at it
// (StareBoost is fed by DreadController). Added at runtime by AntiPlayerFollow,
// so only the double is affected — the player's own body stays clean
// (until ParadoxBleed, which borrows BuildShells for the endgame).
public class AntiPlayerGlitch : MonoBehaviour
{
    [Range(0f, 1f)] public float StareBoost;

    private AntiPlayerFollow follow;
    private DetectPlayer detect;
    private Transform player;
    private Transform modelRoot;
    private Vector3 modelBasePosition;

    private Material shellMaterial;
    private List<(SkinnedMeshRenderer shell, SkinnedMeshRenderer body)> shells = new();

    private float intensity;
    private float burstUntil = -1f;
    private float nextBurst;
    private Vector3 burstOffset;

    private static readonly int Intensity = Shader.PropertyToID("_Intensity");

    void Start()
    {
        follow = GetComponent<AntiPlayerFollow>();
        detect = GetComponent<DetectPlayer>();
        player = follow.Player.transform;
        modelRoot = follow.AntiPlayerAnimator.transform;
        modelBasePosition = modelRoot.localPosition;

        Shader shader = Shader.Find("Cycles/GlitchShell");
        if (shader == null) return;
        shellMaterial = new Material(shader);
        shellMaterial.SetFloat(Intensity, 0f);

        shells = BuildShells(this, shellMaterial);
    }

    // Fresh renderers on the SAME skeleton: bones/rootBone point at the
    // original transforms, so each shell follows every animation for free.
    // Shared with ParadoxBleed (the player's own endgame flicker).
    public static List<(SkinnedMeshRenderer shell, SkinnedMeshRenderer body)> BuildShells(Component root, Material material)
    {
        var built = new List<(SkinnedMeshRenderer, SkinnedMeshRenderer)>();
        foreach (var body in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (body.name.EndsWith("_GlitchShell")) continue;

            var go = new GameObject(body.name + "_GlitchShell");
            go.transform.SetParent(body.transform.parent, false);
            go.transform.localPosition = body.transform.localPosition;
            go.transform.localRotation = body.transform.localRotation;
            go.transform.localScale = body.transform.localScale;

            var shell = go.AddComponent<SkinnedMeshRenderer>();
            shell.sharedMesh = body.sharedMesh;
            shell.bones = body.bones;
            shell.rootBone = body.rootBone;
            shell.localBounds = body.localBounds;
            shell.quality = body.quality;
            shell.updateWhenOffscreen = body.updateWhenOffscreen;
            shell.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shell.receiveShadows = false;

            var materials = new Material[body.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            shell.sharedMaterials = materials;

            shell.enabled = body.enabled;
            built.Add((shell, body));
        }
        return built;
    }

    void Update()
    {
        if (shellMaterial == null) return;

        float target = TargetIntensity();
        // Snap up fast, ease down slow: relief should linger behind danger.
        intensity = target > intensity
            ? Mathf.MoveTowards(intensity, target, 4f * Time.deltaTime)
            : Mathf.MoveTowards(intensity, target, 0.6f * Time.deltaTime);
        shellMaterial.SetFloat(Intensity, intensity);

        // Ghost mode toggles body renderers; shells must always match.
        foreach (var (shell, body) in shells)
            if (shell != null) shell.enabled = body != null && body.enabled;
    }

    private float TargetIntensity()
    {
        if (!follow.Engaged) return 0f;
        if (detect != null && (detect.State == 1 || detect.State == 2)) return 1f;

        float distance = Vector3.Distance(transform.position, player.position);
        float proximity = Mathf.Clamp01(1f - (distance - 3f) / 22f);

        // Never fully clean once it exists — and staring makes it worse, all
        // the way from a faint outline to full abstraction as the stare timer
        // runs out.
        return Mathf.Max(0.12f, proximity * 0.85f, StareBoost);
    }

    // Whole-model displacement snaps, applied after the Animator so they win.
    // Local space (parent is scaled 2x), so amplitudes stay small.
    void LateUpdate()
    {
        if (modelRoot == null || !follow.Engaged) return;

        float t = Time.time;
        if (t < burstUntil)
        {
            if (Random.value < 0.4f) // re-snap mid-burst: jitter, not slide
                burstOffset = NewBurstOffset();
            modelRoot.localPosition = modelBasePosition + burstOffset;
        }
        else
        {
            modelRoot.localPosition = modelBasePosition;
            if (t > nextBurst)
            {
                burstUntil = t + Random.Range(0.06f, 0.22f);
                nextBurst = burstUntil + Random.Range(0.5f, 4f) / Mathf.Max(0.15f, intensity);
                burstOffset = NewBurstOffset();
            }
        }
    }

    private Vector3 NewBurstOffset()
    {
        Vector2 dir = Random.insideUnitCircle.normalized;
        float amount = 0.015f + 0.07f * intensity;
        return new Vector3(dir.x, 0f, dir.y) * amount;
    }
}
