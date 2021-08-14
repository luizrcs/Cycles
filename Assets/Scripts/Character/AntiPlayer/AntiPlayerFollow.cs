using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntiPlayerFollow : MonoBehaviour
{
    public int State = 1;

    public DeckGeneration DeckGeneration;

    public Animator AntiPlayerAnimator;
    public StepSounds StepSounds;
    private Rigidbody rigidbody;

    public GameObject Player;
    private PlayerPath playerPath;

    public Vector3 CurrentTargetPosition;
    private int lastTargetDirection;
    private int targetMatrixX, targetMatrixY;

    private float waitTime = 5f;
    private float timeResolution = 100f;

    private System.Random Random = new();

    void Start()
    {
        playerPath = Player.GetComponent<PlayerPath>();
        rigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        switch (State)
        {
            case 1:
                float currentTime = Time.time;
                if (playerPath.Queue.Count > 0 && currentTime > waitTime + PeekTime())
                {
                    Vector3 targetPosition = TargetPosition();
                    if (targetPosition != CurrentTargetPosition)
                    {
                        CurrentTargetPosition = targetPosition;
                        rigidbody.MovePosition(CurrentTargetPosition);

                        AntiPlayerAnimator.SetBool("isRunning", true);
                        StepSounds.PlayStepSound();
                    }
                    else AntiPlayerAnimator.SetBool("isRunning", false);

                    Vector3 targetRotation = transform.rotation.eulerAngles;
                    targetRotation.y = TargetRotationY();
                    rigidbody.MoveRotation(Quaternion.Euler(targetRotation));
                }
                break;
            case 2:
                Roam();
                break;
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
        targetMatrixX = 1;
        targetMatrixY = 1;

        if ((tempDistance = Vector3.Distance(playerPosition, b)) > distance)
        {
            distance = tempDistance;
            chosen = b;
            targetMatrixX = 1;
            targetMatrixY = DeckGenerator.Height - 2;
        }

        if ((tempDistance = Vector3.Distance(playerPosition, c)) > distance)
        {
            distance = tempDistance;
            chosen = c;
            targetMatrixX = DeckGenerator.Width - 2;
            targetMatrixY = 1;
        }

        if (Vector3.Distance(playerPosition, d) > distance)
        {
            chosen = d;
            targetMatrixX = DeckGenerator.Width - 2;
            targetMatrixY = DeckGenerator.Height - 2;
        }

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

        return new(x, 4.75f, z);
    }

    private float TargetRotationY()
    {
        ulong encodedValue = playerPath.Queue.Dequeue();
        return (encodedValue & 0xFF) / 256f * 360f;
    }

    private void Roam()
    {
        Vector3 currentPosition = transform.position;
        if (currentPosition != CurrentTargetPosition)
        {
            float step = 7.5f * Time.deltaTime;
            transform.position = Vector3.MoveTowards(currentPosition, CurrentTargetPosition, step);
            StepSounds.PlayStepSound();

            Quaternion rotation = Quaternion.LookRotation(CurrentTargetPosition - currentPosition);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 10f * Time.deltaTime);
        }
        else
        {
            int[,] matrix = DeckGeneration.Matrix;
            int direction;

            while (true)
            {
                direction = Random.Next(8);
                if (direction >= 4) direction = lastTargetDirection;
                switch (direction)
                {
                    case 0:
                        if (matrix[targetMatrixX + 1, targetMatrixY] == 1)
                        {
                            targetMatrixX += 2;
                            CurrentTargetPosition = new(currentPosition.x + 7.5f, 4.75f, currentPosition.z);
                            goto afterWhile;
                        }
                        break;
                    case 1:
                        if (matrix[targetMatrixX, targetMatrixY + 1] == 1)
                        {
                            targetMatrixY += 2;
                            CurrentTargetPosition = new(currentPosition.x, 4.75f, currentPosition.z + 7.5f);
                            goto afterWhile;
                        }
                        break;
                    case 2:
                        if (matrix[targetMatrixX - 1, targetMatrixY] == 1)
                        {
                            targetMatrixX -= 2;
                            CurrentTargetPosition = new(currentPosition.x - 7.5f, 4.75f, currentPosition.z);
                            goto afterWhile;
                        }
                        break;
                    case 3:
                        if (matrix[targetMatrixX, targetMatrixY - 1] == 1)
                        {
                            targetMatrixY -= 2;
                            CurrentTargetPosition = new(currentPosition.x, 4.75f, currentPosition.z - 7.5f);
                            goto afterWhile;
                        }
                        break;
                }
            }

        afterWhile:
            lastTargetDirection = direction;
        }
    }
}
