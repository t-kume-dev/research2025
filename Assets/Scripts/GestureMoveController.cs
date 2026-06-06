using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR; 
using MixedReality.Toolkit.Subsystems; 
using MixedReality.Toolkit.Input; 
using MixedReality.Toolkit; 

public class GestureMoveController : MonoBehaviour
{
    private RegistrationManager registrationManager; 
    private Transform anchorParent; 
    private Coroutine grabTimerCoroutine; 
    
    private bool isGrabbed = false; 

    private HandsAggregatorSubsystem handsSubsystem;
    private XRNode handInTrigger = XRNode.RightHand;
    
    // カウンター（複数パーツ接触対策）
    private int handPartsInTriggerCount = 0;
    private Transform lastHandTransform = null;

    // 色変更用
    private MeshRenderer sphereRenderer;
    private Color originalColor;
    public Color countingColor = Color.yellow; 
    public Color grabbedColor = Color.green;  

    // ★★★ 1. ずらす量を設定する変数を追加 ★★★
    // X: 横, Y: 手の甲(正)/手のひら(負), Z: 指先方向
    [Header("位置調整")]
    public Vector3 wristOffset = new Vector3(0, -0.05f, 0.08f); 

    // パー判定用（手のひらからの距離）
    private readonly List<TrackedHandJoint> fingerTips = new List<TrackedHandJoint>
    {
        TrackedHandJoint.IndexTip,
        TrackedHandJoint.MiddleTip,
        TrackedHandJoint.RingTip,
        TrackedHandJoint.LittleTip 
    };
    
    // ★ 手のひら基準なので、しきい値は 8cm～10cm くらいが適切
    public float handOpenThreshold = 0.1f; 

    void Start()
    {
        registrationManager = FindObjectOfType<RegistrationManager>();
        if (transform.parent != null) anchorParent = this.transform.parent;
        else anchorParent = this.transform;

        handsSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>();
        
        sphereRenderer = GetComponentInChildren<MeshRenderer>();
        if (sphereRenderer != null) originalColor = sphereRenderer.material.color;
    }

    // --- 掴む判定（2秒ルール・カウンター方式） ---

