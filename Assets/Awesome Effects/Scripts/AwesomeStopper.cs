using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AwesomeStopper : MonoBehaviour {

	void OnParticleCollision(GameObject other)
	{
		transform.parent.GetComponent<AwesomeTargetController> ().StopUsAll ();
		Debug.Log ("particleCol");
	}
}
