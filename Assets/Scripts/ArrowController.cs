using UnityEngine;

public class ArrowController : MonoBehaviour
{
    // ターゲット（Walletなど）のTransform
    private Transform targetToPointAt;

    void Update()
    {
        // ターゲットが設定されている場合のみ実行
        if (targetToPointAt != null)
        {
            // 矢印をターゲットの方向に向ける
            transform.LookAt(targetToPointAt);
        }
    }

    /// <summary>
    /// 矢印を指定したターゲットの方向に向ける
    /// </summary>
    public void PointAtTarget(Transform target)
    {
        targetToPointAt = target;
        // 矢印を（非表示だった場合）表示する
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 矢印を非表示にする
    /// </summary>
    public void Hide()
    {
        targetToPointAt = null;
        // 矢印を非表示にする
        gameObject.SetActive(false);
    }
}