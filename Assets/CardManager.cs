using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [SerializeField] Sprite[] sprites;
    [SerializeField] card cardPrefab;
    [SerializeField]
    Transform gridTransform;
    card firstSelected;
    card secondSlected;
    private List<Sprite> spritePairs;

    private void Start()
    {
        PrepareSprites();
        CreateCards();

    }
    public void SetSelected(card card)
    {
        if (card.isSelcted == false)
        {
            card.Show();
            if (firstSelected == null)
            {
                firstSelected = card;
                return;
            }
            if (secondSlected == null)
            {
                secondSlected = card;
                StartCoroutine(checkMatch(firstSelected,secondSlected));
                firstSelected=null;
                secondSlected=null;
            }
        }
    }
    IEnumerator checkMatch(card a, card b)
    {
        yield return new WaitForSeconds(.5f);
        if (a.iconSprite == b.iconSprite)
        {

        }
        else
        {
            a.Hide();
            b.Hide();
        }
    }
            public void PrepareSprites()
            {
                spritePairs = new List<Sprite>();
                for (int i = 0; i < sprites.Length; i++)
                {
                    spritePairs.Add(sprites[i]);
                    spritePairs.Add(sprites[i]);
                }
                Shuffle(spritePairs);
            }
    public void Shuffle(List<Sprite> spriteList)
    {
        for (int i = spriteList.Count - 1; i >= 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Sprite temp = spriteList[i];
            spriteList[i] = spriteList[randomIndex];
            spriteList[randomIndex] = temp;
        }
    }
    void CreateCards()
    {
        for (int i = 0; i < spritePairs.Count; i++)
        {
            card card = Instantiate(cardPrefab, gridTransform);
            card.SetIconSprite(spritePairs[i]);
            card.controller = this;
        }
    }
}