    private void OnTriggerEnter(Collider other)
    {
        if (isGrabbed || handsSubsystem == null) return;

        if (IsHand(other, out XRNode node))
        {
            handPartsInTriggerCount++;
            lastHandTransform = other.transform;
            handInTrigger = node;

            if (handPartsInTriggerCount == 1 && grabTimerCoroutine == null)
            {
                Debug.Log("【近接】手がアイテムに触れました。2秒タイマーを開始...");
                SetColor(countingColor);
                grabTimerCoroutine = StartCoroutine(GrabTimer());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // まだ掴んでいない（計測中）時だけ、離れたらキャンセル
        if (!isGrabbed && IsHand(other, out _))
        {
            handPartsInTriggerCount--;
            if (handPartsInTriggerCount < 0) handPartsInTriggerCount = 0;

            if (handPartsInTriggerCount == 0)
            {
                if (grabTimerCoroutine != null)
                {
                    Debug.Log("【近接】手が完全に離れました。タイマーキャンセル。");
                    StopCoroutine(grabTimerCoroutine);
                    grabTimerCoroutine = null;
                    SetColor(originalColor);
                }
            }
        }
    }

    private IEnumerator GrabTimer()
    {
        yield return new WaitForSeconds(2.0f); 

        // 2秒経過後、まだ手が触れていれば「掴んだ」と確定
        if (handPartsInTriggerCount > 0)
        {
            Debug.Log("【近接】2秒経過。ロックオンしました。");
            grabTimerCoroutine = null;
            
            bool shouldFollow = registrationManager.HandleItemGrabbed(anchorParent.gameObject);

            if (shouldFollow) // 日常モード
            {
                isGrabbed = true; // ★ ロックオン状態
                SetColor(grabbedColor); 
            }
            else // 準備モード
            {
                isGrabbed = false;
                handPartsInTriggerCount = 0;
            }
        }
        else
        {
             grabTimerCoroutine = null;
             SetColor(originalColor);
        }
    }
    
    // --- 追従と解放（Update） ---

    void Update()
    {
        // 掴んでいない、またはサブシステムがない場合は何もしない
        if (!isGrabbed || handsSubsystem == null) return;

        // 1. 追従処理（手首基準）
        // 手首はオクルージョンに強いので、移動中の追従に最適
        if (handsSubsystem.TryGetJoint(TrackedHandJoint.Wrist, handInTrigger, out HandJointPose wristPose))
        {
            // 手首の位置 + オフセット に移動
            // (手首の回転に合わせてオフセット方向も回転させる)
            Vector3 targetPosition = wristPose.Position + (wristPose.Rotation * wristOffset);

            anchorParent.position = targetPosition;
            anchorParent.rotation = wristPose.Rotation;
        }
        else
        {
            // ★ 手首すらロストした場合
            // 何もしない（その場に留まり、手が戻ってくるのを待つ）
        }

        // 2. 放す判定（手のひら基準のパー）
        // ユーザーが意図的にカメラに手を向けたときだけ反応する
        if (IsHandOpenFromPalm(handInTrigger))
        {
            Debug.Log("【近接】「パー」ジェスチャーを認識。アイテムを解放します。");
            
            // 放す場所は、現在のアイテムの位置（anchorParent.position）でOK
            ReleaseItem(anchorParent.position, anchorParent.rotation);
        }
    }

    // アイテムを放す処理
    private void ReleaseItem(Vector3 position, Quaternion rotation)
    {
        SetColor(originalColor); 

        registrationManager.HandleItemReleased(anchorParent.gameObject, position, rotation);
        
        isGrabbed = false;
        handPartsInTriggerCount = 0; 
    }

    // ★★★ 手のひら基準のパー判定 ★★★
    private bool IsHandOpenFromPalm(XRNode handNode)
    {
        // 手のひら（Palm）が見えなければ「パーではない」とみなす（安全策）
        if (!handsSubsystem.TryGetJoint(TrackedHandJoint.Palm, handNode, out HandJointPose palmPose))
        {
            return false; 
        }

        int openFingers = 0;
        foreach (var tip in fingerTips)
        {
            if (handsSubsystem.TryGetJoint(tip, handNode, out HandJointPose tipPose))
            {
                // 手のひらからの距離がしきい値を超えているか？
                if (Vector3.Distance(palmPose.Position, tipPose.Position) > handOpenThreshold)
                {
                    openFingers++;
                }
            }
        }
        // 3本以上開いていればパーとみなす
        return openFingers >= 3;
    }

    // ---------------------------------------------------------
    // ヘルパーメソッド
    // ---------------------------------------------------------

    private void SetColor(Color color)
    {
        if(sphereRenderer != null) sphereRenderer.material.color = color;
    }

    private bool IsHand(Collider other, out XRNode node)
    {
        node = XRNode.RightHand; // とりあえず初期値

        if (handsSubsystem == null) return false;

        // 1. システムが認識している「左手」と「右手」の位置を取得
        bool leftValid = handsSubsystem.TryGetJoint(TrackedHandJoint.Palm, XRNode.LeftHand, out HandJointPose leftPalm);
        bool rightValid = handsSubsystem.TryGetJoint(TrackedHandJoint.Palm, XRNode.RightHand, out HandJointPose rightPalm);

        // 2. ぶつかってきた物体（other）と、左右の手の距離を測る
        // （認識されていない手との距離は 無限大 とする）
        float distLeft = leftValid ? Vector3.Distance(other.transform.position, leftPalm.Position) : float.MaxValue;
        float distRight = rightValid ? Vector3.Distance(other.transform.position, rightPalm.Position) : float.MaxValue;

        // 3. 判定：どちらかの手の近く（例: 20cm以内）にあるか？
        float threshold = 0.2f; 

        if (distLeft < threshold && distLeft < distRight)
        {
            // 左手の方が近い
            node = XRNode.LeftHand;
            return true;
        }
        else if (distRight < threshold && distRight < distLeft)
        {
            // 右手の方が近い
            node = XRNode.RightHand;
            return true;
        }

        // どちらの手とも遠い（手ではない何かが当たった）
        return false;
    }
}