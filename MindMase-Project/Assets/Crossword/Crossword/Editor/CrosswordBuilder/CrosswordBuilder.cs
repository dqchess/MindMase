using UnityEngine;
using UnityEditor;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

public class CrosswordBuilder : EditorWindow
{
	#region classes

	public class Cell
	{
		public bool isBlock		= false;
		public bool isNumbered	= false;
		public int	number		= 0;
		public char character	= ' ';

		public int		startRowAcross;
		public int		startColAcross;
		public int		acrossCount;
		public bool		hasAcrossWord;
		public string	acrossWord;
		public int		acrossSelectedClue;
		public bool		acrossUseCustomClue;
		public string	acrossCustomClue;

		public int		startRowDown;
		public int		startColDown;
		public int		downCount;
		public bool		hasDownWord;
		public string	downWord;
		public int		downSelectedClue;
		public bool		downUseCustomClue;
		public string	downCustomClue;
	}

	#endregion

	#region Member Variables

	// UI Constants
	private const int 	MaxGridSize				= 20;
	private const int 	MinWindowWidth			= 550;
	private const float	WindowPadding			= 5f;
	private const float	GridCellSpacing			= 2f;
	private const float	GridWidthPercent		= 0.7f;
	private const float	WordListWidthPercent	= 1f - GridWidthPercent;
	private const float ScrollBarWidth			= 15f;
	private const float ButtonHeight			= 25f;

	// Variables set in editor window
	private TextAsset	wordFile;
	private string		gridName;
	private int			gridSize				= 15;
	private bool		placeBlocks				= true;
	private bool		mirrorBlockPlacements	= true;
	private bool		enterWordsManually		= false;
	private bool		autoFillBoard			= false;

	private float	toolsWindowWidth;
	private string	gridSizeErrorMessage;
	private float	wordListViewportHeight	= 0f;
	private Vector2	windowScrollPosition	= Vector2.zero;
	private Vector2	wordListScrollPosition	= Vector2.zero;
	private bool	showAcrossClues			= false;
	private bool	showDownClues			= false;

	private List<List<Cell>> cells;

	private CBWordDict		wordDictionary;
	private CBWordFinder	wordFinder;
	private List<string>	possibleWords;
	private float			wdCompletePercent;
	private float			pwUpdateTimer;
	private bool			wordFinderWasProcessing;
	private bool			autoFillerFailed;
	private int				dotCount;
	private double			dotIncreaseTime;

	private bool	selectedAcross	= true;
	private int		selectedCellRow	= -1;	// Row of the cell that was clicked
	private int		selectedCellCol	= -1;	// Column of the cell that was clicked
	private int		selectedLength;			// Length of the word for the selected cell
	private string	selectedWord;			// The word that has been selected in the word list
	private int		startCellRow;			// Row of the start of the selected words
	private int		startCellCol;			// Column of the start of the selected words
	private string	manuallyTypedWord;
	private int		maxNeighbourCount;
	private bool	noSquares;

	// Textures used for the grid cells
	private Texture2D lineTexture;
	private Texture2D whiteTexture;
	private Texture2D blackTexture;
	private Texture2D greyTexture;
	private Texture2D blueTexture;
	private Texture2D darkBlueTexture;
	private Texture2D lightBlueTexture;
	private Texture2D boxTexture;

	private CBAutoFiller autoFiller;

	#endregion

	#region Properties

	private Texture2D LineTexture
	{
		get
		{
			if (lineTexture == null)
			{
				lineTexture = CreateTexture(new Color(26f/255f, 26f/255f, 26f/255f));
			}

			return lineTexture;
		}
	}

	private Texture2D WhiteTexture
	{
		get
		{
			if (whiteTexture == null)
			{
				whiteTexture = CreateTexture(Color.white);
			}

			return whiteTexture;
		}
	}

	private Texture2D BlackTexture
	{
		get
		{
			if (blackTexture == null)
			{
				blackTexture = CreateTexture(Color.black);
			}

			return blackTexture;
		}
	}

	private Texture2D GreyTexture
	{
		get
		{
			if (greyTexture == null)
			{
				greyTexture = CreateTexture(Color.grey);
			}

			return greyTexture;
		}
	}

	private Texture2D BlueTexture
	{
		get
		{
			if (blueTexture == null)
			{
				blueTexture = CreateTexture(new Color(183f / 255f, 237f / 255f, 255f / 255f));
			}

			return blueTexture;
		}
	}

	private Texture2D DarkBlueTexture
	{
		get
		{
			if (darkBlueTexture == null)
			{
				darkBlueTexture = CreateTexture(new Color(137f / 255f, 225f / 255f, 255f / 255f));
			}

			return darkBlueTexture;
		}
	}

	private Texture2D LightBlueTexture
	{
		get
		{
			if (lightBlueTexture == null)
			{
				lightBlueTexture = CreateTexture(new Color(240f / 255f, 250f / 255f, 255f / 255f));
			}

			return lightBlueTexture;
		}
	}

	private Texture2D BoxTexture
	{
		get
		{
			if (boxTexture == null)
			{
				boxTexture = CreateTexture(new Color(160f / 255f, 160f / 255f, 160f / 255f));
			}

			return boxTexture;
		}
	}


	/// <summary>
	/// Is the word dictionary currently processing a word file
	/// </summary>
	private bool IsCBWordDictProcessingWordFile { get { return wordDictionary != null && wordDictionary.IsProcessing && wordDictionary.TypeOfWork == CBWordDictWorker.TypeOfWork.PreProcess; } }

	/// <summary>
	/// Is the word dictionary currently loading a pre-processed word file
	/// </summary>
	private bool IsCBWordDictLoadingPreprocessedFile { get { return wordDictionary != null && wordDictionary.IsProcessing && wordDictionary.TypeOfWork == CBWordDictWorker.TypeOfWork.Load; } }

	/// <summary>
	/// Returns tru if the word dictionary has loaded the pre-processed word file that is needed when creating crosswords
	/// </summary>
	private bool EnableGridControlls { get { return wordDictionary != null && !wordDictionary.IsProcessing && wordDictionary.HasLoadedWords; } }

	#endregion

	#region Unity Methods

	[MenuItem("Window/Crossword Builder")]
	private static void Init()
	{
		EditorWindow.GetWindow<CrosswordBuilder>("Crossword Builder");
	}

	#endregion

	#region Unity Methods

	private void OnEnable()
	{
		if (wordDictionary == null)
		{
			wordDictionary = new CBWordDict();
		}

		if (wordFinder == null)
		{
			wordFinder = new CBWordFinder();
		}

		if (autoFiller == null)
		{
			autoFiller = new CBAutoFiller();
		}

		if (possibleWords == null)
		{
			possibleWords = new List<string>();
		}

		CreateCellList();
		ClearSelected();

		placeBlocks				= true;
		mirrorBlockPlacements	= true;
	}

	private void OnDisable()
	{
		if (wordDictionary.IsProcessing)
		{
			wordDictionary.StopProcessing();
		}

		if (wordFinder.IsProcessing)
		{
			wordFinder.StopProcessing();
		}

		wordDictionary.Clear();

		wordDictionary	= null;
		wordFinder		= null;

		// Delete the textures so they don;t hang around in memory
		DestroyImmediate(LineTexture);
		DestroyImmediate(WhiteTexture);
		DestroyImmediate(BlackTexture);
		DestroyImmediate(GreyTexture);
		DestroyImmediate(LightBlueTexture);
		DestroyImmediate(BlueTexture);
		DestroyImmediate(DarkBlueTexture);
		DestroyImmediate(BoxTexture);
	}

