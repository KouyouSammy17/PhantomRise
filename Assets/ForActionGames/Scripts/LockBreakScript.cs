using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class LockBreakScript : MonoBehaviour
{
    private float LockDissolve = 1;
    private SkinnedMeshRenderer LockMesh;

    void Start()
    {
        LockMesh = this.transform.Find("LockBreak").gameObject.GetComponent<SkinnedMeshRenderer>();
    }

    void Update()
    {
        // dissolve
        LockDissolve -= 0.5f * Time.deltaTime;
        LockMesh.material.SetFloat("_Dissolve", LockDissolve);

        if(LockDissolve <= 0)
        {
            Destroy(this.gameObject);
        }
    }
}
}