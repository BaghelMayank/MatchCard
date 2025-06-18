using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int comboMultiplier;
    public int score;
    public int matchStreak;
    public int currentLevelIndex;
    public List<CardState> cardStates;
    public int gridWidth;
    public int gridHeight;
    public int saveVersion;
}

[System.Serializable]
public class CardState
{
    public int cardID;
    public bool isMatched;
}
