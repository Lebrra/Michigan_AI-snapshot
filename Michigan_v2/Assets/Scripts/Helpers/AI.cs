﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AI
{
    const int MAX_LOOPS = 1000;

    /// <summary>
    /// Given a hand, output all possible bundles that hand can make. This does not attempt to create plays or match bundles that could be played together
    /// </summary>
    public static void GetAllBundles(List<Card> hand, int wildValue, out List<CardBundle> bundles)
    {
        bundles = new List<CardBundle>();
        foreach (var cardList in hand.GetCombinations().Where(l => l.Length >= 3).ToList())
        {
            var bundleResult = Utilities.TryCreateValidCardBundle(cardList.ToList(), wildValue, true);
            if (bundleResult != null)
            {
                bundles.Add(bundleResult);
            }
        }
    }

    /// <summary>
    /// Given a hand and another card, output all bundles that could be created including the provided card. This ignores all other bundle possibilities and does not try to make plays
    /// </summary>
    public static void GetNewBundles(List<Card> handWithoutNewCard, Card newCard, int wildValue, out List<CardBundle> bundles)
    {
        bundles = new List<CardBundle>();

        // first, if we already have this card in hand, we don't need to get all combinations again (ignore wilds for now)
        if (handWithoutNewCard.Contains(newCard) && !Utilities.IsWild(newCard, wildValue))
        {
            // if this, then all we need to do is see if a set of this number exists and add a bundle that uses both instances of the card
            List<Card> handSet = handWithoutNewCard.Where(c => Utilities.IsWild(c, wildValue) || c.value == newCard.value).ToList();
            handSet.Add(newCard);
            foreach (var setList in handSet.GetCombinations().Where(l => l.Length >= 3 && l.Contains(newCard)))
            {
                // these should already be valid sets
                var set = Utilities.TryCreateValidCardBundle(setList.ToList(), wildValue);
                if (set != null) bundles.Add(set);
            }
            bundles.Distinct(); // get rid of dupes before returning (ideally including the ones already in the list but come back to this later)
            return;
        }
        
        // todo: if new card is wild we could do a messier version of ^
        
        var handCopy = handWithoutNewCard.Copy();
        handCopy.Add(newCard);
        foreach (var cardList in handCopy.GetCombinations().Where(l => l.Length >= 3 && l.Contains(newCard)).ToList())
        {
            var bundleResult = Utilities.TryCreateValidCardBundle(cardList.ToList(), wildValue, true);
            if (bundleResult != null)
            {
                bundles.Add(bundleResult);
            }
        }
    }

    /// <summary>
    /// Given already processed bundles and their respective hand, find all possible groupings of bundles. This ignores if a discard is needed or not. Assumes bundles list is accurate
    /// </summary>
    public static void GetBundleGroups(List<Card> hand, int round, List<CardBundle> bundles, out List<ValidBundleGroup> bundleGroups)
    {
        bundleGroups = new List<ValidBundleGroup>();

        if (bundles.Count == 0) return;
        if (bundles.Count == 1)
        {
            bundleGroups.Add(Utilities.CreateBundleGroup(hand.Copy(), bundles[0]));
            return;
        }

        // for all loops: iterate forward => don't look backwards at bundles already processed to reduce computations

        // only do this if its possible
        if (round > 5)
        {
            // save by bundle index for now so we can easily tell if a pairing has already been processed
            List<List<int>> groupingsByIndex = new List<List<int>>();
            List<Card> testHand = new List<Card>();

            // 2 bundles:
            for (int bund = 0; bund < bundles.Count - 1; bund++)
            {
                for (int nextBund = 1; nextBund < bundles.Count; nextBund++)
                {
                    // if bund and nextBund could both be created with the provided hand, then they can exist together and form a group
                    bool valid = true;
                    testHand.Clear();
                    testHand.AddRange(hand);
                    foreach (var card in bundles[bund].Cards)
                    {
                        if (testHand.Contains(card)) testHand.Remove(card);
                        else
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (valid)
                    {
                        foreach (var card in bundles[nextBund].Cards)
                        {
                            if (testHand.Contains(card)) testHand.Remove(card);
                            else
                            {
                                valid = false;
                                break;
                            }
                        }
                    }

                    // all done with card iteration for group of 2 bundles
                    if (valid)
                    {
                        // valid!
                        groupingsByIndex.Add(new List<int>
                    {
                        bund,
                        nextBund
                    });
                    }
                }
            }

            if (round > 8)
            {
                // 3 bundles:   (iterate through every grouping and try to add a bundle from the list, skipping any bundle that is already in the grouping)
                int group;
                int currentGroupingCount = groupingsByIndex.Count;
                for (group = 0; group < currentGroupingCount; group++)  // don't iterate through new groupings! (currentGroupingCount)
                {
                    for (int bund = 0; bund < bundles.Count; bund++)
                    {
                        // if bund is already in group, skip
                        if (groupingsByIndex[group].Contains(bund)) continue;

                        // if group and bund could both be created with the provided hand, then they can exist together and form a new group
                        bool valid = true;
                        testHand.Clear();
                        testHand.AddRange(hand);
                        // current bund:
                        foreach (var card in bundles[bund].Cards)
                        {
                            if (testHand.Contains(card)) testHand.Remove(card);
                            else
                            {
                                valid = false;
                                break;
                            }
                        }

                        if (valid)
                        {
                            // bundle 1 in group:
                            foreach (var card in bundles[groupingsByIndex[group][0]].Cards)
                            {
                                if (testHand.Contains(card)) testHand.Remove(card);
                                else
                                {
                                    valid = false;
                                    break;
                                }
                            }
                        }
                        if (valid)
                        {
                            // bundle 2 in group:
                            foreach (var card in bundles[groupingsByIndex[group][1]].Cards)
                            {
                                if (testHand.Contains(card)) testHand.Remove(card);
                                else
                                {
                                    valid = false;
                                    break;
                                }
                            }
                        }


                        // all done with card iteration for group of 2 bundles
                        if (valid)
                        {
                            // valid!
                            groupingsByIndex.Add(new List<int>
                    {
                        groupingsByIndex[group][0],
                        groupingsByIndex[group][1],
                        bund
                    });
                        }
                    }
                }

                if (round > 11)
                {
                    // 4 bundles:   (same as 3 but one bigger this time)
                    currentGroupingCount = groupingsByIndex.Count;
                    while (group < currentGroupingCount)        // don't iterate through new groupings! (currentGroupingCount)
                    {
                        for (int bund = 0; bund < bundles.Count; bund++)
                        {
                            // if bund is already in group, skip
                            if (groupingsByIndex[group].Contains(bund)) continue;

                            // if group and bund could both be created with the provided hand, then they can exist together and form a new group
                            bool valid = true;
                            testHand.Clear();
                            testHand.AddRange(hand);
                            // current bund:
                            foreach (var card in bundles[bund].Cards)
                            {
                                if (testHand.Contains(card)) testHand.Remove(card);
                                else
                                {
                                    valid = false;
                                    break;
                                }
                            }

                            if (valid)
                            {
                                // bundle 1 in group:
                                foreach (var card in bundles[groupingsByIndex[group][0]].Cards)
                                {
                                    if (testHand.Contains(card)) testHand.Remove(card);
                                    else
                                    {
                                        valid = false;
                                        break;
                                    }
                                }
                            }
                            if (valid)
                            {
                                // bundle 2 in group:
                                foreach (var card in bundles[groupingsByIndex[group][1]].Cards)
                                {
                                    if (testHand.Contains(card)) testHand.Remove(card);
                                    else
                                    {
                                        valid = false;
                                        break;
                                    }
                                }
                            }
                            if (valid)
                            {
                                // bundle 3 in group:
                                foreach (var card in bundles[groupingsByIndex[group][2]].Cards)
                                {
                                    if (testHand.Contains(card)) testHand.Remove(card);
                                    else
                                    {
                                        valid = false;
                                        break;
                                    }
                                }
                            }


                            // all done with card iteration for group of 2 bundles
                            if (valid)
                            {
                                // valid!
                                groupingsByIndex.Add(new List<int>
                    {
                        groupingsByIndex[group][0],
                        groupingsByIndex[group][1],
                        groupingsByIndex[group][2],
                        bund
                    });
                            }
                        }
                        group++;
                    }
                }
            }

            // all done! now create structs from grouping data:
            foreach (var grouping in groupingsByIndex)
            {
                CardBundle[] bunds = new CardBundle[grouping.Count];
                for (int i = 0; i < grouping.Count; i++) bunds[i] = bundles[grouping[i]];
                bundleGroups.Add(Utilities.CreateBundleGroup(hand.Copy(), bunds));
            }
        }

        // don't forget that every isolated bundle is also a group!
        foreach (var bund in bundles)
        {
            bundleGroups.Add(Utilities.CreateBundleGroup(hand.Copy(), bund));
        }
        

        // finally, sort by score
        bundleGroups.OrderBy(b => b.score);
    }

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
                if (Utilities.IsWild(finalCard, wildValue))
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
            else bestScore = Utilities.GetScore(left);
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
                        foreach (var card in play)
                        {
                            tempBundle.AddCard(card);
                        }
                        foreach (var card in tempHand)
                        {
                            if (tempBundle.CanAddCard(card, wildValue))
                            {
                                var newPlay = new List<Card>();
                                newPlay.AddRange(play);
                                newPlay.Add(card);

                                if (!plays.Contains(newPlay))   // negates double+ wilds causing extra iterations
                                {
                                    foundPlays.Add(newPlay);
                                    plays.Add(newPlay);
                                }
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

    public static List<List<List<Card>>> GetMixedBundlePlays(List<Card> hand, int round, List<CardBundle> playableBundles)
    {
        // todo: given the plays found so far, find all possible mixes
        var isolatedPlays = GetIsolatedBundlePlays(hand, round, playableBundles);
        isolatedPlays.Distinct();

        if (playableBundles.Count == 1 || isolatedPlays.Count <= 1)
        {
            // no work needed here; exit
            return isolatedPlays;
        }

        List<List<List<Card>>> mixedPlays = new();
        // we already have all possible bundle plays, we just need to know if any of them can be done at the same time
        // => do any cards overlap?

        List<Card> testHand = new List<Card>();

        // 2 bundles:
        for (int iso = 0; iso < isolatedPlays.Count - 1; iso++)
        {
            for (int nextIso = 1; nextIso < isolatedPlays.Count; nextIso++)
            {
                bool doContinue = false;
                // first check if these isolated plays are on the same bundle, if so then skip because iso already accounted for this
                for (int i = 0; i < playableBundles.Count; i++)
                {
                    if (isolatedPlays[iso][i].Count > 0 && isolatedPlays[nextIso][i].Count > 0)
                    {
                        doContinue = true;
                        break;
                    }
                }
                if (doContinue) continue;

                // now check if there's any card overlap
                bool valid = true;

                testHand.Clear();
                testHand.AddRange(hand);
                foreach (var list in isolatedPlays[iso])
                {
                    foreach (var card in list)
                    {
                        if (testHand.Contains(card)) testHand.Remove(card);
                        else
                        {
                            valid = false;
                            break;
                        }
                    }
                    if (!valid) break;
                }

                if (valid)
                {
                    foreach (var list in isolatedPlays[nextIso])
                    {
                        foreach (var card in list)
                        {
                            if (testHand.Contains(card)) testHand.Remove(card);
                            else
                            {
                                valid = false;
                                break;
                            }
                        }
                        if (!valid) break;
                    }
                }

                if (valid)
                {
                    // valid!
                    var mixedPlay = new List<List<Card>>();
                    for (int i = 0; i < playableBundles.Count; i++)
                    {
                        // already determined that plays iso and nextIso are not on the same bundle
                        if (isolatedPlays[iso][i].Count > 0) mixedPlay.Add(isolatedPlays[iso][i]);
                        else if (isolatedPlays[nextIso][i].Count > 0) mixedPlay.Add(isolatedPlays[nextIso][i]);
                        else mixedPlay.Add(new List<Card>());
                    }

                    mixedPlays.Add(mixedPlay);
                }
            }
        }

        if (round > 8)
        {
            // 3 bundles:
            int mixed;
            int currentMixedCount = mixedPlays.Count;
            for (mixed = 0; mixed < currentMixedCount; mixed++)  // don't iterate through new groupings! (currentGroupingCount)
            {
                for (int iso = 0; iso < isolatedPlays.Count; iso++)
                {
                    bool doContinue = false;
                    // first check if these isolated plays are on the same bundle, if so then skip because iso already accounted for this
                    for (int i = 0; i < playableBundles.Count; i++)
                    {
                        if (isolatedPlays[iso][i].Count > 0 && mixedPlays[mixed][i].Count > 0)
                        {
                            doContinue = true;
                            break;
                        }
                    }
                    if (doContinue) continue;

                    bool valid = true;
                    // current isolated:
                    testHand.Clear();
                    testHand.AddRange(hand);
                    foreach (var list in isolatedPlays[iso])
                    {
                        foreach (var card in list)
                        {
                            if (testHand.Contains(card)) testHand.Remove(card);
                            else
                            {
                                valid = false;
                                break;
                            }
                        }
                        if (!valid) break;
                    }

                    if (valid)
                    {
                        // current mixed:
                        foreach (var list in mixedPlays[mixed])
                        {
                            foreach (var card in list)
                            {
                                if (testHand.Contains(card)) testHand.Remove(card);
                                else
                                {
                                    valid = false;
                                    break;
                                }
                            }
                            if (!valid) break;
                        }
                    }


                    if (valid)
                    {
                        // valid!
                        var mixedPlay = new List<List<Card>>();
                        for (int i = 0; i < playableBundles.Count; i++)
                        {
                            // already determined that plays iso and nextIso are not on the same bundle
                            if (isolatedPlays[iso][i].Count > 0) mixedPlay.Add(isolatedPlays[iso][i]);
                            else if (mixedPlays[mixed][i].Count > 0) mixedPlay.Add(mixedPlays[mixed][i]);
                            else mixedPlay.Add(new List<Card>());
                        }

                        mixedPlays.Add(mixedPlay);
                    }
                }
            }

            if (round > 11)
            {
                // 4 bundles:   (same as 3 but one bigger this time)
                currentMixedCount = mixedPlays.Count;
                while (mixed < currentMixedCount)        // don't iterate through new groupings! (currentGroupingCount)
                {
                    for (int iso = 0; iso < isolatedPlays.Count; iso++)
                    {
                        bool doContinue = false;
                        // first check if these isolated plays are on the same bundle, if so then skip because iso already accounted for this
                        for (int i = 0; i < playableBundles.Count; i++) // max 4 iterations here
                        {
                            if (isolatedPlays[iso][i].Count > 0 && mixedPlays[mixed][i].Count > 0)
                            {
                                doContinue = true;
                                break;
                            }
                        }
                        if (doContinue) continue;

                        bool valid = true;
                        // current isolated:
                        testHand.Clear();
                        testHand.AddRange(hand);
                        foreach (var list in isolatedPlays[iso])
                        {
                            foreach (var card in list)
                            {
                                if (testHand.Contains(card)) testHand.Remove(card);
                                else
                                {
                                    valid = false;
                                    break;
                                }
                            }
                            if (!valid) break;
                        }

                        if (valid)
                        {
                            // current mixed:
                            foreach (var list in mixedPlays[mixed])
                            {
                                foreach (var card in list)
                                {
                                    if (testHand.Contains(card)) testHand.Remove(card);
                                    else
                                    {
                                        valid = false;
                                        break;
                                    }
                                }
                                if (!valid) break;
                            }
                        }


                        if (valid)
                        {
                            // valid!
                            var mixedPlay = new List<List<Card>>();
                            for (int i = 0; i < playableBundles.Count; i++)
                            {
                                // already determined that plays iso and nextIso are not on the same bundle
                                if (isolatedPlays[iso][i].Count > 0) mixedPlay.Add(isolatedPlays[iso][i]);
                                else if (mixedPlays[mixed][i].Count > 0) mixedPlay.Add(mixedPlays[mixed][i]);
                                else mixedPlay.Add(new List<Card>());
                            }

                            mixedPlays.Add(mixedPlay);
                        }
                        mixed++;
                    }
                }
            }
        }

        // finally add the isolated plays:
        mixedPlays.AddRange(isolatedPlays);
        return mixedPlays;
    }

    public static Card GetLastTurnDiscard(List<Card> leftovers)
    {
        return leftovers.OrderBy(b => b.value).Last();
    }

    public static Card FindBestDiscard(List<Card> leftovers)
    {
        // first remove wilds; don't discard these
        leftovers = leftovers.Where(c => !Utilities.IsWild(c)).ToList();

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

    public static Card FindBestDiscardImproved(List<Card> leftovers)
    {
        if (leftovers.Count == 0)
        {
            TextDebugger.Error("No cards left to discard from! We should have gone out then...");
            return default;
        }

        // first remove wilds; don't discard these
        leftovers = leftovers.Where(c => !Utilities.IsWild(c)).ToList();

        if (leftovers.Count < 2) return leftovers.LastOrDefault();

        // order so its nice:
        leftovers = leftovers.OrderBy(c => c.value).ToList();

        // now to assign every card ranking based off this:
        /*
         * 1 = having all 3 properties
         * 2 = possible set & strong run
         * 3 = possible set & weak run
         * 4 = strong & weak run
         * 5 = possible set
         * 6 = strong run
         * 7 = weak run
         * 8 = none
         */
        // and discard the lowest ranked card in this list

        // to do this, points will be assigned by 
        /*
         * +6 for possible set
         * +4 for strong run
         * +3 for weak run
         */
        // the least pointed card (with highest value if tie) will be discarded

        int[] rankings = new int[leftovers.Count];
        for (int i = 0; i < rankings.Length; i++) rankings[i] = 0;

        // possible sets:
        for (int i = 0; i < leftovers.Count - 1; i++)
        {
            if (leftovers[i].value == leftovers[i + 1].value)
            {
                // possible set found
                rankings[i] += 6;
                rankings[i + 1] += 6;
            }
        }

        // strong runs:
        for (int i = 0; i < leftovers.Count - 1; i++)
        {
            for (int j = i+1; j < leftovers.Count; j++)
            {
                // test value first to see if we are ready to break this loop
                if (leftovers[j].value <= leftovers[i].value + 1)
                {
                    // now test exact 
                    if (leftovers[i].value + 1 == leftovers[j].value && leftovers[i].suit == leftovers[j].suit)
                    {
                        // strong run found
                        rankings[i] += 4;
                        rankings[i + 1] += 4;
                        break;
                    }
                    // else keep looking
                }
                // stop looking through j loop
                else break;
            }
        }

        // weak runs:
        for (int i = 0; i < leftovers.Count - 1; i++)
        {
            for (int j = i + 1; j < leftovers.Count; j++)
            {
                // test value first to see if we are ready to break this loop
                if (leftovers[j].value <= leftovers[i].value + 2)
                {
                    // now test exact 
                    if (leftovers[i].value + 2 == leftovers[j].value && leftovers[i].suit == leftovers[j].suit)
                    {
                        // strong run found
                        rankings[i] += 3;
                        rankings[i + 1] += 3;
                        break;
                    }
                    // else keep looking
                }
                // stop looking through j loop
                else break;
            }
        }

        // now to get the results: (default to the first card)
        int worstScore = rankings[0];
        List<int> worstScoreIndices = new List<int> { 0 };
        for (int i = 1; i < rankings.Length; i++)
        {
            if (rankings[i] < worstScore)
            {
                worstScore = rankings[i];
                worstScoreIndices.Clear();
                worstScoreIndices.Add(i);
            }
            else if (rankings[i] == worstScore)
            {
                worstScoreIndices.Add(i);
            }
        }

        if (worstScoreIndices.Count > 1)
        {
            // take highest value among them -> the highest index since cards were sorted by value -> the last value since those were also always ordered
            return leftovers[worstScoreIndices[worstScoreIndices.Count - 1]];
        }
        else return leftovers[worstScoreIndices[0]];
    }

    public static Card PickRandomDiscard(List<Card> leftovers)
    {
        return leftovers[Random.Range(0, leftovers.Count)];
    }
}