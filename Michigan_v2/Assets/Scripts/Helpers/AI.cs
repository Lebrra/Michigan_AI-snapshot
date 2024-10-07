using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AI
{
    public static void FindBestPlay(List<Card> hand, int wildValue, out List<CardBundle> bundles, out List<Card> leftovers)
    {
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
        // todo:
        // 1st - try FindBestPlay ignoring playableBundles
        // 2nd - generate a List<List<Card>> of cards that could be played on playableBundles (if one card can be played, can multiple? (runs))
        // 3rd - for each list of playable cards try FindBestPlay of remaining cards in hand

        bundles = new();
        leftovers = new();
        bundlePlaysPerBundle = new();

        FindBestPlay(hand, wildValue, out var outBundles, out var left);
        if (left.Count == 1)
        {
            // we don't need the playableBundles just quit
            bundles.AddRange(outBundles);
            leftovers.AddRange(left);
            bundlePlaysPerBundle = new();
            return;
        }
        else
        {
            // structure: POSSIBLE PLAYS > BUNDLES > CARDS
            List<List<List<Card>>> playsSortedPerBundle = GetMixedBundlePlays(hand, wildValue, playableBundles);

            // after that we can iterate through these to find our best play:
            int bestScore = int.MaxValue;
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
                                if (remainingIsolatedPlay[i].Count > 0) // && newPlay[i].Count == 0)    <- this shouldn't happen
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
}