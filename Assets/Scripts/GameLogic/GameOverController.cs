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
                ReasonTextMeshPro.fontSize = 120;
                ReasonTextMeshPro.text = "FIM";
                break;
            case 1:
                ReasonTextMeshPro.text = "Voc� foi encontrado\ne n�o conseguiu escapar...";
                break;
            case 2:
                ReasonTextMeshPro.text = "Tempo demais se passou e um paradoxo\nfez com que voc� deixasse de existir...";
                break;
        }
    }
}
