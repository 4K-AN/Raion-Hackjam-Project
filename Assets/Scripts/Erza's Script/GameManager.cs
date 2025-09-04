using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System;

public class GameManager : MonoBehaviour
{
    // State machine untuk mengatur setiap tahapan permainan
    public enum GameState
    {
        Intro,              // Tahap 1, 2
        WaitingForReady,    // Tahap 3
        HighNoonWait,       // Tahap 4
        Clash,              // Tahap 5
        RoundOver,
        WaitForContinue,          // Tahap 6, 7, 8
        GameOver            // Tahap 9
    }
    private GameState currentState;

    [Header("Pengaturan Permainan")]
    public int startingLives = 3;
    public float minHighNoonDelay = 2f;
    public float maxHighNoonDelay = 5f;

    [Header("Referensi Manager")]
    public CutsceneManager cutsceneManager;
    public SequenceManager sequenceManager;
    
    [Header("UI Elemen")]
    public TMP_Text stateText; // Teks untuk menampilkan status (GET READY, HIGH NOON, dll)
    public GameObject[] p1HeartSprites; // 3 Sprite hati untuk Player 1
    public GameObject[] p2HeartSprites; // 3 Sprite hati untuk Player 2
    public GameObject gameOverPanel;
    public GameObject replayButton;

    // ============== KOLEKSI VIDEO DAN GAMBAR (URUTAN 1-9) ==============
    [Header("Visuals - Videos")]
    public VideoClip playerMeetClip;       // Video 1: Player bertemu
    public VideoClip faceOffClip;          // Video 2: Kedua karakter face off
    public VideoClip p1WinsRoundClip;      // Video 6: Player 1 menembak
    public VideoClip p2WinsRoundClip;      // Video 6: Player 2 menembak
    public VideoClip p1GetsHitClip;        // Video 7: Player 1 tertembak
    public VideoClip p2GetsHitClip;        // Video 7: Player 2 tertembak
    public VideoClip p1WinsGameClip;       // Video 8: Player 1 menang game
    public VideoClip p2WinsGameClip;       // Video 8: Player 2 menang game
    public VideoClip earlyShotClip;        // Video jika ada yang menembak terlalu cepat
    public VideoClip sequenceFailClip;     // Video jika ada yang gagal sequence

    [Header("Visuals - Sprites")]
    public Sprite readyPromptSprite;    // Gambar 3: Press Q and P to Ready
    public Sprite highNoonWaitSprite;   // Gambar 4: High Noon (menunggu)
    public Sprite highNoonShootSprite;  // Gambar 5: High Noon (mekanik tembak)
    public Sprite continuePromptSprite; // Gambar untuk prompt lanjut ke ronde berikutnya

    // Variabel internal untuk melacak status permainan
    private int p1Lives;
    private int p2Lives;
    private bool p1Ready;
    private bool p2Ready;
    private bool bellRang;
    private bool roundActive;

    #region Unity Lifecycle Functions
    private void Awake()
    {
        // Otomatis mencari manager lain jika belum di-assign
        if (sequenceManager == null) sequenceManager = FindObjectOfType<SequenceManager>();
        if (cutsceneManager == null) cutsceneManager = FindObjectOfType<CutsceneManager>();
    }

    private void OnEnable()
    {
        // Berlangganan event dari skrip input dan sequence manager
        QuickDrawInput.OnReady += HandleReady;
        QuickDrawInput.OnShoot += HandleShoot;
        QuickDrawInput.OnSequence += HandleSequence;
        
        if (sequenceManager != null)
        {
            sequenceManager.OnPlayerWinsClash += HandleClashWin;
            sequenceManager.OnPlayerFailsSequence += HandleSequenceFailure;
        }
    }

    private void OnDisable()
    {
        // Berhenti berlangganan event untuk menghindari error
        QuickDrawInput.OnReady -= HandleReady;
        QuickDrawInput.OnShoot -= HandleShoot;
        QuickDrawInput.OnSequence -= HandleSequence;
        
        if (sequenceManager != null)
        {
            sequenceManager.OnPlayerWinsClash -= HandleClashWin;
            sequenceManager.OnPlayerFailsSequence -= HandleSequenceFailure;
        }
    }

