using UnityEngine;

public class PauseMenuUI : MonoBehaviour
{
    public PauseManager pauseManager; // drag GameObject PauseManager ke sini

    public void OnResumePressed()
    {
        if (pauseManager != null)
        {
            pauseManager.ResumeGame();
        }
        else
        {
            Debug.LogError("PauseManager belum dihubungkan ke PauseMenuUI!");
        }
    }
}
