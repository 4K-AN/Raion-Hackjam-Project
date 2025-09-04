using UnityEngine;

public class BackToPauseMenuButton : MonoBehaviour
{
    public SceneLoader sceneLoader; // Drag Unloader for Control UI ke sini
    public string sceneToUnload; // Ketik "Control UI" di Inspector

    public void OnClick()
    {
        // 1. Tampilkan kembali panel pause di scene gameplay
        if (PauseManager.instance != null)
        {
            PauseManager.instance.ShowPausePanel();
        }

        // 2. Tutup scene ini
        sceneLoader.Unload();
    }
}