    private void Start()
    {
        // Inisialisasi permainan
        p1Lives = startingLives;
        p2Lives = startingLives;
        UpdateHealthUI();
        if(replayButton != null) replayButton.SetActive(false);
        if(gameOverPanel != null) gameOverPanel.SetActive(false);
        
        // Memulai permainan dari state Intro
        ChangeState(GameState.Intro);
    }
    #endregion

    #region State Machine
    // Fungsi pusat untuk mengubah tahapan permainan
    private void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log("Game State changed to: " + newState);
        stateText.text = ""; // Reset teks status setiap ganti state

        switch (currentState)
        {
            case GameState.Intro:
                StartCoroutine(IntroSequence());
                break;
            case GameState.WaitingForReady:
                cutsceneManager.PlaySingleImage(readyPromptSprite, 999f);
                stateText.text = "TEKAN TOMBOL READY";
                break;
            case GameState.HighNoonWait:
                StartCoroutine(HighNoonCountdown());
                break;
            case GameState.Clash:
                cutsceneManager.PlaySingleImage(highNoonShootSprite, 999f);
                sequenceManager.StartClashP1();
                sequenceManager.StartClashP2();
                stateText.text = "SELESAIKAN URUTAN!";
                break;
            case GameState.WaitForContinue:
                cutsceneManager.PlaySingleImage(continuePromptSprite, 999f);
                stateText.text = "Tekan Q dan P untuk Lanjut";
                break;
            case GameState.GameOver:
                VideoClip finalClip = (p1Lives > 0) ? p1WinsGameClip : p2WinsGameClip;
                StartCoroutine(PlayCutsceneAndThen(finalClip, () => {
                    if (gameOverPanel != null) gameOverPanel.SetActive(true);
                    if (replayButton != null) replayButton.SetActive(true);
                }));
                break;
        }
    }
    #endregion

    #region Game Flow Coroutines
    // Urutan #1 & #2: Memutar video intro
    private IEnumerator IntroSequence()
    {
        bool videoDone = false;
        Action onVideoFinish = () => videoDone = true;

        cutsceneManager.OnCutsceneFinished += onVideoFinish;
        cutsceneManager.PlaySingleClip(playerMeetClip);
        yield return new WaitUntil(() => videoDone);
        cutsceneManager.OnCutsceneFinished -= onVideoFinish;

        videoDone = false;
        cutsceneManager.OnCutsceneFinished += onVideoFinish;
        cutsceneManager.PlaySingleClip(faceOffClip);
        yield return new WaitUntil(() => videoDone);
        cutsceneManager.OnCutsceneFinished -= onVideoFinish;

        ChangeState(GameState.WaitingForReady);
    }

    // Urutan #4: Menunggu dengan waktu acak
    private IEnumerator HighNoonCountdown()
    {
        roundActive = true;
        bellRang = false;
        
        stateText.text = "GET READY...";
        bool imageDone = false;
        Action onImageFinish = () => imageDone = true;
        float randomDelay = UnityEngine.Random.Range(minHighNoonDelay, maxHighNoonDelay);
        
        cutsceneManager.OnCutsceneFinished += onImageFinish;
        cutsceneManager.PlaySingleImage(highNoonWaitSprite, randomDelay);
        yield return new WaitUntil(() => imageDone);
        cutsceneManager.OnCutsceneFinished -= onImageFinish;

        // Mainkan suara bel di sini jika ada
        bellRang = true;
        
        // Urutan #5: Memulai duel
        ChangeState(GameState.Clash);
    }
    #endregion

    #region Input Event Handlers
    // Urutan #3: Menangani input ready dari pemain
    private void HandleReady(int playerID)
    {
        if (currentState != GameState.WaitingForReady) return;

        if (playerID == 1) p1Ready = true;
        else if (playerID == 2) p2Ready = true;

        if (p1Ready && p2Ready)
        {
            ChangeState(GameState.HighNoonWait);
        }
    }

    // Menangani penalti jika menembak terlalu cepat
    private void HandleShoot(int playerID)
    {
        if (!roundActive || bellRang) return;

        if (playerID == 1) p1Lives--; else p2Lives--;
        UpdateHealthUI();
        stateText.text = $"Player {playerID} menembak terlalu cepat! (-1 nyawa)";
        StartCoroutine(PlayCutsceneAndThen(earlyShotClip, () => {
            if (!CheckGameOver()) ResetRound();
        }));
    }

    // Urutan #5: Meneruskan input sequence ke SequenceManager
    private bool HandleSequence(int playerID, Key key)
    {
        if (currentState == GameState.Clash)
        {
            return sequenceManager.ProcessInput(playerID, key);
        }
        return false;
    }
    #endregion

    #region Round & Game Logic
    // Urutan #6, #7, #8: Menangani pemenang ronde
    private void HandleClashWin(int winnerID)
    {
        if (currentState != GameState.Clash) return;
        
        currentState = GameState.RoundOver;
        StartCoroutine(RoundOverSequence(winnerID, true));
    }

    private void HandleSequenceFailure(int playerID)
    {
       if (currentState != GameState.Clash) return;
       
       currentState = GameState.RoundOver;
       int winnerID = (playerID == 1) ? 2 : 1; // Pemenangnya adalah lawan
       StartCoroutine(RoundOverSequence(winnerID, false));
    }

    private IEnumerator RoundOverSequence(int winnerID, bool isClashWin)
    {
        VideoClip winnerClip, loserClip;

        if (winnerID == 1)
        {
            p2Lives--;
            winnerClip = isClashWin ? p1WinsRoundClip : sequenceFailClip;
            loserClip = p2GetsHitClip;
        }
        else // winnerID == 2
        {
            p1Lives--;
            winnerClip = isClashWin ? p2WinsRoundClip : sequenceFailClip;
            loserClip = p1GetsHitClip;
        }
        
        UpdateHealthUI();

        // Putar video pemenang menembak
        bool videoDone = false;
        Action onVideoFinish = () => videoDone = true;
        cutsceneManager.OnCutsceneFinished += onVideoFinish;
        cutsceneManager.PlaySingleClip(winnerClip);
        yield return new WaitUntil(() => videoDone);
        cutsceneManager.OnCutsceneFinished -= onVideoFinish;

        // Putar video yang kalah tertembak
        videoDone = false;
        cutsceneManager.OnCutsceneFinished += onVideoFinish;
        cutsceneManager.PlaySingleClip(loserClip);
        yield return new WaitUntil(() => videoDone);
        cutsceneManager.OnCutsceneFinished -= onVideoFinish;
        
        // Cek Game Over atau lanjut ke ronde berikutnya
        if (!CheckGameOver())
        {
            ChangeState(GameState.WaitForContinue);
        }
    }

    private void UpdateHealthUI()
    {
        for(int i = 0; i < p1HeartSprites.Length; i++)
        {
            p1HeartSprites[i].SetActive(i < p1Lives);
        }
        for(int i = 0; i < p2HeartSprites.Length; i++)
        {
            p2HeartSprites[i].SetActive(i < p2Lives);
        }
    }

    private void ResetRound()
    {
        p1Ready = p2Ready = false;
        roundActive = false;
        bellRang = false;
        sequenceManager.ClearSequences();
        ChangeState(GameState.HighNoonWait);
    }

    // Urutan #9: Menangani akhir permainan
    private bool CheckGameOver()
    {
        if (p1Lives <= 0 || p2Lives <= 0)
        {
            ChangeState(GameState.GameOver);
            return true;
        }
        return false;
    }
    
    // Urutan #10: Fungsi untuk tombol Replay
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Fungsi bantuan untuk memutar cutscene
    private IEnumerator PlayCutsceneAndThen(VideoClip clip, Action after)
    {
        if (cutsceneManager == null || clip == null)
        {
            after?.Invoke();
            yield break;
        }
        bool done = false;
        Action onFinish = () => done = true;
        cutsceneManager.OnCutsceneFinished += onFinish;
        cutsceneManager.PlaySingleClip(clip);
        yield return new WaitUntil(() => done);
        cutsceneManager.OnCutsceneFinished -= onFinish;
        after?.Invoke();
    }
    #endregion
}