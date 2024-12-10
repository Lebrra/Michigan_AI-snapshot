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
    bool handProcessed = false;

    HandUI debug_visibleHand = null;

    // round data objects:
    Card lastDiscard = new Card();
    List<CardBundle> roundBundles = new List<CardBundle>();
    List<ValidBundleGroup> roundBundleGroups = new List<ValidBundleGroup>();

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

    public override void NewHand(List<Card> cards)
    {
        base.NewHand(cards);

        // start thinking about hand
        AI.GetAllBundles(cards, GameManager.I.WildValue, out var bunds);
        roundBundles.AddRange(bunds);
        AI.GetBundleGroups(hand, GameManager.I.WildValue, roundBundles, out roundBundleGroups);
        handProcessed = true;
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
        var startTime = currentTime;

        // wait until the initial hand has been processed (did we start with any bundles ?)
        yield return new WaitUntil(() => handProcessed);
        
        var _ = DrawCard();

        if (properties.DrawDelay > 0F)
            yield return new WaitForSeconds(Mathf.Clamp(properties.DrawDelay - (Time.time - currentTime), 0F, properties.DrawDelay));
        else yield return null;

        PrintHand();
        yield return null;

        currentTime = Time.time;

        bool wentOut = false;

        // get best play:
        if (isLastTurn)
        {
            // using all valid bundle groups and mixedbundleplays (AI.cs) find the best possible play for the last turn of the round
            var allbundlePlays = AI.GetMixedBundlePlays(hand, GameManager.I.WildValue, GameManager.I.OutBundles);

            List<List<Card>> roundBundlePlays = new List<List<Card>>();
            int bestScore = int.MaxValue;
            var remainingHand = hand.Copy();

            // set default play to just using the best roundBundleGroup: (if any exist, else we will use the best bundle play later)
            if (roundBundleGroups.Count > 0)
            {
                if (roundBundleGroups[0].CanBundleGroupGoOut())
                {
                    wentOut = true;
                    bestScore = 0;
                }
                else
                {
                    bestScore = roundBundleGroups[0].score;
                }
                roundBundles = roundBundleGroups[0].bundles;
                remainingHand = roundBundleGroups[0].unusedCards;
            }
            
            if (!wentOut)
            {
                // note that since this is the last turn we can be destructive with the groupings
                foreach (var group in roundBundleGroups)
                {
                    foreach (var bundlePlay in allbundlePlays)
                    {
                        // try 'using' all bundlePlay cards from group.unusedCards -> if we can get all the way through these plays can be combined!
                        var leftovers = group.unusedCards.Copy();
                        bool validCombination = true;
                        foreach (var list in bundlePlay)
                        {
                            foreach (var card in list)
                            {
                                if (leftovers.Contains(card)) leftovers.Remove(card);
                                else
                                {
                                    validCombination = false;
                                    break;
                                }
                            }
                            if (!validCombination) break;
                        }

                        if (validCombination)
                        {
                            // tally up the score and see if its better:
                            if (leftovers.Count <= 1)
                            {
                                // rover got a bone!

                                remainingHand = leftovers;
                                roundBundlePlays = bundlePlay;
                                roundBundles = group.bundles;
                                bestScore = 0;
                                wentOut = true;
                                break;
                            }
                            else
                            {
                                // we couldn't go out but is this a better scoring play?
                                int score = Utilities.GetScore(leftovers);
                                if (score < bestScore)
                                {
                                    // update
                                    bestScore = score;
                                    roundBundles = group.bundles;
                                    roundBundlePlays = bundlePlay;
                                    remainingHand = leftovers;
                                }
                            }
                        }
                    }
                    if (wentOut) break;
                }
            }

            // finally test just playing on bundles to see what score we can get there:
            if (!wentOut)
            {
                foreach (var bundlePlay in allbundlePlays)
                {
                    var leftovers = hand.Copy();
                    foreach (var list in bundlePlay)
                    {
                        foreach (var card in list)
                        {
                            // assuming these are valid since they were created with the hand
                            leftovers.Remove(card);
                        }
                    }

                    if (leftovers.Count <= 1)
                    {
                        // went out with only playing on other bundles, kinda crazy
                        roundBundlePlays = bundlePlay;
                        roundBundles = new List<CardBundle>();
                        bestScore = 0;
                        wentOut = true;
                        break;
                    }
                    else
                    {
                        int score = Utilities.GetScore(leftovers);
                        if (score < bestScore)
                        {
                            // update
                            bestScore = score;
                            roundBundles = new List<CardBundle>();
                            roundBundlePlays = bundlePlay;
                            remainingHand = leftovers;
                        }
                    }
                }
            }

            // results time!
            if (remainingHand.Count > 0) lastDiscard = AI.GetLastTurnDiscard(remainingHand);
            else
            {
                // we used all the cards, but still need a discard
                if (roundBundlePlays.Count > 0)
                {
                    // this is more likely since we would have likely gone out sooner if all our cards could have been played
                    for (int i = 0; i < roundBundlePlays.Count; i++)
                    {
                        if (roundBundlePlays[i].Count >= 1)
                        {
                            lastDiscard = roundBundlePlays[i][roundBundlePlays[i].Count - 1];
                            roundBundlePlays[roundBundlePlays[i].Count - 1] = new List<Card>();
                            break;
                        }
                    }
                    // this had to have selected one due to how the lists are structured (I hope)
                }
                else
                {
                    // there must be a bundle that has more than 3 cards, otherwise we have an error
                    var bundle = roundBundles.Where(b => b.Cards.Count > 3).FirstOrDefault();
                    if (bundle != null)
                    {
                        lastDiscard = bundle.RemoveOneCard();
                    }
                    else
                    {
                        TextDebugger.Error("We ran into some crazy error here....");
                        yield break;
                    }
                }
            }
            RemoveCard(lastDiscard);

            if (properties.DiscardDelay > 0F)
                yield return new WaitForSeconds(Mathf.Clamp(properties.DiscardDelay - (Time.time - currentTime), 0F, properties.DiscardDelay));
            else yield return null;

            remainingHand.Remove(lastDiscard);
            bestScore = Utilities.GetScore(remainingHand);
            AddToScore(bestScore);
            VisualizeFinalRoundPlay(roundBundles, roundBundlePlays, bestScore);
            GameManager.I.UpdateOutBundles(roundBundlePlays);
            yield return null;

            GameManager.I.Deck.Discard(lastDiscard);
            yield return null;
            VisualizeCardDiscarded(lastDiscard);

            yield return null;

            // clear round values:
            lastDiscard = default;
            roundBundles.Clear();
            roundBundleGroups.Clear();
            handProcessed = false;
        }
        else
        {
            if (roundBundleGroups.Count == 1 && roundBundleGroups[0].CanBundleGroupGoOut())
            {
                // go out!
                AddToScore(0);
                GameManager.I.SetPlayerOut(roundBundles);
                VisualizeFirstOut(roundBundles);
                lastDiscard = roundBundleGroups[0].GetGroupDiscard();
                wentOut = true;
            }
            else if (roundBundleGroups.Count == 0)
            {
                // we have a bad hand with nothing to build with, so just determine best discard
                lastDiscard = AI.FindBestDiscardImproved(hand);
            }
            else
            {
                // we have something going here, so get a discard from our current best play:
                lastDiscard = roundBundleGroups[0].GetGroupDiscard();
            }

            if (properties.DiscardDelay > 0F)
                yield return new WaitForSeconds(Mathf.Clamp(properties.DiscardDelay - (Time.time - currentTime), 0F, properties.DiscardDelay));
            else yield return null;

            RemoveCard(lastDiscard);
            GameManager.I.Deck.Discard(lastDiscard);
            VisualizeCardDiscarded(lastDiscard);
            yield return null;

            // update bundles and groups with our chosen discard:
            if (!wentOut) UpdateBundles(lastDiscard);
            else
            {
                // clear round values:
                lastDiscard = default;
                roundBundles.Clear();
                roundBundleGroups.Clear();
                handProcessed = false;
            }
        }

        var turnTime = Mathf.Round((Time.time - startTime) * 1000F);
        //TextDebugger.Log($"Turn duration: {turnTime}ms");
        turnTime -= (properties.DiscardDelay + properties.DrawDelay) * 1000F;
        TextDebugger.Log($"Turn duration excluding forced delays: {turnTime}ms");

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

    Card DrawCard()
    {
        bool drawFromDiscard;
        bool requiresBundleUpdate = true;
        Card drawnCard;

        switch (properties.Difficulty)
        {
            case AIDifficulty.Medium:
            case AIDifficulty.Hard:
                // determine if hand would be better with discard:
                //var cardsIncludingDiscard = hand.Copy();
                //cardsIncludingDiscard.Add(GameManager.I.Deck.TopOfDiscard);
                //AI.FindBestPlay(cardsIncludingDiscard, GameManager.I.WildValue, out var _, out var leftovers);

                AI.GetNewBundles(hand, GameManager.I.Deck.TopOfDiscard, GameManager.I.WildValue, out var newBundles);
                if (newBundles.Count == 0)
                {
                    drawFromDiscard = false;
                    break;
                }
                // else need to see if any bundles are better
                var bundlesWithDis = roundBundles.Copy();
                bundlesWithDis.AddRange(newBundles);
                bundlesWithDis.Distinct();
                var handCopy = hand.Copy();
                handCopy.Add(GameManager.I.Deck.TopOfDiscard);
                AI.GetBundleGroups(handCopy, GameManager.I.WildValue, bundlesWithDis, out var newGroups);

                // if we can go out pick up discard
                if (newGroups.Any(g => g.CanBundleGroupGoOut()))
                {
                    drawFromDiscard = true;
                    requiresBundleUpdate = false;
                    // update the roundBundleGroups so this grouping is the only one:
                    roundBundleGroups = new List<ValidBundleGroup> { roundBundleGroups.Where(g => g.CanBundleGroupGoOut()).First() };
                    roundBundles = roundBundleGroups[0].bundles;
                }
                // if we now have a grouping and didn't before pick up discard
                else if (roundBundleGroups.Count == 0 && newGroups.Count > 0)
                {
                    drawFromDiscard = true;
                    roundBundles = bundlesWithDis;
                    roundBundleGroups = newGroups;
                    requiresBundleUpdate = false;
                }
                // if this discard results in a better score than before pick it up
                else if (roundBundleGroups.Count > 0 && newGroups.Count > roundBundleGroups.Count)
                {
                    // assumes group lists are always sorted by score
                    drawFromDiscard = newGroups[0].score < roundBundleGroups[0].score;

                    if (drawFromDiscard)
                    {
                        roundBundles = bundlesWithDis;
                        roundBundleGroups = newGroups;
                        requiresBundleUpdate = false;
                    }
                }
                // don't pick up the discard
                else
                {
                    drawFromDiscard = false;
                }

                break;

            default:
                // if we are stuck in an AI loop that keeps picking up the same card, stop
                if (GameManager.I.Deck.TopOfDiscard == lastDiscard) drawFromDiscard = false;
                else drawFromDiscard = Random.Range(0, 4) == 0;
                break;
        }


        if (drawFromDiscard)
        {
            drawnCard = GameManager.I.Deck.DrawFromDiscard();
            VisualizeCardDrawn(drawnCard, true);
        }
        else
        {
            drawnCard = GameManager.I.Deck.DrawFromDeck();
            VisualizeCardDrawn(drawnCard, false);
        }

        if (requiresBundleUpdate)
        {
            AI.GetNewBundles(hand, drawnCard, GameManager.I.WildValue, out var newBundles);
            roundBundles.AddRange(newBundles);
            roundBundles.Distinct();
            AddCardToEnd(drawnCard);
            AI.GetBundleGroups(hand, GameManager.I.WildValue, roundBundles, out roundBundleGroups);

            if (roundBundleGroups.Any(g => g.CanBundleGroupGoOut()))
            {
                // go out!
                roundBundleGroups = new List<ValidBundleGroup> { roundBundleGroups.Where(g => g.CanBundleGroupGoOut()).First() };
                roundBundles = roundBundleGroups[0].bundles;
            }
        }
        else
        {
            AddCardToEnd(drawnCard);
        }

        return drawnCard;
    }    

    /// <summary>
    /// Assumes discard has already been removed from hand
    /// Ideally we will never see a discard be used here, but this could get complex/fail if there was one here
    /// </summary>
    void UpdateBundles(Card discard)
    {
        bool hasDupe = hand.Contains(discard);

        // iterate backwards so removal doesn't cause conflicts
        for (int i = roundBundles.Count - 1; i > 0; i--)
        {
            if (roundBundles[i].Cards.Contains(discard))
            {
                if (hasDupe && roundBundles[i].Cards.Count(c => c == discard) == 2)
                {
                    // both instances of a card were found here, but we are only removing one, so this bundle is no longer valid
                    roundBundles.RemoveAt(i);
                }
                else if (!hasDupe)
                {
                    // there isn't a second instance of the same card, so get rid of all instances of it
                    roundBundles.RemoveAt(i);
                }
            }
        }

        // and update groupings after
        AI.GetBundleGroups(hand, GameManager.I.WildValue, roundBundles, out roundBundleGroups);
    }

    #endregion
}

[System.Serializable]
public struct AIPlayerData
{
    public string name;
    public AIPlayerProperties properties;
}