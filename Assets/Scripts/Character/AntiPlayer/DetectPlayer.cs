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
    private Rigidbody body;

    // 0 = searching, 1 = running at the player, 2 = battle, 3 = inactive
    public int State = 0;

    private const float SightDistance = 30f;
    private const float TouchDistance = 3.5f;
    private const float BattleDistance = 5f;
    private const float RunSpeed = 15f;

    private static readonly int IsRunning = Animator.StringToHash("isRunning");

    private void Start()
    {
        playerMovement = Player.GetComponent<PlayerMovement>();
        body = GetComponent<Rigidbody>();
    }

    void Update()
    {
        switch (State)
        {
            case 0:
                if (AntiPlayerFollow.Engaged && (SeesPlayer() || TouchesPlayer())) StartEncounter();
                break;
            case 1:
                MoveTowardsPlayer();
                FirstPersonController.LookAtAntiPlayer();

                float distance = Vector3.Distance(transform.position, Player.transform.position);
                if (distance < BattleDistance)
                {
                    State = 2;
                    AntiPlayerAnimator.SetBool(IsRunning, false);

                    GameLogic.PlayBattleEffects();
                }
                break;
            case 2:
                FirstPersonController.LookAtAntiPlayer();
                break;
        }
    }

    private bool SeesPlayer()
    {
        return Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, SightDistance)
            && hit.transform.CompareTag("AntiPlayerDetector");
    }

    // The path replay stops short of the player, so line of sight alone can miss
    // when it catches up from behind; proximity always starts the encounter.
    private bool TouchesPlayer()
    {
        return AntiPlayerFollow.State == 1
            && Vector3.Distance(transform.position, Player.transform.position) < TouchDistance;
    }

    private void StartEncounter()
    {
        State = 1;
        DeactivateFollower();
        AntiPlayerAnimator.SetBool(IsRunning, true);

        FirstPersonController.LockCamera = true;
        playerMovement.LockMovement = true;

        GameLogic.PlayPreBattleEffects();
    }

    private void DeactivateFollower()
    {
        AntiPlayerFollow.State = 0;
        PlayerPath.Active = false;
    }

    private void MoveTowardsPlayer()
    {
        Vector3 position = transform.position;
        Vector3 playerPosition = Player.transform.position;
        float step = RunSpeed * Time.deltaTime;

        Vector3 next = Vector3.MoveTowards(position, playerPosition, step);

        // Never run inside the player's CharacterController; it would shove them around.
        if (Vector3.Distance(next, playerPosition) > 1.5f) body.MovePosition(next);

        StepSounds.PlayStepSound();
        transform.LookAt(playerPosition);
    }
}
