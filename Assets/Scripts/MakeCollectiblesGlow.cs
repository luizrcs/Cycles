using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeCollectiblesGlow : MonoBehaviour
{
    private List<Animator> glowing = new List<Animator>();

    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit))
        {
            Transform transform = hit.transform;
            if (transform.CompareTag("Collectible"))
            {
                foreach (Animator animator in transform.GetComponentsInChildren<Animator>())
                {
                    animator.SetBool("isGlowing", true);
                    glowing.Add(animator);
                }
            }
            else if (glowing.Count != 0)
            {
                foreach (Animator animator in glowing)
                    animator.SetBool("isGlowing", false);

                glowing.Clear();
            }
        }
    }
}
