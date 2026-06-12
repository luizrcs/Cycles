using UnityEngine;

// Breathing the mist makes you sick. Exposure builds with fog density × time
// — ambient corridor haze barely registers, the thick pockets are what get
// you (≈9 s to full inside one) — and drives a drunk tumble of the head:
// layered never-repeating roll/yaw/pitch plus a lateral sway, clearly above
// the ship's own ±1.4° motion once you've soaked. It decays much slower than
// it builds (≈25 s), so lingering costs you. Annoying enough to push you out
// of the pocket, never unplayable.
//
// Distinct on purpose from DreadController's heartbeat anxiety: that one is
// the double; this one is the air. Added at runtime by DetectPlayer onto the
// player camera (same object as CameraSway and DustAndMist).
public class MistNausea : MonoBehaviour
{
    private DustAndMist mist;
    private CameraSway sway;
    private float exposure;

    private const float AmbientFloor = 0.12f;  // density below this never builds
    private const float BuildSeconds = 9f;     // to full effect at pocket center
    private const float DecaySeconds = 25f;    // much slower than it builds

    void Start()
    {
        mist = GetComponent<DustAndMist>();
        if (mist == null) mist = FindFirstObjectByType<DustAndMist>();
        sway = GetComponent<CameraSway>();
        if (sway == null) sway = FindFirstObjectByType<CameraSway>();
        if (sway == null) enabled = false;
    }

    void Update()
    {
        float density = mist != null ? mist.DensityAt(transform.position) : 0f;

        // Only what exceeds the ambient haze poisons you, scaled so the heart
        // of a pocket builds at full rate.
        float poison = Mathf.Clamp01((density - AmbientFloor) / (1f - AmbientFloor));
        if (poison > 0.01f) exposure += poison * Time.deltaTime / BuildSeconds;
        else exposure -= Time.deltaTime / DecaySeconds;
        exposure = Mathf.Clamp01(exposure);

        // Near-linear onset with a soft toe: first whiffs noticeable, a full
        // soak unmistakable.
        float w = Mathf.Pow(exposure, 1.35f);
        float t = Time.time;

        float roll = (Mathf.Sin(t * 0.9f) * 4.5f + Mathf.Sin(t * 0.37f + 1.7f) * 3.0f) * w;
        float pitch = Mathf.Sin(t * 0.55f + 0.8f) * 1.8f * w;
        float yaw = Mathf.Sin(t * 0.28f) * 3.2f * w;
        sway.ExtraRotation = new Vector3(pitch, yaw, roll);

        // The floor tilts under you: a slow sideways drift of the head.
        float drift = Mathf.Sin(t * 0.42f + 2.6f) * 0.06f * w;
        sway.ExtraOffset = new Vector3(drift, 0f, 0f);
    }
}
