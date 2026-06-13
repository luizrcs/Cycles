using UnityEngine;

// The double's broken-transmission noise: a positional, synthesized loop that
// gets louder and angrier the closer it is — the SOMA-style proximity cue.
// You hear it through walls (faintly), which is the point: you learn to keep
// your distance before you ever see it.
//
// Also owns the sound of the double's WRONGNESS as a person:
//  - its own footsteps are pitched down with a roomy reverb (your steps, but
//    not quite),
//  - occasionally a short burst of phantom footsteps plays from its position
//    even while it stands still — the steps of a person who isn't stepping.
//
// During the battle (DetectPlayer.State 2) everything ducks fast so the
// author's punch/stab audio owns the mix. Added at runtime by AntiPlayerFollow.
public class AntiPlayerNoise : MonoBehaviour
{
    private AntiPlayerFollow follow;
    private DetectPlayer detect;
    private Transform player;
    private AudioSource source;
    private AudioSource phantomSource;

    // Wall occlusion: everything the double emits muffles behind geometry, so
    // a sound that reads as coming from a corridor really came down it.
    private AudioLowPassFilter noiseFilter;
    private AudioLowPassFilter stepsFilter;
    private AudioLowPassFilter phantomFilter;
    private float occlusion = 1f; // 1 = clear line, 0 = fully walled off

    private float nextPhantomAt;
    private int phantomStepsLeft;
    private float nextPhantomStepAt;

    // When walls block the direct line, a vague "echo" of the noise carries
    // from partway down the path between you — diffraction you can't quite
    // place, where the direct sound would be cleanly localizable.
    private AudioSource echoSource;

    // Structure-borne knocks from wherever it roams: audible across the deck,
    // unmuffled (they travel through the hull, not the air).
    private AudioSource knockSource;
    private AudioClip knockClip;
    private float nextKnockAt;

    void Start()
    {
        follow = GetComponent<AntiPlayerFollow>();
        detect = GetComponent<DetectPlayer>();
        player = follow.Player.transform;

        source = gameObject.AddComponent<AudioSource>();
        source.clip = ProceduralAudio.MakeGlitchLoop();
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 2f;
        source.maxDistance = 26f;
        source.volume = 0f;
        noiseFilter = gameObject.AddComponent<AudioLowPassFilter>();
        noiseFilter.cutoffFrequency = 22000f;

        MakeStepsWrong();

        var echoGO = new GameObject("EchoVoice");
        echoGO.transform.SetParent(transform, false);
        echoSource = echoGO.AddComponent<AudioSource>();
        echoSource.clip = source.clip;
        echoSource.loop = true;
        echoSource.playOnAwake = false;
        echoSource.spatialBlend = 1f;
        echoSource.dopplerLevel = 0f;
        echoSource.rolloffMode = AudioRolloffMode.Linear;
        echoSource.minDistance = 2f;
        echoSource.maxDistance = 30f;
        echoSource.volume = 0f;
        var echoFilter = echoGO.AddComponent<AudioLowPassFilter>();
        echoFilter.cutoffFrequency = 1100f;

        var knockGO = new GameObject("HullKnocks");
        knockGO.transform.SetParent(transform, false);
        knockSource = knockGO.AddComponent<AudioSource>();
        knockSource.playOnAwake = false;
        knockSource.spatialBlend = 1f;
        knockSource.dopplerLevel = 0f;
        knockSource.rolloffMode = AudioRolloffMode.Linear;
        knockSource.minDistance = 4f;
        knockSource.maxDistance = 65f;
        knockClip = ProceduralAudio.MakeMetalKnock();

        nextPhantomAt = Time.time + Random.Range(20f, 40f);
        nextKnockAt = Time.time + Random.Range(15f, 30f);
    }

    // Same clips as the player's feet, but lower and roomier — the player
    // should slowly realize the footsteps behind them are their own.
    private void MakeStepsWrong()
    {
        if (follow.StepSounds == null) return;
        var steps = follow.StepSounds.GetComponent<AudioSource>();
        if (steps != null)
        {
            steps.pitch = 0.82f;
            var reverb = steps.gameObject.AddComponent<AudioReverbFilter>();
            reverb.reverbPreset = AudioReverbPreset.Hallway;
            // Audio filters disallow duplicates — reuse one if it exists.
            stepsFilter = steps.gameObject.GetComponent<AudioLowPassFilter>();
            if (stepsFilter == null) stepsFilter = steps.gameObject.AddComponent<AudioLowPassFilter>();
            stepsFilter.cutoffFrequency = 22000f;
        }

        var go = new GameObject("PhantomSteps");
        go.transform.SetParent(transform, false);
        phantomSource = go.AddComponent<AudioSource>();
        phantomSource.playOnAwake = false;
        phantomSource.spatialBlend = 1f;
        phantomSource.dopplerLevel = 0f;
        phantomSource.rolloffMode = AudioRolloffMode.Linear;
        phantomSource.minDistance = 2f;
        phantomSource.maxDistance = 30f;
        var reverbFar = go.AddComponent<AudioReverbFilter>();
        reverbFar.reverbPreset = AudioReverbPreset.Cave;
        phantomFilter = go.AddComponent<AudioLowPassFilter>();
        phantomFilter.cutoffFrequency = 22000f;
    }

