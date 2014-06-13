using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CustomEditor(typeof(MainScript))]
public class MainScriptEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
		MainScript myMainScript = (MainScript)target;
		
		if (GUILayout.Button ("Create MolObjects"))
		{
			myMainScript.CreateMolObjects();
		}
	}
	
	//	private void CallbackFunction()
	//    {
	//		//MainScript myMainScript = (MainScript)target;
	//        //myMainScript.Update();
	//    }
	// 
	//    void OnEnable()
	//    {
	//        EditorApplication.update += CallbackFunction;
	//    }
	// 
	//    void OnDisable()
	//    {
	//        EditorApplication.update -= CallbackFunction;
	//    }
}
