using System.Collections.Generic;
using UnityEngine;

// Records the player's position and facing ~100x/second, bit-packed into ulongs:
// time(24 bits, 1/100 s) | x(16 bits, 1/10 unit) | z(16 bits, 1/10 unit) | yaw(8 bits).
// AntiPlayerFollow replays this queue with a 120 s delay — the time-loop mechanic.
public class PlayerPath : MonoBehaviour
{
    public bool Active = false;

    public Queue<ulong> Queue = new();

    private const float TimeResolution = 100f;
    private float lastTime = 0f;

    void Update()
    {
        if (Active)
        {
            float currentTime = Time.time;
            if (currentTime - lastTime > 1f / TimeResolution)
            {
                lastTime = currentTime;
                Queue.Enqueue(CurrentPosition());
            }
        }
    }

    private ulong CurrentPosition()
    {
        ulong encodedValue = (ulong)(Time.time * TimeResolution);

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
