using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public int startingLives = 3;
    public float minHighNoonDelay = 2f;
    public float maxHighNoonDelay = 5f;
    
    [Header("UI Elements")]
    public TMP_Text stateText;
    public TMP_Text p1Health;
    public TMP_Text p2Health;
    public TMP_Text p1ButtonHelp;
    public TMP_Text p2ButtonHelp;
    public GameObject gameOverPanel;
    
    [Header("Cutscene / Video (assign in Inspector)")]
    public CutsceneManager cutsceneManager;         
    public VideoClip earlyShotClip;                 
    public VideoClip p1WinClip;                     
    public VideoClip p2WinClip;                    
    public VideoClip sequenceFailClip;             
    public VideoClip gameOverClip;                  

    private int p1Lives;
    private int p2Lives;
    private SequenceManager sequenceManager;
    private bool p1Ready;
    private bool p2Ready;
    private bool p1InClash;
    private bool p2InClash;
    private bool bellRang;
    private bool roundActive;

    private void Awake()
    {
        sequenceManager = FindObjectOfType<SequenceManager>();
        if (sequenceManager == null)
        {
            Debug.LogError("SequenceManager not found! Please add one to the scene.");
        }
        
        p1Lives = startingLives;
        p2Lives = startingLives;
        UpdateHealthUI();
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
        ResetToWaitingState();
    }

    private void UpdateHealthUI()
    {
        if (p1Health != null) p1Health.text = $"Health: {p1Lives}";
        if (p2Health != null) p2Health.text = $"Health: {p2Lives}";
    }

    private void HandleReady(int playerID)
    {
        if (roundActive) return;

        if (playerID == 1)
        {
            p1Ready = true;
            if (p1ButtonHelp != null) p1ButtonHelp.text = "Ready!";
            Debug.Log("Player 1 is ready");
        }
        else if (playerID == 2)
        {
            p2Ready = true;
            if (p2ButtonHelp != null) p2ButtonHelp.text = "Ready!";
            Debug.Log("Player 2 is ready");
        }

        if (p1Ready && p2Ready)
        {
            StartCoroutine(HighNoonCountdown());
        }
    }

    private IEnumerator HighNoonCountdown()
    {
        roundActive = true;
        p1InClash = p2InClash = false;
        bellRang = false;

        if (stateText != null) stateText.text = "Get Ready... (Wait for HIGH NOON!)";
        if (p1ButtonHelp != null) p1ButtonHelp.text = "Press W to Shoot";
        if (p2ButtonHelp != null) p2ButtonHelp.text = "Press O to Shoot";
        
        float delay = UnityEngine.Random.Range(minHighNoonDelay, maxHighNoonDelay);
        Debug.Log($"Waiting {delay:F2} seconds for HIGH NOON...");
        yield return new WaitForSeconds(delay);

        bellRang = true;
        if (stateText != null) stateText.text = "HIGH NOON! SHOOT!";
        Debug.Log("HIGH NOON! SHOOT!");
    }

    private void HandleShoot(int playerID)
    {
        if (!roundActive) return;
        
        if (!bellRang)
        {
            if (playerID == 1) p1Lives--;
            else if (playerID == 2) p2Lives--;

            UpdateHealthUI();
            if (stateText != null) stateText.text = $"Player {playerID} shot too early! (-1 life)";
            Debug.Log($"Player {playerID} shot too early! Penalty: -1 life");

            
            if (cutsceneManager != null && earlyShotClip != null)
            {
                StartCoroutine(PlayCutsceneAndThen(earlyShotClip, () =>
                {
                    if (!CheckGameOver()) ResetRound();
                }));
            }
            else
            {
                StartCoroutine(DelayedRoundReset(2f));
            }
            return;
        }

        // NORMAL flow when bellRang == true
        if (bellRang)
        {
            Debug.Log($"Player {playerID} shoots! Entering clash...");
            
            if (playerID == 1 && !p1InClash)
            {
                p1InClash = true;
                if (p1ButtonHelp != null) p1ButtonHelp.text = "Enter sequence!";
                sequenceManager.StartClashP1();
            }
            else if (playerID == 2 && !p2InClash)
            {
                p2InClash = true;
                if (p2ButtonHelp != null) p2ButtonHelp.text = "Enter sequence!";
                sequenceManager.StartClashP2();
            }

            if (stateText != null) stateText.text = "CLASH! Complete your sequence!";
        }
    }

    private bool HandleSequence(int playerID, Key key)
    {
        if (sequenceManager != null && (p1InClash || p2InClash))
        {
            return sequenceManager.ProcessInput(playerID, key);
        }
        return false;
    }
    
    private void HandleClashWin(int winnerID)
    {
        if (winnerID == 1)
        {
            p2Lives--;
            if (stateText != null) stateText.text = "Player 1 wins the clash!";
        }
        else if (winnerID == 2)
        {
            p1Lives--;
            if (stateText != null) stateText.text = "Player 2 wins the clash!";
        }
        
        UpdateHealthUI();
        Debug.Log($"Player {winnerID} wins the clash!");
        
       
        VideoClip winClip = (winnerID == 1) ? p1WinClip : p2WinClip;
        if (cutsceneManager != null && winClip != null)
        {
            StartCoroutine(PlayCutsceneAndThen(winClip, () =>
            {
                if (!CheckGameOver()) ResetRound();
            }));
        }
        else
        {
            StartCoroutine(DelayedRoundReset(2f));
        }
    }
    
    private void HandleSequenceFailure(int playerID)
    {
        if (playerID == 1)
        {
            p1Lives--;
            if (stateText != null) stateText.text = "Player 1 failed the sequence!";
        }
        else if (playerID == 2)
        {
            p2Lives--;
            if (stateText != null) stateText.text = "Player 2 failed the sequence!";
        }
        
        UpdateHealthUI();
        Debug.Log($"Player {playerID} failed the sequence!");
        
       
        if (cutsceneManager != null && sequenceFailClip != null)
        {
            StartCoroutine(PlayCutsceneAndThen(sequenceFailClip, () =>
            {
                if (!CheckGameOver()) ResetRound();
            }));
        }
        else
        {
            StartCoroutine(DelayedRoundReset(2f));
        }
    }

   
    private IEnumerator PlayCutsceneAndThen(VideoClip clip, Action after)
    {
        if (cutsceneManager == null || clip == null)
        {
            after?.Invoke();
            yield break;
        }

        bool done = false;
        void Handler() { done = true; }
        cutsceneManager.OnCutsceneFinished += Handler;

       
        bool prevRoundActive = roundActive;
        roundActive = false;

        
        cutsceneManager.PlaySingleClip(clip);

        
        float timeout = 30f; 
        float elapsed = 0f;
        while (!done && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        cutsceneManager.OnCutsceneFinished -= Handler;

        
        after?.Invoke();

        
    }

    private IEnumerator DelayedRoundReset(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (CheckGameOver()) yield break;
        
        ResetRound();
    }

    private bool CheckGameOver()
    {
        if (p1Lives <= 0)
        {
            if (stateText != null) stateText.text = "Player 2 WINS the duel!";
            EndGame();
            return true;
        }
        else if (p2Lives <= 0)
        {
            if (stateText != null) stateText.text = "Player 1 WINS the duel!";
            EndGame();
            return true;
        }
        return false;
    }

    private void ResetRound()
    {
        p1Ready = p2Ready = false;
        roundActive = false;
        bellRang = false;
        p1InClash = p2InClash = false;
       
        if (sequenceManager != null)
        {
            sequenceManager.ClearSequences();
        }

        ResetToWaitingState();
        Debug.Log("Round reset. Waiting for players...");
    }
    
    private void ResetToWaitingState()
    {
        if (stateText != null) stateText.text = "Press Ready to start the next round!";
        if (p1ButtonHelp != null) p1ButtonHelp.text = "Press Q to Ready";
        if (p2ButtonHelp != null) p2ButtonHelp.text = "Press P to Ready";
    }

    private void EndGame()
    {
        roundActive = false;
        Debug.Log("Game Over!");
        
      
        if (cutsceneManager != null && gameOverClip != null)
        {
            StartCoroutine(PlayCutsceneAndThen(gameOverClip, () =>
            {
                if (gameOverPanel != null) gameOverPanel.SetActive(true);
            }));
        }
        else
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }
    }

    public void RestartGame()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public bool IsRoundActive() => roundActive;
    public int GetPlayerLives(int playerID) => playerID == 1 ? p1Lives : p2Lives;
}
