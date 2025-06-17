using System.Collections;
using System.Collections.Generic;
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
    void Start()
    {
        GenerateGrid();
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
            card.Init(pairIDs[i],cardFaces[pairIDs[i]]);
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

    void OnCardClick(Card clickedCard)
    {
        if(isChecking || clickedCard == firstFlipped || clickedCard == secondFlipped)
            return;
        clickedCard.Flip();

        if (firstFlipped == null)
        {
            firstFlipped = clickedCard;
        }
        else
        {
            secondFlipped = clickedCard;
            StartCoroutine(CheckMatch());
        }
    }

    IEnumerator CheckMatch()
    {
        isChecking = true;
        yield return new WaitForSeconds(1f);

        if (firstFlipped.cardID == secondFlipped.cardID)
        {
            firstFlipped.cardButton.interactable = false;
            secondFlipped.cardButton.interactable = false; 
        }
        else
        {
            firstFlipped.ResetCard();
            secondFlipped.ResetCard();
        }
        
        firstFlipped = null;
        secondFlipped = null;
        isChecking = false;
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

}
