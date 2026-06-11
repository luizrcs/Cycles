using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PreGameController : MonoBehaviour
{
    public Image[] Rectangles;
    public Image[] Items;
    public TextMeshProUGUI Message;

    public Animator BlankScreenAnimator;

    void Start()
    {
        Color color = Message.color;
        color.a = 0f;
        Message.color = color;

        foreach (Image rectangle in Rectangles)
        {
            color = rectangle.color;
            color.a = 0f;
            rectangle.color = color;
        }

        foreach (Image item in Items)
        {
            color = item.color;
            color.a = 1f;
            item.color = color;
        }

        StartCoroutine(AnimateScreen());
    }

    IEnumerator AnimateScreen()
    {
        yield return new WaitForSeconds(3f);

        yield return Fades.Graphic(Message, 0f, 1f, 1f);

        yield return new WaitForSeconds(1f);
        StartCoroutine(DisplayRectangles());

        yield return new WaitForSeconds(0.5f);
        StartCoroutine(DisplayItems());

        yield return new WaitForSeconds(6f);
        StartCoroutine(StartGameScene());
    }

    IEnumerator DisplayRectangles()
    {
        foreach (Image rectangle in Rectangles)
            yield return Fades.Graphic(rectangle, 0f, 1f, 0.5f);
    }

    IEnumerator DisplayItems()
    {
        foreach (Image item in Items)
            yield return Fades.Graphic(item, 1f, 0f, 1f);
    }

    IEnumerator StartGameScene()
    {
        BlankScreenAnimator.Play("FadeEnter");

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene("Game");
    }
}
