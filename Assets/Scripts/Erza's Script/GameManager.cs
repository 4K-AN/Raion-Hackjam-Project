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
        Intro,              // Tahap 1, 2 (Face off only)
        WaitingForReady,    // Tahap 3 (Ready prompt)
        HighNoonWait,       // Tahap 4 (Random wait time)
        Clash,              // Tahap 5 (High noon shoot with sequence)
        RoundOver,          // Processing round results
        WaitForContinue,    // Wait for input to continue
        GameOver            // Final game over
    }
    private GameState currentState;

    [Header("Pengaturan Permainan")]
    public int startingLives = 3;
    public float minHighNoonDelay = 2f;
    public float maxHighNoonDelay = 5f;
    public float continueWaitDuration = 3f;

    [Header("Referensi Manager")]
    public CutsceneManager cutsceneManager;
    public SequenceManager sequenceManager;
    
    [Header("UI Elemen")]
    public TMP_Text stateText;
    public GameObject[] p1HeartSprites; // 3 Sprite hati untuk Player 1
    public GameObject[] p2HeartSprites; // 3 Sprite hati untuk Player 2
    public GameObject gameOverPanel;
    public GameObject replayButton;
    public GameObject sequenceUIPanel; // Panel yang berisi sequence containers
    public GameObject pauseMenu; // Menu pause

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // ============== KOLEKSI VIDEO DAN GAMBAR ==============
    [Header("Visuals - Videos")]
    public VideoClip playerMeetClip;       // Video 1: Player bertemu
    public VideoClip faceOffClip;          // Video 2: Face off
    [Header("Visuals - Sprites")]
    public Sprite readyPromptSprite;    // Gambar: Press Q and P to Ready
    public Sprite highNoonWaitSprite;   // Gambar: High Noon (menunggu)
    public Sprite highNoonShootSprite;  // Gambar: High Noon (background untuk sequence)
    [Header("Visuals - Round Result Videos")]
    public VideoClip p1WinsRoundClip;      // Player 1 menang ronde
    public VideoClip p2WinsRoundClip;      // Player 2 menang ronde
    [Header("Visuals - Game End Videos")]
    public VideoClip p1WinsGameClip;       // Player 1 menang game
    public VideoClip p2WinsGameClip;       // Player 2 menang game
    [Header("Visuals - Penalty Videos")]
    public VideoClip earlyShotClip;        // Video jika menembak terlalu cepat
    public VideoClip sequenceFailClip;     // Video jika gagal sequence/timeout

    [Header("Audio")]
    public AudioClip bellClip;
    private AudioSource audioSource;


    // Variabel internal
    private int p1Lives;
    private int p2Lives;
    private bool p1Ready;
    private bool p2Ready;
    private bool bellRang;
    private bool roundActive;
    private bool waitingForContinue;
    private Coroutine currentGameFlowCoroutine;

    #region Unity Lifecycle Functions
    private void Awake()
    {
        if (sequenceManager == null) sequenceManager = FindObjectOfType<SequenceManager>();
        if (cutsceneManager == null) cutsceneManager = FindObjectOfType<CutsceneManager>();
        DebugLog("GameManager initialized");
         audioSource = GetComponent<AudioSource>();
    if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

    DebugLog("GameManager initialized");
    }

    private void OnEnable()
    {
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
        
        // TAMBAHAN DEBUG - CHECK VIDEO ASSIGNMENTS
        DebugLog("=== CHECKING VIDEO ASSIGNMENTS IN START ===");
        DebugLog($"playerMeetClip: {(playerMeetClip != null ? playerMeetClip.name : "NULL")}");
        DebugLog($"faceOffClip: {(faceOffClip != null ? faceOffClip.name : "NULL")}");
        DebugLog($"CutsceneManager: {(cutsceneManager != null ? "Found" : "NULL")}");
        
        // Initialize UI - TAMPILKAN HEALTH UI DARI AWAL
        UpdateHealthUI();
        if(replayButton != null) replayButton.SetActive(false);
        if(gameOverPanel != null) gameOverPanel.SetActive(false);
         // if(sequenceUIPanel != null) sequenceUIPanel.SetActive(false); 
        if(pauseMenu != null) pauseMenu.SetActive(false);
        
        // Memulai permainan dari state Intro
        ChangeState(GameState.Intro);
    }
    
    void Update()
    {
        // Handle continue input - HANYA SAAT WAITING FOR CONTINUE
        if (waitingForContinue && currentState == GameState.WaitForContinue)
        {
            bool qPressed = Input.GetKey(KeyCode.Q);
            bool pPressed = Input.GetKey(KeyCode.P);
            
            if (qPressed && pPressed)
            {
                waitingForContinue = false;
                DebugLog("Continue input detected, starting new round");
                ResetRound();
            }
        }
        
        // Debug input
        if (enableDebugLogs)
        {
            if (Input.GetKeyDown(KeyCode.Q)) DebugLog("Q key detected in GameManager");
            if (Input.GetKeyDown(KeyCode.P)) DebugLog("P key detected in GameManager");
        }
    }
    #endregion

    #region State Machine
    private void ChangeState(GameState newState)
    {
        GameState previousState = currentState;
        currentState = newState;
        DebugLog($"Game State changed from {previousState} to {newState}");
        
        // Reset state text
        if (stateText != null) stateText.text = "";

        // Stop any ongoing coroutine
        if (currentGameFlowCoroutine != null)
        {
            StopCoroutine(currentGameFlowCoroutine);
            currentGameFlowCoroutine = null;
        }

        // SEMBUNYIKAN PANEL SEQUENCE SECARA DEFAULT (kecuali untuk Clash)
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
        roundActive = false; // PENTING: Tidak aktif saat ready wait
        bellRang = false;
        
        // SELALU UPDATE HEALTH UI SAAT READY WAIT
        UpdateHealthUI();
        
        if (stateText != null) stateText.text = "TEKAN Q DAN P BERSAMAAN UNTUK READY";
        
        if (cutsceneManager != null && readyPromptSprite != null)
        {
            cutsceneManager.PlaySingleImage(readyPromptSprite, 999f); // Long duration
        }
    }

    void StartClash()
    {
        DebugLog("Starting clash phase - displaying sequence UI");
        
        roundActive = true; // Round aktif saat clash
        
        // 1. AKTIFKAN Panel UI untuk sequence
        if (sequenceUIPanel != null) 
        {
            sequenceUIPanel.SetActive(true);
            DebugLog("Sequence UI Panel activated");
        }
        else
        {
            DebugLog("ERROR: sequenceUIPanel is NULL!");
        }
        
        // 2. Tampilkan background sprite
        if (cutsceneManager != null && highNoonShootSprite != null)
        {
            cutsceneManager.PlaySingleImage(highNoonShootSprite, 999f);
            DebugLog("High noon shoot sprite displayed as background");
        }

        if (bellClip != null && audioSource != null)
{
    audioSource.PlayOneShot(bellClip);
    DebugLog("Bell sound played at High Noon Shoot");
}

        // 3. MULAI SequenceManager
        if (sequenceManager != null)
        {
            sequenceManager.ClearSequences();
            sequenceManager.StartClashP1();
            sequenceManager.StartClashP2();
            DebugLog("Sequence manager started for both players");
        }
        else
        {
            DebugLog("ERROR: SequenceManager is null!");
        }
        
        // 4. Tampilkan instruksi
        if (stateText != null) stateText.text = "SELESAIKAN URUTAN TOMBOL!";
    }

    void StartContinueWait()
    {
        DebugLog("Starting continue wait phase");
        waitingForContinue = true;
        roundActive = false;
        
        if (stateText != null) stateText.text = "Tekan Q dan P bersamaan untuk lanjut ronde berikutnya";
        
        // Tidak perlu sprite khusus, cukup teks
        if (cutsceneManager != null)
        {
            // Clear any ongoing visuals
            cutsceneManager.StopCurrentCutscene();
        }
    }
    #endregion

    #region Game Flow Coroutines
    private IEnumerator IntroSequence()
{
    DebugLog("Starting intro sequence");
    
    // Putar video player meet terlebih dahulu
    if (playerMeetClip != null)
    {
        yield return StartCoroutine(PlayVideoAndWait(playerMeetClip));
    }
    
    // Putar video face off setelahnya
    if (faceOffClip != null)
    {
        yield return StartCoroutine(PlayVideoAndWait(faceOffClip));
    }

    DebugLog("Intro sequence completed, going to ready wait");
    ChangeState(GameState.WaitingForReady);
}

    private IEnumerator HighNoonCountdown()
    {
        DebugLog("Starting high noon countdown");
        
        roundActive = false; // BELUM aktif saat countdown
        bellRang = false;
        
        if (stateText != null) stateText.text = "GET READY...";
        
        float randomDelay = UnityEngine.Random.Range(minHighNoonDelay, maxHighNoonDelay);
        DebugLog($"High noon delay: {randomDelay:F2} seconds");
        
        // Tampilkan sprite wait
        if (cutsceneManager != null && highNoonWaitSprite != null)
        {
            cutsceneManager.PlaySingleImage(highNoonWaitSprite, randomDelay);
        }
        
        yield return new WaitForSeconds(randomDelay);

        // Bell rang - langsung ke clash
        bellRang = true;
        DebugLog("Bell rang - starting clash immediately");
        
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
        
        // Show game over UI dengan replay button
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
    #endregion

    #region Input Event Handlers
    private void HandleReady(int playerID)
    {
        if (currentState != GameState.WaitingForReady) return;

        DebugLog($"Player {playerID} ready");
        
        if (playerID == 1) p1Ready = true;
        else if (playerID == 2) p2Ready = true;

        // HARUS KEDUA PLAYER READY BERSAMAAN
        if (p1Ready && p2Ready)
        {
            DebugLog("Both players ready simultaneously - starting high noon");
            ChangeState(GameState.HighNoonWait);
        }
    }

    private void HandleShoot(int playerID)
    {
        // HANYA penalti jika menembak sebelum bell (saat HighNoonWait)
        if (currentState == GameState.HighNoonWait && !bellRang)
        {
            DebugLog($"Player {playerID} shot too early during countdown!");
            
            if (playerID == 1) p1Lives--; 
            else p2Lives--;
            
            UpdateHealthUI();
            
            if (stateText != null) stateText.text = $"Player {playerID} menembak terlalu cepat! (-1 nyawa)";
            
            currentGameFlowCoroutine = StartCoroutine(HandleEarlyShotPenalty(playerID));
        }
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
        
        DebugLog($"Player {playerID} failed sequence (timeout)");
        currentState = GameState.RoundOver;
        int winnerID = (playerID == 1) ? 2 : 1;
        currentGameFlowCoroutine = StartCoroutine(RoundOverSequence(winnerID, false));
    }

    private IEnumerator RoundOverSequence(int winnerID, bool isClashWin)
    {
        Debug.Log($"Ronde Selesai - Pemenang: Player {winnerID}, Menang Duel: {isClashWin}");

        roundActive = false;

        // Langsung sembunyikan UI sequence
        if (sequenceUIPanel != null) sequenceUIPanel.SetActive(false);

        VideoClip winnerClip;

        // Kurangi nyawa pemain yang kalah
        if (winnerID == 1)
        {
            p2Lives--;
            // Gunakan video kemenangan P1, atau video gagal sequence jika relevan
            winnerClip = isClashWin ? p1WinsRoundClip : sequenceFailClip;
        }
        else // winnerID == 2
        {
            p1Lives--;
            winnerClip = isClashWin ? p2WinsRoundClip : sequenceFailClip;
        }

        Debug.Log($"Nyawa setelah ronde - P1: {p1Lives}, P2: {p2Lives}");
        UpdateHealthUI();

        // 1. Putar HANYA video pemenang
        if (winnerClip != null)
        {
            // Saya asumsikan Anda memiliki helper coroutine bernama PlayVideoAndWait
            yield return StartCoroutine(PlayVideoAndWait(winnerClip));
        }

        // 2. Bagian untuk memutar video yang kalah (loserClip) sudah DIHAPUS.

        // 3. Langsung cek Game Over atau lanjut ke tahap berikutnya
        if (CheckGameOver())
        {
            // State GameOver akan diatur oleh fungsi CheckGameOver()
        }
        else
        {
            Debug.Log("Ronde selesai, menunggu untuk melanjutkan...");
            ChangeState(GameState.WaitForContinue);
        }
    }


    


    private void UpdateHealthUI()
    {
        DebugLog($"Updating health UI - P1: {p1Lives}, P2: {p2Lives}");

        // Update Player 1 hearts
        if (p1HeartSprites != null)
        {
            for (int i = 0; i < p1HeartSprites.Length; i++)
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
            for (int i = 0; i < p2HeartSprites.Length; i++)
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
    p1Ready = false;
    p2Ready = false;
    roundActive = false;
    bellRang = false;
    
    if (sequenceManager != null)
    {
        sequenceManager.ClearSequences();
    }

    // Perubahan Kunci: Pindah ke state 'WaitingForReady' untuk menampilkan prompt lagi
    ChangeState(GameState.WaitingForReady);
    Debug.Log("Ronde di-reset. Menunggu pemain siap...");
}

// Di dalam GameManager.cs
private bool CheckGameOver()
{
    if (p1Lives <= 0 || p2Lives <= 0)
    {
        // Jika salah satu pemain kalah, langsung ubah state ke GameOver
        ChangeState(GameState.GameOver);
        return true; // Kembalikan nilai true yang menandakan game sudah berakhir
    }
    return false; // Kembalikan nilai false jika game belum berakhir
}
    
    // Public functions untuk UI buttons
    public void RestartGame()
    {
        DebugLog("Restarting game");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void ShowPauseMenu()
    {
        DebugLog("Showing pause menu");
        if (pauseMenu != null) 
        {
            pauseMenu.SetActive(true);
            Time.timeScale = 0f; // Pause game
        }
    }
    
    public void HidePauseMenu()
    {
        DebugLog("Hiding pause menu");
        if (pauseMenu != null) 
        {
            pauseMenu.SetActive(false);
            Time.timeScale = 1f; // Resume game
        }
    }
    
    public void ExitToMainMenu()
    {
        DebugLog("Exiting to main menu");
        Time.timeScale = 1f; // Reset time scale
        // Ganti "MainMenu" dengan nama scene main menu Anda
        SceneManager.LoadScene("MainMenu");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[GameManager] {message}");
        }
    }
    #endregion
}