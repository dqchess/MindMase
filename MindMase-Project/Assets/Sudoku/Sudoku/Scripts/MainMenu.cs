using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public GameObject pauseDialoag;
    [Space(10)]
    public Text timeLable;
    public Text modeLable;

    [Header("NewGameMenu")]
    public GameObject newGameBG;
    public GameObject newGameMenu;

    [Header("Setting Menu")]
    public Toggle soundToggle;
    public Toggle duplicateToggle;
    public Toggle identicalNumToggle;
    public Toggle autoToggle;

    public static MainMenu intance;
    void Awake()
    {
        intance = this;
    }

    void Start()
    {
        SetupToggles();
        StartCoroutine(UpdateTime());
        UpdateModeUI();
    }

    IEnumerator UpdateTime()
    {
        while (true)
        {
            if (!SudokuGameManager.IsGamePause)
            {
                SudokuGameManager.GameTime += 1f;
                UpdateTimeUI();
            }
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    public void playSound(AudioClip clip)
    {
        SudokuGameManager.PlaySound(clip);
    }

    public void UpdateTimeUI()
    {
        timeLable.text = SudokuGameManager.GetTimeString(SudokuGameManager.GameTime);
    }

    public void UpdateModeUI()
    {
        modeLable.text = SudokuGameManager.GameMode.ToString();
    }

    public void OpenNewMenu()
    {
        newGameBG.SetActive(true);
        LeanTween.moveLocalY(newGameMenu, -1280 / 2f, .2f);
    }

    public void CloseNewMenu()
    {
        newGameBG.SetActive(false);
        LeanTween.moveLocalY(newGameMenu, -1400, .2f);
    }

    public void PauseGame()
    {
        SudokuGameManager.IsGamePause = true;
        pauseDialoag.SetActive(true);
        SudokuManager.intance.UpdateAllBlockUI();
    }

    public void ResumeGame()
    {
        SudokuGameManager.IsGamePause = false;
        pauseDialoag.SetActive(false);
        SudokuManager.intance.UpdateAllBlockUI();
    }

    public void StartEasyGame()
    {
        SudokuGameManager.GameMode = SudokuGameManager.GameModeType.Easy;
        SudokuManager.intance.LoadNewGame();
    }

    public void StartMediumGame()
    {
        SudokuGameManager.GameMode = SudokuGameManager.GameModeType.Medium;
        SudokuManager.intance.LoadNewGame();
    }

    public void StartHardGame()
    {
        SudokuGameManager.GameMode = SudokuGameManager.GameModeType.Hard;
        SudokuManager.intance.LoadNewGame();
    }

    public void StartExpertGame()
    {
        SudokuGameManager.GameMode = SudokuGameManager.GameModeType.Expert;
        SudokuManager.intance.LoadNewGame();
    }

    public void RestartGame()
    {
        SudokuManager.intance.RestartGame();
    }

    void SetupToggles()
    {
        soundToggle.onValueChanged.AddListener((arg0) => SudokuGameManager.IsSound = arg0);
        soundToggle.isOn = SudokuGameManager.IsSound;

        duplicateToggle.onValueChanged.AddListener((arg0) => SudokuGameManager.HighlightDuplicate = arg0);
        duplicateToggle.isOn = SudokuGameManager.HighlightDuplicate;

        identicalNumToggle.onValueChanged.AddListener((arg0) => SudokuGameManager.HighlightIdenticalNumbers = arg0);
        identicalNumToggle.isOn = SudokuGameManager.HighlightIdenticalNumbers;

        autoToggle.onValueChanged.AddListener((arg0) => SudokuGameManager.AutoRemoveNotes = arg0);
        autoToggle.isOn = SudokuGameManager.AutoRemoveNotes;
    }
}
