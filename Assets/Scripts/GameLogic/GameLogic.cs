using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLogic : MonoBehaviour
{
    public Animator BlankScreenAnimator;

    public AudioSource Speech;
    public AudioClip WhatSound;
    public AudioClip WhoAreYou;
    public AudioClip MyHead;
    public AudioClip GetOut;

    public PunchSounds PunchSounds;

    public AudioSource BackgroundSoundsController;
    public RandomSoundsController RandomSoundsController;

    public FirstPersonController FirstPersonController;
    public PlayerMovement PlayerMovement;

    public AntiPlayerFollow AntiPlayerFollow;
    public Animator AntiPlayerAnimator;
    public DetectPlayer DetectPlayer;

    public TextMeshProUGUI MessageTextMeshPro;
    public TextMeshProUGUI SubMessageTextMeshPro;

    public GameObject Deck;

    // 0 = no encounter yet, 1 = first encounter underway, 3..5 = post-battle
    // recovery, 5 = roaming until the final encounter.
    private int battleState = 0;

    private const float BackgroundVolume = 0.05f;

    private static readonly int IsRunning = Animator.StringToHash("isRunning");

    private void Start()
    {
        // Static cross-scene state must be reset, or a second playthrough
        // starts with the exit door already active.
        ExitDoorController.EndGame = false;

        Color color = MessageTextMeshPro.color;
        color.a = 0f;
        MessageTextMeshPro.color = color;
        SubMessageTextMeshPro.color = color;
    }

    public void PlayPreBattleEffects()
    {
        if (battleState <= 1)
            StartCoroutine(PlayPreFirstBattleEffects());
    }

    IEnumerator PlayPreFirstBattleEffects()
    {
        yield return new WaitForSeconds(1f);

        Speech.clip = WhoAreYou;
        Speech.Play();

        yield return new WaitForSeconds(5f);

        battleState++;
    }

    public void PlayBattleEffects()
    {
        if (battleState <= 1)
            StartCoroutine(PlayFirstBattleEffects());
        else
            StartCoroutine(PlaySecondBattleEffects());
    }

    IEnumerator PlayFirstBattleEffects()
    {
        BlankScreenAnimator.Play("FadeEnter");
        yield return new WaitForSeconds(1f);

        PunchSounds.PlayPunchLong();
        yield return new WaitForSeconds(1f);

        PunchSounds.PlayPunchShort();
        yield return new WaitForSeconds(1f);

        PunchSounds.PlayPunchShort();

        RandomSoundsController.Active = false;
        yield return Fades.Volume(BackgroundSoundsController, BackgroundVolume, 0f, 0.5f);

        yield return new WaitForSeconds(4f);

        StartCoroutine(FadeInText(MessageTextMeshPro));
        yield return new WaitForSeconds(4f);

        StartCoroutine(FadeInText(SubMessageTextMeshPro));
        yield return new WaitForSeconds(5f);

        StartCoroutine(FadeOutText(MessageTextMeshPro));
        StartCoroutine(FadeOutText(SubMessageTextMeshPro));
        yield return new WaitForSeconds(3f);

        PostFirstBattleSetup();
    }

    IEnumerator PlaySecondBattleEffects()
    {
        BlankScreenAnimator.Play("FadeEnter");
        yield return new WaitForSeconds(1f);

        PunchSounds.PlayPunchLong();
        yield return new WaitForSeconds(1f);

        PunchSounds.PlayPunchShort();
        yield return new WaitForSeconds(1f);

        PunchSounds.PlayPunchShort();

        RandomSoundsController.Active = false;
        yield return Fades.Volume(BackgroundSoundsController, BackgroundVolume, 0f, 0.5f);

        yield return new WaitForSeconds(2f);

        GameOver.Reason = 0;
        SceneManager.LoadScene("GameOver");
    }

    public void TimeUp()
    {
        AntiPlayerFollow.State = 0;
        AntiPlayerAnimator.SetBool(IsRunning, false);

        DetectPlayer.State = 3;

        StartCoroutine(PlayTimeUpEffects());
    }

    IEnumerator PlayTimeUpEffects()
    {
        BlankScreenAnimator.Play("FadeEnter");
        yield return new WaitForSeconds(1f);

        RandomSoundsController.PlayTimeParadox();

        yield return Fades.Volume(BackgroundSoundsController, BackgroundVolume, 0f, 0.5f);

        yield return new WaitForSeconds(2f);

        GameOver.Reason = 1;
        SceneManager.LoadScene("GameOver");
    }

    private void PostFirstBattleSetup()
    {
        battleState = 3;

        AntiPlayerFollow.Respawn();

        StartCoroutine(PlayPostFirstBattleEffects());
    }

    IEnumerator PlayPostFirstBattleEffects()
    {
        yield return Fades.Volume(BackgroundSoundsController, 0f, BackgroundVolume, 0.5f);

        BlankScreenAnimator.Play("FadeExit");

        RandomSoundsController.Active = true;

        yield return new WaitForSeconds(1f);

        Speech.clip = MyHead;
        Speech.Play();

        AfterPostBattleSetup();
    }

    private void AfterPostBattleSetup()
    {
        FirstPersonController.ResetRotation();
        FirstPersonController.LockCamera = false;
        PlayerMovement.LockMovement = false;

        AntiPlayerFollow.State = 2;
        AntiPlayerFollow.CurrentTargetPosition = AntiPlayerFollow.transform.position;
        AntiPlayerAnimator.SetBool(IsRunning, true);

        DetectPlayer.State = 0;

        battleState = 5;
    }

    public void FinalObjective()
    {
        StartCoroutine(_FinalObjective());
    }

    IEnumerator _FinalObjective()
    {
        yield return new WaitForSeconds(0.5f);

        Speech.clip = GetOut;
        Speech.Play();

        ExitDoorController.EndGame = true;
    }

    public void WinGame()
    {
        PlayerMovement.LockMovement = true;

        AntiPlayerFollow.State = 3;
        DetectPlayer.State = 3;

        StartCoroutine(StartWonGameScene());
    }

    IEnumerator StartWonGameScene()
    {
        BlankScreenAnimator.Play("FadeEnter");

        RandomSoundsController.Active = false;
        yield return Fades.Volume(BackgroundSoundsController, BackgroundVolume, 0f, 0.5f);

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene("Win");
    }

    IEnumerator FadeInText(TextMeshProUGUI textMeshPro)
    {
        yield return Fades.Graphic(textMeshPro, 0f, 1f, 0.5f);
    }

    IEnumerator FadeOutText(TextMeshProUGUI textMeshPro)
    {
        yield return Fades.Graphic(textMeshPro, 1f, 0f, 0.5f);

        textMeshPro.enabled = false;
    }
}
