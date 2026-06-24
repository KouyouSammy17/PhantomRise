using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
[ExecuteInEditMode]

#endif
public class AwesomeParticleController : MonoBehaviour {

    void OnEnable()
	{
		#if UNITY_EDITOR
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        bool ExistLayer = false;
        //Debug.Log(ExistLayer);
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);

            //Debug.Log(layerSP.stringValue);
            if (layerSP.stringValue == "Awesome Glow")
            {
                ExistLayer = true;
                break;
            }

        }
        for (int j = 8; j < layers.arraySize; j++)
        {
            SerializedProperty layerSP = layers.GetArrayElementAtIndex(j);
            if (layerSP.stringValue == "" && !ExistLayer)
            {
                layerSP.stringValue = "Awesome Glow";
                tagManager.ApplyModifiedProperties();

                break;
            }
        }

		#endif
		ColorControl[] objs = GameObject.FindObjectsOfType<ColorControl>();
        foreach (ColorControl clr in objs)
        {
            clr.gameObject.layer = LayerMask.NameToLayer("Awesome Glow");
            for(int i=0; i < clr.transform.childCount; i++)
            {
                clr.transform.GetChild(i).gameObject.layer = LayerMask.NameToLayer("Awesome Glow");
            }
        }
    }
    public void ResetGlowLayers()
    {
        ColorControl[] objs = GameObject.FindObjectsOfType<ColorControl>();
        foreach (ColorControl clr in objs)
        {
            clr.gameObject.layer = LayerMask.NameToLayer("Awesome Glow");
            for (int i = 0; i < clr.transform.childCount; i++)
            {
                clr.transform.GetChild(i).gameObject.layer = LayerMask.NameToLayer("Awesome Glow");
            }
        }
    }
    private void LateUpdate()
    {
		gameObject.transform.rotation = Camera.main.transform.rotation;
		gameObject.transform.position = Camera.main.transform.position;
    }
}
