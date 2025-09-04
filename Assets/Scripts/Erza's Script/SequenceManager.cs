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
        foreach (var pair in player1KeyMap) { if (!keySpriteMap.ContainsKey(pair.key)) keySpriteMap.Add(pair.key, pair.sprite); }
        foreach (var pair in player2KeyMap) { if (!keySpriteMap.ContainsKey(pair.key)) keySpriteMap.Add(pair.key, pair.sprite); }
    }
    
    private void Update()
    {
        if (p1Active) { p1Timer -= Time.deltaTime; if (p1Timer <= 0f) { FailSequence(1); } }
        if (p2Active) { p2Timer -= Time.deltaTime; if (p2Timer <= 0f) { FailSequence(2); } }
    }

    public void StartClashP1()
    {
        p1Sequence = GenerateRandomSequence(player1KeyMap, sequenceLength);
        p1Index = 0;
        p1Timer = inputTimeLimit;
        p1Active = true;
        DisplaySequenceIcons(p1Sequence, p1SequenceContainer, ref p1Icons);
    }

    public void StartClashP2()
    {
        p2Sequence = GenerateRandomSequence(player2KeyMap, sequenceLength);
        p2Index = 0;
        p2Timer = inputTimeLimit;
        p2Active = true;
        DisplaySequenceIcons(p2Sequence, p2SequenceContainer, ref p2Icons);
    }

    // --- FUNGSI INI TELAH DIPERBAIKI ---
    public bool ProcessInput(int playerID, Key key)
    {
        if (playerID == 1 && p1Active)
        {
            if (key == p1Sequence[p1Index])
            {
                // Input benar
                p1Icons[p1Index].color = correctColor;
                p1Index++;
                if (p1Index >= sequenceLength) WinClash(1);
            }
            else
            {
                // Input salah, reset urutan untuk Player 1
                ResetPlayerSequence(1);
            }
            return true;
        }
        else if (playerID == 2 && p2Active)
        {
            if (key == p2Sequence[p2Index])
            {
                // Input benar
                p2Icons[p2Index].color = correctColor;
                p2Index++;
                if (p2Index >= sequenceLength) WinClash(2);
            }
            else
            {
                // Input salah, reset urutan untuk Player 2
                ResetPlayerSequence(2);
            }
            return true;
        }
        return false;
    }

    // --- FUNGSI BARU UNTUK MERESET URUTAN ---
    private void ResetPlayerSequence(int playerID)
    {
        if (playerID == 1)
        {
            p1Index = 0; // Kembalikan index ke awal
            foreach (var icon in p1Icons)
            {
                icon.color = pendingColor; // Kembalikan semua warna ikon ke normal
            }
        }
        else if (playerID == 2)
        {
            p2Index = 0;
            foreach (var icon in p2Icons)
            {
                icon.color = pendingColor;
            }
        }
    }

    private void WinClash(int playerID)
    {
        p1Active = false;
        p2Active = false;
        OnPlayerWinsClash?.Invoke(playerID);
    }

    private void FailSequence(int playerID)
    {
        p1Active = false;
        p2Active = false;
        OnPlayerFailsSequence?.Invoke(playerID);
    }

    private List<Key> GenerateRandomSequence(List<KeySpritePair> keyMap, int length)
    {
        var keyPool = keyMap.Select(pair => pair.key).ToList();
        return keyPool.OrderBy(x => System.Guid.NewGuid()).Take(length).ToList();
    }
    
    private void DisplaySequenceIcons(List<Key> sequence, Transform container, ref List<Image> iconList)
    {
        foreach (Transform child in container) Destroy(child.gameObject);
        if(iconList != null) iconList.Clear(); else iconList = new List<Image>();

        foreach (Key k in sequence)
        {
            GameObject iconGO = Instantiate(keyIconPrefab, container);
            Image img = iconGO.GetComponent<Image>();
            if (keySpriteMap.TryGetValue(k, out Sprite sp))
            {
                img.sprite = sp;
                img.color = pendingColor;
            }
            iconList.Add(img);
        }
    }

    public void ClearSequences()
    {
        p1Active = p2Active = false;
        if (p1SequenceContainer != null) foreach (Transform child in p1SequenceContainer) Destroy(child.gameObject);
        if (p2SequenceContainer != null) foreach (Transform child in p2SequenceContainer) Destroy(child.gameObject);
    }
}