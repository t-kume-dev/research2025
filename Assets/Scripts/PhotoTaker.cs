using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Windows.WebCam;
using System.IO;

public class PhotoTaker : MonoBehaviour
{
    private PhotoCapture photoCaptureObject = null;
    private bool isCapturing = false;

    public event Action<byte[]> OnPhotoCapturedAsJPG;

    public void TakePhoto()
    {
        if (isCapturing)
        {
            Debug.Log("既に撮影処理が実行中です。");
            return;
        }

        isCapturing = true;
        Debug.Log("写真撮影を開始します...");
        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
    }

    void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        photoCaptureObject = captureObject;

        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

        CameraParameters c = new CameraParameters
        {
            hologramOpacity = 1.0f,
            cameraResolutionWidth = cameraResolution.width,
            cameraResolutionHeight = cameraResolution.height,
            pixelFormat = CapturePixelFormat.BGRA32
        };

        photoCaptureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
    }

    void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        }
        else
        {
            Debug.LogError("カメラモードを開始できませんでした。");
            isCapturing = false;
        }
    }

    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        if (result.success)
        {
            Debug.Log("写真をメモリにキャプチャしました。JPGに変換します...");

            Resolution res = PhotoCapture.SupportedResolutions.OrderByDescending((r) => r.width * r.height).First();
            Texture2D targetTexture = new Texture2D(res.width, res.height, TextureFormat.RGBA32, false);
            
            // ★★★ ここを修正しました ★★★
            // 古いUnityバージョンと互換性のあるメソッドを使用
            photoCaptureFrame.UploadImageDataToTexture(targetTexture);
            // ★★★★★★★★★★★★★★★★★

            byte[] jpgBytes = targetTexture.EncodeToJPG(75);
            Debug.Log($"JPGへの変換完了。データサイズ: {jpgBytes.Length / 1024} KB");

            // ★★★★★★★★★★★★★★★★★★★★★★★★★★★★★
            //
            // デバッグ（テスト）のために、撮影した写真をローカルに保存する
            //
            try
            {
                // 1. ファイル名を生成
                string timeStamp = System.DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                string fileName = $"Capture_{timeStamp}.jpg";

    // プラットフォーム（実行環境）に応じて保存場所を切り替える
#if ENABLE_WINMD_SUPPORT
                // HoloLens 2 (UWP) 実機で実行中の場合
                // 2. アプリ専用のローカル フォルダ (LocalState) へのパスを取得
                string filePath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, fileName);

                File.WriteAllBytes(filePath, jpgBytes);
                Debug.Log($"【デバッグ】写真をローカル (LocalState) に保存しました: {filePath}");
#else
                // Unityエディタ（シミュレータ）で実行中の場合
                // 2. Unityのプロジェクトフォルダ（Assetsフォルダ）に保存
                string filePath = Path.Combine(Application.dataPath, fileName);
                File.WriteAllBytes(filePath, jpgBytes);
                Debug.Log($"【デバッグ】エディタのため、写真をプロジェクトフォルダに保存しました: {filePath}");
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"【デバッグ】写真のローカル保存に失敗: {e.Message}");
            }
            //
            // ★★★★★★★★★★★★★★★★★★★★★★★★★★★★★

            OnPhotoCapturedAsJPG?.Invoke(jpgBytes);

            Destroy(targetTexture);
        }
        else
        {
            Debug.LogError("写真のメモリへのキャプチャに失敗しました。");
        }

        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObject?.Dispose();
        photoCaptureObject = null;
        isCapturing = false;
        Debug.Log("撮影プロセスを終了しました。");
    }
}