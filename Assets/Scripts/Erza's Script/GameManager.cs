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
        WaitForContinue,    // Tahap 6, 7, 8
        GameOver            // Tahap 9
    }
    private GameState currentState;

    [Header("Pengaturan Permainan")]
    public int startingLives = 3;
    public float minHighNoonDelay = 2f;
    public float maxHighNoonDelay = 5f;
    public float continueWaitDuration = 3f; // Durasi menampilkan prompt continue

    [Header("Referensi Manager")]
    public CutsceneManager cutsceneManager;
    public SequenceManager sequenceManager;
    
    [Header("UI Elemen")]
    public TMP_Text stateText; // Teks untuk menampilkan status (GET READY, HIGH NOON, dll)
    public GameObject[] p1HeartSprites; // 3 Sprite hati untuk Player 1
    public GameObject[] p2HeartSprites; // 3 Sprite hati untuk Player 2
    public GameObject gameOverPanel;
    public GameObject replayButton;
    public GameObject sequenceUIPanel;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // ============== KOLEKSI VIDEO DAN GAMBAR (URUTAN 1-9) ==============
    [Header("Visuals - Videos")]
    public VideoClip playerMeetClip;       // Video 1: Player bertemu
    public VideoClip faceOffClip;          // Video 2: Kedua karakter face off
    [Header("Visuals - Sprites")]
    public Sprite readyPromptSprite;    // Gambar 3: Press Q and P to Ready
    public Sprite highNoonWaitSprite;   // Gambar 4: High Noon (menunggu)
    public Sprite highNoonShootSprite;  // Gambar 5: High Noon (mekanik tembak)
    public VideoClip p1WinsRoundClip;      // Video 6: Player 1 menembak
    public VideoClip p2WinsRoundClip;      // Video 6: Player 2 menembak
    public VideoClip p1GetsHitClip;        // Video 7: Player 1 tertembak
    public VideoClip p2GetsHitClip;        // Video 7: Player 2 tertembak
    public VideoClip p1WinsGameClip;       // Video 8: Player 1 menang game
    public VideoClip p2WinsGameClip;       // Video 8: Player 2 menang game
    public VideoClip earlyShotClip;        // Video jika ada yang menembak terlalu cepat
    public VideoClip sequenceFailClip;     // Video jika ada yang gagal sequence

    [Header("Visuals - Sprites")]
    public Sprite continuePromptSprite; // Gambar untuk prompt lanjut ke ronde berikutnya

    // Variabel internal untuk melacak status permainan
    private int p1Lives;
    private int p2Lives;
    private bool p1Ready;
    private bool p2Ready;
    private bool bellRang;
    private bool roundActive;
    private bool waitingForContinue; // Flag untuk menunggu continue

    // Coroutine tracking untuk mencegah overlap
    private Coroutine currentGameFlowCoroutine;

    #region Unity Lifecycle Functions
    private void Awake()
    {
        // Otomatis mencari manager lain jika belum di-assign
        if (sequenceManager == null) sequenceManager = FindObjectOfType<SequenceManager>();
        if (cutsceneManager == null) cutsceneManager = FindObjectOfType<CutsceneManager>();
        
        DebugLog("GameManager initialized");
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
        
        DebugLog("Event subscriptions completed");
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
        waitingForContinue = false;
        
        // Initialize UI
        UpdateHealthUI();
        if(replayButton != null) replayButton.SetActive(false);
        if(gameOverPanel != null) gameOverPanel.SetActive(false);
        if(sequenceUIPanel != null) sequenceUIPanel.SetActive(false);
        
        // Memulai permainan dari state Intro
        ChangeState(GameState.Intro);
    }

    void Update()
    {
        // Handle continue input dengan debug yang lebih detail
        if (waitingForContinue && currentState == GameState.WaitForContinue)
        {
            bool qPressed = Input.GetKey(KeyCode.Q);
            bool pPressed = Input.GetKey(KeyCode.P);

            // Debug untuk melihat status tombol
            if (qPressed || pPressed)
            {
                DebugLog($"Continue input - Q: {qPressed}, P: {pPressed}");
            }

            if (qPressed && pPressed)
            {
                waitingForContinue = false;
                DebugLog("Continue input detected, starting new round");
                ResetRound();
            }
        }
     if (enableDebugLogs)
    {
        if (Input.GetKeyDown(KeyCode.Q)) DebugLog("Q key detected in GameManager");
        if (Input.GetKeyDown(KeyCode.P)) DebugLog("P key detected in GameManager");
    }
    }
    
    #endregion

    #region State Machine
    // Fungsi pusat untuk mengubah tahapan permainan
    private void ChangeState(GameState newState)
    {
        GameState previousState = currentState;
        currentState = newState;
        DebugLog($"Game State changed from {previousState} to {newState}");

        // Reset state text
        if (stateText != null) stateText.text = "";

        // Stop any ongoing coroutine to prevent overlaps
        if (currentGameFlowCoroutine != null)
        {
            StopCoroutine(currentGameFlowCoroutine);
            currentGameFlowCoroutine = null;
        }

        // SEMBUNYIKAN PANEL DUEL SECARA DEFAULT (kecuali untuk Clash)
        if (sequenceUIPanel != null && newState != GameState.Clash)
            sequenceUIPanel.SetActive(false);

        switch (currentState)
        {
            case GameState.Intro:
                currentGameFlowCoroutine = StartCoroutine(IntroSequence());
                break;

            case GameState.WaitingForReady:
                StartReadyWait();
                break;

            case GameState.HighNoonWait:
                currentGameFlowCoroutine = StartCoroutine(HighNoonCountdown());
                break;

            case GameState.Clash:
                StartClash();
                break;

            case GameState.WaitForContinue:
                StartContinueWait();
                break;

            case GameState.GameOver:
                currentGameFlowCoroutine = StartCoroutine(GameOverSequence());
                break;
        }
    }

    void StartReadyWait()
    {
        DebugLog("Starting ready wait phase");
        p1Ready = false;
        p2Ready = false;
        
        if (stateText != null) stateText.text = "TEKAN TOMBOL READY (Q & P)";
        
        if (cutsceneManager != null && readyPromptSprite != null)
        {
            cutsceneManager.PlaySingleImage(readyPromptSprite, 999f);
        }
    }

    void StartClash()
    {
        DebugLog("Starting clash phase");
        
        // 1. Tampilkan panel UI untuk duel
        if (sequenceUIPanel != null) 
        {
            sequenceUIPanel.SetActive(true);
            DebugLog("Sequence UI Panel activated");
        }
        
        // 2. Mainkan gambar latar belakang
        if (cutsceneManager != null && highNoonShootSprite != null)
        {
            cutsceneManager.PlaySingleImage(highNoonShootSprite, 999f);
            DebugLog("High noon shoot sprite displayed");
        }

        // 3. MEMULAI SequenceManager untuk membuat ikon (INI YANG DIPERBAIKI)
        if (sequenceManager != null)
        {
            // Clear any existing sequences first
            sequenceManager.ClearSequences();
            
            // Start new clash sequences
            sequenceManager.StartClashP1();
            sequenceManager.StartClashP2();
            DebugLog("Sequence manager clash started for both players");
        }
        else
        {
            DebugLog("ERROR: SequenceManager is null!");
        }
        
        // 4. Tampilkan teks status
        if (stateText != null) stateText.text = "SELESAIKAN URUTAN!";
    }

    void StartContinueWait()
    {
        DebugLog("Starting continue wait phase");
        waitingForContinue = true;
        
        if (stateText != null) stateText.text = "Tekan Q dan P untuk Lanjut";
        
        if (cutsceneManager != null && continuePromptSprite != null)
        {
            cutsceneManager.PlaySingleImage(continuePromptSprite, continueWaitDuration);
        }
    }
    #endregion

    #region Game Flow Coroutines
    // Urutan #1 & #2: Memutar video intro
    private IEnumerator IntroSequence()
    {
        DebugLog("Starting intro sequence");
        
        // Video 1: Player meet
        if (playerMeetClip != null)
        {
            yield return StartCoroutine(PlayVideoAndWait(playerMeetClip));
        }

        // Video 2: Face off
        if (faceOffClip != null)
        {
            yield return StartCoroutine(PlayVideoAndWait(faceOffClip));
        }

        DebugLog("Intro sequence completed");
        ChangeState(GameState.WaitingForReady);
    }

    // Urutan #4: Menunggu dengan waktu acak
    private IEnumerator HighNoonCountdown()
    {
        DebugLog("Starting high noon countdown");
        
        roundActive = true;
        bellRang = false;
        
        if (stateText != null) stateText.text = "GET READY...";
        
        float randomDelay = UnityEngine.Random.Range(minHighNoonDelay, maxHighNoonDelay);
        DebugLog($"High noon delay: {randomDelay:F2} seconds");
        
        if (cutsceneManager != null && highNoonWaitSprite != null)
        {
            yield return StartCoroutine(PlayImageAndWait(highNoonWaitSprite, randomDelay));
        }
        else
        {
            yield return new WaitForSeconds(randomDelay);
        }

        // Bell rang!
        bellRang = true;
        DebugLog("Bell rang - starting clash");
        
        ChangeState(GameState.Clash);
    }

    private IEnumerator GameOverSequence()
    {
        DebugLog($"Starting game over sequence - P1 Lives: {p1Lives}, P2 Lives: {p2Lives}");
        
        VideoClip finalClip = (p1Lives > 0) ? p1WinsGameClip : p2WinsGameClip;
        
        if (finalClip != null)
        {
            yield return StartCoroutine(PlayVideoAndWait(finalClip));
        }
        
        // Show game over UI
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (replayButton != null) replayButton.SetActive(true);
        
        DebugLog("Game over sequence completed");
    }

    // Helper coroutines
    private IEnumerator PlayVideoAndWait(VideoClip clip)
    {
        if (cutsceneManager == null || clip == null) yield break;
        
        bool videoDone = false;
        Action onVideoFinish = () => videoDone = true;
        
        cutsceneManager.OnCutsceneFinished += onVideoFinish;
        cutsceneManager.PlaySingleClip(clip);
        
        yield return new WaitUntil(() => videoDone);
        
        cutsceneManager.OnCutsceneFinished -= onVideoFinish;
        
        DebugLog($"Video completed: {clip.name}");
    }

    private IEnumerator PlayImageAndWait(Sprite sprite, float duration)
    {
        if (cutsceneManager == null || sprite == null) yield break;
        
        bool imageDone = false;
        Action onImageFinish = () => imageDone = true;
        
        cutsceneManager.OnCutsceneFinished += onImageFinish;
        cutsceneManager.PlaySingleImage(sprite, duration);
        
        yield return new WaitUntil(() => imageDone);
        
        cutsceneManager.OnCutsceneFinished -= onImageFinish;
        
        DebugLog($"Image completed: {sprite.name}");
    }
    #endregion

    #region Input Event Handlers
    // Urutan #3: Menangani input ready dari pemain
    private void HandleReady(int playerID)
    {
        if (currentState != GameState.WaitingForReady) return;

        DebugLog($"Player {playerID} ready");
        
        if (playerID == 1) p1Ready = true;
        else if (playerID == 2) p2Ready = true;

        if (p1Ready && p2Ready)
        {
            DebugLog("Both players ready - starting high noon");
            ChangeState(GameState.HighNoonWait);
        }
    }

    // Menangani penalti jika menembak terlalu cepat
    private void HandleShoot(int playerID)
    {
        if (!roundActive || bellRang) return;

        DebugLog($"Player {playerID} shot too early!");
        
        if (playerID == 1) p1Lives--; 
        else p2Lives--;
        
        UpdateHealthUI();
        
        if (stateText != null) stateText.text = $"Player {playerID} menembak terlalu cepat! (-1 nyawa)";
        
        currentGameFlowCoroutine = StartCoroutine(HandleEarlyShotPenalty(playerID));
    }

    private IEnumerator HandleEarlyShotPenalty(int playerID)
    {
        roundActive = false;
        
        if (earlyShotClip != null)
        {
            yield return StartCoroutine(PlayVideoAndWait(earlyShotClip));
        }
        
        if (!CheckGameOver())
        {
            ResetRound();
        }
    }

    // Urutan #5: Meneruskan input sequence ke SequenceManager
    private bool HandleSequence(int playerID, Key key)
    {
        if (currentState == GameState.Clash && sequenceManager != null)
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
        
        DebugLog($"Clash won by Player {winnerID}");
        currentState = GameState.RoundOver;
        currentGameFlowCoroutine = StartCoroutine(RoundOverSequence(winnerID, true));
    }

    private void HandleSequenceFailure(int playerID)
    {
       if (currentState != GameState.Clash) return;
       
       DebugLog($"Player {playerID} failed sequence");
       currentState = GameState.RoundOver;
       int winnerID = (playerID == 1) ? 2 : 1; // Pemenangnya adalah lawan
       currentGameFlowCoroutine = StartCoroutine(RoundOverSequence(winnerID, false));
    }

    private IEnumerator RoundOverSequence(int winnerID, bool isClashWin)
    {
        DebugLog($"Round over - Winner: Player {winnerID}, Clash Win: {isClashWin}");
        
        // Hide sequence UI
        if (sequenceUIPanel != null) sequenceUIPanel.SetActive(false);
        
        VideoClip winnerClip, loserClip;
        int loserID;

        if (winnerID == 1)
        {
            p2Lives--;
            loserID = 2;
            winnerClip = isClashWin ? p1WinsRoundClip : sequenceFailClip;
            loserClip = p2GetsHitClip;
        }
        else // winnerID == 2
        {
            p1Lives--;
            loserID = 1;
            winnerClip = isClashWin ? p2WinsRoundClip : sequenceFailClip;
            loserClip = p1GetsHitClip;
        }
        
        DebugLog($"Lives after round - P1: {p1Lives}, P2: {p2Lives}");
        UpdateHealthUI();

        // Putar video pemenang menembak
        if (winnerClip != null)
        {
            yield return StartCoroutine(PlayVideoAndWait(winnerClip));
        }

        // Putar video yang kalah tertembak
        if (loserClip != null)
        {
            yield return StartCoroutine(PlayVideoAndWait(loserClip));
        }
        
        // Cek Game Over atau lanjut ke ronde berikutnya
        if (CheckGameOver())
        {
            // Game over will be handled by CheckGameOver()
        }
        else
        {
            DebugLog("Round completed, waiting for continue");
            ChangeState(GameState.WaitForContinue);
        }
    }

    private void UpdateHealthUI()
    {
        DebugLog($"Updating health UI - P1: {p1Lives}, P2: {p2Lives}");
        
        // Update Player 1 hearts
        if (p1HeartSprites != null)
        {
            for(int i = 0; i < p1HeartSprites.Length; i++)
            {
                if (p1HeartSprites[i] != null)
                {
                    bool shouldShow = i < p1Lives;
                    p1HeartSprites[i].SetActive(shouldShow);
                    DebugLog($"P1 Heart {i}: {(shouldShow ? "Active" : "Inactive")}");
                }
            }
        }
        
        // Update Player 2 hearts
        if (p2HeartSprites != null)
        {
            for(int i = 0; i < p2HeartSprites.Length; i++)
            {
                if (p2HeartSprites[i] != null)
                {
                    bool shouldShow = i < p2Lives;
                    p2HeartSprites[i].SetActive(shouldShow);
                    DebugLog($"P2 Heart {i}: {(shouldShow ? "Active" : "Inactive")}");
                }
            }
        }
    }

    private void ResetRound()
    {
        DebugLog("Resetting round");
        
        p1Ready = p2Ready = false;
        roundActive = false;
        bellRang = false;
        waitingForContinue = false;
        
        if (sequenceManager != null)
        {
            sequenceManager.ClearSequences();
        }
        
        ChangeState(GameState.WaitingForReady);
    }

    // Urutan #9: Menangani akhir permainan
    private bool CheckGameOver()
    {
        if (p1Lives <= 0 || p2Lives <= 0)
        {
            DebugLog($"Game Over! P1 Lives: {p1Lives}, P2 Lives: {p2Lives}");
            ChangeState(GameState.GameOver);
            return true;
        }
        return false;
    }
    
    // Urutan #10: Fungsi untuk tombol Replay
    public void RestartGame()
    {
        DebugLog("Restarting game");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Debug helper
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[GameManager] {message}");
        }
    }
    #endregion
}