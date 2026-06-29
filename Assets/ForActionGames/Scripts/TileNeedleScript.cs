using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class TileNeedleScript : MonoBehaviour
{
    Animator anim;
    private static readonly int DefaultState = Animator.StringToHash("Base Layer.default");
    private static readonly int StabState = Animator.StringToHash("Base Layer.stab");
    [SerializeField] private float OffDelayTime = 3;
    private bool DelayFlg = true;

    [Header("Auto Cycle")]
    [SerializeField] private bool autoCycle = true;
    [SerializeField] private float startDelay = 0f;     // stagger this needle's start
    [SerializeField] private float onDuration = 1.5f;   // how long needle stays out
    [SerializeField] private float offDuration = 2.0f;  // how long needle stays in

    [Header("Damage")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float damageCooldown = 1.0f; // seconds between hits
    private float _lastDamageTime = -999f;

    void Start()
    {
        anim = this.GetComponent<Animator>();
        if (autoCycle)
            StartCoroutine(AutoCycleRoutine(startDelay));
    }

    private IEnumerator AutoCycleRoutine(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        while (true)
        {
            // extend needle
            anim.CrossFade(StabState, 0.1f, 0, 0);
            DelayFlg = false;
            yield return new WaitForSeconds(onDuration);

            // retract needle
            anim.CrossFade(DefaultState, 0.3f, 0, 0);
            DelayFlg = true;
            yield return new WaitForSeconds(offDuration);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        bool needleOut = anim.GetCurrentAnimatorStateInfo(0).fullPathHash == StabState;

        if (needleOut)
        {
            var machine = other.GetComponent<PlayerStateMachine>();
            if (machine != null && machine.CurrentStateName == nameof(GhostState))
            {
                // ghost touches needle → instant game over
                machine.Ghost.OnHit();
                return;
            }

            TryDamagePlayer(other.gameObject);
        }

        // manual trigger mode
        if (!autoCycle && DelayFlg)
        {
            anim.CrossFade(StabState, 0.1f, 0, 0);
            DelayFlg = false;
            Invoke("Interval", OffDelayTime);
        }
    }

    private void TryDamagePlayer(GameObject playerObj)
    {
        if (Time.time - _lastDamageTime < damageCooldown) return;
        _lastDamageTime = Time.time;

        var hp = playerObj.GetComponent<PlayerHP>();
        if (hp != null)
            hp.TakeDamage(damage);
    }

    // return to previous state (manual mode)
    private void Interval()
    {
        anim.CrossFade(DefaultState, 0.3f, 0, 0);
        DelayFlg = true;
    }
}
}