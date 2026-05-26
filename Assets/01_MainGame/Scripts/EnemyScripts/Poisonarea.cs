using UnityEngine;

public class Poisonarea : MonoBehaviour
{
    /// <summary>MushroomEnemySkill から生成時に設定されるダメージ量</summary>
    public int damage = 10;

    // 一度ダメージを与えた敵を覚えておく（重複ダメージ防止）
    private System.Collections.Generic.HashSet<EnemyController> _damaged
        = new System.Collections.Generic.HashSet<EnemyController>();

    public bool isHijackedSkill = false;

    void Start()
    {
        // Update で毎フレーム Invoke を重複スケジュールしていたバグを修正 → Start で一度だけ
        Invoke("Delete", 2f);
    }

    void Delete()
    {
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 乗っ取り中に使った場合Enemy にだけ当たる
        if (isHijackedSkill)
        {
            EnemyController enemy = other.GetComponentInParent<EnemyController>();

            if (enemy == null) return;
            if (enemy.IsHijacked) return;
            if (_damaged.Contains(enemy)) return;

            _damaged.Add(enemy);

            enemy.TakeDamage(damage);

            Debug.Log($"[Poisonarea] {enemy.name} に {damage} 毒ダメージ");
        }

        // 通常の敵が使った場合Player にだけ当たる
        else
        {
            if (!other.CompareTag("Player")) return;

            PlayerStateMachine machine =
                other.GetComponentInParent<PlayerStateMachine>();

            if (machine != null &&
                machine.CurrentStateName == nameof(HijackedState))
            {
                machine.PlayerHP?.TakeDamage(damage);

                Debug.Log($"[Poisonarea] プレイヤーに {damage} 毒ダメージ");
            }
        }
    }
}
