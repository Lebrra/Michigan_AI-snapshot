using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class HandUI : MonoBehaviour
{
    [SerializeField]
    Transform handParent = null;

    Dictionary<Card, CardUI> loadedCards = new Dictionary<Card, CardUI>();  // a duplicate card will be 'value + 20'

    private void Start()
    {
        GameManager.RoundEnd += Clear;
    }

    public void AddCard(Card card)
    {
        var ui = CardManager.GetCard(card);
        ui.transform.SetParent(handParent);

        if (loadedCards.ContainsKey(card))
        {
            loadedCards.Add(new Card(card.value + 20, card.suit), ui);
        }
        else
        {
            loadedCards.Add(card, ui);
        }
    }
    
    public void RemoveCard(Card card)
    {
        var dupe = loadedCards.Keys.Where(k => k.value == card.value + 20 && k.suit == card.suit).ToList();
        if (dupe.Count > 0)
        {
            // there's 2, get rid of the second one:
            CardManager.ReturnCard(loadedCards[dupe[0]]);
            loadedCards.Remove(dupe[0]);
        }
        else if (loadedCards.ContainsKey(card))
        {
            CardManager.ReturnCard(loadedCards[card]);
            loadedCards.Remove(card);
        }
        else
        {
            Debug.LogError($"[HandUI] Tried to remove card UI for {card} but no card found!");
        }
    }

    public void Clear()
    {
        foreach (var card in loadedCards)
        {
            CardManager.ReturnCard(loadedCards[card.Key]);
        }
        loadedCards.Clear();
    }
}
