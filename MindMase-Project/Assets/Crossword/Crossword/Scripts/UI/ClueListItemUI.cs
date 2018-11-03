using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ClueListItemUI : MonoBehaviour
{
	#region Inspector Variables

	public Text		numberText;
	public Text		clueText;
	public Image	backgroundImage;
	public Color	normalBkgColor		= Color.white;
	public Color	selectedBkgColor	= Color.white;
	public Color	normalTextColor		= Color.white;
	public Color	selectedTextColor	= Color.white;

	#endregion

	#region Properties

	public int						ClueNumber		{ get; set; }
	public bool						IsAcrossClue	{ get; set; }
	public System.Action<int, bool>	OnClueSelected	{ get; set; }

	#endregion

	#region Public Methods

	public void OnClicked()
	{
		if (OnClueSelected != null)
		{
			OnClueSelected(ClueNumber, IsAcrossClue);
		}
	}

	#endregion
}
