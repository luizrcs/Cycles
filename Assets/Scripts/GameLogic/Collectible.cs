using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectible : MonoBehaviour
{
    public GameObject Self;
    public int Id;

    public AudioSource Sound;

    private ObjectivesController objectives;

    private void Start()
    {
        objectives = transform.parent.parent.GetComponent<CollectibleContainer>().Objectives;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) Collect();
    }

    private void Collect()
    {
        objectives.Collect(Id);
        Sound.Play();
        Self.SetActive(false);
    }
}
