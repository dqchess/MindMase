using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class Utilities
{
	#region Properties

	public static double SystemTimeInMilliseconds { get { return (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalMilliseconds; } }

	public static float WorldWidth	{ get { return 2f * Camera.main.orthographicSize * Camera.main.aspect; } }
	public static float WorldHeight	{ get { return 2f * Camera.main.orthographicSize; } }
	public static float	XScale		{ get { return (float)Screen.width / 1080f; } }	
	public static float	YScale		{ get { return (float)Screen.height / 1920f; } }

	#endregion

	#region Delegates

	public delegate TResult MapFunc<out TResult, TArg>(TArg arg);
	public delegate bool FilterFunc<TArg>(TArg arg);

	#endregion

	#region Public Methods

	public static List<TOut> Map<TIn, TOut>(List<TIn> list, MapFunc<TOut, TIn> func)
	{
		List<TOut> newList = new List<TOut>(list.Count);

		for (int i = 0; i < list.Count; i++)
		{
			newList.Add(func(list[i]));
		}

		return newList;
	}

	public static void Filter<T>(List<T> list, FilterFunc<T> func)
	{
		for (int i = list.Count - 1; i >= 0; i--)
		{
			if (func(list[i]))
			{
				list.RemoveAt(i);
			}
		}
	}

	public static void SwapValue<T>(ref T value1, ref T value2)
	{
		T temp = value1;
		value1 = value2;
		value2 = temp;
	}

	public static float EaseOut(float t)
	{
		return 1.0f - (1.0f - t) * (1.0f - t) * (1.0f - t);
	}
	
	public static float EaseIn(float t)
	{
		return t * t * t;
	}

	/// <summary>
	/// Returns to mouse position
	/// </summary>
	public static Vector2 MousePosition()
	{
		#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER
		return (Vector2)Input.mousePosition;
		#else
		if (Input.touchCount > 0)
		{
			return Input.touches[0].position;
		}

		return Vector2.zero;
		#endif
	}

	/// <summary>
	/// Returns true if a mouse down event happened, false otherwise
	/// </summary>
	public static bool MouseDown()
	{
		return Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began);
	}
	
	/// <summary>
	/// Returns true if a mouse up event happened, false otherwise
	/// </summary>
	public static bool MouseUp()
	{
		return (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Ended));
	}
	
	/// <summary>
	/// Returns true if no mouse events are happening, false otherwise
	/// </summary>
	public static bool MouseNone()
	{
		return (!Input.GetMouseButton(0) && Input.touchCount == 0);
	}

	public static char CharToLower(char c)
	{
		return (c >= 'A' && c <= 'Z') ? (char)(c + ('a' - 'A')) : c;
	}

	public static int GCD(int a, int b)
	{
		int start = Mathf.Min(a, b);
		
		for (int i = start; i > 1; i--)
		{
			if (a % i == 0 && b % i == 0)
			{
				return i;
			}
		}
		
		return 1;
	}

	public static Canvas GetCanvas(Transform transform)
	{
		if (transform == null)
		{
			return null;
		}

		Canvas canvas = transform.GetComponent<Canvas>();

		if (canvas != null)
		{
			return canvas;
		}

		return GetCanvas(transform.parent);
	}

	public static void CallExternalAndroid(string methodname, params object[] args)
	{
		#if UNITY_ANDROID && !UNITY_EDITOR
		AndroidJavaClass	unity			= new AndroidJavaClass ("com.unity3d.player.UnityPlayer");
		AndroidJavaObject	currentActivity	= unity.GetStatic<AndroidJavaObject>("currentActivity");
		currentActivity.Call(methodname, args);
		#endif
	}

	public static T CallExternalAndroid<T>(string methodname, params object[] args)
	{
		#if UNITY_ANDROID && !UNITY_EDITOR
		AndroidJavaClass	unity			= new AndroidJavaClass ("com.unity3d.player.UnityPlayer");
		AndroidJavaObject	currentActivity	= unity.GetStatic<AndroidJavaObject>("currentActivity");
		return currentActivity.Call<T>(methodname, args);
		#else
		return default(T);
		#endif
	}

	public static List<T> SyncList<T>(int count, T prefab, Transform parent, List<T> list) where T : MonoBehaviour
	{
		while (list.Count < count)
		{
			T billOverview = GameObject.Instantiate(prefab);
			billOverview.transform.SetParent(parent, false);
			list.Add(billOverview);
		}
		
		while (list.Count > count)
		{
			GameObject.Destroy(list[list.Count - 1].gameObject);
			list.RemoveAt(list.Count - 1);
		}

		return list;
	}
	
	public static string ConvertToJsonString(object data, bool addQuoteEscapes = false)
	{
		string jsonString = "";
		
		if (data is IDictionary)
		{
			Dictionary<string, object> dic = data as Dictionary<string, object>;
			
			jsonString += "{";
			
			List<string> keys = new List<string>(dic.Keys);
			
			for (int i = 0; i < keys.Count; i++)
			{
				if (i != 0)
				{
					jsonString += ",";
				}

				if (addQuoteEscapes)
				{
					jsonString += string.Format("\\\"{0}\\\":{1}", keys[i], ConvertToJsonString(dic[keys[i]], addQuoteEscapes));
				}
				else
				{
					jsonString += string.Format("\"{0}\":{1}", keys[i], ConvertToJsonString(dic[keys[i]], addQuoteEscapes));
				}
			}
			
			jsonString += "}";
		}
		else if (data is IList)
		{
			IList list = data as IList;
			
			jsonString += "[";
			
			for (int i = 0; i < list.Count; i++)
			{
				if (i != 0)
				{
					jsonString += ",";
				}
				
				jsonString += ConvertToJsonString(list[i], addQuoteEscapes);
			}
			
			jsonString += "]";
		}
		else if (data is string)
		{
			// If the data is a string then we need to inclose it in quotation marks
			if (addQuoteEscapes)
			{
				jsonString += "\\\"" + data + "\\\"";
			}
			else
			{
				jsonString += "\"" + data + "\"";
			}
		}
		else if (data is bool)
		{
			jsonString += (bool)data ? "true" : "false";
		}
		else
		{
			// Else just return what ever data is as a string
			jsonString += data.ToString();
		}
		
		return jsonString;
	}

	public static void SetLayer(GameObject gameObject, int layer, bool applyToChildren = false)
	{
		gameObject.layer = layer;

		if (applyToChildren)
		{
			for (int i = 0; i < gameObject.transform.childCount; i++)
			{
				SetLayer(gameObject.transform.GetChild(i).gameObject, layer, true);
			}
		}
	}

	public static List<string[]> ParseCSVFile(string fileContents, char delimiter)
	{
		List<string[]>	csvText	= new List<string[]>();
		string[]		lines	= fileContents.Split('\n');

		for (int i = 0; i < lines.Length; i++)
		{
			csvText.Add(lines[i].Split(delimiter));
		}

		return csvText;
	}

	public static void DestroyAllChildren(Transform parent)
	{
		for (int i = parent.childCount - 1; i >= 0; i--)
		{
			GameObject.Destroy(parent.GetChild(i).gameObject);
		}
	}

	public static string FindFile(string fileName, string directory)
	{
		List<string>	files		= new List<string>(System.IO.Directory.GetFiles(directory));
		string[]		directories	= System.IO.Directory.GetDirectories(directory);

		for (int i = 0; i < files.Count; i++)
		{
			if (fileName == System.IO.Path.GetFileNameWithoutExtension(files[i]))
			{
				return files[i];
			}
		}

		for (int i = 0; i < directories.Length; i++)
		{
			string path = FindFile(fileName, directories[i]);

			if (!string.IsNullOrEmpty(path))
			{
				return path;
			}
		}
		
		return null;
	}

	public static string CalculateMD5Hash(string input)
	{
		System.Security.Cryptography.MD5	md5			= System.Security.Cryptography.MD5.Create();
		byte[]								inputBytes	= System.Text.Encoding.ASCII.GetBytes(input);
		byte[]								hash		= md5.ComputeHash(inputBytes);
		System.Text.StringBuilder			sb			= new System.Text.StringBuilder();
		
		for (int i = 0; i < hash.Length; i++)
		{
			sb.Append(hash[i].ToString("x2"));
		}
		
		return sb.ToString();
		
	}

	public static bool CompareLists<T>(List<T> list1, List<T> list2)
	{
		if (list1.Count != list2.Count)
		{
			return false;
		}

		for (int i = list1.Count - 1; i >= 0; i--)
		{
			bool found = false;

			for (int j = 0; j < list2.Count; j++)
			{
				if (list1[i].Equals(list2[j]))
				{
					found = true;
					list1.RemoveAt(i);
					list2.RemoveAt(j);
					break;
				}
			}

			if (!found)
			{
				return false;
			}
		}

		return true;
	}

	public static void PrintList<T>(List<T> list)
	{
		string str = "";

		for (int i = 0; i < list.Count; i++)
		{
			if (i != 0)
			{
				str += ", ";
			}

			str += list[i].ToString();
		}

		Debug.Log(str);
	}

	#endregion
}
