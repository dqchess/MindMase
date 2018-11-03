using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

public class CBWordFinderWorker : Worker
{
	#region Properties

	public CBWordDict							WordDictionary	{ get; set; }
	public List<string>							WordsToTry		{ get; set; }
	public List<List<CrosswordBuilder.Cell>>	Cells			{ get; set; }
	public System.Action<string, int>			AddPossibleWord	{ get; set; }

	public int				StartRow 	{ get; set; }
	public int				StartCol 	{ get; set; }
	public bool				IsAcross 	{ get; set; }
	public int				CellCount	{ get; set; }
	public List<string>		UsedWords	{ get; set; }

	#endregion

	#region Protected Methods

	protected override void Begin()
	{
	}

	protected override void DoWork()
	{
		for (int i = 0; i < WordsToTry.Count; i++)
		{
			if (Stopping)
			{
				return;
			}

			int fitCount = 0;

			if (TryWord(WordsToTry[i], StartRow, StartCol, IsAcross, 1, out fitCount) && !Stopping)
			{
				AddPossibleWord(WordsToTry[i], fitCount);
			}

			Progress = (float)i / (float)WordsToTry.Count;
		}

		Stop();
	}

	#endregion

	#region Private Methods

	private bool TryWord(string word, int startRow, int startCol, bool isAcross, int depth, out int fitCount)
	{
		fitCount = 0;

		UsedWords.Add(word);

		List<CrosswordBuilder.Cell> changedCells = new List<CrosswordBuilder.Cell>();

		// Place the word, keeping track of the cells that where changed
		for (int i = 0; i < word.Length; i++)
		{
			int row = startRow + (isAcross ? 0 : i);
			int col = startCol + (isAcross ? i : 0);

			CrosswordBuilder.Cell cell = Cells[row][col];

			if (cell.character == ' ')
			{
				changedCells.Add(cell);
				cell.character = word[i];
			}
		}

		bool foundFitForAllChangedCells = true;

		// Check the perpendicular cells for all the cells what where blank, these are the ones without words on them
		for (int i = 0; i < changedCells.Count; i++)
		{
			if (Stopping)
			{
				// Threads are shutting down, we don't care about the state of Cells so just return
				return false;
			}

			// Get the start row and col of the perpendicular word
			int sr	= isAcross ? changedCells[i].startRowDown : changedCells[i].startRowAcross;
			int sc	= isAcross ? changedCells[i].startColDown : changedCells[i].startColAcross;

			CrosswordBuilder.Cell startCell = Cells[sr][sc];

			int len	= isAcross ? startCell.downCount : startCell.acrossCount;

			if (len == 1)
			{
				continue;
			}

			// Get the words that can fit in the cells given the characters that are already there
			List<string> wordsToTry = WordDictionary.GetPossibleWords(Cells, sr, sc, len, !isAcross);

			// If wordsToTry is null then there was no word that can fit in the cell
			if (wordsToTry == null)
			{
				foundFitForAllChangedCells = false;
				break;
			}

			fitCount += wordsToTry.Count;

			// If there were words that could fit in the cells and depth is 0 then dont check the individual words, just continue to the next changed cell
			if (depth >= CBUtilities.MaxCheckDepth && len <= CBUtilities.MaxLengthForFullWordMapping)
			{
				continue;
			}

			bool foundFit = false;

			for (int j = 0; j < wordsToTry.Count; j++)
			{
				if (Stopping)
				{
					// Threads are shutting down, we don't care about the state of Cells so just return
					return false;
				}

				string	wordToTry	= wordsToTry[j];
				bool	fits		= true;

				// Don't reuse words
				if (UsedWords.Contains(wordToTry))
				{
					continue;
				}

				if (len > CBUtilities.MaxLengthForFullWordMapping)
				{
					for (int k = 0; k < len; k++)
					{
						int row = sr + (!isAcross ? 0 : k);
						int col = sc + (!isAcross ? k : 0);

						CrosswordBuilder.Cell cell = Cells[row][col];

						if (cell.character != ' ' && cell.character != wordToTry[k])
						{
							fits = false;
							break;
						}
					}
				}

				if (fits)
				{
					int temp = 0;

					// Check if this word will cause the baord to be unsolvable
					if (depth >= CBUtilities.MaxCheckDepth || TryWord(wordToTry, sr, sc, !isAcross, depth + 1, out temp))
					{
						// This word doesn't cause the board to be unsolvable (alteast up to the depth that was set)
						foundFit = true;

						// If depth is one we want to count how many words fit so we can present the best ones to the user
						break;
					}
				}
			}

			// We could not fit a word without causing an unsolvable board, so the word we where given cannot be placed
			if (!foundFit)
			{
				foundFitForAllChangedCells = false;

				break;
			}
		}

		fitCount = (int)((float)fitCount / (float)changedCells.Count);

		// Set the changed cells back to blank
		for (int i = 0; i < changedCells.Count; i++)
		{
			changedCells[i].character = ' ';
		}

		UsedWords.Remove(word);

		return foundFitForAllChangedCells;
	}

	#endregion
}