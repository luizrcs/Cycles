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
        // Only the real player can escape; the AntiPlayer shares the "Player" tag.
        if (ExitDoorController.EndGame && other.GetComponent<PlayerMovement>() != null)
        {
            DoorAnimator.Play("OuterDoorOpen");
            DoorOpen.Play();

            gameLogic.WinGame();
        }
    }
}
