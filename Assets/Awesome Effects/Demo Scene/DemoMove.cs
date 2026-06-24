using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DemoMove : MonoBehaviour {
	public List<GameObject> fx = new List<GameObject>();
	private GameObject LookTarget;
	public bool move = false;
	public bool day = false;
	public Text name;
	public Text camText;
	public Text dayText;
	public Light dayLight;
	private Color col;
	int i;
	void Start()
	{
		LookTarget = fx [0];
		i = 0;
		col.r = 0.50f;
		col.g = 0.50f;
		col.b = 0.50f;
	}
	public void SetGlobalColorRED(float val)
	{
		col.r = val;
		GradientColorKey[] keys = LookTarget.GetComponent<ColorControl> ().colorMap.colorKeys;
		GradientAlphaKey[] aKeys = LookTarget.GetComponent<ColorControl> ().colorMap.alphaKeys; 
		keys [1].color = col;
		LookTarget.GetComponent<ColorControl> ().colorMap.SetKeys (keys, aKeys);
	}
	public void SetGlobalColorGREEN(float val)
	{
		col.g = val;
		GradientColorKey[] keys = LookTarget.GetComponent<ColorControl> ().colorMap.colorKeys;
		GradientAlphaKey[] aKeys = LookTarget.GetComponent<ColorControl> ().colorMap.alphaKeys; 
		keys [1].color = col;
		LookTarget.GetComponent<ColorControl> ().colorMap.SetKeys (keys, aKeys);
	}
	public void SetGlobalColorBLUE(float val)
	{
		col.b = val;
		GradientColorKey[] keys = LookTarget.GetComponent<ColorControl> ().colorMap.colorKeys;
		GradientAlphaKey[] aKeys = LookTarget.GetComponent<ColorControl> ().colorMap.alphaKeys; 
		keys [1].color = col;
		LookTarget.GetComponent<ColorControl> ().colorMap.SetKeys (keys, aKeys);
	}
	void Update () {
		if (move) {
			transform.Translate (0.01f, 0, 0);
			transform.LookAt (LookTarget.transform);
			camText.text = "Stop Cam";
		}
		if (!move) {
			camText.text = "Move Cam";
		}
		name.text = "Fx: " + LookTarget.name;
		if (day) {
			dayText.text = "Switch to night";
			dayLight.transform.eulerAngles = new Vector3 (-302.39f,0,0);
		}
		if (!day) {
			dayText.text = "Switch to day";
			dayLight.transform.eulerAngles = new Vector3 (-9.39f,0,0);
		}
	}
	public void nextFx()
	{
		if(i!=0){
			fx [i-1].SetActive (false);
		}
		if (i == 0) {
			fx [fx.Count - 1].SetActive (false);
		}
		fx [i].SetActive (true);
		LookTarget = fx [i];
		i++;
		if (i == fx.Count) {
			i = 0;
		}
	}
	public void setMove(bool b)
	{
		move = b;
	}
	public void setDay(bool b)
	{
		day = b;
	}
}
