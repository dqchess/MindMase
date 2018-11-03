using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(CrosswordController))]
public class CrosswordControllerEditor : Editor
{
	public override void OnInspectorGUI ()
	{
		base.OnInspectorGUI();

		EditorGUILayout.Space();

		if (GUILayout.Button("Delete Save Data"))
		{
			System.IO.File.Delete(CrosswordController.SaveFilePath);
			PlayerPrefs.DeleteAll();
			Debug.Log("Save data deleted");
		}

		EditorGUILayout.Space();
	}
}
