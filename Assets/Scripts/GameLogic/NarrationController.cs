using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NarrationController : MonoBehaviour
{
    private AudioSource narrator;
    public AudioSource BackgroundSounds;

    public AudioClip[] Clips;
    private int index = 0;

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
        for (float f = 0.5f; f > 0.125f; f -= 0.0025f)
        {
            BackgroundSounds.volume = f;
            yield return new WaitForSeconds(0.0125f);
        }

        yield return new WaitForSeconds(3f);

        narrator.clip = Clips[index++];
        narrator.Play();
        yield return new WaitForSeconds(5f);

        narrator.clip = Clips[index++];
        narrator.Play();
        yield return new WaitForSeconds(6f);

        narrator.clip = Clips[index++];
        narrator.Play();
        yield return new WaitForSeconds(7f);

        narrator.clip = Clips[index++];
        narrator.Play();
        yield return new WaitForSeconds(7f);

        narrator.clip = Clips[index++];
        narrator.Play();
        yield return new WaitForSeconds(7f);

        narrator.clip = Clips[index++];
        narrator.Play();
    }
}
