using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AI
{
    const int MAX_LOOPS = 1000;

    public static void FindBestPlay(List<Card> hand, int wildValue, out List<CardBundle> bundles, out List<Card> leftovers)
    {
        Debug.Log("~~ FINDING BEST PLAY ~~");

        // first: find all bundle possibilities
        // second: find a combination resulting in the least amount of leftover points

        List<CardBundle> allAvailableBundles = new List<CardBundle>();
        foreach (var cardList in hand.GetCombinations().Where(l => l.Length >= 3).ToList())
        {
            var bundleResult = Utilities.TryCreateValidCardBundle(cardList.ToList(), wildValue, true);
            if (bundleResult != null)
            {
                allAvailableBundles.Add(bundleResult);
            }
        }

        // let's sort all available bundles from largest to smallest:
        allAvailableBundles = allAvailableBundles.OrderByDescending(b => b.Cards.Count).ToList();

        // with all combinations, find best possible outcome:
        if (allAvailableBundles.Any(b => b.Cards.Count == hand.Count && b.Cards.Count > 3))
        {
            // we can go out with one bundle! => we will need to discard one within the bundle first though
            var bundle = allAvailableBundles.Where(b => b.Cards.Count == hand.Count && b.Cards.Count > 3).First();
            leftovers = new List<Card> { bundle.RemoveOneCard() };
            bundles = new List<CardBundle> { bundle };
            return;
        }
        else if (allAvailableBundles.Any(b => b.Cards.Count == hand.Count - 1))
        {
            // we can go out perfectly with one discard!
            var bundle = allAvailableBundles.Where(b => b.Cards.Count == hand.Count - 1).First();
            bundles = new List<CardBundle> { bundle };

            // leftovers should end with one card remaining...
            leftovers = hand.Copy();
            foreach (var card in bundle.Cards) leftovers.Remove(card);

            return;
        }
        else if (allAvailableBundles.Count == 0)
        {
            // we do not have any bundles, so we can exit here
            leftovers = hand;
            bundles = new List<CardBundle>();
            return;
        }
        // else => we may still be able to go out with multiple bundles, or we will have to take some points here...

        bundles = new List<CardBundle>();
        leftovers = new List<Card>();

        int bestScore = int.MaxValue;
        // we will update bundles and leftovers whenever best score is updated | if bestscore == 0 immediately exit
        for (int i = 0; i < allAvailableBundles.Count; i++)
        {
            var currentBundles = new List<CardBundle>
            {
                allAvailableBundles[i]
            };

            // attempt #2: recursively get bundles with remaining cards
            var remainingCards = hand.Copy();

            for (int j = 0; j < 4; j++)
            {
                // remove used cards...
                foreach (var bundleCard in currentBundles[j].Cards)
                {
                    // if duplicate, it should only remove one instance
                    remainingCards.Remove(bundleCard);
                }

                // update remaming bundles...
                var remainingBundles = allAvailableBundles.Where(b =>
                {
                    foreach (var card in b.Cards)
                    {
                        if (!remainingCards.Contains(card)) return false;
                    }
                    return b.Cards.Count < remainingCards.Count;    // remember we need to have a discard, so we can't use up all the cards!
                }).ToList();

                // any bundles left?
                if (remainingBundles.Count > 0)
                {
                    // should we always take the first one or try some better logic here?
                    currentBundles.Add(remainingBundles.First());

                    // if last iteration, remove cards:
                    if (i == 3)
                    {
                        foreach (var bundleCard in remainingBundles.First().Cards)
                        {
                            // if duplicate, it should only remove one instance
                            remainingCards.Remove(bundleCard);
                        }
                    }
                }
                else
                {
                    // no more matches => exit and count score
                    break;
                }
            }

            // if there's only one card left we can go out!
            if (remainingCards.Count == 1)
            {
                // lets goooooooooo

                // one final thing; if we are about to discard a wild let's try not to do that
                var finalCard = remainingCards[0];
                if (finalCard.value == 0 || finalCard.value == wildValue)
                {
                    foreach (var bundle in currentBundles)
                    {
                        if (bundle.TryReplaceWithWild(finalCard, out Card newFinalCard))
                        {
                            leftovers = new List<Card> { newFinalCard };
                            break;
                        }
                    }
                }
                else leftovers = new List<Card> { finalCard };

                bundles.Clear();
                bundles.AddRange(currentBundles);
                return;
            }

            // else let's see what score we have made...
            int sum = 0;

            // first let's sort so we ignore the highest card:
            remainingCards = remainingCards.OrderBy(c => c.value).ToList();

            for (int j = 0; j < remainingCards.Count - 1; j++)
            {
                sum += Utilities.GetScoreValue(remainingCards[j]);
            }

            if (sum < bestScore)
            {
                bestScore = sum;
                bundles.Clear();
                bundles.AddRange(currentBundles);
                leftovers.Clear();
                leftovers.AddRange(remainingCards);
            }
        }
    }

    public static void FindBestPlay(List<Card> hand, int wildValue, List<CardBundle> playableBundles, out List<CardBundle> bundles, out List<Card> leftovers, out List<List<Card>> bundlePlaysPerBundle)
    {
        Debug.Log("~~ FINDING BEST PLAY WITH BUNDLES ~~");

        // todo:
        // 1st - try FindBestPlay ignoring playableBundles
        // 2nd - generate a List<List<Card>> of cards that could be played on playableBundles (if one card can be played, can multiple? (runs))
        // 3rd - for each list of playable cards try FindBestPlay of remaining cards in hand

        bundles = new();
        leftovers = new();
        bundlePlaysPerBundle = new();

        FindBestPlay(hand, wildValue, out var outBundles, out var left);

        // recording best non-bundle play in case its better:
        int bestScore = int.MaxValue;
        if (outBundles.Count > 0)
        {
            bundles.AddRange(outBundles);
            leftovers.AddRange(left);
            bundlePlaysPerBundle = new();

            // we can go out; leave
            if (left.Count == 1) return;
            else bestScore = AI.GetScore(left);
        }

        // structure: POSSIBLE PLAYS > BUNDLES > CARDS
        List<List<List<Card>>> playsSortedPerBundle = GetMixedBundlePlays(hand, wildValue, playableBundles);

        // after that we can iterate through these to find our best play:
        List<Card> bundlePlay = new List<Card>();

        foreach (var bundlePlays in playsSortedPerBundle)
        {
            // is bundlePlays up to 4 lists of cards to play per bundle?
            var remainingHand = hand.Copy(bundlePlays.ToArray());
            FindBestPlay(remainingHand, wildValue, out var tempBundles, out var tempLeft);

            if (tempLeft.Count == 1)
            {
                bundles = new();
                leftovers = new();
                bundlePlaysPerBundle = new();

                bundles.AddRange(tempBundles);
                leftovers.AddRange(tempLeft);
                bundlePlaysPerBundle.AddRange(bundlePlays);
                return;
            }

            var score = tempLeft.Sum(c => c.value);
            if (score < bestScore)
            {
                bestScore = score;

                bundles = new();
                leftovers = new();
                bundlePlaysPerBundle = new();

                bundles.AddRange(tempBundles);
                leftovers.AddRange(tempLeft);
                bundlePlaysPerBundle.AddRange(bundlePlays);
            }

            // todo: when returned we need to update the actual bundles (or should/can it be done here?)
        }

        // did we find a valid play?
        if (leftovers.Count == 0)
        {
            // let's just output the original FindBestPlay then => no bundle plays
            leftovers.AddRange(left);
            bundles.AddRange(outBundles);
            foreach (var bund in playableBundles) bundlePlaysPerBundle.Add(new());
        }
    }

    static List<List<List<Card>>> GetIsolatedBundlePlays(List<Card> hand, int wildValue, List<CardBundle> playableBundles)
    {
        List<List<List<Card>>> allPossiblePlays = new List<List<List<Card>>>();

        for (int i = 0; i < playableBundles.Count; i++)
        {
            var bundle = playableBundles[i];

            List<List<Card>> plays = new List<List<Card>>();

            switch (bundle.BundleType)
            {
                case CardBundle.CardBundleType.Set:
                    List<Card> cardsCanAdd = new List<Card>();
                    foreach (var card in hand)
                    {
                        if (bundle.CanAddCard(card, wildValue))
                        {
                            cardsCanAdd.Add(card);
                        }
                    }

                    var combinations = cardsCanAdd.GetCombinations().Where(l => l.Length >= 1).ToList();
                    foreach (var combination in combinations)
                    {
                        plays.Add(combination.ToList());
                    }

                    break;

                case CardBundle.CardBundleType.Run:
                    List<List<Card>> foundPlays = new List<List<Card>>();

                    foreach (var card in hand)
                    {
                        if (bundle.CanAddCard(card, wildValue))
                        {
                            foundPlays.Add(new List<Card> { card });
                        }
                    }

                    plays.AddRange(foundPlays);

                    while (foundPlays.Count > 0)
                    {
                        var play = foundPlays[0];
                        foundPlays.RemoveAt(0);

                        var tempHand = hand.Copy();
                        foreach (var card in play)
                        {
                            tempHand.Remove(card);
                        }

                        var tempBundle = bundle.Copy();
                        foreach (var card in play) tempBundle.AddCard(card);
                        foreach (var card in tempHand)
                        {
                            if (tempBundle.CanAddCard(card, wildValue))
                            {
                                var newPlay = new List<Card>();
                                newPlay.AddRange(play);
                                newPlay.Add(card);

                                foundPlays.Add(newPlay);
                                plays.Add(newPlay);
                            }
                        }
                    }

                    break;
            }

            // todo: there are possible duplicate plays in this list !

            // let's reorganize to PLAY > BUNDLES > CARDS 
            foreach (var play in plays)
            {
                var playPerBundle = new List<List<Card>>();
                for (int j = 0; j < playableBundles.Count; j++)
                {
                    playPerBundle.Add(new());
                    if (j == i) playPerBundle[j].AddRange(play);
                }
                allPossiblePlays.Add(playPerBundle);
            }
        }

        return allPossiblePlays;
    }

    static List<List<List<Card>>> GetMixedBundlePlays(List<Card> hand, int wildValue, List<CardBundle> playableBundles)
    {
        // todo: given the plays found so far, find all possible mixes
        var isolatedPlays = GetIsolatedBundlePlays(hand, wildValue, playableBundles);

        if (playableBundles.Count == 1)
        {
            // no work needed here; exit
            return isolatedPlays;
        }

        var playsToIterate = isolatedPlays.Copy();

        // need to mix bundles - 1 times
        for (int iterations = 0; iterations < playableBundles.Count - 1; iterations++)
        {
            var foundMixedPlays = new List<List<List<Card>>>();

            for (int i = 0; i < playsToIterate.Count; i++)
            {
                // FIRST: if we are combining plays that both have cards in the same bundle, then skip! isolated already accounted for this
                var remainingIsolatedPlays = isolatedPlays.Where(iso =>
                    {
                        for (int bund = 0; bund < playableBundles.Count; bund++)
                        {
                            // assuming a list can't be null here, only empty
                            if (playsToIterate[i][bund].Count == 0) return true;
                            else
                            {
                                if (iso[bund].Count > 0) return false;
                            }
                        }
                        return true;
                    }).ToList();

                if (remainingIsolatedPlays.Count == 0) continue;

                // now prep the current bundle states (with isolated[i])
                var tempHand = hand.Copy(playsToIterate[i].ToArray());
                List<CardBundle> bundlesAfterPlay = new List<CardBundle>();
                for (int bund = 0; bund < playableBundles.Count; bund++)
                {
                    if (playsToIterate[i][bund] != null)
                    {
                        bundlesAfterPlay.Add(playableBundles[bund].Copy());
                        foreach (var card in playsToIterate[i][bund])
                        {
                            // note: for runs, and adding a wild, will this always behave the right way?
                            bundlesAfterPlay[bund].AddCard(card);
                        }
                    }
                    else
                    {
                        bundlesAfterPlay.Add(null);
                    }
                }

                // note we have already checked that there are no overlaps with cards being played in the same bundle
                foreach (var remainingIsolatedPlay in remainingIsolatedPlays)
                {
                    bool validPlay = true;
                    var handCopy = tempHand.Copy();
                    foreach (var bundlePlay in remainingIsolatedPlay)
                    {
                        if (bundlePlay != null && validPlay)
                        {
                            foreach (var card in bundlePlay)
                            {
                                if (handCopy.Contains(card))
                                {
                                    handCopy.Remove(card);
                                }
                                else
                                {
                                    // these plays cannot be played together
                                    validPlay = false;
                                    break;
                                }
                            }
                        }

                        if (validPlay)
                        {
                            var newPlay = playsToIterate[i].Copy();
                            for (int bund = 0; bund < newPlay.Count; bund++)
                            {
                                if (remainingIsolatedPlay.Count > i && remainingIsolatedPlay[i].Count > 0) // && newPlay[i].Count == 0)    <- this shouldn't happen
                                {
                                    // assuming all lists are defined
                                    newPlay[i] = remainingIsolatedPlay[i].Copy();
                                }
                            }

                            foundMixedPlays.Add(newPlay);
                        }
                    }
                }
            }

            // all done, update for next iteration
            isolatedPlays.AddRange(foundMixedPlays);
            playsToIterate = foundMixedPlays.Copy();
        }

        // comments from various outdated places:
        // TODO: need functionality for playing on multiple bundles !
        // could we not just keep calling GetAllBundlePlays while we are looking at a unique remaining hand? -> ew though
        // now we have bundles.Count list of possible plays per bundle - we need to see if we can play on multiple at once now

        return isolatedPlays;
    }

    /// <summary>
    /// Assumes discard is not in here
    /// </summary>
    /// <returns></returns>
    public static int GetScore(List<Card> leftovers)
    {
        int score = 0;
        for (int j = 0; j < leftovers.Count; j++)
        {
            score += Utilities.GetScoreValue(leftovers[j]);
        }
        return score;
    }

    public static Card GetLastTurnDiscard(List<Card> leftovers)
    {
        return leftovers.OrderBy(b => b.value).Last();
    }

    public static Card FindBestDiscard(List<Card> leftovers)
    {
        // first remove wilds; don't discard these
        leftovers = leftovers.Where(c => c.value > 0).ToList();

        if (leftovers.Count < 2) return leftovers.LastOrDefault();

        // infinite loop stopper
        int moderator = 0;

        // with the leftover cards, we want to find any cards that may lead to bundles

        // possibleSets = values that have more than one instance in hand - note this has to be 2 instances or it would have been a bundle
        List<int> possibleSets = new List<int>();
        List<Card> cardIterations = leftovers.Copy();
        while (cardIterations.Count > 0)
        {
            var card = cardIterations[0];
            cardIterations.Remove(card);

            int count = cardIterations.Count(c => c.value == card.value);
            if (count > 0)
            {
                possibleSets.Add(card.value);
                cardIterations = cardIterations.Where(c => c.value != card.value).ToList();
            }

            moderator++;
            if (moderator >= MAX_LOOPS)
            {
                TextDebugger.Error("Maxed out iterations!");
                Debug.Break();
            }
        }
        moderator = 0;

        // assumptions: 
        //  - strong runs are exclusively groups of 2 (if they were 3+ then they would have already been a valid bundle)
        //  - if there are card dupes we don't really care (they will be in a set then which is 'stronger')
        List<List<Card>> strongPartialRuns = new List<List<Card>>();
        cardIterations = leftovers.Copy();
        while (cardIterations.Count > 0)
        {
            var card = cardIterations[0];
            cardIterations.Remove(card);

            var suitCards = cardIterations.Where(c => c.suit == card.suit).ToList();
            List<Card> partialRun = new List<Card> { card };

            var minCard = suitCards.FirstOrDefault(c => c.value == card.value - 1);
            if (minCard != default)
            {
                partialRun.Add(minCard);
                cardIterations.Remove(minCard);
            }
            var maxCard = suitCards.FirstOrDefault(c => c.value == card.value + 1);
            if (maxCard != default)
            {
                partialRun.Add(maxCard);
                cardIterations.Remove(maxCard);
            }

            if (partialRun.Count > 1) strongPartialRuns.Add(partialRun);

            moderator++;
            if (moderator >= MAX_LOOPS)
            {
                TextDebugger.Error("Maxed out iterations!");
                Debug.Break();
            }
        }
        moderator = 0;

        // weak = there is a gap between cards
        // note we can't assume a card is only in one pair here
        List<List<Card>> weakPartialRuns = new List<List<Card>>();
        cardIterations = leftovers.Copy();
        while (cardIterations.Count > 0)
        {
            var card = cardIterations[0];
            cardIterations.Remove(card);

            var suitCards = leftovers.Where(c => c.suit == card.suit).ToList();

            var minCard = suitCards.FirstOrDefault(c => c.value == card.value - 2);
            if (minCard != default)
            {
                List<Card> partialRun = new List<Card> { card };
                partialRun.Add(minCard);
                weakPartialRuns.Add(partialRun);
            }
            var maxCard = suitCards.FirstOrDefault(c => c.value == card.value + 2);
            if (maxCard != default)
            {
                List<Card> partialRun = new List<Card> { card };
                partialRun.Add(maxCard);
                weakPartialRuns.Add(partialRun);
            }

            moderator++;
            if (moderator >= MAX_LOOPS)
            {
                TextDebugger.Error("Maxed out iterations!");
                Debug.Break();
            }
        }
        weakPartialRuns = weakPartialRuns.Distinct().ToList();

        // any cards not fit in any? if yes, output those
        List<Card> trueLeftovers = leftovers.Copy();
        trueLeftovers = trueLeftovers.Where(c => !possibleSets.Contains(c.value)).ToList();
        trueLeftovers = trueLeftovers.Where(c =>
        {
            foreach (var stronglist in strongPartialRuns)
            {
                if (stronglist.Contains(c)) return false;
            }
            return true;
        }).ToList();
        trueLeftovers = trueLeftovers.Where(c =>
        {
            foreach (var weakList in weakPartialRuns)
            {
                if (weakList.Contains(c)) return false;
            }
            return true;
        }).ToList();

        if (trueLeftovers.Any()) return trueLeftovers.OrderBy(c => c.value).LastOrDefault();

        // else sort strong to weak: sets, strong runs, weak runs

        var mixedPartialRuns = weakPartialRuns.Where(ls =>
        {
            foreach (var c in ls)
            {
                foreach (var strongList in strongPartialRuns)
                {
                    if (strongList.Contains(c)) return true;
                }
            }
            return false;
        }).ToList();


        // next if there are any isolated weak sets we can choose to drop one of those:
        var weakestWeakRuns = weakPartialRuns.Copy();
        weakestWeakRuns = weakestWeakRuns.Where(ls => !mixedPartialRuns.Contains(ls)).ToList();
        weakestWeakRuns = weakestWeakRuns.Where(ls =>
        {
            foreach (var c in ls)
            {
                if (possibleSets.Contains(c.value) && leftovers.Any(l => l.value == c.value && l.suit == c.suit)) return false;
            }
            return true;
        }).ToList();
        if (weakestWeakRuns.Any())
        {
            trueLeftovers.Clear();
            foreach (var ls in weakestWeakRuns) trueLeftovers.AddRange(ls);
            return trueLeftovers.Distinct().OrderBy(c => c.value).LastOrDefault();
        }

        // moving up to isolated strong sets:
        var weakestStrongRuns = strongPartialRuns.Copy();
        weakestStrongRuns = weakestStrongRuns.Where(ls => !mixedPartialRuns.Contains(ls)).ToList();
        weakestStrongRuns = weakestStrongRuns.Where(ls =>
        {
            foreach (var c in ls)
            {
                if (possibleSets.Contains(c.value) && leftovers.Any(l => l.value == c.value && l.suit == c.suit)) return false;
            }
            return true;
        }).ToList();
        if (weakestStrongRuns.Any())
        {
            trueLeftovers.Clear();
            foreach (var ls in weakestStrongRuns) trueLeftovers.AddRange(ls);
            return trueLeftovers.Distinct().OrderBy(c => c.value).LastOrDefault();
        }

        // decision time! are mixed runs or isolated sets stronger ?
        // chosing sets for now
        if (possibleSets.Any())
        {
            var isolatedSets = possibleSets.Copy();
            isolatedSets = isolatedSets.Where(n =>
            {
                foreach (var ls in weakPartialRuns) if (ls.Any(c => c.value == n)) return false;
                foreach (var ls in strongPartialRuns) if (ls.Any(c => c.value == n)) return false;
                return true;
            }).ToList();

            if (isolatedSets.Any())
            {
                return leftovers.Where(c => isolatedSets.Contains(c.value)).OrderBy(c => c.value).LastOrDefault();
            }
        }

        // now we have to pick something mixed...
        if (mixedPartialRuns.Any())
        {
            trueLeftovers.Clear();
            foreach (var ls in mixedPartialRuns) trueLeftovers.AddRange(ls);
            return trueLeftovers.Distinct().OrderBy(c => c.value).LastOrDefault();
        }

        // are we still here? just pick one then (it will be a pair)
        return leftovers.OrderBy(c => c.value).LastOrDefault();
    }

    public static Card PickRandomDiscard(List<Card> leftovers)
    {
        return leftovers[Random.Range(0, leftovers.Count)];
    }
}