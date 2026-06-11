using UnityEngine;

public class StepSounds : MonoBehaviour
{
    public AudioClip[] Sounds;

    private AudioSource audioSource;

    private float lastTime = 0f;
    private const float Delay = 0.25f;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayStepSound()
    {
        if (!audioSource.isPlaying)
        {
            float time = Time.time;
            if (time - lastTime > Delay)
            {
                lastTime = time;
                audioSource.clip = Sounds[Random.Range(0, Sounds.Length)];
                audioSource.Play();
            }
        }
    }
}
