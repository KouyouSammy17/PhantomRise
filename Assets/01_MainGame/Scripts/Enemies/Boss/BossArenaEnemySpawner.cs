using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossArenaEnemySpawner : MonoBehaviour
{
    [Header("ボス・プレイヤー")]
    [SerializeField] private Transform boss;
    [SerializeField] private Transform player;

    [Header("敵プレハブ")]
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("スポーンポイント")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("スポーン設定")]
    [SerializeField] private float spawnInterval = 15f;
    [SerializeField] private int maxEnemyCount = 3;

    [Header("安全距離")]
    [SerializeField] private float minDistanceFromPlayer = 10f;
    [SerializeField] private float minDistanceFromBoss = 8f;

    [SerializeField] private float spawnPointEnemyCheckRadius = 2f;

    [Header("デバッグ")]
    [SerializeField] private bool showDebugLog = true;

    private List<GameObject> spawnedEnemies = new List<GameObject>();

    private Coroutine spawnCoroutine;

    private bool isBattleStarted = false;


    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (boss == null)
        {
            GameObject bossObject = GameObject.FindGameObjectWithTag("Boss");

            if (bossObject != null)
            {
                boss = bossObject.transform;
            }
        }
    }

    /// <summary>
    /// ボス戦開始
    /// BossRoomTriggerなどから呼び出す
    /// </summary>
    public void StartBossBattle()
    {
        if (isBattleStarted)
            return;

        isBattleStarted = true;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (boss == null)
        {
            Debug.LogWarning("[BossArenaEnemySpawner] Bossが設定されていません。");
        }

        if (player == null)
        {
            Debug.LogWarning("[BossArenaEnemySpawner] Playerが見つかりません。");
        }

        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);

        spawnCoroutine = StartCoroutine(SpawnRoutine());

        if (showDebugLog)
            Debug.Log("[BossArenaEnemySpawner] ボス戦開始 → 敵スポーン開始");
    }


    /// <summary>
    /// ボス戦終了
    /// </summary>
    public void StopBossBattle()
    {
        isBattleStarted = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        if (showDebugLog)
            Debug.Log("[BossArenaEnemySpawner] ボス戦終了 → 敵スポーン停止");
    }


    /// <summary>
    /// 一定時間ごとに敵を出現させる
    /// </summary>
    private IEnumerator SpawnRoutine()
    {
        // ボス戦開始直後には出さない
        yield return new WaitForSeconds(spawnInterval);

        while (isBattleStarted)
        {
            RemoveNullEnemies();

            // 最大数未満ならスポーン
            if (spawnedEnemies.Count < maxEnemyCount)
            {
                SpawnEnemy();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }


    /// <summary>
    /// 敵を1体スポーン
    /// </summary>
    private void SpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("[BossArenaEnemySpawner] 敵プレハブが設定されていません。");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[BossArenaEnemySpawner] スポーンポイントが設定されていません。");
            return;
        }

        RemoveNullEnemies();

        if (spawnedEnemies.Count >= maxEnemyCount)
            return;


        // 条件を満たすスポーンポイントを探す
        List<Transform> availablePoints = new List<Transform>();

        foreach (Transform point in spawnPoints)
        {
            if (point == null)
                continue;

            if (IsSafeSpawnPoint(point))
            {
                availablePoints.Add(point);
            }
        }


        // 安全な場所がなかった
        if (availablePoints.Count == 0)
        {
            if (showDebugLog)
                Debug.Log("[BossArenaEnemySpawner] 安全なスポーンポイントがありません。");

            return;
        }


        // ランダムなスポーンポイント
        Transform spawnPoint =
            availablePoints[Random.Range(0, availablePoints.Count)];


        // ランダムな敵
        GameObject enemyPrefab =
            enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];


        GameObject enemy =
            Instantiate(
                enemyPrefab,
                spawnPoint.position,
                spawnPoint.rotation);

        //敵のモードを変更

        EnemyController controller = enemy.GetComponent<EnemyController>();

        if (controller != null)
        {
            controller.StartBossArenaCombat(player);
        }


        spawnedEnemies.Add(enemy);


        if (showDebugLog)
        {
            Debug.Log(
                $"[BossArenaEnemySpawner] 敵スポーン: {enemy.name} / " +
                $"位置: {spawnPoint.position}");
        }
    }


    /// <summary>
    /// プレイヤーとボスから十分離れているか確認
    /// </summary>
    private bool IsSafeSpawnPoint(Transform point)
    {
        // プレイヤーとの距離
        if (player != null)
        {
            float playerDistance =
                Vector3.Distance(
                    point.position,
                    player.position);

            if (playerDistance < minDistanceFromPlayer)
                return false;
        }

        //敵が近くにいるか確認
        if (IsEnemyNearSpawnPoint(point))
        {
            return false;
        }


        // ボスとの距離
        if (boss != null)
        {
            float bossDistance =
                Vector3.Distance(
                    point.position,
                    boss.position);

            if (bossDistance < minDistanceFromBoss)
                return false;
        }


        return true;
    }


    private bool IsEnemyNearSpawnPoint(Transform point)
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy == null)
                continue;

            float distance = Vector3.Distance(
                point.position,
                enemy.transform.position);

            if (distance <= spawnPointEnemyCheckRadius)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Destroyされた敵をリストから削除
    /// </summary>
    private void RemoveNullEnemies()
    {
        spawnedEnemies.RemoveAll(enemy => enemy == null);
    }


    /// <summary>
    /// 現在アリーナに存在する敵の数
    /// </summary>
    public int CurrentEnemyCount
    {
        get
        {
            RemoveNullEnemies();
            return spawnedEnemies.Count;
        }
    }


    /// <summary>
    /// ボス戦終了時などに、スポーンした敵を全て削除する場合に使用
    /// </summary>
    public void ClearAllEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }

        spawnedEnemies.Clear();

        if (showDebugLog)
            Debug.Log("[BossArenaEnemySpawner] アリーナ内の敵を全て削除しました。");
    }


    private void OnDrawGizmosSelected()
    {
        if (spawnPoints == null)
            return;

        foreach (Transform point in spawnPoints)
        {
            if (point == null)
                continue;

            Gizmos.DrawWireSphere(
                point.position,
                0.5f);

            // プレイヤーからの安全距離
            Gizmos.DrawWireSphere(
                point.position,
                minDistanceFromPlayer);
        }
    }
}