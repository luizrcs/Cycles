using System.Collections.Generic;
using UnityEngine;

// The double leaves wet footprints wherever it walks — dark, glossy,
// alternating feet, fading over half a minute. The player can TRACK their
// pursuer... or realize it has already walked exactly where they are
// standing. Added at runtime by AntiPlayerFollow; quads have no colliders so
// they never block sight lines or movement.
public class FootprintTrail : MonoBehaviour
{
    private AntiPlayerFollow follow;
    private Texture2D printTexture;
    private readonly List<(GameObject go, Material mat, float born)> prints = new();
    private Vector3 lastPrintPos;
    private bool leftFoot;

    private const float Stride = 1.7f;
    private const float Life = 30f;
    private const int MaxPrints = 44;
    private const float BaseAlpha = 0.75f;

    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int Smoothness = Shader.PropertyToID("_Smoothness");

    void Start()
    {
        follow = GetComponent<AntiPlayerFollow>();
        printTexture = MakeFootprintTexture();
        lastPrintPos = transform.position;
    }

    void Update()
    {
        AgePrints();

        if (!follow.Engaged) return;

        Vector3 position = transform.position;
        Vector3 moved = position - lastPrintPos;
        moved.y = 0f;
        if (moved.sqrMagnitude < Stride * Stride) return;

        lastPrintPos = position;
        if (moved.sqrMagnitude < 0.01f) return;
        SpawnPrint(position, Quaternion.LookRotation(moved.normalized));
    }

    // The floors carry no colliders (the player's Y is forced, never grounded
    // by physics), so a downward raycast finds nothing — the visible floor
    // surface lives at world y≈0 (the model roots stand on it).
    private const float FloorY = 0f;

    private void SpawnPrint(Vector3 at, Quaternion heading)
    {
        float floorY = Physics.Raycast(at + Vector3.up * 0.3f, Vector3.down, out RaycastHit hit, 6f,
            ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : FloorY;

        leftFoot = !leftFoot;
        Vector3 side = heading * Vector3.right * (leftFoot ? -0.16f : 0.16f);

        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "WetFootprint";
        Destroy(go.GetComponent<Collider>());
        go.transform.position = new Vector3(at.x + side.x, floorY + 0.02f, at.z + side.z);
        go.transform.rotation = Quaternion.Euler(90f, heading.eulerAngles.y, 0f);
        go.transform.localScale = new Vector3(0.26f, 0.6f, 1f);

        // Wet wood: nearly black, high sheen, fading out as it dries.
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetFloat("_Surface", 1f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetTexture(BaseMap, printTexture);
        mat.SetColor(BaseColor, new Color(0.03f, 0.05f, 0.07f, BaseAlpha));
        mat.SetFloat(Smoothness, 0.92f);

        var renderer = go.GetComponent<MeshRenderer>();
        renderer.material = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        prints.Add((go, mat, Time.time));
        if (prints.Count > MaxPrints)
        {
            Destroy(prints[0].go);
            prints.RemoveAt(0);
        }
    }

    private void AgePrints()
    {
        for (int i = prints.Count - 1; i >= 0; i--)
        {
            float age = Time.time - prints[i].born;
            if (age > Life)
            {
                Destroy(prints[i].go);
                prints.RemoveAt(i);
                continue;
            }
            Color color = prints[i].mat.GetColor(BaseColor);
            color.a = BaseAlpha * (1f - age / Life);
            prints[i].mat.SetColor(BaseColor, color);
        }
    }

    // A simple sole: heel blob + forefoot ellipse, soft alpha edges.
    private static Texture2D MakeFootprintTexture()
    {
        const int w = 32, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float sole = Ellipse(x, y, 16f, 42f, 9f, 14f);
                float heel = Ellipse(x, y, 16f, 14f, 7f, 8f);
                float a = Mathf.Clamp01(Mathf.Max(sole, heel));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        }
        tex.Apply();
        return tex;
    }

    private static float Ellipse(int x, int y, float cx, float cy, float rx, float ry)
    {
        float dx = (x - cx) / rx;
        float dy = (y - cy) / ry;
        return 1.2f - (dx * dx + dy * dy);
    }
}
