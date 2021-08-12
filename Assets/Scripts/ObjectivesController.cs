using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectivesController : MonoBehaviour
{
    public GameObject[] CheckMarks;

    public void Collect(int id)
    {
        StartCoroutine(EnableCheckMark(id));
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
