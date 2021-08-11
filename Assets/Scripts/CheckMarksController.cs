using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CheckMarksController : MonoBehaviour
{
    public GameObject[] CheckMarks;

    void Start()
    {
        StartCoroutine(TestCheckMarks());
    }

    IEnumerator Enable(int id)
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

    IEnumerator TestCheckMarks()
    {
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(3);
            StartCoroutine(Enable(i));
        }
    }
}
