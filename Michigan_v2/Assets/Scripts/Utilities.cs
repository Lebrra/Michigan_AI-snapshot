using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.XR;
using System.Text.RegularExpressions;
using Unity.VisualScripting;

public static class Utilities
{
    public static Suit[] SuitsList = new Suit[4] { Suit.Spades, Suit.Hearts, Suit.Clubs, Suit.Diamonds };

    public static string ValueToString(int value)
    {
        if (value > 1 && value < 11)
        {
            return value.ToString();
        }
        else 
        {
            switch (value)
            {
                case 0:
                    return "JOKER";
                case 1:
                    return "A";
                case 11:
                    return "J";
                case 12:
                    return "Q";
                case 13:
                    return "K";
                default:
                    return "ERR";
            }
        }
    }

    #region Extensions

    public static List<T> Copy<T>(this List<T> list, List<T> exclusions = null)
    {
        var newList = new List<T>();
        newList.AddRange(list);
        if (exclusions != null)
        {
            foreach (var t in exclusions)
            {
                if (newList.Contains(t))
                    newList.Remove(t);
            }
        }

        return newList;
    }

    public static List<T> Shuffle<T>(this List<T> list)
    {
        System.Random rnd = new System.Random();
        list = list.OrderBy(_ => rnd.Next()).ToList();
        return list;
    }

    public static List<T> GetMatches<T>(this IReadOnlyList<T> list, IReadOnlyList<T> compare)
    {
        List<T> matches = new List<T>();
        foreach (var t in compare)
        {
            if (list.Contains(t))
            {
                matches.Add(t);
            }
        }
        return matches;
    }

    public static bool HasNone<T>(this IReadOnlyList<T> list, IReadOnlyList<T> compare)
    {
        foreach (var t in compare)
        {
            if (list.Contains(t)) return false;
        }
        return true;
    }

    // https://stackoverflow.com/questions/64998630/get-all-combinations-of-liststring-where-order-doesnt-matter-and-minimum-of-2
    // usage: var result = list.GetCombinations().Where(l => l.Length >= 3).ToList();
    public static IEnumerable<T[]> GetCombinations<T>(this List<T> source) => Enumerable
           .Range(0, 1 << source.Count).Select(i => source.Where((t, j) => (i & (1 << j)) != 0).ToArray());

    public static string ToString(this CardBundle bundle)
    {
        string cardList = "";
        int iterator = 0;
        while (iterator < bundle.Cards.Count)
        {
            cardList += bundle.Cards[iterator];
            iterator++;
            if (iterator < bundle.Cards.Count) cardList += ", ";
        }

        return $"{bundle.BundleType}: [{cardList}]";
    }

    #endregion

    #region Deck Functions
    public static List<Card> NewDeck(bool shuffle)
    {
        List<Card> newDeck = new();

        foreach (var suit in SuitsList)
        {
            for (int i = 1; i <= 13; i++)
            {
                newDeck.Add(new Card(i, suit));
            }
        }
        // jokers:
        newDeck.Add(new Card(0, Suit.Hearts));
        newDeck.Add(new Card(0, Suit.Spades));

        if (shuffle)
        {
            newDeck = newDeck.Shuffle();
        }

        return newDeck;
    }

    public static List<Card> NewDoubleDeck(bool shuffle)
    {
        List<Card> newDeck = new();
        newDeck.AddRange(NewDeck(shuffle));
        newDeck.AddRange(NewDeck(shuffle));

        if (shuffle)
        {
            newDeck = newDeck.Shuffle();
        }

        return newDeck;
    }

    #endregion

    #region Card Functions

    public static int GetScoreValue(Card card)
    {
        if (card.value > 10) return 10;
        else return card.value;
    }

