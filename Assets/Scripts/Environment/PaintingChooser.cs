using UnityEngine;

public class PaintingChooser : MonoBehaviour
{
    public Texture2D[] Textures;

    private Texture2D current;

    void Start()
    {
        current = Textures[Random.Range(0, Textures.Length)];
        GetComponent<MeshRenderer>().material.SetTexture("_BaseMap", current);
    }

    // Room event: when you come back, the painting is not the one you saw.
    public void Reroll()
    {
        if (Textures.Length < 2) return;
        Texture2D next;
        do next = Textures[Random.Range(0, Textures.Length)];
        while (next == current);
        current = next;
        GetComponent<MeshRenderer>().material.SetTexture("_BaseMap", next);
    }
}
