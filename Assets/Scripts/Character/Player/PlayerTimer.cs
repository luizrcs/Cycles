using UnityEngine;

public class PlayerTimer : MonoBehaviour
{
    private bool active = true;

    public GameLogic GameLogic;

    // Counted from scene load, not application start: the old absolute Time.time
    // check made the paradox fire instantly on any second playthrough.
    private float elapsed;

    private const float MaxTime = 240f;

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
