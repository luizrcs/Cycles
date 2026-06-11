using UnityEngine;

public class ExitDoorGlow : MonoBehaviour
{
    private bool isGlowing = false;

    private static readonly int IsGlowing = Animator.StringToHash("isGlowing");

    void Update()
    {
        if (ExitDoorController.EndGame && !isGlowing)
        {
            isGlowing = true;
            GetComponent<Animator>().SetBool(IsGlowing, true);
        }
    }
}
