using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Tester : MonoBehaviour
{
    bool allDone = false;

    [Header("Hand Testing"), SerializeField]
    int wild = 3;
    [SerializeField]
    List<Card> testCardList = new List<Card>();

    [SerializeField, Space]
    List<Card> testOutBundle1 = new List<Card>();
    [SerializeField]
    List<Card> testOutBundle2 = new List<Card>();
    [SerializeField]
    List<Card> testOutBundle3 = new List<Card>();
    [SerializeField]
    List<Card> testOutBundle4 = new List<Card>();

    List<List<Card>> testOutBundleLists
    {
        get
        {
            var output = new List<List<Card>>();
            if (testOutBundle1 != null && testOutBundle1.Count >= 3) output.Add(testOutBundle1);
            if (testOutBundle2 != null && testOutBundle2.Count >= 3) output.Add(testOutBundle2);
            if (testOutBundle3 != null && testOutBundle3.Count >= 3) output.Add(testOutBundle3);
            if (testOutBundle4 != null && testOutBundle4.Count >= 3) output.Add(testOutBundle4);
            return output;
        }
    }

    [SerializeField, Header("Game Testing")]
    List<AIPlayerData> testPlayers = new List<AIPlayerData>();
    [SerializeField]
    int turnStartOverride = 3;

    [SerializeField]
    GameObject roundOverBanner = null;
    [SerializeField]
    List<HandUI> handVisualizers = new List<HandUI>();  // assuming handVisualizers.Count == testPlayers.Count or will not display!


    private void Start()
    {
        GameManager.RoundBegin += () => ToggleBanner(false);
        GameManager.RoundEnd += () => ToggleBanner(true);
        ToggleBanner(false);
    }

    void ToggleBanner(bool on)
    {
        if (roundOverBanner) roundOverBanner.SetActive(on);
    }

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
            man.InitializeAIGame(testPlayers, turnStartOverride);
        }
    }

    [ContextMenu("Test Best Score")]
    void TryGettingBestScore()
    {
        AI.FindBestPlay(testCardList, wild, out var bundle, out var left);

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

    [ContextMenu("Test Complex Best Score")]
    void TryGettingBestComplexScore()
    {
        List<CardBundle> bundles = new List<CardBundle>();
        foreach (var cardlist in testOutBundleLists)
        {
            var bund = Utilities.TryCreateValidCardBundle(cardlist, wild, true);
            if (bund == null)
            {
                Debug.LogError("Could not create bundle with: " + cardlist);
                return;
            }
            bundles.Add(bund);
        }

        AI.FindBestPlay(testCardList, wild, bundles, out var bundle, out var left, out var bundlePlays);

        if (left.Count == 0)
        {
            Debug.LogError("There's nothing left to discard!");
        }
        else
        {
            var discard = left.OrderBy(b => b.value).Last();
            left.Remove(discard);

            int score = 0;
            score = AI.GetScore(left);

            Debug.LogWarning("=====================");
            Debug.Log($"My best score is " + score);
            Debug.Log("I will discard the " + discard);
            Debug.Log("My bundles were: ");
            foreach (var b in bundle) Debug.Log(b.ToString());
            Debug.Log("I played these on bundles: ");
            for (int i = 0; i < bundles.Count; i++)
            {
                if (bundlePlays.Count > i && bundlePlays[i].Count > 0)
                {
                    Debug.Log($"Played on bundle {i}:");
                    foreach (Card card in bundlePlays[i]) Debug.Log(card);
                }
                
            }
            Debug.LogWarning("=====================");
        }
    }
}
