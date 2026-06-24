using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AwesomeIcons
{
	[InitializeOnLoad]
	static class DisplayAwesomeIcon
	{
		const string TexturePath = "Assets/Awesome Effects/Scripts/Editor/Resources/Icon.png";
		const string TexturePath2 = "Assets/Awesome Effects/Scripts/Editor/Resources/HL.png";
		static Texture2D Tex;
		static Texture2D Tex2;
		static List<int> aFx = new List<int>();
		static DisplayAwesomeIcon ()
		{
			Tex = AssetDatabase.LoadAssetAtPath(TexturePath, typeof(Texture2D)) as Texture2D;
			Tex2 = AssetDatabase.LoadAssetAtPath(TexturePath2, typeof(Texture2D)) as Texture2D;
			EditorApplication.hierarchyWindowItemOnGUI += HighlightItems;
			EditorApplication.update += UpdateAwesomeIcons;
		}
		static void UpdateAwesomeIcons ()
		{
			GameObject[] g = UnityEngine.Object.FindObjectsOfType (typeof(GameObject)) as GameObject[];
			aFx = new List<int> ();
			foreach (GameObject fx in g) 
			{
				if (fx.GetComponent<ColorControl> () != null)
					aFx.Add (fx.GetInstanceID ());
			}

		}
		static void HighlightItems (int id, Rect r2)
		{
				Rect r = new Rect (r2); 
				r.x = r.width - 20;
				r.width = 18;
			if (aFx.Count != 0) {
				if (aFx.Contains (id)) {
					GUI.DrawTexture (r, Tex);
					GUI.DrawTexture (r2, Tex2);
				}
			}
		}
	}
}
