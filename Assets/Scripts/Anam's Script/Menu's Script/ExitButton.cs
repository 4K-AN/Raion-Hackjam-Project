using UnityEngine;

public class ExitButton : MonoBehaviour
{
    // Fungsi publik tanpa parameter, Unity bisa panggil dari Button OnClick
    public void ExitGame()
    {
        Debug.Log("[ExitButton] Quit game called.");

#if UNITY_EDITOR
        // Kalau lagi di Editor → stop Play mode
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Kalau di build (exe/apk) → keluar aplikasi
        Application.Quit();
#endif
    }
}
