using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitDoorTrigger : MonoBehaviour
{
    public Animator DoorAnimator;
    public AudioSource DoorOpen;

    private GameLogic gameLogic;

    private void Start()
    {
        gameLogic = transform.parent.parent.GetComponent<GameLogicContainer>().GameLogic;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ExitDoorController.EndGame && other.name == "Player")
        {
            DoorAnimator.Play("OuterDoorOpen");
            DoorOpen.Play();

            gameLogic.WinGame();
        }
    }
}
