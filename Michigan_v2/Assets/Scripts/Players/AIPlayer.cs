using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BeauRoutine;

// AI difficulties breakdown:
/*
 * easy: always randomly picks deck/discard; checks for any vaild combinations; randomly discards
 * medium: will pick up wilds or cards that match any values in their hand; checks for any valid combinations; discards non-wilds that match the fewest values in hand (mentality: always goes for sets)
 * hard: tests if discard can result in a NEW bundle in hand; tests if discard can be added to an existing bundle in hand; checks for any valid combinations; discards most irrelevant card in hand by testing if each card was missing for a best outcome (mentality: how I would play)
 */
[System.Serializable]
public class AIPlayer : Player
{
    AIPlayerProperties properties;

    Routine turn;

    HandUI debug_visibleHand = null;

    // reuse data objects:
    Card lastDiscard = new Card();
    List<CardBundle> turnBundles = new List<CardBundle>();
    List<Card> turnLeftovers = new List<Card>();
    int turnScore = int.MaxValue;

    public AIPlayer(string n, AIPlayerProperties prop, HandUI handVisualizer = null) : base(n)
    {
        properties = prop;
        debug_visibleHand = handVisualizer;
    }

    public AIPlayer(AIPlayerData data) : base(data.name)
    {
        properties = data.properties;
    }

    public void UpdateProperties(AIPlayerProperties prop)
    {
        properties = prop;
    }

    public override void TakeTurn(bool isLastTurn)
    {
        if (!turn.Exists())
        {
            turn.Replace(TakeDelayedTurn(isLastTurn));
        }
        else TextDebugger.Error($"Error starting {Name}'s turn, why is it still running?");
    }

    IEnumerator TakeDelayedTurn(bool isLastTurn)
    {
        TextDebugger.Log("=============================================");
        TextDebugger.Log($"Starting {Name}'s turn!");

        var currentTime = Time.time;
        
        var _ = DrawCard();

        yield return new WaitForSeconds(Mathf.Clamp(properties.DrawDelay - (Time.time - currentTime), 0F, properties.DrawDelay));

        PrintHand();
        yield return null;

        currentTime = Time.time;

        // get best play:
        if (isLastTurn)
        {
            AI.FindBestPlay(hand, GameManager.I.WildValue, GameManager.I.OutBundles, out turnBundles, out turnLeftovers, out var bundlePlays);
            lastDiscard = AI.GetLastTurnDiscard(turnLeftovers);
            RemoveCard(lastDiscard);

            yield return new WaitForSeconds(Mathf.Clamp(properties.DiscardDelay - (Time.time - currentTime), 0F, properties.DiscardDelay));

            turnLeftovers.Remove(lastDiscard);
            turnScore = AI.GetScore(turnLeftovers);
            AddToScore(turnScore);
            VisualizeFinalRoundPlay(turnBundles, bundlePlays, turnScore);
            GameManager.I.UpdateOutBundles(bundlePlays);
            yield return null;

            GameManager.I.Deck.Discard(lastDiscard);
            yield return null;
            VisualizeCardDiscarded(lastDiscard);

            yield return null;

            // clear turn values:
            lastDiscard = default;
            turnBundles.Clear();
            turnLeftovers.Clear();
            turnScore = int.MaxValue;
        }
        else
        {
            AI.FindBestPlay(hand, GameManager.I.WildValue, out turnBundles, out turnLeftovers);
            lastDiscard = AI.FindBestDiscardImproved(turnLeftovers);

            yield return new WaitForSeconds(Mathf.Clamp(properties.DiscardDelay - (Time.time - currentTime), 0F, properties.DiscardDelay));

            RemoveCard(lastDiscard);

            if (turnLeftovers.Count == 1)
            {
                AddToScore(0);
                GameManager.I.SetPlayerOut(turnBundles);
                VisualizeFirstOut(turnBundles);
            }

            GameManager.I.Deck.Discard(lastDiscard);
            yield return null;
            VisualizeCardDiscarded(lastDiscard);
        }

        TextDebugger.Log("=============================================");
        yield return null;
        GameManager.I.NextTurn();
    }

    #region Debug - Hand Visualization
    protected override void VisualizeCardDrawn(Card card, bool discard)
    {
        base.VisualizeCardDrawn(card, discard);

        if (debug_visibleHand)
        {
            debug_visibleHand.AddCard(card);
        }
    }

    protected override void VisualizeCardDiscarded(Card card)
    {
        base.VisualizeCardDiscarded(card);

        if (debug_visibleHand)
        {
            debug_visibleHand.RemoveCard(card);
        }
    }
    #endregion

    #region AI Helpers

    bool DrawDecision()
    {
        bool drawFromDiscard = false;

        switch (properties.Difficulty)
        {
            case AIDifficulty.Medium:
            case AIDifficulty.Hard:
                // determine if hand would be better with discard:
                var cardsIncludingDiscard = hand.Copy();
                cardsIncludingDiscard.Add(GameManager.I.Deck.TopOfDiscard);
                AI.FindBestPlay(cardsIncludingDiscard, GameManager.I.WildValue, out var _, out var leftovers);

                var discard = leftovers.OrderBy(b => b.value).Last();
                leftovers.Remove(discard);

                int scoreWithDiscard = AI.GetScore(leftovers);

                if (scoreWithDiscard < turnScore)
                {
                    drawFromDiscard = true;
                    turnScore = scoreWithDiscard;
                }
                else drawFromDiscard = false;

                break;

            default:

                // if we are stuck in an AI loop that keeps picking up the same card, stop
                if (GameManager.I.Deck.TopOfDiscard == lastDiscard) drawFromDiscard = false;
                else drawFromDiscard = Random.Range(0, 4) == 0;
                break;
        }

        return drawFromDiscard;
    }

    Card DrawCard()
    {
        Card drawnCard;
        if (DrawDecision())
        {
            drawnCard = GameManager.I.Deck.DrawFromDiscard();
            VisualizeCardDrawn(drawnCard, true);
        }
        else
        {
            drawnCard = GameManager.I.Deck.DrawFromDeck();
            VisualizeCardDrawn(drawnCard, false);
        }

        AddCardToEnd(drawnCard);
        return drawnCard;
    }    

    #endregion
}

[System.Serializable]
public struct AIPlayerData
{
    public string name;
    public AIPlayerProperties properties;
}