using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public Animator DoorAnimator;

    public AudioSource DoorOpen;
    public AudioSource DoorClose;

    public GameObject Door;
    public bool Inner;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            bool isOpen = Door.transform.localRotation.y != 0f;
            if (!isOpen)
            {
                DoorAnimator.Play((Inner ? "Inner" : "Outer") + "DoorOpen");
                DoorOpen.Play();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            float doorRotation = Door.transform.localRotation.y;
            if (doorRotation > 0f)
            {
                DoorAnimator.Play("InnerDoorClose");
                DoorClose.Play();
            }
            else if (doorRotation < 0f)
            {
                DoorAnimator.Play("OuterDoorClose");
                DoorClose.Play();
            }
        }
    }
}
