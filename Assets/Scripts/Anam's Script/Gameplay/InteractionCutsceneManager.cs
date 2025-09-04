using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class InteractionCutsceneManager : MonoBehaviour
{
    [System.Serializable]
    public class InteractiveSequenceStep
    {
        [Header("Step Configuration")]
        public string stepName = "Interactive Step";
        
        [Header("Pre-Interaction Video (Optional)")]
        public VideoClip preInteractionVideo;
        
        [Header("Interaction Type")]
        public InteractionType interactionType;
        
        [Header("Simple Interaction (P+Q)")]
        public KeyCode[] requiredKeys = { KeyCode.P, KeyCode.Q };
        
        [Header("Minigame Settings")]
        public Vector2 bellDelayRange = new Vector2(2f, 5f);
        public AudioClip bellSound;
        [Range(1, 10)]
        public int sequenceLength = 5;
        public float keyPressTimeLimit = 2f;
        
        [Header("Result Videos")]
        public VideoClip playerAWinVideo;
        public VideoClip playerBWinVideo;
        public VideoClip timeoutVideo;
    }

    public enum InteractionType
    {
        SimpleKeyPress,
        KeySequenceRace
    }

    [Header("STEP 1: DRAG REFERENCES FROM SCENE")]
    [Tooltip("Drag CutsceneManager dari scene")]
    public CutsceneManager cutsceneManager;
    
    [Tooltip("Drag AudioSource component (bisa dari GameObject ini)")]
    public AudioSource audioSource;
    
    [Header("STEP 2: DRAG UI ELEMENTS")]
    [Tooltip("Panel untuk interaction UI")]
    public GameObject interactionUI;
    
    [Tooltip("Text untuk instruksi (misal: 'Press P+Q')")]
    public Text instructionText;
    
    [Tooltip("Image untuk menampilkan sprite key")]
    public Image currentKeyImage;
    
    [Tooltip("Text untuk score Player A")]
    public Text playerAScoreText;
    
    [Tooltip("Text untuk score Player B")]
    public Text playerBScoreText;
    
    [Tooltip("Text untuk countdown timer")]
    public Text countdownText;

    [Header("STEP 3: ASSIGN KEY SPRITES (OPTIONAL - bisa diisi nanti)")]
    public Sprite spriteQ, spriteW, spriteE, spriteA, spriteS, spriteD, spriteZ, spriteX, spriteC;
    public Sprite spriteP, spriteO, spriteI, spriteL, spriteK, spriteJ, spriteComma, spriteM, spriteN;

    // Internal key mappings (auto-generated from sprites above)
    private Dictionary<KeyCode, Sprite> keySprites;

    // Runtime variables
    private InteractiveSequenceStep currentStep;
    private bool isWaitingForInteraction = false;
    private bool isMinigameActive = false;
    private List<KeyCode> currentSequence;
    private int playerAProgress = 0;
    private int playerBProgress = 0;
    private Coroutine activeCoroutine;

    // Events
    public Action<bool> OnInteractionComplete;

    void Start()
    {
        InitializeKeySprites();
        
        // Hide interaction UI at start
        if (interactionUI != null)
            interactionUI.SetActive(false);
            
        // Auto-assign AudioSource if not set
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        Debug.Log("[InteractionManager] Initialized. Ready for interactions.");
    }

    void InitializeKeySprites()
    {
        keySprites = new Dictionary<KeyCode, Sprite>();
        
        // Player A keys
        if (spriteQ != null) keySprites[KeyCode.Q] = spriteQ;
        if (spriteW != null) keySprites[KeyCode.W] = spriteW;
        if (spriteE != null) keySprites[KeyCode.E] = spriteE;
        if (spriteA != null) keySprites[KeyCode.A] = spriteA;
        if (spriteS != null) keySprites[KeyCode.S] = spriteS;
        if (spriteD != null) keySprites[KeyCode.D] = spriteD;
        if (spriteZ != null) keySprites[KeyCode.Z] = spriteZ;
        if (spriteX != null) keySprites[KeyCode.X] = spriteX;
        if (spriteC != null) keySprites[KeyCode.C] = spriteC;
        
        // Player B keys
        if (spriteP != null) keySprites[KeyCode.P] = spriteP;
        if (spriteO != null) keySprites[KeyCode.O] = spriteO;
        if (spriteI != null) keySprites[KeyCode.I] = spriteI;
        if (spriteL != null) keySprites[KeyCode.L] = spriteL;
        if (spriteK != null) keySprites[KeyCode.K] = spriteK;
        if (spriteJ != null) keySprites[KeyCode.J] = spriteJ;
        if (spriteComma != null) keySprites[KeyCode.Comma] = spriteComma;
        if (spriteM != null) keySprites[KeyCode.M] = spriteM;
        if (spriteN != null) keySprites[KeyCode.N] = spriteN;
    }

    void Update()
    {
        if (isWaitingForInteraction)
        {
            HandleSimpleInteraction();
        }
        else if (isMinigameActive)
        {
            HandleMinigameInput();
        }
    }

    // ========================
    // PUBLIC API - EASY TO USE
    // ========================
    
    public void StartInteractiveSequence(InteractiveSequenceStep step)
    {
        currentStep = step;
        
        Debug.Log($"[InteractionManager] Starting: {step.stepName}");
        
        // Play pre-video if available
        if (step.preInteractionVideo != null && cutsceneManager != null)
        {
            cutsceneManager.PlaySingleClip(step.preInteractionVideo);
            cutsceneManager.OnCutsceneFinished += OnPreVideoComplete;
        }
        else
        {
            StartInteraction();
        }
    }

    void OnPreVideoComplete()
    {
        if (cutsceneManager != null)
            cutsceneManager.OnCutsceneFinished -= OnPreVideoComplete;
        StartInteraction();
    }

    void StartInteraction()
    {
        ShowUI(true);
        
        if (currentStep.interactionType == InteractionType.SimpleKeyPress)
        {
            StartSimpleInteraction();
        }
        else
        {
            StartMinigame();
        }
    }

    // ========================
    // SIMPLE INTERACTION (P+Q)
    // ========================
    
    void StartSimpleInteraction()
    {
        isWaitingForInteraction = true;
        
        string keyText = string.Join(" + ", currentStep.requiredKeys);
        SetInstructionText($"Press {keyText} together to continue");
        
        Debug.Log($"[InteractionManager] Waiting for: {keyText}");
    }

    void HandleSimpleInteraction()
    {
        bool allPressed = true;
        
        foreach (KeyCode key in currentStep.requiredKeys)
        {
            if (!Input.GetKey(key))
            {
                allPressed = false;
                break;
            }
        }
        
        if (allPressed)
        {
            Debug.Log("[InteractionManager] Keys pressed! Continuing...");
            FinishInteraction(true);
        }
    }

    // ========================
    // MINIGAME
    // ========================
    
    void StartMinigame()
    {
        SetInstructionText("Get ready for the race...");
        playerAProgress = 0;
        playerBProgress = 0;
        UpdateScores();
        
        GenerateSequence();
        
        float delay = UnityEngine.Random.Range(currentStep.bellDelayRange.x, currentStep.bellDelayRange.y);
        activeCoroutine = StartCoroutine(MinigameSequence(delay));
    }

    void GenerateSequence()
    {
        currentSequence = new List<KeyCode>();
        
        // All possible keys
        KeyCode[] allKeys = { 
            KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.Z, KeyCode.X, KeyCode.C,
            KeyCode.P, KeyCode.O, KeyCode.I, KeyCode.L, KeyCode.K, KeyCode.J, KeyCode.Comma, KeyCode.M, KeyCode.N
        };
        
        for (int i = 0; i < currentStep.sequenceLength; i++)
        {
            KeyCode randomKey = allKeys[UnityEngine.Random.Range(0, allKeys.Length)];
            currentSequence.Add(randomKey);
        }
        
        Debug.Log($"[InteractionManager] Sequence: {string.Join(", ", currentSequence)}");
    }

    IEnumerator MinigameSequence(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Play bell
        if (currentStep.bellSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(currentStep.bellSound);
        }
        
        SetInstructionText("GO! Race to press the keys!");
        isMinigameActive = true;
        
        ShowCurrentKey();
    }

    void ShowCurrentKey()
    {
        int maxProgress = Mathf.Max(playerAProgress, playerBProgress);
        
        if (maxProgress >= currentSequence.Count)
        {
            EndMinigame();
            return;
        }
        
        KeyCode currentKey = currentSequence[maxProgress];
        
        // Show key sprite if available
        if (keySprites.ContainsKey(currentKey) && currentKeyImage != null)
        {
            currentKeyImage.sprite = keySprites[currentKey];
            currentKeyImage.gameObject.SetActive(true);
        }
        
        SetInstructionText($"Press: {currentKey} ({maxProgress + 1}/{currentSequence.Count})");
        
        // Start timeout
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(KeyTimeout());
    }

    IEnumerator KeyTimeout()
    {
        float timeLeft = currentStep.keyPressTimeLimit;
        
        while (timeLeft > 0)
        {
            SetCountdownText($"Time: {timeLeft:F1}s");
            timeLeft -= Time.deltaTime;
            yield return null;
        }
        
        EndMinigame(); // Timeout
    }

    void HandleMinigameInput()
    {
        int currentRound = Mathf.Max(playerAProgress, playerBProgress);
        if (currentRound >= currentSequence.Count) return;
        
        KeyCode targetKey = currentSequence[currentRound];
        
        // Player A input
        if (playerAProgress == currentRound && IsPlayerAKey(targetKey) && Input.GetKeyDown(targetKey))
        {
            playerAProgress++;
            UpdateScores();
            
            if (playerAProgress >= currentSequence.Count)
            {
                PlayResultVideo(currentStep.playerAWinVideo);
                return;
            }
        }
        
        // Player B input
        if (playerBProgress == currentRound && IsPlayerBKey(targetKey) && Input.GetKeyDown(targetKey))
        {
            playerBProgress++;
            UpdateScores();
            
            if (playerBProgress >= currentSequence.Count)
            {
                PlayResultVideo(currentStep.playerBWinVideo);
                return;
            }
        }
        
        ShowCurrentKey();
    }

    void EndMinigame()
    {
        isMinigameActive = false;
        
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        
        // Determine winner
        if (playerAProgress > playerBProgress)
        {
            PlayResultVideo(currentStep.playerAWinVideo);
        }
        else if (playerBProgress > playerAProgress)
        {
            PlayResultVideo(currentStep.playerBWinVideo);
        }
        else
        {
            PlayResultVideo(currentStep.timeoutVideo);
        }
    }

    void PlayResultVideo(VideoClip video)
    {
        ShowUI(false);
        
        if (video != null && cutsceneManager != null)
        {
            cutsceneManager.PlaySingleClip(video);
            cutsceneManager.OnCutsceneFinished += OnResultVideoComplete;
        }
        else
        {
            FinishInteraction(true);
        }
    }

    void OnResultVideoComplete()
    {
        if (cutsceneManager != null)
            cutsceneManager.OnCutsceneFinished -= OnResultVideoComplete;
        FinishInteraction(true);
    }

    // ========================
    // HELPERS
    // ========================
    
    bool IsPlayerAKey(KeyCode key)
    {
        KeyCode[] playerAKeys = { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.Z, KeyCode.X, KeyCode.C };
        return System.Array.IndexOf(playerAKeys, key) >= 0;
    }
    
    bool IsPlayerBKey(KeyCode key)
    {
        KeyCode[] playerBKeys = { KeyCode.P, KeyCode.O, KeyCode.I, KeyCode.L, KeyCode.K, KeyCode.J, KeyCode.Comma, KeyCode.M, KeyCode.N };
        return System.Array.IndexOf(playerBKeys, key) >= 0;
    }

    void UpdateScores()
    {
        if (playerAScoreText != null)
            playerAScoreText.text = $"Player A: {playerAProgress}";
        if (playerBScoreText != null)
            playerBScoreText.text = $"Player B: {playerBProgress}";
    }

    void SetInstructionText(string text)
    {
        if (instructionText != null)
            instructionText.text = text;
        Debug.Log($"[InteractionManager] {text}");
    }

    void SetCountdownText(string text)
    {
        if (countdownText != null)
            countdownText.text = text;
    }

    void ShowUI(bool show)
    {
        if (interactionUI != null)
            interactionUI.SetActive(show);
            
        if (!show && currentKeyImage != null)
            currentKeyImage.gameObject.SetActive(false);
    }

    void FinishInteraction(bool success)
    {
        isWaitingForInteraction = false;
        isMinigameActive = false;
        ShowUI(false);
        
        Debug.Log($"[InteractionManager] Finished! Success: {success}");
        OnInteractionComplete?.Invoke(success);
    }
}