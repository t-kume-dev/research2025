using UnityEngine;
using UnityEngine.XR.ARFoundation;
using MixedReality.Toolkit;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("パネル参照")]
    [SerializeField] private GameObject setupCanvas; // 玄関設定用
    [SerializeField] private GameObject applianceListPanel; // ★ 家電リスト（セットアップ時のみ使用）
    [SerializeField] private GameObject applianceListPanelParent;
    [SerializeField] private GameObject itemListPanel; // 持ち物リスト
    [SerializeField] private GameObject itemListPanelParent;
    [SerializeField] private GameObject mainContentPanel; // コンテンツ親
    [SerializeField] private GameObject mainCanvas; // メインCanvas全体

    [Header("セットアップ用")]
    [SerializeField] private GameObject exitZonePrefab; 
    private GameObject pendingExitZone;
    private Vector3 originalScale;
    // private float previewScaleRatio = 0.1f; // 仮置き時の縮小率（10%）

    [Header("連携")]
    [SerializeField] private RegistrationManager registrationManager;

    [Header("タブボタン")]
    [SerializeField] private StatefulInteractable dailyButtonInteractable; // アイテムリストボタン
    [SerializeField] private StatefulInteractable preparationButtonInteractable; // 準備モードボタン
    
    [Header("追加ボタン")]
    [SerializeField] private GameObject finishSetupButton; // ★ 「設定完了」ボタン

    void Start()
    {
        // 起動直後は「玄関セットアップ」からスタート
        StartExitZoneSetup();
    }

    // --- フェーズ1: 玄関ゾーン設定 ---
    private void StartExitZoneSetup()
    {
        mainCanvas.SetActive(false); // メインUIは隠す
        setupCanvas.SetActive(true); // セットアップUI表示

        Camera mainCamera = Camera.main;
        pendingExitZone = Instantiate(exitZonePrefab, Vector3.zero, Quaternion.identity);

        // ★★★ 修正点 1: サイズを変える「前」に、元のサイズを記憶する ★★★
        // （MeshRendererではなく、pendingExitZone自体のスケールを記憶・操作するのが確実です）
        originalScale = pendingExitZone.transform.localScale;
        
        // カメラ追従設定
        // (SetParent は使わず Follow ソルバー等を使う場合はここに記述)
        // 今回は簡易的にカメラの子にする例で記述します（前回の修正があればそれに従ってください）
        pendingExitZone.transform.SetParent(mainCamera.transform, false);
        pendingExitZone.transform.localPosition = new Vector3(0, 0, 3.0f);
        pendingExitZone.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);

        // MeshRenderer zoneMesh = pendingExitZone.GetComponentInChildren<MeshRenderer>();
        // if (zoneMesh != null) originalScale = zoneMesh.transform.localScale;

        pendingExitZone.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        // ResetExitZonePosition();

        Debug.Log("セットアップモード開始。玄関まで移動して「目の前に移動」を押してください。");
    }

    /// <summary>
    /// 「目の前に移動」ボタンから呼ばれる
    /// </summary>
    // public void ResetExitZonePosition()
    // {
    //     if (pendingExitZone == null) return;

    //     Camera mainCamera = Camera.main;

    //     // ★ カメラの正面 1.5m にワープさせる
    //     // (回転もカメラに合わせるが、水平は保つためにY軸回転だけ適用するのがベスト)
    //     Vector3 targetPos = mainCamera.transform.position + mainCamera.transform.forward * 1.5f;
        
    //     // 傾き（X, Z回転）はリセットし、Y回転（向き）だけカメラに合わせる
    //     Vector3 cameraEuler = mainCamera.transform.rotation.eulerAngles;
    //     Quaternion targetRot = Quaternion.Euler(0, cameraEuler.y, 0);

    //     pendingExitZone.transform.position = targetPos;
    //     pendingExitZone.transform.rotation = targetRot;
        
    //     // サイズをリセット（縮小していた場合）
    //     pendingExitZone.transform.localScale = Vector3.one; 
    // }

    public void OnExitZoneConfirmPressed()
    {
        if (pendingExitZone == null) return;

        pendingExitZone.transform.localScale = originalScale;

        // // 確定処理（サイズ戻し、アンカー固定など）
        // MeshRenderer zoneMesh = pendingExitZone.GetComponentInChildren<MeshRenderer>();
        // if (zoneMesh != null) zoneMesh.transform.localScale = originalScale;
        
        pendingExitZone.transform.SetParent(null, true);
        pendingExitZone.AddComponent<ARAnchor>();
        
        // ★★★ 修正点 4: 「ObjectManipulator」を無効にする（ここに追加） ★★★
        // （これで確定後は動かせなくなります）
        var manipulator = pendingExitZone.GetComponent<MixedReality.Toolkit.SpatialManipulation.ObjectManipulator>();
        if (manipulator != null)
        {
            manipulator.enabled = false;
        }

        // ★ 次のフェーズへ
        StartApplianceSetup();
    }

    // --- フェーズ2: 家電配置（ここだけ家電リストを表示） ---
    private void StartApplianceSetup()
    {
        setupCanvas.SetActive(false); // 玄関UIを消す
        mainCanvas.SetActive(true);   // メイン枠を表示
        
        // ★ ここで「家電リスト」だけを表示する
        mainContentPanel.SetActive(true);
        itemListPanel.SetActive(false);      // 持ち物リストは隠す
        itemListPanelParent.SetActive(false);
        applianceListPanel.SetActive(true);  // 家電リストを表示！
        
        if(finishSetupButton != null) finishSetupButton.SetActive(true); // 「設定完了」ボタン表示

        Debug.Log("【セットアップ】家電を選んで配置してください。");
    }

    // --- フェーズ3: 日常モード開始（セットアップ完了） ---
    public void OnFinishSetupPressed()
    {
        if (registrationManager != null)
        {
            registrationManager.ConfirmFinishSetup();
        }

        // // ★ 家電リストを隠して、日常モード（持ち物リスト）へ
        // SetDailyMode();
        
        // Debug.Log("【完了】セットアップ完了。日常モードを開始します。");
    }

    // ★ 追加: 確認画面で「Yes」が押されたら呼ばれる
    public void OnFinishSetupConfirmed()
    {
        // ★★★ 修正点：Finishボタンを非表示にする ★★★
        if(finishSetupButton != null) 
        {
            finishSetupButton.SetActive(false);
        }

        // 日常モードへ
        // SetDailyMode();
        
        Debug.Log("【完了】セットアップ完了。未設定項目を整理しました。");
    }

    // --- 既存のモード切替（日常モード中は家電リストを出さない） ---
    
    public void OnItemListTogglePressed()
    {
        if (mainContentPanel.activeSelf && itemListPanel.activeSelf) HideContentPanel();
        else
        {
            mainContentPanel.SetActive(true);
            itemListPanel.SetActive(true);    // 持ち物リストを表示
            itemListPanelParent.SetActive(true);
            applianceListPanelParent.SetActive(false);
            applianceListPanel.SetActive(false); // ★ 家電リストはもう表示しない

            // SetDailyMode();
        } 
    }

    public void OnApplianceListTogglePressed()
    {
        if (mainContentPanel.activeSelf && applianceListPanel.activeSelf) HideContentPanel();
        else
        {
            mainContentPanel.SetActive(true);
            itemListPanel.SetActive(false);
            itemListPanelParent.SetActive(false);
            applianceListPanelParent.SetActive(true);
            applianceListPanel.SetActive(true);
            // if(registrationManager) registrationManager.SetModeDaily();
        }
    }

    public void OnPreparationTogglePressed()
    {
        bool isPrep = (preparationButtonInteractable != null && preparationButtonInteractable.IsToggled.Active);
        if (isPrep) SetDailyMode(); 
        else SetPreparationMode(); 
    }

    private void SetDailyMode()
    {
        // mainContentPanel.SetActive(true);
        // itemListPanel.SetActive(true);    // 持ち物リストを表示
        // applianceListPanelParent.SetActive(false);
        // applianceListPanel.SetActive(false); // ★ 家電リストはもう表示しない

        if(registrationManager) registrationManager.SetModeDaily();

        if(dailyButtonInteractable) dailyButtonInteractable.ForceSetToggled(true);
        if(preparationButtonInteractable) preparationButtonInteractable.ForceSetToggled(false);
    }

    private void SetPreparationMode()
    {
        // mainContentPanel.SetActive(true);
        // itemListPanel.SetActive(true); // 準備中も持ち物リストは見たい
        // applianceListPanelParent.SetActive(false);
        // applianceListPanel.SetActive(false);

        if(registrationManager) registrationManager.SetModePreparation();

        if(dailyButtonInteractable) dailyButtonInteractable.ForceSetToggled(false);
        if(preparationButtonInteractable) preparationButtonInteractable.ForceSetToggled(true);
    }

    private void HideContentPanel()
    {
        mainContentPanel.SetActive(false);
        if(dailyButtonInteractable) dailyButtonInteractable.ForceSetToggled(false);
        if(preparationButtonInteractable) preparationButtonInteractable.ForceSetToggled(false);
    }
}