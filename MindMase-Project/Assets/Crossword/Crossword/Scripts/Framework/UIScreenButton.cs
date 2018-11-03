using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Button))]
public class UIScreenButton : MonoBehaviour
{
	#region Inspector Variables

	[SerializeField] private string	screenIdToShow;
	[SerializeField] private bool	isBackButton;

	#endregion

	#region Unity Methods

	private void Start()
	{
		gameObject.GetComponent<Button>().onClick.AddListener(() => { UIScreenController.Instance.Show(screenIdToShow, isBackButton); } );
	}

	#endregion
}
