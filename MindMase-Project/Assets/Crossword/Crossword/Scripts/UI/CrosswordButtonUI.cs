using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CrosswordButtonUI : MonoBehaviour
{
	#region Inspector Variables

	public RawImage		crosswordIcon;
	public Button		crosswordButton;
	public Text			levelNumberText;
	public GameObject	lockedIndicator;
	public Text			completeTimeText;
	public GameObject	completeTimeContainer;

	#endregion

	#region Properties

	public CrosswordController.Crossword				Crossword					{ get; set; }
	public System.Action<CrosswordController.Crossword>	OnCrosswordButtonClicked	{ get; set; }

	#endregion

	#region Public Methods

	public void OnClicked()
	{
		if (OnCrosswordButtonClicked != null)
		{
			OnCrosswordButtonClicked(Crossword);
		}
	}

	#endregion
}
