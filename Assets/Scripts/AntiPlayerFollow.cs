using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntiPlayerFollow : MonoBehaviour
{
    public GameObject Player;

    public Animator antiPlayerAnimator;

    private PlayerPath playerPath;
    private Vector3 currentTargetPosition;
    private float currentTargetRotation;

    private float waitTime = 5f;
    private float timeResolution = 200f;

    void Start()
    {
        playerPath = Player.GetComponent<PlayerPath>();
    }

    void Update()
    {
        float currentTime = Time.time;
        if (playerPath.Queue.Count > 0 && currentTime > waitTime + PeekTime())
        {
            Vector3 newTargetPosition = CurrentPosition();
            antiPlayerAnimator.SetBool("isRunning", newTargetPosition != currentTargetPosition);
            currentTargetPosition = newTargetPosition;

            currentTargetRotation = CurrentRotation();
        }

        transform.position = currentTargetPosition;

        Quaternion rotation = transform.rotation;
        transform.rotation = new Quaternion(rotation.x, currentTargetRotation, rotation.z, rotation.w);
    }

    private float PeekTime()
    {
        ulong encodedValue = playerPath.Queue.Peek();
        return (encodedValue >> 40) / timeResolution;
    }

    private Vector3 CurrentPosition()
    {
        ulong encodedValue = playerPath.Queue.Peek();

        float z = ((encodedValue >>= 8) & 0xFFFF) / 10f;
        float x = ((encodedValue >>= 16) & 0xFFFF) / 10f;

        return new Vector3(x, Player.transform.position.y, z);
    }

    private float CurrentRotation()
    {
        ulong encodedValue = playerPath.Queue.Dequeue();
        return ((encodedValue & 0xFF) / 128f) - 1f;
    }
}
