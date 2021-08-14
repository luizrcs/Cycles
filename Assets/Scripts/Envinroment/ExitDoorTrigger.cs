using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitDoorTrigger : MonoBehaviour
{
    public Animator DoorAnimator;
    public AudioSource DoorOpen;

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "Player")
        {
            DoorAnimator.Play("OuterDoorOpen");
            DoorOpen.Play();
        }
    }
}
