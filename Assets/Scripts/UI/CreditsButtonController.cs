using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CreditsButtonController: MonoBehaviour
{
    public Button NewGameButton;

    public Animator CameraAnimator;
    public Animator LanternAnimator;

    public Animator BlankScreenAnimator;

    public Text[] Texts;

    private TextMeshProUGUI textMeshPro;
    private TextMeshProUGUI newGameTextMeshPro;

    void Start()
    {
        textMeshPro = GetComponent<TextMeshProUGUI>();
        newGameTextMeshPro = NewGameButton.GetComponent<TextMeshProUGUI>();
    }

    public void PlayCredits()
    {
        NewGameButton.interactable = false;
        StartCoroutine(FadeOutNewGameButton());

        GetComponent<Button>().interactable = false;
        StartCoroutine(FadeOutButton());

        StartCoroutine(FadeOutTexts());

        CameraAnimator.Play("MoveCamera");

        StartCoroutine(StartCreditsScene());
    }

    IEnumerator FadeOutButton()
    {
        yield return new WaitForSeconds(2f);

        for (float f = 1f; f > 0f; f -= 0.05f)
        {
            Color color = textMeshPro.color;
            color.a = f;
            textMeshPro.color = color;

            yield return new WaitForSeconds(0.05f);
        }

        textMeshPro.enabled = false;
    }

    IEnumerator FadeOutNewGameButton()
    {
        yield return new WaitForSeconds(2f);

        for (float f = 1f; f > 0f; f -= 0.05f)
        {
            Color color = newGameTextMeshPro.color;
            color.a = f;
            newGameTextMeshPro.color = color;

            yield return new WaitForSeconds(0.05f);
        }

        newGameTextMeshPro.enabled = false;
    }

    IEnumerator FadeOutTexts()
    {
        yield return new WaitForSeconds(2f);

        for (float f = 1f; f > 0; f -= 0.05f)
        {
            foreach (Text text in Texts)
            {
                Color color = text.color;
                color.a = f;
                text.color = color;
            }

            yield return new WaitForSeconds(0.05f);
        }

        foreach (Text text in Texts)
            text.enabled = false;
    }

    IEnumerator StartCreditsScene()
    {
        yield return new WaitForSeconds(2.75f);

        BlankScreenAnimator.Play("FadeEnter");

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene("Credits");
    }
}
