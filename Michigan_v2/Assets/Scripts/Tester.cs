using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Tester : MonoBehaviour
{
    bool allDone = false;

    [SerializeField]
    int wild = 3;
    [SerializeField]
    List<Card> testCardList = new List<Card>();

    [SerializeField]
    List<AIPlayerData> testPlayers = new List<AIPlayerData>();

    [ContextMenu("Display Whole Deck")]
    void ShowAllCards()
    {
        var man = FindObjectOfType<CardManager>();

        if (man)
        {
            var deck = new Deck();
            Deck.DeckIsEmpty += SetDone;

            while (!allDone)
            {
                var card = deck.DrawFromDeck();
                man.CreateNewCard(card);
            }
        }
    }

    void SetDone()
    {
        allDone = true;
        Deck.DeckIsEmpty -= SetDone;
    }


    [ContextMenu("Run Bundle Test")]
    void BundleTest()
    {
        var resultBundle = Utilities.TryCreateValidCardBundle(testCardList, wild);
        if (resultBundle == null)
        {
            Debug.LogError("Invalid bundle!");
        }
        if (resultBundle is CardSet set)
        {
            Debug.Log("Created a set!");
        }
        if (resultBundle is CardRun run)
        {
            Debug.Log("Created a run!");
        }
    }

    [ContextMenu("Start Test Game")]
    void StartTestAIGame()
    {
        var man = FindObjectOfType<GameManager>();
        if (man)
        {
            man.InitializeSingleplayerGame("human", testPlayers);
        }
    }

    [ContextMenu("Test Best Score")]
    void TryGettingBestScore()
    {
        Utilities.FindBestPlay(testCardList, wild, out var bundle, out var left);

        if (left.Count == 0)
        {
            Debug.LogError("There's nothing left to discard!");
        }
        else
        {
            var discard = left.OrderBy(b => b.value).Last();
            left.Remove(discard);

            int score = 0;
            score = left.Sum(c => c.value);

            Debug.LogWarning("=====================");
            Debug.Log($"My best score is " + score);
            Debug.Log("I will discard the " + discard);
            Debug.Log("My bundles were: ");
            foreach (var b in bundle) Debug.Log(b.ToString());
            Debug.LogWarning("=====================");
        }
    }
}
