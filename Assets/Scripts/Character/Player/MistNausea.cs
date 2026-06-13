using UnityEngine;

// The mist is a poison gas. Exposure rises toward a target set by the local
// fog density (the denser, the higher AND the faster — full effect in ≈9 s at
// a pocket's heart) and drains far more slowly (≈25 s), so both density and
// time spent inside visibly matter. The poisoning is multi-modal:
//
//  - drunk tumble: layered roll/yaw/pitch + lateral head drift (CameraSway rig)
//  - GHOSTED vision: a fullscreen NauseaVision quad resamples the scene with a
//    wandering double image (eyes that cannot converge), darkens the picture,
//    drains its color and shifts it sick-green — unmistakable, grows with
//    exposure, and makes you WANT to leave the pocket.
//
// Distinct on purpose from DreadController's heartbeat anxiety: that one is
// the double; this one is the air. Added at runtime by DetectPlayer onto the
// player camera (same object as CameraSway and DustAndMist).
public class MistNausea : MonoBehaviour
{
    private DustAndMist mist;
    private CameraSway sway;
    private float exposure;

    private Material visionMaterial;

    private const float AmbientFloor = 0.10f;  // density below this barely registers
    private const float BuildSeconds = 9f;     // to full effect at a pocket center
    private const float DecaySeconds = 25f;    // much slower than it builds

    private static readonly int Nausea = Shader.PropertyToID("_Nausea");

    void Start()
    {
        mist = GetComponent<DustAndMist>();
        if (mist == null) mist = FindAnyObjectByType<DustAndMist>();
        sway = GetComponent<CameraSway>();
        if (sway == null) sway = FindAnyObjectByType<CameraSway>();
        if (sway == null) { enabled = false; return; }

        BuildVisionQuad();
    }

    // A quad glued to the camera, covering the view, running NauseaVision.
    private void BuildVisionQuad()
    {
        Shader shader = Shader.Find("Cycles/NauseaVision");
        if (shader == null) return;

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "NauseaVision";
        Destroy(quad.GetComponent<Collider>());
        quad.transform.SetParent(transform, false);
        quad.transform.localPosition = new Vector3(0f, 0f, 0.35f);
        quad.transform.localRotation = Quaternion.identity;
        quad.transform.localScale = new Vector3(1.4f, 0.9f, 1f); // generous frustum cover

        visionMaterial = new Material(shader);
        visionMaterial.SetFloat(Nausea, 0f);
        var renderer = quad.GetComponent<MeshRenderer>();
        renderer.material = visionMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void Update()
    {
        float density = mist != null ? mist.DensityAt(transform.position) : 0f;

        // Exposure chases a density-set ceiling: thick pockets push it high
        // and fast; ambient corridor haze only ever nudges it.
        float target = Mathf.Clamp01((density - AmbientFloor) * 1.15f);
        float buildRate = Mathf.Max(0.08f, target) / BuildSeconds; // denser = faster
        exposure = exposure < target
            ? Mathf.MoveTowards(exposure, target, buildRate * Time.deltaTime)
            : Mathf.MoveTowards(exposure, target, Time.deltaTime / DecaySeconds);
        exposure = Mathf.Clamp01(exposure);

        float w = Mathf.Pow(exposure, 1.2f);
        float t = Time.time;

        // The tumble: slow, layered, never repeating.
        float roll = (Mathf.Sin(t * 0.9f) * 4.5f + Mathf.Sin(t * 0.37f + 1.7f) * 3.0f) * w;
        float pitch = Mathf.Sin(t * 0.55f + 0.8f) * 1.8f * w;
        float yaw = Mathf.Sin(t * 0.28f) * 3.2f * w;
        sway.ExtraRotation = new Vector3(pitch, yaw, roll);

        float drift = Mathf.Sin(t * 0.42f + 2.6f) * 0.06f * w;
        sway.ExtraOffset = new Vector3(drift, 0f, 0f);

        if (visionMaterial != null) visionMaterial.SetFloat(Nausea, w);
    }
}
