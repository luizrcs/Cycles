using UnityEngine;

// Ship-at-sea feel: a slow roll around the ship's length axis plus a gentle
// heave, and a subtle step bob while walking. The sway is written absolutely
// onto a runtime-created rig between the player and the camera, so it never
// fights FirstPersonController's look rotation and cannot accumulate drift.
public class CameraSway : MonoBehaviour
{
    public float RollAmplitude = 1.4f;     // degrees
    public float RollPeriod = 9f;          // seconds per full roll cycle
    public float HeaveAmplitude = 0.045f;  // meters
    public float HeavePeriod = 6.5f;
    public float WalkBobAmplitude = 0.035f;
    public float WalkBobFrequency = 1.6f;  // step cycles per second

    // Extra head rotation/offset written by other systems (MistNausea's
    // drunk tumble) — composed here so nothing fights over the rig.
    [HideInInspector] public Vector3 ExtraRotation;
    [HideInInspector] public Vector3 ExtraOffset;

    private Transform rig;
    private Vector3 rigBasePosition;
    private CharacterController characterController;
    private float bobPhase;
    private float bobWeight;

    void Start()
    {
        characterController = GetComponentInParent<CharacterController>();

        rig = new GameObject("CameraSwayRig").transform;
        rig.SetParent(transform.parent, false);
        rig.localPosition = transform.localPosition;
        rig.localRotation = Quaternion.identity;
        transform.SetParent(rig, true);
        transform.localPosition = Vector3.zero;

        rigBasePosition = rig.localPosition;
    }

    void LateUpdate()
    {
        float t = Time.time;

        // The maze (ship) is longest along world Z; rolling around that axis
        // reads as full roll when looking down a lengthwise corridor and as a
        // slight pitch when looking across the ship.
        float roll = RollAmplitude * Mathf.Sin(2f * Mathf.PI * t / RollPeriod);
        float facingAlong = Mathf.Abs(Vector3.Dot(transform.right, Vector3.right));
        float rollPart = roll * facingAlong;
        float pitchPart = roll * 0.5f * Vector3.Dot(transform.forward, Vector3.right);

        float heave = HeaveAmplitude * Mathf.Sin(2f * Mathf.PI * t / HeavePeriod);

        // Step bob fades in/out with actual movement so standing still is calm.
        bool moving = characterController != null && characterController.velocity.sqrMagnitude > 0.5f;
        bobWeight = Mathf.MoveTowards(bobWeight, moving ? 1f : 0f, Time.deltaTime * 3f);
        if (bobWeight > 0f) bobPhase += Time.deltaTime * WalkBobFrequency * 2f * Mathf.PI;
        float bob = WalkBobAmplitude * bobWeight * Mathf.Sin(bobPhase);

        rig.localRotation = Quaternion.Euler(
            pitchPart + ExtraRotation.x, ExtraRotation.y, rollPart + ExtraRotation.z);
        rig.localPosition = rigBasePosition + new Vector3(0f, heave + bob, 0f) + ExtraOffset;
    }
}
