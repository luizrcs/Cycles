using UnityEngine;

// Period radio / old recording treatment for a voice AudioSource:
// band-limited (telephone-ish 400 Hz – 3.4 kHz) with a touch of distortion.
[RequireComponent(typeof(AudioSource))]
public class RadioVoice : MonoBehaviour
{
    public float HighPassCutoff = 400f;
    public float LowPassCutoff = 3400f;
    [Range(0f, 1f)] public float Distortion = 0.12f;

    void Start()
    {
        var high = gameObject.AddComponent<AudioHighPassFilter>();
        high.cutoffFrequency = HighPassCutoff;

        var low = gameObject.AddComponent<AudioLowPassFilter>();
        low.cutoffFrequency = LowPassCutoff;

        var dist = gameObject.AddComponent<AudioDistortionFilter>();
        dist.distortionLevel = Distortion;
    }
}
