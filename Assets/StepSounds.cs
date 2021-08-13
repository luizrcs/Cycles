using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StepSounds : MonoBehaviour
{
    public AudioClip[] Sounds;

    private AudioSource audioSource;

    private float lastTime = 0f;
    private float delay = 0.25f;

    private System.Random random = new System.Random();

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayStepSound()
    {
        if (!audioSource.isPlaying)
        {
            float time = Time.time;
            if (time - lastTime > delay)
            {
                lastTime = time;
                int index = random.Next(Sounds.Length);
                audioSource.clip = Sounds[index];
                audioSource.Play();
            }
        }
    }
}
