using UnityEngine;
using UnityEngine.UI;

public class card : MonoBehaviour
{
    [SerializeField] private Image iconimage;
    public Sprite hiddeniconSprite;
    public Sprite iconSprite;

    public CardManager controller;
    public bool isSelcted;
    private bool isAnimating;

    public void SetIconSprite(Sprite sp)
    {
        iconSprite = sp;
    }

    public void oncardClick()
    {
        if (!isAnimating)
            controller.SetSelected(this);
    }

    public void Show()
    {
        if (isSelcted || isAnimating) return;
        isSelcted = true;
        Flip(iconSprite);
    }

    public void Hide()
    {
        if (!isSelcted || isAnimating) return;
        isSelcted = false;
        Flip(hiddeniconSprite);
    }

    private void Flip(Sprite newSprite)
    {
        isAnimating = true;
        LeanTween.scaleX(gameObject, 0f, 0.2f).setOnComplete(() =>
        {
            iconimage.sprite = newSprite;
            LeanTween.scaleX(gameObject, 1f, 0.2f).setOnComplete(() =>
            {
                isAnimating = false;
            });
        });
    }
}
