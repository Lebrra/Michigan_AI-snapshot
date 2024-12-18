using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class Utilities
{
    public static readonly Suit[] SuitsList = new Suit[4] { Suit.Spades, Suit.Hearts, Suit.Clubs, Suit.Diamonds };

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

    public static int StringToValue(string str)
    {
        switch (str)
        {
            case "J": return 11;
            case "Q": return 12;
            case "K": return 13;
            case "JOKER": return 0;
            default:
                if (int.TryParse(str, out var value)) return value;
                else return -1;
        }
    }

    #region Extensions

    public static List<T> Copy<T>(this List<T> list, params List<T>[] exclusions)
    {
        var newList = new List<T>();
        newList.AddRange(list);
        if (exclusions != null)
        {
            foreach (var e in exclusions)
            {
                foreach (var t in e)
                {
                    if (newList.Contains(t))
                        if (!newList.Remove(t))
                        {
                            Debug.LogError("[CardListCopier] Error copying list; could not remove card " + t);
                            return list;
                        }
                }
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


    /// <summary>
    /// Only verifies if a bundle group can go out on its own
    /// </summary>
    public static bool CanBundleGroupGoOut(this ValidBundleGroup group)
    {
        // if the correct amount of cards are all used, then we can go out!
        if (group.cards.Count == GameManager.I.WildValue) return true;
        if (group.cards.Count == GameManager.I.WildValue + 1)
        {
            // this means all cards (including discard) are used in bundles, which is okay as long as one of the bundles can lose a card and remain valid
            // (although a configuration without using the discard should also exist if this one does)
            return group.bundles.Any(b => b.Cards.Count > 3);
        }
        return false;
    }

    public static Card GetGroupDiscard(this ValidBundleGroup group)
    {
        if (group.CanBundleGroupGoOut())
        {
            if (group.unusedCards.Count == 1) return group.unusedCards[0];
            else if (group.unusedCards.Count > 1)
            {
                TextDebugger.Error("Found a bundle group that claims it can go out but has more than one unused card!");
                return AI.FindBestDiscardImproved(group.unusedCards);
            }
            else
            {
                // all cards are in bundles, so discard one from a bundle of more than 3 cards
                var bund = group.bundles.Where(b => b.Cards.Count > 3).First();
                var discard = bund.RemoveOneCard();
                group.cards.Remove(discard);
                group.unusedCards.Add(discard);
                return discard;
            }
        }
        else
        {
            return AI.FindBestDiscardImproved(group.unusedCards);
        }
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

    /// <summary>
    /// Assumes discard is not in here
    /// </summary>
    /// <returns></returns>
    public static int GetScore(List<Card> leftovers)    // todo: this should be in util not here
    {
        int score = 0;
        for (int j = 0; j < leftovers.Count; j++)
        {
            score += Utilities.GetScoreValue(leftovers[j]);
        }
        return score;
    }

    public static bool IsWild(Card card, int wildValue = -5)
    {
        return card.value == 0 || card.value == wildValue;
    }

    public static CardBundle TryCreateValidCardBundle(List<Card> cards, int wildValue, bool ignoreOrder = false)
    {
        // first let's see if there's enough cards:
        if (cards.Count < 3) return null;

        // pull out the wilds first:
        var remaining = cards.Where(c => !IsWild(c, wildValue)).ToList();

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

                if (IsWild(card, wildValue) || (card.value == end + 1 && runSuit == card.suit))
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
            var wilds = cards.Where(c => IsWild(c, wildValue)).ToList();

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
                        if (IsWild(remaining[j], wildValue) || remaining[j].value == current) continue;
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
                int end = start + remaining.Count - 1;

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

    /// <summary>
    /// Assumes that provided bundles have already been validated to exist together
    /// </summary>
    public static ValidBundleGroup CreateBundleGroup(List<Card> handCopy, params CardBundle[] bundles)
    {
        var usedCards = new List<Card>();

        if (bundles.Length > 0)
        {
            foreach (var bundle in bundles)
            {
                foreach (var card in bundle.Cards)
                {
                    usedCards.Add(card);
                    handCopy.Remove(card);
                }
            }
        }

        return new ValidBundleGroup
        {
            bundles = bundles.Length > 0 ? bundles.ToList() : new List<CardBundle>(),
            cards = usedCards,
            unusedCards = handCopy,
            score = handCopy.Sum(c => GetScoreValue(c))
        };
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