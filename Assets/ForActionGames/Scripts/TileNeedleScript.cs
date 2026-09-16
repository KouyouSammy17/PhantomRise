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

        [Header("Audio")]
        [Tooltip("針が出るときに鳴らす AudioSource（3D設定にしておく）")]
        [SerializeField] private AudioSource needleAudioSource;

        // ステージ開始演出中は針を止める
        private bool _isStageStarting = false;

        void Awake()
        {
            // StageStartSequence.Start() から SetStageStarting() が
            // 先に呼ばれても大丈夫なように Awake で取得する
            anim = this.GetComponent<Animator>();
        }

        void Start()
        {
            if (anim == null)
                anim = this.GetComponent<Animator>();

            if (autoCycle)
                StartCoroutine(AutoCycleRoutine(startDelay));
        }


        // ==================================================
        // ステージ開始演出中の一時停止
        // StageStartSequence から呼ばれる
        // ==================================================

        public void SetStageStarting(bool value)
        {
            _isStageStarting = value;

            if (!value) return;

            // 針を戻して音も止める
            if (anim != null)
            {
                anim.CrossFade(
                    DefaultState,
                    0.1f,
                    0,
                    0
                );
            }

            if (needleAudioSource != null && needleAudioSource.isPlaying)
                needleAudioSource.Stop();

            DelayFlg = true;
        }

        private IEnumerator AutoCycleRoutine(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            while (true)
            {
                // 開始演出が終わるまで待つ
                while (_isStageStarting)
                    yield return null;

                // ==============================
                // 針を出す
                // ==============================

                anim.CrossFade(
                    StabState,
                    0.1f,
                    0,
                    0
                );

                PlayNeedleSound();

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
            // 開始演出中は何もしない
            if (_isStageStarting) return;

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

                    PlayNeedleSound();

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
        // 効果音
        //
        // 3D 音源として鳴らす。
        // AudioSource 側で Spatial Blend = 1（3D）、
        // Volume Rolloff = Linear、Max Distance を
        // 聞こえてほしい範囲に設定しておくこと。
        // （Logarithmic のままだと Max Distance 以降も
        //   音量が下がりきらず、どこにいても聞こえてしまう）
        // ==================================================

        private void PlayNeedleSound()
        {
            if (_isStageStarting) return;

            if (needleAudioSource == null || needleAudioSource.clip == null)
                return;

            // 遠くの針は鳴らさない（同時発音数の節約）
            Transform listener = GetListenerTransform();

            if (listener != null)
            {
                float sqrDistance = (listener.position
                                     - needleAudioSource.transform.position).sqrMagnitude;

                float maxDistance = needleAudioSource.maxDistance;

                if (sqrDistance > maxDistance * maxDistance)
                    return;
            }

            needleAudioSource.Play();
        }


        // AudioListener はシーンに1つなので全針で共有する
        private static Transform _listener;

        private static Transform GetListenerTransform()
        {
            if (_listener != null) return _listener;

            AudioListener listener = FindFirstObjectByType<AudioListener>();

            _listener = (listener != null) ? listener.transform : null;

            return _listener;
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
