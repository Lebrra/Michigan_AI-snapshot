using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;

public class CardManager : MonoBehaviour
{
    public static Func<Card, CardUI> GetCard;
    public static Action<CardUI> ReturnCard;

    [SerializeField]
    CardData cardData;

    [SerializeField]
    CardUI cardPrefab;

    [SerializeField]
    Transform defaultCardParent;

    // object pooling:
    Transform inactiveParent = null;
    List<CardUI> inactiveCardUIPool = new List<CardUI>();

    List<CardUI> cardsToDispose = new List<CardUI>();
    Routine disposal;

    private void Awake()
    {
        GetCard += CreateNewCard;
        ReturnCard += QueueDisposeCard;
    }

    public CardUI CreateNewCard(Card card)
    {
        CardUI cardUI = null;

        if (!disposal.Exists() && inactiveCardUIPool.Count > 0)
        {
            // reuse
            cardUI = inactiveCardUIPool[0];
            inactiveCardUIPool.RemoveAt(0);
        }
        else
        {
            // make a new card
            cardUI = Instantiate(cardPrefab, defaultCardParent);
        }
        
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

    public void QueueDisposeCard(CardUI card)
    {
        if (card != null) cardsToDispose.Add(card);
        if (!disposal.Exists()) disposal.Replace(DisposeCards());
    }

    IEnumerator DisposeCards()
    {
        if (inactiveParent == null)
        {
            var parent = new GameObject("CardUIPool");
            parent.SetActive(false);
            inactiveParent = parent.transform;
        }
        yield return null;

        while (cardsToDispose.Count > 0)
        {
            inactiveCardUIPool.Add(cardsToDispose[0]);
            cardsToDispose[0].transform.SetParent(inactiveParent);
            cardsToDispose.RemoveAt(0);
            yield return null;
        }        
    }
}
