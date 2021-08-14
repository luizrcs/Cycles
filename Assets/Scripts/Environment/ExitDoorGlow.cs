using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitDoorGlow : MonoBehaviour
{
    private bool isGlowing = false;

    void Update()
    {
        if (ExitDoorController.EndGame && !isGlowing)
        {
            isGlowing = true;
            GetComponent<Animator>().SetBool("isGlowing", true);
        }
    }
}
