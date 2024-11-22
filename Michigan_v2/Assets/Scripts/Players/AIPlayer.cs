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

    int currentHandScore = int.MaxValue;

    Card lastDiscard = new Card();

    Routine turn;

    HandUI debug_visibleHand = null;

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
            AI.FindBestPlay(hand, GameManager.I.WildValue, GameManager.I.OutBundles, out var bundles, out var leftovers, out var bundlePlays);
            var discard = AI.GetLastTurnDiscard(leftovers);
            RemoveCard(discard);

            yield return new WaitForSeconds(Mathf.Clamp(properties.DiscardDelay - (Time.time - currentTime), 0F, properties.DiscardDelay));

            leftovers.Remove(discard);
            var score = AI.GetScore(leftovers);
            AddToScore(score);
            VisualizeFinalRoundPlay(bundles, bundlePlays, score);
            GameManager.I.UpdateOutBundles(bundlePlays);
            yield return null;

            lastDiscard = discard;
            GameManager.I.Deck.Discard(discard);
            yield return null;
            VisualizeCardDiscarded(discard);
        }
        else
        {
            AI.FindBestPlay(hand, GameManager.I.WildValue, out var bundles, out var leftovers);
            var discard = AI.FindBestDiscard(leftovers);

            yield return new WaitForSeconds(Mathf.Clamp(properties.DiscardDelay - (Time.time - currentTime), 0F, properties.DiscardDelay));

            RemoveCard(discard);

            if (leftovers.Count == 1)
            {
                AddToScore(0);
                GameManager.I.SetPlayerOut(bundles);
                VisualizeFirstOut(bundles);
            }

            lastDiscard = discard;
            GameManager.I.Deck.Discard(discard);
            yield return null;
            VisualizeCardDiscarded(discard);
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
                var cardsIncludingDiscard = new List<Card> { GameManager.I.Deck.TopOfDiscard };
                AI.FindBestPlay(cardsIncludingDiscard, GameManager.I.WildValue, out var bundles, out var leftovers);

                var discard = leftovers.OrderBy(b => b.value).Last();
                leftovers.Remove(discard);

                int scoreWithDiscard = AI.GetScore(leftovers);

                if (scoreWithDiscard < currentHandScore)
                {
                    drawFromDiscard = true;
                    currentHandScore = scoreWithDiscard;
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