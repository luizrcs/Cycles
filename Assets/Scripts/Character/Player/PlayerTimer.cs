using UnityEngine;

public class PlayerTimer : MonoBehaviour
{
    private bool active = true;

    public GameLogic GameLogic;

    // Counted from scene load, not application start: the old absolute Time.time
    // check made the paradox fire instantly on any second playthrough.
    private float elapsed;

    // Read by ParadoxBleed to time the endgame glitch on the player's body.
    public float Elapsed => elapsed;
    public const float MaxTime = 240f;

    void Update()
    {
        if (!active) return;

        elapsed += Time.deltaTime;
        if (elapsed > MaxTime)
        {
            active = false;
            GameLogic.TimeUp();
        }
    }
}
