using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using System.Linq;

[System.Serializable]
public abstract class Player
{
    string name;
    public string Name => name;

    protected List<Card> hand;

    List<int> scores;
    public int Score => scores.Sum();

    public Player(string n)
    {
        name = n;

        scores = new List<int>();
        hand = new List<Card>();
    }

    public void NewHand(List<Card> cards)
    {
        hand.Clear();
        hand.AddRange(cards);
    }

    public void AddCardToEnd(Card card)
    {
        hand.Add(card);
    }

    // occurence = there are two identical cards and we chose which one to remove => I don't think this matters here
    public void RemoveCard(Card card, int occurence = 1)
    {
        if (hand.Contains(card))
        {
            // find both occurences if exist:
            int found = -1;
            for (int i = 0; i < hand.Count; i++)
            {
                if (found >= 0)
                {
                    // we found our second card!
                    if (occurence != 1)
                        found = i;
                    break;
                }
                else if (hand[i] == card)
                {
                    found = i;
                }
            }

            // remove card
            hand.RemoveAt(found);
        }
        else Debug.LogError($"Tried to remove {card} but failed!");
    }

    public void AddToScore(int adder)    // may be handled internally
    {
        scores.Add(adder);
    }

    public virtual void TakeTurn(bool isLastTurn) { }
}
