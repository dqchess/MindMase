using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Keyboard : MonoBehaviour
{
	#region Enums

	public enum KeyId
	{
		Character,
		Backspace,
		CloseKeyboard
	}

	#endregion

	#region Inspector Variables

	[SerializeField] private List<KeyboardButton> keyboardButtons;

	#endregion

	#region Properties

	public System.Action<KeyId, char> OnKeyPressed { get; set; }

	#endregion

	#region Unity Methods

	private void Start()
	{
		for (int i = 0; i < keyboardButtons.Count; i++)
		{
			KeyboardButton keyboardButton = keyboardButtons[i];

			keyboardButton.UIButton.onClick.AddListener(() => { OnButtonClicked(keyboardButton); });
		}
	}

	#endregion

	#region Private Methods

	private void OnButtonClicked(KeyboardButton keyboardButton)
	{
		Debug.Log("[Keyboard] Key pressed: " + keyboardButton.keyId + ", " + keyboardButton.character);

		if (OnKeyPressed != null)
		{
			OnKeyPressed(keyboardButton.keyId, keyboardButton.character);
		}
	}

	#endregion
}
