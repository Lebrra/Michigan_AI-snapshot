using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// AI difficulties breakdown:
/*
 * easy: always randomly picks deck/discard; checks for any vaild combinations; randomly discards
 * medium: will pick up wilds or cards that match any values in their hand; checks for any valid combinations; discards non-wilds that match the fewest values in hand (mentality: always goes for sets)
 * hard: tests if discard can result in a NEW bundle in hand; tests if discard can be added to an existing bundle in hand; checks for any valid combinations; discards most irrelevant card in hand by testing if each card was missing for a best outcome (mentality: how I would play)
 */
public class AIPlayer : Player
{
    AIPlayerProperties properties;

    int currentHandScore = int.MaxValue;

    public AIPlayer(string n, AIPlayerProperties prop) : base(n)
    {
        properties = prop;
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
        // todo: beauroutines
        
    }

    IEnumerator TakeDelayedTurn(bool isLastTurn)
    {
        var currentTime = Time.time;
        var drawn = DrawCard();

        yield return new WaitForSeconds(Mathf.Clamp(properties.DrawDelay - (Time.time - currentTime), 0F, properties.DrawDelay));

        // todo: wait for draw animation (generally, not just ai)
        VisualizeCardDrawn(drawn);

        currentTime = Time.time;

        // get best play:
        if (isLastTurn)
        {
            AI.FindBestPlay(hand, GameManager.I.WildValue, GameManager.I.OutBundles, out var bundles, out var leftovers, out var bundlePlays);
            var score = GetScoreAndDiscard(leftovers, out var discard);
            AddToScore(score);
            GameManager.I.UpdateOutBundles(bundlePlays);

            yield return new WaitForSeconds(Mathf.Clamp(properties.DiscardDelay - (Time.time - currentTime), 0F, properties.DiscardDelay));

            GameManager.I.Deck.Discard(discard);
            yield return null;
            VisualizeCardDiscarded(discard);
        }
        else
        {
            AI.FindBestPlay(hand, GameManager.I.WildValue, out var bundles, out var leftovers);
            var score = GetScoreAndDiscard(leftovers, out var discard);

            yield return new WaitForSeconds(Mathf.Clamp(properties.DiscardDelay - (Time.time - currentTime), 0F, properties.DiscardDelay));

            if (score == 0)
            {
                AddToScore(score);
                GameManager.I.SetPlayerOut(bundles);
                VisualizeFirstOut(bundles);
            }

            GameManager.I.Deck.Discard(discard);
            yield return null;
            VisualizeCardDiscarded(discard);
        }
    }

    #region AI Helpers

    Card DrawCard()
    {
        Card drawnCard;

        switch (properties.Difficulty)
        {
            case AIDifficulty.Medium:
            case AIDifficulty.Hard:
                // determine if hand would be better with discard:
                var cardsIncludingDiscard = new List<Card> { GameManager.I.Deck.TopOfDiscard };
                AI.FindBestPlay(cardsIncludingDiscard, GameManager.I.WildValue, out var bundles, out var leftovers);

                var discard = leftovers.OrderBy(b => b.value).Last();
                leftovers.Remove(discard);

                int scoreWithDiscard = GetScoreAndDiscard(leftovers, out var _);

                if (scoreWithDiscard < currentHandScore)
                {
                    drawnCard =  GameManager.I.Deck.DrawFromDiscard();
                    currentHandScore = scoreWithDiscard;
                }
                else drawnCard = GameManager.I.Deck.DrawFromDeck();

                break;

            default:

                bool drawChoice = Random.Range(0, 2) == 0;
                if (drawChoice) drawnCard = GameManager.I.Deck.DrawFromDiscard();
                else drawnCard = GameManager.I.Deck.DrawFromDeck();

                break;
        }

        AddCardToEnd(drawnCard);
        return drawnCard;
    }

    int GetScoreAndDiscard(List<Card> leftovers, out Card discard)
    {
        int score = 0;
        leftovers = leftovers.OrderBy(c => c.value).ToList();
        for (int j = 0; j < leftovers.Count - 1; j++)
        {
            score += Utilities.GetScoreValue(leftovers[j]);
        }
        discard = leftovers.Last();
        return score;
    }

    #endregion
}

[System.Serializable]
public struct AIPlayerData
{
    public string name;
    public AIPlayerProperties properties;
}