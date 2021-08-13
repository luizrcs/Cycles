using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntiPlayerFollow : MonoBehaviour
{
    public GameObject Player;

    public Animator antiPlayerAnimator;

    private Rigidbody rigidbody;

    private PlayerPath playerPath;
    private Vector3 lastTargetPosition;

    private float waitTime = 5f;
    private float timeResolution = 100f;

    void Start()
    {
        playerPath = Player.GetComponent<PlayerPath>();
        rigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float currentTime = Time.time;
        if (playerPath.Queue.Count > 0 && currentTime > waitTime + PeekTime())
        {
            Vector3 targetPosition = TargetPosition();
            if (targetPosition != lastTargetPosition)
            {
                lastTargetPosition = targetPosition;
                antiPlayerAnimator.SetBool("isRunning", true);
                rigidbody.MovePosition(targetPosition);
            }
            else antiPlayerAnimator.SetBool("isRunning", false);

            Vector3 targetRotation = transform.rotation.eulerAngles;
            targetRotation.y = TargetRotationY();
            rigidbody.MoveRotation(Quaternion.Euler(targetRotation));
        }
    }

    private float PeekTime()
    {
        ulong encodedValue = playerPath.Queue.Peek();
        return (encodedValue >> 40) / timeResolution;
    }

    private Vector3 TargetPosition()
    {
        ulong encodedValue = playerPath.Queue.Peek();

        float z = ((encodedValue >>= 8) & 0xFFFF) / 10f;
        float x = ((encodedValue >>= 16) & 0xFFFF) / 10f;

        return new Vector3(x, Player.transform.position.y, z);
    }

    private float TargetRotationY()
    {
        ulong encodedValue = playerPath.Queue.Dequeue();
        return (encodedValue & 0xFF) / 256f * 360f;
    }
}
