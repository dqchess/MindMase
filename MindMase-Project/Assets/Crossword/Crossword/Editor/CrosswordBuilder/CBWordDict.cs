using UnityEngine;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

public class CBWordDict
{
	#region Member Variables

	private Dictionary<int, List<string>> 						loadedWords;
	private Dictionary<int, Dictionary<string, List<string>>>	letterMappings;
	private Dictionary<string, List<string>>					clues;
	private int													numberOrWords	= -1;

	private CBWordDictWorker			worker;
	private bool						waitingForWorkerToStop;
	private bool						startNewThreadWhenStopped;
	private bool						clearWhenStopped;
	private bool						deletePreProcessedFileWhenStopped;
	private string						fileText;
	private CBWordDictWorker.TypeOfWork	typeOfWork;

	#endregion

	#region Properties

	public bool							HasLoadedWords	{ get { return loadedWords != null && letterMappings != null; } }
	public CBWordDictWorker.TypeOfWork	TypeOfWork		{ get { return typeOfWork; } }
	public bool							IsProcessing	{ get; set; }

	public int NumberOfWords
	{
		get
		{
			if (numberOrWords == -1 && HasLoadedWords && !IsProcessing)
			{
				numberOrWords = 0;

				foreach (KeyValuePair<int, List<string>> pair in loadedWords)
				{
					numberOrWords += pair.Value.Count;
				}
			}

			return numberOrWords;
		}
	}

	public Dictionary<int, List<string>> LoadedWords { get { return loadedWords; } }

	#endregion

	#region Public Methods

	/// <summary>
	/// Checks the progress of the thread. After this is called, State will be set to Loaded if the worker thread has finished.
	/// </summary>
	public float CheckProgress()
	{
		float progress = 0f;

		if (IsProcessing)
		{
			// Check if we are waiting for the old thread to stop
			if (waitingForWorkerToStop)
			{
				if (worker.Stopped)
				{
					waitingForWorkerToStop	= false;
					IsProcessing			= false;
					worker					= null;

					if (deletePreProcessedFileWhenStopped)
					{
						deletePreProcessedFileWhenStopped = false;
						CBUtilities.DeletePreProcessedFile();
					}

					if (clearWhenStopped)
					{
						clearWhenStopped = false;
						Clear();
					}

					if (startNewThreadWhenStopped)
					{
						startNewThreadWhenStopped = false;
						StartProcessing();
					}
				}
			}
			else
			{
				if (worker.Stopped)
				{
					// Worker thread is done
					worker			= null;
					progress		= 1f;
					IsProcessing	= false;
				}
				else
				{
					progress = worker.Progress;
				}
			}
		}

		return progress;
	}

	/// <summary>
	/// Start processing the word file
	/// </summary>
	public void ProcessWordFile(string fileText)
	{
		Setup(fileText, CBWordDictWorker.TypeOfWork.PreProcess);
	}

	/// <summary>
	/// Starts loading the pre-processed word file
	/// </summary>
	public void LoadPreProcessedFile()
	{
		Setup(System.IO.File.ReadAllText(CBUtilities.PreProcessedFileFullPath), CBWordDictWorker.TypeOfWork.Load);
	}

	/// <summary>
	/// Stops the processing
	/// </summary>
	public void StopProcessing()
	{
		if (worker != null)
		{
			switch (typeOfWork)
			{
			case CBWordDictWorker.TypeOfWork.PreProcess:
				deletePreProcessedFileWhenStopped = true;
				break;
			case CBWordDictWorker.TypeOfWork.Load:
				clearWhenStopped = true;
				break;
			}

			waitingForWorkerToStop = true;

			worker.Stop();
		}
	}

	/// <summary>
	/// Clears the local dictionaries.
	/// </summary>
	public void Clear()
	{
		if (HasLoadedWords)
		{
			loadedWords		= null;
			letterMappings	= null;
			numberOrWords	= -1;
		}
	}

	/// <summary>
	/// Handles formating the key for word dictionary entries
	/// </summary>
	public static string AddToKey(string currentKey, int i, char c)
	{
		if (!string.IsNullOrEmpty(currentKey))
		{
			currentKey += "_";
		}

		return currentKey + i + c;
	}

	/// <summary>
	/// Determines whether this instance has words of the specified length
	/// </summary>
	public bool HasWordsOfLength(int length)
	{
		return loadedWords.ContainsKey(length);
	}

	public List<string> GetMappedWords(int len, string key)
	{
		return letterMappings.ContainsKey(len) &&  letterMappings[len].ContainsKey(key) ? letterMappings[len][key] : null;
	}

