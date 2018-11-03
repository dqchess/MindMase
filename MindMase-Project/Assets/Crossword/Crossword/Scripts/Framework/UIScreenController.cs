using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIScreenController : SingletonComponent<UIScreenController>
{
	#region Inspector Variables

	[SerializeField] private float			animationSpeed;
	[SerializeField] private List<UIScreen> uiScreens;

	#endregion

	#region Member Variables

	// The UIScreen Ids currently used in the game
	public const string MainScreenId		= "main";
	public const string SectionsScreenId	= "sections";
	public const string GameScreenId		= "game";

	// The screen that is currently being shown
	private UIScreen	currentUIScreen;
	private bool		isAnimating;

	#endregion

	#region Properties

	public float	AnimationSpeed	{ get { return animationSpeed; } }
	public string	CurrentScreenId	{ get { return currentUIScreen == null ? "" : currentUIScreen.id; } }

	#endregion

	#region Properties

	/// <summary>
	/// Invoked when the UIScreenController is transitioning from one screen to another. The first argument is the current showing screen id, the
	/// second argument is the screen id of the screen that is about to show (null if its the first screen). The third argument id true if the screen
	/// that is being show is an overlay
	/// </summary>
	public System.Action<string, string, bool> OnSwitchingScreens;

	#endregion

	#region Unity Methods

	private void Start()
	{
		// Initialize and hide all the screens
		for (int i = 0; i < uiScreens.Count; i++)
		{
			uiScreens[i].Initialize();
			uiScreens[i].gameObject.SetActive(true);

			HideUIScreen(uiScreens[i], false, false, null);
		}

		// Show the main screen when the app starts up
		Show(MainScreenId, false, false);
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// Shows the screen with the specified id.
	/// </summary>
	/// <param name="id">Id of UIScreen to be shown.</param>
	/// <param name="back">If set to true back then the screens will animateleft to right on the screen, if false they animate right to left.</param>
	/// <param name="animate">If set to true animate the screens will animate, if false the screens will snap into place.</param>
	/// <param name="overlay">If set to true then the current screen will not hide.</param>
	/// <param name="onTweenFinished">Called when the screens finish animating.</param>
	public void Show(string id, bool back = false, bool animate = true, bool overlay = false, System.Action onTweenFinished = null, string data = "")
	{
		if (isAnimating || (currentUIScreen != null && id == currentUIScreen.id))
		{
			return;
		}

		UIScreen uiScreen = GetScreenInfo(id);

		if (uiScreen != null)
		{
			ShowUIScreen(uiScreen, animate, back, onTweenFinished, data);

			// If its not an overlay screen then hide the current screen
			if (!overlay)
			{
				HideUIScreen(currentUIScreen, animate, back, null);
			}

			if (OnSwitchingScreens != null)
			{
				OnSwitchingScreens(currentUIScreen == null ? null : currentUIScreen.id, id, overlay);
			}

			currentUIScreen = uiScreen;
		}
	}

	/// <summary>
	/// Hides the UI screen that was shown as an overlay
	/// </summary>
	public void HideOverlay(string id, System.Action onTweenFinished = null)
	{
		HideUIScreen(GetScreenInfo(id), true, false, onTweenFinished);
	}

	#endregion

	#region Private Methods

	private void ShowUIScreen(UIScreen uiScreen, bool animate, bool back, System.Action onTweenFinished, string data)
	{
		if (uiScreen == null)
		{
			return;
		}

		uiScreen.OnShowing(data);

		float direction = (back ? -1f : 1f);

		float fromX			= uiScreen.RectT.rect.width * direction;
		float toX			= 0;
		float fromWorldX	= Utilities.WorldWidth * direction;
		float toWorldX		= 0;

		isAnimating = animate;

		TransitionUIScreen(uiScreen, fromX, toX, fromWorldX, toWorldX, animate, () =>
			{
				isAnimating = false;

				if (onTweenFinished != null)
				{
					onTweenFinished();
				}
			});
	}

	private void HideUIScreen(UIScreen uiScreen, bool animate, bool back, System.Action onTweenFinished)
	{
		if (uiScreen == null)
		{
			return;
		}

		float direction = (back ? 1f : -1f);

		float fromX			= 0;
		float toX			= uiScreen.RectT.rect.width * direction;
		float fromWorldX	= 0;
		float toWorldX		= Utilities.WorldWidth * direction;

		TransitionUIScreen(uiScreen, fromX, toX, fromWorldX, toWorldX, animate, onTweenFinished);
	}

	private void TransitionUIScreen(UIScreen uiScreen, float fromX, float toX, float worldFromX, float worldToX, bool animate, System.Action onTweenFinished)
	{
		uiScreen.RectT.anchoredPosition = new Vector2(fromX, uiScreen.RectT.anchoredPosition.y);

		if (animate)
		{
			Tween tween = Tween.PositionX(uiScreen.RectT, Tween.TweenStyle.EaseOut, fromX, toX, animationSpeed);

			tween.SetUseRectTransform(true);

			if (onTweenFinished != null)
			{
				tween.SetFinishCallback((tweenedObject) => { onTweenFinished(); });
			}
		}
		else
		{
			uiScreen.RectT.anchoredPosition = new Vector2(toX, uiScreen.RectT.anchoredPosition.y);
		}

		for (int i = 0; i < uiScreen.worldObjects.Count; i++)
		{
			uiScreen.worldObjects[i].transform.position = new Vector3(worldFromX, uiScreen.worldObjects[i].transform.position.y, uiScreen.worldObjects[i].transform.position.z);

			if (animate)
			{
				Tween.PositionX(uiScreen.worldObjects[i].transform, Tween.TweenStyle.EaseOut, worldFromX, worldToX, animationSpeed);
			}
			else
			{
				uiScreen.worldObjects[i].transform.position = new Vector3(worldToX, uiScreen.worldObjects[i].transform.position.y, uiScreen.worldObjects[i].transform.position.z);
			}
		}
	}

	private UIScreen GetScreenInfo(string id)
	{
		for (int i = 0; i < uiScreens.Count; i++)
		{
			if (id == uiScreens[i].id)
			{
				return uiScreens[i];
			}
		}

		Debug.LogError("[UIScreenController] No UIScreen exists with the id " + id);

		return null;
	}

	#endregion
}
