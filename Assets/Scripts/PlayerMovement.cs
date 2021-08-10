using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController PlayerController;
    public Animator Animator;

    public int Speed;

    void Update()
    {
        float movementX = Input.GetAxis("Horizontal");
        float movementZ = Input.GetAxis("Vertical");
        Vector3 movement = Vector3.ClampMagnitude((transform.right * movementX + transform.forward * movementZ), 1f);
        Vector3 motion = movement * Speed;
        PlayerController.Move(motion * Time.deltaTime);

        Animator.SetBool("isRunning", movement.magnitude != 0);
    }
}
