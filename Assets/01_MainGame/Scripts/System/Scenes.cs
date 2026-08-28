// ============================================================
// Scenes.cs
// シーン名を1か所にまとめる。
//
// 各シーンの Inspector に文字列を書くと、
// シーンごとに食い違って「Scene couldn't be loaded」の原因になる。
// シーン名はここだけを直せばよいようにする。
//
// Build Settings の並び順と一致させること。
// ============================================================

public static class Scenes
{
    public const string Title    = "NewTitle";     // buildIndex 0
    public const string Tutorial = "NewTutorial";  // buildIndex 1
    public const string Stage2   = "Stage2";       // buildIndex 2
}
