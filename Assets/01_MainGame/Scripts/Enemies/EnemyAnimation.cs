using System.Collections;
using UnityEngine;
using UnityEngine.AI;


public class EnemyAnimation : MonoBehaviour
{
    //[SerializeField] 
    private Animator animator;
    //[SerializeField]
    private NavMeshAgent agent;

    private bool isPlayingAction = false;

    private bool isDead = false;

    public bool IsPlayingAction => isPlayingAction;

    private Coroutine attackCoroutine;
    private Coroutine skillCoroutine;
    private Coroutine hitCoroutine;
    private Coroutine stunCoroutine;
    private Coroutine tauntingCoroutine;

    // ←追加
    private bool useAgentMove = true;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        UpdateMoveAnimation();
    }
     private void UpdateMoveAnimation()
    {
        // 攻撃・スキル中は移動アニメーション変更しない
        if (isPlayingAction)
            return;

        // 乗っ取り中は agent が無効 → velocity が常に 0 になり
        // SetMove() の値を上書きしてしまうのでスキップ
        if (agent == null || !agent.enabled)
            return;

        bool moving = agent.velocity.magnitude > 0.1f;
        animator.SetBool("Move", moving);
    }

    public void PlayAttack()
    {
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
    
        isPlayingAction = true;

        animator.SetTrigger("Attack");

        // アニメーション長さに合わせる
        yield return new WaitForSeconds(0.5f);

        isPlayingAction = false;
    }

    public void PlaySkill()
    {
        if (skillCoroutine != null)
            StopCoroutine(skillCoroutine);

        StartCoroutine(SkillRoutine());
    }

    private IEnumerator SkillRoutine()
    {
        isPlayingAction = true;

        //animator.SetBool("Move",false);
        animator.SetTrigger("Skill");

        yield return new WaitForSeconds(1f);

        isPlayingAction = false;
    }

    // 外部から移動アニメーション制御
    public void SetMove(bool moving)
    {
        animator.SetBool("Move", moving);
    }

   
    public void PlayDie()
    {
        if (isDead) return;

        isDead = true;

        StopAllCoroutines();

        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Skill");

        animator.SetBool("Move", false);
        isPlayingAction = true;
      StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        animator.SetTrigger("Die");

        // 死亡アニメーション時間
        yield return new WaitForSeconds(1f);

        //Destroy(gameObject);
    }

    //被ダメージ時のアニメーション
    public void PlayHit()
    {
        if (isDead) return;

        if(hitCoroutine != null)
            StopCoroutine(hitCoroutine);

        hitCoroutine =StartCoroutine (HitRoutine());
       
    }

    private IEnumerator HitRoutine()
    {
        isPlayingAction = true;

        animator.SetTrigger("Hit");
        yield return new WaitForSeconds(0.3f);

        isPlayingAction= false;

    }

    //被ダメージ時のアニメーション
    //public void PlayStun()
    //{
    //    if (isDead) return;

    //    if (stunCoroutine != null)
    //        StopCoroutine(stunCoroutine);

    //    stunCoroutine = StartCoroutine(StunRoutine());

    //}

    ////スタンアニメーション
    //private IEnumerator StunRoutine()
    //{
    //    isPlayingAction = true;

    //    animator.SetTrigger("Stun");

    //    yield return new WaitForSeconds(1f); 
        
    //    isPlayingAction = false;

    //}

    public void SetStun(bool stun)
    {
       if (isDead == true)
        {
            return;
        }

       // Debug.Log($"SetStun : {stun}");

        animator.SetBool("Stun", stun);

       
        isPlayingAction = stun;
        // スタン中は移動アニメーションを止める
        if (stun)
        {
            animator.SetBool("Move", false);
        }
    }

    public void PlayTaunting()
    {
        if (isDead)
            return;

        if (tauntingCoroutine != null)
            StopCoroutine(tauntingCoroutine);

        tauntingCoroutine = StartCoroutine(TauntingRoutine());
    }

    private IEnumerator TauntingRoutine()
    {
        isPlayingAction = true;

        // 移動アニメーションを停止
        animator.SetBool("Move", false);

        // Tauntingアニメーションを再生
        animator.SetTrigger("Taunting");

        // Tauntingアニメーションの長さに合わせる
        yield return new WaitForSeconds(2f);

        isPlayingAction = false;

        tauntingCoroutine = null;
    }
}
