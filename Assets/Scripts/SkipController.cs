using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SkipController : MonoBehaviour
{
    public Animator BlankScreenAnimator;

    public void ForceStartGameScene()
    {
        GetComponent<Button>().interactable = false;
        StartCoroutine(StartGameScene());
    }

    IEnumerator StartGameScene()
    {
        BlankScreenAnimator.Play("FadeEnter");

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene("Game");
    }
}
