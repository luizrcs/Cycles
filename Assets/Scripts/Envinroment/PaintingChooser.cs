using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaintingChooser : MonoBehaviour
{
    public Texture2D[] Textures;

    private readonly System.Random random = new();

    void Start()
    {
        Texture2D texture = Textures[random.Next(Textures.Length)];
        GetComponent<MeshRenderer>().material.SetTexture("_BaseMap", texture);
    }
}
