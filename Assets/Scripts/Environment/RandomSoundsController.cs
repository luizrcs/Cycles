using UnityEngine;

// The ship groans from PLACES now: each random creak plays as a positional 3D
// source teleported to a random spot in the hull around the listener, so the
// ear can locate it (and mislocate the double because of it). The time
// paradox stays 2D — that one happens inside your head.
public class RandomSoundsController : MonoBehaviour
{
    public bool Active = true;

    public AudioClip[] sounds;
    public AudioClip TimeParadox;

    private AudioSource audioSource;
    private Transform listener;

    private float lastTime = 0f;
    private const float Delay = 5f;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.dopplerLevel = 0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 4f;
        audioSource.maxDistance = 40f;
    }

    void Update()
    {
        if (Active)
        {
            float time = Time.time;
            if (time - lastTime > Delay)
            {
                lastTime = time;

                if (listener == null && Camera.main != null) listener = Camera.main.transform;
                if (listener != null)
                {
                    Vector2 direction = Random.insideUnitCircle.normalized * Random.Range(7f, 22f);
                    transform.position = listener.position
                        + new Vector3(direction.x, Random.Range(-1.5f, 2.5f), direction.y);
                }

                audioSource.clip = sounds[Random.Range(0, sounds.Length)];
                audioSource.Play();
            }
        }
    }

    public void PlayTimeParadox()
    {
        Active = false;

        audioSource.Stop();
        audioSource.spatialBlend = 0f;
        audioSource.clip = TimeParadox;
        audioSource.Play();
    }
}
