using System.Collections;
using TMPro;
using UnityEngine;

public class GameOverController : MonoBehaviour
{
    public TextMeshProUGUI ReasonTextMeshPro;

    private const float GrowAmount = 12f;
    private const float GrowDuration = 6f;

    void Start()
    {
        switch (GameOver.Reason)
        {
            case 0:
                ReasonTextMeshPro.text = "Você foi encontrado\ne não conseguiu escapar...";
                break;
            case 1:
                ReasonTextMeshPro.text = "Tempo demais se passou, uma nova\nversão sua chegou ao navio e um paradoxo\nfez com que você deixasse de existir...";
                break;
        }

        StartCoroutine(GrowText());
    }

    IEnumerator GrowText()
    {
        float startSize = ReasonTextMeshPro.fontSize;

        for (float t = 0f; t < GrowDuration; t += Time.deltaTime)
        {
            ReasonTextMeshPro.fontSize = startSize + GrowAmount * (t / GrowDuration);
            yield return null;
        }

        ReasonTextMeshPro.fontSize = startSize + GrowAmount;
    }
}
