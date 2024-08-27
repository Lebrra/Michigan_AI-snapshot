using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Hand 
{
    // todo: support reordering
    List<Card> cards;

    public Hand(List<Card> newHand)
    {
        cards = new List<Card>();
        cards.AddRange(newHand);
    }

    
}
