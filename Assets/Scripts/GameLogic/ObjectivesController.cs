using System.Collections;
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
        if (CheckCollectibles()) GameLogic.FinalObjective();

        StartCoroutine(EnableCheckMark(id));
    }

    private bool CheckCollectibles()
    {
        foreach (bool b in collectibles) if (!b) return false;
        return true;
    }

    private IEnumerator EnableCheckMark(int id)
    {
        Image checkMark = CheckMarks[id].GetComponent<Image>();
        yield return Fades.Graphic(checkMark, 0f, 1f, 0.2f);
    }
}