    void Update()
    {
        if (!follow.Engaged)
        {
            if (source.isPlaying) source.Stop();
            if (echoSource != null && echoSource.isPlaying) echoSource.Stop();
            return;
        }
        if (!source.isPlaying) source.Play();
        if (echoSource != null && !echoSource.isPlaying)
        {
            echoSource.Play();
            echoSource.timeSamples = source.timeSamples;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        float closeness = Mathf.Clamp01(1f - (distance - 2f) / 24f);
        bool battle = detect != null && detect.State == 2;
        bool aggro = detect != null && (detect.State == 1 || detect.State == 2);

        UpdateOcclusion();

        float target = battle
            ? 0f
            : (aggro ? 0.95f : 0.55f) * Mathf.Max(0.15f, closeness) * (0.35f + 0.65f * occlusion);
        float lerpSpeed = battle ? 8f : 4f;
        source.volume = Mathf.Lerp(source.volume, target, lerpSpeed * Time.deltaTime);

        // Erratic pitch wobble — a recording of a person that never happened.
        source.pitch = 1f + (Mathf.PerlinNoise(Time.time * 2.3f, 0.7f) - 0.5f)
            * (0.12f + 0.5f * closeness);

        UpdateEcho(battle, closeness);
        UpdatePhantomSteps(distance, battle);
        UpdateKnocks(battle);
    }

    private void UpdateEcho(bool battle, float closeness)
    {
        if (echoSource == null) return;

        Camera listener = Camera.main;
        if (listener != null)
            echoSource.transform.position =
                Vector3.Lerp(transform.position, listener.transform.position, 0.5f);

        // Speaks only when the direct line is blocked: the sound finds a way
        // around, vague and muffled — you know it's near, not where.
        float target = battle ? 0f : 0.5f * Mathf.Max(0.1f, closeness) * (1f - occlusion);
        echoSource.volume = Mathf.Lerp(echoSource.volume, target, 4f * Time.deltaTime);
        echoSource.pitch = source.pitch;
    }

    private void UpdateKnocks(bool battle)
    {
        if (knockSource == null || battle || detect == null || detect.State != 0) return;
        if (follow.State != 2) return; // only while it roams, hunting

        if (Time.time > nextKnockAt)
        {
            nextKnockAt = Time.time + Random.Range(18f, 42f);
            knockSource.pitch = Random.Range(0.85f, 1.1f);
            knockSource.PlayOneShot(knockClip, Random.Range(0.45f, 0.7f));
        }
    }

    // Two thin rays to its head and feet: any clear one means sound travels
    // freely; both blocked means walls in the way — muffle and quieten.
    // Smoothed so opening a door "lets the sound in" rather than snapping.
    private void UpdateOcclusion()
    {
        Camera listener = Camera.main;
        if (listener == null) return;

        Vector3 ear = listener.transform.position;
        bool clear =
            !Physics.Linecast(ear, transform.position + Vector3.up * 1.0f, out _, ~0, QueryTriggerInteraction.Ignore)
            || !Physics.Linecast(ear, transform.position - Vector3.up * 0.6f, out _, ~0, QueryTriggerInteraction.Ignore);

        occlusion = Mathf.MoveTowards(occlusion, clear ? 1f : 0f, 3.5f * Time.deltaTime);

        float cutoff = Mathf.Lerp(650f, 22000f, occlusion * occlusion);
        if (noiseFilter != null) noiseFilter.cutoffFrequency = cutoff;
        if (stepsFilter != null) stepsFilter.cutoffFrequency = cutoff;
        if (phantomFilter != null) phantomFilter.cutoffFrequency = cutoff;
    }

    private void UpdatePhantomSteps(float distance, bool battle)
    {
        if (phantomSource == null || battle || detect == null || detect.State != 0) return;
        if (follow.StepSounds == null || follow.StepSounds.Sounds == null
            || follow.StepSounds.Sounds.Length == 0) return;

        float t = Time.time;
        if (phantomStepsLeft <= 0 && t > nextPhantomAt && distance < 32f)
        {
            phantomStepsLeft = Random.Range(3, 6);
            nextPhantomStepAt = t;
            nextPhantomAt = t + Random.Range(16f, 38f);
        }

        if (phantomStepsLeft > 0 && t >= nextPhantomStepAt)
        {
            phantomStepsLeft--;
            nextPhantomStepAt = t + Random.Range(0.34f, 0.5f);
            phantomSource.pitch = Random.Range(0.62f, 0.74f);
            var clips = follow.StepSounds.Sounds;
            phantomSource.PlayOneShot(clips[Random.Range(0, clips.Length)], 0.5f);
        }
    }
}
