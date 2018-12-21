using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RecordPacmanTime {

    public static float GameTimeRecorded
    {
        get
        {
            return PlayerPrefs.GetFloat("GameTimeRecorded", 0f);
        }
        set
        {
            PlayerPrefs.SetFloat("GameTimeRecorded", value);
        }
    }

    public static float GameTimeSession
    {
        get
        {
            return PlayerPrefs.GetFloat("GameTimeSession", 0f);
        }
        set
        {
            PlayerPrefs.SetFloat("GameTimeSession", value);
        }
    }
}
