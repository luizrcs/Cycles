using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    public GameObject AntiPlayer;

    public float MouseSensitivity = 100.0f;

    public Transform PlayerTransform;

    public bool LockCamera = true;
    private float rotationX = 0.0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (!LockCamera)
        {
            float mouseX = Input.GetAxis("Mouse X") * MouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * MouseSensitivity * Time.deltaTime;

            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -90.0f, 90.0f);

            transform.localRotation = Quaternion.Euler(rotationX, 0.0f, 0.0f);
            PlayerTransform.Rotate(Vector3.up * mouseX);
        }
    }

    public void LookAtAntiPlayer()
    {
        Vector3 position = transform.position;
        Vector3 antiPlayerPosition = AntiPlayer.transform.position;
        Quaternion rotation = Quaternion.LookRotation(antiPlayerPosition - position);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 0.25f * Time.deltaTime);
        PlayerTransform.rotation = Quaternion.Slerp(PlayerTransform.rotation, rotation, 1f * Time.deltaTime);
    }

    public void ResetRotation()
    {
        PlayerTransform.rotation = Quaternion.identity;
    }
}
