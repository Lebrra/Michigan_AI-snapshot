using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable, CreateAssetMenu(fileName = "AIProperty", menuName = "ScriptableObjects/AIProperty", order = 2)]
public class AIPlayerProperties : ScriptableObject
{
    [Header("Delays"), SerializeField]
    float drawDelay = 1F;
    [SerializeField]
    float discardDelay = 1F;

    [Space, SerializeField]
    AIDifficulty defaultDifficulty = AIDifficulty.Easy;

    public float DrawDelay => drawDelay;
    public float DiscardDelay => discardDelay;
    public AIDifficulty Difficulty => defaultDifficulty;
}
