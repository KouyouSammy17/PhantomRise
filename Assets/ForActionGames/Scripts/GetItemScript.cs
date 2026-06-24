using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class GetItemScript : MonoBehaviour
{
    private Animator Anim;
    private PublicValueScript _Value;
    void Start()
    {
        Anim = this.GetComponent<Animator>();
        _Value = FindObjectOfType<PublicValueScript>();
    }

    void Update()
    {
        if (Anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1)
        {
            StartCoroutine(GetItem());
        }
    }
    // Move toward the player character.
    private IEnumerator GetItem ()
    {
        float t = 0;
        Vector3 start_pos = this.transform.position;
        float sca = 1;

        while(true)
        {
            t += 3* Time.deltaTime;
            this.transform.position = Vector3.Lerp(start_pos, _Value.Player_Public.transform.position + new Vector3(0,0.5f,0), t);
            sca = 1 - (t/2);
            this.transform.localScale = new Vector3(sca,sca,sca);

            if(t > 1)
            {
                _Value.HaveKeyNum_Public += 1;
                Destroy(this.gameObject);
                yield break;
            }
            yield return null;
        }
    }
}
}