using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Deck
{
    /// <summary>
    /// Whenever the discard pile visually changes
    /// Card = the current card on top of the discard pile
    /// </summary>
    public static Action DiscardPileTaken;
    public static Action<Card> DiscardPileUpdated;
    public static Action DiscardPPileSentToDeck;
    public static Action DeckIsEmpty;

    List<Card> deck;
    List<Card> discard;

    public Deck()
    {
        discard = new List<Card>();
        deck = Utilities.NewDoubleDeck(true);

        // put the first card of the deck in the discard pile:
        Discard(DrawFromDeck());
    }

    #region Getters

    public Card TopOfDiscard => discard.LastOrDefault();

    #endregion

    #region Helpers

    void DiscardIntoDeck()
    {
        var topOfDiscard = discard.LastOrDefault();
        discard.Remove(topOfDiscard);

        deck.Clear();
        deck.AddRange(discard);
        deck.Shuffle();

        discard.Clear();
        discard.Add(topOfDiscard);

        DiscardPPileSentToDeck?.Invoke();
    }

    #endregion

    public List<Card> DrawNewHand(int amount)
    {
        List<Card> hand = new();
        while (hand.Count < amount)
        {
            hand.Add(DrawFromDeck());
        }
        return hand;
    }

    public Card DrawFromDeck()
    {
        if (deck.Count == 0)
        {
            DiscardIntoDeck();
        }

        var draw = deck.FirstOrDefault();
        deck.Remove(draw);

        if (deck.Count == 0)
        {
            DeckIsEmpty?.Invoke();
        }

        return draw;
    }

    public Card DrawFromDiscard()
    {
        if (discard.Count > 0)
        {
            var topOfDiscard = discard.LastOrDefault();
            discard.Remove(topOfDiscard);
            DiscardPileTaken?.Invoke();
            return topOfDiscard;
        }
        else return new Card(-1, Suit.None);
    }

    public void Discard(Card card)
    {
        discard.Add(card);
        DiscardPileUpdated?.Invoke(TopOfDiscard);
    }
}
