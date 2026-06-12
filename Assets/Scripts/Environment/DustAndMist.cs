using System.Collections.Generic;
using UnityEngine;

// The atmosphere of a dying ship at sea. Everything is generated at runtime:
//
//  - deck-wide drifting mist, dense enough to SEE down a corridor
//  - low floor-hugging haze layers
//  - localized "fog pockets": random areas of the deck are much thicker
//  - dust motes around the camera, catching the lamps
//  - sea mist hovering over the water outside the hull
//
// All interior mist shares a global drift that oscillates with the ship's
// roll period, so the air visibly moves with the ship (synced to CameraSway).
public class DustAndMist : MonoBehaviour
{
    public float MistAlpha = 0.12f;
    public float DustAlpha = 0.45f;
    public float RollPeriod = 9f;          // keep equal to CameraSway.RollPeriod
    public float DriftStrength = 0.45f;

    private readonly List<ParticleSystem> drifting = new();
    private ParticleSystem.Particle[] buffer;

    void Start()
    {
        StartCoroutine(BuildWhenDeckReady());
    }

    // DeckGeneration computes its bounds in Start; execution order between the
    // two is undefined, and reading them too early placed every mist system at
    // float.MinValue coordinates (the "no mist anywhere" bug).
    private System.Collections.IEnumerator BuildWhenDeckReady()
    {
        DeckGeneration deck = null;
        for (int frame = 0; frame < 300; frame++)
        {
            deck = FindFirstObjectByType<DeckGeneration>();
            if (deck != null && deck.maxX > deck.minX && deck.maxZ > deck.minZ) break;
            deck = null;
            yield return null;
        }

        Texture2D sprite = MakeSoftCircle(64);
        Material mat = MakeParticleMaterial(sprite);

        if (deck != null)
        {
            float cx = (deck.minX + deck.maxX) / 2f;
            float cz = (deck.minZ + deck.maxZ) / 2f;
            float sx = deck.maxX - deck.minX + 10f;
            float sz = deck.maxZ - deck.minZ + 10f;

            // body mist filling the deck
            drifting.Add(MakeMist("CorridorMist", mat, new Vector3(cx, 6f, cz),
                new Vector3(sx, 3.5f, sz), rate: 30f, size: new Vector2(4f, 8f),
                alpha: MistAlpha, gray: 0.8f));

            // floor-hugging haze
            drifting.Add(MakeMist("FloorHaze", mat, new Vector3(cx, 5.1f, cz),
                new Vector3(sx, 0.7f, sz), rate: 22f, size: new Vector2(3f, 6f),
                alpha: MistAlpha * 1.3f, gray: 0.7f));

            // fog pockets: some areas of the ship are simply worse
            var rng = new System.Random(deck.GetInstanceID());
            for (int i = 0; i < 9; i++)
            {
                float px = Mathf.Lerp(deck.minX, deck.maxX, (float)rng.NextDouble());
                float pz = Mathf.Lerp(deck.minZ, deck.maxZ, (float)rng.NextDouble());
                float py = rng.NextDouble() < 0.5 ? 5.3f : 6.5f;  // some low, some at head height
                drifting.Add(MakeMist("FogPocket_" + i, mat, new Vector3(px, py, pz),
                    new Vector3(12f, 2.2f, 12f), rate: 26f, size: new Vector2(3f, 6f),
                    alpha: MistAlpha * 2.2f, gray: 0.75f));
            }

            // sea mist outside the hull, hovering over the water
            drifting.Add(MakeMist("SeaMist", mat, new Vector3(cx, 1.5f, cz),
                new Vector3(sx + 90f, 4f, sz + 90f), rate: 50f, size: new Vector2(10f, 22f),
                alpha: 0.10f, gray: 0.55f));
        }

        BuildDust(mat);
    }

    void Update()
    {
        // The air follows the ship's roll: a slow push across the hull,
        // reversing with the same period the camera sways with.
        float drift = Mathf.Sin(2f * Mathf.PI * Time.time / RollPeriod) * DriftStrength;
        Vector3 push = new Vector3(drift, 0f, drift * 0.2f) * Time.deltaTime;

        foreach (var ps in drifting)
        {
            if (ps == null) continue;
            int max = ps.main.maxParticles;
            if (buffer == null || buffer.Length < max) buffer = new ParticleSystem.Particle[max];
            int count = ps.GetParticles(buffer);
            for (int i = 0; i < count; i++) buffer[i].position += push;
            ps.SetParticles(buffer, count);
        }
    }

    private static Texture2D MakeSoftCircle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = Mathf.Clamp01(1f - d);
                a *= a * a; // very soft edge so quads never read as quads
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return tex;
    }

    private static Material MakeParticleMaterial(Texture2D sprite)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        mat.SetTexture("_BaseMap", sprite);
        mat.SetFloat("_Surface", 1f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return mat;
    }

    private static ParticleSystem MakeMist(string name, Material mat, Vector3 center,
        Vector3 volume, float rate, Vector2 size, float alpha, float gray)
    {
        var go = new GameObject(name);
        go.transform.position = center;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(18f, 32f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startColor = new Color(gray, gray * 1.04f, gray * 1.1f, alpha);
        main.maxParticles = Mathf.CeilToInt(rate * 32f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = volume;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.015f, 0.03f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;
        renderer.sortMode = ParticleSystemSortMode.Distance;

        ps.Play();
        ps.Simulate(28f, true, false);
        ps.Play();
        return ps;
    }

    private void BuildDust(Material mat)
    {
        var go = new GameObject("DustMotes");
        go.transform.SetParent(transform, false);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 12f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.005f, 0.016f);
        main.startColor = new Color(1f, 0.95f, 0.85f, DustAlpha);
        main.maxParticles = 200;

        var emission = ps.emission;
        emission.rateOverTime = 18f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(6f, 3f, 6f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.05f;
        noise.frequency = 0.35f;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.01f, 0.01f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.03f, -0.005f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.01f, 0.01f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;

        ps.Play();
        ps.Simulate(12f, true, false);
        ps.Play();
    }
}
