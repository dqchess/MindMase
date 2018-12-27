using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;

public class CrosswordController : MonoBehaviour
{
    #region Classes

    [System.Serializable]
	public class SectionInfo
	{
		public string			displayName;	// Display text that appears in the sections header
		public string			uniqueId;		// Unique id used in code to identify this section
		public bool				iapLocked;		// If true then this section will be locked and can only be unlocked by purchasing it in the iap store
		public string			iapProductId;	// The product id which must be purchased for the section to be unlocked
		public List<TextAsset>	crosswordFiles;	// List of corssword files for this section
	}

	[System.Serializable]
	public class CrosswordIconSettings
	{
		public int		preferredSize	= 100;
		public Color	normalCellColor	= Color.white;
		public Color	blockCellColor	= Color.black;
		public bool		includeBorder	= true;
		public int		borderLineSize	= 2;
		public Color	borderLineColor	= Color.black;
		public bool		includeGridLine	= true;
		public int		gridLineSize	= 2;
		public Color	gridLineColor	= Color.grey;
	}

	public class Crossword
	{
		public string						sectionUniqueId;
		public int							levelIndex;
		public string						name;
		public int							size;
		public List<List<CrosswordCell>>	cells;
		public List<CrosswordClue>			acrossClues;
		public List<CrosswordClue>			downClues;

		public bool 	showIncorrect;
		public float	timer;
		public bool		completed;
	}

	public class CrosswordCell
	{
		public bool	isBlock;			// True if this cell is a black box
		public bool	isNumbered;			// True if this cell is a numbered cell
		public int	number;				// If isNumbered is true then this is the number of the cell
		public char correctCharacter;	// The character that goes in the cell
		public char setCharacter;		// The character that has been typed by the user

		public Pos	thisPos;		// The index position of this cell
		public Pos	acrossStartPos;	// The index position of the starting cell for the across word that this cell is part of
		public Pos	acrossEndPos;	// The index position of the ending cell for the across word that this cell is part of
		public Pos	downStartPos;	// The index position of the starting cell for the down word that this cell is part of
		public Pos	downEndPos;		// The index position of the ending cell for the down word that this cell is part of

	}

	public class CrosswordClue
	{
		public int		number;
		public string	clue;
	}

	public class Pos
	{
		public int rowIndex;
		public int colIndex;

		public Pos(int r, int c) { rowIndex = r; colIndex = c; }
	}

	#endregion

	#region Enums

	public enum Direction
	{
		Across,
		Down
	}

	#endregion

	#region Inspector Variables

	[Space]

	// Main screen
	[SerializeField] private ProgressRing	mainScreenProgressRing;
	[SerializeField] private Button			mainScreenContinueButton;
	[SerializeField] private Popup			storePopup;

	[Space]

	// Sections screen
	[SerializeField] private List<SectionInfo>		sectionInfos;
	[SerializeField] private CrosswordIconSettings	crosswordIconSettings;
	[SerializeField] private SectionUI				sectionUIPrefab;
	[SerializeField] private Transform				sectionUIContainer;
	[SerializeField] private CrosswordButtonUI		crosswordButtonUIPrefab;
	[SerializeField] private RectTransform			crosswordButtonParentPrefab;

	[Space]

	// Crossword setup variables
	[SerializeField] private GridCell			gridCellPrefab;
	[SerializeField] private GridLayoutGroup	gridCellParent;
	[SerializeField] private float				gridCellDesignSize;			// We will use this size to determine how to scale the text in each cell for difference size crosswords
	[SerializeField] private float				minNumberTextScale;			// The numberText is already pretty small and scaling down might make it unreadable
	[SerializeField] private bool				forceUseOnScreenKeyboard;	// If this is true then the on screen keyboard will be used even if touch screen keyboard is not supported
	[SerializeField] private int				numCrosswordsTillAdShown;	// The number of times the player must click a crossword to play it before an interstitial ad is shown

	[Space]

	// Game screen
	[SerializeField] private Text			timerText;
	[SerializeField] private Text			selectedClueText;
	[SerializeField] private RectTransform	selectedClueContainer;
	[SerializeField] private ClueListItemUI	clueListItemUIPrefab;
	[SerializeField] private Transform		acrossClueListParent;
	[SerializeField] private Transform		downClueListParent;
	[SerializeField] private RectTransform	clueListContainer;
	[SerializeField] private RectTransform	clueListContent;
	[SerializeField] private Keyboard		keyboard;
	[SerializeField] private Image			switchDirectionIconImage;
	[SerializeField] private Sprite			switchDirectionDownIcon;
	[SerializeField] private Sprite			switchDirectionAcrossIcon;
	[SerializeField] private Button			showIncorrectButton;
	[SerializeField] private Text			showIncorrectButtonText;
	[SerializeField] private Popup			completedPopup;

	#endregion

	#region Member Variables

	private const float SelectedClueAnimationDuration = 0.5f;

	// ObjectPools for various UI elements
	private ObjectPool	crosswordButtonUIPool;
	private ObjectPool	crosswordButtonParentPool;
	private ObjectPool	gridCellPool;
	private ObjectPool	clueListItemUIPool;

	// List of active UI elements that are currently in use
	private List<SectionUI>			sectionUIs;
	private List<List<GridCell>>	gridCells;
	private List<ClueListItemUI>	clueListItemUIs;

	// Variables for the current active crossword being played
	private Crossword	activeCrossword;
	private float		activeTextScale;
	private Pos			selectedCellPos;
	private Direction	selectedDirection;
	private bool		pauseTimer;

	// Save data
	private int								numCrosswordsStarted;
	private Dictionary<string, Crossword>	savedCrosswords;		// An entry for each crossword that is in progress (Has had words place on it)
	private Dictionary<string, int>			savedCompletedCount;	// How many levels have been completed in each section
	private Dictionary<string, float>		savedCompletedTimes;	// Time taken to complete each of the completed levels
	private string							savedActiveCrosswordKey;

	// Flags for animation
	private bool 	isSectionAnimating;
	private bool 	isSelectedClueShown;
	private bool	isSelectedClueAnimating;
	private bool	isSelectedClueAnimatingIn;
	private float	selectedClueAnimTime;

    // Variables for the played game recording time
    public float updateInterval = 0.5F;
    private double lastInterval;
    private int frames = 0;
    private float fps;

    //todo add comments
    private GameTimeRecord loadedSavedTime;

    #endregion

    #region Properties

    /// <summary>
    /// Full path to the save file on the device
    /// </summary>
    public static string SaveFilePath
	{
		get { return Application.persistentDataPath + "/save_data.json"; }
	}

	/// <summary>
	/// Returns true if we should show the on-screen keyboard when a cell is selected. If false then a physcial keyboard must be used.
	/// </summary>
	public bool UseOnScreenKeyboard
	{
		get { return TouchScreenKeyboard.isSupported || forceUseOnScreenKeyboard; }
	}

	/// <summary>
	/// The CrosswordCell for the currently selected cell
	/// </summary>
	public CrosswordCell SelectedCrosswordCell
	{
		get
		{
			return activeCrossword.cells[selectedCellPos.rowIndex][selectedCellPos.colIndex];
		}
	}

	/// <summary>
	/// The GridCell for the currently selected cell
	/// </summary>
	public GridCell SelectedGridCell
	{
		get
		{
			return gridCells[selectedCellPos.rowIndex][selectedCellPos.colIndex];
		}
	}

	/// <summary>
	/// The CrosswordCell for the start of the currently highlighed cells
	/// </summary>
	public CrosswordCell HighlightedStartCell
	{
		get
		{
			Pos start = (selectedDirection == Direction.Across) ? SelectedCrosswordCell.acrossStartPos : SelectedCrosswordCell.downStartPos;
			return activeCrossword.cells[start.rowIndex][start.colIndex];
		}
	}

