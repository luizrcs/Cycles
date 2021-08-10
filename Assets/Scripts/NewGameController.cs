using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NewGameController : MonoBehaviour
{
    public Animator CameraAnimator;
    public Animator LanternAnimator;

    public Animator Storyboard_0;
    public Animator Storyboard_1;
    public Animator Storyboard_2;
    public Animator Storyboard_3;
    public Animator Storyboard_4;
    public Animator Storyboard_5;

    public Animator BlankScreenAnimator;

    public Text[] Texts;

    private TextMeshProUGUI textMeshPro;

    void Start()
    {
        textMeshPro = GetComponent<TextMeshProUGUI>();
    }

    public void PlayStoryboard()
    {
        GetComponent<Button>().interactable = false;
        StartCoroutine(FadeOutButton());

        StartCoroutine(FadeOutTexts());

        CameraAnimator.Play("StoryboardCamera");
        LanternAnimator.Play("StoryboardLantern");

        StartCoroutine(PlayStoryboardAnimation(Storyboard_0, 3f));
        StartCoroutine(PlayStoryboardAnimation(Storyboard_1, 7f));
        StartCoroutine(PlayStoryboardAnimation(Storyboard_2, 14f));
        StartCoroutine(PlayStoryboardAnimation(Storyboard_3, 21f));
        StartCoroutine(PlayStoryboardAnimation(Storyboard_4, 28f));
        StartCoroutine(PlayStoryboardAnimation(Storyboard_5, 35f));

        StartCoroutine(StartGameScene());
    }

    IEnumerator FadeOutButton()
    {
        yield return new WaitForSeconds(2f);

        for (float f = 1f; f > 0; f -= 0.05f)
        {
            Color color = textMeshPro.color;
            color.a = f;
            textMeshPro.color = color;

            yield return new WaitForSeconds(0.05f);
        }

        textMeshPro.enabled = false;
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

    IEnumerator PlayStoryboardAnimation(Animator storyboardAnimator, float delay)
    {
        yield return new WaitForSeconds(delay);

        storyboardAnimator.Play("Storyboard");
    }

    IEnumerator StartGameScene()
    {
        yield return new WaitForSeconds(40f);

        BlankScreenAnimator.Play("FadeEnter");

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene("Game");
    }
}
