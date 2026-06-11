using System.Collections.Generic;
using UnityEngine;

public class MakeCollectiblesGlow : MonoBehaviour
{
    private readonly List<Animator> glowing = new();
    private Transform currentTarget;

    private const float SightDistance = 30f;

    private static readonly int IsGlowing = Animator.StringToHash("isGlowing");

    void Update()
    {
        Transform seen = null;
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, SightDistance)
            && hit.transform.CompareTag("Collectible"))
            seen = hit.transform;

        if (seen == currentTarget) return;

        StopGlow();

        if (seen != null)
        {
            currentTarget = seen;
            foreach (Animator animator in seen.GetComponentsInChildren<Animator>())
            {
                animator.SetBool(IsGlowing, true);
                glowing.Add(animator);
            }
        }
    }

    private void StopGlow()
    {
        foreach (Animator animator in glowing)
            if (animator != null) animator.SetBool(IsGlowing, false);

        glowing.Clear();
        currentTarget = null;
    }
}
