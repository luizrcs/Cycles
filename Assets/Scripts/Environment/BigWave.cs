using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The rare BIG wave (random tension category): once every minute or two the
// sea lands a real one — for a single roll period the ship's sway triples,
// a deep hull groan swells from one side, and the corridor ceiling lamps
// swing on their mounts. The first one earns the author's unused voice line:
// "que som foi esse?". Never harmful; purely the world refusing to be still.
// Added at runtime by DeckGeneration.
public class BigWave : MonoBehaviour
{
    private DeckGeneration deck;
    private CameraSway sway;
    private GameLogic gameLogic;
    private DetectPlayer detect;
    private AudioSource groanSource;
    private AudioClip groan;

    private readonly List<(Transform mount, Quaternion baseRotation, float phase)> swingingLamps = new();

    private float nextWaveAt;
    private bool saidWhatSound;

    private const float MinInterval = 70f;
    private const float MaxInterval = 140f;
    private const float Boost = 3f;
    private const float SwingDegrees = 6f;

    void Start()
    {
        deck = GetComponent<DeckGeneration>();
        sway = FindAnyObjectByType<CameraSway>();
        gameLogic = FindAnyObjectByType<GameLogic>();
        detect = FindAnyObjectByType<DetectPlayer>();

        groan = ProceduralAudio.MakeGroanSwell();
        var go = new GameObject("HullGroan");
        go.transform.SetParent(transform, false);
        groanSource = go.AddComponent<AudioSource>();
        groanSource.playOnAwake = false;
        groanSource.spatialBlend = 1f;
        groanSource.dopplerLevel = 0f;
        groanSource.rolloffMode = AudioRolloffMode.Linear;
        groanSource.minDistance = 6f;
        groanSource.maxDistance = 70f;

        // Only ceiling fixtures swing; sconces are bolted to the walls.
        foreach (Light lamp in deck.CorridorLamps)
        {
            if (lamp == null || lamp.transform.parent == null) continue;
            if (!lamp.transform.parent.name.Contains("Ceiling")) continue;
            swingingLamps.Add((lamp.transform, lamp.transform.localRotation,
                Random.Range(0f, 0.8f)));
        }

        nextWaveAt = Time.time + Random.Range(45f, 90f); // first one lands early-ish
    }

    void Update()
    {
        if (sway == null || Time.time < nextWaveAt) return;
        nextWaveAt = float.MaxValue;
        StartCoroutine(Wave());
    }

    IEnumerator Wave()
    {
        float period = sway.RollPeriod;

        // The groan starts a beat before the roll peaks — sound first, like
        // thunder that arrives wrong.
        Camera listener = Camera.main;
        if (listener != null)
        {
            Vector2 dir = Random.insideUnitCircle.normalized * Random.Range(10f, 20f);
            groanSource.transform.position =
                listener.transform.position + new Vector3(dir.x, Random.Range(-2f, 1f), dir.y);
        }
        groanSource.pitch = Random.Range(0.9f, 1.05f);
        groanSource.PlayOneShot(groan, 0.85f);

        if (!saidWhatSound) StartCoroutine(SayWhatSound());

        for (float t = 0f; t < period; t += Time.deltaTime)
        {
            float k = Mathf.Sin(Mathf.PI * t / period); // ease in and out
            sway.AmplitudeBoost = 1f + (Boost - 1f) * k;

            float swing = SwingDegrees * k * Mathf.Sin(2f * Mathf.PI * t / period);
            foreach (var (mount, baseRotation, phase) in swingingLamps)
            {
                if (mount == null) continue;
                mount.localRotation = baseRotation
                    * Quaternion.Euler(swing * Mathf.Cos(phase), 0f, swing * Mathf.Sin(phase + 1f));
            }
            yield return null;
        }

        sway.AmplitudeBoost = 1f;
        foreach (var (mount, baseRotation, _) in swingingLamps)
            if (mount != null) mount.localRotation = baseRotation;

        nextWaveAt = Time.time + Random.Range(MinInterval, MaxInterval);
    }

    // The author's unused clip, finally wired: the player reacts to the first
    // big wave — but never over an encounter, and never over another line.
    IEnumerator SayWhatSound()
    {
        saidWhatSound = true;
        yield return new WaitForSeconds(2.2f);

        if (gameLogic == null || gameLogic.Speech == null || gameLogic.WhatSound == null) yield break;
        if (detect != null && detect.State != 0) yield break;
        if (gameLogic.Speech.isPlaying) yield break;

        gameLogic.Speech.clip = gameLogic.WhatSound;
        gameLogic.Speech.Play();
    }
}
