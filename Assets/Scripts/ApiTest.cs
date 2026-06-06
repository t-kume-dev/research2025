using UnityEngine;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
// using Newtonsoft.Json; // ログに出すだけなので不要

public class ApiTest : MonoBehaviour
{
    [Header("API設定")]
    [SerializeField]
    private string predictionEndpoint = "EndPointURL"; 
    [SerializeField]
    private string predictionKey = "Key";

    [Header("テスト画像")]
    [SerializeField]
    private Texture2D testImage; // ★ AssetsからJPG画像を設定

    /// <summary>
    /// PC（シミュレーター）でスペースキーを押すか、
    /// HoloLens実機で「登録完了」ボタン（FinalizeRegistration）を押すと実行されます
    /// </summary>
    public void StartApiTest()
    {
        Debug.Log("--- APIテスト開始 ---");

        if (testImage == null)
        {
            Debug.LogError("【テストエラー】: 'Test Image' がインスペクターで設定されていません！");
            return;
        }

        // Texture2D を JPG (byte[]) に変換
        byte[] jpgData;
        try
        {
            jpgData = testImage.EncodeToJPG(75);
            if (jpgData == null || jpgData.Length == 0)
            {
                Debug.LogError("【テストエラー】: テスト画像のJPGへのエンコードに失敗しました。");
                Debug.LogError("【対策】: テスト画像のインポート設定で 'Read/Write Enabled' にチェックを入れ、'Compression' を 'None' にしてください。");
                return;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"【テストエラー】: JPGエンコード中に例外発生: {e.Message}");
            Debug.LogError("【対策】: テスト画像のインポート設定で 'Read/Write Enabled' にチェックを入れ、'Compression' を 'None' にしてください。");
            return;
        }

        Debug.Log($"【テスト】: 画像（{jpgData.Length / 1024} KB）をAPIに送信します...");

        // 非同期メソッドを呼び出す（ただし待機しない）
        // 結果はコンソールログで確認する
        _ = SendRequest(jpgData);
    }

    /// <summary>
    /// APIに画像データを送信し、応答をログに出力する
    /// </summary>
    private async Task SendRequest(byte[] jpgData)
    {
        try
        {
            // 公式ドキュメントと同じ「毎回 new する」作法
            using (var client = new HttpClient())
            {
                // 1. キーを設定
                client.DefaultRequestHeaders.Add("Prediction-Key", predictionKey);
                
                // 2. Accept ヘッダーを設定（Hoppscotchの成功テストに合わせる）
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                using (var content = new ByteArrayContent(jpgData))
                {
                    // 3. Content-Type を設定
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    Debug.Log("【テスト】: HttpClient で画像送信を開始します...");
                    HttpResponseMessage response = await client.PostAsync(predictionEndpoint, content);
                    Debug.Log("【テスト】: APIサーバーから応答を受信しました。");

                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        Debug.Log("--- ★ APIテスト成功 ★ ---");

                        Debug.Log($"【RAW JSON】: {jsonResponse}");
                        Debug.Log("---------------------------");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("--- ★ APIテスト失敗（例外） ★ ---");
            Debug.LogError($"【例外メッセージ】: {e.Message}");
            Debug.LogError(e.StackTrace);
            Debug.Log("---------------------------------");
        }
    }
}