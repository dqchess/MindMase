using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SectionUI : MonoBehaviour
{
	#region Inspector Variables

	public Text			headerText;
	public Text			completedText;
	public Transform	crosswordParentMarker;
	public Image		iapLockedIcon;
	public GameObject	iapLockedObject;

	#endregion

	#region Properties

	public int							SectionIndex			{ get; set; }
	public System.Action<int, string>	OnArrowClicked			{ get; set; }
	public System.Action				OnUnlockSectionClicked	{ get; set; }
	public int							IndexOffset				{ get; set; }
	public RectTransform				CurrentButtonContainer	{ get; set; }

	#endregion

	#region Public Methods

	public void OnLeftArrowClicked()
	{
		if (OnArrowClicked != null)
		{
			OnArrowClicked(SectionIndex, "left");
		}
	}

	public void OnRightArrowClicked()
	{
		if (OnArrowClicked != null)
		{
			OnArrowClicked(SectionIndex, "right");
		}
	}

	public void OnUnlockClicked()
	{
		if (OnUnlockSectionClicked != null)
		{
			OnUnlockSectionClicked();
		}
	}

	#endregion
}
