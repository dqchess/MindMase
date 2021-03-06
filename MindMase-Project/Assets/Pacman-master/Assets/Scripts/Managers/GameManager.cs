﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour {

    //--------------------------------------------------------
    // Game variables

    public static int Level = 0;
    public static int lives = 3;

    public enum GameState { Init, Game, Dead, Scores }
    public static GameState gameState;

    private GameObject pacman;
    private GameObject blinky;
    private GameObject pinky;
    private GameObject inky;
    private GameObject clyde;
    private GameGUINavigation gui;
    private string direction;

    public static bool scared;
    static public int score;

    public float scareLength;
    private float _timeToCalm;

    public float SpeedPerLevel;


    //-----------------------------------
    //Time
    public GameTimeRecord loadedSavedTime;

    //-------------------------------------------------------------------
    // singleton implementation
    private static GameManager _instance;

    public static GameManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<GameManager>();
                DontDestroyOnLoad(_instance.gameObject);
            }

            return _instance;
        }
    }

    //-------------------------------------------------------------------
    // function definitions

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            if (this != _instance)
                Destroy(this.gameObject);
        }

        AssignGhosts();
    }

    void Start()
    {
        gameState = GameState.Init;
        UpdateTime();

        if (File.Exists(Application.dataPath + "/recordingData.txt"))
        {
            string savedData = File.ReadAllText(Application.dataPath + "/recordingData.txt");
            loadedSavedTime = JsonUtility.FromJson<GameTimeRecord>(savedData);
            GameTimeRecord.GameTimeRecorded = loadedSavedTime.pacmanTimePlayed;
            Debug.Log("Load recorded time : " + RecordPacmanTime.GameTimeRecorded);
            Debug.Log("loadedSavedTime : " + loadedSavedTime.pacmanTimePlayed);
        }
        else
        {
            loadedSavedTime = new GameTimeRecord()
            {
                crosswordTimePlayed = 0,
                sudokuTimePlayed = 0,
                pacmanTimePlayed = 0
            };
        }
    }

    IEnumerator UpdateTime()
    {
        while (true)
        {
            if (gameState == GameState.Init)
            {
                RecordPacmanTime.GameTimeRecorded += 1f;
            }
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    void OnLevelWasLoaded()
    {
        if (Level == 0) lives = 3;

        Debug.Log("Level " + Level + " Loaded!");
        AssignGhosts();
        ResetVariables();


        // Adjust Ghost variables!
        clyde.GetComponent<GhostMove>().speed += Level * SpeedPerLevel;
        blinky.GetComponent<GhostMove>().speed += Level * SpeedPerLevel;
        pinky.GetComponent<GhostMove>().speed += Level * SpeedPerLevel;
        inky.GetComponent<GhostMove>().speed += Level * SpeedPerLevel;
        pacman.GetComponent<PlayerController>().speed += Level * SpeedPerLevel / 2;

    }

    private void ResetVariables()
    {
        _timeToCalm = 0.0f;
        scared = false;
        PlayerController.killstreak = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (scared && _timeToCalm <= Time.time)
            CalmGhosts();
        pacman.GetComponent<PlayerController>().ReadInputAndMove(this.direction);
    }

    public void ResetScene()
    {
        CalmGhosts();

        pacman.transform.position = new Vector3(15f, 11f, 0f);
        blinky.transform.position = new Vector3(15f, 20f, 0f);
        pinky.transform.position = new Vector3(14.5f, 17f, 0f);
        inky.transform.position = new Vector3(16.5f, 17f, 0f);
        clyde.transform.position = new Vector3(12.5f, 17f, 0f);

        pacman.GetComponent<PlayerController>().ResetDestination();
        blinky.GetComponent<GhostMove>().InitializeGhost();
        pinky.GetComponent<GhostMove>().InitializeGhost();
        inky.GetComponent<GhostMove>().InitializeGhost();
        clyde.GetComponent<GhostMove>().InitializeGhost();

        gameState = GameState.Init;
        gui.H_ShowReadyScreen();

    }

    public void ToggleScare()
    {
        if (!scared) ScareGhosts();
        else CalmGhosts();
    }

    public void ScareGhosts()
    {
        scared = true;
        blinky.GetComponent<GhostMove>().Frighten();
        pinky.GetComponent<GhostMove>().Frighten();
        inky.GetComponent<GhostMove>().Frighten();
        clyde.GetComponent<GhostMove>().Frighten();
        _timeToCalm = Time.time + scareLength;

        Debug.Log("Ghosts Scared");
    }

    public void CalmGhosts()
    {
        scared = false;
        blinky.GetComponent<GhostMove>().Calm();
        pinky.GetComponent<GhostMove>().Calm();
        inky.GetComponent<GhostMove>().Calm();
        clyde.GetComponent<GhostMove>().Calm();
        PlayerController.killstreak = 0;
    }

    void AssignGhosts()
    {
        // find and assign ghosts
        clyde = GameObject.Find("clyde");
        pinky = GameObject.Find("pinky");
        inky = GameObject.Find("inky");
        blinky = GameObject.Find("blinky");
        pacman = GameObject.Find("pacman");

        if (clyde == null || pinky == null || inky == null || blinky == null) Debug.Log("One of ghosts are NULL");
        if (pacman == null) Debug.Log("Pacman is NULL");

        gui = GameObject.FindObjectOfType<GameGUINavigation>();

        if (gui == null) Debug.Log("GUI Handle Null!");

    }

    public void LoseLife()
    {
        lives--;
        gameState = GameState.Dead;

        // update UI too
        UIScript ui = GameObject.FindObjectOfType<UIScript>();
        Destroy(ui.lives[ui.lives.Count - 1]);
        ui.lives.RemoveAt(ui.lives.Count - 1);
    }

    public static void DestroySelf()
    {

        score = 0;
        Level = 0;
        lives = 3;
        Destroy(GameObject.Find("Game Manager"));
    }

    public void ChangeDirection(string direction)
    {
        this.direction = direction;
    }

    //TODO Add comments
    public void OnApplicationQuit()
    {
        Debug.Log("Recording information before savetime: " + GameTimeRecord.GameTimeRecorded);
        SaveTimeRecord();

    }

    private void SaveTimeRecord()
    {

        GameTimeRecord rec = new GameTimeRecord()
        {
            sudokuTimePlayed = loadedSavedTime.sudokuTimePlayed,
            crosswordTimePlayed = loadedSavedTime.crosswordTimePlayed,
            pacmanTimePlayed = GameTimeRecord.GameTimeRecorded
        };

        string gameRecordedTime = JsonUtility.ToJson(rec);
        File.WriteAllText(Application.dataPath + "/recordingData.txt", gameRecordedTime);
        Debug.Log("Data saved");

        GameTimeRecord.GameTimeRecorded = 0;
    }
}
