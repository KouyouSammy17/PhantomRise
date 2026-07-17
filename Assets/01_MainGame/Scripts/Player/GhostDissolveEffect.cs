// ============================================================
// GhostDissolveEffect.cs
// 幽霊モデルのディゾルブ演出
// ShaderGhost.shadergraph の _Dissolve プロパティ（1 = 表示 / 0 = 消滅）を
// コードからトゥイーンする。シェーダー内蔵のノイズディゾルブをそのまま利用。
// PlayerStateMachine が自動追加するのでアタッチ不要。
// ============================================================

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GhostDissolveEffect : MonoBehaviour
{
    private static readonly int DissolveProp = Shader.PropertyToID("_Dissolve");

    [Tooltip("死亡時に追加でスポーンする VFX プレハブ（任意 — 例: Epic Toon FX の SoulCuteDeath）")]
    [SerializeField] private GameObject _deathVfxPrefab;

    [Tooltip("VFX スポーン位置のオフセット")]
    [SerializeField] private Vector3 _vfxOffset = new Vector3(0f, 0.5f, 0f);

    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();

        // 幽霊モデル配下の全レンダラーを取得
        var machine = GetComponent<PlayerStateMachine>();
        GameObject root = machine != null && machine.PlayerVisual != null
            ? machine.PlayerVisual
            : gameObject;

        _renderers = root.GetComponentsInChildren<Renderer>(true);
    }

    /// <summary>
    /// ディゾルブ開始。_Dissolve を 1 → 0 に duration 秒かけてトゥイーンする。
    /// _Dissolve を持たないマテリアル（目など）には無害。
    /// </summary>
    public void PlayDissolve(float duration)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(
            this.GetCancellationTokenOnDestroy());

        // 追加 VFX（未設定ならスキップ）
        if (_deathVfxPrefab != null)
        {
            GameObject fx = Instantiate(
                _deathVfxPrefab,
                transform.position + _vfxOffset,
                Quaternion.identity);
            Destroy(fx, 3f);
        }

        DissolveAsync(duration, _cts.Token).Forget();
    }

    /// <summary>リスタートなどで元に戻す場合用</summary>
    public void ResetDissolve()
    {
        _cts?.Cancel();
        SetDissolve(1f);
    }

    private async UniTaskVoid DissolveAsync(float duration, CancellationToken ct)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            ct.ThrowIfCancellationRequested();

            float t = Mathf.Clamp01(elapsed / duration);
            SetDissolve(1f - t);   // 1 → 0

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
            elapsed += Time.deltaTime;
        }

        SetDissolve(0f);
    }

    private void SetDissolve(float value)
    {
        if (_renderers == null) return;

        foreach (Renderer r in _renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetFloat(DissolveProp, value);
            r.SetPropertyBlock(_mpb);
        }
    }
}
