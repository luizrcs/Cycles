using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameOverController : MonoBehaviour
{
    public TextMeshProUGUI ReasonTextMeshPro;

    void Start()
    {
        switch (GameOver.Reason)
        {
            case 0:
                ReasonTextMeshPro.text = "Voc� foi encontrado\ne n�o conseguiu escapar...";
                break;
            case 1:
                ReasonTextMeshPro.text = "Tempo demais se passou e um paradoxo\nfez com que voc� deixasse de existir...";
                break;
        }

        StartCoroutine(GrowText());
    }

    IEnumerator GrowText()
    {
        for (float f = 0; f < 12; f += 0.05f)
        {
            ReasonTextMeshPro.fontSize += 0.05f;
            yield return new WaitForSeconds(0.025f);
        }
    }
}
