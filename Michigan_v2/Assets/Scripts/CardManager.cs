using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public static Func<Card, CardUI> GetCard;
    public static Action<CardUI> ReturnCard;    // todo: object pool card prefabs

    [SerializeField]
    CardData cardData;

    [SerializeField]
    CardUI cardPrefab;

    [SerializeField]
    Transform defaultCardParent;

    private void Awake()
    {
        GetCard += CreateNewCard;
    }

    public CardUI CreateNewCard(Card card)
    {
        var cardUI = Instantiate(cardPrefab, defaultCardParent);
        if (card.value == 0)
        {
            // joker
            cardUI.LoadCard(
                cardData.GetJokerSprite, 
                cardData.GetJokerEmoji, 
                cardData.GetColorFromSuit(card.suit));
        }
        else
        {
            cardUI.LoadCard(
                cardData.GetSuitSprite(card.suit),
                Utilities.ValueToString(card.value),
                cardData.GetColorFromSuit(card.suit));
        }

        return cardUI;
    }
}
