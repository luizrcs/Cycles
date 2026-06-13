using UnityEngine;

// The missing half of the sanity system (USER design, sessions 2/4): darkness
// itself is hostile. Standing where no working lamp reaches — a dead stretch,
// a brownout, the endgame cascade — slowly raises the heart (through
// DreadController), and stepping back under a live bulb drains it at more
// than twice the speed. Capped well below the double's own dread so it
// unnerves without ever blocking navigation. Added at runtime by DetectPlayer.
public class DarknessDread : MonoBehaviour
{
    private DreadController dread;
    private Light[] lights;
    private float nextSample;
    private float darkSeconds;

    private const float SampleEvery = 0.35f;
    private const float DarkThreshold = 0.18f; // light level below this is "dark"
    private const float Grace = 3f;            // seconds of dark before it registers
    private const float RampSeconds = 14f;     // dark seconds to full (capped) dread
    private const float HealRate = 2.5f;       // light drains dark-time this much faster
    private const float Cap = 0.45f;

    public void Init(DreadController dreadController)
    {
        dread = dreadController;
    }

    void Update()
    {
        if (dread == null) return;

        if (Time.time >= nextSample)
        {
            nextSample = Time.time + SampleEvery;

            // Cache the deck's lights once it has finished generating them
            // (moonlight shafts included — moonlight calms too).
            if (lights == null && Time.timeSinceLevelLoad > 1f)
                lights = FindObjectsByType<Light>();

            float lit = lights != null ? LightLevel() : 1f;
            darkSeconds = lit < DarkThreshold
                ? darkSeconds + SampleEvery
                : Mathf.Max(0f, darkSeconds - SampleEvery * HealRate);
        }

        dread.DarknessDread = Mathf.Clamp01((darkSeconds - Grace) / RampSeconds) * Cap;
    }

    // How much working light reaches this spot: intensity-weighted inverse
    // falloff of every enabled point/spot in range. A flashing bulb gives
    // intermittent relief — exactly as unnerving as it should be.
    private float LightLevel()
    {
        Vector3 position = transform.position;
        float sum = 0f;
        foreach (Light light in lights)
        {
            if (light == null || !light.enabled || !light.gameObject.activeInHierarchy) continue;
            if (light.type == LightType.Directional) continue;

            float distance = Vector3.Distance(position, light.transform.position);
            if (distance >= light.range) continue;
            float k = 1f - distance / light.range;
            sum += light.intensity * k * k;
            if (sum > 3f) break; // clearly lit; stop counting
        }
        return Mathf.Clamp01(sum / 1.5f);
    }
}