	private void Update()
	{
		EditorUtility.ClearProgressBar();

		if (IsCBWordDictProcessingWordFile)
		{
			float	progress	= wordDictionary.CheckProgress();
			bool	canceled	= EditorUtility.DisplayCancelableProgressBar("Processing Words", (Mathf.RoundToInt(progress * 100f)) + "% Complete", progress);

			if (canceled)
			{
				wordDictionary.StopProcessing();
			}
		}
		else if (IsCBWordDictLoadingPreprocessedFile)
		{
			wordDictionary.CheckProgress();
			Repaint();
		}
		else if (!wordDictionary.HasLoadedWords && CBUtilities.PreProcessedFileExists)
		{
			wordDictionary.LoadPreProcessedFile();
		}
		else if (wordFinder.IsProcessing)
		{
			wdCompletePercent = wordFinder.CheckProgress();

			if (Time.realtimeSinceStartup >= pwUpdateTimer + 1)
			{
				possibleWords	= wordFinder.GetPossibleWords();
				pwUpdateTimer	= Time.realtimeSinceStartup;
			}

			Repaint();
		}

		// If the word finder has finished processing then get the possible words one last time
		if (!wordFinder.IsProcessing && wordFinderWasProcessing)
		{
			possibleWords			= wordFinder.GetPossibleWords();
			wordFinderWasProcessing	= false;

			Repaint();
		}

		// Check if auto filler was started
		if (autoFiller.IsProcessing)
		{
			// Call the check progress method to update IsProcessing
			autoFiller.CheckProgress();

			// If IsProcessing is now false then it has finished
			if (!autoFiller.IsProcessing)
			{
				if (!autoFiller.Cancelled)
				{
					string completedBoard = autoFiller.CompletedBoard;

					if (string.IsNullOrEmpty(completedBoard))
					{
						autoFillerFailed = true;
					}
					else
					{
						ParseAutoFillerCompletedBoard(completedBoard);
					}
				}

				Repaint();
			}

			if (dotIncreaseTime - Utilities.SystemTimeInMilliseconds <= 0)
			{
				dotCount		= (dotCount + 1) % 3;
				dotIncreaseTime	= Utilities.SystemTimeInMilliseconds + 750f;
				Repaint();
			}
		}
	}

	private void OnGUI()
	{
		if (Event.current != null && Event.current.type == EventType.KeyDown)
		{
			switch (Event.current.keyCode)
			{
			case KeyCode.Return:
				if (!enterWordsManually && !string.IsNullOrEmpty(selectedWord))
				{
					GUIUtility.keyboardControl = 0;
					PlaceWordOnSelectedCell(selectedWord);
					Repaint();
					return;
				}
				else if (enterWordsManually && !string.IsNullOrEmpty(manuallyTypedWord) && manuallyTypedWord.Length >= selectedLength)
				{
					GUIUtility.keyboardControl = 0;
					PlaceWordOnSelectedCell(manuallyTypedWord);
					manuallyTypedWord = "";
					Repaint();
					return;
				}
				break;
			case KeyCode.UpArrow:
			case KeyCode.DownArrow:
				GUIUtility.keyboardControl = 0;
				selectedAcross = false;
				UpdateSelectedCell();
				return;
			case KeyCode.LeftArrow:
			case KeyCode.RightArrow:
				GUIUtility.keyboardControl = 0;
				selectedAcross = true;
				UpdateSelectedCell();
				return;
			}
		}

		// Get the width of area we will draw the ui elements in, it cannot be smaller than MinWindowWidth or thigns will be to squished
		toolsWindowWidth = Mathf.Max(position.width, MinWindowWidth) - WindowPadding * 2f - ScrollBarWidth;

		windowScrollPosition = EditorGUILayout.BeginScrollView(windowScrollPosition,false, false);

		EditorGUILayout.Space();

		DrawTitle();

		EditorGUILayout.Space();

		DrawWordDictionarySettings();

		EditorGUILayout.Space();

		GUI.enabled = wordDictionary.HasLoadedWords && !wordDictionary.IsProcessing;

		DrawSettings();

		EditorGUILayout.Space();

		DrawGrid();

		EditorGUILayout.Space();

		DrawGridInfo();

		EditorGUILayout.Space();

		DrawClues();

		EditorGUILayout.Space();

		DrawCreateGridButton();

		EditorGUILayout.Space();

		GUI.enabled = true;

		EditorGUILayout.EndScrollView();
	}

	#endregion

	#region Private Methods

	private void ProcessWordFile()
	{
		if (wordFile != null)
		{
			ClearSelected();

			if (autoFiller.IsProcessing)
			{
				autoFiller.Cancelled = true;
				autoFiller.StopProcessing();
			}

			// New pre-processed file is about to be generated, clear the current loaded dictionary and remove the current pre-processed file.
			wordDictionary.Clear();
			CBUtilities.DeletePreProcessedFile();

			wordDictionary.ProcessWordFile(wordFile.text);

			// Clear the field so the warning doesnt appear right away
			wordFile = null;
		}
	}

	private void UpdateSelectedCell()
	{
		if (selectedCellRow == -1 || selectedCellCol == -1)
		{
			return;
		}

		startCellRow	= GetStartRow(selectedCellRow, selectedCellCol, selectedAcross);
		startCellCol	= GetStartCol(selectedCellRow, selectedCellCol, selectedAcross);
		selectedLength	= CountLetters(startCellRow, startCellCol, selectedAcross ? 0 : 1, selectedAcross ? 1 : 0);

		// If selected legnth is 1 then switch the direction
		if (selectedLength == 1)
		{
			selectedAcross = !selectedAcross;

			// Re-calculate the start row/col/length
			startCellRow	= GetStartRow(selectedCellRow, selectedCellCol, selectedAcross);
			startCellCol	= GetStartCol(selectedCellRow, selectedCellCol, selectedAcross);
			selectedLength	= CountLetters(startCellRow, startCellCol, selectedAcross ? 0 : 1, selectedAcross ? 1 : 0);

			// If selected length is still 1 then this must but a single block so don't select anything
			if (selectedLength == 1)
			{
				// Switch selectedAcross back to what it was
				selectedAcross	= !selectedAcross;
				selectedCellRow	= -1;
				selectedCellCol	= -1;

				return;
			}
		}

		Cell startCell = cells[startCellRow][startCellCol];

		// Set the selected word to the word that is already place (if there is one)
		if (selectedAcross && startCell.hasAcrossWord)
		{
			selectedWord = startCell.acrossWord;
		}
		else if (!selectedAcross && startCell.hasDownWord)
		{
			selectedWord = startCell.downWord;
		}
		else
		{
			selectedWord = "";
		}

		StopAndClearWordFinder();

		if (!enterWordsManually && !autoFillBoard && string.IsNullOrEmpty(selectedWord))
		{
			if (!wordDictionary.HasWordsOfLength(selectedLength))
			{
				Debug.LogError("[CrosswordBuilder] There are no words of length " + selectedLength + " in the word dictionary.");
				ClearSelected();
				return;
			}

			List<string> wordsToTry = wordDictionary.GetPossibleWords(cells, startCellRow, startCellCol, selectedLength, selectedAcross);

			if (wordsToTry == null)
			{
				Debug.Log("There are no words that fit.");
				return;
			}

			pwUpdateTimer			= Time.realtimeSinceStartup;
			wordFinderWasProcessing	= true;

			wordFinder.StartFindingWords(
				wordDictionary,
				wordsToTry,
				CBUtilities.CopyCellsList(cells),
				new List<string>(),
				startCellRow,
				startCellCol,
				selectedLength,
				selectedAcross
			);
		}
	}

