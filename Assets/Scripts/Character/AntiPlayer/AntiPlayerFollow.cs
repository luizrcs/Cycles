using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntiPlayerFollow : MonoBehaviour
{
    // 0 = disabled, 1 = replaying the player's recorded path, 2 = roaming the maze, 3 = stopped
    public int State = 1;

    public DeckGeneration DeckGeneration;

    public Animator AntiPlayerAnimator;
    public StepSounds StepSounds;

    public GameObject Player;
    private PlayerPath playerPath;

    public Vector3 CurrentTargetPosition;
    private int lastTargetDirection;
    private int targetMatrixX, targetMatrixY;

    // The scene parks the AntiPlayer off-deck. Until its entrance it is a
    // ghost — invisible and non-colliding. Story-wise it enters the ship
    // 120 s after you did, the same way you did: through the entry door.
    public bool Engaged { get; private set; }

    private Rigidbody body;

    private const float FollowDelay = 120f;
    private const float TimeResolution = 100f;

    // Stop short of the player instead of stepping into their
    // CharacterController; DetectPlayer owns the encounter from this range.
    private const float EncounterDistance = 3f;

    private const float CellSize = 7.5f;
    private const float WalkY = 4.75f;

    // --- entrance through the entry door --------------------------------
    // 0 = ghost, waiting for the replay to come due; 1 = door open, walking
    // in; 2 = replaying the path.
    private int entrancePhase;
    private Animator enterDoorAnimator;
    private Collider enterDoorCollider;
    private bool enterDoorOpen;
    private float enterDoorCloseAt = -1f;
    private const float EntranceLead = 2.4f;   // door opens this long before the first point is due
    private const float EntranceSpeed = 3.2f;
    private static readonly Vector3 OutsideDoor = new(-2.4f, WalkY, 71.25f);
    private static readonly Vector3 InsideDoor = new(1.4f, WalkY, 71.25f);

    // --- replay route ----------------------------------------------------
    // Every recorded point that has become 120 s old, in order. The replay
    // walks through ALL of them — speeding up if it falls behind (e.g. after
    // an encounter pause) instead of beelining at the newest point, so it can
    // never cut a corner the player walked around.
    private readonly Queue<Vector4> route = new();   // xyz = position, w = yaw
    private Vector4 waypoint;
    private bool hasWaypoint;
    private float replayYaw;

    private const float BaseReplaySpeed = 5.5f;
    private const float CatchUpPerPoint = 0.01f;   // 100 queued points (1 s behind) = +1 m/s
    private const float MaxCatchUp = 6f;

    private static readonly int IsRunning = Animator.StringToHash("isRunning");

    void Start()
    {
        playerPath = Player.GetComponent<PlayerPath>();
        body = GetComponent<Rigidbody>();
        body.interpolation = RigidbodyInterpolation.Interpolate;

        SetGhost(true);

        // The wrongness layers live in their own components.
        gameObject.AddComponent<AntiPlayerGlitch>();
        gameObject.AddComponent<AntiPlayerNoise>();
        gameObject.AddComponent<FootprintTrail>();
    }

    private void SetGhost(bool ghost)
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            renderer.enabled = !ghost;
        foreach (var collider in GetComponentsInChildren<Collider>(true))
            collider.enabled = !ghost;
    }

    void Update()
    {
        switch (State)
        {
            case 1:
                DrainDuePoints();
                if (entrancePhase == 0) WaitOutside();
                else if (entrancePhase == 1) WalkIn();
                else FollowPath();
                break;
            case 2:
                Roam();
                break;
        }
    }

    // The entry door must close even if an encounter interrupts the entrance
    // (its collider was disabled — left open, the player could walk into the void).
    void LateUpdate()
    {
        if (!enterDoorOpen) return;

        if (enterDoorCloseAt < 0f && (entrancePhase == 2 || State != 1))
            enterDoorCloseAt = Time.time + 1.2f;

        if (enterDoorCloseAt > 0f && Time.time >= enterDoorCloseAt)
        {
            enterDoorOpen = false;
            if (enterDoorAnimator != null) enterDoorAnimator.Play("InnerDoorClose");
            StartCoroutine(ReenableEnterDoorCollider());
        }
    }

    IEnumerator ReenableEnterDoorCollider()
    {
        yield return new WaitForSeconds(0.5f);
        if (enterDoorCollider != null) enterDoorCollider.enabled = true;
    }

    // Move every point whose recorded time is now 120 s old onto the route.
    private void DrainDuePoints()
    {
        while (playerPath.Queue.Count > 0 && Time.time > FollowDelay + PeekTime())
        {
            Vector3 p = NextPoint(out float yaw);
            route.Enqueue(new Vector4(p.x, p.y, p.z, yaw));
        }
    }

    // GazeDiscipline's early sentence: breaking the watch-the-corridor rule
    // before the double has boarded summons it ahead of schedule — no door,
    // no warning, it is simply already inside.
    public void ForceEngage()
    {
        if (Engaged) return;
        Engaged = true;
        SetGhost(false);
        entrancePhase = 2;
    }

    private void WaitOutside()
    {
        bool due = route.Count > 0
            || (playerPath.Queue.Count > 0 && Time.time > FollowDelay + PeekTime() - EntranceLead);
        if (!due) return;

        Engaged = true;
        SetGhost(false);
        transform.position = OutsideDoor;
        body.position = OutsideDoor; // interpolated rigidbody: move the body too
        transform.rotation = Quaternion.Euler(0f, 90f, 0f); // facing into the ship

        GameObject enterDoor = DeckGeneration.GetComponent<EnterDoorContainer>().EnterDoor;
        if (enterDoor != null)
        {
            enterDoorAnimator = enterDoor.GetComponent<Animator>();
            enterDoorCollider = enterDoor.GetComponentInChildren<Collider>();
            enterDoorAnimator.Play("InnerDoorOpen");
            if (enterDoorCollider != null) enterDoorCollider.enabled = false;
            enterDoorOpen = true;
        }

        entrancePhase = 1;
    }

    private void WalkIn()
    {
        transform.position = Vector3.MoveTowards(transform.position, InsideDoor, EntranceSpeed * Time.deltaTime);

        if (transform.position != InsideDoor)
        {
            AntiPlayerAnimator.SetBool(IsRunning, true);
            StepSounds.PlayStepSound();
            return;
        }

        AntiPlayerAnimator.SetBool(IsRunning, false);
        if (route.Count == 0) return; // inside; wait for the replay to come due

        // Skip the recorded walk-in points still behind it in the doorway.
        while (route.Count > 0 && route.Peek().x <= InsideDoor.x) route.Dequeue();

        entrancePhase = 2;
    }

    private void FollowPath()
    {
        if (Vector3.Distance(transform.position, Player.transform.position) < EncounterDistance)
        {
            // Close enough — DetectPlayer owns the encounter. Stand still; the
            // route backlog accumulates and is walked back faster afterwards.
            AntiPlayerAnimator.SetBool(IsRunning, false);
            return;
        }

        float speed = BaseReplaySpeed + Mathf.Min(MaxCatchUp, route.Count * CatchUpPerPoint);
        float step = speed * Time.deltaTime;
        Vector3 position = transform.position;

        // Walk the route IN ORDER, consuming as many waypoints as this
        // frame's step covers. Never skips a point, so never cuts a wall.
        while (step > 0f)
        {
            if (!hasWaypoint)
            {
                if (route.Count == 0) break;
                waypoint = route.Dequeue();
                hasWaypoint = true;
            }

            Vector3 target = new(waypoint.x, waypoint.y, waypoint.z);
            float distance = Vector3.Distance(position, target);
            if (distance <= step)
            {
                position = target;
                step -= distance;
                replayYaw = waypoint.w;
                hasWaypoint = false;
            }
            else
            {
                position = Vector3.MoveTowards(position, target, step);
                step = 0f;
            }
        }

        if (position != transform.position)
        {
            body.MovePosition(position);
            AntiPlayerAnimator.SetBool(IsRunning, true);
            StepSounds.PlayStepSound();
        }
        else AntiPlayerAnimator.SetBool(IsRunning, false);

        Quaternion rotation = Quaternion.Euler(0f, replayYaw, 0f);
        body.MoveRotation(Quaternion.Slerp(transform.rotation, rotation, 12f * Time.deltaTime));
    }

    public void Respawn()
    {
        route.Clear();
        hasWaypoint = false;
        entrancePhase = 2;

        float minX = DeckGeneration.minX;
        float minZ = DeckGeneration.minZ;
        float maxX = DeckGeneration.maxX;
        float maxZ = DeckGeneration.maxZ;

        Vector3 playerPosition = Player.transform.position;
        Vector3 a = new(minX, transform.position.y, minZ);
        Vector3 b = new(minX, transform.position.y, maxZ);
        Vector3 c = new(maxX, transform.position.y, minZ);
        Vector3 d = new(maxX, transform.position.y, maxZ);

        float tempDistance;
        float distance = Vector3.Distance(playerPosition, a);
        Vector3 chosen = a;
        targetMatrixX = 1;
        targetMatrixY = 1;

        if ((tempDistance = Vector3.Distance(playerPosition, b)) > distance)
        {
            distance = tempDistance;
            chosen = b;
            targetMatrixX = 1;
            targetMatrixY = DeckGenerator.Height - 2;
        }

        if ((tempDistance = Vector3.Distance(playerPosition, c)) > distance)
        {
            distance = tempDistance;
            chosen = c;
            targetMatrixX = DeckGenerator.Width - 2;
            targetMatrixY = 1;
        }

        if (Vector3.Distance(playerPosition, d) > distance)
        {
            chosen = d;
            targetMatrixX = DeckGenerator.Width - 2;
            targetMatrixY = DeckGenerator.Height - 2;
        }

        transform.position = chosen;
        body.position = chosen; // interpolated rigidbody: move the body too
    }

    private float PeekTime()
    {
        return (playerPath.Queue.Peek() >> 40) / TimeResolution;
    }

    private Vector3 NextPoint(out float yaw)
    {
        ulong encoded = playerPath.Queue.Dequeue();
        yaw = (encoded & 0xFF) / 256f * 360f;
        float z = ((encoded >> 8) & 0xFFFF) / 10f;
        float x = ((encoded >> 24) & 0xFFFF) / 10f;
        return new Vector3(x, WalkY, z);
    }

    private void Roam()
    {
        Vector3 currentPosition = transform.position;
        if (currentPosition != CurrentTargetPosition)
        {
            float step = CellSize * Time.deltaTime;
            transform.position = Vector3.MoveTowards(currentPosition, CurrentTargetPosition, step);
            StepSounds.PlayStepSound();

            Quaternion rotation = Quaternion.LookRotation(CurrentTargetPosition - currentPosition);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 10f * Time.deltaTime);
        }
        else PickRoamTarget(currentPosition);
    }

    private void PickRoamTarget(Vector3 currentPosition)
    {
        int[,] matrix = DeckGeneration.Matrix;

        // Biased random walk: half the draws repeat the previous direction so the
        // roamer prefers straight lines, like the original. Bounded attempts so a
        // boxed-in roamer can never hard-freeze the game.
        for (int attempt = 0; attempt < 64; attempt++)
        {
            int direction = Random.Range(0, 8);
            if (direction >= 4) direction = lastTargetDirection;

            switch (direction)
            {
                case 0:
                    if (matrix[targetMatrixX + 1, targetMatrixY] == 1)
                    {
                        targetMatrixX += 2;
                        CurrentTargetPosition = new(currentPosition.x + CellSize, WalkY, currentPosition.z);
                        lastTargetDirection = direction;
                        return;
                    }
                    break;
                case 1:
                    if (matrix[targetMatrixX, targetMatrixY + 1] == 1)
                    {
                        targetMatrixY += 2;
                        CurrentTargetPosition = new(currentPosition.x, WalkY, currentPosition.z + CellSize);
                        lastTargetDirection = direction;
                        return;
                    }
                    break;
                case 2:
                    if (matrix[targetMatrixX - 1, targetMatrixY] == 1)
                    {
                        targetMatrixX -= 2;
                        CurrentTargetPosition = new(currentPosition.x - CellSize, WalkY, currentPosition.z);
                        lastTargetDirection = direction;
                        return;
                    }
                    break;
                case 3:
                    if (matrix[targetMatrixX, targetMatrixY - 1] == 1)
                    {
                        targetMatrixY -= 2;
                        CurrentTargetPosition = new(currentPosition.x, WalkY, currentPosition.z - CellSize);
                        lastTargetDirection = direction;
                        return;
                    }
                    break;
            }
        }
    }
}
