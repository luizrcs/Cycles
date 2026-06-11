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

    // The scene places the AntiPlayer on the player's spawn point. Until the
    // 120 s delayed path replay begins it must be a ghost — invisible and
    // non-colliding — or it overlaps the player's CharacterController from the
    // first frame, slowly shoving them through the maze and triggering an
    // instant encounter. Story-wise it "enters the ship" 120 s after you did.
    public bool Engaged { get; private set; }

    private Rigidbody body;

    private const float FollowDelay = 120f;
    private const float TimeResolution = 100f;

    // Stop short of the player instead of teleporting into their CharacterController,
    // which used to physically shove them across the maze. DetectPlayer handles the
    // encounter once we are this close.
    private const float EncounterDistance = 3f;

    private const float CellSize = 7.5f;
    private const float WalkY = 4.75f;

    private static readonly int IsRunning = Animator.StringToHash("isRunning");

    void Start()
    {
        playerPath = Player.GetComponent<PlayerPath>();
        body = GetComponent<Rigidbody>();

        SetGhost(true);
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
                FollowPath();
                break;
            case 2:
                Roam();
                break;
        }
    }

    private void FollowPath()
    {
        if (playerPath.Queue.Count == 0 || Time.time <= FollowDelay + PeekTime()) return;

        if (!Engaged)
        {
            Engaged = true;
            SetGhost(false);
        }

        if (Vector3.Distance(transform.position, Player.transform.position) < EncounterDistance)
        {
            AntiPlayerAnimator.SetBool(IsRunning, false);
            return;
        }

        Vector3 targetPosition = TargetPosition();
        if (targetPosition != CurrentTargetPosition)
        {
            CurrentTargetPosition = targetPosition;
            body.MovePosition(CurrentTargetPosition);

            AntiPlayerAnimator.SetBool(IsRunning, true);
            StepSounds.PlayStepSound();
        }
        else AntiPlayerAnimator.SetBool(IsRunning, false);

        Vector3 targetRotation = transform.rotation.eulerAngles;
        targetRotation.y = TargetRotationY();
        body.MoveRotation(Quaternion.Euler(targetRotation));
    }

    public void Respawn()
    {
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
    }

    private float PeekTime()
    {
        ulong encodedValue = playerPath.Queue.Peek();
        return (encodedValue >> 40) / TimeResolution;
    }

    private Vector3 TargetPosition()
    {
        ulong encodedValue = playerPath.Queue.Peek();

        float z = ((encodedValue >>= 8) & 0xFFFF) / 10f;
        float x = ((encodedValue >>= 16) & 0xFFFF) / 10f;

        return new(x, WalkY, z);
    }

    private float TargetRotationY()
    {
        ulong encodedValue = playerPath.Queue.Dequeue();
        return (encodedValue & 0xFF) / 256f * 360f;
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