	private void PlaceWordOnSelectedCell(string word)
	{
		if (string.IsNullOrEmpty(word) || selectedCellRow == -1 || selectedCellCol == -1)
		{
			return;
		}

		CBUtilities.PlaceWordOnCell(word, cells, startCellRow, startCellCol, selectedLength, selectedAcross);

		UpdateSelectedCell();
	}

	/// <summary>
	/// Removes the letter for the selected cell only. Removes the down and cross word because its now broken.
	/// </summary>
	private void RemoveSelectedLetter(bool callUpdateSelectedCells = true)
	{
		Cell selectedCell = cells[selectedCellRow][selectedCellCol];

		selectedCell.character = ' ';

		Cell acrossStartCell 				= cells[selectedCell.startRowAcross][selectedCell.startColAcross];
		acrossStartCell.hasAcrossWord		= false;
		acrossStartCell.acrossWord			= "";
		acrossStartCell.acrossSelectedClue	= 0;
		acrossStartCell.acrossUseCustomClue	= false;
		acrossStartCell.acrossCustomClue	= "";

		Cell downStartCell 				= cells[selectedCell.startRowDown][selectedCell.startColDown];
		downStartCell.hasDownWord		= false;
		downStartCell.downWord			= "";
		downStartCell.downSelectedClue	= 0;
		downStartCell.downUseCustomClue	= false;
		downStartCell.downCustomClue	= "";

		CBUtilities.RemoveWord(cells, selectedCell.startRowAcross, selectedCell.startColAcross, selectedCell.acrossCount, true);
		CBUtilities.RemoveWord(cells, selectedCell.startRowDown, selectedCell.startColDown, selectedCell.downCount, false);

		if (callUpdateSelectedCells)
		{
			UpdateSelectedCell();
		}
	}

	/// <summary>
	/// Removes all letters the the selected cell. Removes any words that are broken as a result of removing the letters.
	/// </summary>
	private void RemoveSelectedLetters()
	{
		Cell startCell = cells[startCellRow][startCellCol];

		if (selectedAcross)
		{
			startCell.hasAcrossWord 		= false;
			startCell.acrossWord			= "";
			startCell.acrossSelectedClue	= 0;
			startCell.acrossUseCustomClue	= false;
			startCell.acrossCustomClue		= "";
		}
		else
		{
			startCell.hasDownWord		= false;
			startCell.downWord			= "";
			startCell.downSelectedClue	= 0;
			startCell.downUseCustomClue	= false;
			startCell.downCustomClue	= "";
		}

		for (int i = 0; i < selectedLength; i++)
		{
			int row = startCellRow + (selectedAcross ? 0 : i);
			int col = startCellCol + (selectedAcross ? i : 0);

			Cell cell = cells[row][col];

			// Clear the cells character
			cell.character = ' ';

			// Get the start of the cross word for this cell
			bool	crossAcross		= !selectedAcross;
			int		crossRowStart	= crossAcross ? cell.startRowAcross : cell.startRowDown;
			int		crossColStart	= crossAcross ? cell.startColAcross : cell.startColDown;
			int		crossLength		= crossAcross ? cell.acrossCount : cell.downCount;

			// Remove the word since it is now broken
			CBUtilities.RemoveWord(cells, crossRowStart, crossColStart, crossLength, crossAcross);
		}

		UpdateSelectedCell();
	}

	/// <summary>
	/// Removes the word by only removing the letters that are not part of a crossing word.
	/// </summary>
	private void RemoveSelectedWord()
	{
		if (startCellRow != -1 && startCellCol != -1)
		{
			CBUtilities.RemoveWord(cells, startCellRow, startCellCol, selectedLength, selectedAcross);
			UpdateSelectedCell();
		}
	}

	private int GetStartRow(int row, int col, bool across)
	{
		return across ? row : row - CountLetters(row, col, -1, 0) + 1;
	}

	private int GetStartCol(int row, int col, bool across)
	{
		return !across ? col : col - CountLetters(row, col, 0, -1) + 1;
	}

	private int CountLetters(int iStart, int jStart, int iInc, int jInc)
	{
		int count = 0;

		for (int i = iStart, j = jStart; i >= 0 && i < gridSize && j >= 0 && j < gridSize; i += iInc, j += jInc)
		{
			if (cells[i][j].isBlock)
			{
				break;
			}

			count++;
		}

		return count;
	}

	private void CreateCellList()
	{
		placeBlocks = true;

		cells = new List<List<Cell>>();

		for (int i = 0; i < gridSize; i++)
		{
			cells.Add(new List<Cell>());

			for (int j = 0; j < gridSize; j++)
			{
				cells[i].Add(new Cell());
			}
		}
	}

	private void ClearGrid()
	{
		CreateCellList();
		ClearSelected();
	}

	private void ClearCharacters()
	{
		for (int i = 0; i < cells.Count; i++)
		{
			for (int j = 0; j < cells.Count; j++)
			{
				cells[i][j].character			= ' ';
				cells[i][j].hasDownWord			= false;
				cells[i][j].hasAcrossWord		= false;
				cells[i][j].downWord			= "";
				cells[i][j].acrossWord			= "";
				cells[i][j].acrossSelectedClue	= 0;
				cells[i][j].acrossUseCustomClue	= false;
				cells[i][j].acrossCustomClue	= "";
				cells[i][j].downSelectedClue	= 0;
				cells[i][j].downUseCustomClue	= false;
				cells[i][j].downCustomClue		= "";
			}
		}
	}

	private void ClearSelected()
	{
		selectedAcross	= true;
		selectedCellRow	= -1;
		selectedCellCol	= -1;
		startCellRow	= -1;
		startCellCol	= -1;

		selectedWord 		= "";
		manuallyTypedWord	= "";

		StopAndClearWordFinder();
	}

	private void StopAndClearWordFinder()
	{
		possibleWords.Clear();
		wordFinder.StopProcessing();
		wordFinderWasProcessing = false;
	}

	private Texture2D CreateTexture(Color color)
	{
		Texture2D texture = new Texture2D(1, 1);
		texture.SetPixel(0, 0, color);
		texture.Apply();

		return texture;
	}

	#region GUI Draw Methods

	private void DrawTitle()
	{
		GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
		titleStyle.fontSize = 15;
		titleStyle.fontStyle = FontStyle.Bold;

		EditorGUILayout.LabelField("Crossword Grid Tool", titleStyle, GUILayout.Height(20), GUILayout.Width(toolsWindowWidth));
	}

