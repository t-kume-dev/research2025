using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Security.Cryptography; // HMACSHA256に必要
using System.Text;
using Newtonsoft.Json;

public class SwitchBotService : MonoBehaviour
{
    [Header("SwitchBot API Settings")]
    [SerializeField] private string token = "YOUR_TOKEN"; // アプリから取得したトークン
    [SerializeField] private string secret = "YOUR_SECRET"; // アプリから取得したクライアントシークレット

    private const string BaseUrl = "https://api.switch-bot.com/v1.1";

    /// <summary>
    /// 全デバイスのリストを取得する
    /// </summary>
    public IEnumerator GetDeviceList(Action<SwitchBotDevicesResponse> callback)
    {
        string url = $"{BaseUrl}/devices";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            SetHeaders(request); // 認証ヘッダーをセット

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log($"【SwitchBot】Device List: {json}");
                
                try {
                    var response = JsonConvert.DeserializeObject<SwitchBotDevicesResponse>(json);
                    callback?.Invoke(response);
                } catch (Exception e) {
                    Debug.LogError($"【SwitchBot】JSON Parse Error: {e.Message}");
                    callback?.Invoke(null); 
                }
            }
            else
            {
                Debug.LogError($"【SwitchBot】List Error: {request.error} \n {request.downloadHandler.text}");
                callback?.Invoke(null);
            }
        }
    }

    // /// <summary>
    // /// 指定したデバイスの状態を取得する（プラグや開閉センサー用）
    // /// </summary>
    // public IEnumerator GetDeviceStatus(string deviceId, Action<SwitchBotStatusResponse> callback)
    // {
    //     string url = $"{BaseUrl}/devices/{deviceId}/status";
        
    //     using (UnityWebRequest request = UnityWebRequest.Get(url))
    //     {
    //         SetHeaders(request);

    //         yield return request.SendWebRequest();

    //         if (request.result == UnityWebRequest.Result.Success)
    //         {
    //             string json = request.downloadHandler.text;
    //             Debug.Log($"【SwitchBot】Status: {json}");
                
    //             try {
    //                 var response = JsonConvert.DeserializeObject<SwitchBotStatusResponse>(json);
    //                 callback?.Invoke(response);
    //             } catch { callback?.Invoke(null); }
    //         }
    //         else
    //         {
    //             Debug.LogError($"【SwitchBot】Status Error: {request.error}");
    //             callback?.Invoke(null);
    //         }
    //     }
    // }

    public IEnumerator GetDeviceStatus(string deviceId, Action<SwitchBotStatusResponse> callback)
    {
        string url = $"{BaseUrl}/devices/{deviceId}/status";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            SetHeaders(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                
                // ★★★★★★★★★★★★★★★★★★★★★★★★★★★★★
                //
                // 修正点：このログを追加して、生のJSONを確認します
                //
                // ★★★★★★★★★★★★★★★★★★★★★★★★★★★★★
                Debug.Log($"【SwitchBot RAW JSON】: {json}");
                
                try {
                    var response = JsonConvert.DeserializeObject<SwitchBotStatusResponse>(json);
                    callback?.Invoke(response);
                } catch { callback?.Invoke(null); }
            }
            else
            {
                Debug.LogError($"【SwitchBot】Status Error: {request.error}");
                callback?.Invoke(null);
            }
        }
    }

    /// <summary>
    /// デバイスにコマンドを送る（例: "turnOff"）
    /// </summary>
    public IEnumerator SendCommand(string deviceId, string command, string parameter = "default", string commandType = "command")
    {
        string url = $"{BaseUrl}/devices/{deviceId}/commands";
        
        var bodyData = new { command = command, parameter = parameter, commandType = commandType };
        string jsonBody = JsonConvert.SerializeObject(bodyData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            SetHeaders(request);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"【SwitchBot】Command '{command}' Success: {deviceId}");
            }
            else
            {
                Debug.LogError($"【SwitchBot】Command Error: {request.error}");
            }
        }
    }

    // --- 認証ヘッダー生成（HMAC-SHA256署名） ---
    private void SetHeaders(UnityWebRequest request)
    {
        long t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string nonce = Guid.NewGuid().ToString();
        string data = token + t + nonce;
        string sign = "";

        using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            byte[] signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            sign = Convert.ToBase64String(signatureBytes);
        }

        request.SetRequestHeader("Authorization", token);
        request.SetRequestHeader("sign", sign);
        request.SetRequestHeader("nonce", nonce);
        request.SetRequestHeader("t", t.ToString());
    }
}

// --- JSON用データクラス ---

public class SwitchBotDevicesResponse
{
    public int statusCode;
    public string message;
    public SwitchBotDeviceListBody body;
}

public class SwitchBotDeviceListBody
{
    public SwitchBotDevice[] deviceList;
    public SwitchBotDevice[] infraredRemoteList;
}

public class SwitchBotDevice
{
    public string deviceId;
    public string deviceName;
    public string deviceType; // "Plug", "Meter", "Lock" etc.
}

public class SwitchBotStatusResponse
{
    public int statusCode;
    public string message;
    public SwitchBotStatusBody body;
}

public class SwitchBotStatusBody
{
    public string deviceId;
    public string deviceType;
    public string power; // "on" / "off"
    public string openState; // "open" / "close"
}