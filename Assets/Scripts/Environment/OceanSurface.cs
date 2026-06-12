using UnityEngine;

// Slow scrolling of the ocean's wave normal map + a gentle vertical heave
// synced to the ship's roll period, so the water outside is alive.
public class OceanSurface : MonoBehaviour
{
    public float ScrollSpeedX = 0.008f;
    public float ScrollSpeedZ = 0.0035f;
    public float HeaveAmplitude = 0.4f;
    public float RollPeriod = 9f;

    private Material material;
    private float baseY;

    void Start()
    {
        material = GetComponent<Renderer>().material;
        baseY = transform.position.y;
    }

    void Update()
    {
        float t = Time.time;
        material.SetTextureOffset("_BaseMap", new Vector2(t * ScrollSpeedX, t * ScrollSpeedZ));

        Vector3 p = transform.position;
        p.y = baseY + Mathf.Sin(2f * Mathf.PI * t / RollPeriod) * HeaveAmplitude;
        transform.position = p;
    }
}
