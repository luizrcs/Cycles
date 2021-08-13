using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLogic : MonoBehaviour
{
    public Animator BlankScreenAnimator;

    public void EndGame()
    {
        StartCoroutine(StartEndGameScene());
    }

    IEnumerator StartEndGameScene()
    {
        BlankScreenAnimator.Play("FadeEnter");

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene("End");
    }
}
