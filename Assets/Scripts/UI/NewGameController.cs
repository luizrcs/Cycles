using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NewGameController : MonoBehaviour
{
    public Button CreditsButton;
    public Button SkipButton;

    public Animator CameraAnimator;
    public Animator LanternAnimator;

    public Animator Storyboard_0;
    public Animator Storyboard_1;
    public Animator Storyboard_2;
    public Animator Storyboard_3;
    public Animator Storyboard_4;
    public Animator Storyboard_5;

    public Animator BlankScreenAnimator;

    public NarrationController NarrationController;

    public Text[] Texts;

    private TextMeshProUGUI textMeshPro;
    private TextMeshProUGUI creditsTextMeshPro;
    private TextMeshProUGUI skipTextMeshPro;

    void Start()
    {
        textMeshPro = GetComponent<TextMeshProUGUI>();
        creditsTextMeshPro = CreditsButton.GetComponent<TextMeshProUGUI>();
        skipTextMeshPro = SkipButton.GetComponent<TextMeshProUGUI>();

        Color color = skipTextMeshPro.color;
        color.a = 0f;
        skipTextMeshPro.color = color;
    }

    public void PlayStoryboard()
    {
        CreditsButton.interactable = false;
        GetComponent<Button>().interactable = false;

        StartCoroutine(FadeOutButtonText(creditsTextMeshPro));
        StartCoroutine(FadeOutButtonText(textMeshPro));
        StartCoroutine(FadeOutTexts());
        StartCoroutine(FadeInSkipButton());

        CameraAnimator.Play("MoveCamera");
        LanternAnimator.Play("MoveLantern");

        NarrationController.StartNarration();

        StartCoroutine(PlayStoryboardCameraAnimation());

        StartCoroutine(PlayStoryboardAnimation(Storyboard_0, 3f));
        StartCoroutine(PlayStoryboardAnimation(Storyboard_1, 7f));
        StartCoroutine(PlayStoryboardAnimation(Storyboard_2, 14f));
        StartCoroutine(PlayStoryboardAnimation(Storyboard_3, 21f));
        StartCoroutine(PlayStoryboardAnimation(Storyboard_4, 28f));
        StartCoroutine(PlayStoryboardAnimation(Storyboard_5, 35f));

        StartCoroutine(StartPreGameScene());
    }

    IEnumerator FadeInSkipButton()
    {
        yield return new WaitForSeconds(4f);

        SkipButton.interactable = true;

        yield return Fades.Graphic(skipTextMeshPro, 0f, 1f, 1f);
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

    IEnumerator PlayStoryboardCameraAnimation()
    {
        yield return new WaitForSeconds(4f);
        CameraAnimator.Play("StoryboardCamera");
        LanternAnimator.Play("StoryboardLantern");
    }

    IEnumerator PlayStoryboardAnimation(Animator storyboardAnimator, float delay)
    {
        yield return new WaitForSeconds(delay);

        storyboardAnimator.Play("Storyboard");
    }

    IEnumerator StartPreGameScene()
    {
        yield return new WaitForSeconds(40f);

        BlankScreenAnimator.Play("FadeEnter");

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene("PreGame");
    }
}
