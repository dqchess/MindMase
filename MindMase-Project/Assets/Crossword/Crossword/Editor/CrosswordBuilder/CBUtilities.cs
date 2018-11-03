using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class CBUtilities
{
	#region Member Variables

	public const string	AssetName					= "Crossword";	// Name of this asset, used to create the directory where editor genereated files are saved
	public const string DataDirectory				= "CrosswordBuilder";
	public const string BuilderDataDirectoryPath	= AssetName + "/" + DataDirectory + "/Data";
	public const string CrosswordFileDirectoryPath	= AssetName + "/" + DataDirectory + "/CrosswordFiles";
	public const int	MaxLengthForFullWordMapping	= 8;				// When generating the pre-processed word dictionary, this is the max word length for mapping every letter combination of words
	public const int	MaxCheckDepth				= 3;

	#endregion

	#region Properties

	public static string DataDirectoryFullPath
	{
		get
		{
			string path = Application.dataPath + "/" + BuilderDataDirectoryPath;

			if (!System.IO.Directory.Exists(path))
			{
				System.IO.Directory.CreateDirectory(path);
			}

			return path;
		}
	}

	public static string CrosswordFileDirectoryFullPath
	{
		get
		{
			string path = Application.dataPath + "/" + CrosswordFileDirectoryPath;

			if (!System.IO.Directory.Exists(path))
			{
				System.IO.Directory.CreateDirectory(path);
			}

			return path;
		}
	}

	public static string	PreProcessedFileFullPath	{ get { return DataDirectoryFullPath + "/letter_dictionary.txt"; } }
	public static string	CluesFileFullPath			{ get { return DataDirectoryFullPath + "/clues.txt"; } }
	public static bool		PreProcessedFileExists		{ get { return System.IO.File.Exists(CBUtilities.PreProcessedFileFullPath); } }

	#endregion

	#region Public Methods

	public static List<CrosswordBuilder.Cell> PlaceWordOnCell(string word, List<List<CrosswordBuilder.Cell>> cells, int startRow, int startCol, int length, bool across)
	{
		List<CrosswordBuilder.Cell>	blankCells	= new List<CrosswordBuilder.Cell>();
		CrosswordBuilder.Cell		startCell	= cells[startRow][startCol];

		if (across)
		{
			startCell.hasAcrossWord = true;
			startCell.acrossWord	= word;
			length					= startCell.acrossCount;
		}
		else
		{
			startCell.hasDownWord	= true;
			startCell.downWord		= word;
			length					= startCell.downCount;
		}

		for (int i = 0; i < length; i++)
		{
			int row = startRow + (across ? 0 : i);
			int col = startCol + (across ? i : 0);

			CrosswordBuilder.Cell cell = cells[row][col];

			if (cell.character == ' ')
			{
				blankCells.Add(cell);
			}

			// Set the character in the cell
			cell.character = word[i];

			// Now we need to check if this created a crossing word
			bool	crossAcross		= !across;
			int		crossRowStart	= crossAcross ? cell.startRowAcross : cell.startRowDown;
			int		crossColStart	= crossAcross ? cell.startColAcross : cell.startColDown;
			int		crossLength		= crossAcross ? cell.acrossCount : cell.downCount;

			string	crossWord		= "";
			bool	setCrossWord	= true;

			if (crossLength == 1)
			{
				setCrossWord = false;
			}
			else
			{
				for (int j = 0; j < crossLength; j++)
				{
					int crossRow = crossRowStart + (crossAcross ? 0 : j);
					int crossCol = crossColStart + (crossAcross ? j : 0);

					CrosswordBuilder.Cell crossCell = cells[crossRow][crossCol];

					if (crossCell.character == ' ')
					{
						setCrossWord = false;
						break;
					}

					crossWord += crossCell.character;
				}
			}

			if (setCrossWord)
			{
				CrosswordBuilder.Cell crossStartCell = cells[crossRowStart][crossColStart];

				if (crossAcross)
				{
					crossStartCell.hasAcrossWord	= true;
					crossStartCell.acrossWord		= crossWord;
				}
				else
				{
					crossStartCell.hasDownWord	= true;
					crossStartCell.downWord		= crossWord;
				}
			}
		}

		return blankCells;
	}

	public static void RemoveWord(List<List<CrosswordBuilder.Cell>> cells, int startRow, int startCol, int length, bool across)
	{
		bool removedOneChar = false;

		for (int i = 0; i < length; i++)
		{
			int row = startRow + (across ? 0 : i);
			int col = startCol + (across ? i : 0);

			CrosswordBuilder.Cell cell = cells[row][col];

			if (across)
			{
				CrosswordBuilder.Cell downStartCell = cells[cell.startRowDown][cell.startColDown];

				if (!downStartCell.hasDownWord)
				{
					cell.character	= ' ';
					removedOneChar	= true;
				}
			}
			else
			{
				CrosswordBuilder.Cell acrossStartCell = cells[cell.startRowAcross][cell.startColAcross];

				if (!acrossStartCell.hasAcrossWord)
				{
					cell.character	= ' ';
					removedOneChar	= true;
				}
			}
		}

		if (removedOneChar)
		{
			CrosswordBuilder.Cell startCell = cells[startRow][startCol];

			if (across)
			{
				startCell.hasAcrossWord = false;
				startCell.acrossWord	= "";
			}
			else
			{
				startCell.hasDownWord	= false;
				startCell.downWord		= "";
			}
		}
	}

	public static void DeletePreProcessedFile()
	{
		if (PreProcessedFileExists)
		{
			System.IO.File.Delete(PreProcessedFileFullPath);
		}
	}

	public static List<List<CrosswordBuilder.Cell>> CopyCellsList(List<List<CrosswordBuilder.Cell>> cells)
	{
		List<List<CrosswordBuilder.Cell>> cellsCopy = new List<List<CrosswordBuilder.Cell>>();

		for (int i = 0; i < cells.Count; i++)
		{
			cellsCopy.Add(new List<CrosswordBuilder.Cell>());

			for (int j = 0; j < cells[i].Count; j++)
			{
				CrosswordBuilder.Cell cell		= cells[i][j];
				CrosswordBuilder.Cell cellCopy	= new CrosswordBuilder.Cell();

				cellCopy.isBlock		= cell.isBlock;
				cellCopy.isNumbered		= cell.isNumbered;
				cellCopy.character		= cell.character;

				cellCopy.startRowAcross	= cell.startRowAcross;
				cellCopy.startColAcross	= cell.startColAcross;
				cellCopy.acrossCount	= cell.acrossCount;
				cellCopy.hasAcrossWord	= cell.hasAcrossWord;

				cellCopy.startRowDown	= cell.startRowDown;
				cellCopy.startColDown	= cell.startColDown;
				cellCopy.downCount		= cell.downCount;
				cellCopy.hasDownWord	= cell.hasDownWord;

				cellsCopy[i].Add(cellCopy);
			}
		}

		return cellsCopy;
	}

	public static void PrintCellsList(List<List<CrosswordBuilder.Cell>> cells)
	{
		string print = "*** Board ***";

		for (int i = 0; i < cells.Count; i++)
		{
			print += "\n";

			for (int j = 0; j < cells[i].Count; j++)
			{
				print += cells[i][j].isBlock ? '#' : cells[i][j].character;
			}
		}

		Debug.Log(print);
	}

	#endregion

	#region Protected Methods

	#endregion

	#region Private Methods

	#endregion
}
