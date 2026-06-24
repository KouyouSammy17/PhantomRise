using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class DestroyScript : MonoBehaviour
{
    [SerializeField] private float DelayTime = 1;
    void Start()
    {
        Invoke("Destroy", DelayTime);
    }
    private void Destroy ()
    {
        Destroy(this.gameObject);
    }
}
}