using System.Collections.Generic;
using UnityEngine;

public class Poisonarea : MonoBehaviour
{
    /// <summary>MushroomEnemySkill から生成時に設定されるダメージ量</summary>
    [SerializeField] private int damage = 10;

    // 一度毒状態にした対象を覚える（重複付与防止）
    private HashSet<EnemyController> _damaged =
        new HashSet<EnemyController>();

    public bool isHijackedSkill = false;

    public int Damage
    {
        get => damage;
        set => damage = value;
    }

    void Start()
    {
        Invoke(nameof(Delete), 2f);
    }

    void Delete()
    {
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // ─────────────────────────────
        // 乗っ取り中
        // 敵に毒を付与
        // ─────────────────────────────
        if (isHijackedSkill)
        {
            EnemyController enemy =
                other.GetComponentInParent<EnemyController>();

            if (enemy == null) return;
            if (enemy.IsHijacked) return;
            if (_damaged.Contains(enemy)) return;

            _damaged.Add(enemy);

            EnemyHealth hp =
                enemy.GetComponent<EnemyHealth>();

            if (hp != null)
            {
                hp.ApplyPoison(
                    duration: 5f,
                    interval: 1f,
                    percent: 0.15f);

                Debug.Log(
                    $"[Poisonarea] {enemy.name} を毒状態にした");
            }
        }

        // ─────────────────────────────
        // 通常時
        // プレイヤーに毒を付与
        // ─────────────────────────────
        else
        {
            if (!other.CompareTag("Player"))
                return;

            PlayerStateMachine machine =
                other.GetComponentInParent<PlayerStateMachine>();

            if (machine != null &&
                machine.CurrentStateName == nameof(HijackedState))
            {
                machine.PlayerHP?.ApplyPoison(
                    duration: 5f,
                    interval: 1f,
                    percent: 0.15f);

                Debug.Log(
                    "[Poisonarea] プレイヤーを毒状態にした");
            }
        }
    }
}