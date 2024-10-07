using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable, CreateAssetMenu(fileName = "Card", menuName = "ScriptableObjects/Card", order = 2)]
public class CardData : ScriptableObject
{
    [SerializeField]
    string jokerEmoji;

    [Space, SerializeField]
    Sprite spade;
    [SerializeField]
    Sprite heart;
    [SerializeField]
    Sprite club;
    [SerializeField]
    Sprite diamond;
    [SerializeField]
    Sprite joker;

    [Space, SerializeField]
    Color primary;
    [SerializeField]
    Color secondary;

    public Sprite GetJokerSprite => joker;
    public string GetJokerEmoji => jokerEmoji; // ?

    public Color GetColorFromSuit(Suit suit)
    {
        switch (suit)
        {
            case Suit.Diamonds:
            case Suit.Hearts:
                return primary;

            default:
                return secondary;
        }
    }

    public Sprite GetSuitSprite(Suit suit)
    {
        switch (suit)
        {
            case Suit.Diamonds: return diamond;
            case Suit.Hearts: return heart;
            case Suit.Spades: return spade;
            case Suit.Clubs: return club;
            default: return joker;
        }
    }
}
