using System.Collections;
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
        Cursor.lockState = CursorLockMode.None;

        backTextMeshPro = BackButton.GetComponent<TextMeshProUGUI>();

        BackButton.interactable = false;

        Color color = backTextMeshPro.color;
        color.a = 0f;
        backTextMeshPro.color = color;

        StartCoroutine(FadeInBackButton());
    }

    public void GoBackToMenu()
    {
        StartCoroutine(StartMainMenuScene());
    }

    IEnumerator FadeInBackButton()
    {
        yield return new WaitForSeconds(4f);

        BackButton.interactable = true;

        yield return Fades.Graphic(backTextMeshPro, 0f, 1f, 1f);
    }

    IEnumerator StartMainMenuScene()
    {
        BlankScreenAnimator.Play("FadeEnter");

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene("MainMenu");
    }
}
