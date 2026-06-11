using UnityEngine;

public class PaintingChooser : MonoBehaviour
{
    public Texture2D[] Textures;

    void Start()
    {
        Texture2D texture = Textures[Random.Range(0, Textures.Length)];
        GetComponent<MeshRenderer>().material.SetTexture("_BaseMap", texture);
    }
}
