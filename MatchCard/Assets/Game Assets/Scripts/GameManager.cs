using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Core References")]
    public GameObject cardPrefab;
    public Transform cardsParent;
    public GridLayoutGroup gridLayoutGroup;

    [Header("UI Panels")]
    public GameObject gamePanel;
    public GameObject winPanel;
    public GameObject levelCompletePanel;
    public GameObject levelSelectPanel;

    [Header("UI Text")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI finalScoreText;

    [Header("Level Select")]
    public Transform levelButtonParent;
    public GameObject levelButtonPrefab;

    [Header("Level Data")]
    public List<LevelData> levels = new();

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioSource musicSource;
    public AudioClip flipClip;
    public AudioClip matchClip;
    public AudioClip winClip;
    
    private Vector2Int gridSize;
    private int currentLevelIndex = 0;
    private int score = 0, comboMultiplier = 0, matchStreak = 0, matchedPairs = 0;
    private bool isChecking = false;

    private Card firstFlipped, secondFlipped;
    private List<Card> spawnedCards = new();

    void Start()
    {
        comboText.gameObject.SetActive(false);
        gamePanel.SetActive(false);
        musicSource.mute = PlayerPrefs.GetInt("MusicEnabled", 1) == 0;
        if (!PlayerPrefs.HasKey("MaxUnlockedLevel"))
        {
            PlayerPrefs.SetInt("MaxUnlockedLevel", 0);
            PlayerPrefs.Save();
        }

        if (TryLoadGame(out SaveData data))
        {
            LoadLevel(data);
            gamePanel.SetActive(true);
        }
        else
        {
            ShowLevelSelect();
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

    public void ShowLevelSelect()
    {
        levelSelectPanel.SetActive(true);

        foreach (Transform child in levelButtonParent)
            Destroy(child.gameObject);

        int maxUnlocked = PlayerPrefs.GetInt("MaxUnlockedLevel", 0);

        for (int i = 0; i < levels.Count; i++)
        {
            int levelIndex = i;
            GameObject btnObj = Instantiate(levelButtonPrefab, levelButtonParent);
            var text = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            var btn = btnObj.GetComponent<Button>();
            var lockIcon = btnObj.transform.Find("LockImage");

            bool unlocked = i <= maxUnlocked;
            btn.interactable = unlocked;
            text.text = $"Level {i + 1}";
            if (lockIcon != null) lockIcon.gameObject.SetActive(!unlocked);

            if (unlocked)
            {
                int capturedIndex = levelIndex;
                btn.onClick.AddListener(() => SelectLevel(capturedIndex));
            }
        }
    }

    public void SelectLevel(int levelIndex)
    {
        currentLevelIndex = levelIndex;
        PlayerPrefs.SetInt("LastPlayedLevel", levelIndex);
        PlayerPrefs.DeleteKey("SaveData");
        PlayerPrefs.Save();

        levelSelectPanel.SetActive(false);
        gamePanel.SetActive(true);
        GenerateGrid();
    }

    void GenerateGrid()
    {
        matchedPairs = 0;
        ClearBoard();

        LevelData level = levels[currentLevelIndex];
        gridSize = level.gridSize;
        Sprite[] faces = level.levelCardFaces;

        levelText.text = $"Level: {currentLevelIndex + 1}";
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = gridSize.x;

        int total = gridSize.x * gridSize.y;
        int pairs = total / 2;

        if (faces.Length < pairs)
        {
            Debug.LogError("Not enough card face sprites assigned!");
            return;
        }

        List<int> pairIDs = new();
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
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }

    void OnCardClick(Card clicked)
    {
        sfxSource.PlayOneShot(flipClip);
        
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
            sfxSource.PlayOneShot(matchClip);
            comboText.gameObject.SetActive(true);
            comboText.text = "Combo X: " + comboMultiplier;
            yield return new WaitForSeconds(1f);
            comboText.gameObject.SetActive(false);
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
            StartCoroutine(ShowLevelComplete());
        }

        firstFlipped = secondFlipped = null;
        isChecking = false;
    }

    IEnumerator ShowLevelComplete()
    {
        int newUnlock = currentLevelIndex + 1;
        int saved = PlayerPrefs.GetInt("MaxUnlockedLevel", 0);

        if (newUnlock > saved)
        {
            PlayerPrefs.SetInt("MaxUnlockedLevel", newUnlock);
            PlayerPrefs.Save();
        }

        levelCompletePanel.SetActive(true);
        yield return new WaitForSeconds(2);
        levelCompletePanel.SetActive(false);

        matchStreak = 0;
        comboMultiplier = 1;

        currentLevelIndex++;
        if (currentLevelIndex < levels.Count)
        {
            GenerateGrid();
            SaveGame();
        }
        else
        {
            winPanel.SetActive(true);
            finalScoreText.text = "Final Score: " + score;
            sfxSource.PlayOneShot(winClip);
            PlayerPrefs.DeleteKey("SaveData");
        }
    }

    void ClearBoard()
    {
        if (!cardsParent) return;
        foreach (Transform t in cardsParent) Destroy(t.gameObject);
        spawnedCards.Clear();
        firstFlipped = secondFlipped = null;
    }

    IEnumerator PreviewCards()
    {
        foreach (var card in spawnedCards)
        {
            card.Flip();
            card.cardButton.interactable = false;
        }

        yield return new WaitForSeconds(1.5f);

        foreach (var card in spawnedCards)
        {
            card.ResetCard();
            card.cardButton.interactable = true;
        }
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

        levelText.text = $"Level: {currentLevelIndex + 1}";
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = gridSize.x;

        Sprite[] faces = level.levelCardFaces;
        for (int i = 0; i < data.cardStates.Count; i++)
        {
            var obj = Instantiate(cardPrefab, cardsParent);
            var card = obj.GetComponent<Card>();
            card.cardID = data.cardStates[i].cardID;
            card.Init(card.cardID, faces[card.cardID]);

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

        StartCoroutine(PreviewCards());
        UpdateScore();
    }
    public void ToggleMusic(bool isOn)
    {
        musicSource.mute = !isOn;
        PlayerPrefs.SetInt("MusicEnabled", isOn ? 1 : 0);
    }
    public void BackToLevelSelect()
    {
        ClearBoard();
        ShowLevelSelect();
        gamePanel.SetActive(false);
        PlayerPrefs.DeleteKey("SaveData");
    }

    public void RestartGame()
    {
        PlayerPrefs.DeleteAll();
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    void OnApplicationQuit() => SaveGame();
}
