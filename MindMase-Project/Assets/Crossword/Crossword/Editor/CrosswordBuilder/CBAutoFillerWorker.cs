using UnityEditor;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

public class CBAutoFillerWorker : Worker
{
	#region Classes

	public class CellInfo
	{
		public Cell cell;
		public bool isAcross;

		public CellInfo(Cell cell, bool isAcross)
		{
			this.cell		= cell;
			this.isAcross	= isAcross;
		}
	}

	public class Cell
	{
		public int	i;
		public int	j;
		public char	character;
		public bool	isBlock;

		public int acrossLen;
		public int downLen;

		public Cell acrossStart;
		public Cell downStart;

		public bool hasDownWord;
		public bool hasAcrossWord;

		public Cell(int i, int j)
		{
			this.i = i;
			this.j = j;
		}
	}

	#endregion

	#region Member Variables

	private System.Random	rng;
	private List<string>	usedWords;

	private List<List<bool>> blocks;
	private List<List<bool>> marks;
	private List<List<Cell>> cells;

	#endregion

	#region Properties

	private static double SystemTimeInMilliseconds { get { return (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalMilliseconds; } }

	public CBWordDict		WordDictionary	{ get; set; }
	public List<List<bool>> Blocks			{ get; set; }
	public string			CompletedBoard	{ get; private set; }
	public int				BoardSize		{ get; set; }

	#endregion

	#region Protected Methods

	protected override void Begin()
	{
		blocks	= Blocks;
		rng		= new Random();
	}

	protected override void DoWork()
	{
		usedWords	= new List<string>();
		marks		= new List<List<bool>>();
		cells		= new List<List<Cell>>();

		for (int i = 0; i < BoardSize; i++)
		{
			marks.Add(new List<bool>());
			cells.Add(new List<Cell>());

			for (int j = 0; j < BoardSize; j++)
			{
				marks[i].Add(false);
				cells[i].Add(new Cell(i, j));
			}
		}

		ResetCells();

		Cell startCell		= null;
		bool startAcross	= true;

		for (int r = 0; r < BoardSize; r++)
		{
			for (int c = 0; c < BoardSize; c++)
			{
				Cell cell = cells[r][c];

				if (!cell.isBlock && cell.acrossLen > 1)
				{
					startCell	= cells[r][c];
					startAcross	= true;
					break;
				}
				else if (!cell.isBlock && cell.downLen > 1)
				{
					startCell	= cells[r][c];
					startAcross	= false;
					break;
				}
			}

			if (startCell != null)
			{
				break;
			}
		}

		if (FillCells(new List<CellInfo>(){ new CellInfo(startCell, startAcross) }))
		{
			CompletedBoard = PrintCells();
		}
		else
		{
			CompletedBoard = "";
		}	

		Stop();
	}

	public List<List<bool>> GenerateRandomBlocks(int maxNeighbourCount, bool noSquares)
	{
		if (rng == null)
		{
			rng		= new Random();
			blocks	= new List<List<bool>>();
			marks	= new List<List<bool>>();

			for (int i = 0; i < BoardSize; i++)
			{
				marks.Add(new List<bool>());
				blocks.Add(new List<bool>());

				for (int j = 0; j < BoardSize; j++)
				{
					marks[i].Add(false);
					blocks[i].Add(false);
				}
			}
		}

		List<int[]> cellPositions = new List<int[]>();

		for (int i = 0; i < BoardSize; i++)
		{
			for (int j = 0; j < BoardSize; j++)
			{
				int[] pos = new int[2];

				pos[0] = i;
				pos[1] = j;

				if (cellPositions.Count == 0)
				{
					cellPositions.Add(pos);
				}
				else
				{
					cellPositions.Insert(rng.Next(0, cellPositions.Count), pos);
				}
			}
		}

		float middle = (float)BoardSize / 2f;

		for (int i = 0; i < cellPositions.Count; i++)
		{
			int iStart = cellPositions[i][0];
			int jStart = cellPositions[i][1];

			if (blocks[iStart][jStart])
			{
				continue;
			}

			int mirroredI = (int)(middle + (middle - (float)iStart)) - 1;
			int mirroredJ = (int)(middle + (middle - (float)jStart)) - 1;

			blocks[mirroredI][mirroredJ] = true;

			int a = CountLetters(iStart, jStart, 1, 0);
			int b = CountLetters(iStart, jStart, 0, 1);
			int c = CountLetters(iStart, jStart, -1, 0);
			int d = CountLetters(iStart, jStart, 0, -1);

			if ((a == 1 || a > 3) &&
				(b == 1 || b > 3) &&
				(c == 1 || c > 3) &&
				(d == 1 || d > 3) &&
				(!noSquares || !DoesCreateSquare(iStart, jStart)))
			{
				blocks[iStart][jStart] = true;

				if (!CheckSingleRegion() || CheckBlockNeighbourCount(iStart, jStart, true) > maxNeighbourCount)
				{
					blocks[iStart][jStart]			= false;
					blocks[mirroredI][mirroredJ]	= false;
				}
			}
			else
			{
				blocks[mirroredI][mirroredJ] = false;
			}
		}

		return blocks;
	}

	#endregion

	#region Private Methods

	private bool FillCells(List<CellInfo> startCells)
	{
		if (Stopping)
		{
			return false;
		}

		if (startCells.Count == 0)
		{
			return true;
		}

		CellInfo startCell = startCells[0];

		if ((startCell.isAcross && startCell.cell.hasAcrossWord) ||
			(!startCell.isAcross && startCell.cell.hasDownWord))
		{
			List<CellInfo> newStartCells = new List<CellInfo>(startCells);

			newStartCells.RemoveAt(0);

			return FillCells(newStartCells);
		}

		int		iStart 		= startCell.cell.i;
		int		jStart		= startCell.cell.j;
		int		len			= startCell.isAcross ? startCell.cell.acrossLen : startCell.cell.downLen;
		bool	isAcross	= startCell.isAcross;

		List<string> possibleWords = new List<string>(WordDictionary.GetPossibleWords(cells, iStart, jStart, len, isAcross));

		if (possibleWords != null)
		{
			for (int i = 0; i < possibleWords.Count; i++)
			{
				if (Stopping)
				{
					return false;
				}

				// Get a random word to try from the list of possible words
				int		randIndex = rng.Next(i, possibleWords.Count);
				string	wordToTry = possibleWords[randIndex];

				// Swap the words so if it does fit we wont pick it again in the next iteration of the for loop
				possibleWords[randIndex]	= possibleWords[i];
				possibleWords[i]			= wordToTry;

				if (usedWords.Contains(wordToTry))
				{
					continue;
				}

				List<CellInfo>	newStartCells	= new List<CellInfo>();
				List<Cell>		changedCells	= new List<Cell>();

				bool canWordFit = true;

				// Place the words characters in the cells
				for (int j = 0; j < len; j++)
				{
					int iCell = iStart + (isAcross ? 0 : j);
					int jCell = jStart + (isAcross ? j : 0);

					Cell cell = cells[iCell][jCell];

					if (cell.character == ' ')
					{
						cell.character = wordToTry[j];

						changedCells.Add(cell);

						// If the crossing word is only length one then we dont need to check it
						if ((isAcross && cell.downStart.downLen == 1) ||
							(!isAcross && cell.acrossStart.acrossLen == 1))
						{
							continue;
						}

						newStartCells.Add(new CellInfo(isAcross ? cell.downStart : cell.acrossStart, !isAcross));
						
						// Now check that there is aleast on possible word for the crossing cells
						Cell	crossStart	= isAcross ? cell.downStart : cell.acrossStart;
						int		crossLen	= isAcross ? crossStart.downLen : crossStart.acrossLen;
						
						List<string> possibleCrossWords = WordDictionary.GetPossibleWords(cells, crossStart.i, crossStart.j, crossLen, !isAcross);
						
						if (possibleCrossWords == null || possibleCrossWords.Count == 0)
						{
							canWordFit = false;
							break;
						}
					}
				}

				if (canWordFit)
				{
					if (isAcross)
					{
						startCell.cell.hasAcrossWord = true;
					}
					else
					{
						startCell.cell.hasDownWord = true;
					}

					// Add the start cells that still need words on them
					for (int j = 1; j < startCells.Count; j++)
					{
						newStartCells.Add(startCells[j]);
					}

					usedWords.Add(wordToTry);

					// Try filling the rest of the cells
					if (FillCells(newStartCells))
					{
						return true;
					}

					usedWords.RemoveAt(usedWords.Count - 1);

					if (isAcross)
					{
						startCell.cell.hasAcrossWord = false;
					}
					else
					{
						startCell.cell.hasDownWord = false;
					}
				}

				// Set the changed cells character back to blank
				for (int j = 0; j < changedCells.Count; j++)
				{
					changedCells[j].character = ' ';
				}
			}
		}

		// Non of the possible words worked
		return false;
	}

	private bool CheckSingleRegion()
	{
		int iStart = -1;
		int jStart = -1;

		// Find any non block cell as the starting position
		for (int i = 0; i < BoardSize; i++)
		{
			for (int j = 0; j < BoardSize; j++)
			{
				if (!blocks[i][j])
				{
					iStart = i;
					jStart = j;

					break;
				}
			}

			if (iStart != -1)
			{
				break;
			}
		}

		ResetMarks();

		Mark(iStart, jStart);

		for (int i = 0; i < BoardSize; i++)
		{
			for (int j = 0; j < BoardSize; j++)
			{
				if (!marks[i][j])
				{
					return false;
				}
			}
		}

		return true;
	}

	private void Mark(int i, int j)
	{
		if (marks[i][j])
		{
			return;
		}

		marks[i][j] = true;

		if (i > 0)
		{
			Mark(i - 1, j);
		}

		if (j > 0)
		{
			Mark(i, j - 1);
		}

		if (i < BoardSize - 1)
		{
			Mark(i + 1, j);
		}

		if (j < BoardSize - 1)
		{
			Mark(i, j + 1);
		}
	}

	private bool DoesCreateSquare(int i, int j)
	{
		// ##
		// #_
		if (i > 0 && j > 0)
		{
			if (blocks[i-1][j] && blocks[i-1][j-1] && blocks[i][j-1])
			{
				return true;
			}
		}

		// ##
		// _#
		if (i > 0 && j + 1 < BoardSize)
		{
			if (blocks[i-1][j] && blocks[i-1][j+1] && blocks[i][j+1])
			{
				return true;
			}
		}

		// #_
		// ##
		if (j > 0 && i + 1 < BoardSize)
		{
			if (blocks[i][j-1] && blocks[i+1][j-1] && blocks[i+1][j])
			{
				return true;
			}
		}

		// _#
		// ##
		if (i + 1 < BoardSize && j + 1 < BoardSize)
		{
			if (blocks[i][j+1] && blocks[i+1][j+1] && blocks[i+1][j])
			{
				return true;
			}
		}

		return false;
	}

	private int CheckBlockNeighbourCount(int i, int j, bool resetMarks = false)
	{
		if (resetMarks)
		{
			ResetMarks(false);
		}

		int count = 1;

		marks[i][j] = true;

		if (i > 0 && blocks[i - 1][j] && !marks[i - 1][j])
		{
			count += CheckBlockNeighbourCount(i - 1, j);
		}

		if (i < BoardSize - 1 && blocks[i + 1][j] && !marks[i + 1][j])
		{
			count += CheckBlockNeighbourCount(i + 1, j);
		}

		if (j > 0 && blocks[i][j - 1] && !marks[i][j - 1])
		{
			count += CheckBlockNeighbourCount(i, j - 1);
		}

		if (j < BoardSize - 1 && blocks[i][j + 1] && !marks[i][j + 1])
		{
			count += CheckBlockNeighbourCount(i, j + 1);
		}

		if (i > 0 && j > 0 && blocks[i - 1][j - 1] && !marks[i - 1][j - 1])
		{
			count += CheckBlockNeighbourCount(i - 1, j - 1);
		}

		if (i > 0 && j < BoardSize - 1 && blocks[i - 1][j + 1] && !marks[i - 1][j + 1])
		{
			count += CheckBlockNeighbourCount(i - 1, j + 1);
		}

		if (i < BoardSize - 1 && j > 0 && blocks[i + 1][j - 1] && !marks[i + 1][j - 1])
		{
			count += CheckBlockNeighbourCount(i + 1, j - 1);
		}

		if (i < BoardSize - 1 && j < BoardSize - 1 && blocks[i + 1][j + 1] && !marks[i + 1][j + 1])
		{
			count += CheckBlockNeighbourCount(i + 1, j + 1);
		}

		return count;
	}

	private void ResetBlocks()
	{
		for (int i = 0; i < BoardSize; i++)
		{
			for (int j = 0; j < BoardSize; j++)
			{
				blocks[i][j] = false;
			}
		}
	}

	private void ResetMarks(bool markBlocks = true)
	{
		for (int i = 0; i < BoardSize; i++)
		{
			for (int j = 0; j < BoardSize; j++)
			{
				marks[i][j] = markBlocks ? blocks[i][j] : false;
			}
		}
	}

	private void ResetCells()
	{
		cells = new List<List<Cell>>();

		for (int i = 0; i < BoardSize; i++)
		{
			cells.Add(new List<Cell>());

			for (int j = 0; j < BoardSize; j++)
			{
				cells[i].Add(new Cell(i, j));
			}
		}

		for (int i = 0; i < BoardSize; i++)
		{
			for (int j = 0; j < BoardSize; j++)
			{
				Cell cell = cells[i][j];

				cell.character		= ' ';
				cell.isBlock		= blocks[i][j];
				cell.hasDownWord	= false;
				cell.hasAcrossWord	= false;

				if(!cell.isBlock)
				{
					cell.acrossLen	= CountLetters(i, j, 0, 1);
					cell.downLen	= CountLetters(i, j, 1, 0);

					int tempI = (CountLetters(i, j, -1, 0) - 1);
					int tempJ = (CountLetters(i, j, 0, -1) - 1);

					cell.acrossStart	= cells[i][j - tempJ];
					cell.downStart		= cells[i - tempI][j];
				}
			}
		}
	}

	private string PrintBlocks()
	{
		string print = "";

		int blockCount = 0;

		for (int k = 0; k < BoardSize; k++)
		{
			for (int j = 0; j < BoardSize; j++)
			{
				print += blocks[k][j] ? "#" : "_";

				if (blocks[k][j])
				{
					blockCount++;
				}
			}

			print += "\n";
		}

		print = "Block Count: " + blockCount + "\n" + print;

		return print;
	}

	private string PrintCells()
	{
		string print = "";

		for (int i = 0; i < BoardSize; i++)
		{
			for (int j = 0; j < BoardSize; j++)
			{
				Cell cell = cells[i][j];

				if (cell.isBlock)
				{
					print += '#';
				}
				else if (cell.character == ' ')
				{
					print += '_';
				}
				else
				{
					print += cell.character;
				}
			}

			print += "\n";
		}

		return print;
	}

	private int CountLetters(int iStart, int jStart, int iInc, int jInc)
	{
		int count = 0;

		for (int i = iStart, j = jStart; i >= 0 && i < BoardSize && j >= 0 && j < BoardSize; i += iInc, j += jInc)
		{
			if (blocks[i][j])
			{
				break;
			}

			count++;
		}

		return count;
	}

	private void AssignBlocks(string grid)
	{
		string[] lines = grid.Split('\n');

		for (int i = 0; i < lines.Length; i++)
		{
			for (int j = 0; j < lines[i].Length; j++)
			{
				if (lines[i][j] == '#')
				{
					blocks[i][j] = true;
				}
				else
				{
					blocks[i][j] = false;
				}
			}
		}
	}

	#endregion
}