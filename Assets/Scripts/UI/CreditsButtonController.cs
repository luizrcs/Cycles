using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CreditsButtonController : MonoBehaviour
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
        GetComponent<Button>().interactable = false;

        StartCoroutine(FadeOutButtonText(newGameTextMeshPro));
        StartCoroutine(FadeOutButtonText(textMeshPro));
        StartCoroutine(FadeOutTexts());

        CameraAnimator.Play("MoveCamera");

        StartCoroutine(StartCreditsScene());
    }

    IEnumerator FadeOutButtonText(TextMeshProUGUI text)
    {
        yield return new WaitForSeconds(2f);

        yield return Fades.Graphic(text, 1f, 0f, 1f);

        text.enabled = false;
    }

    IEnumerator FadeOutTexts()
    {
        yield return new WaitForSeconds(2f);

        foreach (Text text in Texts)
            StartCoroutine(Fades.Graphic(text, 1f, 0f, 1f));

        yield return new WaitForSeconds(1f);

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
