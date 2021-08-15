using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SkipController : MonoBehaviour
{
    public Animator BlankScreenAnimator;

    public AudioSource Narrator;

    public void ForceStartPreGameScene()
    {
        GetComponent<Button>().interactable = false;
        StartCoroutine(StartPreGameScene());
    }

    IEnumerator StartPreGameScene()
    {
        BlankScreenAnimator.Play("FadeEnter");

        for (float f = 0.05f; f > 0f; f -= 0.0025f)
        {
            Narrator.volume = f;
            yield return new WaitForSeconds(0.025f);
        }

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene("PreGame");
    }
}
