using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform cardsParent;
    public GridLayoutGroup gridLayoutGroup;
    public TextMeshProUGUI scoreText, comboText, levelText;
    public GameObject levelCompletePanel;
    public List<LevelData> levels = new List<LevelData>();

    private Vector2Int gridSize;
    private int currentLevelIndex = 0;
    private int score = 0, comboMultiplier = 0, matchStreak = 0, matchedPairs = 0;
    private bool isChecking = false;

    private Card firstFlipped, secondFlipped;
    private List<Card> spawnedCards = new List<Card>();

    void Start()
    {
        comboText.gameObject.SetActive(false);

        if (TryLoadGame(out SaveData data))
        {
            LoadLevel(data);
            Debug.Log("Previous Data: " + data.saveVersion);
        }
        else
        {
            GenerateGrid();
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            PlayerPrefs.DeleteAll();
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }
    void GenerateGrid()
    {
        Debug.Log("Generating Grid for Level " + currentLevelIndex);
        matchedPairs = 0;

        ClearBoard();

        LevelData level = levels[currentLevelIndex];
        gridSize = level.gridSize;
        Sprite[] faces = level.levelCardFaces;

        if (levelText) levelText.text = $"Level: {currentLevelIndex + 1}";
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = gridSize.x;

        int total = gridSize.x * gridSize.y;
        int pairs = total / 2;

        if (faces.Length < pairs)
        {
            Debug.LogError("Not enough faces assigned.");
            return;
        }

        List<int> pairIDs = new List<int>();
        for (int i = 0; i < pairs; i++) { pairIDs.Add(i); pairIDs.Add(i); }
        Shuffle(pairIDs);

        for (int i = 0; i < total; i++)
        {
            GameObject obj = Instantiate(cardPrefab, cardsParent);
            Card card = obj.GetComponent<Card>();
            card.Init(pairIDs[i], faces[pairIDs[i]]);
            card.cardButton.onClick.AddListener(() => OnCardClick(card));
            spawnedCards.Add(card);
        }

        StartCoroutine(PreviewCards());
    }
    void Shuffle(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i],list[rand]) = (list[rand], list[i]);
        }
    }
    void OnCardClick(Card clicked)
    {
        if (isChecking || clicked == firstFlipped || clicked == secondFlipped) return;
        clicked.Flip();

        if (firstFlipped == null) firstFlipped = clicked;
        else
        {
            secondFlipped = clicked;
            isChecking = true;
            StartCoroutine(CheckMatch());
        }
    }

    IEnumerator CheckMatch()
    {
        yield return new WaitForSeconds(1f);

        if (firstFlipped.cardID == secondFlipped.cardID)
        {
            firstFlipped.cardButton.interactable = false;
            secondFlipped.cardButton.interactable = false;
            matchedPairs++;
            matchStreak++;
            comboMultiplier = matchStreak;
            score += 10 * comboMultiplier;
            if (comboText!=null) 
            { 
                comboText.gameObject.SetActive(true);
                comboText.text = "Combo X: " + comboMultiplier; 
                yield return new WaitForSeconds(1f);
                comboText.gameObject.SetActive(false);
                
            }
        }
        else
        {
            firstFlipped.ResetCard();
            secondFlipped.ResetCard();
            score -= 2;
            matchStreak = 0;
            comboMultiplier = 1;
        }

        UpdateScore();

        if (matchedPairs == (gridSize.x * gridSize.y) / 2)
        {
            Debug.Log("Level Complete");
            StartCoroutine(ShowLevelComplete());
        }

        firstFlipped = secondFlipped = null;
        isChecking = false;
    }

    IEnumerator ShowLevelComplete()
    {
        levelCompletePanel.SetActive(true);
        yield return new WaitForSeconds(2);
        levelCompletePanel.SetActive(false);
        matchStreak = 0;
        comboMultiplier = 1;
        comboText.text = "Combo X: " + comboMultiplier;

        currentLevelIndex++;
        if (currentLevelIndex < levels.Count)
        {
            GenerateGrid();
            SaveGame();
        }
        else
        {
            Debug.Log("Game completed!");
            PlayerPrefs.DeleteKey("SaveData");
        }
    }

    void ClearBoard()
    {
        foreach (Transform t in cardsParent) Destroy(t.gameObject);
        spawnedCards.Clear();
        firstFlipped = secondFlipped = null;
    }

    IEnumerator PreviewCards()
    {
        foreach (var card in spawnedCards) { card.Flip(); card.cardButton.interactable = false; }
        yield return new WaitForSeconds(1.5f);
        foreach (var card in spawnedCards) { card.ResetCard(); card.cardButton.interactable = true; }
    }

    void UpdateScore()
    {
        if (scoreText) scoreText.text = "Score: " + score;
       
    }

    void SaveGame()
    {
        SaveData data = new SaveData
        {
            score = score,
            comboMultiplier = comboMultiplier,
            matchStreak = matchStreak,
            currentLevelIndex = currentLevelIndex,
            gridWidth = gridSize.x,
            gridHeight = gridSize.y,
            saveVersion = 1,
            cardStates = new List<CardState>()
        };

        foreach (var card in spawnedCards)
        {
            data.cardStates.Add(new CardState
            {
                cardID = card.cardID,
                isMatched = !card.cardButton.interactable
            });
        }

        PlayerPrefs.SetString("SaveData", JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        Debug.Log("Game Saved");
    }

    bool TryLoadGame(out SaveData data)
    {
        data = null;
        if (!PlayerPrefs.HasKey("SaveData")) return false;

        string json = PlayerPrefs.GetString("SaveData");
        data = JsonUtility.FromJson<SaveData>(json);
        if (data.saveVersion != 1 || data.cardStates == null || data.cardStates.Count == 0) return false;
        return true;
    }

    void LoadLevel(SaveData data)
    {
        currentLevelIndex = data.currentLevelIndex;
        score = data.score;
        comboMultiplier = data.comboMultiplier;
        matchStreak = data.matchStreak;

        LevelData level = levels[currentLevelIndex];
        gridSize = new Vector2Int(data.gridWidth, data.gridHeight);
        matchedPairs = 0;
        ClearBoard();

        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = gridSize.x;

        Sprite[] cardFaces = level.levelCardFaces;
        for (int i = 0; i < data.cardStates.Count; i++)
        {
            var obj = Instantiate(cardPrefab, cardsParent);
            var card = obj.GetComponent<Card>();
            card.cardID = data.cardStates[i].cardID;
            card.Init(card.cardID, cardFaces[card.cardID]);

            if (data.cardStates[i].isMatched)
            {
                card.Flip();
                card.cardButton.interactable = false;
                matchedPairs++;
            }
            else
            {
                card.ResetCard();
                card.cardButton.interactable = true;
            }

            card.cardButton.onClick.AddListener(() => OnCardClick(card));
            spawnedCards.Add(card);
        }

        UpdateScore();
        Debug.Log("Game Loaded: Level " + currentLevelIndex);
    }

    void OnApplicationQuit() => SaveGame();
}
