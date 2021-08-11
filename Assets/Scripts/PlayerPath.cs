using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPath : MonoBehaviour
{
    public Queue<ulong> Queue = new Queue<ulong>();

    private float timeResolution = 200f;
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
        ulong encodedValue = (ulong)(Time.time * timeResolution);

        Vector3 position = transform.position;
        ulong x = (ulong)(position.x * 10);
        ulong z = (ulong)(position.z * 10);

        encodedValue <<= 16;
        encodedValue |= x;

        encodedValue <<= 16;
        encodedValue |= z;

        Quaternion rotation = transform.rotation;
        ulong y = (ulong)((rotation.y + 1f) * 128f);
        encodedValue <<= 8;
        encodedValue |= y;

        return encodedValue;
    }
}
