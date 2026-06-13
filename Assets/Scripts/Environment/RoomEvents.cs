using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The cabins are not passive (random tension category). Entering a room rolls
// a die: its lamp sputters violently as you step in (and recovers — rooms
// never go dark, items must stay findable), the door slams shut behind you,
// or muffled footsteps cross the deck above your head. Coming BACK to a room
// you already visited can swap its painting — the "looked twice" beat.
// Detection is matrix-based: corridor → doorway → interior. Added at runtime
// by DeckGeneration.
public class RoomEvents : MonoBehaviour
{
    private DeckGeneration deck;
    private Transform player;
    private StepSounds playerSteps;
    private DetectPlayer detect;

    private AudioSource overheadSource;

    private class RoomState
    {
        public GameObject root;
        public int entries;
    }
    private readonly List<RoomState> rooms = new();

    private bool wasInsideRoom;
    private bool crossedDoorway;
    private float nextEventAllowedAt;

    private const float MatrixCell = 3.75f;
    private const float FirstEntryChance = 0.4f;
    private const float RevisitSwapChance = 0.35f;
    private const float EventCooldown = 25f;

    void Start()
    {
        deck = GetComponent<DeckGeneration>();
        var movement = FindAnyObjectByType<PlayerMovement>();
        if (movement == null) { enabled = false; return; }
        player = movement.transform;
        playerSteps = movement.GetComponentInChildren<StepSounds>();
        detect = FindAnyObjectByType<DetectPlayer>();

        foreach (GameObject room in deck.RoomInstances)
            rooms.Add(new RoomState { root = room });

        var go = new GameObject("OverheadSteps");
        go.transform.SetParent(transform, false);
        overheadSource = go.AddComponent<AudioSource>();
        overheadSource.playOnAwake = false;
        overheadSource.spatialBlend = 1f;
        overheadSource.dopplerLevel = 0f;
        overheadSource.rolloffMode = AudioRolloffMode.Linear;
        overheadSource.minDistance = 1.5f;
        overheadSource.maxDistance = 14f;
        var muffle = go.AddComponent<AudioLowPassFilter>();
        muffle.cutoffFrequency = 700f; // a whole deck of wood in the way
    }

    void Update()
    {
        if (player == null) return;

        int cell = CellValue();
        bool doorway = cell >= 4 && cell < 12;
        bool insideRoom = cell != 1 && !doorway;

        if (doorway) crossedDoorway = true;
        else if (cell == 1) crossedDoorway = false;

        // Entering = first interior cell after passing through a doorway.
        if (insideRoom && !wasInsideRoom && crossedDoorway) OnRoomEntered();
        wasInsideRoom = insideRoom;
    }

    private int CellValue()
    {
        int[,] matrix = deck.Matrix;
        if (matrix == null) return 1;
        int x = Mathf.RoundToInt(player.position.x / MatrixCell);
        int y = Mathf.RoundToInt(player.position.z / MatrixCell);
        if (x < 0 || y < 0 || x >= DeckGenerator.Width || y >= DeckGenerator.Height) return 1;
        return matrix[x, y];
    }

    private void OnRoomEntered()
    {
        RoomState room = NearestRoom();
        if (room == null) return;
        room.entries++;

        // Quiet during encounters; rate-limited so rooms don't become a circus.
        if (detect != null && detect.State != 0) return;
        if (Time.time < nextEventAllowedAt) return;

        if (room.entries == 1)
        {
            if (Random.value > FirstEntryChance) return;
            nextEventAllowedAt = Time.time + EventCooldown;

            float roll = Random.value;
            if (roll < 0.4f && SputterRoomLamp(room)) return;
            if (roll < 0.7f && SlamDoorBehind()) return;
            StartCoroutine(OverheadSteps());
        }
        else if (Random.value < RevisitSwapChance)
        {
            nextEventAllowedAt = Time.time + EventCooldown;
            SwapPainting(room);
        }
    }

    private RoomState NearestRoom()
    {
        RoomState best = null;
        float bestDistance = 9f; // rooms are 3 cells wide; anything farther is wrong
        foreach (RoomState room in rooms)
        {
            if (room.root == null) continue;
            float d = Vector3.Distance(player.position, room.root.transform.position);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = room;
            }
        }
        return best;
    }

    // --- the events ---------------------------------------------------------

    // A healthy room lamp greets you with a violent sputter, then recovers.
    private bool SputterRoomLamp(RoomState room)
    {
        foreach (Light lamp in room.root.GetComponentsInChildren<Light>())
        {
            if (lamp == null || !lamp.enabled) continue;
            if (lamp.GetComponent<FlickeringLight>() != null) continue; // already defective
            StartCoroutine(Sputter(lamp));
            return true;
        }
        return false;
    }

    private IEnumerator Sputter(Light lamp)
    {
        float baseIntensity = lamp.intensity;
        float end = Time.time + Random.Range(0.9f, 1.6f);
        while (Time.time < end)
        {
            if (lamp == null) yield break;
            lamp.enabled = Random.value > 0.45f;
            lamp.intensity = baseIntensity * Random.Range(0.3f, 1f);
            yield return new WaitForSeconds(Random.Range(0.03f, 0.09f));
        }
        if (lamp == null) yield break;
        lamp.enabled = true;
        lamp.intensity = baseIntensity;
    }

    // The door you just came through closes on its own.
    private bool SlamDoorBehind()
    {
        DoorTrigger nearest = null;
        float bestDistance = 6f;
        foreach (DoorTrigger trigger in FindObjectsByType<DoorTrigger>())
        {
            float d = Vector3.Distance(player.position, trigger.transform.position);
            if (d < bestDistance)
            {
                bestDistance = d;
                nearest = trigger;
            }
        }
        if (nearest == null || nearest.Door == null) return false;
        StartCoroutine(Slam(nearest));
        return true;
    }

    private IEnumerator Slam(DoorTrigger trigger)
    {
        yield return new WaitForSeconds(Random.Range(0.8f, 1.4f));
        if (trigger == null || trigger.Door == null) yield break;

        // Close whichever way it is open right now (same angle logic DoorState uses).
        float angle = Mathf.DeltaAngle(0f, trigger.Door.transform.localEulerAngles.y);
        if (Mathf.Abs(angle) < 1f) yield break; // already closed

        trigger.DoorAnimator.Play(angle > 0f ? "InnerDoorClose" : "OuterDoorClose");
        if (trigger.DoorClose != null) trigger.DoorClose.Play();
    }

    // Footsteps from the deck above — someone pacing where no one can be.
    private IEnumerator OverheadSteps()
    {
        if (playerSteps == null || playerSteps.Sounds == null || playerSteps.Sounds.Length == 0)
            yield break;

        int steps = Random.Range(4, 7);
        for (int i = 0; i < steps; i++)
        {
            yield return new WaitForSeconds(Random.Range(0.45f, 0.7f));
            if (player == null) yield break;
            overheadSource.transform.position = player.position
                + new Vector3(Random.Range(-1.5f, 1.5f), 3.4f, Random.Range(-1.5f, 1.5f));
            overheadSource.pitch = Random.Range(0.78f, 0.88f);
            overheadSource.PlayOneShot(
                playerSteps.Sounds[Random.Range(0, playerSteps.Sounds.Length)], 0.7f);
        }
    }

    private void SwapPainting(RoomState room)
    {
        foreach (PaintingChooser painting in room.root.GetComponentsInChildren<PaintingChooser>())
            painting.Reroll();
    }
}
