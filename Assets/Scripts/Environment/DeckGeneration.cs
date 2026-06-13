using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckGeneration : MonoBehaviour
{
    public GameObject Ceiling_A;
    public GameObject Ceiling_B;
    public GameObject Ceiling_Thin;

    public GameObject Floor_A;
    public GameObject Floor_A_Thin;
    public GameObject Floor_B;
    public GameObject Floor_C;
    public GameObject Floor_D;
    public GameObject Floor_E;

    public GameObject Wall_A;
    public GameObject Wall_B;
    public GameObject Wall_Thin;
    public GameObject Wall_Door_A;
    public GameObject Wall_Door_B;

    public GameObject ExitDoor;

    public GameObject Room_A;
    public GameObject Room_B;

    public int WallWidth;
    public int WallHeight;

    public float EvenWallFactor;
    public float OddWallFactor;

    private DeckGenerator deckGenerator;
    public int[,] Matrix;

    private int oars, nails, compass;
    private int currentRoom = -1;

    public float minX = -1f, minZ = -1f, maxX = float.MinValue, maxZ = float.MinValue;

    // --- the dying ship: light-failure cascade -----------------------------
    // In the last minute before the paradox the corridor lamps die in waves,
    // farthest-from-the-exit first — the darkness closes in and chases the
    // player toward the one door that matters. Rooms stay lit (items must
    // stay findable to the end).
    private readonly List<Light> corridorLamps = new();
    public IReadOnlyList<Light> CorridorLamps => corridorLamps;

    // Every instantiated cabin root, for the room/gramophone systems.
    public readonly List<GameObject> RoomInstances = new();

    private Vector3 exitDoorPosition;
    private PlayerTimer playerTimer;
    private bool cascadeStarted;

    private const float CascadeStart = 180f; // T-60
    private const float CascadeEnd = 225f;   // T-15: only the exit's glow is left

    void Start()
    {
        deckGenerator = new DeckGenerator();
        while (deckGenerator.GeneratedRooms < 3) deckGenerator.GenerateDeck();

        Matrix = deckGenerator.Matrix;

        DecideItemsRooms();

        float deckX = 0;
        float deckZ = 0;

        for (int y = 0; y < DeckGenerator.Height; y++)
        {
            for (int x = 0; x < DeckGenerator.Width; x++)
            {
                InstantiateCeiling(x, y, deckX, deckZ);
                InstantiateFloor(x, y, deckX, deckZ);
                InstantiateWalls(x, y, deckX, deckZ);
                InstantiateRoom(x, y, deckX, deckZ);

                deckX += WallWidth * (EvenWallFactor + OddWallFactor) / 2;
            }

            deckX = 0;
            deckZ += WallWidth * (EvenWallFactor + OddWallFactor) / 2;
        }

        ApplyLightVariation();

        playerTimer = FindAnyObjectByType<PlayerTimer>();
        var exitTrigger = GetComponentInChildren<ExitDoorTrigger>();
        if (exitTrigger != null) exitDoorPosition = exitTrigger.transform.position;

        // Session 9: the ship grew events — big waves, living cabins, the sea
        // at the portholes, one gramophone, moonlight at the exits. All
        // runtime-added; no scene edits.
        gameObject.AddComponent<BigWave>();
        gameObject.AddComponent<RoomEvents>();
        gameObject.AddComponent<PortholeScares>();
        SetupGramophone();
        SetupMoonlight();
    }

    void Update()
    {
        // The exit-door moment: the instant the lifeboat is repairable, the
        // ship's power dies for a breath — only the exit's glow remains, a
        // beacon — then returns.
        if (!exitBlackoutStarted && ExitDoorController.EndGame)
        {
            exitBlackoutStarted = true;
            StartCoroutine(ExitBlackout());
        }

        if (cascadeStarted || playerTimer == null || playerTimer.Elapsed < CascadeStart) return;
        cascadeStarted = true;
        StartCoroutine(LightFailureCascade());
    }

    private bool exitBlackoutStarted;

    private IEnumerator ExitBlackout()
    {
        var restore = new List<(Light lamp, FlickeringLight flicker)>();
        foreach (Light lamp in corridorLamps)
        {
            if (lamp == null || !lamp.enabled) continue;
            var flicker = lamp.GetComponent<FlickeringLight>();
            if (flicker != null) flicker.enabled = false; // it would re-light its bulb
            lamp.enabled = false;
            var buzz = lamp.GetComponent<AudioSource>();
            if (buzz != null) buzz.mute = true;
            restore.Add((lamp, flicker));
        }

        // One deep structural thud, inside the head — the ship acknowledging.
        var go = new GameObject("PowerThud");
        var thud = go.AddComponent<AudioSource>();
        thud.spatialBlend = 0f;
        thud.pitch = 0.45f;
        thud.PlayOneShot(ProceduralAudio.MakeMetalKnock(), 0.9f);
        Destroy(go, 3f);

        yield return new WaitForSeconds(2.2f);

        foreach (var (lamp, flicker) in restore)
        {
            if (lamp == null) continue;
            lamp.enabled = true;
            var buzz = lamp.GetComponent<AudioSource>();
            if (buzz != null) buzz.mute = false;
            if (flicker != null) flicker.enabled = true;
        }
    }

    // One random cabin hides the gramophone.
    private void SetupGramophone()
    {
        if (RoomInstances.Count == 0) return;
        GameObject room = RoomInstances[deckGenerator.Random.Next(RoomInstances.Count)];

        var go = new GameObject("Gramophone");
        go.transform.SetParent(room.transform, false);
        go.transform.localPosition = new Vector3(0f, 1.1f, 0f);

        var gramophone = go.AddComponent<Gramophone>();
        var follow = FindAnyObjectByType<AntiPlayerFollow>();
        if (follow != null) gramophone.Init(follow);
    }

    // Moonlight: thin cold shafts angled in through the exit doors' portholes.
    // Local spots, no shadows — a bare directional would leak through the
    // unshadowed hull and flat-light the interior.
    private void SetupMoonlight()
    {
        Vector3 center = new((minX + maxX) / 2f, 0f, (minZ + maxZ) / 2f);
        foreach (ExitDoorTrigger trigger in GetComponentsInChildren<ExitDoorTrigger>())
        {
            Vector3 doorPosition = trigger.transform.position;
            Vector3 d = doorPosition - center;
            Vector3 outward = Mathf.Abs(d.x) > Mathf.Abs(d.z)
                ? new Vector3(Mathf.Sign(d.x), 0f, 0f)
                : new Vector3(0f, 0f, Mathf.Sign(d.z));

            var go = new GameObject("MoonShaft");
            go.transform.SetParent(transform, false);
            go.transform.position = doorPosition + outward * 2.1f + Vector3.up * 2.8f;
            go.transform.rotation = Quaternion.LookRotation(
                doorPosition - outward * 1.6f + Vector3.up * 0.3f - go.transform.position);

            var moon = go.AddComponent<Light>();
            moon.type = LightType.Spot;
            moon.color = new Color(0.6f, 0.71f, 0.95f);
            moon.intensity = 5.5f;
            moon.range = 13f;
            moon.spotAngle = 36f;
            moon.innerSpotAngle = 14f;
            moon.shadows = LightShadows.None;
        }
    }

    private IEnumerator LightFailureCascade()
    {
        // Farthest from the exit dies first: the lit region shrinks toward
        // the way out.
        corridorLamps.RemoveAll(l => l == null);
        corridorLamps.Sort((a, b) =>
            Vector3.Distance(b.transform.position, exitDoorPosition)
                .CompareTo(Vector3.Distance(a.transform.position, exitDoorPosition)));

        float window = CascadeEnd - CascadeStart;
        for (int i = 0; i < corridorLamps.Count; i++)
        {
            float dueAt = CascadeStart + window * (i + 1) / (corridorLamps.Count + 1);
            while (playerTimer.Elapsed < dueAt) yield return null;

            Light lamp = corridorLamps[i];
            if (lamp == null) continue;

            var flicker = lamp.GetComponent<FlickeringLight>();
            if (flicker != null) Destroy(flicker);
            StartCoroutine(DeathSputter(lamp));
        }
    }

    // A bulb does not just stop: a fast dirty sputter, then nothing.
    private IEnumerator DeathSputter(Light lamp)
    {
        float baseIntensity = lamp.intensity;
        float end = Time.time + Random.Range(0.25f, 0.6f);
        while (Time.time < end)
        {
            if (lamp == null) yield break;
            bool on = Random.value > 0.5f;
            lamp.enabled = on;
            lamp.intensity = baseIntensity * Random.Range(0.4f, 1f);
            yield return new WaitForSeconds(Random.Range(0.02f, 0.07f));
        }
        if (lamp != null) KillLamp(lamp);
    }

    // An old ship's wiring is failing. Distribution (of ALL lamps, not of
    // survivors):
    //   corridor ceiling lamps: 20% dead, 20% flasher, 20% dimmer, 40% "healthy"
    //   wall sconces:           50% dead, 20% flasher, 20% dimmer, 10% "healthy"
    //   room lamps:             never dead (collectibles must stay findable),
    //                           but 20% flasher + 20% dimmer like everywhere.
    // "Healthy" corridor bulbs still run a luminosity lottery between 0 and
    // 100%, eased toward bright (pow 0.35), plus a warmth variance — no two
    // bulbs burn alike.
    private void ApplyLightVariation()
    {
        foreach (Light light in GetComponentsInChildren<Light>())
        {
            bool inRoom = IsInsideRoom(light.transform);
            bool sconce = HasAncestorContaining(light.transform, "Wall_Lamp");

            double roll = deckGenerator.Random.NextDouble();

            if (!inRoom)
            {
                double deadChance = sconce ? 0.5 : 0.2;
                if (roll < deadChance) { KillLamp(light); continue; }
                corridorLamps.Add(light); // survivor: eligible for the endgame cascade
                roll = (roll - deadChance) / (1.0 - deadChance); // re-normalize the survivor roll
                double defectShare = sconce ? 0.4 / 0.5 : 0.4 / 0.8;

                if (roll < defectShare / 2) { AddDefect(light, FlickeringLight.FailureMode.Flasher); continue; }
                if (roll < defectShare) { AddDefect(light, FlickeringLight.FailureMode.Dimmer); continue; }

                // healthy: eased luminosity lottery, anywhere from near-dark to full
                light.intensity *= Mathf.Pow(Random.value, 0.35f);
            }
            else
            {
                if (roll < 0.2) { AddDefect(light, FlickeringLight.FailureMode.Flasher); continue; }
                if (roll < 0.4) { AddDefect(light, FlickeringLight.FailureMode.Dimmer); continue; }
            }

            float warmth = Random.Range(-0.06f, 0.04f);
            light.color = new Color(
                Mathf.Clamp01(light.color.r),
                Mathf.Clamp01(light.color.g + warmth),
                Mathf.Clamp01(light.color.b + warmth * 2f));
        }
    }

    private static void AddDefect(Light light, FlickeringLight.FailureMode mode)
    {
        var flicker = light.gameObject.AddComponent<FlickeringLight>();
        flicker.Mode = mode;
    }

    private static bool HasAncestorContaining(Transform t, string fragment)
    {
        for (Transform p = t; p != null; p = p.parent)
            if (p.name.Contains(fragment)) return true;
        return false;
    }

    private static bool IsInsideRoom(Transform t)
    {
        for (Transform p = t; p != null; p = p.parent)
            if (p.name.StartsWith("Room_")) return true;
        return false;
    }

    private static void KillLamp(Light light)
    {
        light.enabled = false;

        // A dead bulb makes no sound (cascade victims may carry a buzz source).
        var buzz = light.GetComponent<AudioSource>();
        if (buzz != null) buzz.Stop();

        if (light.transform.parent == null) return;
        foreach (Renderer renderer in light.transform.parent.GetComponentsInChildren<Renderer>())
        {
            Material m = renderer.material;
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
        }
    }

    private int RandomIntExceptOne(int max, int exception)
    {
        int value = deckGenerator.Random.Next(max - 1);
        return value < exception ? value : value + 1;
    }

    private int RandomIntExceptTwo(int max, int exception1, int exception2)
    {
        int value = deckGenerator.Random.Next(max - 2);
        return value < Mathf.Min(exception1, exception2) ? value : (value < Mathf.Max(exception1, exception2) - 1 ? value + 1 : value + 2);
    }

    private void DecideItemsRooms()
    {
        int generatedRooms = deckGenerator.GeneratedRooms;
        oars = deckGenerator.Random.Next(generatedRooms);
        nails = RandomIntExceptOne(generatedRooms, oars);
        compass = RandomIntExceptTwo(generatedRooms, oars, nails);
    }

    private void InstantiateCeiling(int x, int y, float deckX, float deckZ)
    {
        int cell = Matrix[x, y];
        if (cell == 1)
        {
            if (x % 2 == 0) Instantiate(Ceiling_Thin, new(deckX, WallHeight, deckZ), Quaternion.Euler(0, 0, 0), transform);
            else if (y % 2 == 0) Instantiate(Ceiling_Thin, new(deckX, WallHeight, deckZ), Quaternion.Euler(0, 90, 0), transform);
            else
            {
                GameObject ceiling = ((x - 1) % 4 == 0 ^ (y - 1) % 4 == 0) ? Ceiling_B : Ceiling_A;
                Instantiate(ceiling, new(deckX, WallHeight, deckZ), Quaternion.Euler(0, 0, 0), transform);
            }
        }
    }

    private void InstantiateFloor(int x, int y, float deckX, float deckZ)
    {
        int cell = Matrix[x, y];
        if (cell == 1)
        {
            if (minX == -1f) minX = deckX;
            if (minZ == -1f) minZ = deckZ;
            if (deckX > maxX) maxX = deckX;
            if (deckZ > maxZ) maxZ = deckZ;

            int north = Mathf.Min(Matrix[x, y + 1], 1);
            int east = Mathf.Min(Matrix[x + 1, y], 1);
            int south = Mathf.Min(Matrix[x, y - 1], 1);
            int west = Mathf.Min(Matrix[x - 1, y], 1);
            int sum = north + east + south + west;

            if (x % 2 == 0) Instantiate(Floor_A_Thin, new(deckX, 0, deckZ), Quaternion.Euler(90, 0, 0), transform);
            else if (y % 2 == 0) Instantiate(Floor_A_Thin, new(deckX, 0, deckZ), Quaternion.Euler(90, 90, 0), transform);
            else
            {
                switch (sum)
                {
                    case 1:
                        if (north == 1) Instantiate(Floor_E, new(deckX, 0, deckZ), Quaternion.Euler(90, 0, 0), transform);
                        else if (east == 1) Instantiate(Floor_E, new(deckX, 0, deckZ), Quaternion.Euler(90, 90, 0), transform);
                        else if (south == 1) Instantiate(Floor_E, new(deckX, 0, deckZ), Quaternion.Euler(90, 180, 0), transform);
                        else if (west == 1) Instantiate(Floor_E, new(deckX, 0, deckZ), Quaternion.Euler(90, 270, 0), transform);
                        break;
                    case 2:
                        if (north == 1)
                        {
                            if (south == 1) Instantiate(Floor_A, new(deckX, 0, deckZ), Quaternion.Euler(90, 90, 0), transform);
                            else if (east == 1) Instantiate(Floor_B, new(deckX, 0, deckZ), Quaternion.Euler(90, 0, 0), transform);
                            else if (west == 1) Instantiate(Floor_B, new(deckX, 0, deckZ), Quaternion.Euler(90, 270, 0), transform);
                        }
                        else if (east == 1)
                        {
                            if (west == 1) Instantiate(Floor_A, new(deckX, 0, deckZ), Quaternion.Euler(90, 0, 0), transform);
                            else Instantiate(Floor_B, new(deckX, 0, deckZ), Quaternion.Euler(90, 90, 0), transform);
                        }
                        else Instantiate(Floor_B, new(deckX, 0, deckZ), Quaternion.Euler(90, 180, 0), transform);
                        break;
                    case 3:
                        if (north == 0) Instantiate(Floor_C, new(deckX, 0, deckZ), Quaternion.Euler(90, 0, 0), transform);
                        else if (east == 0) Instantiate(Floor_C, new(deckX, 0, deckZ), Quaternion.Euler(90, 90, 0), transform);
                        else if (south == 0) Instantiate(Floor_C, new(deckX, 0, deckZ), Quaternion.Euler(90, 180, 0), transform);
                        else if (west == 0) Instantiate(Floor_C, new(deckX, 0, deckZ), Quaternion.Euler(90, 270, 0), transform);
                        break;
                    case 4:
                        Instantiate(Floor_D, new(deckX, 0, deckZ), Quaternion.Euler(90, 0, 0), transform);
                        break;
                }
            }
        }
    }

    private void InstantiateWalls(int x, int y, float deckX, float deckZ)
    {
        int cell = Matrix[x, y];
        if (cell == 1)
        {
            if (x % 2 == 0)
            {
                Instantiate(Wall_Thin, new(deckX, 0, deckZ + WallWidth / 2f), Quaternion.Euler(0, 0, 0), transform);
                Instantiate(Wall_Thin, new(deckX, 0, deckZ - WallWidth / 2f), Quaternion.Euler(0, 180, 0), transform);
            }
            else if (y % 2 == 0)
            {
                Instantiate(Wall_Thin, new(deckX + WallWidth / 2f, 0, deckZ), Quaternion.Euler(0, 90, 0), transform);
                Instantiate(Wall_Thin, new(deckX - WallWidth / 2f, 0, deckZ), Quaternion.Euler(0, 270, 0), transform);
            }
            else
            {
                int north = Matrix[x, y + 1];
                int east = Matrix[x + 1, y];
                int south = Matrix[x, y - 1];
                int west = Matrix[x - 1, y];

                GameObject wall = ((x - 1) % 4 == 0 ^ (y - 1) % 4 == 0) ? Wall_A : Wall_B;

                if (north == 0)
                {
                    // Choose per direction: reusing `wall` leaked ExitDoor into the
                    // other branches of the same cell, spawning bonus exit doors
                    // that opened into unfloored void cells.
                    GameObject northWall = wall;
                    if (
                        y == DeckGenerator.Height - 2
                        && (x - 1) % deckGenerator.RoomDistance == 0
                        && x != 1
                        && x != DeckGenerator.Width - 2
                    ) northWall = ExitDoor;

                    Instantiate(northWall, new(deckX, 0, deckZ + WallWidth / 2f), Quaternion.Euler(0, 0, 0), transform);
                }
                else if (north >= 4 && north < 12)
                {
                    int corner = north / 4;
                    GameObject wallDoor = corner == 1 ? Wall_Door_A : Wall_Door_B;
                    Instantiate(wallDoor, new(deckX, 0, deckZ + WallWidth / 2f), Quaternion.Euler(0, 0, 0), transform);
                }

                if (east == 0)
                {
                    GameObject eastWall = wall;
                    if (
                        x == DeckGenerator.Width - 2
                        && (y - 1) % deckGenerator.RoomDistance == 0
                        && y != 1
                        && y != DeckGenerator.Height - 2
                    ) eastWall = ExitDoor;

                    Instantiate(eastWall, new(deckX + WallWidth / 2f, 0, deckZ), Quaternion.Euler(0, 90, 0), transform);
                }
                else if (east >= 4 && east < 12)
                {
                    int corner = east / 4;
                    GameObject wallDoor = corner == 1 ? Wall_Door_A : Wall_Door_B;
                    Instantiate(wallDoor, new(deckX + WallWidth / 2f, 0, deckZ), Quaternion.Euler(0, 90, 0), transform);
                }

                if (south == 0)
                {
                    GameObject southWall = wall;
                    if (
                        y == 1
                        && (x - 1) % deckGenerator.RoomDistance == 0
                        && x != 1
                        && x != DeckGenerator.Width - 2
                    ) southWall = ExitDoor;

                    Instantiate(southWall, new(deckX, 0, deckZ - WallWidth / 2f), Quaternion.Euler(0, 180, 0), transform);
                }
                else if (south >= 4 && south < 12)
                {
                    int corner = south / 4;
                    GameObject wallDoor = corner == 1 ? Wall_Door_A : Wall_Door_B;
                    Instantiate(wallDoor, new(deckX, 0, deckZ - WallWidth / 2f), Quaternion.Euler(0, 180, 0), transform);
                }

                if (west == 0)
                {
                    GameObject westWall = wall;
                    if (
                        x == 1
                        && (y - 1) % deckGenerator.RoomDistance == 0
                        && y != 1
                        && y != DeckGenerator.Height - 2
                    ) westWall = ExitDoor;

                    GameObject theWall = Instantiate(westWall, new(deckX - WallWidth / 2f, 0, deckZ), Quaternion.Euler(0, 270, 0), transform);

                    if (deckX == 3.75f && deckZ == 71.25f)
                    {
                        GameObject enterDoor = null;
                        foreach(Transform transform in theWall.GetComponentsInChildren<Transform>())
                        {
                            if (transform.CompareTag("Exit"))
                            {
                                enterDoor = transform.gameObject;
                                break;
                            }
                        }

                        GetComponent<EnterDoorContainer>().EnterDoor = enterDoor;
                    }
                }
                else if (west >= 4 && west < 12)
                {
                    int corner = west / 4;
                    GameObject wallDoor = corner == 1 ? Wall_Door_A : Wall_Door_B;
                    Instantiate(wallDoor, new(deckX - WallWidth / 2f, 0, deckZ), Quaternion.Euler(0, 270, 0), transform);
                }
            }
        }
    }

    private void InstantiateRoom(int x, int y, float deckX, float deckZ)
    {
        int cell = Matrix[x, y];
        if (cell >= 12)
        {
            currentRoom++;

            cell -= 12;
            int corner = cell / 4;
            int rotation = cell % 4;

            GameObject chosenRoom = corner == 0 ? Room_A : Room_B;
            GameObject room = Instantiate(chosenRoom, new(deckX + 3.75f, 0, deckZ + 3.75f), Quaternion.Euler(0, 90 * rotation, 0), transform);
            RoomInstances.Add(room);

            foreach (Transform transform in room.GetComponentsInChildren<Transform>())
            {
                if (transform.CompareTag("Collectible"))
                {
                    switch (transform.name)
                    {
                        case "Oars":
                            if (currentRoom != oars) transform.gameObject.SetActive(false);
                            break;
                        case "Nails":
                            if (currentRoom != nails) transform.gameObject.SetActive(false);
                            break;
                        case "Compass":
                            if (currentRoom != compass) transform.gameObject.SetActive(false);
                            break;
                    }
                }
                else if (transform.CompareTag("Globe") && currentRoom == oars) transform.gameObject.SetActive(false);
            }
        }
    }
}
