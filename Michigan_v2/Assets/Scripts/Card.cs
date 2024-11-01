using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Card
{
    public int value;
    public Suit suit;

    public Card(int v, Suit s)
    {
        value = v;
        suit = s;
    }

    public static bool operator ==(Card a, Card b)
    {
        return a.value == b.value && a.suit == b.suit;
    }

    public static bool operator !=(Card a, Card b)
    {
        return !(a == b);
    }

    public override string ToString()
    {
        if (value == 0)
            return $"[{(suit == Suit.Hearts || suit == Suit.Diamonds ? "Red" : "Black")} Joker]";
        else
            return $"[{Utilities.ValueToString(value)} of {suit}]";
    }
}
