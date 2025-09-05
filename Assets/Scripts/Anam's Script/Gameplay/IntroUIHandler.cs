using UnityEngine;

public class IntroUIHandler : MonoBehaviour
{
    [Header("UI yang Akan Diatur")]
    public GameObject heartsUIContainer; // Wadah untuk UI Hati
    public GameObject sequenceUIPanel;   // Panel untuk UI Sequence

    [Header("Pengaturan Waktu")]
    [Tooltip("Isi dengan total durasi video intro Anda (dalam detik).")]
    public float introDuration = 10f; // Contoh: 10 detik

    void Start()
    {
        // 1. Langsung sembunyikan UI saat game dimulai
        if (heartsUIContainer != null)
        {
            heartsUIContainer.SetActive(false);
        }
        if (sequenceUIPanel != null)
        {
            sequenceUIPanel.SetActive(false);
        }

        // 2. Jadwalkan fungsi untuk menampilkan UI Hati setelah intro selesai
        Invoke("ShowHeartsUI", introDuration);
    }

    void ShowHeartsUI()
    {
        // 3. Tampilkan kembali UI Hati
        if (heartsUIContainer != null)
        {
            heartsUIContainer.SetActive(true);
        }
    }
}