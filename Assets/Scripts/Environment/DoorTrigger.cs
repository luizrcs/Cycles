using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public Animator DoorAnimator;

    public AudioSource DoorOpen;
    public AudioSource DoorClose;

    public GameObject Door;
    public bool Inner;

    // Doors swing positive (inner) or negative (outer) around Y. The old code
    // compared the raw quaternion Y component; the signed angle is what it meant.
    private float DoorAngle => Mathf.DeltaAngle(0f, Door.transform.localEulerAngles.y);

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            bool isOpen = Mathf.Abs(DoorAngle) > 1f;
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
            float angle = DoorAngle;
            if (angle > 1f)
            {
                DoorAnimator.Play("InnerDoorClose");
                DoorClose.Play();
            }
            else if (angle < -1f)
            {
                DoorAnimator.Play("OuterDoorClose");
                DoorClose.Play();
            }
        }
    }
}
