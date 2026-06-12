using UnityEngine;

// A failing incandescent lamp on a decaying ship. Two real-world failure modes:
//
//  Dimmer  — bad supply/filament: long, deep brownouts that sink to 10–25%
//            brightness over a second or two, hold, then crawl back up.
//            Occasionally drops out completely for a moment.
//  Flasher — loose contact: burns steadily, then a burst of harsh rapid
//            on/off sputtering (8–25 Hz), often ending in a dead second
//            before snapping back on.
//
// The lamp's glass emission follows the light so the fixture itself dies,
// not just its illumination. Added at runtime by DeckGeneration.
public class FlickeringLight : MonoBehaviour
{
    public enum FailureMode { Dimmer, Flasher }
    public FailureMode Mode;

    private Light lamp;
    private float baseIntensity;

    private Renderer[] emissiveRenderers;
    private Color[] baseEmissions;

    private float level = 1f;

    // dimmer state
    private float dimTarget = 1f;
    private float dimSpeed = 1f;
    private float nextDimChange;

    // flasher state
    private float burstUntil = -1f;
    private float deadUntil = -1f;
    private float nextBurst;
    private float sputterRate;

    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private AudioSource buzz;

    void Start()
    {
        lamp = GetComponent<Light>();
        baseIntensity = lamp.intensity;

        if (Mode == FailureMode.Flasher)
        {
            nextBurst = Time.time + Random.Range(1f, 6f);

            // Electricity sells the failure better than light alone: a quiet
            // mains buzz that follows the bulb's level and dies with it.
            buzz = gameObject.AddComponent<AudioSource>();
            buzz.clip = ProceduralAudio.GetBuzzLoop();
            buzz.loop = true;
            buzz.spatialBlend = 1f;
            buzz.dopplerLevel = 0f;
            buzz.rolloffMode = AudioRolloffMode.Linear;
            buzz.minDistance = 0.7f;
            buzz.maxDistance = 7f;
            buzz.volume = 0f;
            buzz.pitch = Random.Range(0.94f, 1.08f);
            buzz.Play();
        }

        emissiveRenderers = transform.parent != null
            ? transform.parent.GetComponentsInChildren<Renderer>()
            : new Renderer[0];
        baseEmissions = new Color[emissiveRenderers.Length];
        for (int i = 0; i < emissiveRenderers.Length; i++)
        {
            Material m = emissiveRenderers[i].material;
            baseEmissions[i] = m.HasProperty(EmissionColor) ? m.GetColor(EmissionColor) : Color.black;
        }
    }

    void Update()
    {
        level = Mode == FailureMode.Dimmer ? DimmerLevel() : FlasherLevel();
        Apply(level);
    }

    private float DimmerLevel()
    {
        float t = Time.time;

        if (t > nextDimChange)
        {
            // Pick the next brightness to crawl toward: usually a deep sag,
            // sometimes a recovery, rarely a full dropout.
            float roll = Random.value;
            if (roll < 0.15f) dimTarget = 0f;                       // dies for a bit
            else if (roll < 0.55f) dimTarget = Random.Range(0.1f, 0.3f);  // deep brownout
            else dimTarget = Random.Range(0.7f, 1f);                // recovers

            dimSpeed = Random.Range(0.6f, 2.5f);
            nextDimChange = t + Random.Range(1.5f, 4.5f);
        }

        return Mathf.MoveTowards(level, dimTarget, dimSpeed * Time.deltaTime);
    }

    private float FlasherLevel()
    {
        float t = Time.time;

        if (t < deadUntil) return 0f;

        if (t < burstUntil)
        {
            // Harsh sputter: square-wave-ish on/off with jitter.
            bool on = Mathf.PerlinNoise(t * sputterRate, 0.5f) > 0.45f;
            return on ? Random.Range(0.7f, 1f) : 0f;
        }

        if (t > nextBurst)
        {
            burstUntil = t + Random.Range(0.4f, 1.8f);
            sputterRate = Random.Range(8f, 25f);
            nextBurst = burstUntil + Random.Range(2.5f, 9f);

            // A third of bursts end with the lamp dead for a moment.
            if (Random.value < 0.33f) deadUntil = burstUntil + Random.Range(0.4f, 1.5f);
        }

        return 1f;
    }

    private void Apply(float l)
    {
        lamp.intensity = baseIntensity * l;
        lamp.enabled = l > 0.02f;

        if (buzz != null) buzz.volume = 0.16f * l + (l > 0.02f ? 0.04f : 0f);

        for (int i = 0; i < emissiveRenderers.Length; i++)
        {
            Material m = emissiveRenderers[i].material;
            if (m.HasProperty(EmissionColor)) m.SetColor(EmissionColor, baseEmissions[i] * l);
        }
    }
}
