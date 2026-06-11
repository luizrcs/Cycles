using System.Collections;
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

        StartCoroutine(Fades.Volume(Narrator, Narrator.volume, 0f, 0.5f));

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene("PreGame");
    }
}