    public static CardBundle TryCreateValidCardBundle(List<Card> cards, int wildValue, bool ignoreOrder = false)
    {
        // first let's see if there's enough cards:
        if (cards.Count < 3) return null;

        // pull out the wilds first:
        var remaining = cards.Where(c => c.value != 0 && c.value != wildValue).ToList();

        if (remaining.Count == 0)
        {
            // we need at least one non-wild card in the bundle to be able to identify it
            return null;
        }

        // time to check sets (note that if remaining.Count == 1 then we will always default to a set)
        int setValue = remaining[0].value;
        bool isValidSet = true;
        foreach (var card in remaining)
        {
            if (card.value != setValue)
            {
                isValidSet = false;
                break;
            }
        }

        if (isValidSet)
        {
            // we found a set!
            return new CardSet(cards, setValue);
        }

        // let's try making a run:
        if (!ignoreOrder)
        {
            // note: we need to assume order when coming from a player to correctly place wilds
            Suit runSuit = remaining[0].suit;

            // we will need to determine current by finding a non-wild and backtracking:
            int start = remaining[0].value;
            int indexOfFirstRemaining = cards.IndexOf(remaining[0]);
            while (indexOfFirstRemaining > 0)
            {
                start--;
                indexOfFirstRemaining--;
            }
            int end = start;
            bool isValidRun = true;

            // let's just look at each card in order; if we break sequence (excluding wild cards) then we are invalid
            foreach (var card in cards)
            {
                // skip the first card but we still need to reference it later
                if (remaining.IndexOf(card) == 0) continue;

                if (card.value == wildValue || card.value == 0 || (card.value == end + 1 && runSuit == card.suit))
                {
                    end++;
                    continue;
                }
                else
                {
                    isValidRun = false;
                    break;
                }
            }

            // finally let's make sure we aren't using wild cards in invalid places: (below ace or above king)
            if (start <= 0 || end > 13) isValidRun = false;

            if (isValidRun)
            {
                // we found a run!
                return new CardRun(cards, new Card(start, runSuit), new Card(end, runSuit));
            }
        }
        else
        {
            // this is an AI asking -> we don't care where the wilds end up and we can't assume order
            remaining = remaining.OrderBy(c => c.value).ToList();
            var wilds = cards.Where(c => c.value == 0 || c.value == wildValue).ToList();

            Suit runSuit = remaining[0].suit;
            int holes = 0;
            int current = remaining[0].value;
            bool isValidRun = true;

            if (remaining.Count > 1)
            {
                for (int i = 1; i < remaining.Count; i++)
                {
                    if (remaining[i].suit != runSuit || current == remaining[i].value)
                    {
                        isValidRun = false;
                        break;
                    }
                    else
                    {
                        holes += (remaining[i].value - current - 1);
                        current = remaining[i].value;
                    }
                }
            }

            if (holes > wilds.Count) isValidRun = false;

            if (isValidRun)
            {
                // re assemble the remaining with wilds in the right places
                for (int i = 0; i < holes; i++)
                {
                    current = remaining[0].value;
                    for (int j = 1; j < remaining.Count; j++)
                    {
                        current++;
                        if (remaining[j].value == wildValue ||
                            remaining[j].value == 0 ||
                            remaining[j].value == current) continue;
                        else
                        {
                            // hole found!
                            var wild = wilds[0];
                            wilds.Remove(wild);
                            remaining.Insert(j, wild);
                            break;
                        }
                    }
                }

                // get start and end values:
                int start = remaining[0].value;
                int end = start + remaining.Count;

                // are there any wilds left? if so, randomly place them at the beginning or end (if valid!)
                while (wilds.Count > 0)
                {
                    bool randIfBothOpen = Random.Range(0, 1) == 0;

                    if (start == 1 || (end < 13 && randIfBothOpen))
                    {
                        // can only add to end
                        var wild = wilds[0];
                        wilds.Remove(wild);
                        remaining.Add(wild);
                        end++;
                    }
                    else if (end == 13 || (start > 1 && !randIfBothOpen))
                    {
                        // can only add to start
                        var wild = wilds[0];
                        wilds.Remove(wild);
                        remaining.Insert(0, wild);
                        start--;
                    }
                    // else shouldn't be possible
                }

                // all done!
                return new CardRun(remaining, new Card(start, runSuit), new Card(end, runSuit));
            }
        }
        

        // not vaild!
        return null;
    }

    public static void FindBestPlay(List<Card> hand, int wildValue, out List<CardBundle> bundles, out List<Card> leftovers)
    {
        // first: find all bundle possibilities
        // second: find a combination resulting in the least amount of leftover points

        List<CardBundle> allAvailableBundles = new List<CardBundle>();
        foreach (var cardList in hand.GetCombinations().Where(l => l.Length >= 3).ToList())
        {
            var bundleResult = TryCreateValidCardBundle(cardList.ToList(), wildValue, true);
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
                sum += GetScoreValue(remainingCards[j]);
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

    public static void FindBestPlay(List<Card> hand, int wildValue, ref List<CardBundle> playableBundles, out List<CardBundle> bundles, out List<Card> leftovers)
    {
        // todo:
        // 1st - try FindBestPlay ignoring playableBundles
        // 2nd - generate a List<List<Card>> of cards that could be played on playableBundles (if one card can be played, can multiple? (runs))
        // 3rd - for each list of playable cards try FindBestPlay of remaining cards in hand

        bundles = new();
        leftovers = new();

        FindBestPlay(hand, wildValue, out var outBundles, out var left);
        if (left.Count == 1)
        {
            // we don't need the playableBundles just quit
            bundles.AddRange(outBundles);
            leftovers.AddRange(left);
            return;
        }
        else
        {

            List<List<List<Card>>> playsPerBundle = GetAllBundlePlays(hand, wildValue, playableBundles);
            // could we not just keep calling GetAllBundlePlays while we are looking at a unique remaining hand? -> ew though

            // now we have bundles.Count list of possible plays per bundle - we need to see if we can play on multiple at once now

            // todo: we could ask if any plays use all/all but one card -> if yes exit here with that play!

            // after that we can iterate through these to find our best play:
            int bestScore = int.MaxValue;
            List<Card> bundlePlay = new List<Card>();

            foreach (var bundlePlays in playsPerBundle)
            {
                foreach (var play in bundlePlays)
                {
                    var remainingHand = hand.Copy(play);
                    FindBestPlay(remainingHand, wildValue, out var tempBundles, out var tempLeft);
                    
                    if (tempLeft.Count == 1)
                    {
                        // todo: play cards on bundles !

                        bundles = new();
                        leftovers = new();
                        bundles.AddRange(tempBundles);
                        leftovers.AddRange(tempLeft);
                        return;
                    }
                    //else if (score is better than bestScore, document, update, and continue)

                }
            }
        }


    }

    // todo: make this recursive so we can test playing on multiple bundles:
    static List<List<List<Card>>> GetAllBundlePlays(List<Card> hand, int wildValue, List<CardBundle> playableBundles)
    {
        List<List<List<Card>>> playsPerBundle = new List<List<List<Card>>>();
        foreach (var bundle in playableBundles)
        {
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

                    plays.AddRange(cardsCanAdd.GetCombinations().Where(l => l.Length >= 1).ToList());

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

            // remove dupes and add to master list:
            plays = plays.Distinct().ToList();
            playsPerBundle.AddRange(plays);
        }

        return playsPerBundle;
    }

    #endregion
}

#region Enums

public enum Suit
{
    None,
    Hearts,
    Diamonds,
    Spades,
    Clubs
}

public enum AIDifficulty
{
    Easy,
    Medium,
    Hard
}

#endregion
