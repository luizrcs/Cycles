using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPath : MonoBehaviour
{
    public Queue<ulong> Queue = new Queue<ulong>();

    private float lastTime = 0f;

    void Update()
    {
        float currentTime = Time.time;
        if (currentTime - lastTime > 0.01)
        {
            lastTime = currentTime;
            Queue.Enqueue(CurrentPosition());
        }
    }

    private ulong CurrentPosition()
    {
        ulong encodedValue = (ulong)(Time.time / 0.01f);

        Vector3 position = transform.position;
        ulong x = (ulong)(position.x * 10);
        ulong z = (ulong)(position.z * 10);

        encodedValue <<= 16;
        encodedValue |= x;

        encodedValue <<= 16;
        encodedValue |= z;

        return encodedValue;
    }
}
