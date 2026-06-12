using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public Animator DoorAnimator;

    public AudioSource DoorOpen;
    public AudioSource DoorClose;

    public GameObject Door;
    public bool Inner;

    private DoorState state;

    private void Start()
    {
        // One DoorState per door, shared by its inner and outer triggers.
        state = Door.GetComponent<DoorState>();
        if (state == null)
        {
            state = Door.AddComponent<DoorState>();
            state.DoorAnimator = DoorAnimator;
            state.Door = Door.transform;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) state.Enter(Inner, DoorOpen);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) state.Exit(DoorClose);
    }
}
