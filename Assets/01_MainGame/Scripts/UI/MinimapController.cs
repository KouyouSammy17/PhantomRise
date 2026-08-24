// MinimapController.cs
// ミニマップカメラをプレイヤーに追従させ、敵アイコンを表示する

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MinimapController : MonoBehaviour
{
    [Header("=== カメラ ===")]
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private Transform player;
    [SerializeField] private float cameraHeight = 30f;

    [Header("=== アイコン ===")]
    [SerializeField] private RectTransform minimapRect;   // RawImageのRectTransform
    [SerializeField] private GameObject enemyIconPrefab;  // 小さい赤丸のPrefab
    [SerializeField] private RectTransform playerIcon;    // プレイヤーアイコン

    [Header("=== 設定 ===")]
    [SerializeField] private float mapWorldSize = 40f;    // カメラのOrtho Sizeの2倍

    private List<(Transform enemy, RectTransform icon)> _enemies = new();

    private void LateUpdate()
    {
        if (player == null || minimapCamera == null) return;

        // カメラをプレイヤーに追従（X,Z のみ、Yは固定）
        minimapCamera.transform.position = new Vector3(
            player.position.x,
            player.position.y + cameraHeight,
            player.position.z
        );

        // プレイヤーアイコンは常にミニマップ中央
        if (playerIcon != null)
            playerIcon.anchoredPosition = Vector2.zero;

        // 敵アイコンの位置を更新
        foreach (var (enemy, icon) in _enemies)
        {
            if (enemy == null) { icon.gameObject.SetActive(false); continue; }
            icon.gameObject.SetActive(true);
            icon.anchoredPosition = WorldToMinimapPos(enemy.position);
        }
    }

    private Vector2 WorldToMinimapPos(Vector3 worldPos)
    {
        Vector3 offset = worldPos - player.position;
        float mapSize = minimapRect.sizeDelta.x; // ミニマップのピクセルサイズ

        float x = (offset.x / mapWorldSize) * mapSize;
        float y = (offset.z / mapWorldSize) * mapSize;
        return new Vector2(x, y);
    }

    /// <summary>敵をミニマップに登録する（EnemyController の Start から呼ぶ）</summary>
    public void RegisterEnemy(Transform enemy)
    {
        var iconGo = Instantiate(enemyIconPrefab, minimapRect);
        var icon = iconGo.GetComponent<RectTransform>();
        _enemies.Add((enemy, icon));
    }

    /// <summary>敵を登録解除する（EnemyHealth の死亡処理から呼ぶ）</summary>
    public void UnregisterEnemy(Transform enemy)
    {
        int idx = _enemies.FindIndex(e => e.enemy == enemy);
        if (idx < 0) return;
        Destroy(_enemies[idx].icon.gameObject);
        _enemies.RemoveAt(idx);
    }
}