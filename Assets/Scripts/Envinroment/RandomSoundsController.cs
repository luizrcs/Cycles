using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSoundsController : MonoBehaviour
{
    public AudioClip[] sounds;

    private AudioSource audioSource;

    private float lastTime = 0f;
    private float delay = 5f;

    private System.Random random = new System.Random();

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
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
