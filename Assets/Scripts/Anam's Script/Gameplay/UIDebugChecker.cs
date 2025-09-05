using UnityEngine;

public class UIDebugChecker : MonoBehaviour
{
    [Header("Debug Buttons")]
    public bool checkUIAssignments;
    public bool forceShowHearts;
    public bool testSequenceUI;
    
    void Update()
    {
        // Check assignments
        if (checkUIAssignments)
        {
            checkUIAssignments = false;
            CheckUIAssignments();
        }
        
        // Force show hearts
        if (forceShowHearts)
        {
            forceShowHearts = false;
            ForceShowHearts();
        }
        
        // Test sequence UI
        if (testSequenceUI)
        {
            testSequenceUI = false;
            TestSequenceUI();
        }
    }
    [ContextMenu("Check All UI Assignments")]
    public void CheckUIAssignments()
    {
        GameManager gm = GetComponent<GameManager>();
        if (gm == null) 
        {
            Debug.LogError("GameManager not found!");
            return;
        }

        Debug.Log("=== CHECKING UI ASSIGNMENTS ===");
        
        // Check Heart Sprites
        Debug.Log("--- Player 1 Hearts ---");
        if (gm.p1HeartSprites != null)
        {
            for (int i = 0; i < gm.p1HeartSprites.Length; i++)
            {
                if (gm.p1HeartSprites[i] != null)
                    Debug.Log($"P1 Heart {i}: {gm.p1HeartSprites[i].name} - Active: {gm.p1HeartSprites[i].activeInHierarchy}");
                else
                    Debug.LogError($"P1 Heart {i}: NULL!");
            }
        }
        else
        {
            Debug.LogError("p1HeartSprites array is null!");
        }

        Debug.Log("--- Player 2 Hearts ---");
        if (gm.p2HeartSprites != null)
        {
            for (int i = 0; i < gm.p2HeartSprites.Length; i++)
            {
                if (gm.p2HeartSprites[i] != null)
                    Debug.Log($"P2 Heart {i}: {gm.p2HeartSprites[i].name} - Active: {gm.p2HeartSprites[i].activeInHierarchy}");
                else
                    Debug.LogError($"P2 Heart {i}: NULL!");
            }
        }
        else
        {
            Debug.LogError("p2HeartSprites array is null!");
        }

        // Check Sequence UI
        Debug.Log("--- Sequence UI ---");
        if (gm.sequenceUIPanel != null)
            Debug.Log($"SequenceUIPanel: {gm.sequenceUIPanel.name} - Active: {gm.sequenceUIPanel.activeInHierarchy}");
        else
            Debug.LogError("sequenceUIPanel is NULL!");

        // Check SequenceManager
        SequenceManager sm = FindObjectOfType<SequenceManager>();
        if (sm != null)
        {
            Debug.Log("--- SequenceManager ---");
            if (sm.p1SequenceContainer != null)
                Debug.Log($"P1 Container: {sm.p1SequenceContainer.name}");
            else
                Debug.LogError("p1SequenceContainer is NULL!");
                
            if (sm.p2SequenceContainer != null)
                Debug.Log($"P2 Container: {sm.p2SequenceContainer.name}");
            else
                Debug.LogError("p2SequenceContainer is NULL!");
                
            if (sm.keyIconPrefab != null)
                Debug.Log($"KeyIconPrefab: {sm.keyIconPrefab.name}");
            else
                Debug.LogError("keyIconPrefab is NULL!");

            Debug.Log($"Player1 KeyMap count: {(sm.player1KeyMap != null ? sm.player1KeyMap.Count : 0)}");
            Debug.Log($"Player2 KeyMap count: {(sm.player2KeyMap != null ? sm.player2KeyMap.Count : 0)}");
        }
        else
        {
            Debug.LogError("SequenceManager not found!");
        }

        Debug.Log("=== ASSIGNMENT CHECK COMPLETE ===");
    }

    [ContextMenu("Force Show Hearts")]
    public void ForceShowHearts()
    {
        GameManager gm = GetComponent<GameManager>();
        if (gm == null) return;

        Debug.Log("Force showing all hearts...");
        
        if (gm.p1HeartSprites != null)
        {
            foreach (var heart in gm.p1HeartSprites)
            {
                if (heart != null) 
                {
                    heart.SetActive(true);
                    Debug.Log($"Activated: {heart.name}");
                }
            }
        }

        if (gm.p2HeartSprites != null)
        {
            foreach (var heart in gm.p2HeartSprites)
            {
                if (heart != null) 
                {
                    heart.SetActive(true);
                    Debug.Log($"Activated: {heart.name}");
                }
            }
        }
    }

    [ContextMenu("Test Sequence UI")]
    public void TestSequenceUI()
    {
        SequenceManager sm = FindObjectOfType<SequenceManager>();
        if (sm != null)
        {
            Debug.Log("Testing sequence UI generation...");
            
            // Activate sequence panel
            GameManager gm = GetComponent<GameManager>();
            if (gm != null && gm.sequenceUIPanel != null)
            {
                gm.sequenceUIPanel.SetActive(true);
                Debug.Log("SequenceUIPanel activated");
            }
            
            // Generate sequences
            sm.StartClashP1();
            sm.StartClashP2();
            
            Debug.Log("Sequence generation test completed");
        }
    }
}