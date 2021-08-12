using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeColor : MonoBehaviour
{
    private readonly System.Random random = new System.Random();

    void Start()
    {
        float newH = (float)random.NextDouble();

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
