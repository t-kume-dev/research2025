using UnityEngine;

public class UIToggler : MonoBehaviour
{
    [Header("トグル対象のUI")]
    [SerializeField]
    private GameObject mainUICanvas; // ★ メインのCanvas（アイテムリスト）をここに設定

    void Start()
    {
        // 念のため、対象が設定されているか確認
        if (mainUICanvas == null)
        {
            Debug.LogError("トグル対象のUI (Main UI Canvas) が設定されていません！");
        }
    }

    /// <summary>
    /// このメソッドをボタンの OnClick イベントから呼び出す
    /// </summary>
    public void ToggleVisibility()
    {
        if (mainUICanvas == null) return;

        // 現在のアクティブ状態を取得し、それを反転させる
        bool currentVisibility = mainUICanvas.activeSelf;
        mainUICanvas.SetActive(!currentVisibility);

        if (currentVisibility)
        {
            Debug.Log("メインUIを非表示にしました。");
        }
        else
        {
            Debug.Log("メインUIを表示しました。");
        }
    }
}