using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectivesController : MonoBehaviour
{
    public GameLogic GameLogic;

    public GameObject[] CheckMarks;

    private bool[] collectibles = new bool[3];

    public void Collect(int id)
    {
        collectibles[id] = true;
        if (CheckCollectibles()) GameLogic.WinGame();

        StartCoroutine(EnableCheckMark(id));
    }

    private bool CheckCollectibles()
    {
        foreach (bool b in collectibles) if (!b) return false;
        return true;
    }

    private IEnumerator EnableCheckMark(int id)
    {
        for (float f = 0f; f < 1f; f += 0.05f)
        {
            Image checkMark = CheckMarks[id].GetComponent<Image>();
            Color color = checkMark.color;
            color.a = f;
            checkMark.color = color;

            yield return new WaitForSeconds(0.01f);
        }
    }
}
