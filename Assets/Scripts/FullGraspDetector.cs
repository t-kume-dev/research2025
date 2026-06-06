using UnityEngine;
using UnityEngine.XR;
using MixedReality.Toolkit.Subsystems;
using MixedReality.Toolkit; // HandJoint enum を使うために必要
using MixedReality.Toolkit.Input;
using System.Collections.Generic;
using System.IO;

public class FullGraspDetector : MonoBehaviour
{
    public float distanceThreshold = 0.05f; // 5cm
    public int requiredFingerCount = 3;

    public PhotoTaker photoTaker; // PhotoTakerへの参照を追加

    public float captureCooldown = 5.0f; // 3秒のクールダウン
    private bool canTakePhoto = true; // 撮影可能かどうかのフラグ
    private float cooldownTimer = 0f;
    
    private HandsAggregatorSubsystem aggregator;
    public GameObject objectToActivate;

    private bool wasGrabbingLastFrame = false;

    private readonly List<TrackedHandJoint> fingerTips = new List<TrackedHandJoint>
    {
        TrackedHandJoint.IndexTip,
        TrackedHandJoint.MiddleTip,
        TrackedHandJoint.RingTip,
        TrackedHandJoint.LittleTip
    };

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


        if (photoTaker != null)
        {
            // OnPhotoCapturedAsJPGイベントが発生したら、HandleCapturedJPGメソッドを呼び出すように登録
            photoTaker.OnPhotoCapturedAsJPG += HandleCapturedJPG;
        }
    }

    void Update()
    {
        if(aggregator == null || objectToActivate == null) return;

        bool isGrabbing = IsHandGrabbing(XRNode.LeftHand) || IsHandGrabbing(XRNode.RightHand);

        objectToActivate.SetActive(isGrabbing);

        

        // クールダウンタイマーの処理
        if (!canTakePhoto)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                canTakePhoto = true;
                Debug.Log("撮影準備完了。");
            }
        }
        
        // 撮影可能な状態かチェック
        if (canTakePhoto && isGrabbing && !wasGrabbingLastFrame)
        {
            // PhotoTakerが割り当てられていれば撮影を指示
            if (photoTaker != null)
            {
                photoTaker.TakePhoto();
                canTakePhoto = false; // 撮影フラグを倒す
                cooldownTimer = captureCooldown; // タイマーをセット
                Debug.Log("掴むジェスチャーを認識！撮影をリクエストします。");
            }
        }

        wasGrabbingLastFrame = isGrabbing;
    }

    private bool IsHandGrabbing(XRNode handNode)
    {
        if (!aggregator.TryGetJoint(TrackedHandJoint.Palm, handNode, out HandJointPose palmPose))
        {
            return false; // 手のひらが追跡できていなければ false
        }

        int closedFingers = 0;

        foreach (var tip in fingerTips)
        {
            // 指先の位置を取得
            if (aggregator.TryGetJoint(tip, handNode, out HandJointPose tipPose))
            {
                // 3. 手のひらと指先の距離を計算
                float distance = Vector3.Distance(palmPose.Position, tipPose.Position);

                // 4. 距離がしきい値より短ければ、指が閉じているとカウント
                if (distance < distanceThreshold)
                {
                    closedFingers++;
                }
            }
        }

        return closedFingers >= requiredFingerCount;
    }

    // JPGデータを受け取ったときに実行されるメソッド
    private void HandleCapturedJPG(byte[] jpgData)
    {
        Debug.Log($"GraspCheckerがJPGデータを受け取りました。サイズ: {jpgData.Length / 1024} KB");

        // ★★★
        // ここで受け取った jpgData を使って、
        // 将来的にAPI送信などの処理を実装します。
        // ★★★
        try
        {
            // 1. 保存先のフォルダパスを取得
            // Application.persistentDataPathは、アプリが安全にファイルを読み書きできる永続的なフォルダを指します。
            string directoryPath = Application.persistentDataPath;

            // 2. ユニークなファイル名を生成
            string fileName = string.Format("Capture_{0:yyyy-MM-dd-HH-mm-ss}.jpg", System.DateTime.Now);

            // 3. 完全なファイルパスを結合
            string filePath = Path.Combine(directoryPath, fileName);

            // 4. バイト配列をファイルとして書き込む
            File.WriteAllBytes(filePath, jpgData);

            Debug.Log($"写真を保存しました: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"写真の保存に失敗しました: {e.Message}");
        }
    }

    void OnDestroy()
    {
        if (photoTaker != null)
        {
            photoTaker.OnPhotoCapturedAsJPG -= HandleCapturedJPG;
        }
    }
}