	private void DrawWordDictionarySettings()
	{
		EditorGUILayout.LabelField("Word Dictionary", EditorStyles.boldLabel);

		DrawLine();

		EditorGUILayout.Space();

		string statusText = "";

		GUIStyle statusStyle	= new GUIStyle(GUI.skin.label);
		statusStyle.fontStyle	= FontStyle.Bold;

		if (wordDictionary.IsProcessing)
		{
			switch (wordDictionary.TypeOfWork)
			{
			case CBWordDictWorker.TypeOfWork.PreProcess:
				statusText = "Processing Word File";
				break;
			case CBWordDictWorker.TypeOfWork.Load:
				statusText = string.Format("Loading Dictionary File ({0}% complete)", Mathf.RoundToInt(100f * wordDictionary.CheckProgress()));
				break;
			}

			statusStyle.normal.textColor = new Color(196f / 255f, 112f / 255f, 0);
		}
		else if (wordDictionary.HasLoadedWords)
		{
			statusText						= "Ready";
			statusStyle.normal.textColor	= new Color(78f / 196f, 160f / 255f, 0);
		}
		else
		{
			statusText = "No Pre-Processed Dictionary File";
		}

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Status:", GUILayout.Width(40f));
		EditorGUILayout.LabelField(statusText, statusStyle);
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.LabelField("Number of words: " + (!wordDictionary.IsProcessing && wordDictionary.HasLoadedWords ? wordDictionary.NumberOfWords.ToString() : "---"));

		EditorGUILayout.Space();

		if (!wordDictionary.HasLoadedWords && !CBUtilities.PreProcessedFileExists)
		{
			EditorGUILayout.HelpBox("There is no pre-processed word dictionary file. Before crosswords can be created the word dictionary file needs to be generated. Drag the word file into the \"Word File\" field and click \"Process Word File\".", MessageType.Error);
		}

		wordFile = EditorGUILayout.ObjectField("Word File:", wordFile, typeof(TextAsset), false, GUILayout.Width(toolsWindowWidth)) as TextAsset;

		if (wordFile == null)
		{
			GUI.enabled = false;
		}
		else if (CBUtilities.PreProcessedFileExists)
		{
			EditorGUILayout.HelpBox("A pre-processed word file already exists, clicking the \"Process Word File\" button will delete this file and generate a new one.", MessageType.Warning);
		}

		if (GUILayout.Button("Process Word File", GUILayout.Width(toolsWindowWidth), GUILayout.Height(20)))
		{
			ProcessWordFile();
		}

		GUI.enabled = wordDictionary.HasLoadedWords;
	}

	private void DrawSettings()
	{
		EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);

		DrawLine();

		EditorGUILayout.Space();

		int prevGirdSize = gridSize;

		gridName = EditorGUILayout.TextField("Grid Name", gridName, GUILayout.Width(toolsWindowWidth));
		gridSize = EditorGUILayout.IntField("Grid Size", gridSize, GUILayout.Width(toolsWindowWidth));

		// Grid name cannot be empty
		if (string.IsNullOrEmpty(gridName))
		{
			gridName = "grid";
		}

		// If the grid size changes then clear all the blocks
		if (gridSize != prevGirdSize)
		{
			// Check if the user tried to set the grid size to something greater than the hardcoded max grid size
			if (gridSize > MaxGridSize)
			{
				gridSize				= MaxGridSize;
				gridSizeErrorMessage	= "The maximum Grid Size is set to " + MaxGridSize + ". This is so the editor does not freeze when trying to generate the grid buttons. If you really wish to set the Grid Size larger than " + MaxGridSize + " then open BoardCreatorTool.cs located in the Editor folder and set the MaxGridSize variable.";
			}
			else
			{
				gridSizeErrorMessage = "";
			}

			ClearGrid();
		}

