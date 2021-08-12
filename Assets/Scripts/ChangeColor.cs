using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeColor : MonoBehaviour
{
    public string Type;

    private readonly System.Random random = new System.Random();

    void Start()
    {
        float newH = (float)random.NextDouble();
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
        Material material = transform.GetComponent<MeshRenderer>().material;
        Color color = material.color;

        float h, s, v;
        Color.RGBToHSV(color, out h, out s, out v);

        material.color = Color.HSVToRGB(newH, s, v);
    }

    void ChangeTulipsColor(float newH)
    {
        foreach (Transform transform in GetComponentsInChildren<Transform>())
        {
            if (transform.name.Contains("tulips"))
            {
                Material material = transform.GetComponent<MeshRenderer>().material;
                Color color = material.color;

                float h, s, v;
                Color.RGBToHSV(color, out h, out s, out v);

                material.color = Color.HSVToRGB(newH, s, v);
            }
        }
    }
}
