using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GridCell : MonoBehaviour
{
	#region Inspector Variables

	public Image		bkgImage;
	public Text			numberText;
	public Text			characterText;
	public GameObject	blockObj;
	public GameObject	selectedObj;
	public Button		cellButton;
	public Color		bkgNormalColor			= Color.white;
	public Color		bkgHighlightedColor		= Color.white;
	public Color		textNormalColor			= Color.white;
	public Color		textHighlightedColor	= Color.white;
	public Color		textIncorrectColor		= Color.white;

	#endregion

	#region Properties

	public CrosswordController.Pos					CellPos			{ get; set; }
	public System.Action<CrosswordController.Pos>	OnCellClicked	{ get; set; }

	private bool IsHighlighted	{ get; set; }
	private bool IsSelected		{ get; set; }
	private bool IsIncorrect	{ get; set; }

	#endregion

	#region Public Methods

	public void UpdateUI()
	{
		selectedObj.SetActive(IsSelected);

		bkgImage.color		= IsHighlighted ? bkgHighlightedColor : bkgNormalColor;
		numberText.color	= IsHighlighted ? textHighlightedColor : textNormalColor;
		characterText.color = IsIncorrect ? textIncorrectColor : (IsHighlighted ? textHighlightedColor : textNormalColor);
	}

	public void SetSelected(bool isSelected)
	{
		IsSelected = isSelected;
		UpdateUI();
	}

	public void SetHighlighted(bool isHighlighted)
	{
		IsHighlighted = isHighlighted;
		UpdateUI();
	}

	public void SetIncorrect(bool isIncorrect)
	{
		IsIncorrect = isIncorrect;
		UpdateUI();
	}

	public void OnClicked()
	{
		if (OnCellClicked != null)
		{
			OnCellClicked(CellPos);
		}
	}

	#endregion
}