		// Display the error if there is one
		if (!string.IsNullOrEmpty(gridSizeErrorMessage))
		{
			EditorGUILayout.HelpBox(gridSizeErrorMessage, MessageType.Warning);
		}
	}

	private void DrawGrid()
	{
		/********************************************************/
		/* Initialize variables and create styles used in grids */
		/********************************************************/

		bool wasGUIEnabled	= GUI.enabled;

		float gridWidth		= toolsWindowWidth * GridWidthPercent;
		float gridHeight	= gridWidth + ButtonHeight + 3f;
		float wordListWidth	= toolsWindowWidth * WordListWidthPercent;
		float gridCellSize	= (gridWidth - WindowPadding * 2f - GridCellSpacing * (float)(gridSize - 1)) / (float)gridSize;

		// Create the styles used by the buttons and grid cell toggles
		GUIStyle boxStyle			= new GUIStyle(GUI.skin.box);
		boxStyle.normal.background	= BoxTexture;

		GUIStyle gridCellStyle				= new GUIStyle();
		gridCellStyle.fixedHeight			= gridCellSize;
		gridCellStyle.fixedWidth			= gridCellSize;
		gridCellStyle.margin				= new RectOffset((int)GridCellSpacing, (int)GridCellSpacing, (int)GridCellSpacing, (int)GridCellSpacing);
		gridCellStyle.fontStyle				= FontStyle.Bold;
		gridCellStyle.fontSize				= 12;
		gridCellStyle.alignment				= TextAnchor.MiddleCenter;
		gridCellStyle.normal.background		= WhiteTexture;
		gridCellStyle.active.background		= GreyTexture;
		gridCellStyle.onNormal.background	= BlackTexture;
		gridCellStyle.onActive.background	= GreyTexture;

		GUIStyle selectedGridCellStyle			= new GUIStyle(gridCellStyle);
		selectedGridCellStyle.normal.background	= BlueTexture;

		GUIStyle startGridCellStyle				= new GUIStyle(gridCellStyle);
		startGridCellStyle.normal.background	= DarkBlueTexture;

		GUIStyle blueCellStyle				= new GUIStyle(gridCellStyle);
		blueCellStyle.normal.background		= LightBlueTexture;

		GUIStyle numberLabelStyle	= new GUIStyle(GUI.skin.label);
		numberLabelStyle.fontSize	= 8;

		GUIStyle toggleButtonStyle		= new GUIStyle(GUI.skin.button);
		toggleButtonStyle.fixedWidth	= 100;
		toggleButtonStyle.fixedHeight	= ButtonHeight;

		Rect gridUIRect		= new Rect(GUILayoutUtility.GetLastRect());
		gridUIRect.x		= WindowPadding;
		gridUIRect.y		+= 8;
		gridUIRect.width	= toolsWindowWidth;
		gridUIRect.height	= gridHeight + ButtonHeight + 3f;

		// Draw the box that will go around the gird
		GUI.Box(gridUIRect, "", boxStyle);

		/********************************************************/
		/* Draw the toggle buttons on top of the grid           */
		/********************************************************/

		// Space from top of grey box
		GUILayout.Space(3);

		GUILayout.BeginHorizontal();

		// Space from left of grey box
		GUILayout.Space(9);

		bool pbToggled = GUILayout.Toggle(placeBlocks, "Place Blocks", toggleButtonStyle);
		bool pwToggled = GUILayout.Toggle(!placeBlocks, "Place Words", toggleButtonStyle);

		if (placeBlocks && pwToggled)
		{
			placeBlocks = false;
		}
		else if (!placeBlocks && pbToggled)
		{
			placeBlocks = true;
		}

		if (placeBlocks)
		{
			mirrorBlockPlacements = GUILayout.Toggle(mirrorBlockPlacements, "Mirror", toggleButtonStyle);
			ClearSelected();
		}
		else
		{
			GUI.enabled = wasGUIEnabled && selectedCellCol != -1 && selectedCellRow != -1;

			if (GUILayout.Button(selectedAcross ? "Down" : "Across", GUILayout.Width(100), GUILayout.Height(ButtonHeight)))
			{
				selectedAcross = !selectedAcross;
				UpdateSelectedCell();
			}

			GUI.enabled = wasGUIEnabled;
		}

		GUILayout.EndHorizontal();

		/********************************************************/
		/* Draw the grid                                        */
		/********************************************************/

		GUILayout.Space(1);

		int		number	= 1;
		float	middle	= (float)gridSize / 2f;

		for (int i = 0; i < gridSize; i++)
		{
			GUILayout.BeginHorizontal();

			GUILayout.Space(WindowPadding * 2f);

			for (int j = 0; j < gridSize; j++)
			{
				Cell cell			= cells[i][j];
				bool isAcross		= !cell.isBlock && (j == 0 || cells[i][j - 1].isBlock);
				bool isDown			= !cell.isBlock && (i == 0 || cells[i - 1][j].isBlock);
				bool isNumberedCell	= isAcross || isDown;

				GUIStyle	guiStyle		= null;
				bool		selectedCell	= true;

				if (startCellRow != -1 && startCellCol != -1 && i == startCellRow && j == startCellCol)
				{
					guiStyle = startGridCellStyle;
				}
				else if (selectedCellRow != -1 && selectedCellCol != -1 && i == selectedCellRow && j == selectedCellCol)
				{
					guiStyle = selectedGridCellStyle;
				}
				else if (selectedAcross && i == startCellRow && j > startCellCol && j < startCellCol + selectedLength)
				{
					guiStyle = blueCellStyle;
				}
				else if (!selectedAcross && j == startCellCol && i > startCellRow && i < startCellRow + selectedLength)
				{
					guiStyle = blueCellStyle;
				}
				else
				{
					guiStyle		= gridCellStyle;
					selectedCell	= false;
				}

				string cellCharacter = cell.isBlock ? "" : cell.character.ToString();

				// If the cell is one of the selected cells and there is a selected word and the cell is blank then set the text color to grey and set the character
				if (!cell.isBlock && selectedCell && !string.IsNullOrEmpty(selectedWord) && cell.character == ' ')
				{
					guiStyle.normal.textColor	= Color.gray;
					cellCharacter				= selectedWord[selectedAcross ? j - startCellCol : i - startCellRow].ToString();
				}
				else
				{
					guiStyle.normal.textColor = Color.black;
				}

				if (enterWordsManually && selectedCell)
				{
					int charIndex = selectedAcross ? j - startCellCol : i - startCellRow;

					if (charIndex < manuallyTypedWord.Length && charIndex < selectedLength)
					{
						cellCharacter = manuallyTypedWord[charIndex].ToString();
						guiStyle.normal.textColor = Color.blue;
					}
				}

				bool prevToggle	= cell.isBlock;
				bool toggled	= GUILayout.Toggle(cell.isBlock, cellCharacter, guiStyle);

				if (placeBlocks)
				{
					// First remove the character from this cell if there is one and the cell was changed to a block
					if (!cell.isBlock && toggled && cell.character != ' ')
					{
						// Briefly set the selected row/col to this cell
						selectedCellRow	= i;
						selectedCellCol	= j;

						// Call remove selected letter to remove the character on this cell if there is one
						RemoveSelectedLetter(false);

						// Set the selected row/col back to -1
						selectedCellRow	= -1;
						selectedCellCol	= -1;
					}

					cell.isBlock		= toggled;
					cell.startRowAcross	= GetStartRow(i, j, true);
					cell.startColAcross	= GetStartCol(i, j, true);
					cell.acrossCount	= CountLetters(cell.startRowAcross, cell.startColAcross, 0, 1);
					cell.startRowDown	= GetStartRow(i, j, false);
					cell.startColDown	= GetStartCol(i, j, false);
					cell.downCount		= CountLetters(cell.startRowDown, cell.startColDown, 1, 0);

					if (isNumberedCell)
					{
						if (isAcross)
						{
							isAcross = cell.acrossCount > 1;
						}

						if (isDown)
						{
							isDown = cell.downCount > 1;
						}

						isNumberedCell = isAcross || isDown;
					}

					cell.isNumbered		= isNumberedCell;
					cell.number			= isNumberedCell ? number : 0;
				}
				else if (toggled && !cell.isBlock)
				{
					selectedCellRow	= i;
					selectedCellCol	= j;

					UpdateSelectedCell();
				}

				Rect cellRect = GUILayoutUtility.GetLastRect();

				cellRect.x -= 1f;
				cellRect.y -= 2f;

				GUI.Label(cellRect, cell.isNumbered ? number.ToString() : "", numberLabelStyle);

				// If the toggled value changes then we need to change the mirrored value aswell
				if (mirrorBlockPlacements && prevToggle != cell.isBlock)
				{
					int mirroredI = (int)(middle + (middle - (float)i)) - 1;
					int mirroredJ = (int)(middle + (middle - (float)j)) - 1;

					Cell mirroredCell = cells[mirroredI][mirroredJ];

					if (!mirroredCell.isBlock && cell.isBlock && mirroredCell.character != ' ')
					{
						// Briefly set the selected row/col to this cell
						selectedCellRow	= mirroredI;
						selectedCellCol	= mirroredJ;

						// Call remove selected letter to remove the character on this cell if there is one
						RemoveSelectedLetter(false);

						// Set the selected row/col back to -1
						selectedCellRow	= -1;
						selectedCellCol	= -1;
					}

					mirroredCell.isBlock = cell.isBlock;
				}

				if (cell.isNumbered)
				{
					number++;
				}
			}

			GUILayout.EndHorizontal();
		}

		/********************************************************/
		/* Draw the list of possible words                      */
		/********************************************************/

		// Draw word list
		float topHeight = ButtonHeight + 8;

		Rect wordListRect	= new Rect();
		wordListRect.x		= gridUIRect.xMax - wordListWidth;
		wordListRect.y		= gridUIRect.yMin + topHeight;
		wordListRect.width	= wordListWidth - WindowPadding;
		wordListRect.height	= gridUIRect.height - topHeight - WindowPadding - ButtonHeight - 3f;

		Rect labelRect		= new Rect(wordListRect);
		labelRect.y			= wordListRect.y - 29;
		labelRect.height	= 20;

		// Reset the fixed width/height so we can reuse it
		toggleButtonStyle.fixedWidth	= 0;
		toggleButtonStyle.fixedHeight	= 0;

		float buttonHeight	= 20f;
		float buttonPadding	= 5f;
		float buttonSpacing	= 2f;

		toggleButtonStyle.fixedHeight = ButtonHeight;

		bool prevEnterWordsManually	= enterWordsManually;
		bool prevAutoFillBoard		= autoFillBoard;

		enterWordsManually = GUI.Toggle(labelRect, this.enterWordsManually, "Enter Words Manually", toggleButtonStyle);

		labelRect.y += 28;

		autoFillBoard = GUI.Toggle(labelRect, this.autoFillBoard, "Auto Fill Board", toggleButtonStyle);

		toggleButtonStyle.fixedHeight = 0;

		labelRect.y += 28;

		// If either toggle changed then call ClearSelected
		if (enterWordsManually != prevEnterWordsManually || autoFillBoard != prevAutoFillBoard)
		{
			UpdateSelectedCell();
		}

		// If "Enter Words Manually" changed and it turned on then make sure "Auto Fill Board" is turned off
		if (enterWordsManually != prevEnterWordsManually && enterWordsManually)
		{
			autoFillBoard = false;
		}

		// If "Auto Fill Board" changed and it turned on then make sure "Enter Words Manually" is turned off
		if (autoFillBoard != prevAutoFillBoard && autoFillBoard)
		{
			enterWordsManually = false;
		}

		// If "Auto Fill Board" changed and it was turned off then we need to stop the autoFiller worker if its processing
		if (autoFillBoard != prevAutoFillBoard && !autoFillBoard)
		{
			autoFiller.Cancelled = true;
			autoFiller.StopProcessing();
		}

		if (enterWordsManually)
		{
			if (selectedCellRow != -1 && selectedCellCol != -1)
			{
				GUI.Label(labelRect, string.Format("Cell: {0} {1}", cells[startCellRow][startCellCol].number, selectedAcross ? "Across" : "Down"));
				labelRect.y += 18;
				GUI.Label(labelRect, string.Format("Length: {0}", selectedLength));
				labelRect.y += 18;
				GUI.Label(labelRect, "Enter Word:");
				labelRect.y += 18;
				manuallyTypedWord = GUI.TextField(labelRect, manuallyTypedWord).ToUpper();
				labelRect.y += 20;

				GUI.enabled = wasGUIEnabled && manuallyTypedWord.Length >= selectedLength;

				if (GUI.Button(labelRect, "Place Word On Grid!"))
				{
					PlaceWordOnSelectedCell(manuallyTypedWord);
					manuallyTypedWord = "";
				}

				GUI.enabled = wasGUIEnabled;
			}
			else
			{
				GUI.Label(labelRect, "Select a grid cell");
			}
		}
		else if (autoFillBoard)
		{
			labelRect.y += 10;

			GUIStyle labelStyle	= new GUIStyle(GUI.skin.label);

			labelStyle.fontStyle = FontStyle.Bold;

			string dots = "";

			if (autoFiller.IsProcessing)
			{
				for (int i = 0; i <= dotCount; i++)
				{
					dots += ".";
				}
			}

			GUI.Label(labelRect, autoFiller.IsProcessing ? "Status: Filling Crossword" + dots : (autoFillerFailed ? "Status: Failed" : "Status: ---"));

			GUI.enabled = wasGUIEnabled && !autoFiller.IsProcessing;

			labelRect.y += 25;
			GUI.Label(labelRect, "Auto Generate Blocks:", labelStyle);
			labelRect.y += 16;
			GUI.Label(labelRect, "Max Neightbour Count:");
			labelRect.y += 17;
			maxNeighbourCount = EditorGUI.IntField(labelRect, maxNeighbourCount);
			labelRect.y += 23;
			noSquares = EditorGUI.ToggleLeft(labelRect, "No Squares", noSquares);
			labelRect.y += 20;

			if (GUI.Button(labelRect, "Auto Generate Blocks") && !autoFiller.IsProcessing)
			{
				ClearGrid();

				autoFillerFailed = false;

				List<List<bool>> blocks = autoFiller.GenerateRandomBlocks(wordDictionary, gridSize, maxNeighbourCount, noSquares);

				// First set the isBlock flag on all blocks
				for (int i = 0; i < gridSize; i++)
				{
					for (int j = 0; j < gridSize; j++)
					{
						cells[i][j].isBlock = blocks[i][j];
					}
				}

				number = 1;

				// Now go through and set all the other important things
				for (int i = 0; i < gridSize; i++)
				{
					for (int j = 0; j < gridSize; j++)
					{
						Cell cell			= cells[i][j];
						bool isAcross		= !cell.isBlock && (j == 0 || cells[i][j - 1].isBlock);
						bool isDown			= !cell.isBlock && (i == 0 || cells[i - 1][j].isBlock);
						bool isNumberedCell	= isAcross || isDown;

						cell.isNumbered		= isNumberedCell;
						cell.number			= isNumberedCell ? number : 0;
						cell.startRowAcross	= GetStartRow(i, j, true);
						cell.startColAcross	= GetStartCol(i, j, true);
						cell.acrossCount	= CountLetters(cell.startRowAcross, cell.startColAcross, 0, 1);
						cell.startRowDown	= GetStartRow(i, j, false);
						cell.startColDown	= GetStartCol(i, j, false);
						cell.downCount		= CountLetters(cell.startRowDown, cell.startColDown, 1, 0);

						if (isNumberedCell)
						{
							number++;
						}
					}
				}
			}

			GUI.enabled = wasGUIEnabled;

			labelRect.y += 40;
			GUI.Label(labelRect, "Auto Fill Crossword:", labelStyle);
			labelRect.y += 20;

			if (!autoFiller.IsProcessing)
			{
				if (GUI.Button(labelRect, "Auto Fill Crossword"))
				{
					autoFillerFailed = false;
					ClearCharacters();
					autoFiller.StartFindingWords(wordDictionary, gridSize, cells);
				}
			}
			else
			{
				if (GUI.Button(labelRect, "Stop Auto Filler"))
				{
					autoFiller.Cancelled = true;
					autoFiller.StopProcessing();
				}
			}

			labelRect.y += 30;

			Rect boxRect = new Rect(labelRect);

			boxRect.height			= 100;
			labelStyle.wordWrap		= true;
			labelStyle.fontStyle	= FontStyle.Normal;

			GUI.Label(boxRect, "NOTE:\nAuto filling a crossword will clear all words already placed. Auto filling could take a few seconds or up to a couple minutes.", labelStyle);
		}
		else
		{
			GUI.Label(labelRect, wordFinder.IsProcessing ? string.Format("Status: {0}% Complete", Mathf.RoundToInt(wdCompletePercent * 100)) : "Status: ---");
			labelRect.y += 18;
			GUI.Label(labelRect, "Selected Word: " + (string.IsNullOrEmpty(selectedWord) ? "---" : selectedWord));
			labelRect.y += 18;

			GUI.enabled = wasGUIEnabled && !string.IsNullOrEmpty(selectedWord);

			if (GUI.Button(labelRect, "Place Word On Grid!"))
			{
				PlaceWordOnSelectedCell(selectedWord);
			}

			GUI.enabled = wasGUIEnabled;

			wordListRect.y += 90;
			wordListRect.height -= 90;

			GUI.Box(wordListRect, "");

			// Adjust the size of the Rect slightly so the buttons dont overlap the border of the board when scrolling
			wordListRect.y		+= 1;
			wordListRect.height	-= 2f;

			// Begin the word list scroll view
			wordListScrollPosition = GUI.BeginScrollView(wordListRect, wordListScrollPosition, new Rect(0, 0, wordListRect.width - ScrollBarWidth, wordListViewportHeight));

			wordListViewportHeight = buttonPadding * 2f;

			// Draw all the items in the list
			for (int i = 0; i < possibleWords.Count; i++)
			{
				string possibleWord = possibleWords[i];

				wordListViewportHeight += buttonHeight + ((i != 0) ? buttonSpacing : 0);

				Rect position = new Rect(buttonPadding, 5 + i * buttonHeight + i * buttonSpacing, wordListRect.width - buttonPadding * 2f - ScrollBarWidth, buttonHeight);

				bool toggled = GUI.Toggle(position, possibleWord == selectedWord, possibleWord, toggleButtonStyle);

				if (toggled)
				{
					selectedWord = possibleWord;
				}
			}

			// End the word list scroll view
			GUI.EndScrollView();
		}

		/********************************************************/
		/* Draw the remove/clear below the grid                 */
		/********************************************************/

		GUILayout.Space(1);

		EditorGUILayout.BeginHorizontal();

		GUILayout.Space(9);

		GUI.enabled = wasGUIEnabled && selectedCellRow >= 0 && selectedCellCol >= 0;

		if (GUILayout.Button("Remove Word", GUILayout.Width(100), GUILayout.Height(ButtonHeight)))
		{
			RemoveSelectedWord();
		}

		if (GUILayout.Button("Remove Letters", GUILayout.Width(100), GUILayout.Height(ButtonHeight)))
		{
			RemoveSelectedLetters();
		}

		if (GUILayout.Button("Remove Letter", GUILayout.Width(100), GUILayout.Height(ButtonHeight)))
		{
			RemoveSelectedLetter();
		}

		GUI.enabled = wasGUIEnabled;

		if (GUILayout.Button("Clear Letters", GUILayout.Width(100), GUILayout.Height(ButtonHeight)))
		{
			ClearCharacters();
			ClearSelected();
		}

		if (GUILayout.Button("Clear Grid", GUILayout.Width(100), GUILayout.Height(ButtonHeight)))
		{
			ClearGrid();
		}

		EditorGUILayout.EndHorizontal();
	}

	private void DrawGridInfo()
	{
		int totalCharacters		= 0;
		int totalWords			= 0;
		int totalAcrossWords	= 0;
		int totalDownWords		= 0;

		Dictionary<int, int> wordLengthCount = new Dictionary<int, int>();

		for (int i = 2; i <= gridSize; i++)
		{
			wordLengthCount.Add(i, 0);
		}

		for (int i = 0; i < cells.Count; i++)
		{
			for (int j = 0; j < cells[i].Count; j++)
			{
				Cell cell = cells[i][j];

				if (!cell.isBlock)
				{
					totalCharacters++;
				}

				if (cell.isNumbered)
				{
					if (i == cell.startRowAcross && j == cell.startColAcross && cell.acrossCount > 1)
					{
						totalWords++;
						totalAcrossWords++;
						wordLengthCount[cell.acrossCount]++;
					}

					if (i == cell.startRowDown && j == cell.startColDown && cell.downCount > 1)
					{
						totalWords++;
						totalDownWords++;
						wordLengthCount[cell.downCount]++;
					}
				}
			}
		}

		EditorGUILayout.LabelField("Grid Information", EditorStyles.boldLabel);

		DrawLine();

		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Total Characters: " + totalCharacters);
		EditorGUILayout.LabelField("Total Words: " + totalWords);
		EditorGUILayout.LabelField("Total Across Words: " + totalAcrossWords);
		EditorGUILayout.LabelField("Total Down Words: " + totalDownWords);

		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Word Lengths:", EditorStyles.boldLabel);

		EditorGUILayout.BeginHorizontal();

		for (int i = 0; i <= gridSize - 2; i++)
		{
			int col		= i % 3;
			int row		= Mathf.FloorToInt((float)i / 3f);
			int number	= 2 + row * 3 + col;

			if (i != 0 && col == 0)
			{
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
			}

			EditorGUILayout.LabelField(string.Format("Length {0} : {1}", number, wordLengthCount[number]), GUILayout.Width(100f));
		}

		EditorGUILayout.EndHorizontal();
	}

	private void DrawClues()
	{
		GUIStyle popupStyle = new GUIStyle(EditorStyles.popup);
		popupStyle.fixedHeight = 19;

		EditorGUILayout.LabelField("Clues", EditorStyles.boldLabel);

		DrawLine();

		EditorGUILayout.Space();

		List<Cell> acrossWords	= new List<Cell>();
		List<Cell> downWords	= new List<Cell>();

		for (int i = 0; i < cells.Count; i++)
		{
			for (int j = 0; j < cells[i].Count; j++)
			{
				Cell cell = cells[i][j];

				if (cell.hasAcrossWord)
				{
					acrossWords.Add(cell);
				}

				if (cell.hasDownWord)
				{
					downWords.Add(cell);
				}
			}
		}

		showAcrossClues = EditorGUILayout.Foldout(showAcrossClues, "ACROSS - " + acrossWords.Count + " words");

		if (showAcrossClues)
		{
			EditorGUI.indentLevel++;

			if (acrossWords.Count > 0)
			{
				for (int i = 0; i < acrossWords.Count; i++)
				{
					Cell			cell	= acrossWords[i];
					List<string>	clues	= wordDictionary.GetClues(cell.acrossWord);

					EditorGUILayout.LabelField(cell.number + " : " + cell.acrossWord);

					EditorGUILayout.BeginHorizontal();

					GUILayout.Space(20);

					if (clues.Count > 0)
					{
						cell.acrossUseCustomClue = GUILayout.Toggle(cell.acrossUseCustomClue, "Custom", GUILayout.Width(60));
					}

					if (clues.Count == 0 || cell.acrossUseCustomClue)
					{
						cell.acrossCustomClue = EditorGUILayout.TextField(cell.acrossCustomClue);
					}
					else
					{
						cell.acrossSelectedClue = EditorGUILayout.Popup(cell.acrossSelectedClue, clues.ToArray(), popupStyle);
					}

					EditorGUILayout.EndHorizontal();

					EditorGUILayout.Space();
				}
			}
			else
			{
				EditorGUILayout.LabelField("No across words");
			}

			EditorGUI.indentLevel--;
		}

		showDownClues = EditorGUILayout.Foldout(showDownClues, "DOWN - " + downWords.Count + " words");

		if (showDownClues)
		{
			EditorGUI.indentLevel++;

			if (downWords.Count > 0)
			{
				for (int i = 0; i < downWords.Count; i++)
				{
					Cell			cell	= downWords[i];
					List<string>	clues	= wordDictionary.GetClues(cell.downWord);

					EditorGUILayout.LabelField(cell.number + " : " + cell.downWord);

					EditorGUILayout.BeginHorizontal();

					GUILayout.Space(20);

					if (clues.Count > 0)
					{
						cell.downUseCustomClue = GUILayout.Toggle(cell.downUseCustomClue, "Custom", GUILayout.Width(60));
					}

					if (clues.Count == 0 || cell.downUseCustomClue)
					{
						cell.downCustomClue = EditorGUILayout.TextField(cell.downCustomClue);
					}
					else
					{
						cell.downSelectedClue = EditorGUILayout.Popup(cell.downSelectedClue, clues.ToArray(), popupStyle);
					}

					EditorGUILayout.EndHorizontal();

					EditorGUILayout.Space();
				}
			}
			else
			{
				EditorGUILayout.LabelField("No down words");
			}

			EditorGUI.indentLevel--;
		}
	}

	private void DrawLine()
	{
		GUIStyle lineStyle			= new GUIStyle();
		lineStyle.normal.background	= LineTexture;

		GUILayout.BeginHorizontal();
		GUILayout.Space(WindowPadding);
		GUILayout.BeginVertical(lineStyle, GUILayout.Width(toolsWindowWidth));
		GUILayout.Space(1);
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
	}

	private void DrawCreateGridButton()
	{
		if (GUILayout.Button("Create Crossword File", GUILayout.Height(25), GUILayout.Width(toolsWindowWidth)))
		{
			bool blankCells = false;

			// Check if there are any blank cells
			for (int i = 0; i < cells.Count; i++)
			{
				for (int j = 0; j < cells[i].Count; j++)
				{
					if (!cells[i][j].isBlock && cells[i][j].character == ' ')
					{
						blankCells = true;
						break;
					}
				}

				if (blankCells)
				{
					break;
				}
			}

			if (blankCells)
			{
				EditorUtility.DisplayDialog("Blank Cells", "Some of the cells in the crossword do not have a character on them.", "Okay");
			}
			else if (gridName.Contains("~"))
			{
				EditorUtility.DisplayDialog("Invalid Grid Name", "Grid Name cannot contain the '~' character, please remove this character from the Grid Name.", "Okay");
			}
			else
			{
				CreateGridFile();
			}
		}
	}

	#endregion

	#endregion

	private void CreateGridFile()
	{
		if (gridSize == 0)
		{
			return;
		}

		string gridFilePath = CBUtilities.CrosswordFileDirectoryFullPath + "/" + gridName + ".txt";

		if (System.IO.File.Exists(gridFilePath))
		{
			bool okay = EditorUtility.DisplayDialog("File Already Exists", string.Format("Grid file \"{0}\" already exists. Do you want to overwrite it?", gridName), "Overwrite", "Cancel");

			if (!okay)
			{
				return;
			}
		}

		string contents = "";

		contents += gridName + "\n";
		contents += gridSize + "\n";

		List<int>		acrossCluesNumber	= new List<int>();
		List<string> 	acrossCluesWord		= new List<string>();
		List<string>	acrossCluesClue		= new List<string>();
		List<int>		downCluesNumber		= new List<int>();
		List<string>	downCluesWord		= new List<string>();
		List<string>	downCluesClue		= new List<string>();

		for (int i = 0; i < cells.Count; i++)
		{
			for (int j = 0; j < cells[i].Count; j++)
			{
				Cell cell = cells[i][j];

				// Lets just add everything
				contents += (cell.isBlock ? "1" : "0") + "\t";		// 0
				contents += (cell.isNumbered ? "1" : "0") + "\t";	// 1
				contents += (cell.number) + "\t";					// 2
				contents += (cell.character) + "\t";				// 3
				contents += (cell.startRowAcross) + "\t";			// 4
				contents += (cell.startColAcross) + "\t";			// 5
				contents += (cell.acrossCount) + "\t";				// 6
				contents += (cell.hasAcrossWord ? "1" : "0") + "\t";// 7
				contents += (cell.acrossWord) + "\t";				// 8
				contents += (cell.startRowDown) + "\t";				// 9
				contents += (cell.startColDown) + "\t";				// 10
				contents += (cell.downCount) + "\t";				// 11
				contents += (cell.hasDownWord ? "1" : "0") + "\t";	// 12
				contents += (cell.downWord);						// 13
				contents += "\n";

				if (cell.hasAcrossWord)
				{
					List<string> clues = wordDictionary.GetClues(cell.acrossWord);

					string clue = (cell.acrossUseCustomClue || clues.Count == 0) ? cell.acrossCustomClue : clues[cell.acrossSelectedClue];

					acrossCluesNumber.Add(cell.number);
					acrossCluesWord.Add(cell.acrossWord);
					acrossCluesClue.Add(clue);
				}

				if (cell.hasDownWord)
				{
					List<string> clues = wordDictionary.GetClues(cell.downWord);

					string clue = (cell.downUseCustomClue || clues.Count == 0) ? cell.downCustomClue : clues[cell.downSelectedClue];

					downCluesNumber.Add(cell.number);
					downCluesWord.Add(cell.downWord);
					downCluesClue.Add(clue);
				}
			}
		}

		// Add the number of across words/clues
		contents += acrossCluesNumber.Count + "\n";

		// Add all the across words and their clues
		for (int i = 0; i < acrossCluesNumber.Count; i++)
		{
			int		number	= acrossCluesNumber[i];
			string	word	= acrossCluesWord[i];
			string	clue	= acrossCluesClue[i];

			// Use tabs this time incase the clue contains special charaters
			contents += number + "\t" + word + "\t" + clue + "\n";
		}

		// Add the number of down words/clues
		contents += downCluesNumber.Count + "\n";

		// Add all the down words and their clues
		for (int i = 0; i < downCluesNumber.Count; i++)
		{
			int		number	= downCluesNumber[i];
			string	word	= downCluesWord[i];
			string	clue	= downCluesClue[i];

			// Use tabs this time incase the clue contains special charaters
			contents += number + "\t" + word + "\t" + clue + "\n";
		}

		System.IO.File.WriteAllText(gridFilePath, contents);

		AssetDatabase.Refresh();

		EditorUtility.DisplayDialog("Grid File Created", string.Format("The grid file \"{0}\" was successfully created and placed in {1}", gridName, CBUtilities.CrosswordFileDirectoryFullPath), "Okay");
	}

	private void ParseAutoFillerCompletedBoard(string completedBoard)
	{
		string[] lines = completedBoard.Split('\n');

		// Clear the grid since all info will be coming from the string
		ClearGrid();

		int number = 1;

		// Parse the file characters
		for (int i = 0; i < gridSize; i++)
		{
			for (int j = 0; j < gridSize; j++)
			{
				char character = lines[i][j];

				bool isBlock		= character == '#';
				bool isAcross		= !isBlock && (j == 0 || cells[i][j - 1].isBlock);
				bool isDown			= !isBlock && (i == 0 || cells[i - 1][j].isBlock);
				bool isNumberedCell	= isAcross || isDown;

				Cell cell = cells[i][j];

				cell.isBlock		= isBlock;
				cell.isNumbered		= isNumberedCell;
				cell.number			= isNumberedCell ? number++ : 0;
				cell.character		= isBlock ? ' ' : character;

				cell.hasAcrossWord	= !isBlock && isAcross;
				cell.hasDownWord	= !isBlock && isDown;
			}
		}

		// Now that all the characters have been placed we need to set all the word variables
		for (int i = 0; i < gridSize; i++)
		{
			for (int j = 0; j < gridSize; j++)
			{
				Cell cell = cells[i][j];

				cell.startRowAcross	= GetStartRow(i, j, true);
				cell.startColAcross	= GetStartCol(i, j, true);
				cell.acrossCount	= CountLetters(cell.startRowAcross, cell.startColAcross, 0, 1);
				cell.startRowDown	= GetStartRow(i, j, false);
				cell.startColDown	= GetStartCol(i, j, false);
				cell.downCount		= CountLetters(cell.startRowDown, cell.startColDown, 1, 0);

				if (cell.hasAcrossWord)
				{
					if (cell.acrossCount == 1)
					{
						cell.hasAcrossWord = false;
					}
					else
					{
						string word = "";

						for (int k = j; k < j + cell.acrossCount; k++)
						{
							word += cells[i][k].character;
						}

						cell.acrossWord = word;

						int numClues = wordDictionary.GetClues(word).Count;

						cell.acrossSelectedClue = UnityEngine.Random.Range(0, numClues);
					}
				}

				if (cell.hasDownWord)
				{
					if (cell.downCount == 1)
					{
						cell.hasDownWord = false;
					}
					else
					{
						string word = "";

						for (int k = i; k < i + cell.downCount; k++)
						{
							word += cells[k][j].character;
						}

						cell.downWord = word;

						int numClues = wordDictionary.GetClues(word).Count;

						cell.downSelectedClue = UnityEngine.Random.Range(0, numClues);
					}
				}
			}
		}
	}
}