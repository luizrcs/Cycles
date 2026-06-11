using UnityEngine;

public class RandomSoundsController : MonoBehaviour
{
    public bool Active = true;

    public AudioClip[] sounds;
    public AudioClip TimeParadox;

    private AudioSource audioSource;

    private float lastTime = 0f;
    private const float Delay = 5f;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (Active)
        {
            float time = Time.time;
            if (time - lastTime > Delay)
            {
                lastTime = time;

                audioSource.clip = sounds[Random.Range(0, sounds.Length)];
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
