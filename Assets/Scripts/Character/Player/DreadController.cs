using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// The player's body knows the double is near before the player does. Drives:
//
//  - a wrong, arrhythmic heartbeat: synthesized lub-dub thumps that accelerate
//    with proximity, with premature beats and skipped beats,
//  - the camera "pulse": every beat kicks the FOV through a barely-damped
//    spring — it swells fast, collapses past rest and rings. Erratic, never a
//    pretty easing curve,
//  - a global post volume (lens distortion + chromatic aberration + vignette)
//    whose weight throbs on the same beat,
//  - the stare rule: hold the double in the middle of your view for ~3 s
//    (line of sight to ANY part of its body — corner-peeking counts) and it
//    notices — and comes.
//
// During the battle itself (DetectPlayer.State 2, screen black, the stab
// sounds) everything here ducks fast so the author's punch audio owns the
// mix. Surviving leaves an afterShock — the heart keeps pounding for several
// seconds after you wake — and a permanent low floor for the rest of the run.
//
// GazeDiscipline feeds ExternalDread (the watch-the-corridor rule shares
// these effects). Added at runtime by DetectPlayer onto the player camera.
public class DreadController : MonoBehaviour
{
    // Extra dread injected by other systems (GazeDiscipline). 0..1.
    public float ExternalDread;

    // The dark side of the sanity system: set by DarknessDread (already
    // capped there) — standing in deep darkness raises the heart, working
    // lamps drain it. 0..1.
    public float DarknessDread;

    private DetectPlayer detect;
    private AntiPlayerFollow follow;
    private AntiPlayerGlitch glitch;
    private Transform player;
    private Transform doubleTransform;

    private Camera cam;
    private float baseFov;

    private AudioSource heartSource;
    private AudioClip thump;

    private Volume volume;
    private LensDistortion lens;

    private float intensity;
    private float fovKick, fovVelocity;
    private float nextBeat;
    private float dubAt = -1f;
    private float beatEnvelope;

    private int previousDetectState;
    private float afterShock;
    private bool survivedEncounter;

    private float stare;
    private const float StareLimit = 3f;
    private const float StareMaxDistance = 35f;
    private const float StareMaxAngle = 16f;

    public void Init(DetectPlayer detectPlayer, AntiPlayerFollow antiPlayerFollow, Transform playerTransform)
    {
        detect = detectPlayer;
        follow = antiPlayerFollow;
        player = playerTransform;
        doubleTransform = antiPlayerFollow.transform;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        baseFov = cam.fieldOfView;

        heartSource = gameObject.AddComponent<AudioSource>();
        heartSource.playOnAwake = false;
        heartSource.spatialBlend = 0f;
        thump = ProceduralAudio.MakeHeartThump();

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        lens = profile.Add<LensDistortion>();
        lens.intensity.Override(-0.32f);
        var chroma = profile.Add<ChromaticAberration>();
        chroma.intensity.Override(0.9f);
        var vignette = profile.Add<Vignette>();
        vignette.intensity.Override(0.42f);
        vignette.smoothness.Override(0.5f);

        var go = new GameObject("DreadVolume");
        volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.profile = profile;
        volume.weight = 0f;
    }

    void Update()
    {
        if (follow == null || detect == null) return;

        // Surviving a battle: heart still hammering as you wake, then a
        // permanent unease floor — your body remembers.
        if (previousDetectState == 2 && detect.State == 0)
        {
            afterShock = 1f;
            survivedEncounter = true;
        }
        previousDetectState = detect.State;
        afterShock = Mathf.MoveTowards(afterShock, 0f, 0.14f * Time.deltaTime);

        UpdateStare();

        float target = TargetIntensity();
        float downSpeed = detect.State == 2 ? 1.8f : 0.5f; // duck fast under the battle
        intensity = target > intensity
            ? Mathf.MoveTowards(intensity, target, 2.5f * Time.deltaTime)
            : Mathf.MoveTowards(intensity, target, downSpeed * Time.deltaTime);

        UpdateHeart();
        UpdatePulse();

        // The film damage is a dying effect in-game: it follows this danger.
        FilmDamage.ReportDanger(intensity);
    }

