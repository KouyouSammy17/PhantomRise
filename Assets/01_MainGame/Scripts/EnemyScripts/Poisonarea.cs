using UnityEngine;

public class Poisonarea : MonoBehaviour
{
    /// <summary>MushroomEnemySkill から生成時に設定されるダメージ量</summary>
    public int damage = 10;

    // 一度ダメージを与えた敵を覚えておく（重複ダメージ防止）
    private System.Collections.Generic.HashSet<EnemyController> _damaged
        = new System.Collections.Generic.HashSet<EnemyController>();

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
        EnemyController enemy = other.GetComponentInParent<EnemyController>();
        if (enemy == null) return;
        if (enemy.IsHijacked) return;   // 乗っ取り中の自分自身には当たらない
        if (_damaged.Contains(enemy)) return;

        _damaged.Add(enemy);
        enemy.TakeDamage(damage);
        Debug.Log($"[Poisonarea] {enemy.name} に {damage} 毒ダメージ");
    }
}
