using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTimer : MonoBehaviour
{
    private bool active = true;

    public GameLogic GameLogic;

    private float maxTime = 240f;

    void Update()
    {
        if (active && Time.time > maxTime)
        {
            active = false;
            GameLogic.TimeUp();
        }
    }
}
