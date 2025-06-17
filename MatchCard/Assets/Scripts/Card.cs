using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

public class Card : MonoBehaviour
{
    public Image frontImage;
    public Image backImage;
    public Button cardButton;

    [HideInInspector] public int cardID;
    private bool isFlipped = false;

    public void Init(int id, Sprite frontSprite)
    {
        cardID = id;
        frontImage.sprite = frontSprite;
        ResetCard();
    }

    public void Flip()
    {
        if(isFlipped) return;
        isFlipped = true;
        frontImage.gameObject.SetActive(true);
        backImage.gameObject.SetActive(false);
    }

    public void ResetCard()
    {
        isFlipped = false;
        frontImage.gameObject.SetActive(false);
        backImage.gameObject.SetActive(true);
    }
}
