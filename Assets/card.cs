using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class card : MonoBehaviour
{
    [SerializeField]
    private Image iconimage;
    public Sprite hiddeniconSprite;
    public Sprite iconSprite;

    public CardManager controller;

    public bool isSelcted;

    public void oncardClick()
    {
        controller.SetSelected(this);

    }
    // Start is called before the first frame update
    public void SetIconSprite(Sprite sp)
    {
        iconSprite = sp;
    }

    public void Show()
    {
        if (isSelcted) return;
        isSelcted = true;
        iconimage.sprite = iconSprite;
        isSelcted = true;
        Flip(iconSprite);
    }
    public void Hide()
    {
        if (!isSelcted) return;
        iconimage.sprite = hiddeniconSprite;
        isSelcted = false;
        Flip(hiddeniconSprite);
    }

    private void Flip(Sprite newSprite)
    {
        // Animate scale X to 0 (closing)
        LeanTween.scaleX(gameObject, 0f, 0.2f).setOnComplete(() =>
        {
            // Change sprite when hidden
            iconimage.sprite = newSprite;

            // Animate scale X back to 1 (opening)
            LeanTween.scaleX(gameObject, 1f, 0.2f);
        });
    }
}