    private float TargetIntensity()
    {
        if (detect.State == 3) return 0f;
        if (detect.State == 2) return 0f; // battle: the stab sounds own the mix
        if (detect.State == 1) return 1f;

        // Before the double boards only the gaze rule and the dark speak (the
        // recording is already running, and the body fears darkness always).
        if (!follow.Engaged) return Mathf.Max(Mathf.Clamp01(ExternalDread), DarknessDread);

        float distance = Vector3.Distance(doubleTransform.position, player.position);
        float proximity = Mathf.Clamp01(1f - (distance - 3f) / 20f);
        float floor = survivedEncounter ? 0.12f : 0f;

        return Mathf.Max(proximity, stare / StareLimit * 0.85f, Mathf.Clamp01(ExternalDread),
            afterShock * 0.75f, floor, DarknessDread);
    }

    // Glances are free; holding it in the middle of the view is not. Brief
    // looks decay; ~3 s of sustained staring provokes it even if it never saw
    // you. Partial cover does not protect you: seeing ANY part of its body
    // (head, chest, legs, either shoulder) counts.
    private void UpdateStare()
    {
        if (glitch == null) glitch = follow.GetComponent<AntiPlayerGlitch>();

        bool staring = false;
        if (follow.Engaged && detect.State == 0 && (follow.State == 1 || follow.State == 2))
        {
            Vector3 eye = transform.position;
            Vector3 center = doubleTransform.position;
            Vector3 to = center + Vector3.up * 0.4f - eye;

            if (to.magnitude < StareMaxDistance && Vector3.Angle(transform.forward, to) < StareMaxAngle)
            {
                Vector3 flat = to;
                flat.y = 0f;
                Vector3 right = flat.sqrMagnitude > 0.01f
                    ? Vector3.Cross(Vector3.up, flat.normalized)
                    : Vector3.right;

                // The double's own colliders are triggers — a fully clear
                // line means nothing stands between the eye and that body part.
                Vector3[] points =
                {
                    center + Vector3.up * 1.0f,
                    center + Vector3.up * 0.3f,
                    center - Vector3.up * 0.7f,
                    center + Vector3.up * 0.3f + right * 0.45f,
                    center + Vector3.up * 0.3f - right * 0.45f,
                };
                foreach (Vector3 point in points)
                {
                    if (!Physics.Linecast(eye, point, out _, ~0, QueryTriggerInteraction.Ignore))
                    {
                        staring = true;
                        break;
                    }
                }
            }
        }

        stare = staring
            ? stare + Time.deltaTime
            : Mathf.MoveTowards(stare, 0f, 2f * Time.deltaTime);

        if (glitch != null) glitch.StareBoost = Mathf.Clamp01(stare / StareLimit);

        if (stare >= StareLimit)
        {
            stare = 0f;
            detect.ProvokeFromStare();
        }
    }

    private void UpdateHeart()
    {
        if (intensity < 0.05f) return;
        float t = Time.time;

        if (dubAt > 0f && t >= dubAt)
        {
            dubAt = -1f;
            heartSource.pitch = Random.Range(1.05f, 1.2f);
            heartSource.PlayOneShot(thump, 0.5f * intensity);
        }

        if (t >= nextBeat)
        {
            float interval = Mathf.Lerp(1.15f, 0.42f, intensity) * Random.Range(0.85f, 1.15f);
            float roll = Random.value;
            if (roll < 0.12f) interval *= 0.45f;      // premature beat — it stumbles
            else if (roll < 0.2f) interval *= 1.8f;   // skipped beat — it stops...
            nextBeat = t + interval;
            dubAt = t + 0.16f;

            heartSource.pitch = Random.Range(0.92f, 1.05f);
            heartSource.PlayOneShot(thump, Mathf.Lerp(0.25f, 0.95f, intensity));

            fovVelocity += Random.Range(28f, 50f) * intensity;
            beatEnvelope = 1f;
        }
    }

    private void UpdatePulse()
    {
        // Under-damped spring around rest: ~0.4 s per swell/collapse cycle,
        // overshooting below base FOV before settling — the wrong heartbeat
        // seen through the eyes.
        const float stiffness = 230f, damping = 7f;
        fovVelocity += (-stiffness * fovKick - damping * fovVelocity) * Time.deltaTime;
        fovKick += fovVelocity * Time.deltaTime;
        beatEnvelope = Mathf.MoveTowards(beatEnvelope, 0f, 2.2f * Time.deltaTime);

        cam.fieldOfView = baseFov + fovKick;

        if (volume != null)
        {
            volume.weight = Mathf.Clamp01(intensity * (0.45f + 0.55f * beatEnvelope));
            lens.intensity.value = -0.32f - 0.1f * beatEnvelope * intensity;
        }
    }
}
