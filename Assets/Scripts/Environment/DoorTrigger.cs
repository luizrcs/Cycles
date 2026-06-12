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
        if (!other.CompareTag("Player")) return;

        // Both bodies are tagged Player so doors open for the double too —
        // but the double leaves them hanging open behind it.
        bool isDouble = other.GetComponent<PlayerMovement>() == null;
        state.Exit(DoorClose, isDouble ? 10f : 0f);
    }
}
