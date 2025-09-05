using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Linq;

// "Wadah" untuk pasangan Key dan Sprite.
[System.Serializable]
public class KeySpritePair
{
    public Key key;
    public Sprite sprite;
}

public class SequenceManager : MonoBehaviour
{
    [Header("Pengaturan Urutan")]
    [SerializeField] private int sequenceLength = 5;
    [SerializeField] private float inputTimeLimit = 10f; 
    
    [Header("Elemen UI")]
    public Transform p1SequenceContainer;
    public Transform p2SequenceContainer;
    public GameObject keyIconPrefab;
    
    [Header("Pengaturan Tombol & Sprite")]
    public List<KeySpritePair> player1KeyMap;
    public List<KeySpritePair> player2KeyMap;

    [Header("Umpan Balik Visual")]
    public Color correctColor = Color.green;
    public Color incorrectColor = Color.red;
    public Color pendingColor = Color.white;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    // Event untuk GameManager
    public System.Action<int> OnPlayerWinsClash;
    public System.Action<int> OnPlayerFailsSequence;

    // Variabel Internal
    private Dictionary<Key, Sprite> keySpriteMap;
    private List<Key> p1Sequence;
    private List<Key> p2Sequence;
    private List<Image> p1Icons;
    private List<Image> p2Icons;
    private int p1Index;
    private int p2Index;
    private float p1Timer;
    private float p2Timer;
    private bool p1Active;
    private bool p2Active;

    private void Awake()
    {
        keySpriteMap = new Dictionary<Key, Sprite>();
        
        DebugLog("Setting up key sprite map...");
        
        // Setup key mapping dengan debug
        foreach (var pair in player1KeyMap) 
        { 
            if (!keySpriteMap.ContainsKey(pair.key)) 
            {
                keySpriteMap.Add(pair.key, pair.sprite);
                DebugLog($"Added P1 key mapping: {pair.key} -> {(pair.sprite != null ? pair.sprite.name : "NULL")}");
            }
        }
        
        foreach (var pair in player2KeyMap) 
        { 
            if (!keySpriteMap.ContainsKey(pair.key)) 
            {
                keySpriteMap.Add(pair.key, pair.sprite);
                DebugLog($"Added P2 key mapping: {pair.key} -> {(pair.sprite != null ? pair.sprite.name : "NULL")}");
            }
        }
        
        DebugLog($"Total key mappings: {keySpriteMap.Count}");
    }
    
    private void Update()
    {
        if (p1Active) { p1Timer -= Time.deltaTime; if (p1Timer <= 0f) { FailSequence(1); } }
        if (p2Active) { p2Timer -= Time.deltaTime; if (p2Timer <= 0f) { FailSequence(2); } }
    }

    public void StartClashP1()
    {
        DebugLog("StartClashP1 called");
        
        // Validasi komponen
        if (p1SequenceContainer == null)
        {
            DebugLog("ERROR: p1SequenceContainer is NULL!");
            return;
        }
        
        if (keyIconPrefab == null)
        {
            DebugLog("ERROR: keyIconPrefab is NULL!");
            return;
        }
        
        if (player1KeyMap == null || player1KeyMap.Count == 0)
        {
            DebugLog("ERROR: player1KeyMap is empty or null!");
            return;
        }
        
        p1Sequence = GenerateRandomSequence(player1KeyMap, sequenceLength);
        p1Index = 0;
        p1Timer = inputTimeLimit;
        p1Active = true;
        
        DebugLog($"P1 sequence generated: {string.Join(", ", p1Sequence)}");
        
        DisplaySequenceIcons(p1Sequence, p1SequenceContainer, ref p1Icons);
    }

    public void StartClashP2()
    {
        DebugLog("StartClashP2 called");
        
        // Validasi komponen
        if (p2SequenceContainer == null)
        {
            DebugLog("ERROR: p2SequenceContainer is NULL!");
            return;
        }
        
        if (keyIconPrefab == null)
        {
            DebugLog("ERROR: keyIconPrefab is NULL!");
            return;
        }
        
        if (player2KeyMap == null || player2KeyMap.Count == 0)
        {
            DebugLog("ERROR: player2KeyMap is empty or null!");
            return;
        }
        
        p2Sequence = GenerateRandomSequence(player2KeyMap, sequenceLength);
        p2Index = 0;
        p2Timer = inputTimeLimit;
        p2Active = true;
        
        DebugLog($"P2 sequence generated: {string.Join(", ", p2Sequence)}");
        
        DisplaySequenceIcons(p2Sequence, p2SequenceContainer, ref p2Icons);
    }

    // --- FUNGSI INI TELAH DIPERBAIKI ---
    public bool ProcessInput(int playerID, Key key)
    {
        DebugLog($"ProcessInput called - Player: {playerID}, Key: {key}");
        
        if (playerID == 1 && p1Active)
        {
            DebugLog($"P1 input processing - Expected: {p1Sequence[p1Index]}, Got: {key}, Index: {p1Index}");
            
            if (key == p1Sequence[p1Index])
            {
                // Input benar
                DebugLog($"P1 correct input at index {p1Index}");
                if (p1Icons != null && p1Index < p1Icons.Count)
                {
                    p1Icons[p1Index].color = correctColor;
                }
                p1Index++;
                if (p1Index >= sequenceLength) 
                {
                    DebugLog("P1 completed sequence!");
                    WinClash(1);
                }
            }
            else
            {
                // Input salah, reset urutan untuk Player 1
                DebugLog($"P1 incorrect input! Resetting sequence.");
                ResetPlayerSequence(1);
            }
            return true;
        }
        else if (playerID == 2 && p2Active)
        {
            DebugLog($"P2 input processing - Expected: {p2Sequence[p2Index]}, Got: {key}, Index: {p2Index}");
            
            if (key == p2Sequence[p2Index])
            {
                // Input benar
                DebugLog($"P2 correct input at index {p2Index}");
                if (p2Icons != null && p2Index < p2Icons.Count)
                {
                    p2Icons[p2Index].color = correctColor;
                }
                p2Index++;
                if (p2Index >= sequenceLength) 
                {
                    DebugLog("P2 completed sequence!");
                    WinClash(2);
                }
            }
            else
            {
                // Input salah, reset urutan untuk Player 2
                DebugLog($"P2 incorrect input! Resetting sequence.");
                ResetPlayerSequence(2);
            }
            return true;
        }
        
        DebugLog($"Input not processed - P1Active: {p1Active}, P2Active: {p2Active}");
        return false;
    }

