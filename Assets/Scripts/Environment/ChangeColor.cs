using UnityEngine;

public class ChangeColor : MonoBehaviour
{
    public string Type;

    void Start()
    {
        // Sigmoid pushes hues toward the extremes so colors read as distinct.
        float newH = Random.value;
        newH = 1 / (1 + Mathf.Exp(-(newH - 0.5f) * 8));

        switch (Type)
        {
            case "Tulips":
                ChangeTulipsColor(newH);
                break;
            case "Bed":
                ChangeBedColor(newH);
                break;
        }
    }

    void ChangeBedColor(float newH)
    {
        ShiftHue(GetComponent<MeshRenderer>(), newH);
    }

    void ChangeTulipsColor(float newH)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child.name.Contains("tulips"))
                ShiftHue(child.GetComponent<MeshRenderer>(), newH);
        }
    }

    private static void ShiftHue(MeshRenderer renderer, float newH)
    {
        if (renderer == null) return;

        Material material = renderer.material;
        Color.RGBToHSV(material.color, out _, out float s, out float v);
        material.color = Color.HSVToRGB(newH, s, v);
    }
}
