using System.Collections;
using UnityEngine;

public class NarrationController : MonoBehaviour
{
    private AudioSource narrator;
    public AudioSource BackgroundSounds;

    public AudioClip[] Clips;
    private int index = 0;

    // Seconds before each clip; tuned to the storyboard panel animations.
    private static readonly float[] ClipDelays = { 3f, 5f, 6f, 7f, 7f, 7f };

    private void Start()
    {
        narrator = GetComponent<AudioSource>();
    }

    public void StartNarration()
    {
        StartCoroutine(_StartNarration());
    }

    IEnumerator _StartNarration()
    {
        yield return Fades.Volume(BackgroundSounds, 0.5f, 0.125f, 1.875f);

        foreach (float delay in ClipDelays)
        {
            yield return new WaitForSeconds(delay);

            narrator.clip = Clips[index++];
            narrator.Play();
        }
    }
}
