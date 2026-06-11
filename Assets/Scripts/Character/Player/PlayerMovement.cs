using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController PlayerController;
    public Animator Animator;

    public StepSounds StepSounds;

    public bool LockMovement = true;
    public int Speed;

    private const float WalkY = 4.75f;

    private static readonly int IsRunning = Animator.StringToHash("isRunning");

    void Update()
    {
        if (!LockMovement)
        {
            Vector3 currentPosition = transform.position;
            if (!Mathf.Approximately(currentPosition.y, WalkY))
                transform.position = new(currentPosition.x, WalkY, currentPosition.z);

            float movementX = Input.GetAxis("Horizontal");
            float movementZ = Input.GetAxis("Vertical");

            Vector3 movement = Vector3.ClampMagnitude(transform.right * movementX + transform.forward * movementZ, 1f);
            PlayerController.Move(Speed * Time.deltaTime * movement);

            bool hasMovement = movement.sqrMagnitude > 0f;
            Animator.SetBool(IsRunning, hasMovement);
            if (hasMovement) StepSounds.PlayStepSound();
        }
        else Animator.SetBool(IsRunning, false);
    }
}
