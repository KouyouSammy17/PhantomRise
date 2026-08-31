// ============================================================
// StageStats.cs
// ステージ 1 回の挑戦ぶんの記録（クリア判定に使う）。
//
// リスタートは同じシーンを読み直すだけなので、
// シーンをまたいでも値が残るよう static にしてある。
//
// 別のシーンに移った時点で「別の挑戦」とみなしてリセットする。
// つまり:
//   ・ゲームオーバー → リスタート → 死亡数と時間は引き継ぐ
//   ・チュートリアル → Stage2       → まっさらから始まる
// これが無いと、死ぬたびにシーンが再読み込みされて
// 「ノーデス」条件が必ず成立してしまう。
// ============================================================

public static class StageStats
{
    /// <summary>このステージに費やした合計時間（秒）</summary>
    public static float ElapsedTime { get; private set; }

    /// <summary>このステージで死んだ回数</summary>
    public static int Deaths { get; private set; }

    /// <summary>今記録しているシーン名</summary>
    private static string _sceneName;

    /// <summary>
    /// シーン開始時に GameManager から呼ぶ。
    /// 前回と違うシーンなら記録をリセットする。
    /// </summary>
    public static void BeginScene(string sceneName)
    {
        if (_sceneName == sceneName) return;   // リスタート → 引き継ぐ

        _sceneName  = sceneName;
        ElapsedTime = 0f;
        Deaths      = 0;
    }

    public static void Tick(float deltaTime) => ElapsedTime += deltaTime;

    public static void RegisterDeath() => Deaths++;

    /// <summary>タイトルへ戻るときなど、明示的に消したいとき用。</summary>
    public static void Clear()
    {
        _sceneName  = null;
        ElapsedTime = 0f;
        Deaths      = 0;
    }
}
