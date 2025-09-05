using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class BGMController : MonoBehaviour
{
    public static BGMController instance;

    [Header("Komponen Audio")]
    public AudioSource audioSource;

    [Header("Pengaturan Scene & Lagu")]
    [Tooltip("Tulis nama scene untuk Main Menu.")]
    public string mainMenuSceneName = "Main Menu";
    [Tooltip("Lagu yang akan diputar di scene Main Menu.")]
    public AudioClip mainMenuMusic;

    [Tooltip("Tulis nama scene untuk Gameplay.")]
    public string gameplaySceneName = "Gameplay";

    [Header("Pengaturan Lanjutan")]
    [Tooltip("Tulis nama scene di mana musik harus tetap berjalan (Contoh: Tutorial).")]
    public List<string> scenesToKeepMusic;

    // Fungsi Awake untuk Singleton Pattern
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Fungsi yang berjalan setiap pindah scene
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scenesToKeepMusic.Contains(scene.name))
        {
            return;
        }
        
        if (scene.name == mainMenuSceneName)
        {
            if (audioSource.clip != mainMenuMusic || !audioSource.isPlaying)
            {
                PlayMusic(mainMenuMusic);
            }
        }
        else if (scene.name == gameplaySceneName)
        {
            StopMusic();
        }
        else
        {
            StopMusic();
        }
    }

    // Fungsi untuk memainkan musik
    public void PlayMusic(AudioClip clipToPlay)
    {
        if (clipToPlay == null) 
        {
            StopMusic();
            return;
        }
        audioSource.clip = clipToPlay;
        audioSource.loop = true;
        audioSource.Play();
    }

    // Fungsi untuk menghentikan musik
    public void StopMusic()
    {
        audioSource.Stop();
    }

    // Fungsi OnDestroy untuk membersihkan event
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}