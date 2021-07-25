using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController playerController;

    public int Speed;

    void Update()
    {
        float movementX = Input.GetAxis("Horizontal");
        float movementZ = Input.GetAxis("Vertical");
        Vector3 movement = (transform.right * movementX + transform.forward * movementZ) * Speed * Time.deltaTime;
        playerController.Move(movement);
    }
}
