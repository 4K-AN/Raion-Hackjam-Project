using UnityEngine;

public class BGMController : MonoBehaviour
{
    private static BGMController instance;
    public AudioSource audioSource;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // tetap hidup di semua scene
        }
        else
        {
            Destroy(gameObject); // hancurkan duplikat supaya hanya ada satu
        }
    }

    public void ChangeMusic(AudioClip newClip)
    {
        if (audioSource.clip == newClip && audioSource.isPlaying)
            return; // kalau sudah mainkan musik yang sama, jangan restart

        audioSource.clip = newClip;
        audioSource.loop = true;
        audioSource.Play();
    }
}
