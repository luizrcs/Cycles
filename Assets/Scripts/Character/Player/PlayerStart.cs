using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStart : MonoBehaviour
{
    public FirstPersonController FirstPersonController;
    public PlayerMovement PlayerMovement;

    private PlayerPath playerPath;

    public EnterDoorContainer EnterDoorContainer;

    private Animator enterDoorAnimator;
    private Collider enterDoorCollider;

    public AudioClip AnyoneThere;
    public AudioSource Speech;
    public StepSounds StepSounds;

    private bool startGame = false;

    private Vector3 startTarget = new(3.75f, 4.75f, 71.25f);

    void Start()
    {
        playerPath = GetComponent<PlayerPath>();

        StartCoroutine(BeginStart());
    }

    void Update()
    {
        if (startGame)
        {
            if (transform.position != startTarget)
            {
                float step = 5f * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, startTarget, step);
                StepSounds.PlayStepSound();

                if (transform.position.x >= 0) playerPath.Active = true;
            }
            else
            {
                startGame = false;

                StartCoroutine(FinishStart());
            }
        }
    }

    IEnumerator BeginStart()
    {
        yield return new WaitForSeconds(0.5f);

        GameObject enterDoor = EnterDoorContainer.EnterDoor;
        enterDoorAnimator = enterDoor.GetComponent<Animator>();
        enterDoorCollider = enterDoor.GetComponentInChildren<Collider>();

        enterDoorAnimator.Play("InnerDoorOpen");
        enterDoorCollider.enabled = false;

        startGame = true;
    }

    IEnumerator FinishStart()
    {
        enterDoorAnimator.Play("InnerDoorClose");

        yield return new WaitForSeconds(0.5f);

        enterDoorCollider.enabled = true;

        FirstPersonController.LockCamera = false;
        PlayerMovement.LockMovement = false;

        Speech.clip = AnyoneThere;
        Speech.Play();
    }
}
