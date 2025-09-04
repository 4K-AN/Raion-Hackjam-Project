// File: PauseManager.cs
using UnityEngine.Video;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    // === BAGIAN 1: Tambahkan kode Singleton ===
    // Ini membuat PauseManager bisa diakses dari mana saja dengan memanggil PauseManager.instance
    public static PauseManager instance;

    void Awake()
    {
        // Logika sederhana untuk memastikan hanya ada satu PauseManager
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // ===========================================

    [Header("UI Settings")]
    public GameObject pauseMenuPanel;

    [Header("Game Components")]
    public VideoPlayer mainVideoPlayer;
    public static bool isPaused = false;

    void Start()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        ResumeGame();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // === BAGIAN 2: Tambahkan fungsi baru untuk menampilkan panel ===
    // Fungsi ini akan dipanggil oleh tombol "Kembali" dari scene Control UI / Tutorial UI
    public void ShowPausePanel()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
    }
    // =============================================================

    public void PauseGame()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            isPaused = true;

            if (mainVideoPlayer != null && mainVideoPlayer.isPlaying)
            {
                mainVideoPlayer.Pause();
            }

            AudioListener.pause = true;
            Time.timeScale = 0f;
            Debug.Log("Game Paused. Video and Audio should be stopped.");
        }
    }

    public void ResumeGame()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            isPaused = false;

            if (mainVideoPlayer != null)
            {
                mainVideoPlayer.Play();
            }

            AudioListener.pause = false;
            Time.timeScale = 1f;
            Debug.Log("Game Resumed.");
        }
    }
}