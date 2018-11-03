using System.Collections;
using System.Collections.Generic;

public class CBWordDictWorker : Worker
{
	#region enums

	public enum TypeOfWork
	{
		PreProcess,
		Load
	}

	#endregion

	#region Member Variables

	private string[] wordFileLines;

	#endregion

	#region Properties

	public TypeOfWork	Type			{ get; set; }
	public string		WordFileText	{ get; set; }

	// Should be set for TypeOfWork.PreProcess
	public System.IO.StreamWriter PreProcessedStreamWriter	{ get; set; }
	public System.IO.StreamWriter CluesStreamWriter			{ get; set; }

	// Should be set for TypeOfWork.Load
	public Dictionary<int, List<string>> 						Words			{ get; set; }
	public Dictionary<int, Dictionary<string, List<string>>>	LetterMappings	{ get; set; }
	public string												CluesFileText	{ get; set; }
	public Dictionary<string, List<string>>						Clues			{ get; set; }

	#endregion

	#region Protected Methods

	protected override void Begin()
	{
		wordFileLines	= WordFileText.Split('\n');
		Progress		= 0;
	}

	protected override void DoWork()
	{
		for (int i = 0; i < wordFileLines.Length; i++)
		{
			if (Stopping)
			{
				return;
			}

			switch (Type)
			{
			case TypeOfWork.PreProcess:
				PreProcess(wordFileLines[i]);
				break;
			case TypeOfWork.Load:
				Load(wordFileLines[i]);
				break;
			}

			Progress = (float)i / (float)wordFileLines.Length;
		}

		if (Type == TypeOfWork.Load)
		{
			// Load all the lines in the clues file
			string[] clueLines = CluesFileText.Split('\n');

			for (int i = 0; i < clueLines.Length; i++)
			{
				// Split based on tabs to seperate the clues
				string[] line = clueLines[i].Split('\t');

				List<string> clues = new List<string>();

				// Gather all the clues and not the word
				for (int j = 1; j < line.Length; j++)
				{
					clues.Add(line[j]);
				}

				// Add the words clues
				Clues[line[0]] = clues;
			}
		}

		Stop();
	}

	protected override void SetStopped()
	{
		switch (Type)
		{
		case TypeOfWork.PreProcess:
			PreProcessedStreamWriter.Flush();
			PreProcessedStreamWriter.Close();
			CluesStreamWriter.Flush();
			CluesStreamWriter.Close();
			break;
		}

		wordFileLines = null;

		base.SetStopped();
	}

	#endregion

	#region Private Methods

	private void PreProcess(string line)
	{
		List<string> splitLine = new List<string>(line.Split('\t'));

		if (splitLine.Count > 0)
		{
			// First element in the list will be the word, all others are the clues
			string word = splitLine[0].Trim();

			if (word.Length > 0)
			{
				PreProcessedStreamWriter.WriteLine(word + ";" + AddLetterMappings(word, 0, ""));
				CluesStreamWriter.WriteLine(line);
			}
		}
	}

	private void Load(string line)
	{
		line = line.Replace("\r", "");

		List<string> splitLine = new List<string>(line.Split(';'));

		if (splitLine.Count > 0)
		{
			string	word	= splitLine[0];
			int		len		= word.Length;

			if (len > 0)
			{
				if (!Words.ContainsKey(len))
				{
					Words.Add(len, new List<string>());
					LetterMappings.Add(len, new Dictionary<string, List<string>>());
				}

				Words[len].Add(word);

				for (int i = 1; i < splitLine.Count; i++)
				{
					if (Stopping)
					{
						return;
					}

					string key = splitLine[i];

					if (!LetterMappings[len].ContainsKey(key))
					{
						LetterMappings[len].Add(key, new List<string>());
					}

					LetterMappings[len][key].Add(word);
				}
			}
		}
	}

	private string AddLetterMappings(string word, int index, string currentKey)
	{
		string allKeys = "";

		for (int i = index; i < word.Length; i++)
		{
			if (Stopping)
			{
				return "";
			}

			string key = CBWordDict.AddToKey(currentKey, i, word[i]);

			if (!string.IsNullOrEmpty(allKeys))
			{
				allKeys += ";";
			}

			allKeys += key;

			// Only recurse if this word is less than the max length and there is another letter in the word
			if (word.Length <= CBUtilities.MaxLengthForFullWordMapping && i + 1 < word.Length)
			{
				allKeys += ";";
				allKeys += AddLetterMappings(word, i + 1, key);
			}
		}

		return allKeys;
	}

	#endregion
}
