using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SkipController : MonoBehaviour
{
    public Animator BlankScreenAnimator;

    public void ForceStartPreGameScene()
    {
        GetComponent<Button>().interactable = false;
        StartCoroutine(StartPreGameScene());
    }

    IEnumerator StartPreGameScene()
    {
        BlankScreenAnimator.Play("FadeEnter");

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene("PreGame");
    }
}
