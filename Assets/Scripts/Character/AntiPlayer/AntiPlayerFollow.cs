using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntiPlayerFollow : MonoBehaviour
{
    public bool Active = true;

    public DeckGeneration DeckGeneration;

    public Animator AntiPlayerAnimator;
    public StepSounds StepSounds;
    private Rigidbody rigidbody;

    public GameObject Player;
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
        if (Active)
        {
            float currentTime = Time.time;
            if (playerPath.Queue.Count > 0 && currentTime > waitTime + PeekTime())
            {
                Vector3 targetPosition = TargetPosition();
                if (targetPosition != lastTargetPosition)
                {
                    lastTargetPosition = targetPosition;
                    rigidbody.MovePosition(targetPosition);

                    AntiPlayerAnimator.SetBool("isRunning", true);
                    StepSounds.PlayStepSound();
                }
                else AntiPlayerAnimator.SetBool("isRunning", false);

                Vector3 targetRotation = transform.rotation.eulerAngles;
                targetRotation.y = TargetRotationY();
                rigidbody.MoveRotation(Quaternion.Euler(targetRotation));
            }
        }
    }

    public void Respawn()
    {
        float minX = DeckGeneration.minX;
        float minZ = DeckGeneration.minZ;
        float maxX = DeckGeneration.maxX;
        float maxZ = DeckGeneration.maxZ;

        Vector3 playerPosition = Player.transform.position;
        Vector3 a = new(minX, transform.position.y, minZ);
        Vector3 b = new(minX, transform.position.y, maxZ);
        Vector3 c = new(maxX, transform.position.y, minZ);
        Vector3 d = new(maxX, transform.position.y, maxZ);

        float tempDistance;
        float distance = Vector3.Distance(playerPosition, a);
        Vector3 chosen = a;

        if ((tempDistance = Vector3.Distance(playerPosition, b)) > distance)
        {
            distance = tempDistance;
            chosen = b;
        }

        if ((tempDistance = Vector3.Distance(playerPosition, c)) > distance)
        {
            distance = tempDistance;
            chosen = c;
        }

        if (Vector3.Distance(playerPosition, d) > distance) chosen = d;

        transform.position = chosen;
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

        return new(x, transform.position.y, z);
    }

    private float TargetRotationY()
    {
        ulong encodedValue = playerPath.Queue.Dequeue();
        return (encodedValue & 0xFF) / 256f * 360f;
    }
}
