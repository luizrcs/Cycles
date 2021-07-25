using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntiPlayerFollow : MonoBehaviour
{
    public GameObject Player;

    private PlayerPath playerPath;
    private Vector3 currentTarget;

    private float waitTime = 5f;

    void Start()
    {
        playerPath = Player.GetComponent<PlayerPath>();
    }

    void Update()
    {
        float currentTime = Time.time;
        if (playerPath.Queue.Count > 0 && currentTime > waitTime + PeekTime())
            currentTarget = CurrentPosition();

        transform.position = currentTarget;
    }

    private float PeekTime()
    {
        ulong encodedValue = playerPath.Queue.Peek();
        return (encodedValue >> 32) / 100f;
    }

    private Vector3 CurrentPosition()
    {
        ulong encodedValue = playerPath.Queue.Dequeue();

        float x = ((encodedValue >> 16) & 0xFFFF) / 10f;
        float z = (encodedValue & 0xFFFF) / 10f;

        return new Vector3(x, Player.transform.position.y, z);
    }
}
