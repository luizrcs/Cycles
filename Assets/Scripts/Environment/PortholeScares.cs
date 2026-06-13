using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The sea is right outside the glass (random tension category). When the
// player is near one of the border exit doors, occasionally a wave breaks
// against its porthole — spray across the glass and a deep slap through the
// hull. Rarely, instead, something dark glides past outside. Neither ever
// harms; both make the night outside REAL. Added at runtime by DeckGeneration.
public class PortholeScares : MonoBehaviour
{
    private Transform player;
    private AudioSource slapSource;
    private AudioClip slap;
    private AudioClip knock;
    private ParticleSystem spray;
    private GameObject silhouette;

    private readonly List<(Vector3 position, Vector3 outward)> portholes = new();

    private float nextScareAt;

    private const float NearDistance = 15f;
    private const float MinInterval = 50f;
    private const float MaxInterval = 110f;
    private const float RetryInterval = 12f;
    private const float SilhouetteChance = 0.2f;

    void Start()
    {
        var movement = FindAnyObjectByType<PlayerMovement>();
        if (movement == null) { enabled = false; return; }
        player = movement.transform;

        var deck = GetComponent<DeckGeneration>();
        Vector3 center = new((deck.minX + deck.maxX) / 2f, 0f, (deck.minZ + deck.maxZ) / 2f);
        foreach (ExitDoorTrigger trigger in GetComponentsInChildren<ExitDoorTrigger>())
        {
            Vector3 position = trigger.transform.position;
            Vector3 d = position - center;
            Vector3 outward = Mathf.Abs(d.x) > Mathf.Abs(d.z)
                ? new Vector3(Mathf.Sign(d.x), 0f, 0f)
                : new Vector3(0f, 0f, Mathf.Sign(d.z));
            portholes.Add((position, outward));
        }
        if (portholes.Count == 0) { enabled = false; return; }

        slap = ProceduralAudio.MakeWaveSlap();
        knock = ProceduralAudio.MakeMetalKnock();
        var go = new GameObject("PortholeSlap");
        go.transform.SetParent(transform, false);
        slapSource = go.AddComponent<AudioSource>();
        slapSource.playOnAwake = false;
        slapSource.spatialBlend = 1f;
        slapSource.dopplerLevel = 0f;
        slapSource.rolloffMode = AudioRolloffMode.Linear;
        slapSource.minDistance = 2f;
        slapSource.maxDistance = 30f;

        BuildSpray();
        BuildSilhouette();

        nextScareAt = Time.time + Random.Range(MinInterval, MaxInterval);
    }

    void Update()
    {
        if (Time.time < nextScareAt) return;

        // Needs an exit door close enough that the glass is in earshot/eyeshot.
        Vector3 best = default;
        Vector3 bestOutward = default;
        float bestDistance = NearDistance;
        foreach (var (position, outward) in portholes)
        {
            float d = Vector3.Distance(player.position, position);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = position;
                bestOutward = outward;
            }
        }
        if (bestDistance >= NearDistance)
        {
            nextScareAt = Time.time + RetryInterval; // try again soon, player may wander over
            return;
        }

        nextScareAt = Time.time + Random.Range(MinInterval, MaxInterval);
        if (Random.value < SilhouetteChance) StartCoroutine(SilhouettePass(best, bestOutward));
        else WaveSlap(best, bestOutward);
    }

    private void WaveSlap(Vector3 door, Vector3 outward)
    {
        Vector3 glass = door + outward * 0.9f + Vector3.up * 1.7f;
        slapSource.transform.position = glass + outward * 0.8f;
        slapSource.pitch = Random.Range(0.85f, 1.05f);
        slapSource.PlayOneShot(slap, Random.Range(0.65f, 0.9f));

        spray.transform.position = glass + outward * 0.5f;
        spray.transform.rotation = Quaternion.LookRotation(-outward); // burst across the glass
        spray.Emit(45);
    }

    // Something passes between the porthole and the sea. Slow. Person-sized.
    private IEnumerator SilhouettePass(Vector3 door, Vector3 outward)
    {
        slapSource.transform.position = door + outward * 1.5f + Vector3.up * 1.2f;
        slapSource.pitch = 0.5f;
        slapSource.PlayOneShot(knock, 0.3f); // one low structural thud

        Vector3 lateral = Vector3.Cross(Vector3.up, outward);
        Vector3 center = door + outward * 1.3f + Vector3.up * 1.55f;
        silhouette.transform.rotation = Quaternion.LookRotation(outward);

        const float duration = 1.6f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            silhouette.transform.position = center + lateral * Mathf.Lerp(-1.4f, 1.4f, k);
            silhouette.SetActive(true);
            yield return null;
        }
        silhouette.SetActive(false);
    }

    private void BuildSpray()
    {
        var go = new GameObject("PortholeSpray");
        go.transform.SetParent(transform, false);
        spray = go.AddComponent<ParticleSystem>();
        var main = spray.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.3f);
        main.startColor = new Color(0.75f, 0.82f, 0.9f, 0.55f);
        main.maxParticles = 60;
        main.gravityModifier = 1.2f;
        main.playOnAwake = false;

        var emission = spray.emission;
        emission.rateOverTime = 0f; // burst-only via Emit()

        var shape = spray.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.15f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        mat.SetFloat("_Surface", 1f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        renderer.material = mat;
    }

    private void BuildSilhouette()
    {
        silhouette = GameObject.CreatePrimitive(PrimitiveType.Quad);
        silhouette.name = "PortholeSilhouette";
        Destroy(silhouette.GetComponent<Collider>());
        silhouette.transform.localScale = new Vector3(0.7f, 1.9f, 1f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", new Color(0.01f, 0.012f, 0.018f, 1f));
        silhouette.GetComponent<MeshRenderer>().material = mat;
        silhouette.SetActive(false);
    }
}
