using UnityEngine;

// One random cabin keeps a gramophone playing (location category): a slow
// music-box waltz under shellac crackle, with live wow & flutter on the
// pitch. As the DOUBLE nears the gramophone the song muffles and warps —
// the music is afraid of it before you are. Added at runtime by
// DeckGeneration into one of the generated rooms.
public class Gramophone : MonoBehaviour
{
    private AudioSource source;
    private AudioLowPassFilter filter;
    private Transform doubleTransform;
    private AntiPlayerFollow follow;

    private const float Volume = 0.34f;
    private const float WarpRange = 12f; // double within this distorts the song

    public void Init(AntiPlayerFollow antiPlayerFollow)
    {
        follow = antiPlayerFollow;
        doubleTransform = antiPlayerFollow.transform;
    }

    void Start()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.clip = ProceduralAudio.MakeWaltz();
        source.loop = true;
        source.spatialBlend = 1f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1.5f;
        source.maxDistance = 16f;
        source.volume = Volume;
        filter = gameObject.AddComponent<AudioLowPassFilter>();
        filter.cutoffFrequency = 7500f; // a horn speaker was never hi-fi
        source.Play();
    }

    void Update()
    {
        // Base wow & flutter of a worn turntable.
        float wow = (Mathf.PerlinNoise(Time.time * 0.6f, 0.3f) - 0.5f) * 0.035f;

        float menace = 0f;
        if (follow != null && follow.Engaged && doubleTransform != null)
        {
            float distance = Vector3.Distance(doubleTransform.position, transform.position);
            menace = Mathf.Clamp01(1f - distance / WarpRange);
        }

        // It drags, warps and drowns as the double approaches the horn.
        source.pitch = 1f + wow * (1f + 4f * menace) - 0.12f * menace;
        filter.cutoffFrequency = Mathf.Lerp(7500f, 700f, menace);
        source.volume = Volume * (1f - 0.45f * menace);
    }
}