	public List<string> GetPossibleWords(List<List<CrosswordBuilder.Cell>> cells, int startRow, int startCol, int length, bool isAcross)
	{
		if (!HasWordsOfLength(length))
		{
			// Return an empty list
			return null;
		}

		string			wordDictKey		= "";
		List<string>	possibleWords	= null;

		for (int i = 0; i < length; i++)
		{
			int row = startRow + (isAcross ? 0 : i);
			int col = startCol + (isAcross ? i : 0);

			CrosswordBuilder.Cell cell = cells[row][col];

			if (cell.character != ' ')
			{
				wordDictKey = CBWordDict.AddToKey(wordDictKey, i, cell.character);

				if (length > CBUtilities.MaxLengthForFullWordMapping)
				{
					List<string> words = GetMappedWords(length, wordDictKey);

					// If there are no words with this key then there are no words that fit in the given cells
					if (words == null)
					{
						return null;
					}

					// If the new list is less than the one we have so far then lets use that one instead
					if (possibleWords == null || words.Count < possibleWords.Count)
					{
						possibleWords = words;
					}

					wordDictKey = "";
				}
			}
		}

		// If possible words is not null then we still need to get the words
		if (possibleWords == null)
		{
			// If wordDictKey is empty then all the cells are blank
			if (string.IsNullOrEmpty(wordDictKey))
			{
				// Return all words of the given length
				return loadedWords[length];
			}

			// Return the words using the key
			return GetMappedWords(length, wordDictKey);
		}

		return possibleWords;
	}

	public List<string> GetPossibleWords(List<List<CBAutoFillerWorker.Cell>> cells, int startRow, int startCol, int length, bool isAcross)
	{
		if (!HasWordsOfLength(length))
		{
			// Return an empty list
			return null;
		}

		string			wordDictKey		= "";
		List<string>	possibleWords	= null;

		for (int i = 0; i < length; i++)
		{
			int row = startRow + (isAcross ? 0 : i);
			int col = startCol + (isAcross ? i : 0);

			char character = cells[row][col].character;

			if (character != ' ')
			{
				wordDictKey = CBWordDict.AddToKey(wordDictKey, i, character);

				if (length > CBUtilities.MaxLengthForFullWordMapping)
				{
					List<string> words = GetMappedWords(length, wordDictKey);

					// If there are no words with this key then there are no words that fit in the given cells
					if (words == null)
					{
						return null;
					}

					// If the new list is less than the one we have so far then lets use that one instead
					if (possibleWords == null)
					{
						possibleWords = new List<string>(words);
					}
					else
					{
						// Remove all words from possibleWords that are not in words
						for (int j = possibleWords.Count - 1; j >= 0; j--)
						{
							if (!words.Contains(possibleWords[j]))
							{
								possibleWords.RemoveAt(j);
							}
						}
					}

					wordDictKey = "";
				}
			}
		}

		// If possible words is not null then we still need to get the words
		if (possibleWords == null)
		{
			// If wordDictKey is empty then all the cells are blank
			if (string.IsNullOrEmpty(wordDictKey))
			{
				// Return all words of the given length
				return loadedWords[length];
			}

			// Return the words using the key
			return GetMappedWords(length, wordDictKey);
		}

		return possibleWords;
	}

	public List<string> GetClues(string word)
	{
		// Return the list of clues if there are clues for the given word
		if (clues.ContainsKey(word))
		{
			return clues[word];
		}

		// Else return an empty list
		return new List<string>();
	}

	#endregion

	#region Private Methods

	private void Setup(string fileText, CBWordDictWorker.TypeOfWork typeOfWork)
	{
		Clear();

		this.typeOfWork	= typeOfWork;
		this.fileText	= fileText;

		// If there is a previous worker then we need to stop it
		if (worker != null)
		{
			// We cannot start the new thread until this one has stopped
			this.startNewThreadWhenStopped = true;

			StopProcessing();
		}
		else
		{
			StartProcessing();
		}
	}

	private void StartProcessing()
	{
		IsProcessing = true;

		// Create a new worker thread that will process the words and place them into loadedWords and letterMappings
		worker					= new CBWordDictWorker();
		worker.WordFileText		= fileText;
		worker.Type				= typeOfWork;
		
		switch (typeOfWork)
		{
		case CBWordDictWorker.TypeOfWork.PreProcess:
			worker.PreProcessedStreamWriter	= new System.IO.StreamWriter(CBUtilities.PreProcessedFileFullPath);
			worker.CluesStreamWriter		= new System.IO.StreamWriter(CBUtilities.CluesFileFullPath);
			break;
		case CBWordDictWorker.TypeOfWork.Load:
			loadedWords 	= new Dictionary<int, List<string>>();
			letterMappings	= new Dictionary<int, Dictionary<string, List<string>>>();
			clues			= new Dictionary<string, List<string>>();

			worker.Words			= loadedWords;
			worker.LetterMappings	= letterMappings;
			worker.Clues			= clues;
			worker.CluesFileText	= System.IO.File.ReadAllText(CBUtilities.CluesFileFullPath);
			break;
		}

		// Start the thread
		new Thread(new ThreadStart(worker.Run)).Start();
	}

	#endregion
}
