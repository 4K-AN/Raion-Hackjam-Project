using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System.Linq; // Diperlukan untuk OrderBy

// "Wadah" untuk pasangan Key dan Sprite. Letakkan di luar kelas utama.
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
    public Transform p1SequenceContainer; // Parent untuk ikon-ikon P1
    public Transform p2SequenceContainer; // Parent untuk ikon-ikon P2
    public GameObject keyIconPrefab;      // Prefab untuk ikon tombol
    
    [Header("Pengaturan Tombol & Sprite")]
    // Ini adalah dua daftar yang seharusnya muncul di Inspector
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
        foreach (var pair in player1KeyMap)
        {
            if (!keySpriteMap.ContainsKey(pair.key))
                keySpriteMap.Add(pair.key, pair.sprite);
        }
        foreach (var pair in player2KeyMap)
        {
            if (!keySpriteMap.ContainsKey(pair.key))
                keySpriteMap.Add(pair.key, pair.sprite);
        }
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

    public bool ProcessInput(int playerID, Key key)
    {
        if (playerID == 1 && p1Active)
        {
            if (key == p1Sequence[p1Index])
            {
                p1Icons[p1Index].color = correctColor;
                p1Index++;
                if (p1Index >= sequenceLength) WinClash(1);
            }
            else
            {
                p1Icons[p1Index].color = incorrectColor;
                FailSequence(1);
            }
            return true;
        }
        else if (playerID == 2 && p2Active)
        {
            if (key == p2Sequence[p2Index])
            {
                p2Icons[p2Index].color = correctColor;
                p2Index++;
                if (p2Index >= sequenceLength) WinClash(2);
            }
            else
            {
                p2Icons[p2Index].color = incorrectColor;
                FailSequence(2);
            }
            return true;
        }
        return false;
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
        if(iconList != null) iconList.Clear(); else iconList = new List<Image>();s

        foreach (Key k in sequence)
        {
            GameObject iconGO = Instantiate(keyIconPrefab, container);
            Image img = iconGO.GetComponent<Image>();
            if (keySpriteMap.TryGetValue(k, out Sprite sp))
{
    img.sprite = sp; // <-- TAMBAHKAN TITIK KOMA DI SINI
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