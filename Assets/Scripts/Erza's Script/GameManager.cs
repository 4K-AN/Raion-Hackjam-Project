using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public int p1Lives = 3;
    public int p2Lives = 3;

    private SequenceManager sequenceManager;

    private bool p1Ready;
    private bool p2Ready;

    private bool p1InClash;
    private bool p2InClash;

    private bool bellRang;
    private bool roundActive;

    [Header("UI")]
    public TMP_Text stateText;
    public TMP_Text p1Health;
    public TMP_Text p2Health;
    public TMP_Text p1ButtonHelp;
    public TMP_Text p2ButtonHelp;
    public GameObject gameOverPanel;

    [Header("High Noon Timing")]
    public float minDelay = 2f;
    public float maxDelay = 5f;

    private void Awake()
    {
        sequenceManager = FindObjectOfType<SequenceManager>();
        UpdateHealthUI();
    }

    private void OnEnable()
    {
        QuickDrawInput.OnReady += HandleReady;
        QuickDrawInput.OnShoot += HandleShoot;
        QuickDrawInput.OnSequence += HandleSequence;
    }

    private void OnDisable()
    {
        QuickDrawInput.OnReady -= HandleReady;
        QuickDrawInput.OnShoot -= HandleShoot;
        QuickDrawInput.OnSequence -= HandleSequence;
    }

    private void Start()
    {
        stateText.text = "Press Ready to start!";
        p1ButtonHelp.text = "Press Q to Ready";
        p2ButtonHelp.text = "Press P to Ready";
        roundActive = false;
    }

    private void UpdateHealthUI()
    {
        p1Health.text = "Health: " + p1Lives;
        p2Health.text = "Health: " + p2Lives;
    }

    private void HandleReady(int playerID)
    {
        if (roundActive) return;

        if (playerID == 1)
        {
            p1Ready = true;
            p1ButtonHelp.text = "Ready!";
        }
        if (playerID == 2)
        {
            p2Ready = true;
            p2ButtonHelp.text = "Ready!";
        }

            Debug.Log($"Player {playerID} is ready");

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

        stateText.text = "Get Ready... (Wait till HIGH NOON)";
        p1ButtonHelp.text = "Press W to Shoot";
        p2ButtonHelp.text = "Press O to Shoot";
        float delay = Random.Range(minDelay, maxDelay);
        Debug.Log($"Waiting {delay:F2} seconds for HIGH NOON...");
        yield return new WaitForSeconds(delay);

        bellRang = true;
        stateText.text = "HIGH NOON! SHOOT!";
        
        Debug.Log("🔔 HIGH NOON! SHOOT!");
    }

    private void HandleShoot(int playerID)
    {
        if (!bellRang)
        {
            if (playerID == 1) p1Lives--;
            else p2Lives--;

            UpdateHealthUI();
            stateText.text = $"Player {playerID} shot too early!";
            Debug.Log($"Player {playerID} shot too early! Penalty -1 life");

            CheckGameOver();
            return;
        }

        if (bellRang)
        {
            Debug.Log($"Player {playerID} SHOOTS! Entering clash");
            if (playerID == 1) p1ButtonHelp.text = "";
            else p2ButtonHelp.text = "";

            if (playerID == 1 && !p1InClash)
            {
                p1InClash = true;
                sequenceManager.StartClashP1();
            }
            else if (playerID == 2 && !p2InClash)
            {
                p2InClash = true;
                sequenceManager.StartClashP2();
            }

            stateText.text = "CLASH!";
        }
    }

    private bool HandleSequence(int playerID, Key key)
    {
        if (p1InClash || p2InClash)
        {
            bool win = sequenceManager.ProcessInput(playerID, key);
            if (win)
            {
                if (playerID == 1) p2Lives--;
                else p1Lives--;

                UpdateHealthUI();
                stateText.text = $"Player {playerID} wins the clash!";
                Debug.Log($"Player {playerID} wins the clash!");

                CheckGameOver();

                if (p1Lives > 0 && p2Lives > 0)
                {
                    ResetRound();
                }
            }
            return win;
        }
        return false;
    }

    private void CheckGameOver()
    {
        if (p1Lives <= 0)
        {
            stateText.text = "Player 2 WINS!";
            roundActive = false;
            EndGame();
        }
        else if (p2Lives <= 0)
        {
            stateText.text = "Player 1 WINS!";
            roundActive = false;
            EndGame();
        }
    }

    private void ResetRound()
    {
        p1Ready = p2Ready = false;
        roundActive = false;
        bellRang = false;
        p1InClash = p2InClash = false;
        sequenceManager.ClearSequences();

        stateText.text = "Press Ready for next round!";
        p1ButtonHelp.text = "Press Q to Ready";
        p2ButtonHelp.text = "Press P to Ready";
        Debug.Log("Round reset. Waiting for players...");
    }

    private void EndGame()
    {
        QuickDrawInput.OnReady -= HandleReady;
        QuickDrawInput.OnShoot -= HandleShoot;
        QuickDrawInput.OnSequence -= HandleSequence;
        Debug.Log("Game Over!");
        gameOverPanel.SetActive(true);
    }

    public void RestartGame()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}