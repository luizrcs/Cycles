using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPath : MonoBehaviour
{
    public bool Active = true;

    public Queue<ulong> Queue = new();

    private float timeResolution = 100f;
    private float lastTime = 0f;

    void Update()
    {
        if (Active)
        {
            float currentTime = Time.time;
            if (currentTime - lastTime > 1f / timeResolution)
            {
                lastTime = currentTime;
                Queue.Enqueue(CurrentPosition());
            }
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

        float rotationY = transform.rotation.eulerAngles.y;
        ulong y = (ulong)(rotationY / 360f * 256f);
        encodedValue <<= 8;
        encodedValue |= y;

        return encodedValue;
    }
}
