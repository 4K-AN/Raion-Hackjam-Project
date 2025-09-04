using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class BGMController : MonoBehaviour
{
    public static BGMController instance;

    public AudioSource audioSource;
    public AudioClip mainMenuMusic;
    public List<string> scenesToKeepMusic;

    // VARIABEL BARU: "Ingatan" untuk lagu yang sedang diputar
    private AudioClip currentMusicPlaying;

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

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Debug untuk melacak status
        Debug.Log("Scene Loaded: " + scene.name + ". Musik saat ini: " + (currentMusicPlaying ? currentMusicPlaying.name : "None"));

        if (scenesToKeepMusic.Contains(scene.name))
        {
            // Jika scene ini ada di daftar "keep music" (misal: Tutorial), jangan lakukan apa-apa.
            Debug.Log("Scene ada di daftar 'Keep Music'. Tidak ada perubahan musik.");
            return;
        }

        // --- LOGIKA UTAMA YANG DIPERBARUI ---
        if (scene.name == "Main Menu")
        {
            // Cek "ingatan" kita. Jika lagu yang seharusnya main BUKAN lagu MainMenu, baru kita mainkan.
            if (currentMusicPlaying != mainMenuMusic)
            {
                PlayMusic(mainMenuMusic);
            }
        }
        else if (scene.name == "Gameplay")
        {
            StopMusic();
        }
        else
        {
            // Untuk scene lain yang tidak terdaftar, hentikan musik
            StopMusic();
        }
    }

    public void PlayMusic(AudioClip clipToPlay)
    {
        Debug.Log("Memainkan musik: " + clipToPlay.name);
        audioSource.clip = clipToPlay;
        audioSource.loop = true;
        audioSource.Play();
        // Simpan lagu yang sedang main ke dalam "ingatan"
        currentMusicPlaying = clipToPlay;
    }

    public void StopMusic()
    {
        Debug.Log("Musik dihentikan.");
        audioSource.Stop();
        // Kosongkan "ingatan" karena tidak ada musik yang main
        currentMusicPlaying = null;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}