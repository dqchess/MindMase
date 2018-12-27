using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTimeRecord
{

    public float sudokuTimePlayed = 0;
    public float crosswordTimePlayed = 0;
    public float pacmanTimePlayed = 0;

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

    public static float CrosswordTimeRecorded
    {

        get
        {
            return PlayerPrefs.GetFloat("CrosswordTimeRecorded", 0f);
        }
        set
        {
            PlayerPrefs.SetFloat("CrosswordTimeRecorded", value);
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
