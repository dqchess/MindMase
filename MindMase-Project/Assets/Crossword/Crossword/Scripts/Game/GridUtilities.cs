using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class CrosswordUtilities
{
	#region Public Methods

	public static string FormateCompleteTime(float time)
	{
		System.TimeSpan timeSpan = (System.DateTime.Now.AddSeconds(time) - System.DateTime.Now);

		string secondsStr	= string.Format("{0}{1}", (timeSpan.Seconds < 10) ? "0" : "", timeSpan.Seconds.ToString());
		string minutesStr	= string.Format("{0}:", timeSpan.Minutes.ToString());
		string hoursStr		= timeSpan.Hours > 0 ? timeSpan.Hours.ToString() + ":" : "";

		if (timeSpan.Hours > 0 && timeSpan.Minutes < 10)
		{
			minutesStr = "0" + minutesStr;
		}

		return hoursStr + minutesStr + secondsStr;
	}

	public static Texture2D CreateCrosswordIcon(
		CrosswordController.Crossword	crossword,
		int 							preferredSize,
		Color							normalCellColor,
		Color							blockCellColor,
		bool 							includeBorder,
		int 							borderLineSize,
		Color							borderLineColor,
		bool 							includeGridLine,
		int 							gridLineSize,
		Color							gridLineColor)
	{
		int tempSize			= preferredSize - (includeGridLine ? gridLineSize * (crossword.size - 1) : 0) - (includeBorder ? borderLineSize * 2 : 0);
		int blockSize			= Mathf.FloorToInt((float)tempSize / (float)crossword.size);
		int actualTextureSize	= blockSize * crossword.size + (includeGridLine ? gridLineSize * (crossword.size - 1) : 0) + (includeBorder ? borderLineSize * 2 : 0);

		Texture2D texture = new Texture2D(actualTextureSize, actualTextureSize, TextureFormat.ARGB32, false, true);

		texture.filterMode = FilterMode.Point;
		texture.Apply();

		if (includeBorder)
		{
			FillBorder(texture, borderLineSize, borderLineColor);
		}

		if (includeGridLine)
		{
			FillGridLines(texture, crossword.size, gridLineSize, gridLineColor, blockSize, includeBorder, borderLineSize);
		}

		for (int y = 0; y < crossword.size; y++)
		{
			for (int x = 0; x < crossword.size; x++)
			{
				bool isBlock = crossword.cells[y][x].isBlock;

				int startX = x * blockSize + (includeBorder ? borderLineSize : 0) + (includeGridLine ? gridLineSize * x : 0);
				int startY = y * blockSize + (includeBorder ? borderLineSize : 0) + (includeGridLine ? gridLineSize * y : 0);

				FillBlock(texture, startX, startY, blockSize, blockSize, isBlock ? blockCellColor : normalCellColor);
			}
		}

		texture.Apply();

		return texture;
	}

	#endregion

	#region Private Methods

	private static void FillBlock(Texture2D texture, int startX, int startY, float blockWidth, float blockHeight, Color color)
	{
		for (int y = 0; y < blockHeight; y++)
		{
			for (int x = 0; x < blockWidth; x++)
			{
				texture.SetPixel(startX + x, texture.height - startY - y - 1, color);
			}
		}
	}

	private static void FillBorder(Texture2D texture, int lineSize, Color color)
	{
		// Top
		FillBlock(texture, 0, 0, texture.width, lineSize, color);

		// Bottom
		FillBlock(texture, 0, texture.height - lineSize, texture.width, lineSize, color);

		// Left
		FillBlock(texture, 0, 0, lineSize, texture.height, color);

		// Right
		FillBlock(texture, texture.width - lineSize, 0, lineSize, texture.height, color);
	}

	private static void FillGridLines(Texture2D texture, float gridSize, int lineSize, Color color, int blockSize, bool includeBorder, int borderLineSize)
	{
		// Vertical grid lines
		for (int i = 0; i < gridSize - 1; i++)
		{
			int x		= (includeBorder ? borderLineSize : 0) + (i + 1) * blockSize + i * lineSize;
			int y		= (includeBorder ? borderLineSize : 0);
			int height	= texture.height - (includeBorder ? borderLineSize * 2 : 0);

			FillBlock(texture, x, y, lineSize, height, color);
		}

		// Horizontal grid lines
		for (int i = 0; i < gridSize - 1; i++)
		{
			int x		= (includeBorder ? borderLineSize : 0);
			int y		= (includeBorder ? borderLineSize : 0) + (i + 1) * blockSize + i * lineSize;
			int width	= texture.width - (includeBorder ? borderLineSize * 2 : 0);

			FillBlock(texture, x, y, width, lineSize, color);
		}
	}

	#endregion
}
