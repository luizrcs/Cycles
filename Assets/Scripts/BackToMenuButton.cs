using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackToMenuButton : MonoBehaviour
{
    public Animator BlankScreenAnimator;

    public Button BackButton;
    private TextMeshProUGUI backTextMeshPro;

    void Start()
    {
        backTextMeshPro = BackButton.GetComponent<TextMeshProUGUI>();

        BackButton.interactable = false;

        Color color = backTextMeshPro.color;
        color.a = 0f;
        backTextMeshPro.color = color;

        StartCoroutine(FadeInBackButton());
    }

    IEnumerator FadeInBackButton()
    {
        yield return new WaitForSeconds(4f);

        BackButton.interactable = true;

        for (float f = 0f; f < 1f; f += 0.05f)
        {
            Color color = backTextMeshPro.color;
            color.a = f;
            backTextMeshPro.color = color;

            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator StartMainMenuScene()
    {
        BlankScreenAnimator.Play("FadeEnter");

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene("MainMenu");
    }
}
