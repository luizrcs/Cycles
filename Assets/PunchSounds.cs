using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PunchSounds : MonoBehaviour
{
    public AudioClip[] Sounds;
    private int index;

    public AudioSource PunchLong;
    public AudioSource Punch;

    public void PlayPunchLong()
    {
        PunchLong.Play();
    }

    public void PlayPunchShort()
    {
        Punch.clip = Sounds[index % 2];
        index++;

        Punch.Play();
    }
}
