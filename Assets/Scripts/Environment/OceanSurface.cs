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

        SetupMoonGlint();
    }

    // A cold light that exists only for the water: the ocean gets a specular
    // moon band without a directional that would leak through the unshadowed
    // hull. Spot culling masks are reliably honored for additional lights.
    private void SetupMoonGlint()
    {
        gameObject.layer = 4; // built-in Water layer

        var go = new GameObject("MoonGlint");
        go.transform.SetParent(transform.parent, true);
        go.transform.position = transform.position + new Vector3(60f, 35f, 90f);
        go.transform.rotation = Quaternion.LookRotation(
            transform.position + new Vector3(-20f, 0f, -30f) - go.transform.position);

        var moon = go.AddComponent<Light>();
        moon.type = LightType.Spot;
        moon.color = new Color(0.62f, 0.72f, 0.95f);
        moon.intensity = 3.2f;
        moon.range = 400f;
        moon.spotAngle = 95f;
        moon.innerSpotAngle = 40f;
        moon.shadows = LightShadows.None;
        moon.cullingMask = 1 << 4; // the water alone
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
