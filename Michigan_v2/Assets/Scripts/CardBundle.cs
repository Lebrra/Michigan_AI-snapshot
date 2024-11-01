using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

/// <summary>
/// Bundle = Set or Run
/// </summary>
[System.Serializable]
public abstract class CardBundle
{
    /// <summary>
    /// It is assumed that if this was filled the cards have already been verified to be here
    /// </summary>
    protected List<Card> cards = new List<Card>();
    public IReadOnlyList<Card> Cards => cards;

    public virtual CardBundleType BundleType => CardBundleType.None;

    public virtual bool CanAddCard(Card card, int wildValue)
    {
        return true;
    }

    public virtual void AddCard(Card card) { }

    /// <summary>
    /// AI: if we found that our whole hand can bundle but we need a discard still. Assumes cards.Count > 3
    /// </summary>
    /// <returns></returns>
    public virtual Card RemoveOneCard() { return default; }

    /// <summary>
    /// Tries to replace a non wild card with a wild one. Returns Card.value = -1 if invalid
    /// </summary>
    /// <param name="wild"></param>
    /// <returns></returns>
    public virtual bool TryReplaceWithWild(Card wild, out Card card) 
    {
        card = default;
        return false; 
    }

    public virtual CardBundle Copy()
    {
        return null;
    }

    public override string ToString()
    {
        string cardList = "";
        int iterator = 0;
        while (iterator < cards.Count)
        {
            cardList += cards[iterator];
            iterator++;
            if (iterator < cards.Count) cardList += ", ";
        }

        return $"{BundleType}: ({cardList})";
    }

    public enum CardBundleType
    {
        None,
        Set,
        Run
    }
}

[System.Serializable]
public class CardSet : CardBundle
{
    int setValue;

    public override CardBundleType BundleType => CardBundleType.Set;

    public CardSet(List<Card> cardlist, int value)
    {
        setValue = value;
        cards.AddRange(cardlist);
    }

    public override bool CanAddCard(Card card, int wildValue)
    {
        return card.value == setValue
            || card.value == wildValue
            || card.value == 0; // joker value
    }

    public override void AddCard(Card card)
    {
        cards.Add(card);
    }

    /// <summary>
    /// Tried to remove a non-wild first, but if there's only one non-wild we have to remove a wild. Assumes cards.Count > 3
    /// </summary>
    public override Card RemoveOneCard()
    {
        Card removedCard;
        var nonWilds = cards.Where(c => c.value == setValue).ToList();
        if (nonWilds.Count > 1) removedCard = nonWilds.First();
        else removedCard = cards.First(c => c.value != setValue);
        cards.Remove(removedCard);
        return removedCard;
    }

    public override bool TryReplaceWithWild(Card wild, out Card card)
    {
        if (cards.Count(c => c.value == setValue) > 1)
        {
            card = cards.Where(c => c.value == setValue).First();
            cards.Remove(card);
            cards.Add(wild);
            return true;
        }
        else return base.TryReplaceWithWild(wild, out card);
    }

    public override CardBundle Copy()
    {
        var newBundle = new CardSet(cards, setValue);
        return newBundle;
    }
}

[System.Serializable]
public class CardRun : CardBundle
{
    Card minCard, maxCard;

    public override CardBundleType BundleType => CardBundleType.Run;

    public CardRun(List<Card> cardlist, Card min, Card max)
    {
        cards.AddRange(cardlist);
        minCard = min;
        maxCard = max;
    }

    public override bool CanAddCard(Card card, int wildValue)
    {
        // wilds:
        if (card.value == wildValue || card.value == 0) return true;

        if (card.suit == minCard.suit)
        {
            return card.value == minCard.value - 1 || card.value == maxCard.value + 1;
        }
        else return false;
    }

    public override void AddCard(Card card)
    {
        if (card.value == 0 || (card.value != minCard.value - 1 && card.value != maxCard.value + 1))
        {
            if (minCard.value == 1)
            {
                // there's only one place the wild can go, not a problem
                cards.Add(card);
                maxCard.value++;
            }
            else if (maxCard.value == 13)
            {
                // there's only one place the wild can go, not a problem
                cards.Add(card);
                minCard.value--;
            }
            else
            {
                Debug.Log("We are adding a wild card to a run! We need to ask where it should go");
                // todo: handle this case
            }
        }
        else
        {
            cards.Add(card);
            if (card.value == minCard.value - 1)
            {
                minCard.value--;
            }
            else if (card.value == maxCard.value + 1)
            {
                maxCard.value++;
            }
            else
            {
                Debug.LogError("Error adding a card to a run!");
            }
        }
    }

    /// <summary>
    /// Given ends, tries to take the non-wild if there is one. Assumes cards.Count > 3 and cards are already valid in this run.
    /// </summary>
    public override Card RemoveOneCard()
    {
        Card removedCard;

        // try to take the min first:
        if (cards.Any(c => c.value == minCard.value))
        {
            removedCard = minCard;
            minCard.value++;
            
        }
        // max?
        else if (cards.Any(c => c.value == maxCard.value))
        {
            removedCard = maxCard;
            maxCard.value--;
        }
        else
        {
            // wild needs to be removed
            //removedCard = cards.First(c => c.value == 0 || c.value == GameManager.I.WildValue);   // to get a card not in order

            // assuming these are ordered for now; they may not be
            bool useMin = Random.Range(0, 2) == 0;
            if (useMin)
            {
                removedCard = cards.First();
                minCard.value++;
            }
            else
            {
                removedCard = cards.Last();
                maxCard.value--;
            }
        }

        cards.Remove(removedCard);
        return removedCard;
    }

    public override bool TryReplaceWithWild(Card wild, out Card card)
    {
        int iterator = minCard.value;
        int nonWildCount = 0;
        for (int i = 0; i <= cards.Count; i++)
        {
            if (cards[i].value == iterator)
            {
                nonWildCount++;
            }
            iterator++;
        }

        if (nonWildCount > 1)
        {
            int index = 0;
            iterator = minCard.value;
            for (int i = 0; i <= cards.Count; i++)
            {
                if (cards[i].value == iterator)
                {
                    // use this card
                    index = i;
                    break;
                }
                iterator++;
            }

            card = cards[index];
            cards.Remove(card);
            cards.Insert(index, wild);
            return true;
        }
        else return base.TryReplaceWithWild(wild, out card);
    }

    public override CardBundle Copy()
    {
        var newBundle = new CardRun(cards, minCard, maxCard);
        return newBundle;
    }
}
