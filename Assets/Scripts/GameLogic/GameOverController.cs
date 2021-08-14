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
                ReasonTextMeshPro.text = "Você foi encontrado\ne não conseguiu escapar...";
                break;
            case 2:
                ReasonTextMeshPro.text = "Tempo demais se passou e um paradoxo\nfez com que você deixasse de existir...";
                break;
        }
    }
}
