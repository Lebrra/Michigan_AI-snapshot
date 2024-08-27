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
    AIDifficulty difficulty;

    int currentHandScore = int.MaxValue;

    public AIPlayer(string n, AIDifficulty d) : base(n)
    {
        difficulty = d;
    }

    public override void TakeTurn(bool isLastTurn)
    {
        DrawCard();

        // get best play:
        Utilities.FindBestPlay(hand, GameManager.I.WildValue, out var bundles, out var leftovers);
        var score = GetScoreAndDiscard(leftovers, out var discard);

        if (isLastTurn)
        {
            // todo: find best play given another player has gone out
            AddToScore(score);

        }
        else if(score == 0)
        {
            // todo: we can go out!
            AddToScore(score);
        }

        // discard and end
        GameManager.I.Deck.Discard(discard);
    }

    #region AI Helpers

    void DrawCard()
    {
        switch (difficulty)
        {
            case AIDifficulty.Easy:

                bool drawChoice = Random.Range(0, 2) == 0;
                if (drawChoice) AddCardToEnd(GameManager.I.Deck.DrawFromDiscard());
                else AddCardToEnd(GameManager.I.Deck.DrawFromDeck());

                break;

            case AIDifficulty.Medium:
            case AIDifficulty.Hard:
                // determine if hand would be better with discard:
                var cardsIncludingDiscard = new List<Card> { GameManager.I.Deck.TopOfDiscard };
                Utilities.FindBestPlay(cardsIncludingDiscard, GameManager.I.WildValue, out var bundles, out var leftovers);

                var discard = leftovers.OrderBy(b => b.value).Last();
                leftovers.Remove(discard);

                int scoreWithDiscard = GetScoreAndDiscard(leftovers, out var _);

                if (scoreWithDiscard < currentHandScore)
                {
                    AddCardToEnd(GameManager.I.Deck.DrawFromDiscard());
                    currentHandScore = scoreWithDiscard;
                }
                else AddCardToEnd(GameManager.I.Deck.DrawFromDeck());

                break;
        }
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
    public AIDifficulty difficulty;
}