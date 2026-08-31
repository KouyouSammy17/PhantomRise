// ============================================================
// CursorVisibility.cs
// マウスカーソルの表示を制御する。
//
// パッド前提のゲームなので既定では非表示にする。
// UI Manager にアタッチしてあるので、UI Manager が置いてある
// シーンならどこでも効く。
//
// 実装メモ:
//   ・Unity はウィンドウのフォーカスが外れるとカーソル設定を戻すので、
//     フォーカスが返ってきたら再適用する
//   ・Confined はウィンドウ外へ出さないだけで座標は動かさない。
//     Locked は画面中央に固定される（エディタでは Esc で解除される）
// ============================================================

using UnityEngine;

public class CursorVisibility : MonoBehaviour
{
    [Tooltip("カーソルを消す。パッド操作前提なら ON のまま")]
    [SerializeField] private bool hideCursor = true;

    [Tooltip("非表示のときのロック方法")]
    [SerializeField] private CursorLockMode lockMode = CursorLockMode.Confined;

    private void Start() => Apply();

    private void OnApplicationFocus(bool hasFocus)
    {
        // フォーカスが戻るとカーソル設定がリセットされるので入れ直す
        if (hasFocus) Apply();
    }

    private void Apply()
    {
        Cursor.visible   = !hideCursor;
        Cursor.lockState = hideCursor ? lockMode : CursorLockMode.None;
    }

    /// <summary>外から切り替えたいとき用（デバッグメニューなど）。</summary>
    public void SetHidden(bool hidden)
    {
        hideCursor = hidden;
        Apply();
    }
}
