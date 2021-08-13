using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectPlayer : MonoBehaviour
{
    public GameLogic GameLogic;

    public GameObject Player;
    public FirstPersonController FirstPersonController;
    private PlayerMovement playerMovement;

    public AntiPlayerFollow AntiPlayerFollow;
    public StepSounds StepSounds;

    public PlayerPath PlayerPath;

    public Animator AntiPlayerAnimator;
    private Rigidbody rigidbody;

    public int State = 0;

    private void Start()
    {
        playerMovement = Player.GetComponent<PlayerMovement>();
        rigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        switch (State)
        {
            case 0:
                RaycastHit hit;
                if (Physics.Raycast(transform.position, transform.forward, out hit))
                {
                    Transform hitTransform = hit.transform;
                    if (hitTransform.CompareTag("AntiPlayerDetector"))
                    {
                        State = 1;

                        DeactivateFollower();

                        AntiPlayerAnimator.SetBool("isRunning", true);
                        MoveTowardsPlayer();

                        FirstPersonController.LockCamera = true;
                        FirstPersonController.LookAtAntiPlayer();
                        playerMovement.LockMovement = true;

                        GameLogic.PlayPreBattleEffects();
                    }
                }
                break;
            case 1:
                MoveTowardsPlayer();
                FirstPersonController.LookAtAntiPlayer();

                Vector3 position = transform.position;
                Vector3 playerPosition = Player.transform.position;
                float distance = Vector3.Distance(position, playerPosition);
                if (distance < 5f)
                {
                    State = 2;
                    AntiPlayerAnimator.SetBool("isRunning", false);
                    GameLogic.PlayBattleEffects();
                }
                break;
            case 2:
                FirstPersonController.LookAtAntiPlayer();
                break;
        }
    }

    private void DeactivateFollower()
    {
        AntiPlayerFollow.Active = false;
        PlayerPath.Active = false;
    }

    private void MoveTowardsPlayer()
    {
        float step = 15f * Time.deltaTime;

        Vector3 position = transform.position;
        Vector3 playerPosition = Player.transform.position;

        rigidbody.MovePosition(Vector3.MoveTowards(position, playerPosition, step));
        transform.LookAt(playerPosition);

        StepSounds.PlayStepSound();
    }
}
