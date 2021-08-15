using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSoundsController : MonoBehaviour
{
    public bool Active = true;

    public AudioClip[] sounds;
    public AudioClip TimeParadox;

    private AudioSource audioSource;

    private float lastTime = 0f;
    private float delay = 5f;

    private System.Random random = new();

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (Active)
        {
            float time = Time.time;
            if (time - lastTime > delay)
            {
                lastTime = time;

                int index = random.Next(sounds.Length);
                audioSource.clip = sounds[index];
                audioSource.Play();
            }
        }
    }

    public void PlayTimeParadox()
    {
        Active = false;

        audioSource.clip = TimeParadox;
        audioSource.Play();
    }
}
