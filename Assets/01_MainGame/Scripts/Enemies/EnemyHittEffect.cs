using System.Collections;
using UnityEngine;

public class EnemyHitEffect : MonoBehaviour
{
    [Header("揺れ")]
    [SerializeField] private float shakeAmount = 0.08f;
    [SerializeField] private float shakeDuration = 0.12f;

    [Header("白フラッシュ")]
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private float emissionPower = 3f;

    private Transform visualRoot;
    private Renderer[] renderers;

    private Vector3 originalVisualPosition;

    private Coroutine hitCoroutine;

    // 元のEmission設定を保存
    private Color[][] originalEmissionColors;
    private Texture[][] originalEmissionMaps;
    private bool[][] originalEmissionEnabled;

    private void Awake()
    {
        EnemyController enemy = GetComponent<EnemyController>();

        if (enemy != null)
        {
            visualRoot = enemy.GetHitEffectRoot();
        }
        else
        {
            Debug.LogError("EnemyControllerが見つかりません！");
            return;
        }

        if (visualRoot == null)
        {
            Debug.LogError($"{name} にHitEffectRootが設定されていません！");
            return;
        }

        // 見た目の部分だけ取得
        renderers = visualRoot.GetComponentsInChildren<Renderer>();

        // 元の位置を保存
        originalVisualPosition = visualRoot.localPosition;

        // 元のEmission設定を保存
        SaveOriginalEmission();
    }

    public void PlayHitEffect()
    {
        if (visualRoot == null)
            return;

        if (hitCoroutine != null)
        {
            StopCoroutine(hitCoroutine);

            // 前回の演出を完全に元に戻す
            visualRoot.localPosition = originalVisualPosition;
            RestoreEmission();
        }

        hitCoroutine = StartCoroutine(HitCoroutine());
    }

    private IEnumerator HitCoroutine()
    {
        SetWhiteEmission();

        float timer = 0f;

        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;

            float x = Mathf.Sin(timer * 80f) * shakeAmount;

            visualRoot.localPosition =
                originalVisualPosition + new Vector3(x, 0f, 0f);

            yield return null;
        }

        visualRoot.localPosition = originalVisualPosition;

        yield return new WaitForSeconds(flashDuration);

        RestoreEmission();

        hitCoroutine = null;
    }
    /// <summary>
    /// 被弾前のEmission設定を保存
    /// </summary>
    private void SaveOriginalEmission()
    {
        originalEmissionColors = new Color[renderers.Length][];
        originalEmissionMaps = new Texture[renderers.Length][];
        originalEmissionEnabled = new bool[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].materials;

            originalEmissionColors[i] = new Color[materials.Length];
            originalEmissionMaps[i] = new Texture[materials.Length];
            originalEmissionEnabled[i] = new bool[materials.Length];

            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];

                if (material.HasProperty("_EmissionColor"))
                {
                    originalEmissionColors[i][j] =
                        material.GetColor("_EmissionColor");
                }

                if (material.HasProperty("_EmissionMap"))
                {
                    originalEmissionMaps[i][j] =
                        material.GetTexture("_EmissionMap");
                }

                originalEmissionEnabled[i][j] =
                    material.IsKeywordEnabled("_EMISSION");
            }
        }
    }

    /// <summary>
    /// 被弾時に白いEmissionだけを表示
    /// </summary>
    private void SetWhiteEmission()
    {
        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.materials)
            {
                if (!material.HasProperty("_EmissionColor"))
                    continue;

                // Emissionを有効化
                material.EnableKeyword("_EMISSION");

                // Emission Mapを一時的に外す
                if (material.HasProperty("_EmissionMap"))
                {
                    material.SetTexture("_EmissionMap", null);
                }

                // 白く発光
                material.SetColor(
                    "_EmissionColor",
                    Color.white * emissionPower
                );
            }
        }
    }

    /// <summary>
    /// 元のEmission設定に戻す
    /// </summary>
    private void RestoreEmission()
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].materials;

            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];

                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor(
                        "_EmissionColor",
                        originalEmissionColors[i][j]
                    );
                }

                if (material.HasProperty("_EmissionMap"))
                {
                    material.SetTexture(
                        "_EmissionMap",
                        originalEmissionMaps[i][j]
                    );
                }

                if (originalEmissionEnabled[i][j])
                {
                    material.EnableKeyword("_EMISSION");
                }
                else
                {
                    material.DisableKeyword("_EMISSION");
                }
            }
        }
    }
}