using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
	using UnityEditor;

[ExecuteInEditMode]
#endif
public class ColorControl : MonoBehaviour
{
    ///*****************//
    ///Use these for setting the colors of the prefab.
    ///Also various tweaks exist too. 
    ///*****************//
    #region Public Vars
	/// <summary>
	/// The color gradient for the effect.
	/// </summary>
	public Gradient colorMap;
	[HideInInspector]
	public bool glowActive = true;
    #endregion

    ///*****************//
    ///These are not meant to be used.
    ///*****************//
    #region Private Vars
    private Gradient StartingColorMap;
    private ParticleSystem ps;
    private ParticleSystem[] pss;
    #endregion

    ///*****************//
    ///Editor specific stuff. 
    ///Dangerous if you do not know what you are doing.
    ///*****************//

    #region Editor Stuff
    private void Awake()
    {
        StartingColorMap = GetComponent<ParticleSystem>().colorOverLifetime.color.gradient;
        colorMap = GetComponent<ParticleSystem>().colorOverLifetime.color.gradient;
    }
    private void OnEnable()
    {
		StartingColorMap = GetComponent<ParticleSystem>().colorOverLifetime.color.gradient;
        if (!GameObject.FindObjectOfType<AwesomeParticleController>())
        {
			#if UNITY_EDITOR
            if(EditorUtility.DisplayDialog("No Awesome Particle Controller Detected",
                "For the particles and the glow to work properly, your scene needs the prefab 'AwesomeParticleController' located in Prefabs folder of Awesome Effects. Would you like to add it now? ", 
                "Add Prefab", "Do Not Add Prefab")){
                GameObject awe = (GameObject)Instantiate(AssetDatabase.LoadAssetAtPath("Assets/Awesome Effects/Prefabs/AwesomeParticleController.prefab", typeof(GameObject)));
                awe.name = "AwesomeParticleController";
                awe.GetComponent<AwesomeParticleController>().ResetGlowLayers();
            }
			#endif
        }
    }
    private void CheckLayerExists()
    {
        if (!GameObject.FindObjectOfType<AwesomeParticleController>() || LayerMask.NameToLayer("Awesome Glow") != 0)
        {
			#if UNITY_EDITOR
            if (EditorUtility.DisplayDialog("No Awesome Particle Controller or the necessary glow layer detected.",
                "For the particles and the glow to work properly, your scene needs the prefab 'AwesomeParticleController' located in Prefabs folder of Awesome Effects. Would you like to add it now? ",
                "Add Prefab", "Do Not Add Prefab"))
            {
                GameObject awe = (GameObject)Instantiate(AssetDatabase.LoadAssetAtPath("Assets/Awesome Effects/Prefabs/AwesomeParticleController.prefab", typeof(GameObject)));
                awe.name = "AwesomeParticleController";
                awe.GetComponent<AwesomeParticleController>().ResetGlowLayers();
            }
			#endif
        }
    }
    public void ResetColor()
    {
        colorMap = StartingColorMap;
        Debug.Log("Color Reset: " + gameObject.name);
    }
    private void LateUpdate()
    {
        pss = GetComponentsInChildren<ParticleSystem>();
        foreach(ParticleSystem p in pss)
        {
			if (p.name != "Smoke") {
				var c = p.colorOverLifetime;
					c.color = colorMap;
			}
        }
		gameObject.transform.Find("Point light").GetComponent<Light>().color = colorMap.colorKeys[1].color;
        if (!glowActive) {
            gameObject.layer = LayerMask.NameToLayer("Default");
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.layer = LayerMask.NameToLayer("Default");
            }
        }
        if (glowActive) {
            gameObject.layer = LayerMask.NameToLayer("Awesome Glow");
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.layer = LayerMask.NameToLayer("Awesome Glow");
            }
        }
    }
    #endregion

}