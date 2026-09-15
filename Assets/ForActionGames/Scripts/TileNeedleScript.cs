using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample
{
    public class TileNeedleScript : MonoBehaviour
    {
        Animator anim;

        private static readonly int DefaultState =
            Animator.StringToHash("Base Layer.default");

        private static readonly int StabState =
            Animator.StringToHash("Base Layer.stab");

        [SerializeField] private float OffDelayTime = 3;
        private bool DelayFlg = true;

        [Header("Auto Cycle")]
        [SerializeField] private bool autoCycle = true;
        [SerializeField] private float startDelay = 0f;
        [SerializeField] private float onDuration = 1.5f;
        [SerializeField] private float offDuration = 2.0f;

        [Header("Damage")]
        [SerializeField] private int damage = 10;

        [Tooltip("プレイヤーが針からダメージを受ける間隔")]
        [SerializeField] private float playerDamageCooldown = 1.0f;

        [Tooltip("敵が針からダメージを受ける間隔")]
        [SerializeField] private float enemyDamageCooldown = 1.0f;

        // プレイヤー用
        private float _lastPlayerDamageTime = -999f;

        // 敵用
        private float _lastEnemyDamageTime = -999f;

        void Start()
        {
            anim = this.GetComponent<Animator>();

            if (autoCycle)
                StartCoroutine(AutoCycleRoutine(startDelay));
        }

        private IEnumerator AutoCycleRoutine(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            while (true)
            {
                // ==============================
                // 針を出す
                // ==============================

                anim.CrossFade(
                    StabState,
                    0.1f,
                    0,
                    0
                );

                DelayFlg = false;

                yield return new WaitForSeconds(onDuration);


                // ==============================
                // 針を戻す
                // ==============================

                anim.CrossFade(
                    DefaultState,
                    0.3f,
                    0,
                    0
                );

                DelayFlg = true;

                yield return new WaitForSeconds(offDuration);
            }
        }

        void OnTriggerStay(Collider other)
        {
            // ==============================
            // 針が出ているか確認
            // ==============================

            bool needleOut =
                anim.GetCurrentAnimatorStateInfo(0).fullPathHash
                == StabState;

            // 針が出ていない場合
            // プレイヤーの手動モードだけ処理
            if (!needleOut)
            {
                // manual trigger mode
                if (!autoCycle && DelayFlg)
                {
                    anim.CrossFade(
                        StabState,
                        0.1f,
                        0,
                        0
                    );

                    DelayFlg = false;

                    Invoke(
                        nameof(Interval),
                        OffDelayTime
                    );
                }

                return;
            }


            // ==================================================
            // プレイヤー
            // ==================================================

            if (other.CompareTag("Player"))
            {
                var machine =
                    other.GetComponent<PlayerStateMachine>();

                if (machine != null &&
                    machine.CurrentStateName == nameof(GhostState))
                {
                    // Ghost状態で針に触れたら即ゲームオーバー
                    machine.Ghost.OnHit();
                    return;
                }

                TryDamagePlayer(other.gameObject);

                return;
            }


            // ==================================================
            // 敵
            // ==================================================

            if (other.CompareTag("Enemy"))
            {
                TryDamageEnemy(other.gameObject);

                return;
            }
        }


        // ==================================================
        // プレイヤーへのダメージ
        // ==================================================

        private void TryDamagePlayer(GameObject playerObj)
        {
            if (Time.time - _lastPlayerDamageTime
                < playerDamageCooldown)
                return;

            _lastPlayerDamageTime = Time.time;

            var hp =
                playerObj.GetComponent<PlayerHP>();

            if (hp != null)
            {
                hp.TakeDamage(damage);

                Debug.Log(
                    "針がプレイヤーに " +
                    damage +
                    " ダメージ！"
                );
            }
        }


        // ==================================================
        // 敵へのダメージ
        // ==================================================

        private void TryDamageEnemy(GameObject enemyObj)
        {
            if (Time.time - _lastEnemyDamageTime
                < enemyDamageCooldown)
                return;

            _lastEnemyDamageTime = Time.time;

            var ehp =
                enemyObj.GetComponent<EnemyHealth>();

            if (ehp != null)
            {
                ehp.TakeDamage(damage);

                Debug.Log(
                    "針が敵に " +
                    damage +
                    " ダメージ！"
                );
            }
        }


        // ==================================================
        // 手動モード
        // ==================================================

        private void Interval()
        {
            anim.CrossFade(
                DefaultState,
                0.3f,
                0,
                0
            );

            DelayFlg = true;
        }
    }
}
