using UnityEngine;
using System.Collections;

public class DemonEnemySkill : EnemySkillBase
{
    //デーモンのスキルを使うと自分に10秒間バフをかける(20秒間クールタイム)
    //バフの効果として、攻撃力が2倍、移動速度が1.5倍になる

    [SerializeField] private GameObject Bufficon;

    private Coroutine buffCoroutine;

    [SerializeField] private ParticleSystem BuffParticle;



    public override bool TryUseSkill()
    {
        if (!CanUseSkill())
            return false;

        enemyController.PlaySkillAnimation();

        ResetCooldown();
        if (buffCoroutine != null)
        {
            StopCoroutine(buffCoroutine);
        }
        buffCoroutine = StartCoroutine(BuffRoutine());
        return true;
    }

    private IEnumerator BuffRoutine()
    {
       

        //プレイヤーが乗っ取った敵の場合は、UIにバフアイコンを表示する
        if (enemyController.IsHijacked)
        {
            FindAnyObjectByType<PlayerStateMachine>()?.StartDemonBuff(10f);
        }
        else
        {
            Bufficon.SetActive(true);   // 敵の頭上アイコン
        }

        if (BuffParticle != null)
        {
            BuffParticle.Play();
        }

        //Bufficon.SetActive(true);
        // 攻撃力を2倍にする
        enemyController.SetDamageMultiplier(2f);
        //敵の時の移動速度を1.5倍にする
        enemyController.SetSpeedMultiplier(1.5f);
        Debug.Log("デーモンのバフ開始");
        yield return new WaitForSeconds(10f);

        // バフを解除する
        //if (enemyController.IsHijacked)
        //{
        //    BuffUIController.Instance.HideBuff(BuffType.DemonBuff);
        //}
        
        
            Bufficon.SetActive(false);   // 敵の頭上アイコン
        

        if (BuffParticle != null)
        {
            BuffParticle.Stop();
        }

        Bufficon.SetActive(false);
        enemyController.SetDamageMultiplier(1f);
        enemyController.SetSpeedMultiplier(1f);
        buffCoroutine = null;
        Debug.Log("デーモンのバフ終了");
    }
}
