using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform cardsParent;
    public Sprite[] cardFaces;

    private Card firstFlipped = null;
    private Card secondFlipped = null;
    private bool isChecking = false;
    
    [SerializeField] private Vector2Int gridSize = new Vector2Int(2,2);
    private List<Card> spawnedCards = new List<Card>();
    private List<int> cardIDs = new List<int>();

    private int score = 0;
    private int comboMultiplier = 0;
    private int matchStreak = 0;
    
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    

    void Start()
    {
        comboText.gameObject.SetActive(false);
        GenerateGrid();
        Invoke(nameof(LoadGame),0.5f);
    }

    void LoadGame()
    {
        if(!PlayerPrefs.HasKey("SaveData"))
            return;
        
        string json = PlayerPrefs.GetString("SaveData");
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        
        score = data.score;
        comboMultiplier = data.comboMultiplier;
        matchStreak = data.matchStreak;
        
        UpdateScore();

        for (int i = 0; i < data.cardStates.Count; i++)
        {
            Card card = spawnedCards[i];
            card.cardID = data.cardStates[i].cardID;
            card.Init(card.cardID,cardFaces[card.cardID]);

            if (data.cardStates[i].isMatched)
            {
                card.Flip();
                card.cardButton.interactable = false;
            }
            else
            {
                card.ResetCard();
                card.cardButton.interactable = true;
            }
        }
        Debug.Log("Game Loaded");
    }
    void GenerateGrid()
    {
       
        int totalCards = gridSize.x * gridSize.y;
        int totalPairs = totalCards / 2;
        if (cardFaces.Length < totalPairs)
        {
            Debug.LogError("Not enough card face sprites assigned!");
            return;
        }
        List<int> pairIDs = new List<int>();
        for (int i = 0; i < totalPairs; i++)
        {
            pairIDs.Add(i);
            pairIDs.Add(i);
        }
        
        Shuffle(pairIDs);
       
        for (int i = 0; i < totalCards; i++)
        {
            GameObject obj = Instantiate(cardPrefab, cardsParent);
            Card card = obj.GetComponent<Card>();
            Card capturedCard = card;
            card.Init(pairIDs[i],cardFaces[pairIDs[i]]);
            capturedCard.cardButton.onClick.AddListener(() => OnCardClick(capturedCard));
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

    void OnCardClick(Card clickedCard)
    {
        Debug.Log("Card Clicked: " + clickedCard.cardID);
        if(isChecking || clickedCard == firstFlipped || clickedCard == secondFlipped)
            return;
        clickedCard.Flip();

        if (firstFlipped == null)
        {
            firstFlipped = clickedCard;
        }
        else if (secondFlipped == null)
        {
            secondFlipped = clickedCard;
            isChecking = true;
            StartCoroutine(CheckMatch());
        }
        Debug.Log("Checking Bool: " + isChecking);
    }

    IEnumerator CheckMatch()
    {
        
        yield return new WaitForSeconds(1f);

        if (firstFlipped.cardID == secondFlipped.cardID)
        {
            firstFlipped.cardButton.interactable = false;
            secondFlipped.cardButton.interactable = false;

            matchStreak++;
            comboMultiplier = matchStreak;

            int gainedPoints = 10 * comboMultiplier;
            score += gainedPoints;
            
            Debug.Log($"Match! +{gainedPoints} points (Combo x{comboMultiplier})");
            comboText.gameObject.SetActive(true);
            comboText.text = "Combo X : " + comboMultiplier;
            yield return new WaitForSeconds(0.5f);
            comboText.gameObject.SetActive(false);
        }
        else
        {
            firstFlipped.ResetCard();
            secondFlipped.ResetCard();

            score -= 2; //for mismatch -2 points
            matchStreak = 0;
            comboMultiplier = 1;
            
            Debug.Log("Mismatch! -2 points");

        }
       
        firstFlipped = null;
        secondFlipped = null;
        isChecking = false;
        UpdateScore();

    }

    void UpdateScore()
    {
        if(scoreText != null) scoreText.text = "Score: " + score;
    }

    IEnumerator PreviewCards()
    {
        // Flip all cards face up
        foreach (var card in spawnedCards)
        {
            card.Flip();
            card.cardButton.interactable = false;
        }

        yield return new WaitForSeconds(1.5f);

        // Flip all cards face down
        foreach (var card in spawnedCards)
        {
            card.ResetCard();
            card.cardButton.interactable = true;
        }
    }

    void SaveGame()
    {
        SaveData data = new SaveData();
        data.score = score;
        data.comboMultiplier = comboMultiplier;
        data.matchStreak = matchStreak;
        
        data.cardStates = new List<CardState>();
        foreach (Card card in spawnedCards)
        {
            CardState state = new CardState
            {
                cardID = card.cardID,
                isMatched = !card.cardButton.interactable

            };
            data.cardStates.Add(state);
        }
        
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("SaveData", json);
        PlayerPrefs.Save();
        
        Debug.Log("Game Saved");
    }
    void OnApplicationQuit()
    {
        SaveGame();
    }
}
