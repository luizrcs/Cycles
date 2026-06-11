using UnityEngine;

public class Collectible : MonoBehaviour
{
    public GameObject Self;
    public int Id;

    public AudioSource Sound;

    private ObjectivesController objectives;
    private bool collected;

    private void Start()
    {
        objectives = transform.parent.parent.GetComponent<CollectibleContainer>().Objectives;
    }

    private void OnTriggerEnter(Collider other)
    {
        // The AntiPlayer shares the "Player" tag (so doors open for it);
        // only the real player carries PlayerMovement.
        if (!collected && other.GetComponent<PlayerMovement>() != null) Collect();
    }

    private void Collect()
    {
        collected = true;

        objectives.Collect(Id);
        Sound.Play();
        Self.SetActive(false);
    }
}