    // --- FUNGSI BARU UNTUK MERESET URUTAN ---
    private void ResetPlayerSequence(int playerID)
    {
        DebugLog($"Resetting sequence for Player {playerID}");
        
        if (playerID == 1)
        {
            p1Index = 0; // Kembalikan index ke awal
            if (p1Icons != null)
            {
                foreach (var icon in p1Icons)
                {
                    if (icon != null) icon.color = pendingColor; // Kembalikan semua warna ikon ke normal
                }
            }
        }
        else if (playerID == 2)
        {
            p2Index = 0;
            if (p2Icons != null)
            {
                foreach (var icon in p2Icons)
                {
                    if (icon != null) icon.color = pendingColor;
                }
            }
        }
    }

    private void WinClash(int playerID)
    {
        DebugLog($"Player {playerID} wins clash!");
        p1Active = false;
        p2Active = false;
        OnPlayerWinsClash?.Invoke(playerID);
    }

    private void FailSequence(int playerID)
    {
        DebugLog($"Player {playerID} failed sequence (timeout)");
        p1Active = false;
        p2Active = false;
        OnPlayerFailsSequence?.Invoke(playerID);
    }

    private List<Key> GenerateRandomSequence(List<KeySpritePair> keyMap, int length)
    {
        DebugLog($"Generating random sequence of length {length} from {keyMap.Count} available keys");
        
        var keyPool = keyMap.Select(pair => pair.key).ToList();
        var sequence = keyPool.OrderBy(x => System.Guid.NewGuid()).Take(length).ToList();
        
        DebugLog($"Generated sequence: {string.Join(", ", sequence)}");
        
        return sequence;
    }
    
    private void DisplaySequenceIcons(List<Key> sequence, Transform container, ref List<Image> iconList)
    {
        DebugLog($"DisplaySequenceIcons called - Container: {(container != null ? container.name : "NULL")}, Sequence count: {sequence.Count}");
        
        // Clear existing icons
        if (container != null)
        {
            int childCount = container.childCount;
            DebugLog($"Clearing {childCount} existing children from container");
            
            foreach (Transform child in container) 
            {
                Destroy(child.gameObject);
            }
        }
        
        // Initialize icon list
        if(iconList != null) 
        {
            iconList.Clear(); 
        }
        else 
        {
            iconList = new List<Image>();
        }

        // Create new icons
        foreach (Key k in sequence)
        {
            DebugLog($"Creating icon for key: {k}");
            
            if (container == null)
            {
                DebugLog("ERROR: Container is null, cannot create icon!");
                continue;
            }
            
            if (keyIconPrefab == null)
            {
                DebugLog("ERROR: keyIconPrefab is null, cannot create icon!");
                continue;
            }
            
            GameObject iconGO = Instantiate(keyIconPrefab, container);
            DebugLog($"Icon GameObject created: {iconGO.name}");
            
            Image img = iconGO.GetComponent<Image>();
            if (img == null)
            {
                DebugLog("ERROR: No Image component found on icon prefab!");
                continue;
            }
            
            if (keySpriteMap.TryGetValue(k, out Sprite sp))
            {
                if (sp != null)
                {
                    img.sprite = sp;
                    img.color = pendingColor;
                    DebugLog($"Icon sprite set: {sp.name}");
                }
                else
                {
                    DebugLog($"ERROR: Sprite for key {k} is null!");
                }
            }
            else
            {
                DebugLog($"ERROR: No sprite mapping found for key {k}!");
            }
            
            iconList.Add(img);
        }
        
        DebugLog($"Created {iconList.Count} icons in container {container.name}");
        
        // Force layout rebuild
        if (container.GetComponent<LayoutGroup>() != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(container as RectTransform);
            DebugLog("Layout rebuilt");
        }
    }

    public void ClearSequences()
    {
        DebugLog("Clearing sequences");
        
        p1Active = p2Active = false;
        
        if (p1SequenceContainer != null) 
        {
            foreach (Transform child in p1SequenceContainer) 
                Destroy(child.gameObject);
        }
        
        if (p2SequenceContainer != null) 
        {
            foreach (Transform child in p2SequenceContainer) 
                Destroy(child.gameObject);
        }
        
        if (p1Icons != null) p1Icons.Clear();
        if (p2Icons != null) p2Icons.Clear();
    }
    
    // Debug method untuk testing manual
    [ContextMenu("Test Generate P1 Sequence")]
    public void TestGenerateP1Sequence()
    {
        DebugLog("Testing P1 sequence generation...");
        StartClashP1();
    }
    
    [ContextMenu("Test Generate P2 Sequence")]
    public void TestGenerateP2Sequence()
    {
        DebugLog("Testing P2 sequence generation...");
        StartClashP2();
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[SequenceManager] {message}");
        }
    }
}