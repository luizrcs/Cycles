using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController PlayerController;
    public Animator Animator;

    public StepSounds StepSounds;

    public bool LockMovement = false;
    public int Speed;

    void Update()
    {
        if (!LockMovement)
        {
            Vector3 currentPosition = transform.position;
            transform.position = new(currentPosition.x, 4.75f, currentPosition.z);

            float movementX = Input.GetAxis("Horizontal");
            float movementZ = Input.GetAxis("Vertical");

            Vector3 movement = Vector3.ClampMagnitude((transform.right * movementX + transform.forward * movementZ), 1f);
            Vector3 motion = movement * Speed;
            PlayerController.Move(motion * Time.deltaTime);

            bool hasMovement = movement.magnitude != 0;
            Animator.SetBool("isRunning", hasMovement);
            if (hasMovement) StepSounds.PlayStepSound();
        }
        else Animator.SetBool("isRunning", false);
    }
}
