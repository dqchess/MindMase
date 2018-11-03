using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Button))]
public class KeyboardButton : MonoBehaviour
{
	#region Inspector Variables

	public Keyboard.KeyId	keyId;
	public char				character;

	#endregion

	#region Properties

	public Button UIButton { get { return gameObject.GetComponent<Button>(); } }

	#endregion
}
