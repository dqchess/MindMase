using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Used by Crossword Builder when we need to check which words the user can place on the selected cells.
/// </summary>
public class CBWordFinder
{
	#region Member Variables

	private CBWordFinderWorker					worker;
	private bool								waitForWorkerToStop;
	private CBWordDict							wordDictionary;
	private List<string>						wordsToTry;
	private List<List<CrosswordBuilder.Cell>>	cells;
	private List<string>						usedWords;
	private int									startRow;
	private int									startCol;
	private int									length;
	private bool								isAcross;

	private static object	possibleWordsLock	= new object();
	private List<string>	possibleWords		= new List<string>();
	private List<int>		possibleWordsCount	= new List<int>();

	#endregion

	#region Properties

	public bool IsProcessing { get; set; }

	#endregion

	#region Public Methods

	public List<string> GetPossibleWords()
	{
		lock (possibleWordsLock)
		{
			return new List<string>(possibleWords);
		}
	}

	public float CheckProgress()
	{
		float progress = 0f;

		if (IsProcessing)
		{
			// Check if the worker has stopped
			if (worker.Stopped)
			{
				worker			= null;
				progress		= 1f;
				IsProcessing	= false;

				// If we were waiting for it to stop, then start it back up with the new values
				if (waitForWorkerToStop)
				{
					waitForWorkerToStop = false;
					StartProcessing();
				}
			}
			else
			{
				progress = worker.Progress;
			}
		}

		return progress;
	}

	public void StartFindingWords(
		CBWordDict							wordDictionary,
		List<string>						wordsToTry,
		List<List<CrosswordBuilder.Cell>>	cells,
		List<string>						usedWords,
		int									startRow,
		int									startCol,
		int									length,
		bool								isAcross)
	{
		this.wordDictionary		= wordDictionary;
		this.wordsToTry			= wordsToTry;
		this.cells				= cells;
		this.usedWords			= usedWords;
		this.startRow			= startRow;
		this.startCol			= startCol;
		this.length				= length;
		this.isAcross			= isAcross;

		if (IsProcessing)
		{
			waitForWorkerToStop = true;
			worker.Stop();
		}
		else
		{
			StartProcessing();
		}
	}

	public void StopProcessing()
	{
		if (IsProcessing)
		{
			worker.Stop();
		}
	}

	#endregion

	#region Private Methods

	private void StartProcessing()
	{
		possibleWords.Clear();
		possibleWordsCount.Clear();

		IsProcessing = true;

		worker					= new CBWordFinderWorker();
		worker.WordDictionary	= wordDictionary;
		worker.WordsToTry		= wordsToTry;
		worker.Cells			= cells;
		worker.AddPossibleWord	= AddPossibleWord;
		worker.StartRow			= startRow;
		worker.StartCol			= startCol;
		worker.IsAcross			= isAcross;
		worker.CellCount		= length;
		worker.UsedWords		= usedWords;

		new Thread(new ThreadStart(worker.Run)).Start();
	}

	private void AddPossibleWord(string word, int count)
	{
		lock (possibleWordsLock)
		{
			int insertIndex = 0;

			for (int i = 0; i < possibleWordsCount.Count; i++)
			{
				if (count > possibleWordsCount[i])
				{
					insertIndex = i;
					break;
				}
			}

			possibleWords.Insert(insertIndex, word);
			possibleWordsCount.Insert(insertIndex, count);
		}
	}

	#endregion
}
