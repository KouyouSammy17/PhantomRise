using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class AnimEventScript : MonoBehaviour
{
    [SerializeField] private GameObject[] Effect;

    // Angel
    private void AttackAngel1 ()
    {
        GameObject obj = Instantiate(Effect[0]);
        obj.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0,0.4f,0.7f));
        obj.transform.rotation = this.transform.rotation;
        obj.name = "AttackEffect";
    }
    private void AttackAngel2 ()
    {
        GameObject obj1 = Instantiate(Effect[1]);
        GameObject obj2 = Instantiate(Effect[1]);
        GameObject obj3 = Instantiate(Effect[1]);        
        obj1.transform.position = this.transform.position + (this.transform.rotation * new Vector3(-0.3f,0.6f,0.6f));
        obj1.transform.rotation = this.transform.rotation ;
        obj1.transform.Rotate(Vector3.up, -20);
        obj1.name = "AttackEffect";
        obj2.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0,0.6f,0.7f));
        obj2.transform.rotation = this.transform.rotation;
        obj2.name = "AttackEffect";
        obj3.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0.3f,0.6f,0.6f));
        obj3.transform.rotation = this.transform.rotation;
        obj3.transform.Rotate(Vector3.up, 20);
        obj3.name = "AttackEffect";
    }
    private void AttackAngel3 ()
    {
        GameObject obj = Instantiate(Effect[2]);
        obj.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0,0.8f,0));
        obj.transform.rotation = this.transform.rotation;
        obj.name = "AttackEffect";
    }

    // Devil
    private void AttackDevil1 ()
    {
        GameObject obj = Instantiate(Effect[0]);
        obj.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0.072f,0.32f,0.8f));
        obj.transform.rotation = this.transform.rotation;
        obj.name = "AttackEffect";
    }
    private void AttackDevil2 ()
    {
        GameObject obj = Instantiate(Effect[1]);
        obj.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0,0.5f,0.5f));
        obj.transform.rotation = this.transform.rotation;
        obj.name = "AttackEffect";
    }
    private void AttackDevil3 ()
    {
        GameObject obj = Instantiate(Effect[2]);
        obj.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0,0,1.1f));
        obj.transform.rotation = this.transform.rotation;
        obj.name = "AttackEffect";
    }

    // Dragon
    private void AttackDragon1 ()
    {
        GameObject obj = Instantiate(Effect[0]);
        obj.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0,0.5f,0.7f));
        obj.transform.rotation = this.transform.rotation;
        obj.name = "AttackEffect";
    }
    private void AttackDragon2 ()
    {
        GameObject obj = Instantiate(Effect[1]);
        obj.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0,0.5f,0.4f));
        obj.transform.rotation = this.transform.rotation;
        obj.name = "AttackEffect";
    }
    private void AttackDragon3 ()
    {
        GameObject obj = Instantiate(Effect[2]);
        obj.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0,0.5f,0.7f));
        obj.transform.rotation = this.transform.rotation;
        obj.name = "AttackEffect";
    }

    // Ghost
    private void AttackGhost1 ()
    {
        GameObject obj = Instantiate(Effect[0]);
        obj.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0,0.5f,0.5f));
        obj.transform.rotation = this.transform.rotation;
        obj.name = "AttackEffect";
    }
    private void AttackGhost2 ()
    {
        GameObject obj = Instantiate(Effect[1]);
        obj.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0,0,1.0f));
        obj.transform.rotation = this.transform.rotation;
        obj.name = "AttackEffect";
    }
    private void AttackGhost3 ()
    {
        GameObject obj = Instantiate(Effect[2]);
        obj.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0,0.8f,0));
        obj.transform.rotation = this.transform.rotation;
        obj.name = "AttackEffect";
    }

    // Reaper
    private void AttackReaper1 ()
    {
        GameObject obj = Instantiate(Effect[0]);
        obj.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0,0.5f,0.5f));
        obj.transform.rotation = this.transform.rotation;
        obj.name = "AttackEffect";
    }
    private void AttackReaper2 ()
    {
        GameObject obj = Instantiate(Effect[1]);
        obj.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0,0.5f,0.5f));
        obj.transform.rotation = this.transform.rotation;
        obj.name = "AttackEffect";
    }
    private void AttackReaper3 ()
    {
        GameObject obj = Instantiate(Effect[2]);
        obj.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0,0,0.8f));
        obj.transform.rotation = this.transform.rotation;
        obj.name = "AttackEffect";
    }

    // Minotaur
    private void AttackMinotaur1 ()
    {
        GameObject obj = Instantiate(Effect[0]);
        obj.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0,0.5f,0.5f));
        obj.transform.rotation = this.transform.rotation;
        obj.name = "AttackEffect";
    }
    private void AttackMinotaur2 ()
    {
        GameObject obj = Instantiate(Effect[1]);
        obj.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0,0,0.8f));
        obj.transform.rotation = this.transform.rotation;
        obj.name = "AttackEffect";
    }
    private void AttackMinotaur3 ()
    {
        GameObject obj = Instantiate(Effect[2]);
        obj.transform.position = this.transform.position + (this.transform.rotation * new Vector3(0,0,0.8f));
        obj.transform.rotation = this.transform.rotation;
        obj.name = "AttackEffect";
    }
}
}