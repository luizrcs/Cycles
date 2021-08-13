using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLogic : MonoBehaviour
{
    public Animator BlankScreenAnimator;

    public AudioSource Speech;
    public PunchSounds PunchSounds;

    public AudioSource BackgroundSoundsController;
    public RandomSoundsController RandomSoundsController;

    public FirstPersonController FirstPersonController;
    public PlayerMovement PlayerMovement;

    public AntiPlayerFollow AntiPlayerFollow;
    public DetectPlayer DetectPlayer;

    private int battleState = 0;

    public void PlayPreBattleEffects()
    {
        StartCoroutine(_PlayPreBattleEffects());
    }

    IEnumerator _PlayPreBattleEffects()
    {
        if (battleState <= 1)
        {
            yield return new WaitForSeconds(2f);

            // Voice

            yield return new WaitForSeconds(5f);

            battleState++;
        }
    }

    public void PlayBattleEffects()
    {
        if (battleState <= 1)
        {
            BlankScreenAnimator.Play("FadeEnter");

            StartCoroutine(_PlayBattleEffects());
        }
    }

    IEnumerator _PlayBattleEffects()
    {
        yield return new WaitForSeconds(1f);

        PunchSounds.PlayPunchLong();
        yield return new WaitForSeconds(1f);

        PunchSounds.PlayPunchShort();
        yield return new WaitForSeconds(1f);

        PunchSounds.PlayPunchShort();

        RandomSoundsController.Active = false;

        for (float f = 0.05f; f > 0f; f -= 0.0025f)
        {
            BackgroundSoundsController.volume = f;
            yield return new WaitForSeconds(0.025f);
        }

        yield return new WaitForSeconds(5f);

        battleState++;
    }

    public void PostBattleSetup()
    {
        AntiPlayerFollow.Respawn();

        StartCoroutine(PlayPostBattleEffects());
    }

    IEnumerator PlayPostBattleEffects()
    {
        for (float f = 0f; f < 0.05f; f += 0.0025f)
        {
            BackgroundSoundsController.volume = f;
            yield return new WaitForSeconds(0.025f);
        }

        BlankScreenAnimator.Play("FadeExit");

        RandomSoundsController.Active = true;

        battleState = 3;
    }

    public void AfterPostBattleSetup()
    {
        DetectPlayer.State = 3;

        FirstPersonController.ResetRotation();
        FirstPersonController.LockCamera = false;
        PlayerMovement.LockMovement = false;

        battleState = 4;
    }

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

    private void Update()
    {
        if (battleState == 2) PostBattleSetup();
        if (battleState == 3) AfterPostBattleSetup();
    }
}
