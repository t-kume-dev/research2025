using UnityEngine;
using UnityEngine.XR;
using MixedReality.Toolkit.Subsystems;
using System.Collections.Generic; // Listを使うために必要

public class GraspDetector : MonoBehaviour
{
    [Tooltip("つかむジェスチャーでアクティブにしたいオブジェクトをここに設定します。")]
    public GameObject objectToActivate;

    private HandsAggregatorSubsystem aggregator;
    
    void Start()
    {
        // HandsAggregatorSubsystemのインスタンスを取得
        List<HandsAggregatorSubsystem> subsystems = new List<HandsAggregatorSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);

        if (subsystems.Count > 0)
        {
            aggregator = subsystems[0];
            Debug.Log("HandsAggregatorSubsystemが見つかりました。");
        }
        else
        {
            Debug.LogError("HandsAggregatorSubsystemが見つかりませんでした。MRTKのセットアップを確認してください。");
        }

        // 念のため、最初にオブジェクトを非表示にしておく
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(false);
        }
    }

    void Update()
    {
        // aggregatorまたは対象オブジェクトがなければ何もしない
        if (aggregator == null || objectToActivate == null)
        {
            return;
        }

        // 1. 左手のピンチ状態をチェック
        // TryGetPinchProgressは、手が追跡できていればtrueを返す
        bool isLeftHandPinching = false;
        if (aggregator.TryGetPinchProgress(XRNode.LeftHand, out bool isLeftReady, out bool isLeftPinchingValue, out float leftPinchAmount))
        {
            // isLeftPinchingValueがtrueなら、ピンチしていると判断
            isLeftHandPinching = isLeftPinchingValue;
        }

        // 2. 右手のピンチ状態をチェック
        bool isRightHandPinching = false;
        if (aggregator.TryGetPinchProgress(XRNode.RightHand, out bool isRightReady, out bool isRightPinchingValue, out float rightPinchAmount))
        {
            // isRightPinchingValueがtrueなら、ピンチしていると判断
            isRightHandPinching = isRightPinchingValue;
        }
        
        // 3. どちらかの手がピンチしていたらオブジェクトをアクティブに、そうでなければ非アクティブにする
        if (isLeftHandPinching || isRightHandPinching)
        {
            objectToActivate.SetActive(true);
        }
        else
        {
            objectToActivate.SetActive(false);
        }
    }
}