using UnityEngine;
using UnityEngine.Video;

public class SimpleTestController : MonoBehaviour
{
    [Header("References - DRAG FROM SCENE")]
    public CutsceneManager cutsceneManager;
    public InteractionCutsceneManager interactionManager;
    
    [Header("Test Videos - DRAG FROM PROJECT")]
    public VideoClip normalVideo;
    public VideoClip playerAWinVideo;
    public VideoClip playerBWinVideo;
    
    [Header("Test Audio")]
    public AudioClip bellSound;

    void Start()
    {
        // Subscribe to events
        if (interactionManager != null)
        {
            interactionManager.OnInteractionComplete += OnInteractionFinished;
        }
        
        Debug.Log("[TestController] Ready! Press SPACE to start test");
    }

    void Update()
    {
        // Press SPACE to start test
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartTestSequence();
        }
        
        // Press R to reset
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetTest();
        }
    }

    void StartTestSequence()
    {
        Debug.Log("[TestController] Starting test sequence...");
        
        // Test 1: Simple P+Q interaction
        TestSimpleInteraction();
    }

    void TestSimpleInteraction()
    {
        Debug.Log("[TestController] Testing simple P+Q interaction");
        
        // Create simple interaction step
        var simpleStep = new InteractionCutsceneManager.InteractiveSequenceStep();
        simpleStep.stepName = "Test P+Q Interaction";
        simpleStep.interactionType = InteractionCutsceneManager.InteractionType.SimpleKeyPress;
        simpleStep.requiredKeys = new KeyCode[] { KeyCode.P, KeyCode.Q };
        
        // Optional: add pre-interaction video
        if (normalVideo != null)
        {
            simpleStep.preInteractionVideo = normalVideo;
        }
        
        interactionManager.StartInteractiveSequence(simpleStep);
    }

    void TestMinigame()
    {
        Debug.Log("[TestController] Testing minigame");
        
        // Create minigame step
        var minigameStep = new InteractionCutsceneManager.InteractiveSequenceStep();
        minigameStep.stepName = "Test Key Race";
        minigameStep.interactionType = InteractionCutsceneManager.InteractionType.KeySequenceRace;
        
        // Minigame settings
        minigameStep.bellDelayRange = new Vector2(1f, 3f); // 1-3 seconds delay
        minigameStep.bellSound = bellSound;
        minigameStep.sequenceLength = 3; // Start with 3 keys for testing
        minigameStep.keyPressTimeLimit = 3f; // 3 seconds per key
        
        // Result videos
        minigameStep.playerAWinVideo = playerAWinVideo;
        minigameStep.playerBWinVideo = playerBWinVideo;
        
        interactionManager.StartInteractiveSequence(minigameStep);
    }

    void OnInteractionFinished(bool success)
    {
        Debug.Log($"[TestController] Interaction finished! Success: {success}");
        
        // After simple interaction, test minigame
        if (success)
        {
            // Wait a bit then start minigame test
            Invoke(nameof(TestMinigame), 2f);
        }
    }

    void ResetTest()
    {
        Debug.Log("[TestController] Resetting test...");
        
        // Stop any ongoing interactions
        if (interactionManager != null)
        {
            // Reset UI
            interactionManager.transform.GetComponentInChildren<Canvas>()?.gameObject.SetActive(false);
        }
        
        // Stop cutscene
        if (cutsceneManager != null && cutsceneManager.videoPlayer != null)
        {
            cutsceneManager.videoPlayer.Stop();
        }
        
        Debug.Log("[TestController] Reset complete. Press SPACE to start again.");
    }

    void OnDestroy()
    {
        // Cleanup
        if (interactionManager != null)
        {
            interactionManager.OnInteractionComplete -= OnInteractionFinished;
        }
    }
}