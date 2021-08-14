using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    public Animator AntiPlayerAnimator;
    public DetectPlayer DetectPlayer;

    public TextMeshProUGUI MessageTextMeshPro;
    public TextMeshProUGUI SubMessageTextMeshPro;

    public GameObject Deck;

    private int battleState = 0;

    private void Start()
    {
        Color color = MessageTextMeshPro.color;
        color.a = 0f;
        MessageTextMeshPro.color = color;
        SubMessageTextMeshPro.color = color;

        GlowDoors();
    }

    public void PlayPreBattleEffects()
    {
        if (battleState <= 1)
            StartCoroutine(PlayPreFirstBattleEffects());
    }

    IEnumerator PlayPreFirstBattleEffects()
    {
        yield return new WaitForSeconds(2f);

        // Voice

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
        for (float f = 0.05f; f > 0f; f -= 0.0025f)
        {
            BackgroundSoundsController.volume = f;
            yield return new WaitForSeconds(0.025f);
        }

        yield return new WaitForSeconds(4f);

        StartCoroutine(FadeInText(MessageTextMeshPro));
        yield return new WaitForSeconds(4f);

        StartCoroutine(FadeInText(SubMessageTextMeshPro));
        yield return new WaitForSeconds(5f);

        StartCoroutine(FadeOutText(MessageTextMeshPro));
        StartCoroutine(FadeOutText(SubMessageTextMeshPro));
        yield return new WaitForSeconds(3f);

        battleState++;
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
        for (float f = 0.05f; f > 0f; f -= 0.0025f)
        {
            BackgroundSoundsController.volume = f;
            yield return new WaitForSeconds(0.025f);
        }

        yield return new WaitForSeconds(2f);

        GameOver.Reason = 0;
        SceneManager.LoadScene("GameOver");
    }

    public void PostFirstBattleSetup()
    {
        AntiPlayerFollow.Respawn();

        StartCoroutine(PlayPostFirstBattleEffects());

        battleState = 3;
    }

    IEnumerator PlayPostFirstBattleEffects()
    {
        for (float f = 0f; f < 0.05f; f += 0.0025f)
        {
            BackgroundSoundsController.volume = f;
            yield return new WaitForSeconds(0.025f);
        }

        BlankScreenAnimator.Play("FadeExit");

        RandomSoundsController.Active = true;

        battleState = 4;
    }

    public void AfterPostBattleSetup()
    {
        DetectPlayer.State = 3;

        FirstPersonController.ResetRotation();
        FirstPersonController.LockCamera = false;
        PlayerMovement.LockMovement = false;

        AntiPlayerFollow.State = 2;
        AntiPlayerFollow.CurrentTargetPosition = AntiPlayerFollow.transform.position;
        AntiPlayerAnimator.SetBool("isRunning", true);

        DetectPlayer.State = 0;

        battleState = 5;
    }

    public void FinalObjective()
    {
        StartCoroutine(_FinalObjective());
    }

    IEnumerator _FinalObjective()
    {
        yield return new WaitForSeconds(1f);

        // Voice

        ExitDoorController.EndGame = true;
        GlowDoors();
    }

    public void GlowDoors()
    {
        foreach (Animator animator in Deck.GetComponentsInChildren<Animator>())
            if (animator.CompareTag("Exit")) animator.SetBool("isGlowing", true);
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
        for (float f = 0.05f; f > 0f; f -= 0.0025f)
        {
            BackgroundSoundsController.volume = f;
            yield return new WaitForSeconds(0.025f);
        }

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene("Win");
    }

    IEnumerator FadeInText(TextMeshProUGUI textMeshPro)
    {
        for (float f = 0f; f < 1f; f += 0.05f)
        {
            Color color = textMeshPro.color;
            color.a = f;
            textMeshPro.color = color;

            yield return new WaitForSeconds(0.025f);
        }
    }

    IEnumerator FadeOutText(TextMeshProUGUI textMeshPro)
    {
        for (float f = 1f; f > 0; f -= 0.05f)
        {
            Color color = textMeshPro.color;
            color.a = f;
            textMeshPro.color = color;

            yield return new WaitForSeconds(0.025f);
        }

        textMeshPro.enabled = false;
    }

    private void Update()
    {
        if (battleState == 2) PostFirstBattleSetup();
        if (battleState == 4) AfterPostBattleSetup();
    }
}
