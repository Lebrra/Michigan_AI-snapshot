using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AIDemo : MonoBehaviour
{
    [SerializeField, Header("Game Testing")]
    List<AIPlayerData> testPlayers = new List<AIPlayerData>();
    [SerializeField]
    int turnStartOverride = 3;
    [SerializeField]
    AIPlayerProperties defaultProps = null;

    [SerializeField, Header("Startup")]
    Transform allDemoObjects = null;
    [SerializeField]
    TMP_Dropdown playerCount = null;
    [SerializeField]
    List<TMP_InputField> playerNames = new List<TMP_InputField>();
    [SerializeField]
    TMP_Dropdown roundStart = null;
    [SerializeField]
    Button startButton = null;

    public void AdjustPlayerCount(int inValue)
    {
        if (int.TryParse(playerCount.options[inValue].text, out int count))
        {
            for (int i = 0; i < playerNames.Count; i++)
            {
                playerNames[i].gameObject.SetActive(i < count);
            }
        }
    }

    public void StartTestAIGame()
    {
        // init values first
        if (int.TryParse(playerCount.options[playerCount.value].text, out int count))
        {
            if (int.TryParse(roundStart.options[roundStart.value].text, out int round))
                turnStartOverride = round;

            for (int i = 0; i < count; i++)
            {
                testPlayers.Add(new AIPlayerData
                {
                    name = playerNames[i].text,
                    properties = defaultProps
                });
            }

            allDemoObjects.gameObject.SetActive(false);

            // start game
            var man = FindObjectOfType<GameManager>();
            if (man)
            {
                man.InitializeAIGame(testPlayers, turnStartOverride);
            }
        }
    }
}
