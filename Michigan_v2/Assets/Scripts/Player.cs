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

    protected void VisualizeCardDrawn(Card card, bool discard)    // todo: should this be a location instead?
    {
        if (discard) Debug.Log($"{name} has drawn {card} from the discard pile!");
        else Debug.Log($"{name} has drawn {card} from the deck!");
    }

    protected void VisualizeFirstOut(List<CardBundle> bundles)
    {
        Debug.LogWarning($"{name} has gone out!");
        Debug.Log($"{name} has played...");

        foreach (var bundle in bundles)
            Debug.Log(bundle.ToString());

        Debug.Log($"{name}'s current score after round {GameManager.I.WildValue} is {Score}");
    }

    protected void VisualizeCardDiscarded(Card card)
    {
        Debug.Log($"{name} has discarded {card}!");
    }

    protected void VisualizeFinalRoundPlay(List<CardBundle> bundles, List<List<Card>> bundlePlays, int points)
    {
        Debug.Log($"{name} played their final round turn and got a score of {points}");

        if (bundles.Count > 0 || bundlePlays.Count > 0)
        {
            Debug.Log($"{name} was able to play...");

            foreach (var bundle in bundles)
                Debug.Log(bundle.ToString());

            for (int i = 0; i < bundlePlays.Count; i++)
            {
                if (bundlePlays[i].Count > 0)
                {
                    string playString = $"Played on bundle {i + 1}: ";
                    if (bundlePlays[i].Count > 1)
                    {
                        for (int j = 0; j < bundlePlays[i].Count - 1; j++)
                        {
                            playString += bundlePlays[i][j].ToString() + ", ";
                        }
                    }
                    playString += bundlePlays[i][bundlePlays[i].Count - 1].ToString();
                    Debug.Log(playString);
                }
            }
        }

        Debug.Log($"{name}'s current score after round {GameManager.I.WildValue} is {Score}");
    }

    protected void PrintHand()
    {
        string handString = "";
        for(int i = 0; i < hand.Count - 1; i++)
        {
            handString += hand[i].ToString() + ", ";
        }
        handString += hand[hand.Count - 1].ToString();

        Debug.Log($"{name}'s current hand: {handString}");
    }
}
