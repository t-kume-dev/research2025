using UnityEngine;
using UnityEngine.XR;
using MixedReality.Toolkit.Subsystems;
using System.Collections.Generic; // Listを使うために必要

public class GetGesture : MonoBehaviour
{
    // Inspectorから設定できるようにpublicにする
    public GameObject leftHandIndicator;
    public GameObject rightHandIndicator;

    private HandsAggregatorSubsystem aggregator;

    void Start()
    {
        // 以前のコードでaggregatorを取得
        List<HandsAggregatorSubsystem> subsystems = new List<HandsAggregatorSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);

        if (subsystems.Count > 0)
        {
            aggregator = subsystems[0];
            Debug.Log("Subsystem found: " + aggregator.subsystemDescriptor.id);
        }
        else
        {
            Debug.LogError("Failed to find a running subsystem.");
        }
    }

    void Update()
    {
        // aggregatorが取得できていなければ何もしない
        if (aggregator == null)
        {
            return;
        }

        // 1. 左手が認識されているかチェック
        // TryGetEntireHandは、手が認識されていれば true を返す
        bool isLeftHandTracked = aggregator.TryGetEntireHand(XRNode.LeftHand, out _);

        // 2. 右手が認識されているかチェック
        bool isRightHandTracked = aggregator.TryGetEntireHand(XRNode.RightHand, out _);

        Debug.Log($"Left Hand Tracked: {isLeftHandTracked}, Right Hand Tracked: {isRightHandTracked}");

        // 3. 結果に応じてインジケーターの表示を切り替える
        if (leftHandIndicator != null)
        {
            leftHandIndicator.SetActive(isLeftHandTracked);
        }

        if (rightHandIndicator != null)
        {
            rightHandIndicator.SetActive(isRightHandTracked);
        }
    }
}