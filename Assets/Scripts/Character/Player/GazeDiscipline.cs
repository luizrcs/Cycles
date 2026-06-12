using UnityEngine;
using UnityEngine.UI;

// The loop does not tolerate hiding. Once the double is aboard you must keep
// watching the corridors — staring at the floor, the ceiling or a nearby wall
// to avoid ever facing it is cheating the loop, and the loop notices:
//
//  - WRONG gaze = camera pitched past ±38° (floor/ceiling), or the LOOK
//    direction (not the corridor you stand in — peeking down a side corridor
//    from a corner is valid) hits geometry within 4.5 m. Suspended inside
//    rooms and doorways (deck matrix cell != 1), where staring at walls and
//    furniture is the whole point.
//  - Walking backwards counts too, but at less than half rate — checking the
//    hallway behind you is a legitimate fear.
//  - 1 s of grace, then ~2.2 s of ramp: glitch slices flash over the view and
//    the heartbeat rises (through DreadController.ExternalDread) — the
//    warning IS the tutorial. Recovery decays at half the buildup rate, so
//    looking away costs more time than it bought.
//  - At full penalty the double is simply THERE, right behind you (Ambush) —
//    survivable the first time, like every first encounter: the player gets
//    to learn the rule and live.
//
// Inactive until the double is engaged (the first 120 s are free practice)
// and during encounters. Added at runtime by DetectPlayer.
public class GazeDiscipline : MonoBehaviour
{
    private DetectPlayer detect;
    private AntiPlayerFollow follow;
    private DreadController dread;
    private Transform player;
    private CharacterController controller;
    private DeckGeneration deck;

    private float wrong; // accumulated wrong-gaze seconds
    private float armedAt = -1f;
    private RawImage flash;

    // The rule arms a few seconds AFTER the double boards: the entry door's
    // creak is the warning, and a player idling at the spawn (which faces a
    // wall) gets time to react instead of an inputless ambush.
    private const float ArmDelay = 6f;

    private const float Grace = 1.0f;
    private const float Ramp = 2.2f;
    private const float DecayRate = 0.5f;
    private const float BackwardsRate = 0.45f;
    private const float PitchLimit = 38f;
    // Must stay under half the corridor width (3.75) so looking ACROSS a
    // corridor from its center line is always legal.
    private const float OpenDistance = 3.2f;
    private const float MatrixCell = 3.75f;

    public void Init(DetectPlayer detectPlayer, AntiPlayerFollow antiPlayerFollow, DreadController dreadController)
    {
        detect = detectPlayer;
        follow = antiPlayerFollow;
        dread = dreadController;
        player = detectPlayer.Player.transform;
        controller = detectPlayer.Player.GetComponent<CharacterController>();
        deck = antiPlayerFollow.DeckGeneration;
    }

    void Start()
    {
        BuildFlashOverlay();
    }

    void Update()
    {
        if (detect == null) return;

        if (follow.Engaged && armedAt < 0f) armedAt = Time.time + ArmDelay;
        bool active = follow.Engaged && detect.State == 0 && armedAt > 0f && Time.time >= armedAt;
        float rate;
        if (!active) rate = -2f; // reset quickly before arrival / during encounters
        else if (WrongGaze()) rate = 1f;
        else if (MovingBackwards()) rate = BackwardsRate;
        else rate = -DecayRate;

        wrong = Mathf.Clamp(wrong + rate * Time.deltaTime, 0f, Grace + Ramp);
        float penalty = Mathf.Clamp01((wrong - Grace) / Ramp);

        if (dread != null) dread.ExternalDread = penalty * 0.9f;
        UpdateFlash(penalty);

        if (penalty >= 1f)
        {
            wrong = Grace + Ramp * 0.35f; // residual unease after it gets you
            detect.Ambush();
        }
    }

    private bool WrongGaze()
    {
        if (InsideRoomOrDoorway()) return false;

        float pitch = Mathf.DeltaAngle(0f, transform.localEulerAngles.x);
        if (Mathf.Abs(pitch) > PitchLimit) return true;

        // Judge the LOOK direction flattened to the deck plane: does open
        // corridor extend ahead of the gaze? Triggers (the double, items,
        // door triggers) never block; only real geometry counts.
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) return true;
        forward.Normalize();

        return Physics.Raycast(transform.position, forward, OpenDistance, ~0, QueryTriggerInteraction.Ignore);
    }

    private bool MovingBackwards()
    {
        if (controller == null) return false;
        Vector3 velocity = controller.velocity;
        velocity.y = 0f;
        if (velocity.magnitude < 1.2f) return false;

        Vector3 forward = player.forward;
        forward.y = 0f;
        return Vector3.Dot(velocity.normalized, forward.normalized) < -0.35f;
    }

    private bool InsideRoomOrDoorway()
    {
        int[,] matrix = deck != null ? deck.Matrix : null;
        if (matrix == null) return true;

        int x = Mathf.RoundToInt(player.position.x / MatrixCell);
        int y = Mathf.RoundToInt(player.position.z / MatrixCell);
        if (x < 0 || y < 0 || x >= DeckGenerator.Width || y >= DeckGenerator.Height) return true;

        return matrix[x, y] != 1; // 1 = corridor; anything else suspends the rule
    }

    // --- the flashes: slices of the glitch forcing themselves into view ----

    private void BuildFlashOverlay()
    {
        var canvasGO = new GameObject("GlitchFlash");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1; // under the scene canvas' blackout fade

        var imageGO = new GameObject("Flash");
        imageGO.transform.SetParent(canvasGO.transform, false);
        flash = imageGO.AddComponent<RawImage>();
        var rect = flash.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        flash.texture = MakeSliceTexture();
        flash.color = new Color(1f, 1f, 1f, 0f);
        flash.raycastTarget = false;
    }

    private void UpdateFlash(float penalty)
    {
        if (flash == null) return;
        if (penalty <= 0.02f)
        {
            SetFlashAlpha(0f);
            return;
        }

        // Time-snapped flicker, same language as the GlitchShell shader.
        float tick = Mathf.Floor(Time.time * 14f);
        float h = Mathf.Abs(Mathf.Sin(tick * 12.9898f) * 43758.5453f) % 1f;
        bool show = h < 0.25f + 0.45f * penalty;
        flash.uvRect = new Rect(0f, h * 7f, 1f, 0.6f + h);
        SetFlashAlpha(show ? penalty * (0.12f + 0.35f * h) : 0f);
    }

    private void SetFlashAlpha(float alpha)
    {
        Color color = flash.color;
        if (Mathf.Approximately(color.a, alpha)) return;
        color.a = alpha;
        flash.color = color;
    }

    private static Texture2D MakeSliceTexture()
    {
        var tex = new Texture2D(4, 96, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };
        var rng = new System.Random(404);
        for (int y = 0; y < 96; y++)
        {
            // Analog interference bands: pale desaturated static with a faint
            // warm/cool drift — a TV losing the signal, not a GPU artifact.
            bool on = rng.NextDouble() < 0.3;
            float gray = 0.75f + (float)rng.NextDouble() * 0.25f;
            Color c = rng.NextDouble() < 0.5
                ? new Color(gray * 1.05f, gray * 0.96f, gray * 0.9f)
                : new Color(gray * 0.9f, gray * 0.98f, gray * 1.05f);
            float a = on ? (float)rng.NextDouble() * 0.8f : 0f;
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, new Color(c.r, c.g, c.b, a));
        }
        tex.Apply();
        return tex;
    }
}
