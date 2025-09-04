using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System;

public class GameManager : MonoBehaviour
{
    // State machine untuk mengatur alur permainan
    public enum GameState
    {
        Intro,
        WaitingForReady,
        HighNoonWait,
        Clash,
        RoundOver,
        WaitForContinue,
        GameOver
    }
    private GameState currentState;

    [Header("Game Settings")]
    public int startingLives = 3;
    public float minHighNoonDelay = 2f;
    public float maxHighNoonDelay = 5f;

    [Header("UI Elements")]
    public TMP_Text stateText;
    public TMP_Text p1Health;
    public TMP_Text p2Health;
    public TMP_Text p1ButtonHelp; // Sekarang tidak dipakai, tapi disimpan jika perlu
    public TMP_Text p2ButtonHelp; // Sekarang tidak dipakai, tapi disimpan jika perlu
    public GameObject gameOverPanel;
    public GameObject replayButton; // Tombol untuk replay

    [Header("Managers & Cutscenes")]
    public CutsceneManager cutsceneManager;
    public SequenceManager sequenceManager;

    [Header("Visuals - Videos")]
    public VideoClip playerMeetClip;
    public VideoClip faceOffClip;
    public VideoClip earlyShotClip;
    public VideoClip p1WinClip;
    public VideoClip p2WinClip;
    public VideoClip sequenceFailClip;
    public VideoClip gameOverClip;

    [Header("Visuals - Sprites")]
    public Sprite readyPromptSprite; // Gambar 3
    public Sprite highNoonWaitSprite; // Gambar 4
    public Sprite highNoonShootSprite; // Gambar 5
    public Sprite continuePromptSprite;

    // Variabel internal
    private int p1Lives;
    private int p2Lives;
    private bool p1Ready;
    private bool p2Ready;
    private bool bellRang;
    private bool roundActive;

    // --- FUNGSI UTAMA UNITY ---

    private void Awake()
    {
        if (sequenceManager == null) sequenceManager = FindObjectOfType<SequenceManager>();
        if (cutsceneManager == null) cutsceneManager = FindObjectOfType<CutsceneManager>();
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
        p1Lives = startingLives;
        p2Lives = startingLives;
        UpdateHealthUI();
        if(replayButton != null) replayButton.SetActive(false);
        ChangeState(GameState.Intro); // Memulai permainan dari Intro
    }

    private void Update()
    {
        // Hanya state yang membutuhkan input keyboard berkelanjutan yang ditaruh di sini
        if (currentState == GameState.WaitForContinue)
        {
            if (Input.GetKeyDown(KeyCode.Q) && Input.GetKeyDown(KeyCode.P))
            {
                if (!CheckGameOver())
                {
                    ResetRound();
                }
            }
        }
    }

    // --- PENGATUR STATE MACHINE ---

    private void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log("Game State changed to: " + newState);