	/// <summary>
	/// The CrosswordCell for the end of the currently highlighed cells
	/// </summary>
	public CrosswordCell HighlightedEndCell
	{
		get
		{
			Pos end = (selectedDirection == Direction.Across) ? SelectedCrosswordCell.acrossEndPos : SelectedCrosswordCell.downEndPos;
			return activeCrossword.cells[end.rowIndex][end.colIndex];
		}
	}

	#endregion

	#region Unity Methods

	private void Awake()
	{
		Debug.Log("Save file location: " + SaveFilePath);

		sectionUIs		= new List<SectionUI>();
		gridCells		= new List<List<GridCell>>();
		clueListItemUIs	= new List<ClueListItemUI>();

		Transform poolContainer = new GameObject("pool_container").transform;
		poolContainer.SetParent(transform, false);

		gridCellPool				= new ObjectPool(gridCellPrefab.gameObject, 15 * 15, poolContainer);
		crosswordButtonUIPool		= new ObjectPool(crosswordButtonUIPrefab.gameObject, sectionInfos.Count * 6 + 6, poolContainer);
		crosswordButtonParentPool	= new ObjectPool(crosswordButtonParentPrefab.gameObject, sectionInfos.Count + 1, poolContainer);
		clueListItemUIPool			= new ObjectPool(clueListItemUIPrefab.gameObject, 20, poolContainer);

		numCrosswordsStarted	= 0;
		savedCrosswords			= new Dictionary<string, Crossword>();
		savedCompletedCount		= new Dictionary<string, int>();
		savedCompletedTimes		= new Dictionary<string, float>();

		if (!LoadSave())
		{
			SetSavedActiveCrosswordKey("");
		}
	}

	private void Start()
	{
    //	  lastInterval = Time.realtimeSinceStartup;
	//    frames = 0;

        // TODO 
        StartCoroutine(UpdateTime());

        UpdateMainScreen();

		for (int i = 0; i < sectionInfos.Count; i++)
		{
			SectionInfo	sectionInfo	= sectionInfos[i];
			SectionUI	sectionUI	= Instantiate(sectionUIPrefab);

			sectionUI.transform.SetParent(sectionUIContainer, false);

			// Check if this section is locked
			bool isLocked = sectionInfo.iapLocked && IAPController.IsEnabled && !IAPController.Instance.IsProductPurchased(sectionInfo.iapProductId);

			// Setup the UI
			sectionUI.headerText.text			= sectionInfo.displayName;
			sectionUI.completedText.text		= isLocked ? sectionInfo.crosswordFiles.Count.ToString() : string.Format("{0} / {1}", GetNumCompletedLevels(sectionInfo.uniqueId), sectionInfo.crosswordFiles.Count);
			sectionUI.SectionIndex				= i;
			sectionUI.OnArrowClicked			= OnSectionArrowClicked;
			sectionUI.OnUnlockSectionClicked	= storePopup.Show;
			sectionUI.IndexOffset				= 0;

			// Update the UI if the section is locked
			sectionUI.iapLockedIcon.gameObject.SetActive(isLocked);
			sectionUI.iapLockedObject.SetActive(isLocked);

			sectionUIs.Add(sectionUI);

			sectionUI.CurrentButtonContainer = CreateCrosswordButtons(i);
		}

		// If touch screen keyboard is not supported for the device then we need to turn some stuff off and assume theres a physical keyboard
		if (!UseOnScreenKeyboard)
		{
			keyboard.gameObject.SetActive(false);
			selectedClueContainer.transform.SetAsFirstSibling();
			(selectedClueContainer.transform as RectTransform).anchoredPosition = new Vector2(0, 0);
		}
		else
		{
			keyboard.OnKeyPressed = OnKeyPressed;
		}

		if (IAPController.IsEnabled)
		{
			IAPController.Instance.OnProductPurchased += OnIAPProductPurchased;
		}
	}

