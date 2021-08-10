using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewGameController : MonoBehaviour
{
    public TextMeshProUGUI TextMeshPro;

    public Animator CameraAnimator;
    public Animator Storyboard_0;
    public Animator Storyboard_1;
    public Animator Storyboard_2;
    public Animator Storyboard_3;
    public Animator Storyboard_4;
    public Animator Storyboard_5;

    public void PlayStoryboard()
    {
        GetComponent<Button>().interactable = false;
        StartCoroutine(FadeOut());

        CameraAnimator.Play("StoryboardCamera");

        StartCoroutine(PlayStoryboardAnimation(Storyboard_0, 3f));
        StartCoroutine(PlayStoryboardAnimation(Storyboard_1, 7f));
        StartCoroutine(PlayStoryboardAnimation(Storyboard_2, 14f));
        StartCoroutine(PlayStoryboardAnimation(Storyboard_3, 21f));
        StartCoroutine(PlayStoryboardAnimation(Storyboard_4, 28f));
        StartCoroutine(PlayStoryboardAnimation(Storyboard_5, 35f));
    }

    IEnumerator FadeOut()
    {
        for (float f = 1f; f > 0; f -= 0.05f)
        {
            Color color = TextMeshPro.material.color;
            color.a = f;
            TextMeshPro.material.color = color;
            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator PlayStoryboardAnimation(Animator storyboardAnimator, float delay)
    {
        yield return new WaitForSeconds(delay);
        storyboardAnimator.Play("Storyboard");
    }
}