        switch (currentState)
        {
            case GameState.Intro:
                StartCoroutine(IntroSequence());
                break;
            case GameState.WaitingForReady:
                cutsceneManager.PlaySingleImage(readyPromptSprite, 999f);
                stateText.text = "PRESS Q AND P TO READY";
                break;
            case GameState.HighNoonWait:
                StartCoroutine(HighNoonCountdown());
                break;
            case GameState.Clash:
                cutsceneManager.PlaySingleImage(highNoonShootSprite, 999f);
                sequenceManager.StartClashP1(); // Disederhanakan, bisa juga StartClash()
                sequenceManager.StartClashP2();
                stateText.text = "SEQUENCE!";
                break;
            case GameState.WaitForContinue:
                cutsceneManager.PlaySingleImage(continuePromptSprite, 999f);
                stateText.text = "Press Q and P to continue";
                break;
            case GameState.GameOver:
                VideoClip finalClip = (p1Lives > 0) ? p1WinClip : p2WinClip; // Menggunakan p1/p2WinClip sebagai contoh
                StartCoroutine(PlayCutsceneAndThen(finalClip, () => {
                    if (replayButton != null) replayButton.SetActive(true);
                }));
                break;
        }
    }
    
    // --- ALUR PERMAINAN (COROUTINES) ---

    private IEnumerator IntroSequence()
    {
        stateText.text = "";
        bool videoDone = false;
        Action onVideoFinish = () => videoDone = true;

        // Langkah 1: Video Player Bertemu
        cutsceneManager.OnCutsceneFinished += onVideoFinish;
        cutsceneManager.PlaySingleClip(playerMeetClip);
        yield return new WaitUntil(() => videoDone);
        cutsceneManager.OnCutsceneFinished -= onVideoFinish;

        // Langkah 2: Video Face Off
        videoDone = false;
        cutsceneManager.OnCutsceneFinished += onVideoFinish;
        cutsceneManager.PlaySingleClip(faceOffClip);
        yield return new WaitUntil(() => videoDone);
        cutsceneManager.OnCutsceneFinished -= onVideoFinish;

        // Langkah 3: Lanjut ke state berikutnya
        ChangeState(GameState.WaitingForReady);
    }

    private IEnumerator HighNoonCountdown()
    {
        roundActive = true;
        bellRang = false;
        
        // Langkah 4: Gambar High Noon Wait
        stateText.text = "GET READY...";
        bool imageDone = false;
        Action onImageFinish = () => imageDone = true;
        float randomDelay = UnityEngine.Random.Range(minHighNoonDelay, maxHighNoonDelay);
        
        cutsceneManager.OnCutsceneFinished += onImageFinish;
        cutsceneManager.PlaySingleImage(highNoonWaitSprite, randomDelay);
        yield return new WaitUntil(() => imageDone);
        cutsceneManager.OnCutsceneFinished -= onImageFinish;

        // Langkah 5: Waktunya Duel (Clash)
        bellRang = true;
        ChangeState(GameState.Clash);
    }

    // --- PENGELOLA INPUT (EVENT HANDLERS) ---

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

    private void HandleShoot(int playerID)
    {
        // Logika menembak (Clash) sekarang diatur oleh SequenceManager
        // Namun, kita tetap butuh ini untuk penalti menembak terlalu cepat
        if (!roundActive) return;

        if (!bellRang)
        {
            if (playerID == 1) p1Lives--; else p2Lives--;
            UpdateHealthUI();
            stateText.text = $"Player {playerID} shot too early! (-1 life)";
            StartCoroutine(PlayCutsceneAndThen(earlyShotClip, () => {
                if (!CheckGameOver()) ResetRound();
            }));
        }
    }

    private bool HandleSequence(int playerID, Key key)
    {
        if (currentState == GameState.Clash)
        {
            return sequenceManager.ProcessInput(playerID, key);
        }
        return false;
    }

    private void HandleClashWin(int winnerID)
    {
        if (currentState != GameState.Clash) return;
        
        currentState = GameState.RoundOver; // Tandai ronde selesai
        VideoClip winClip = null;

        if (winnerID == 1)
        {
            p2Lives--;
            winClip = p1WinClip; // Video 6/7
        }
        else if (winnerID == 2)
        {
            p1Lives--;
            winClip = p2WinClip; // Video 6/7
        }

        UpdateHealthUI();
        StartCoroutine(PlayCutsceneAndThen(winClip, () => {
             // Langkah 8: Setelah video, tunggu input Q & P
            if (!CheckGameOver())
            {
                ChangeState(GameState.WaitForContinue);
            }
        }));
    }

    private void HandleSequenceFailure(int playerID)
    {
       if (currentState != GameState.Clash) return;

        currentState = GameState.RoundOver;
        if (playerID == 1) p1Lives--; else p2Lives--;
        UpdateHealthUI();

        StartCoroutine(PlayCutsceneAndThen(sequenceFailClip, () => {
            if (!CheckGameOver())
            {
                ChangeState(GameState.WaitForContinue);
            }
        }));
    }

    // --- FUNGSI BANTUAN ---

    private void UpdateHealthUI()
    {
        // Ganti ini dengan logika sprite hati Anda
        if (p1Health != null) p1Health.text = $"Health: {p1Lives}";
        if (p2Health != null) p2Health.text = $"Health: {p2Lives}";
    }

    private void ResetRound()
    {
        p1Ready = p2Ready = false;
        roundActive = false;
        bellRang = false;
        sequenceManager.ClearSequences();
        ChangeState(GameState.HighNoonWait); // Kembali ke hitung mundur untuk ronde baru
    }

    private bool CheckGameOver()
    {
        if (p1Lives <= 0 || p2Lives <= 0)
        {
            ChangeState(GameState.GameOver);
            return true;
        }
        return false;
    }
    
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
    
    public void RestartGame()
    {
        // Langkah 10: Tombol Replay
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}