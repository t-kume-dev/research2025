using UnityEngine;
using UnityEngine.XR.ARFoundation;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation; 
using System.Threading.Tasks;
using UnityEngine.XR.Interaction.Toolkit;
using MixedReality.Toolkit.Subsystems;
using System.Collections; 
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using MixedReality.Toolkit.UX; 

public enum AppMode
{
    Daily,        
    Preparation   
}

public struct SwitchBotDeviceData
{
    public string deviceId;
    public string deviceType;
}

public class RegistrationManager : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameObject itemProxyPrefab; 
    [SerializeField] private PhotoTaker photoTaker;
    
    [Header("Services")]
    [SerializeField] private CustomVisionService visionService; 
    [SerializeField] private SwitchBotService switchBotService;

    // ★★★ 1. UIManager への参照を追加 ★★★
    [Header("Managers")]
    [SerializeField] private UIManager uiManager;

    private bool isRegistering = false; 
    private GameObject pendingProxy; 
    private string pendingItemName;  
    private string pendingDeviceId;
    private string pendingDeviceType;

    private string currentlyPointedItemName = null;

    [Header("UI Components")]
    [SerializeField] private GameObject itemButtonPrefab; 
    [SerializeField] private Transform buttonContainer; 
    [SerializeField] private Transform applianceButtonContainer; 
    [SerializeField] private GameObject preparationModeOn; 
    // ★★★ 1. 追加：確認ボタンへの直接参照 ★★★
    [SerializeField] private GameObject confirmYesButton; // 「はい」ボタン
    [SerializeField] private GameObject confirmNoButton;  // 「いいえ」ボタン

    private Dictionary<string, GameObject> registeredItems = new Dictionary<string, GameObject>();
    private Dictionary<string, SwitchBotDeviceData> switchBotDevices = new Dictionary<string, SwitchBotDeviceData>();

    [Header("Navigation")]
    [SerializeField] private ArrowController arrowController;

    [Header("Confirmation UI")]
    [SerializeField] private GameObject confirmationPanel; 
    [SerializeField] private TMP_Text confirmationText; 

    // --- 【追加】新しい2つの通知UI ---
    [Header("Notification: Items (持ち物)")]
    [SerializeField] private GameObject itemNotificationCanvas; 
    [SerializeField] private TMP_Text itemNotificationText;     
    [SerializeField] private GameObject itemCloseButton;

    [Header("Notification: Smart Home (家電)")]
    [SerializeField] private GameObject smartHomeNotificationCanvas; 
    [SerializeField] private TMP_Text smartHomeNotificationText;     
    [SerializeField] private GameObject smartHomeCloseButton;

    [Header("Smart Home UI")]
    [SerializeField] private GameObject smartHomePanel; 
    // [SerializeField] private TMP_Text smartHomeText;    
    // [SerializeField] private GameObject smartHomeActionButton; 
    // [SerializeField] private GameObject smartHomeSkipButton;   
    
    private Coroutine notificationCoroutine; 

    [Header("Appliance Settings")]
    [SerializeField] private Color applianceProxyColor = Color.cyan;

    [Header("Mode")]
    public AppMode currentMode = AppMode.Daily;

    private Coroutine statusPollingCoroutine; // 自動更新用のコルーチン

    void Start()
    {
        if (photoTaker != null) photoTaker.OnPhotoCapturedAsJPG += HandlePhotoCaptured;
        
        var keywordRecognitionSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<KeywordRecognitionSubsystem>();
        if (keywordRecognitionSubsystem != null)
        {
            keywordRecognitionSubsystem.CreateOrGetEventForKeyword("Register this").AddListener(() => StartRegistrationProcess());
            keywordRecognitionSubsystem.CreateOrGetEventForKeyword("Finish Registration").AddListener(() => FinalizeRegistration());
        }

        if(arrowController != null) arrowController.Hide();
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (smartHomePanel != null) smartHomePanel.SetActive(false);

        LoadSwitchBotDevices();
    }

    void OnDestroy()
    {
        if (photoTaker != null) photoTaker.OnPhotoCapturedAsJPG -= HandlePhotoCaptured;
    }

    // ---------------------------------------------------------
    // SwitchBot Logic
    // ---------------------------------------------------------

    public void LoadSwitchBotDevices()
    {
        if (switchBotService == null) return;
        Debug.Log("[SwitchBot] Loading device list...");
        StartCoroutine(switchBotService.GetDeviceList((response) => {
            if (response != null && response.body != null)
            {
                foreach (var device in response.body.deviceList)
                {
                    string type = device.deviceType;
                    bool isTarget = false;
                    if (type.Contains("Plug")) isTarget = true;
                    if (type.Contains("Light")) isTarget = true;
                    if (type.Contains("Bulb")) isTarget = true;
                    if (type == "Contact Sensor") isTarget = true;

                    if (isTarget)
                    {
                        CreateApplianceButton(device.deviceName, device.deviceId, device.deviceType);
                    }
                }
                foreach (var device in response.body.infraredRemoteList)
                {
                     if (device.deviceType.Contains("Light"))
                     {
                         CreateApplianceButton(device.deviceName, device.deviceId, device.deviceType);
                     }
                }
            }
        }));
    }

    private void CreateApplianceButton(string name, string id, string type)
    {
        if (registeredItems.ContainsKey(name)) return;

        GameObject newButton = Instantiate(itemButtonPrefab, applianceButtonContainer);
        newButton.name = $"Button_{name}";
        
        var text = newButton.GetComponentInChildren<TMP_Text>();
        if(text) text.text = $"{name}\n(Not Set)";

        var pressable = newButton.GetComponent<PressableButton>();
        if (pressable)
        {
            pressable.OnClicked.AddListener(() => {
                if (!registeredItems.ContainsKey(name)) 
                {
                    StartAppliancePlacement(name, id, type);
                }
                else
                {
                    OnItemSelected(name);
                }
            });
        }
    }

    public void StartAppliancePlacement(string name, string id, string type)
    {
        if (isRegistering || pendingProxy != null) return;

        isRegistering = true;
        pendingItemName = name;
        pendingDeviceId = id;
        pendingDeviceType = type;

        if (confirmationText != null) confirmationText.text = $"Place appliance\n'{name}'?\n\nPress 'Yes' to spawn.";
        if (confirmationPanel != null) confirmationPanel.SetActive(true);
    }

    // ★★★ 単一デバイスの状態チェック（登録直後などに使用） ★★★
    private void CheckSingleDeviceStatus(string itemName)
    {
        if (!switchBotDevices.ContainsKey(itemName) || switchBotService == null) return;

        var deviceData = switchBotDevices[itemName];
        Debug.Log($"【SwitchBot】{itemName} の状態を確認します...");

        StartCoroutine(switchBotService.GetDeviceStatus(deviceData.deviceId, (response) => {
            if (response == null) return;

            bool isSafe = false; // Safe = OFF or Closed

            if (deviceData.deviceType == "Contact Sensor")
            {
                Debug.Log($"Debug:{response.body.openState}");
                if (response.body.openState == "close") isSafe = true;
            }
            else 
            {
                if (response.body.power == "off") isSafe = true;
            }

            UpdateChecklistItem(itemName, isSafe);
            Debug.Log($"【SwitchBot】{itemName} Status: {(isSafe ? "OK(OFF/Close)" : "Active(ON/Open)")}");
        }));
    }

    // ★★★ 家電のON/OFFトグル（ボタン操作） ★★★
    private void ToggleApplianceState(string itemName)
    {
        if (!switchBotDevices.ContainsKey(itemName) || switchBotService == null) return;

        var deviceData = switchBotDevices[itemName];
        
        // 開閉センサーは操作できないので、状態更新だけして終了
        if (deviceData.deviceType == "Contact Sensor")
        {
            CheckSingleDeviceStatus(itemName);
            return;
        }

        // 現在のチェック状態（UI）を確認
        bool isCurrentlyChecked = false;
        Transform buttonTransform = applianceButtonContainer.Find($"Button_{itemName}");
        if (buttonTransform != null)
        {
            var toggle = buttonTransform.GetComponent<StatefulInteractable>();
            if (toggle != null) isCurrentlyChecked = toggle.IsToggled.Active;
        }

        // ロジック:
        // チェック済み(OFF)なら -> ONにするコマンド
        // 未チェック(ON)なら -> OFFにするコマンド
        string command = isCurrentlyChecked ? "turnOn" : "turnOff";

        Debug.Log($"【SwitchBot】{itemName} にコマンド送信: {command}");
        
        StartCoroutine(switchBotService.SendCommand(deviceData.deviceId, command));

        // コマンド送信後、少し待ってから状態を再確認してUI更新
        // (SwitchBotの反映ラグを考慮して遅延させる)
        StartCoroutine(DelayedStatusCheck(itemName, 2.0f));
    }

    private IEnumerator DelayedStatusCheck(string itemName, float delay)
    {
        yield return new WaitForSeconds(delay);
        CheckSingleDeviceStatus(itemName);
    }


    public void CheckSmartHomeStatus()
    {
        if (switchBotService == null) return;
        foreach (var itemName in switchBotDevices.Keys)
        {
            CheckSingleDeviceStatus(itemName);
        }
    }

    // ---------------------------------------------------------
    // Main Logic
    // ---------------------------------------------------------

    public void StartRegistrationProcess()
    {
        if (currentMode != AppMode.Daily) return;
        if (isRegistering || pendingProxy != null) return;

        pendingDeviceId = null;
        pendingDeviceType = null;
        isRegistering = true;
        photoTaker.TakePhoto();
    }

    private async void HandlePhotoCaptured(byte[] jpgData)
    {
        if (jpgData == null || jpgData.Length == 0)
        {
            isRegistering = false;
            return;
        }
        if (!isRegistering) return;

        string recognizedItemName = await visionService.RecognizeItemAsync(jpgData);

        if (!string.IsNullOrEmpty(recognizedItemName))
        {
            pendingItemName = recognizedItemName; 
            pendingDeviceId = null;
            pendingDeviceType = null;

            if (confirmationText != null) confirmationText.text = $"Register\n'{pendingItemName}'?";
            if (confirmationPanel != null) confirmationPanel.SetActive(true);
        }
        else
        {
            isRegistering = false; 
        }
    }
    
    public void OnConfirmRegistration()
    {
        if (string.IsNullOrEmpty(pendingItemName)) return;

        Camera mainCamera = Camera.main;
        Vector3 spawnPosition = mainCamera.transform.position + mainCamera.transform.forward * 1.0f;

        pendingProxy = Instantiate(itemProxyPrefab, spawnPosition, Quaternion.identity);
        pendingProxy.name = $"Pending_Proxy_{pendingItemName}";
        
        ObjectManipulator manipulator = pendingProxy.GetComponent<ObjectManipulator>();
        if (manipulator != null) manipulator.enabled = true;
        
        Debug.Log($"Placed '{pendingItemName}'. Adjust position.");
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
    }

    public void OnCancelRegistration()
    {
        pendingItemName = null;
        pendingDeviceId = null;
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        isRegistering = false; 
    }

    // public void FinalizeRegistration()
    // {
    //     if (pendingProxy == null) return;

    //     string uniqueName = pendingItemName;
    //     int count = 1;
    //     while (registeredItems.ContainsKey(uniqueName))
    //     {
    //         count++;
    //         uniqueName = $"{pendingItemName}_{count}";
    //     }

    //     // --- SwitchBotデバイスとして登録 ---
    //     if (!string.IsNullOrEmpty(pendingDeviceId))
    //     {
    //         SwitchBotDeviceData data = new SwitchBotDeviceData
    //         {
    //             deviceId = pendingDeviceId,
    //             deviceType = pendingDeviceType
    //         };
    //         switchBotDevices.Add(uniqueName, data);
    //     }

    //     GameObject anchorParent = new GameObject($"Anchor_{uniqueName}");
    //     anchorParent.transform.SetPositionAndRotation(pendingProxy.transform.position, pendingProxy.transform.rotation);
    //     anchorParent.AddComponent<ARAnchor>();
    //     pendingProxy.transform.SetParent(anchorParent.transform);
        
    //     ObjectManipulator manipulator = pendingProxy.GetComponent<ObjectManipulator>();
    //     if (manipulator != null) manipulator.enabled = false; 

    //     Collider proxyCollider = pendingProxy.GetComponent<Collider>();
    //     if (proxyCollider != null) proxyCollider.isTrigger = true;
        
    //     Rigidbody rb = pendingProxy.GetComponent<Rigidbody>();
    //     if (rb == null) rb = pendingProxy.AddComponent<Rigidbody>();
    //     rb.isKinematic = true;
    //     rb.useGravity = false;

    //     if (!string.IsNullOrEmpty(pendingDeviceId))
    //     {
    //         MeshRenderer mesh = pendingProxy.GetComponentInChildren<MeshRenderer>();
    //         if (mesh != null) mesh.material.color = applianceProxyColor; 
    //     }

    //     pendingProxy.AddComponent<GestureMoveController>();
        
    //     Debug.Log($"Registration Complete.");

    //     UpdateUIForRegisteredItem(uniqueName, anchorParent);

    //     // ★★★ 追加：登録直後にステータスを確認する ★★★
    //     if (!string.IsNullOrEmpty(pendingDeviceId))
    //     {
    //         CheckSingleDeviceStatus(uniqueName);
    //     }

    //     pendingProxy.name = $"Final_Proxy_{uniqueName}";
    //     pendingProxy = null;
    //     pendingItemName = null;
    //     pendingDeviceId = null; 
    //     isRegistering = false;
    // }

    public void FinalizeRegistration()
    {
        if (pendingProxy == null) return;

        string uniqueName = pendingItemName;
        int count = 1;
        while (registeredItems.ContainsKey(uniqueName))
        {
            count++;
            uniqueName = $"{pendingItemName}_{count}";
        }

        // --- SwitchBotデバイスとして登録 ---
        if (!string.IsNullOrEmpty(pendingDeviceId))
        {
            SwitchBotDeviceData data = new SwitchBotDeviceData
            {
                deviceId = pendingDeviceId,
                deviceType = pendingDeviceType
            };
            switchBotDevices.Add(uniqueName, data);
            Debug.Log($"【登録】{uniqueName} をSwitchBotデバイスとして登録しました。");
        }

        GameObject anchorParent = new GameObject($"Anchor_{uniqueName}");
        anchorParent.transform.SetPositionAndRotation(pendingProxy.transform.position, pendingProxy.transform.rotation);
        anchorParent.AddComponent<ARAnchor>();
        pendingProxy.transform.SetParent(anchorParent.transform);
        
        // 1. ObjectManipulator を無効化（ピンチ移動禁止）
        ObjectManipulator manipulator = pendingProxy.GetComponent<ObjectManipulator>();
        if (manipulator != null) manipulator.enabled = false; 
        
        // ★★★ 修正点：エラーになる StatefulInteractable の設定コードを削除しました ★★★
        // (ColliderをTriggerにするだけで操作できなくなるので不要です)

        // 2. 物理設定（Collider/Rigidbody）
        Collider proxyCollider = pendingProxy.GetComponent<Collider>();
        if (proxyCollider != null) proxyCollider.isTrigger = true;
        
        Rigidbody rb = pendingProxy.GetComponent<Rigidbody>();
        if (rb == null) rb = pendingProxy.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // 3. 家電かどうかで分岐
        if (!string.IsNullOrEmpty(pendingDeviceId))
        {
            // --- 家電の場合 ---
            // 色を変える（水色など）
            MeshRenderer mesh = pendingProxy.GetComponentInChildren<MeshRenderer>();
            if (mesh != null) mesh.material.color = applianceProxyColor; 
            
            // 家電には「移動スクリプト」を付けない（固定）
        }
        else
        {
            // --- 通常アイテムの場合 ---
            // 移動できるようにスクリプトを追加
            pendingProxy.AddComponent<GestureMoveController>();
        }

        
        Debug.Log($"Registration Complete.");

        UpdateUIForRegisteredItem(uniqueName, anchorParent);

        if (!string.IsNullOrEmpty(pendingDeviceId))
        {
            CheckSingleDeviceStatus(uniqueName);
        }

        pendingProxy.name = $"Final_Proxy_{uniqueName}";
        pendingProxy = null;
        pendingItemName = null;
        pendingDeviceId = null; 
        isRegistering = false;
    }

    private void UpdateUIForRegisteredItem(string uniqueName, GameObject anchorParent)
    {
        Transform existingButton = applianceButtonContainer.Find($"Button_{pendingItemName}");
        if (existingButton == null) existingButton = buttonContainer.Find($"Button_{pendingItemName}");

        if (existingButton != null && !registeredItems.ContainsKey(uniqueName))
        {
            existingButton.name = $"Button_{uniqueName}";
            var text = existingButton.GetComponentInChildren<TMP_Text>();
            if(text) text.text = uniqueName; 
            
            registeredItems.Add(uniqueName, anchorParent);
            
            var pressable = existingButton.GetComponent<PressableButton>();
            if (pressable)
            {
                string nameForButton = uniqueName;
                pressable.OnClicked.RemoveAllListeners(); 
                pressable.OnClicked.AddListener(() => {
                    OnItemSelected(nameForButton);
                });
            }
        }
        else if (!registeredItems.ContainsKey(uniqueName))
        {
            registeredItems.Add(uniqueName, anchorParent);
            
            GameObject newButton = Instantiate(itemButtonPrefab, buttonContainer);
            newButton.name = $"Button_{uniqueName}";

            TMP_Text buttonText = newButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null) buttonText.text = uniqueName;

            PressableButton buttonComponent = newButton.GetComponent<PressableButton>();
            if (buttonComponent != null)
            {
                string itemNameForButton = uniqueName; 
                buttonComponent.OnClicked.AddListener(() => {
                    OnItemSelected(itemNameForButton);
                });
            }
        }
    }

    public bool HandleItemGrabbed(GameObject anchorParent)
    {
        switch (currentMode)
        {
            case AppMode.Daily:
                if (anchorParent != null)
                {
                    ARAnchor anchor = anchorParent.GetComponent<ARAnchor>();
                    if (anchor != null) Destroy(anchor); 
                }
                return true; 

            case AppMode.Preparation:
                string itemName = anchorParent.name.Replace("Anchor_", "");
                
                if (switchBotDevices.ContainsKey(itemName))
                {
                    // 家電は何もしない（ボタンで操作する）
                    return false; 
                }
                else
                {
                    HandleItemPacked(anchorParent); 
                    return false; 
                }
                
            default: return false;
        }
    }

    public void HandleItemReleased(GameObject anchorParent, Vector3 releasePosition, Quaternion releaseRotation)
    {
        if (currentMode != AppMode.Daily) return;
        if (anchorParent == null) return;

        anchorParent.transform.SetPositionAndRotation(releasePosition, releaseRotation);
        anchorParent.AddComponent<ARAnchor>();

        // ★★★ 修正点：以下の古いコード（ItemProximityTrigger関連）を削除しました ★★★
        /*
        ItemProximityTrigger trigger = anchorParent.GetComponentInChildren<ItemProximityTrigger>();
        if (trigger != null) trigger.ResetAnchorFlag();
        */
    }
    public void HandleItemPacked(GameObject anchorParent)
    {
        anchorParent.SetActive(false); 
        string itemName = anchorParent.name.Replace("Anchor_", ""); 
        UpdateChecklistItem(itemName, true);
        if (currentlyPointedItemName == itemName)
        {
            arrowController.Hide();
            currentlyPointedItemName = null;
        }
    }

    private void UpdateChecklistItem(string itemName, bool isChecked)
    {
        Transform buttonTransform = buttonContainer.Find($"Button_{itemName}");
        if (buttonTransform == null) buttonTransform = applianceButtonContainer.Find($"Button_{itemName}");

        if (buttonTransform != null)
        {
            StatefulInteractable toggle = buttonTransform.GetComponent<StatefulInteractable>();
            if (toggle != null) toggle.ForceSetToggled(isChecked);
        }
    }

    private void OnItemSelected(string itemName)
    {
        if (arrowController == null) return;
        Debug.Log($"【ボタン確認】: 「{itemName}」のOnClickイベントが発火しました！");
        // ★★★ 1. SwitchBotデバイスなら、操作（トグル）のみ行い、矢印は出さない ★★★
        if (switchBotDevices.ContainsKey(itemName))
        {
            ToggleApplianceState(itemName);
            Debug.Log($"【ボタン確認】: 「{itemName}」のOnClickイベントが発火しました！");
            // ★ 修正点：矢印を表示するのではなく、隠す
            arrowController.Hide();
            currentlyPointedItemName = null;
            
            return; // ここで終了
        }

        // --- 以下、通常アイテム（持ち物）の処理 ---

        // 2. 矢印のトグル（表示/非表示）
        if (arrowController.gameObject.activeInHierarchy && currentlyPointedItemName == itemName)
        {
            // 同じボタンを押した → 矢印を隠す
            arrowController.Hide();
            currentlyPointedItemName = null; 
        }
        else
        {
            // 3. チェック済みか確認
            Transform buttonTransform = buttonContainer.Find($"Button_{itemName}");
            if (buttonTransform == null) buttonTransform = applianceButtonContainer.Find($"Button_{itemName}"); // 念のため

            if (buttonTransform != null)
            {
                StatefulInteractable toggle = buttonTransform.GetComponent<StatefulInteractable>();
                if (toggle != null && toggle.IsToggled.Active)
                {
                    return; // チェック済みなら矢印出さない
                }
            }

            // 4. 矢印を表示
            if (registeredItems.TryGetValue(itemName, out GameObject anchorParent))
            {
                Transform itemProxyTransform = anchorParent.transform.Find($"Final_Proxy_{itemName}");
                if(itemProxyTransform == null) itemProxyTransform = anchorParent.transform; 
                
                arrowController.PointAtTarget(itemProxyTransform);
                currentlyPointedItemName = itemName; 
            }
        }
    }
    
    public IEnumerator FinalCheckForItems()
    {
        if (currentMode != AppMode.Preparation) yield break;

        Debug.Log("【最終チェック】開始...");

        // 1. 辞書自体のnullチェック
        if (registeredItems == null)
        {
            Debug.LogError("【エラー】registeredItems が null です！");
            yield break;
        }
        Debug.Log("1");
        if (switchBotDevices == null)
        {
            Debug.LogError("【エラー】switchBotDevices が null です！（初期化されていません）");
            // ここで止まらないように空の辞書を入れておく手もあるが、一旦エラーとして出す
            yield break;
        }
        Debug.Log("2");
        CloseNotification();
        Debug.Log("3");
        string itemWarningMessage = "";
        int itemWarningCount = 0;

        // コピーを作って回す（ループ中の変更エラー回避のため念のため）
        List<string> keys = new List<string>(registeredItems.Keys);

        // 1. 持ち物チェック
        foreach (var itemName in registeredItems.Keys)
        {
            // SwitchBot以外で、表示されているもの
            // if (registeredItems[itemName].activeSelf && !switchBotDevices.ContainsKey(itemName))
            // {
            //     itemWarningMessage += $"・{itemName} (持ち物)\n";
            //     itemWarningCount++;
            // }
            Debug.Log("loop");
            try
            {
                // 2. オブジェクトの中身チェック
                GameObject targetObj = registeredItems[itemName];

                // オブジェクトが「Missing（削除済み）」または null か？
                if (targetObj == null)
                {
                    Debug.LogWarning($"【警告】リストにある「{itemName}」のオブジェクトが見つかりません（Destroy済み？）。スキップします。");
                    continue;
                }

                // 3. 判定ロジック
                // SwitchBot辞書に含まれていない、かつ、表示されているなら
                if (targetObj.activeSelf && !switchBotDevices.ContainsKey(itemName))
                {
                    itemWarningMessage += $"・{itemName} (持ち物)\n";
                    itemWarningCount++;
                    Debug.Log($"【チェック】忘れ物検知: {itemName}");
                }
            }
            catch (System.Exception e)
            {
                // ここでエラー内容を特定する
                Debug.LogError($"【ループ内エラー】アイテム名: {itemName} の処理中に例外発生: {e.Message}");
            }
        }
        Debug.Log("foreach nuketa");

        if (itemWarningCount > 0)
        {
            // ★ 持ち物忘れがある場合 -> ここでストップして ItemCanvas を表示
            string msg = $"Warning!\n{itemWarningCount} コの忘れ物があります!\n忘れ物リストを確認してください";
            ShowItemNotification(msg, Color.red, false);
            
            Debug.Log("【チェック中断】持ち物忘れがあります。");
            yield break; // ここで終了（家電チェックには進まない）
        }

        Debug.Log($"{itemWarningCount} nuketa");

        // 2. SwitchBotチェック
        string homeWarningMsg = "";
        int homeWarningCount = 0;
        string targetDeviceId = ""; // 操作ボタン用（最初に見つかった操作可能なデバイスIDを入れる）

        if (switchBotService != null)
        {
            foreach (var itemName in switchBotDevices.Keys)
            {
                var device = switchBotDevices[itemName];
                bool isCheckDone = false;

                StartCoroutine(switchBotService.GetDeviceStatus(device.deviceId, (response) => {
                    if (response != null && response.body != null)
                    {
                        bool isNG = false;
                        string statusText = "";

                        // 開閉センサー (Contact Sensor)
                        if (device.deviceType.Contains("Contact"))
                        {
                            if (response.body.openState != "close")
                            {
                                isNG = true;
                                statusText = $"・{itemName}（開いています）\n";
                                
                                // 矢印で場所を教える
                                if (registeredItems.TryGetValue(itemName, out GameObject anchor))
                                {
                                    Transform target = anchor.transform.Find($"Final_Proxy_{itemName}");
                                    if(target == null) target = anchor.transform;
                                    if (arrowController != null) arrowController.PointAtTarget(target);
                                }
                            }
                        }
                        // ライト・プラグ
                        else
                        {
                            if (response.body.power == "on")
                            {
                                isNG = true;
                                statusText += $"・{itemName} (ついています)\n";

                                // // ★ 対策B：リモート操作ボタンを表示
                                // if(remoteActionButton)
                                // {
                                //     remoteActionButton.SetActive(true);
                                //     // ボタンのテキストを変えたり、OnClickにコマンドを登録
                                //     // (簡易的に、一番最初に見つかったON家電を消す設定にする例)
                                //     var btn = remoteActionButton.GetComponent<PressableButton>();
                                //     btn.OnClicked.RemoveAllListeners();
                                //     btn.OnClicked.AddListener(()=> {
                                //         StartCoroutine(switchBotService.SendCommand(device.deviceId, "turnOff"));
                                //         remoteActionButton.SetActive(false); // 押したら消す
                                //     });
                                // }
                            }
                        }

                        if (isNG)
                        {
                            homeWarningMsg += $"・{itemName} ({statusText})\n";
                            homeWarningCount++;
                        }

                        // ついでにリストのチェックボックスも更新
                        UpdateChecklistItem(itemName, !isNG);
                    }
                    isCheckDone = true;
                }));

                // API待ち
                while (!isCheckDone) yield return null;
            }
        }

        if (homeWarningCount > 0)
        {
            // ★ 家電の消し忘れがある場合 -> SmartHomeCanvas を表示
            string msg = $"Warning!\n{homeWarningCount}コの家電がアクティブです!\n家電リストを確認してください";
            ShowSmartHomeNotification(msg, Color.yellow, false); 
        }
        else
        {
            // ★ 全てOKの場合 -> ItemCanvas で「完了」を表示
            ShowItemNotification("完璧です！\nいってらっしゃい！", Color.green, true);
        }
    }
        
    
    // (ShowNotification, HideNotificationTimer, CloseNotification, SetModeDaily, SetModePreparation は変更なし)
    // private void ShowNotification(string message, Color color, bool autoHide)
    // {
    //     if (notificationCanvas == null || notificationText == null) return;
    //     notificationText.text = message;
    //     notificationText.color = color;
    //     if (notificationCloseButton != null) notificationCloseButton.SetActive(!autoHide);
    //     notificationCanvas.SetActive(true);
    //     if (notificationCoroutine != null)
    //     {
    //         StopCoroutine(notificationCoroutine);
    //         notificationCoroutine = null;
    //     }
    //     if (autoHide)
    //     {
    //         notificationCoroutine = StartCoroutine(HideNotificationTimer(3.0f));
    //     }
    // }
    private IEnumerator HideNotificationTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
        CloseNotification();
    }
    // public void CloseNotification()
    // {
    //     if (notificationCanvas != null) notificationCanvas.SetActive(false);
    //     notificationCoroutine = null;
    // }

    // --- 表示用ヘルパーメソッド ---

    // 持ち物用通知を表示
    private void ShowItemNotification(string message, Color color, bool autoHide)
    {
        if (itemNotificationCanvas == null || itemNotificationText == null) return;

        itemNotificationText.text = message;
        itemNotificationText.color = color;
        
        if (itemCloseButton != null) itemCloseButton.SetActive(!autoHide);

        itemNotificationCanvas.SetActive(true);
        if(smartHomeNotificationCanvas != null) smartHomeNotificationCanvas.SetActive(false); // 他方は隠す

        if (autoHide) StartCoroutine(HideNotificationTimer(3.0f));
    }

    // 家電用通知を表示
    private void ShowSmartHomeNotification(string message, Color color, bool autoHide)
    {
        if (smartHomeNotificationCanvas == null || smartHomeNotificationText == null) return;

        smartHomeNotificationText.text = message;
        smartHomeNotificationText.color = color;

        if (smartHomeCloseButton != null) smartHomeCloseButton.SetActive(!autoHide);

        smartHomeNotificationCanvas.SetActive(true);
        if(itemNotificationCanvas != null) itemNotificationCanvas.SetActive(false); // 他方は隠す

        if (autoHide) StartCoroutine(HideNotificationTimer(3.0f));
    }

    // 再チェック用
    private IEnumerator ReCheckDelay()
    {
        yield return new WaitForSeconds(2.0f);
        StartCoroutine(FinalCheckForItems()); 
    }

    // 両方のキャンバスを閉じる
    public void CloseNotification()
    {
        if (itemNotificationCanvas != null) itemNotificationCanvas.SetActive(false);
        if (smartHomeNotificationCanvas != null) smartHomeNotificationCanvas.SetActive(false);
        notificationCoroutine = null;
    }

    public void SetModeDaily()
    {
        currentMode = AppMode.Daily;
        Debug.Log("【モード変更】: 日常モードになりました。");
        preparationModeOn.SetActive(true);
    }
    public void SetModePreparation()
    {
        currentMode = AppMode.Preparation;
        Debug.Log("【モード変更】: 準備モードになりました。");
        preparationModeOn.SetActive(false);
    }

    /// <summary>
    /// 「配置完了」ボタンから呼ばれる：確認パネルを表示
    /// </summary>
    public void ConfirmFinishSetup()
    {
        if (confirmationPanel == null) return;

        // 1. テキスト設定 (英語)
        if (confirmationText != null)
        {
            confirmationText.text = "Finish Setup?\n\nUnset items will be\nhidden from the list.";
        }

        // 2. ボタンのイベントを上書き
        // (Yesボタン: フィルタリング実行 -> 日常モードへ)
        // (Noボタン: キャンセル)
        SetupConfirmationButtons(
            onYes: () => {
                FilterUnsetAppliances(); // ★ 未設定を隠す
                if (uiManager != null) uiManager.OnFinishSetupConfirmed(); // ★ 日常モードへ
                confirmationPanel.SetActive(false);
            },
            onNo: () => {
                confirmationPanel.SetActive(false);
            }
        );

        // 3. 表示
        confirmationPanel.SetActive(true);
    }

    /// <summary>
    /// 未設定の家電ボタンを非表示にする
    /// </summary>
    private void FilterUnsetAppliances()
    {
        Debug.Log("Cleaning up appliance list...");

        // 家電リストの全ボタンを走査
        foreach (Transform child in applianceButtonContainer)
        {
            // ボタンの名前からアイテム名を復元 ("Button_LivingLight" -> "LivingLight")
            string buttonName = child.name;
            if (buttonName.StartsWith("Button_"))
            {
                string itemName = buttonName.Replace("Button_", "");

                // ★ 登録済みリストになければ非表示にする
                if (!registeredItems.ContainsKey(itemName))
                {
                    // (削除してもいいですが、後で復活させる可能性も考えて非表示推奨)
                    child.gameObject.SetActive(false);
                    Debug.Log($"Removed unset item: {itemName}");
                }
            }
        }
    }

    // ★★★ 2. 修正：ボタンを検索せず、変数を使うように変更 ★★★
    private void SetupConfirmationButtons(System.Action onYes, System.Action onNo)
    {
        // Yesボタンの設定
        if (confirmYesButton != null)
        {
            var btn = confirmYesButton.GetComponent<PressableButton>();
            if (btn != null)
            {
                btn.OnClicked.RemoveAllListeners();
                btn.OnClicked.AddListener(() => onYes?.Invoke());
            }
            else
            {
                Debug.LogError("ConfirmYesButton に PressableButton がありません！");
            }
        }
        else
        {
            Debug.LogError("ConfirmYesButton がインスペクターで設定されていません！");
        }

        // Noボタンの設定
        if (confirmNoButton != null)
        {
            var btn = confirmNoButton.GetComponent<PressableButton>();
            if (btn != null)
            {
                btn.OnClicked.RemoveAllListeners();
                btn.OnClicked.AddListener(() => onNo?.Invoke());
            }
        }
    }
}
