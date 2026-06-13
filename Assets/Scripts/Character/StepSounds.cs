using UnityEngine;

public class StepSounds : MonoBehaviour
{
    public AudioClip[] Sounds;

    private AudioSource audioSource;

    private float lastTime = 0f;
    private const float Delay = 0.25f;

    // Footstep materiality: corridors wear a carpet runner — soft, muffled —
    // while rooms and doorways are bare wood, bright and hard. No separate
    // clip sets exist, so the surface is sold through DSP, decided per step
    // from the deck matrix. Outside the Game scene this stays inert.
    private DeckGeneration deck;
    private AudioLowPassFilter surfaceFilter;
    private float baseVolume;
    private const float MatrixCell = 3.75f;
    private const float CarpetCutoff = 2400f;
    private const float CarpetVolume = 0.74f;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        baseVolume = audioSource.volume;

        // Player only: the double's steps are already deliberately wrong
        // (pitched, reverbed, occlusion-filtered by AntiPlayerNoise — which
        // owns the one low-pass filter its steps object is allowed).
        deck = FindAnyObjectByType<DeckGeneration>();
        if (deck != null && GetComponentInParent<PlayerMovement>() != null)
        {
            surfaceFilter = gameObject.AddComponent<AudioLowPassFilter>();
            surfaceFilter.cutoffFrequency = 22000f;
        }
    }

    public void PlayStepSound()
    {
        if (!audioSource.isPlaying)
        {
            float time = Time.time;
            if (time - lastTime > Delay)
            {
                lastTime = time;
                ApplySurface();
                audioSource.clip = Sounds[Random.Range(0, Sounds.Length)];
                audioSource.Play();
            }
        }
    }

    private void ApplySurface()
    {
        if (surfaceFilter == null) return;
        bool carpet = OnCorridorCarpet();
        surfaceFilter.cutoffFrequency = carpet ? CarpetCutoff : 22000f;
        audioSource.volume = baseVolume * (carpet ? CarpetVolume : 1f);
    }

    private bool OnCorridorCarpet()
    {
        int[,] matrix = deck.Matrix;
        if (matrix == null) return true;

        int x = Mathf.RoundToInt(transform.position.x / MatrixCell);
        int y = Mathf.RoundToInt(transform.position.z / MatrixCell);
        if (x < 0 || y < 0 || x >= DeckGenerator.Width || y >= DeckGenerator.Height) return true;

        return matrix[x, y] == 1; // corridor = carpet; rooms/doorways = wood
    }
}
