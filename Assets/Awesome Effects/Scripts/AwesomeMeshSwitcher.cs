using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[ExecuteInEditMode]
#endif
public class AwesomeMeshSwitcher : MonoBehaviour {
	private ParticleSystem[] pss;
	public MeshRenderer MeshToUse;
	void LateUpdate () {
		pss = GetComponentsInChildren<ParticleSystem>();
		foreach(ParticleSystem p in pss)
		{
			if (p.shape.shapeType == ParticleSystemShapeType.MeshRenderer) {
				ParticleSystem.ShapeModule s = p.shape;
				s.meshRenderer = MeshToUse;
			}
		}
	}
}
