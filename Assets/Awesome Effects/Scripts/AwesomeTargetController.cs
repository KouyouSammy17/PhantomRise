using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AwesomeTargetController : MonoBehaviour {
	private bool OnTarget = false;
	private float startDelay = 0;
	private float startTime = 0;
	public Transform Shooter;
	public Transform Target;
	public float speed = 10;
	public AnimationCurve Ease;

	void OnEnable()
	{
		OnTarget = false;
		startDelay = GetComponent<ParticleSystem> ().main.startDelay.constant;
		StartCoroutine (shootParticle ());
	}
	void LateUpdate()
	{
		if(!OnTarget){
			transform.position = Shooter.position;
		}
		else{
			transform.position = Vector3.MoveTowards(transform.position,Target.position,Ease.Evaluate(startTime));
			startTime += Time.deltaTime * speed;
		}
	}
	IEnumerator shootParticle()
	{
		yield return new WaitForSeconds (startDelay);
		OnTarget = true;
		startTime = 0;
	}

	void OnParticleCollision(GameObject other)
	{
		GetComponent<ParticleSystem> ().Stop (true);
		Debug.Log ("particleCol");
	}
	public void StopUsAll()
	{
		GetComponent<ParticleSystem> ().Stop (true);
		Debug.Log ("particleCol");
	}
}
