using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscardUI : MonoBehaviour
{
    [SerializeField]
    Transform discardTransform;

    CardUI topDiscard = null;
    CardUI bottomDiscard = null;

    private void Start()
    {
        Deck.DiscardPileTaken += RemoveDiscard;
        Deck.DiscardPileUpdated += AddToDiscard;

        Deck.DiscardPPileSentToDeck += Clear;
        GameManager.RoundEnd += Clear;
    }

    public void AddToDiscard(Card card)
    {
        var ui = CardManager.GetCard(card);
        if (topDiscard != null)
        {
            if (bottomDiscard != null)
            {
                CardManager.ReturnCard(bottomDiscard);
            }
            bottomDiscard = topDiscard;
        }

        topDiscard = CardManager.GetCard(card);
        topDiscard.transform.SetParent(discardTransform, false);
    }

    public void RemoveDiscard()
    {
        if (topDiscard) // this should never be false here...
        {
            CardManager.ReturnCard(topDiscard);
            topDiscard = null;
        }
    }

    public void Clear()
    {
        if (topDiscard) CardManager.ReturnCard(topDiscard);
        if (bottomDiscard) CardManager.ReturnCard(bottomDiscard);
        topDiscard = bottomDiscard = null;
    }
}
