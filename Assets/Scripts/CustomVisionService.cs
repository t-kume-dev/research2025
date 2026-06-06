using UnityEngine;
using System;
using System.Net.Http; // ★ UnityWebRequest ではなく、公式と同じ HttpClient を使う
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json; 
// using UnityEngine.Networking; // ← 不要

public class CustomVisionService : MonoBehaviour
{
    [Header("Custom Vision API設定")]
    [SerializeField]
    private string predictionEndpoint = "YOUR_PREDICTION_ENDPOINT"; 
    [SerializeField]
    private string predictionKey = "YOUR_PREDICTION_KEY";

    // ★ HttpClient を使う
    private HttpClient client;

    public async Task<string> RecognizeItemAsync(byte[] jpgData)
    {
        if (jpgData == null || jpgData.Length == 0)
        {
            Debug.LogError("【APIエラー】: 送信しようとした画像データ (jpgData) が null または 0バイトです。");
            return null;
        }
        else
        {
            Debug.Log($"【APIステップ0.5】: 受信した画像データサイズ: {jpgData.Length / 1024} KB");
        }

        try
        {
            // ★★★ 修正点 ★★★
            //
            // リクエストのたびに HttpClient を new して使い捨てる
            // （公式ドキュメントと同じ作法）
            //
            // ★★★★★★★★★★★
            using (var client = new HttpClient())
            {
                // --- Hoppscotch（成功したテスト）のヘッダーをすべて設定 ---
                
                // 1. キーを設定
                client.DefaultRequestHeaders.Add("Prediction-Key", predictionKey);
                
                using (var content = new ByteArrayContent(jpgData))
                {
                    // 4. Content-Type を設定
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    Debug.Log("【APIステップ1】: HttpClient (毎回 new) で画像送信を開始します...");
                    HttpResponseMessage response = await client.PostAsync(predictionEndpoint, content);
                    // Response response = new Response();
                    // response = await Rest.PostAsync();
                    Debug.Log("【APIステップ2】: APIサーバーから応答を受信しました。");

                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.LogError($"【APIエラー】: API呼び出し失敗。ステータスコード: {response.StatusCode}");
                        Debug.LogError($"【APIエラー詳細】: {await response.Content.ReadAsStringAsync()}");
                        return null;
                    }

                    Debug.Log("【APIステップ3】: API呼び出し成功！ 応答JSONを解析します...");
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Debug.Log($"【API RAW JSON】: {jsonResponse}");

                    
                    Debug.Log($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");
                    Debug.Log($"Content-Length: {response.Content.Headers.ContentLength}");

                    if (string.IsNullOrEmpty(jsonResponse))
                    {
                        Debug.LogWarning("【API警告】: APIは成功しましたが、応答JSONが空でした。");
                        return null;
                    }
                    
                    // 5. JSON解析 
                    try
                    {
                        CustomVisionResponse visionResponse = JsonConvert.DeserializeObject<CustomVisionResponse>(jsonResponse);

                        if (visionResponse == null || visionResponse.Predictions == null)
                        {
                            Debug.LogError("【APIエラー】: JSONの解析に失敗しました。");
                            return null;
                        }

                        Prediction bestPrediction = null;
                        double maxProbability = 0;

                        foreach (var prediction in visionResponse.Predictions)
                        {
                            if (prediction.Probability > maxProbability)
                            {
                                maxProbability = prediction.Probability;
                                bestPrediction = prediction;
                            }
                        }

                        if (bestPrediction != null && bestPrediction.Probability > 0.1) 
                        {
                            Debug.Log($"【APIステップ4】: 認識成功！ -> {bestPrediction.TagName} ({(bestPrediction.Probability * 100):F1}%)");
                            return bestPrediction.TagName;
                        }
                        else
                        {
                            Debug.LogWarning("【APIステップ4】: 認識失敗（確信度不足、または予測結果ゼロ）");
                            return null;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"【APIエラー】: JSON解析中に例外発生: {e.Message}");
                        return null;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"【APIエラー】: APIリクエスト中に例外発生: {e.Message}");
            return null;
        }
    }

    // --- APIのJSON応答をパースするためのヘルパークラス ---
    // (APIの応答形式に合わせて調整が必要な場合があります)
    public class CustomVisionResponse
    {
        [JsonProperty("predictions")]
        public Prediction[] Predictions { get; set; }
    }

    public class Prediction
    {
        [JsonProperty("tagName")]
        public string TagName { get; set; }

        [JsonProperty("probability")]
        public double Probability { get; set; }
    }

}