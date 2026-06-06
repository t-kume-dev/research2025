using UnityEngine;
using UnityEngine.InputSystem; // Unityの新しいInput Systemを利用するために必要

/// <summary>
/// MRTK3で、特定のオブジェクトをターゲットせずに、グローバルな「Select」アクション
/// （HoloLens 2のエアタップやVRコントローラーのトリガーなど）を検出します。
/// </summary>
public class GlobalGestureDetector : MonoBehaviour
{
    [Tooltip("検出する左手のSelectアクション。MRTKのデフォルト設定を推奨します。")]
    [SerializeField]
    private InputActionProperty leftHandSelectAction;

    [Tooltip("検出する右手のSelectアクション。MRTKのデフォルト設定を推奨します。")]
    [SerializeField]
    private InputActionProperty rightHandSelectAction;

    private void OnEnable()
    {
        // スクリプトが有効になった時に、左右のアクションのイベント購読を開始します
        leftHandSelectAction.action.performed += OnSelectActionPerformed;
        rightHandSelectAction.action.performed += OnSelectActionPerformed;
    }

    private void OnDisable()
    {
        // スクリプトが無効になった時に、イベント購読を解除します（メモリリーク防止）
        leftHandSelectAction.action.performed -= OnSelectActionPerformed;
        rightHandSelectAction.action.performed -= OnSelectActionPerformed;
    }

    /// <summary>
    /// Selectアクションが実行された（指が閉じられた）時に呼び出されるメソッドです。
    /// </summary>
    /// <param name="context">入力アクションのコンテキスト情報</param>
    private void OnSelectActionPerformed(InputAction.CallbackContext context)
    {
        // どのコントローラー（手）がアクションを実行したかを取得
        var controllerName = context.control.device.displayName;

        // ★★★ジェスチャーを検出した際の処理をここに書きます★★★
        Debug.Log($"【MRTK3 Global】ジェスチャーを検知！ 実行した手: {controllerName}", this.gameObject);
    }
}
