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
    private const float SightHalfAngle = 35f;
    private const float TouchDistance = 3.5f;
    private const float BattleDistance = 5f;
    private const float RunSpeed = 15f;

    private static readonly int IsRunning = Animator.StringToHash("isRunning");

    private void Start()
    {
        playerMovement = Player.GetComponent<PlayerMovement>();
        body = GetComponent<Rigidbody>();

        // The dread feedback loop lives on the player camera but is owned by
        // this encounter logic: heartbeat + camera pulse + stare rule
        // (DreadController), the watch-the-corridor rule (GazeDiscipline),
        // the mist nausea, and the endgame paradox bleed on the player's body.
        var dread = FirstPersonController.gameObject.AddComponent<DreadController>();
        dread.Init(this, AntiPlayerFollow, Player.transform);
        var gaze = FirstPersonController.gameObject.AddComponent<GazeDiscipline>();
        gaze.Init(this, AntiPlayerFollow, dread);
        FirstPersonController.gameObject.AddComponent<MistNausea>();
        Player.AddComponent<ParadoxBleed>();
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

    // Stared at too long (DreadController's rule): it noticed you noticing.
    public void ProvokeFromStare()
    {
        if (State == 0 && AntiPlayerFollow.Engaged && CanSeeBody()) StartEncounter();
    }

    // GazeDiscipline's sentence: you refused to watch the corridor, and it
    // was right behind you the whole time.
    public void Ambush()
    {
        if (State != 0 || !AntiPlayerFollow.Engaged) return;

        Vector3 back = -Player.transform.forward;
        back.y = 0f;
        back.Normalize();

        float distance = 3f;
        if (Physics.Raycast(Player.transform.position, back, out RaycastHit hit, distance + 0.6f,
                ~0, QueryTriggerInteraction.Ignore))
            distance = Mathf.Max(0.9f, hit.distance - 0.6f);

        transform.position = Player.transform.position + back * distance;

        StartEncounter();
    }

    private Vector3 Eye => transform.position + Vector3.up * 0.5f;

    // A cone of vision instead of the old single forward ray, but always
    // backed by an obstruction check — it cannot see through walls.
    private bool SeesPlayer()
    {
        Vector3 to = Player.transform.position + Vector3.up * 0.3f - Eye;
        if (to.magnitude > SightDistance) return false;
        if (Vector3.Angle(transform.forward, to) > SightHalfAngle) return false;
        return CanSeeBody();
    }

    // The path replay stops short of the player, so sight alone can miss when
    // it catches up from behind; proximity starts the encounter — but only
    // with an actual sightline. The old distance-only check aggroed through
    // walls whenever the replayed path passed the player on the other side.
    private bool TouchesPlayer()
    {
        return AntiPlayerFollow.State == 1
            && Vector3.Distance(transform.position, Player.transform.position) < TouchDistance
            && CanSeeBody();
    }

    // Bodies are 3D: a player strafe-peeking around a corner with half their
    // body exposed IS visible. Test head, chest, legs and both shoulders —
    // any unobstructed point counts (thin rays, triggers ignored; the only
    // solid thing they can land on besides geometry is the player).
    private bool CanSeeBody()
    {
        Vector3 eye = Eye;
        Vector3 center = Player.transform.position;
        Vector3 flat = center - eye;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.01f) return true;
        Vector3 right = Vector3.Cross(Vector3.up, flat.normalized);

        Vector3[] points =
        {
            center + Vector3.up * 1.0f,
            center + Vector3.up * 0.3f,
            center - Vector3.up * 0.7f,
            center + Vector3.up * 0.3f + right * 0.4f,
            center + Vector3.up * 0.3f - right * 0.4f,
        };

        foreach (Vector3 point in points)
        {
            if (!Physics.Linecast(eye, point, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)
                || hit.collider.GetComponentInParent<PlayerMovement>() != null)
                return true;
        }
        return false;
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

        Vector3 direction = playerPosition - position;
        direction.y = 0f;
        float distance = direction.magnitude;
        if (distance > 0.01f)
        {
            direction /= distance;

            // Encounters can start from a sliver of visibility around a
            // corner; steer around geometry instead of clipping through it.
            if (Physics.SphereCast(position + Vector3.up * 0.3f, 0.45f, direction, out RaycastHit hit,
                    step + 0.6f, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<PlayerMovement>() == null)
            {
                Vector3 slide = Vector3.ProjectOnPlane(direction, hit.normal);
                slide.y = 0f;
                if (slide.sqrMagnitude > 0.001f) direction = slide.normalized;
            }

            Vector3 next = position + direction * step;

            // Never run inside the player's CharacterController; it would shove them around.
            if (Vector3.Distance(next, playerPosition) > 1.5f) body.MovePosition(next);
        }

        StepSounds.PlayStepSound();
        transform.LookAt(playerPosition);
    }
}
