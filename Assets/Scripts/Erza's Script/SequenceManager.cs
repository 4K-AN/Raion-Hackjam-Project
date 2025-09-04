using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class SequenceManager : MonoBehaviour
{
    [Header("Sequence Settings")]
    [SerializeField] private int sequenceLength = 5;
    [SerializeField] private float inputTimeLimit = 10f; 
    
    [Header("UI Elements")]
    public TMP_Text p1SequenceList;
    public TMP_Text p2SequenceList;
    public TMP_Text p1Progress;
    public TMP_Text p2Progress;
    public Transform p1SequenceContainer;
    public Transform p2SequenceContainer;
    public GameObject keyIconPrefab;
    public List<KeySpritePair> keySpritePairs;

    private Dictionary<Key, Sprite> keySpriteMap;

    [Header("Visual Feedback")]
    public Color correctColor = Color.green;
    public Color incorrectColor = Color.red;
    public Color pendingColor = Color.white;
    
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
    
    
    private Key[] p1Keys = { Key.Q, Key.W, Key.E, Key.A, Key.S, Key.D, Key.Z, Key.X, Key.C };
    private Key[] p2Keys = { Key.I, Key.O, Key.P, Key.J, Key.K, Key.L, Key.N, Key.M, Key.Comma };
    
   
    public System.Action<int> OnPlayerWinsClash;
    public System.Action<int> OnPlayerFailsSequence;

    private void Awake()
    {
        keySpriteMap = new Dictionary<Key, Sprite>();
        foreach (var pair in keySpritePairs)
        {
            if (!keySpriteMap.ContainsKey(pair.key))
                keySpriteMap.Add(pair.key, pair.sprite);
        }
    }
    private void Update()
    {
        
        if (p1Active)
        {
            p1Timer -= Time.deltaTime;
            if (p1Timer <= 0f)
            {
                Debug.Log("Player 1 ran out of time!");
                OnPlayerFailsSequence?.Invoke(1);
                p1Active = false;
            }
        }
        
        if (p2Active)
        {
            p2Timer -= Time.deltaTime;
            if (p2Timer <= 0f)
            {
                Debug.Log("Player 2 ran out of time!");
                OnPlayerFailsSequence?.Invoke(2);
                p2Active = false;
            }
        }
    }

    public void StartClashP1()
    {
        p1Sequence = GenerateRandomSequence(p1Keys, sequenceLength);
        p1Index = 0;
        p1Timer = inputTimeLimit;
        p1Active = true;

        DisplaySequence(p1Sequence, p1SequenceContainer, ref p1Icons);
        UpdateSequenceDisplay(1);
        Debug.Log("Player 1 sequence: " + string.Join(", ", p1Sequence));
    }

    public void StartClashP2()
    {
        p2Sequence = GenerateRandomSequence(p2Keys, sequenceLength);
        p2Index = 0;
        p2Timer = inputTimeLimit;
        p2Active = true;

        DisplaySequence(p2Sequence, p2SequenceContainer, ref p2Icons);
        UpdateSequenceDisplay(2);
        Debug.Log("Player 2 sequence: " + string.Join(", ", p2Sequence));
    }

    public bool ProcessInput(int playerID, Key key)
    {
        if (playerID == 1 && p1Active)
        {
            return ProcessPlayerInput(ref p1Index, p1Sequence, p1Icons, key, 1);
        }
        else if (playerID == 2 && p2Active)
        {
            return ProcessPlayerInput(ref p2Index, p2Sequence, p2Icons, key, 2);
        }
        
        return false;
    }

    private bool ProcessPlayerInput(ref int index, List<Key> sequence, List<Image> icons, Key inputKey, int playerID)
    {
        if (index >= sequence.Count) return false;

        if (index == 0 && inputKey != sequence[0])
        {
        Debug.Log($"Player {playerID} pressed {inputKey}, waiting for {sequence[0]} to start sequence...");
        return false;
        }

        if (inputKey == sequence[index])
        {
            Debug.Log($"Player {playerID} correct input: {inputKey} ({index}/{sequence.Count})");
            
            UpdateProgressDisplay(playerID, index, sequence.Count);

            icons[index].color = correctColor;
            index++;
            if (index >= sequence.Count)
            {
                Debug.Log($"Player {playerID} menang clash!");
                if (playerID == 1) p1Active = false;
                else p2Active = false;
                return true;
            }

            if (index >= sequence.Count)
            {
                Debug.Log($"Player {playerID} wins the clash!");
                if (playerID == 1) p1Active = false;
                else p2Active = false;
                
                OnPlayerWinsClash?.Invoke(playerID);
                return true;
            }
        }
        else
        {
            Debug.Log($"Player {playerID} incorrect input: {inputKey} (expected: {sequence[index]})");
            icons[index].color = incorrectColor;
        }
        
        return false;
    }

    private void DisplaySequence(List<Key> sequence, Transform container, ref List<Image> iconList)
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);

        iconList = new List<Image>();

        foreach (Key k in sequence)
        {
            GameObject iconGO = Instantiate(keyIconPrefab, container);
            Image img = iconGO.GetComponent<Image>();

            if (keySpriteMap.TryGetValue(k, out Sprite sp))
            {
                img.sprite = sp;
                img.color = pendingColor;
                img.enabled = true;
            }
            else
            {
                img.enabled = false;
            }

            img.color = pendingColor; 
            iconList.Add(img);
        }
    }
    private List<Key> GenerateRandomSequence(Key[] keyPool, int length)
    {
        List<Key> sequence = new List<Key>();
        for (int i = 0; i < length; i++)
        {
            int randomIndex = Random.Range(0, keyPool.Length);
            sequence.Add(keyPool[randomIndex]);
        }
        return sequence;
    }
    
    private void UpdateSequenceDisplay(int playerID)
    {
        if (playerID == 1 && p1SequenceList != null)
        {
            p1SequenceList.text = string.Join(" ", p1Sequence);
        }
        else if (playerID == 2 && p2SequenceList != null)
        {
            p2SequenceList.text = string.Join(" ", p2Sequence);
        }
    }
    
    private void UpdateProgressDisplay(int playerID, int currentIndex, int totalLength)
    {
        string progressText = $"{currentIndex}/{totalLength}";
        
        if (playerID == 1 && p1Progress != null)
        {
            p1Progress.text = progressText;
        }
        else if (playerID == 2 && p2Progress != null)
        {
            p2Progress.text = progressText;
        }
    }

    public void ClearSequences()
    {
        p1Active = p2Active = false;
        p1Timer = p2Timer = 0f;
        
        if (p1SequenceList != null) p1SequenceList.text = "";
        if (p2SequenceList != null) p2SequenceList.text = "";
        if (p1Progress != null) p1Progress.text = "";
        if (p2Progress != null) p2Progress.text = "";

        if (p1SequenceContainer != null)
        {
            foreach (Transform child in p1SequenceContainer)
            {
                Destroy(child.gameObject);
            }
        }

        if (p2SequenceContainer != null)
        {
            foreach (Transform child in p2SequenceContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
    
   
    public bool IsPlayerActive(int playerID)
    {
        return playerID == 1 ? p1Active : p2Active;
    }
    
    public float GetPlayerTimeRemaining(int playerID)
    {
        return playerID == 1 ? p1Timer : p2Timer;
    }
}

[System.Serializable]
public class KeySpritePair
{
    public Key key;
    public Sprite sprite;
}