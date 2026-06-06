using UnityEngine;

public class ExitZoneTrigger : MonoBehaviour
{
    private RegistrationManager registrationManager;
    
    // ★ 連続で警告が出ないようにするフラグ
    private bool hasTriggeredFinalCheck = false; 

    void Start()
    {
        // 司令塔（RegistrationManager）を探しておく
        registrationManager = FindObjectOfType<RegistrationManager>();

        if (registrationManager == null)
        {
            Debug.LogError("ExitZone が RegistrationManager を見つけられません！");
        }
    }

    /// <summary>
    /// 何か（Collider）がこのゾーンに入ってきた時
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // ★★★ このログを「if文の外」に追加 ★★★
        // Debug.Log($"【玄関ゾーン】: 何かが当たった！当たった相手: {other.name}, タグ: {other.tag}");
        // ★★★★★★★★★★★★★★★★★★★★★★★
        
        if (registrationManager == null) return;
        
        // （まだ最終チェックを実行しておらず、
        // 　かつ「準備モード」の時）
        if (!hasTriggeredFinalCheck && 
            registrationManager.currentMode == AppMode.Preparation)
        {
            // ★ 入ってきたのが「プレイヤー（カメラ）」か？
            // （MRTKのカメラには "MainCamera" タグが付いているはず）
            if (other.CompareTag("MainCamera"))
            {
                Debug.Log("【玄関ゾーン】: 準備モード中にプレイヤーがゾーンを通過しました。最終チェックを実行します。");
                
                // 司令塔の「最終チェック」メソッドを呼び出す
                StartCoroutine(registrationManager.FinalCheckForItems());
                
                // 一度警告したら、再度入ってくるまで警告しない
                hasTriggeredFinalCheck = true; 
            }
            // else
            // {
            //     Debug.Log("Other");
            // }
        }
    }

    /// <summary>
    /// 何か（Collider）がこのゾーンから出た時
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // プレイヤーがゾーンから出たら、
        // 再度ゾーンに入ったときに警告できるようにフラグをリセット
        if (other.CompareTag("MainCamera"))
        {
            hasTriggeredFinalCheck = false;
        }
    }
}