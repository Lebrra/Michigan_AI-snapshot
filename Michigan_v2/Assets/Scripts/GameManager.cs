using BeauRoutine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I;

    [SerializeField]
    List<Player> players = new List<Player>();

    int playerTurn = 0;
    int startingPlayer = 0;

    static readonly int FirstRound = 3;
    static readonly int LastRound = 13;
    int currentRound = 3;
    bool isRoundRunning = false;
    Routine roundReset;

    Deck currentDeck;
    public Deck Deck => currentDeck;

    int outPlayer = -1;

    List<CardBundle> playerOutBundles;
    public List<CardBundle> OutBundles => playerOutBundles;

    public bool HasPlayerGoneOut => outPlayer >= 0;
    public int WildValue => currentRound;

    private void Awake()
    {
        if (I) Destroy(I);
        I = this;
    }

    public void InitializeAIGame(List<AIPlayerData> aiPlayers)   // todo: aiProps should be a setting
    {
        // validate player count:
        if (aiPlayers.Count < 1 || aiPlayers.Count > 5)
        {
            Debug.LogError("Invalid player count; cannot start game!");
            return;
        }

        players = new List<Player>();
        for (int i = 0; i < aiPlayers.Count; i++)
        {
            players.Add(new AIPlayer(aiPlayers[i]));
        }

        // set values:
        currentRound = FirstRound;
        playerTurn = startingPlayer = Random.Range(0, players.Count);
        outPlayer = -1;

        // start round!
        StartNewRound(false);
    }

    public void InitializeSingplayerGame(string playerName, List<AIPlayerData> aiPlayers)   // todo: aiProps should be a setting
    {
        // validate player count:
        if (aiPlayers.Count < 1 || aiPlayers.Count > 5)
        {
            Debug.LogError("Invalid player count; cannot start game!");
            return;
        }

        // load players:
        players.Add(new HumanPlayer(playerName));

        for (int i = 0; i < aiPlayers.Count; i++)
        {
            players.Add(new AIPlayer(aiPlayers[i]));
        }

        // set values:
        currentRound = FirstRound;
        playerTurn = startingPlayer = Random.Range(0, players.Count);
        outPlayer = -1;

        // start round!
        StartNewRound(false);
    }

    IEnumerator WaitToStartNewRound()
    {
        yield return new WaitUntil(() => !isRoundRunning);
        yield return 1F;
        StartNewRound();
    }

    public void StartNewRound(bool updateValues = true)
    {
        if (updateValues)
        {
            currentRound++; // assuming we check if game ended elsewhere
            startingPlayer = (startingPlayer + 1) % players.Count;
            playerTurn = startingPlayer;
            outPlayer = -1;
        }

        currentDeck = new Deck();
        foreach (var player in players)
        {
            player.NewHand(currentDeck.DrawNewHand(currentRound));
        }

        Debug.LogWarning($"[GameManager] STARTING ROUND {Utilities.ValueToString(WildValue)}");
        isRoundRunning = true;
        // start turn order !
        players[playerTurn].TakeTurn(false);
    }

    void EndRound()
    {
        Debug.LogWarning($"[GameManager] COMPLETED ROUND {Utilities.ValueToString(WildValue)}");
        isRoundRunning = false;

        if (currentRound == LastRound)
        {
            // game is over!
        }
        else
        {
            roundReset.Replace(WaitToStartNewRound());
        }
    }

    public void SetPlayerOut(List<CardBundle> outBundles)
    {
        outPlayer = playerTurn;
        playerOutBundles = new();
        playerOutBundles.AddRange(outBundles);
    }

    public void UpdateOutBundles(List<List<Card>> playsPerBundle)
    {
        // todo: ensure that cards aren't out of order in these card lists
        for (int i = 0; i < playerOutBundles.Count; i++)
        {
            foreach (var card in playsPerBundle[i])
            {
                playerOutBundles[i].AddCard(card);
            }
        }
    }

    public void NextTurn()
    {
        if (!isRoundRunning) return;

        // first test end states:
        int nextTurn = (playerTurn + 1) % players.Count;

        if (nextTurn == outPlayer)
        {
            EndRound();
        }
        else
        {
            playerTurn = nextTurn;
            players[playerTurn].TakeTurn(outPlayer != -1);
        }
    }
}
