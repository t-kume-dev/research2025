using UnityEngine;

public class SwitchBotTest : MonoBehaviour
{
    [Header("テスト設定")]
    [SerializeField] private SwitchBotService switchBotService;
    [SerializeField] private string targetDeviceId; // ★ 確認したいデバイスIDをコピペ

    // インスペクターのコンテキストメニュー（右クリック）から実行できるようにする
    [ContextMenu("Check Device Status")]
    public void CheckStatus()
    {
        if (switchBotService == null || string.IsNullOrEmpty(targetDeviceId))
        {
            Debug.LogError("SwitchBotService または DeviceID が設定されていません。");
            return;
        }

        Debug.Log($"【テスト】デバイスID: {targetDeviceId} の状態を確認します...");
        StartCoroutine(switchBotService.GetDeviceStatus(targetDeviceId, (response) => {
            // 結果は SwitchBotService 側のログ (【SwitchBot RAW JSON】) で確認します
        }));
    }
}