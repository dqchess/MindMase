using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Used by Crossword Builder when we need to check which words the user can place on the selected cells.
/// </summary>
public class CBAutoFiller
{
	#region Member Variables

	private CBAutoFillerWorker	worker;
	private bool				waitForWorkerToStop;
	private CBWordDict			wordDictionary;
	private int					boardSize;
	private List<List<bool>>	blocks;

	#endregion

	#region Properties

	public bool		IsProcessing	{ get; set; }
	public bool		Cancelled		{ get; set; }
	public string	CompletedBoard	{ get; private set; }

	#endregion

	#region Public Methods

	public float CheckProgress()
	{
		float progress = 0f;

		if (IsProcessing)
		{
			// Check if the worker has stopped
			if (worker.Stopped)
			{
				// Get the completed board from the worker
				CompletedBoard = worker.CompletedBoard;

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

	public void StartFindingWords(CBWordDict wordDictionary, int boardSize, List<List<CrosswordBuilder.Cell>> cells)
	{
		this.wordDictionary	= wordDictionary;
		this.boardSize		= boardSize;

		blocks = new List<List<bool>>();

		for (int i = 0; i < cells.Count; i++)
		{
			blocks.Add(new List<bool>());

			for (int j = 0; j < cells[i].Count; j++)
			{
				blocks[i].Add(cells[i][j].isBlock);
			}
		}

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

	public List<List<bool>> GenerateRandomBlocks(CBWordDict wordDictionary, int boardSize, int maxNeighbourCount, bool noSquares)
	{
		CBAutoFillerWorker tempWorker	= new CBAutoFillerWorker();
		tempWorker.WordDictionary		= wordDictionary;
		tempWorker.BoardSize			= boardSize;

		// This method completes really fast so theres no need to run it in a seperate thread
		return tempWorker.GenerateRandomBlocks(maxNeighbourCount, noSquares);
	}

	#endregion

	#region Private Methods

	private void StartProcessing()
	{
		worker					= new CBAutoFillerWorker();
		worker.WordDictionary	= wordDictionary;
		worker.BoardSize		= boardSize;
		worker.Blocks			= blocks;

		Cancelled		= false;
		IsProcessing	= true;
		CompletedBoard	= "";

		new Thread(new ThreadStart(worker.Run)).Start();
	}

	#endregion
}