    IEnumerator UpdateTime()
    {
        while (true)
        {
            if (activeCrossword != null && !activeCrossword.completed && !pauseTimer)
            {
               RecordCrosswordTime.GameTimeRecorded += 1f;
            }
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    private void Update()
	{
        // Update the timer for the recording
	   /* ++frames;
	    float timeNow = Time.realtimeSinceStartup;
	    if (timeNow > lastInterval + updateInterval)
	    {
	        fps = (float)(frames / (timeNow - lastInterval));
	        frames = 0;
	        lastInterval = timeNow;
	    }
        */

        // Update the timer
        if (activeCrossword != null && !activeCrossword.completed && !pauseTimer)
		{
			activeCrossword.timer += Time.deltaTime;
			timerText.text = CrosswordUtilities.FormateCompleteTime(activeCrossword.timer);
		    UpdateTime();

		}

		if (isSelectedClueAnimating)
		{
			selectedClueAnimTime -= Time.deltaTime;

			float fromY = 0;
			float toY	= 0;

			// Get the from and to values base on weither we are animating the on screen keyboard or to clues list
			if (UseOnScreenKeyboard)
			{
				fromY	= isSelectedClueAnimatingIn ? selectedClueContainer.rect.height : 0f;
				toY		= isSelectedClueAnimatingIn ? 0f : selectedClueContainer.rect.height;
			}
			else
			{
				fromY	= isSelectedClueAnimatingIn ? 0f : selectedClueContainer.rect.height;
				toY		= isSelectedClueAnimatingIn ? selectedClueContainer.rect.height : 0f;
			}

			// Check if the animation time is up
			if (selectedClueAnimTime <= 0)
			{
				isSelectedClueAnimating = false;

				// Set the animated object in its final position
				if (UseOnScreenKeyboard)
				{
					(selectedClueContainer.transform as RectTransform).anchoredPosition = new Vector2(0, -toY);
				}
				else
				{
					clueListContainer.offsetMax	= new Vector2(clueListContainer.offsetMax.x, -toY);
				}
			}
			else
			{
				float t = Utilities.EaseOut((SelectedClueAnimationDuration - selectedClueAnimTime) / SelectedClueAnimationDuration);
				float y = Mathf.Lerp(fromY, toY, t);

				if (UseOnScreenKeyboard)
				{
					// If we are using the on screen keyboard then animate the keybaord up from the bottom of the
					// screen with the selected clue container on top of the keybaord
					(selectedClueContainer.transform as RectTransform).anchoredPosition = new Vector2(0, -y);
				}
				else
				{
					// If we are not using the on screen keybaord animate the clue list down to show the clue container
					// that as been positioned behind it
					clueListContainer.offsetMax = new Vector2(clueListContainer.offsetMax.x, -y);
				}
			}
		}

		// Process keyboard input if there is a selected cell
		if (selectedCellPos != null)
		{
			// Process any input from the touch screen keyboard if it's supported
			if (!UseOnScreenKeyboard)
			{
				// Now process any input from a physical keyboard
				Event	evt				= new Event();
				string	validCharacters = "";

				while (Event.PopEvent(evt))
				{
					if (evt.rawType == EventType.KeyDown)
					{
						// Check if the keycode is one of the arrow keys and if so set the selected direction
						if (evt.keyCode == KeyCode.UpArrow || evt.keyCode == KeyCode.DownArrow)
						{
							if (selectedDirection != Direction.Down)
							{
								SetSelectedDirection(Direction.Down);
							}
							else if (evt.keyCode == KeyCode.DownArrow)
							{
								MoveSelectedCellForward();
							}
							else
							{
								MoveSelectedCellBack();
							}
						}
						else if (evt.keyCode == KeyCode.LeftArrow || evt.keyCode == KeyCode.RightArrow)
						{
							if (selectedDirection != Direction.Across)
							{
								SetSelectedDirection(Direction.Across);
							}
							else if (evt.keyCode == KeyCode.RightArrow)
							{
								MoveSelectedCellForward();
							}
							else
							{
								MoveSelectedCellBack();
							}
						}
						else if (evt.keyCode == KeyCode.Backspace)
						{
							DeleteCharacter();
						}
						else
						{
							// Else check if the character is a valid character
							char character = evt.character;

							if (IsValidCharacter(character))
							{
								validCharacters += character;
							}
						}
					}
				}

				if (validCharacters.Length > 0)
				{
					SetCharacters(validCharacters);
				}
			}
		}
	}

	private void OnApplicationPause(bool paused)
	{
		if (paused)
		{
			Save();
		}
	}

	private void OnDestroy()
	{
		if (IAPController.IsEnabled)
		{
			IAPController.Instance.OnProductPurchased -= OnIAPProductPurchased;
		}

		Save();
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// Called when the continue button on the main screen is clicked, starts the last active crossword
	/// </summary>
	public void OnContinueButtonClicked()
	{
		if (!string.IsNullOrEmpty(savedActiveCrosswordKey) && savedCrosswords.ContainsKey(savedActiveCrosswordKey))
		{
			// Set the active crossword
			SetActiveCrossword(savedCrosswords[savedActiveCrosswordKey]);
		}
	}

	/// <summary>
	/// Called when the left or right arrow for the selected clue is clicked, moves to the next or previous clue and selects the cells for it.
	/// </summary>
	/// <param name="arrow">Arrow.</param>
	public void OnSelectedClueArrowClicked(string arrow)
	{
		int clueNumber = HighlightedStartCell.number;

		List<CrosswordClue> cluesToSearch = (selectedDirection == Direction.Across) ? activeCrossword.acrossClues : activeCrossword.downClues;

		int			nextClueNumber	= 0;
		Direction	nextDirection	= selectedDirection;

		for (int i = 0; i < cluesToSearch.Count; i++)
		{
			if (clueNumber == cluesToSearch[i].number)
			{
				switch (arrow)
				{
				case "left":
					if (selectedDirection == Direction.Across)
					{
						if (i > 0)
						{
							nextClueNumber = activeCrossword.acrossClues[i - 1].number;
						}
					}
					else if (selectedDirection == Direction.Down)
					{
						if (i == 0)
						{
							nextClueNumber	= activeCrossword.acrossClues[activeCrossword.acrossClues.Count - 1].number;
							nextDirection	= Direction.Across;
						}
						else
						{
							nextClueNumber = activeCrossword.downClues[i - 1].number;
						}
					}
					break;
				case "right":
					if (selectedDirection == Direction.Across)
					{
						if (i == activeCrossword.acrossClues.Count - 1)
						{
							nextClueNumber	= activeCrossword.downClues[0].number;
							nextDirection	= Direction.Down;
						}
						else
						{
							nextClueNumber = activeCrossword.acrossClues[i + 1].number;
						}
					}
					else if (selectedDirection == Direction.Down)
					{
						if (i < activeCrossword.downClues.Count - 1)
						{
							nextClueNumber = activeCrossword.downClues[i + 1].number;
						}
					}
					break;
				}
			}
		}

		if (nextClueNumber != 0)
		{
			for (int i = 0; i < activeCrossword.cells.Count; i++)
			{
				for (int j = 0; j < activeCrossword.cells[i].Count; j++)
				{
					CrosswordCell crosswordCell = activeCrossword.cells[i][j];

					if (crosswordCell.isNumbered && nextClueNumber == crosswordCell.number)
					{
						SetSelectedCell(crosswordCell.thisPos);
						SetSelectedDirection(nextDirection);

						return;
					}
				}
			}
		}
	}

	/// <summary>
	/// Toggles the selected direction
	/// </summary>
	public void OnSwitchDirectionsButtonClicked()
	{
		SetSelectedDirection((selectedDirection == Direction.Across) ? Direction.Down : Direction.Across);
	}

	/// <summary>
	/// Toggles to showing or incorrect characters
	/// </summary>
	public void OnShowIncorrectButtonClicked()
	{
		// If the player has purchased the show incorrect ability then toggle the flag that turns it on/off;
		activeCrossword.showIncorrect = !activeCrossword.showIncorrect;

		for (int i = 0; i < gridCells.Count; i++)
		{
			for (int j = 0; j < gridCells[i].Count; j++)
			{
				GridCell		gridCell		= gridCells[i][j];
				CrosswordCell	crosswordCell	= activeCrossword.cells[i][j];

				gridCell.SetIncorrect(activeCrossword.showIncorrect && crosswordCell.setCharacter != crosswordCell.correctCharacter);
			}
		}

		UpdateControlButtons();
	}

	/// <summary>
	/// Clears the currently selected cells
	/// </summary>
	public void OnClearButtonClicked()
	{
		Pos startPos	= HighlightedStartCell.thisPos;
		Pos endPos		= HighlightedEndCell.thisPos;

		int iInc = (selectedDirection == Direction.Across) ? 0 : 1;
		int jInc = (selectedDirection == Direction.Across) ? 1 : 0;

		// Set all the highlighed cells to blank
		for (int i = startPos.rowIndex, j = startPos.colIndex; i <= endPos.rowIndex && j <= endPos.colIndex; i += iInc, j += jInc)
		{
			activeCrossword.cells[i][j].setCharacter	= (char)0;
			gridCells[i][j].characterText.text			= "";
		}
	}

	/// <summary>
	/// Called when a back button on the game screen is clicked
	/// </summary>
	public void OnGameScreenBackButtonClicked()
	{
		DeselectSelectedCell();

		activeCrossword = null;

		completedPopup.Hide();

		UIScreenController.Instance.Show(UIScreenController.SectionsScreenId, true);
	}

	/// <summary>
	/// Called when the next button on the complete popup is called, starts the next level
	/// </summary>
	public void OnCompletedNextButtonClicked()
	{
		string	sectionId	= activeCrossword.sectionUniqueId;
		int		levelIndex	= activeCrossword.levelIndex;

		SectionInfo nextSection		= null;
		int			nextLevelIndex	= 0;

		// Get the crossword file for the next level
		for (int i = 0; i < sectionInfos.Count; i++)
		{
			SectionInfo sectionInfo = sectionInfos[i];

			// Find the section info for the level that was just completed
			if (sectionId == sectionInfo.uniqueId)
			{
				// Check if it was the last level in the section, is so then go to the next section
				if (levelIndex + 1 >= sectionInfo.crosswordFiles.Count)
				{
					nextLevelIndex = 0;

					// Check if it was the last section, is so just go back to the very first section / level
					if (i + 1 >= sectionInfo.crosswordFiles.Count)
					{
						nextSection = sectionInfos[0];
					}
					else
					{
						nextSection = sectionInfos[i + 1];
					}
				}
				else
				{
					nextSection		= sectionInfo;
					nextLevelIndex	= levelIndex + 1;
				}

				break;
			}
		}

		SetActiveCrossword(ParseCrosswordFile(nextSection, nextLevelIndex));
	}

	/// <summary>
	/// Debug method, completes the current active puzzle
	/// </summary>
	public void OnDebugCompleteActivePuzzle()
	{
		for (int i = 0; i < activeCrossword.cells.Count; i++)
		{
			for (int j = 0; j < activeCrossword.cells[i].Count; j++)
			{
				activeCrossword.cells[i][j].setCharacter = activeCrossword.cells[i][j].correctCharacter;
			}
		}

		DeselectSelectedCell();
		CreateGridCells();
		CreateClueList();
		UpdateControlButtons();

		CheckBoardCompleted();
	}

	/// <summary>
	/// Debug method, unlocks all levels in every section
	/// </summary>
	public void OnDebugUnlockAllLevels()
	{
		// Set the completed count for all sections to the number of levels in the section, this will unlock all levels
		for (int i = 0; i < sectionInfos.Count; i++)
		{
			SectionInfo sectionInfo = sectionInfos[i];

			savedCompletedCount[sectionInfo.uniqueId] = sectionInfo.crosswordFiles.Count;

			UpdateSectionCrosswordButtons(sectionInfo.uniqueId);
		}
	}

	#endregion

	#region Private Methods

	/// <summary>
	/// Called when a key on the on-screen keyboard is clicked.
	/// </summary>
	private void OnKeyPressed(Keyboard.KeyId keyId, char character)
	{
		switch (keyId)
		{
		case Keyboard.KeyId.Character:
			SetCharacters(character.ToString());
			break;
		case Keyboard.KeyId.Backspace:
			DeleteCharacter();
			break;
		case Keyboard.KeyId.CloseKeyboard:
			DeselectSelectedCell();
			break;
		}
	}

	/// <summary>
	/// Called when a sections left or right arrow is clicked, shows the next or previous 6 crossword level buttons.
	/// </summary>
	private void OnSectionArrowClicked(int sectionIndex, string arrow)
	{
		if (isSectionAnimating)
		{
			return;
		}

		SectionUI	sectionUI = sectionUIs[sectionIndex];
		int			newOffset = 0;

		switch (arrow)
		{
		case "left":
			newOffset = Mathf.Max(0, sectionUI.IndexOffset - 1);
			break;
		case "right":
			newOffset = Mathf.Min(Mathf.FloorToInt(sectionInfos[sectionIndex].crosswordFiles.Count / 6), sectionUI.IndexOffset + 1);
			break;
		}

		if (newOffset != sectionUI.IndexOffset)
		{
			sectionUI.IndexOffset = newOffset;

			float width		= sectionUI.CurrentButtonContainer.rect.width;
			float tween1X	= (arrow == "right") ?  -width - 100f : width + 100f;
			float tween2X	= (arrow == "right") ?  width + 100f : -width - 100f;

			// Animate the current button container off screen
			Tween tween1 = Tween.PositionX(sectionUI.CurrentButtonContainer, Tween.TweenStyle.EaseOut, 0f, tween1X, 500f);
			tween1.SetUseRectTransform(true);
			tween1.SetFinishCallback((GameObject obj) => 
			{
				// This will return all the CrosswordButtonUIs back to the pool
				for (int i = obj.transform.childCount - 1; i >= 0; i--)
				{
					ObjectPool.ReturnObjectToPool(obj.transform.GetChild(i).gameObject);
				}

				// Return the crossword button parent back to the pool
				ObjectPool.ReturnObjectToPool(obj);
			});

			// Create 6 new buttons and the container
			sectionUI.CurrentButtonContainer = CreateCrosswordButtons(sectionIndex);

			isSectionAnimating = true;

			Tween tween2 = Tween.PositionX(sectionUI.CurrentButtonContainer, Tween.TweenStyle.EaseOut, tween2X, 0f, 500f);
			tween2.SetUseRectTransform(true);
			tween2.SetFinishCallback((GameObject obj) => 
			{
				isSectionAnimating = false;
			});
		}
	}

	/// <summary>
	/// Creates 6 new CrosswordButtonUIs for the given section index. Returns the new RectTransform that the 6 buttons are children of.
	/// </summary>
	private RectTransform CreateCrosswordButtons(int sectionIndex)
	{
		SectionInfo sectionInfo = sectionInfos[sectionIndex];
		SectionUI	sectionUI	= sectionUIs[sectionIndex];

		RectTransform crosswordButtonParent = crosswordButtonParentPool.GetObject<RectTransform>(sectionUI.crosswordParentMarker);

		int startIndex = sectionUI.IndexOffset * 6;

		for (int i = startIndex, count = 0; i < sectionInfo.crosswordFiles.Count && count < 6; i++, count++)
		{
			string		saveKey			= string.Format("{0}_{1}", sectionInfo.uniqueId, i);
			Crossword	crossword		= savedCrosswords.ContainsKey(saveKey) ? savedCrosswords[saveKey] : ParseCrosswordFile(sectionInfo, i);
			Texture2D	icon			= CrosswordUtilities.CreateCrosswordIcon(crossword, crosswordIconSettings.preferredSize, crosswordIconSettings.normalCellColor, crosswordIconSettings.blockCellColor, crosswordIconSettings.includeBorder, crosswordIconSettings.borderLineSize, crosswordIconSettings.borderLineColor, crosswordIconSettings.includeGridLine, crosswordIconSettings.gridLineSize, crosswordIconSettings.gridLineColor);

			crossword.sectionUniqueId	= sectionInfo.uniqueId;
			crossword.levelIndex		= i;

			CrosswordButtonUI crosswordButtonUI = crosswordButtonUIPool.GetObject<CrosswordButtonUI>(crosswordButtonParent);

			// Destroy the previous icon
			if (crosswordButtonUI.crosswordIcon.texture != null)
			{
				Destroy(crosswordButtonUI.crosswordIcon.texture);
			}

			// Setup the crossword buttons UI and click values
			crosswordButtonUI.crosswordIcon.texture		= icon;
			crosswordButtonUI.levelNumberText.text		= (i + 1).ToString();
			crosswordButtonUI.Crossword					= crossword;
			crosswordButtonUI.OnCrosswordButtonClicked	= SetActiveCrossword;

			// Get the number of completed levels for this section
			int		completedCount	= savedCompletedCount.ContainsKey(sectionInfo.uniqueId) ? savedCompletedCount[sectionInfo.uniqueId] : 0;
			bool	isLocked		= i + 1 > completedCount + 6;

			// Set the crossword locked if its level is passed the completed count + 6
			crosswordButtonUI.lockedIndicator.SetActive(isLocked);
			crosswordButtonUI.crosswordButton.interactable = !isLocked;

			// Check if this level is completed and if so show the completed time
			float completeTime = savedCompletedTimes.ContainsKey(saveKey) ? savedCompletedTimes[saveKey] : 0f;

			crosswordButtonUI.completeTimeContainer.SetActive(completeTime > 0);
			crosswordButtonUI.completeTimeText.text = CrosswordUtilities.FormateCompleteTime(completeTime);
		}

		return crosswordButtonParent;
	}

	/// <summary>
	/// Re-creates the given setions crosswords buttons
	/// </summary>
	private void UpdateSectionCrosswordButtons(string sectionId)
	{
		if (sectionUIs == null || sectionUIs.Count <= 0)
		{
			return;
		}

		for (int i = 0; i < sectionInfos.Count; i++)
		{
			SectionInfo sectionInfo = sectionInfos[i];

			if (sectionId == sectionInfo.uniqueId)
			{
				// Section UI will be the same index as the section info
				SectionUI sectionUI = sectionUIs[i];

				// First return the currect crossword buttons to their pool
				for (int j = sectionUI.CurrentButtonContainer.transform.childCount - 1; j >= 0; j--)
				{
					ObjectPool.ReturnObjectToPool(sectionUI.CurrentButtonContainer.transform.GetChild(j).gameObject);
				}

				ObjectPool.ReturnObjectToPool(sectionUI.CurrentButtonContainer.gameObject);

				// Re-Create the crossword buttons
				sectionUI.CurrentButtonContainer = CreateCrosswordButtons(i);

				// Set it in place
				sectionUI.CurrentButtonContainer.anchoredPosition = Vector2.zero;

				// Update the number of completed levels
				sectionUI.completedText.text = string.Format("{0} / {1}", GetNumCompletedLevels(sectionInfo.uniqueId), sectionInfo.crosswordFiles.Count);

				break;
			}
		}
	}

	/// <summary>
	/// Sets the given crossword as the new active crossword. This will setup the game UI using the Crossword and show the game screen.
	/// </summary>
	private void SetActiveCrossword(Crossword crossword)
	{
		activeCrossword = crossword;
		pauseTimer		= false;

		// Set the key for the last active crossword so if the game is closed then opened again we can continue the last crossword
		SetSavedActiveCrosswordKey(string.Format("{0}_{1}", activeCrossword.sectionUniqueId, activeCrossword.levelIndex));

		float width		= (gridCellParent.transform as RectTransform).rect.width;
		float height	= (gridCellParent.transform as RectTransform).rect.height;

		float size			= Mathf.Min(width, height);
		float totalSpacing	= gridCellParent.spacing.x * (float)(activeCrossword.size - 1);
		float cellSize		= (size - totalSpacing) / (float)activeCrossword.size;

		activeTextScale = cellSize / gridCellDesignSize;

		gridCellParent.cellSize			= new Vector2(cellSize, cellSize);
		gridCellParent.constraint		= GridLayoutGroup.Constraint.FixedColumnCount;
		gridCellParent.constraintCount	= activeCrossword.size;

		selectedCellPos		= null;
		selectedDirection	= Direction.Across;

		CreateGridCells();
		CreateClueList();
		UpdateControlButtons();

		clueListContent.anchoredPosition = Vector2.zero;

		completedPopup.Hide();

		// Set the crossword in the save data
		string saveKey = string.Format("{0}_{1}", activeCrossword.sectionUniqueId, activeCrossword.levelIndex);

		if (!savedCrosswords.ContainsKey(saveKey))
		{
			savedCrosswords[saveKey] = activeCrossword;
		}

		UIScreenController.Instance.Show(UIScreenController.GameScreenId);

		numCrosswordsStarted++;

		// Check if its time to show an interstitial ad
		if (numCrosswordsStarted >= numCrosswordsTillAdShown)
		{
			numCrosswordsStarted = 0;

			if (AdsController.Exists())
			{
				// Show an interstital ad and only pause the timer if an ad actually shows, when the interstitial is closed un-pause the timer
				pauseTimer = AdsController.Instance.ShowInterstitialAd(() => { pauseTimer = false; } );
			}
		}
	}

	/// <summary>
	/// Creates the active crosswords GridCells.
	/// </summary>
	private void CreateGridCells()
	{
		gridCellPool.ReturnAllObjectsToPool();
		gridCells.Clear();

		float numberTextScale = Mathf.Max(minNumberTextScale, activeTextScale);

		for (int i = 0; i < activeCrossword.cells.Count; i++)
		{
			List<CrosswordCell> rowCells = activeCrossword.cells[i];

			gridCells.Add(new List<GridCell>());

			for (int j = 0; j < rowCells.Count; j++)
			{
				CrosswordCell	crosswordCell	= rowCells[j];
				GridCell		gridCell		= gridCellPool.GetObject<GridCell>(gridCellParent.transform);

				gridCell.blockObj.SetActive(crosswordCell.isBlock);
				gridCell.characterText.text			= crosswordCell.isBlock ? "" : crosswordCell.setCharacter.ToString().ToUpper();
				gridCell.numberText.text			= crosswordCell.isNumbered ? crosswordCell.number.ToString() : "";
				gridCell.cellButton.interactable	= !crosswordCell.isBlock;
				
				gridCell.SetSelected(false);
				gridCell.SetHighlighted(false);
				gridCell.SetIncorrect(false);

				gridCell.characterText.transform.localScale	= new Vector3(activeTextScale, activeTextScale, 1f);
				gridCell.numberText.transform.localScale	= new Vector3(numberTextScale, numberTextScale, 1f);

				gridCell.CellPos		= crosswordCell.thisPos;
				gridCell.OnCellClicked	= SetSelectedCell;

				gridCells[i].Add(gridCell);
			}
		}
	}

	/// <summary>
	/// Creates the active crosswords clue list
	/// </summary>
	private void CreateClueList()
	{
		clueListItemUIPool.ReturnAllObjectsToPool();

		if (activeCrossword != null)
		{
			for (int i = 0; i < activeCrossword.acrossClues.Count; i++)
			{
				clueListItemUIs.Add(CreateClueListItemUI(activeCrossword.acrossClues[i], true));
			}

			for (int i = 0; i < activeCrossword.downClues.Count; i++)
			{
				clueListItemUIs.Add(CreateClueListItemUI(activeCrossword.downClues[i], false));
			}
		}
	}

	/// <summary>
	/// Creates a single 
	/// </summary>
	private ClueListItemUI CreateClueListItemUI(CrosswordClue crosswordClue, bool isAcrossClue)
	{
		ClueListItemUI clueListItemUI	= clueListItemUIPool.GetObject<ClueListItemUI>(isAcrossClue ? acrossClueListParent : downClueListParent);
		clueListItemUI.numberText.text	= crosswordClue.number.ToString();
		clueListItemUI.clueText.text	= crosswordClue.clue;

		clueListItemUI.ClueNumber		= crosswordClue.number;
		clueListItemUI.IsAcrossClue 	= isAcrossClue;
		clueListItemUI.OnClueSelected	= OnClueSelected;

		clueListItemUI.numberText.color			= clueListItemUI.normalTextColor;
		clueListItemUI.clueText.color			= clueListItemUI.normalTextColor;
		clueListItemUI.backgroundImage.color	= clueListItemUI.normalBkgColor;

		return clueListItemUI;
	}

	/// <summary>
	/// Updates the UI that shows the clue for the selected cell.
	/// </summary>
	private void UpdateSelectedClue()
	{
		CrosswordClue crosswordClue = null;

		if (selectedCellPos != null)
		{
			List<CrosswordClue> crosswordClues = null;

			// Get the list of clues to search based on what the selected direction is
			switch (selectedDirection)
			{
			case Direction.Across:
				crosswordClues = activeCrossword.acrossClues;
				break;
			case Direction.Down:
				crosswordClues = activeCrossword.downClues;
				break;
			}

			// The clue number will always be the starting highlighted cell
			int number = HighlightedStartCell.number;

			// Find the clue with the same number
			for (int i = 0; i < crosswordClues.Count; i++)
			{
				if (number == crosswordClues[i].number)
				{
					crosswordClue = crosswordClues[i];
				}
			}
		}

		selectedClueText.text = (crosswordClue == null) ? "" : crosswordClue.clue;
	}

	/// <summary>
	/// Called when a clue in the list of clues is clicked
	/// </summary>
	private void OnClueSelected(int clueNumber, bool isAcrossClue)
	{
		for (int i = 0; i < activeCrossword.cells.Count; i++)
		{
			for (int j = 0; j < activeCrossword.cells[i].Count; j++)
			{
				CrosswordCell crosswordCell = activeCrossword.cells[i][j];

				if (crosswordCell.isNumbered && crosswordCell.number == clueNumber)
				{
					SetSelectedCell(crosswordCell.thisPos);
					SetSelectedDirection(isAcrossClue ? Direction.Across : Direction.Down);

					return;
				}
			}
		}
	}

	/// <summary>
	/// Sets the selected direction
	/// </summary>
	private void SetSelectedDirection(Direction direction, bool checkForOneCell = true)
	{
		if (selectedDirection == direction)
		{
			return;
		}

		// Un-highlight the previous direction cells
		UpdateSelectedCellsHighlightState(false);

		selectedDirection = direction;

		// Highlight the new directions cells
		UpdateSelectedCellsHighlightState(true);

		UpdateSelectedClue();
		UpdateHighlighedClueListItem();
		UpdateControlButtons();

		if (checkForOneCell)
		{
			CheckForOneCell();
		}
	}

	/// <summary>
	/// Called when a GridCell is clicked by the user
	/// </summary>
	private void SetSelectedCell(Pos cellPos)
	{
		// If the selected cell has not changed then there is no need to update anything
		if (selectedCellPos != null && selectedCellPos.rowIndex == cellPos.rowIndex && selectedCellPos.colIndex == cellPos.colIndex)
		{
			return;
		}

		// If there was no selected cell before then we need to animate the selected clue container into view
		if (!isSelectedClueShown)
		{
			ShowSelectedClue();
		}

		// If there is already a selected cell then un-highlight all the highlighted cells
		UpdateSelectedCellsHighlightState(false);

		selectedCellPos = new Pos(cellPos.rowIndex, cellPos.colIndex);

		// Highligh the new selected cells
		UpdateSelectedCellsHighlightState(true);

		UpdateSelectedClue();
		UpdateHighlighedClueListItem();

		CheckForOneCell();
	}

	/// <summary>
	/// Checks if the selected cell and selected direction is only 1 cell and switchs the direction if so
	/// </summary>
	private void CheckForOneCell()
	{
		if (IsOneCell())
		{
			// Don't check for one cell again because if for some reason this cell only has one cell in both directions it will cause an infinite loop
			SetSelectedDirection(selectedDirection == Direction.Across ? Direction.Down : Direction.Across, false);
		}
	}

	/// <summary>
	/// Checks if the selected cell and selected direction is only 1 cell
	/// </summary>
	private bool IsOneCell()
	{
		return (HighlightedStartCell == HighlightedEndCell);
	}

	/// <summary>
	/// Deselects the selected cells, closes the on screen keyboard if its showing
	/// </summary>
	private void DeselectSelectedCell()
	{
		if (selectedCellPos == null)
		{
			// There is no selected cell
			return;
		}

		UpdateSelectedCellsHighlightState(false);

		selectedCellPos = null;

		UpdateHighlighedClueListItem();
		HideSelectedClue();
	}

	/// <summary>
	/// Sets the characters to the selected cell and moves the selected cell to the next cell
	/// </summary>
	private void SetCharacters(string characters)
	{
		if (selectedCellPos == null)
		{
			return;
		}

		characters = characters.ToLower();

		// There could be multiple characters typed in a single frame
		for (int i = 0; i < characters.Length; i++)
		{
			SelectedCrosswordCell.setCharacter	= characters[i];
			SelectedGridCell.characterText.text	= characters[i].ToString().ToUpper();
			SelectedGridCell.SetIncorrect(activeCrossword.showIncorrect && SelectedCrosswordCell.setCharacter != SelectedCrosswordCell.correctCharacter);

			MoveSelectedCellForward();
		}

		// Check if the board is completed
		CheckBoardCompleted();
	}

	/// <summary>
	/// Checks if the board completed
	/// </summary>
	private void CheckBoardCompleted()
	{
		if (IsBoardComplete())
		{
			string	sectionId	= activeCrossword.sectionUniqueId;
			int		levelIndex	= activeCrossword.levelIndex;

			// Get the save key
			string saveKey = string.Format("{0}_{1}", sectionId, levelIndex);

			// If it has a saved completed time then it was already completed previously
			bool wasCompleted = savedCompletedTimes.ContainsKey(saveKey);

			activeCrossword.completed = true;

			DeselectSelectedCell();

			// Save the time it took for them to complete it
			if (!wasCompleted)
			{
				savedCompletedTimes[saveKey] = activeCrossword.timer;
			}
			else
			{
				// Update the time if it's better than the previours one
				float prevTime = savedCompletedTimes[saveKey];

				if (activeCrossword.timer < prevTime)
				{
					savedCompletedTimes[saveKey] = activeCrossword.timer;
				}
			}

			// Set the completed level count for the section
			if (!savedCompletedCount.ContainsKey(activeCrossword.sectionUniqueId))
			{
				savedCompletedCount.Add(activeCrossword.sectionUniqueId, 0);
			}

			// Only increment the completed count if this crossword was not already completed
			if (!wasCompleted)
			{
				savedCompletedCount[activeCrossword.sectionUniqueId]++;
			}

			// Remove the crossword from the savedCrosswords dictionary since it's completed
			savedCrosswords.Remove(saveKey);

			// Set the saved actie crossword key to empty
			SetSavedActiveCrosswordKey("");

			// Update the sections crossword buttons
			UpdateSectionCrosswordButtons(activeCrossword.sectionUniqueId);

			// Update the progress ring on the main screen
			UpdateMainScreen();

			// Show the crossword completed popup
			completedPopup.Show();
		}
	}

	private bool IsBoardComplete()
	{
		for (int i = 0; i < activeCrossword.cells.Count; i++)
		{
			List<CrosswordCell> rowCells = activeCrossword.cells[i];

			for (int j = 0; j < rowCells.Count; j++)
			{
				CrosswordCell crosswordCell = rowCells[j];

				if (!crosswordCell.isBlock && crosswordCell.setCharacter != crosswordCell.correctCharacter)
				{
					return false;
				}
			}
		}

		return true;
	}

	/// <summary>
	/// Deletes the character in the selected cell and moves the selected cell back
	/// </summary>
	private void DeleteCharacter()
	{
		if (selectedCellPos == null)
		{
			return;
		}

		// If the selected cell has no character then first move back one
		if ((int)SelectedCrosswordCell.setCharacter == 0)
		{
			MoveSelectedCellBack();
		}

		// Set the character to blank
		SelectedCrosswordCell.setCharacter	= (char)0;
		SelectedGridCell.characterText.text	= "";
	}

	private void MoveSelectedCellForward()
	{
		if (selectedCellPos != null && SelectedCrosswordCell != HighlightedEndCell)
		{
			UpdateSelectedCellsHighlightState(false);

			switch (selectedDirection)
			{
			case Direction.Across:
				selectedCellPos.colIndex++;
				break;
			case Direction.Down:
				selectedCellPos.rowIndex++;
				break;
			}

			UpdateSelectedCellsHighlightState(true);
		}
	}

	private void MoveSelectedCellBack()
	{
		if (selectedCellPos != null && SelectedCrosswordCell != HighlightedStartCell)
		{
			UpdateSelectedCellsHighlightState(false);

			switch (selectedDirection)
			{
			case Direction.Across:
				selectedCellPos.colIndex--;
				break;
			case Direction.Down:
				selectedCellPos.rowIndex--;
				break;
			}

			UpdateSelectedCellsHighlightState(true);
		}
	}

	private void UpdateSelectedCellsHighlightState(bool highlight)
	{
		if (selectedCellPos != null)
		{
			Pos startPos	= HighlightedStartCell.thisPos;
			Pos endPos		= HighlightedEndCell.thisPos;

			int iInc = (selectedDirection == Direction.Across) ? 0 : 1;
			int jInc = (selectedDirection == Direction.Across) ? 1 : 0;

			// Set the highligh state for all the cells
			for (int i = startPos.rowIndex, j = startPos.colIndex; i <= endPos.rowIndex && j <= endPos.colIndex; i += iInc, j += jInc)
			{
				gridCells[i][j].SetHighlighted(highlight);
			}

			// Set the selected state of the selected cell
			gridCells[selectedCellPos.rowIndex][selectedCellPos.colIndex].SetSelected(highlight);
		}
	}

	private void UpdateHighlighedClueListItem()
	{
		for (int i = 0; i < clueListItemUIs.Count; i++)
		{
			ClueListItemUI clueListItemUI = clueListItemUIs[i];

			bool isSelectedNumber		= selectedCellPos != null && clueListItemUI.ClueNumber == HighlightedStartCell.number;
			bool isSelectedDirection	= selectedCellPos != null && ((selectedDirection == Direction.Across && clueListItemUI.IsAcrossClue) || (selectedDirection == Direction.Down && !clueListItemUI.IsAcrossClue));

			clueListItemUI.numberText.color			= (isSelectedNumber && isSelectedDirection) ? clueListItemUI.selectedTextColor : clueListItemUI.normalTextColor;
			clueListItemUI.clueText.color			= (isSelectedNumber && isSelectedDirection) ? clueListItemUI.selectedTextColor : clueListItemUI.normalTextColor;
			clueListItemUI.backgroundImage.color	= (isSelectedNumber && isSelectedDirection) ? clueListItemUI.selectedBkgColor : clueListItemUI.normalBkgColor;
		}
	}

	private void UpdateControlButtons()
	{
		switchDirectionIconImage.sprite	= (selectedDirection == Direction.Across) ? switchDirectionDownIcon : switchDirectionAcrossIcon;
		showIncorrectButtonText.text	= activeCrossword.showIncorrect ? "HIDE INCORRECT" : "SHOW INCORRECT";
	}

	/// <summary>
	/// Updates the UI on the main screen. Sets the progress rings percentage and shows / hides the continue button
	/// </summary>
	private void UpdateMainScreen()
	{
		float numCompleted	= 0;
		float numTotal		= 0;

		for (int i = 0; i < sectionInfos.Count; i++)
		{
			SectionInfo sectionInfo = sectionInfos[i];

			numTotal		+= sectionInfo.crosswordFiles.Count;
			numCompleted	+= GetNumCompletedLevels(sectionInfo.uniqueId);
		}

		mainScreenProgressRing.SetProgress(numCompleted / numTotal);
	}

	/// <summary>
	/// Shows the selected clue container and tells the touch screen keyboard to display if it's supported.
	/// </summary>
	private void ShowSelectedClue()
	{
		if (isSelectedClueShown)
		{
			return;
		}

		isSelectedClueShown 		= true;
		isSelectedClueAnimating		= true;
		isSelectedClueAnimatingIn	= true;
		selectedClueAnimTime		= SelectedClueAnimationDuration;
	}

	/// <summary>
	/// Hides the selected clue container and hides the touch screen keyboard if its supported
	/// </summary>
	private void HideSelectedClue()
	{
		if (!isSelectedClueShown)
		{
			return;
		}

		isSelectedClueShown			= false;
		isSelectedClueAnimating		= true;
		isSelectedClueAnimatingIn	= false;
		selectedClueAnimTime		= SelectedClueAnimationDuration;
	}

	/// <summary>
	/// Determines if the given character is a valid character that can go on the crossword.
	/// </summary>
	private bool IsValidCharacter(char character)
	{
		return (character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z');
	}

	/// <summary>
	/// Returns the SectionInfo with the geiven unique id
	/// </summary>
	private SectionInfo GetSectionInfo(string uniqueId)
	{
		for (int i = 0; i < sectionInfos.Count; i++)
		{
			if (uniqueId == sectionInfos[i].uniqueId)
			{
				return sectionInfos[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Returns the number of completed crosswords in the given section.
	/// </summary>
	private int GetNumCompletedLevels(string sectionId)
	{
		if (savedCompletedCount.ContainsKey(sectionId))
		{
			return savedCompletedCount[sectionId];
		}

		return 0;
	}

	/// <summary>
	/// Called by the IAPController when the player makes a purchase
	/// </summary>
	private void OnIAPProductPurchased(string productId)
	{
		if (sectionUIs == null || sectionUIs.Count <= 0)
		{
			return;
		}

		for (int i = 0; i < sectionInfos.Count; i++)
		{
			SectionInfo sectionInfo = sectionInfos[i];

			// Find the unlocked section
			if (sectionInfo.iapLocked && sectionInfo.iapProductId == productId)
			{
				SectionUI sectionUI = sectionUIs[i];

				// Set the UI so it's no longer locked and levels can be started
				sectionUI.completedText.text = string.Format("{0} / {1}", GetNumCompletedLevels(sectionInfo.uniqueId), sectionInfo.crosswordFiles.Count);
				sectionUI.iapLockedIcon.gameObject.SetActive(false);
				sectionUI.iapLockedObject.SetActive(false);
			}
		}
	}

	/// <summary>
	/// Sets the saved active crossword key so we know what crossword to continue when the continue button is clicked
	/// </summary>
	private void SetSavedActiveCrosswordKey(string key)
	{
		savedActiveCrosswordKey = key;

		mainScreenContinueButton.gameObject.SetActive(!string.IsNullOrEmpty(key));
	}

	/// <summary>
	/// Parses the crossword file and creates a Crossword object
	/// </summary>
	private Crossword ParseCrosswordFile(SectionInfo sectionInfo, int levelIndex)
	{
		string contents = sectionInfo.crosswordFiles[levelIndex].text.Replace("\r", "");

		string[]	lines		= contents.Split('\n');
		int			lineIndex	= 0;
		Crossword	crossword	= new Crossword();

		crossword.sectionUniqueId	= sectionInfo.uniqueId;
		crossword.levelIndex		= levelIndex;
		crossword.name				= lines[lineIndex++];
		crossword.size				= System.Convert.ToInt32(lines[lineIndex++]);
		crossword.cells 			= new List<List<CrosswordCell>>();
		crossword.acrossClues		= new List<CrosswordClue>();
		crossword.downClues			= new List<CrosswordClue>();

		for (int i = 0; i < crossword.size; i++)
		{
			crossword.cells.Add(new List<CrosswordCell>());

			for (int j = 0; j < crossword.size; j++)
			{
				string[]		line			= lines[lineIndex++].Split('\t');
				CrosswordCell	crosswordCell	= new CrosswordCell();

				crosswordCell.isBlock			= line[0] == "1";
				crosswordCell.isNumbered		= line[1] == "1";
				crosswordCell.number			= crosswordCell.isNumbered ? System.Convert.ToInt32(line[2]) : 0;
				crosswordCell.correctCharacter	= line[3].ToLower()[0];

				int		acrossStartRow	= System.Convert.ToInt32(line[4]);
				int		acrossStartCol	= System.Convert.ToInt32(line[5]);
				int		acrossCount		= System.Convert.ToInt32(line[6]);
				int		downStartRow	= System.Convert.ToInt32(line[9]);
				int		downStartCol	= System.Convert.ToInt32(line[10]);
				int		downCount		= System.Convert.ToInt32(line[11]);

				crosswordCell.thisPos			= new Pos(i, j);
				crosswordCell.acrossStartPos	= new Pos(acrossStartRow, acrossStartCol);
				crosswordCell.acrossEndPos		= new Pos(acrossStartRow, acrossStartCol + acrossCount - 1);
				crosswordCell.downStartPos		= new Pos(downStartRow, downStartCol);
				crosswordCell.downEndPos		= new Pos(downStartRow + downCount - 1, downStartCol);

				crossword.cells[i].Add(crosswordCell);
			}
		}

		// Parse all the across clues
		int numAcross = System.Convert.ToInt32(lines[lineIndex++]);

		for (int i = 0; i < numAcross; i++)
		{
			string[]	line	= lines[lineIndex++].Split('\t');
			int			number	= System.Convert.ToInt32(line[0]);
			string		clue	= line[2];

			CrosswordClue crosswordClue = new CrosswordClue();

			crosswordClue.number	= number;
			crosswordClue.clue		= clue;

			crossword.acrossClues.Add(crosswordClue);
		}

		// Parse all the down clues
		int numDown = System.Convert.ToInt32(lines[lineIndex++]);

		for (int i = 0; i < numDown; i++)
		{
			string[]	line	= lines[lineIndex++].Split('\t');
			int			number	= System.Convert.ToInt32(line[0]);
			string		clue	= line[2];

			CrosswordClue crosswordClue = new CrosswordClue();

			crosswordClue.number	= number;
			crosswordClue.clue		= clue;

			crossword.downClues.Add(crosswordClue);
		}

		return crossword;
	}

    //TODO Add comments
    void OnApplicationQuit()
    {
        Debug.Log("Recording information before savetime: " + GameTimeRecord.GameTimeRecorded);
       

    }
    //TODO add comments
    private void SaveTimeRecorded()
    {

        
        GameTimeRecord recCrosswordTime = new GameTimeRecord()
        {
            crosswordTimePlayed = GameTimeRecord.GameTimeRecorded,
            sudokuTimePlayed = loadedSavedTime.sudokuTimePlayed
        };

        string gameRecordedTime = JsonUtility.ToJson(recCrosswordTime);
        File.WriteAllText(Application.dataPath + "/recordingData.txt", gameRecordedTime);
       

        RecordCrosswordTime.GameTimeRecorded = 0;
        Debug.Log("Data saved");
    }

    private void Save()
	{
		Dictionary<string, object> json = new Dictionary<string, object>();

		List<object> savedCrosswordsJson		= new List<object>();
		List<object> savedCompletedCountsJson	= new List<object>();
		List<object> savedCompletedTimesJson	= new List<object>();

		foreach (KeyValuePair<string, Crossword> pair in savedCrosswords)
		{
			string		key			= pair.Key;
			Crossword	crossword	= pair.Value;

			Dictionary<string, object> savedCrosswordJson = new Dictionary<string, object>();

			savedCrosswordJson["key"]				= key;
			savedCrosswordJson["showIncorrect"]		= crossword.showIncorrect;
			savedCrosswordJson["timer"]				= crossword.timer;
			savedCrosswordJson["completed"]			= crossword.completed;
			savedCrosswordJson["sectionUniqueId"]	= crossword.sectionUniqueId;
			savedCrosswordJson["levelIndex"]		= crossword.levelIndex;

			string setCharacters = "";

			for (int i = 0; i < crossword.cells.Count; i++)
			{
				for (int j = 0; j < crossword.cells[i].Count; j++)
				{
					CrosswordCell cell = crossword.cells[i][j];

					if (!cell.isBlock && cell.setCharacter != (char)0)
					{
						setCharacters += string.Format("{0},{1},{2},", i, j, cell.setCharacter);
					}
				}
			}

			if (!string.IsNullOrEmpty(setCharacters))
			{
				// The last character will be an extra ',' so remove it
				setCharacters = setCharacters.Remove(setCharacters.Length - 1);
			}

			savedCrosswordJson["setCharacters"] = setCharacters;

			savedCrosswordsJson.Add(savedCrosswordJson);
		}

		foreach (KeyValuePair<string, int> pair in savedCompletedCount)
		{
			Dictionary<string, object> savedCompletedCountJson = new Dictionary<string, object>();

			savedCompletedCountJson["key"]		= pair.Key;
			savedCompletedCountJson["count"]	= pair.Value;

			savedCompletedCountsJson.Add(savedCompletedCountJson);
		}

		foreach (KeyValuePair<string, float> pair in savedCompletedTimes)
		{
			Dictionary<string, object> savedCompletedTimeJson = new Dictionary<string, object>();

			savedCompletedTimeJson["key"]	= pair.Key;
			savedCompletedTimeJson["time"]	= pair.Value;

			savedCompletedTimesJson.Add(savedCompletedTimeJson);
		}

		json["numCrosswordsStarted"]	= numCrosswordsStarted;
		json["savedCrosswords"]			= savedCrosswordsJson;
		json["savedCompletedCount"]		= savedCompletedCountsJson;
		json["savedCompletedTimes"]		= savedCompletedTimesJson;
		json["savedActiveCrosswordKey"]	= savedActiveCrosswordKey;

		System.IO.File.WriteAllText(SaveFilePath, Utilities.ConvertToJsonString(json));

        //TODO add comments
	    Debug.Log("Saving");
	    // Debug.Log("Recording information : " + fps.ToString("f2"));
	    SaveTimeRecorded();

        

    }

	private bool LoadSave()
	{
		if (!System.IO.File.Exists(SaveFilePath))
		{
			return false;
		}

	    //todo add comment
	    if (File.Exists(Application.dataPath + "/recordingData.txt"))
	    {
	        string savedData = File.ReadAllText(Application.dataPath + "/recordingData.txt");
	        loadedSavedTime = JsonUtility.FromJson<GameTimeRecord>(savedData);
	        GameTimeRecord.GameTimeRecorded = loadedSavedTime.crosswordTimePlayed;
            
	        //  GameTimeRecord.GameTimeRecorded = loadedSavedTime.sudokuTimePlayed;
	        Debug.Log("Load recorded time : " + GameTimeRecord.GameTimeRecorded);
	    }
	    else
	    {
	        loadedSavedTime = new GameTimeRecord()
	        {
                crosswordTimePlayed = 0,
                sudokuTimePlayed = 0
	        };

	    }


	    Debug.Log("Loading save");

		JSONNode json = JSON.Parse(System.IO.File.ReadAllText(SaveFilePath));

		numCrosswordsStarted = json["numCrosswordsStarted"].AsInt;

		SetSavedActiveCrosswordKey(json["savedActiveCrosswordKey"].Value);

		JSONArray savedCrosswordsJson		= json["savedCrosswords"].AsArray;
		JSONArray savedCompletedCountsJson	= json["savedCompletedCount"].AsArray;
		JSONArray savedCompletedTimesJson	= json["savedCompletedTimes"].AsArray;

		foreach (JSONNode savedCrosswordJson in savedCrosswordsJson)
		{
			string	key				= savedCrosswordJson["key"].Value;
			bool	showIncorrect	= savedCrosswordJson["showIncorrect"].AsBool;
			float	timer			= savedCrosswordJson["timer"].AsFloat;
			bool	completed		= savedCrosswordJson["completed"].AsBool;
			string	sectionUniqueId	= savedCrosswordJson["sectionUniqueId"].Value;
			int		levelIndex		= savedCrosswordJson["levelIndex"].AsInt;
			string	setCharatersStr	= savedCrosswordJson["setCharacters"].Value;

			Crossword crossword = ParseCrosswordFile(GetSectionInfo(sectionUniqueId), levelIndex);

			crossword.showIncorrect = showIncorrect;
			crossword.timer			= timer;
			crossword.completed		= completed;

			savedCrosswords[key] = crossword;

			if (!string.IsNullOrEmpty(setCharatersStr))
			{
				string[] setCharacters = setCharatersStr.Split(',');

				for (int i = 0; i < setCharacters.Length; i += 3)
				{
					int 	row			= System.Convert.ToInt32(setCharacters[i]);
					int 	col			= System.Convert.ToInt32(setCharacters[i + 1]);
					char	character	= setCharacters[i + 2][0];

					crossword.cells[row][col].setCharacter = character;
				}
			}
		}

		foreach (JSONNode savedCompletedCountJson in savedCompletedCountsJson)
		{
			string	key		= savedCompletedCountJson["key"].Value;
			int		count	= savedCompletedCountJson["count"].AsInt;

			savedCompletedCount[key] = count;
		}

		foreach (JSONNode savedCompletedTimeJson in savedCompletedTimesJson)
		{
			string	key		= savedCompletedTimeJson["key"].Value;
			float	time	= savedCompletedTimeJson["time"].AsFloat;

			savedCompletedTimes[key] = time;
		}

		return true;
	}

	#endregion
}
