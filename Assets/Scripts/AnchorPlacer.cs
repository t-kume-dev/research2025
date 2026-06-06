using UnityEngine;
using UnityEngine.XR.ARFoundation;
using MixedReality.Toolkit.SpatialManipulation;

public class AnchorPlacer : MonoBehaviour
{
    private TapToPlace tapToPlace;
    private ARAnchor anchor;

    void Start()
    {
        tapToPlace = GetComponent<TapToPlace>();
        if (tapToPlace == null)
        {
            Debug.LogError("TapToPlace component not found on this GameObject.");
            return;
        }

        // 配置が完了したときのイベントを購読
        tapToPlace.OnPlacingStopped.AddListener(PlaceAnchor);
    }

    private void PlaceAnchor()
    {
        // 既にアンカーがある場合は何もしない
        if (gameObject.GetComponent<ARAnchor>() != null)
        {
            return;
        }

        // このゲームオブジェクトにARAnchorコンポーネントを追加してアンカーを作成
        anchor = gameObject.AddComponent<ARAnchor>();
        Debug.Log("Anchor created at: " + anchor.transform.position);

        // アンカーを作成したら、TapToPlaceを無効にして再配置を防ぐ
        tapToPlace.enabled = false;
    }
}