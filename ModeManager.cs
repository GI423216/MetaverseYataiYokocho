// --- ModeManager.cs ---

using UnityEngine;

public class ModeManager : MonoBehaviour
{
    public MainManager mainManager;
    private bool isCooking = false;

    public void OnClickModeChange()
    {
        // モードを反転
        isCooking = !isCooking;

        // MainManagerに「中身を差し替えて」と命令
        mainManager.SetMode(isCooking);

        // 必要に応じて、左側のインスペクターパネルの表示/非表示もここで切り替える
        // buildPanel.SetActive(!isCooking);
        // cookPanel.SetActive(isCooking);
    }
}