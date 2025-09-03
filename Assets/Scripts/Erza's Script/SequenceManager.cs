using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SequenceManager : MonoBehaviour
{
    private List<Key> p1Sequence;
    private List<Key> p2Sequence;
    private int p1Index;
    private int p2Index;
    public TMP_Text p1SequenceList;
    public TMP_Text p2SequenceList;
    private Key[] p1Keys = { Key.Q, Key.W, Key.E, Key.A, Key.S, Key.D, Key.Z, Key.X, Key.C };
    private Key[] p2Keys = { Key.I, Key.O, Key.P, Key.J, Key.K, Key.L, Key.N, Key.M, Key.Comma };

    public void StartClashP1()
    {
        p1Sequence = GenerateRandomSeq(p1Keys, 5);
        p1Index = 0;

        p1SequenceList.text = string.Join(" ", p1Sequence);
    }

    public void StartClashP2()
    {
        p2Sequence = GenerateRandomSeq(p2Keys, 5);
        p2Index = 0;

        p2SequenceList.text = string.Join(" ", p2Sequence);
    }

    public bool ProcessInput(int playerID, Key key)
    {
        if (playerID == 1)
        {
            if (p1Index < p1Sequence.Count && key == p1Sequence[p1Index])
            {
                p1Index++;
                Debug.Log("Player 1 correct input: " + key);
                if (p1Index == p1Sequence.Count)
                {
                    Debug.Log("Player 1 wins the clash!");
                    return true;
                }
            }
            else
            {
                Debug.Log("Player 1 incorrect input: " + key);
            }
        }
        else if (playerID == 2)
        {
            if (p2Index < p2Sequence.Count && key == p2Sequence[p2Index])
            {
                p2Index++;
                Debug.Log("Player 2 correct input: " + key);
                if (p2Index == p2Sequence.Count)
                {
                    Debug.Log("Player 2 wins the clash!");
                    return true; 
                }
            }
            else
            {
                Debug.Log("Player 2 incorrect input: " + key);
            }
        }
        return false; 
    }
    private List<Key> GenerateRandomSeq(Key[] pool, int length)
    {
        List<Key> sequence = new List<Key>();
        for (int i = 0; i < length; i++)
        {
            int randomIndex = Random.Range(0, pool.Length);
            sequence.Add(pool[randomIndex]);
        }
        return sequence;
    }

    public void ClearSequences()
    {
        p1SequenceList.text = "";
        p2SequenceList.text = "";
    }
}
