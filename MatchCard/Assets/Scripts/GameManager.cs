using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform cardsParent;
    public Sprite[] cardFaces;
    
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
            spawnedCards.Add(card);
        }
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
        clickedCard.Flip();
    }
    
}
