using UnityEngine;

// Shared open/close coordinator for one door. Both of a door's triggers
// (inner and outer side) talk to this instead of fighting each other, which
// used to make the door shake when the player lingered on a trigger edge:
// every Enter/Exit replayed an animation immediately. Now state changes are
// rate-limited and closing only happens once nobody is in either trigger.
public class DoorState : MonoBehaviour
{
    public Animator DoorAnimator;
    public Transform Door;

    private int occupancy;
    private float lastChange = -10f;
    private bool wantClose;

    private const float MinInterval = 0.6f;

    private float DoorAngle => Mathf.DeltaAngle(0f, Door.localEulerAngles.y);
    private bool IsOpen => Mathf.Abs(DoorAngle) > 1f;

    public void Enter(bool inner, AudioSource openSound)
    {
        occupancy++;
        wantClose = false;

        if (!IsOpen && Time.time - lastChange > MinInterval)
        {
            lastChange = Time.time;
            DoorAnimator.Play((inner ? "Inner" : "Outer") + "DoorOpen");
            openSound.Play();
        }
    }

    public void Exit(AudioSource closeSound)
    {
        occupancy = Mathf.Max(0, occupancy - 1);
        if (occupancy == 0) wantClose = true;
        pendingCloseSound = closeSound;
    }

    private AudioSource pendingCloseSound;

    void Update()
    {
        if (!wantClose || occupancy > 0) return;
        if (Time.time - lastChange <= MinInterval) return;

        float angle = DoorAngle;
        if (angle > 1f) DoorAnimator.Play("InnerDoorClose");
        else if (angle < -1f) DoorAnimator.Play("OuterDoorClose");
        else { wantClose = false; return; }

        lastChange = Time.time;
        wantClose = false;
        if (pendingCloseSound != null) pendingCloseSound.Play();
    }
}
