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
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[BGMController] Scene loaded: " + scene.name);

        // Kalau scene ada di daftar KeepMusic (misalnya Tutorial), biarkan musik jalan
        if (scenesToKeepMusic.Contains(scene.name))
        {
            Debug.Log("[BGMController] KeepMusic scene, musik tetap berjalan");
            return;
        }

        if (scene.name == mainMenuSceneName)
        {
            // Hanya play ulang kalau musik belum jalan
            if (audioSource.clip != mainMenuMusic || !audioSource.isPlaying)
            {
                PlayMusic(mainMenuMusic);
                Debug.Log("[BGMController] Main Menu, musik diputar");
            }
            else
            {
                Debug.Log("[BGMController] Main Menu, musik sudah jalan -> tidak restart");
            }
        }
        else if (scene.name == gameplaySceneName)
        {
            StopMusic();
            Debug.Log("[BGMController] Gameplay, musik dihentikan");
        }
        else
        {
            StopMusic();
            Debug.Log("[BGMController] Scene lain, musik dihentikan");
        }
    }

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

    public void StopMusic()
    {
        audioSource.Stop();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
