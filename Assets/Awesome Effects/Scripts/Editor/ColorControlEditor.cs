using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ColorControl))]
public class ColorControlEditor : Editor
{

    ///*****************//
    ///Editor specific stuff. 
    ///Dangerous if you do not know what you are doing.
    ///*****************//

    #region Editor Stuff
    private GUIStyle styleHeader = new GUIStyle();
	private GUIStyle styleLine = new GUIStyle();
	private GUIStyle styleSmall = new GUIStyle();
    private GUIStyle styleSmall2 = new GUIStyle();
    public override void OnInspectorGUI()
    {
        styleHeader.fontSize = 17;
        styleHeader.alignment = TextAnchor.MiddleCenter;
        styleHeader.fontStyle = FontStyle.Bold;
        styleHeader.normal.textColor = Color.white;
        styleLine.fontSize = 15;
		styleLine.alignment = TextAnchor.MiddleCenter;
		styleSmall.fontSize = 10;
		styleSmall.alignment = TextAnchor.MiddleLeft;
		styleSmall2.fontSize = 8;
		styleSmall2.alignment = TextAnchor.MiddleLeft;
        ColorControl t = (ColorControl)target;
		//
        GUILayout.BeginHorizontal();
        GUILayout.Label("Options", styleHeader);
        GUILayout.EndHorizontal();
		//
        GUILayout.BeginHorizontal();
        GUILayout.Label("---------------------------------------------------",styleLine);
        GUILayout.EndHorizontal();
		//

		//
        GUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Reset Color", GUILayout.Width(90f), GUILayout.Height(35f)))
        {
            t.ResetColor();
            EditorUtility.SetDirty(t);
        }
        if (GUILayout.Button("Enable Glow", GUILayout.Width(90f), GUILayout.Height(35f)))
        {
            t.glowActive = true;
            EditorUtility.SetDirty(t);
        }
        if (GUILayout.Button("Disable Glow", GUILayout.Width(90f), GUILayout.Height(35f)))
        {
            t.glowActive = false;
            EditorUtility.SetDirty(t);
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
		//

		//
		GUILayout.BeginVertical(EditorStyles.helpBox);
		DrawDefaultInspector();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Light color always gets the second color tick in the gradient.",styleSmall);
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
		//

    }
    #endregion

}