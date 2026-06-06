using UnityEngine;
using UnityEngine.UI;

public class DynamicScrollView : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private RectTransform contentRect; // 中身（Content）

    [Header("設定")]
    [SerializeField] private float maxHeight = 400f;    // これ以上は伸びずにスクロールする
    [SerializeField] private float minHeight = 60f;     // 最低限の高さ
    [SerializeField] private float padding = 20f;       // 上下の余白

    private RectTransform myRect;

    void Start()
    {
        myRect = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (contentRect == null) return;

        // 1. コンテンツの現在の高さを取得（ボタンの数に応じて変わる）
        float contentHeight = contentRect.rect.height;

        // 2. ターゲットの高さを計算
        //    (コンテンツ + 余白) と (最大値) のうち、小さい方を採用
        float targetHeight = Mathf.Min(contentHeight + padding, maxHeight);

        //    (最低値) よりは小さくならないようにする
        targetHeight = Mathf.Max(targetHeight, minHeight);

        // 3. 自分の高さを更新
        Vector2 size = myRect.sizeDelta;
        
        // 値が変わる時だけ適用（負荷軽減）
        if (Mathf.Abs(size.y - targetHeight) > 1f) 
        {
            myRect.sizeDelta = new Vector2(size.x, targetHeight);
        }
    }
}