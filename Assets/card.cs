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

    public bool IsAnimating => isAnimating;   // ✅ expose for manager

    public void SetIconSprite(Sprite sp)
    {
        iconSprite = sp;
        if (iconimage != null)
            iconimage.sprite = hiddeniconSprite;
    }

    public void oncardClick()
    {
        if (!isAnimating)
            controller.SetSelected(this);
    }
    public void HighlightMatch(System.Action onComplete = null)
    {
        LeanTween.scale(gameObject, Vector3.one * 1.2f, 0.2f).setEasePunch().setOnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }


    public void Show(System.Action onComplete = null)
    {
        if (isSelcted || isAnimating) return;

        isSelcted = true;
        isAnimating = true;

        // Flip animation (scale X to 0 → swap sprite → scale X back)
        LeanTween.scaleX(gameObject, 0, 0.15f).setOnComplete(() =>
        {
            if (iconimage != null)
                iconimage.sprite = iconSprite;   // 👈 swap to face sprite
            iconimage.enabled = true;            // make sure it's visible

            LeanTween.scaleX(gameObject, 1, 0.15f).setOnComplete(() =>
            {
                isAnimating = false;
                onComplete?.Invoke(); // notify that flip is done
            });
        });
    }



    public void Hide()
    {
        if (!isSelcted || isAnimating) return;

        isSelcted = false;
        Flip(hiddeniconSprite);  // 👈 flip back to hidden sprite
    }

    public void ShowInstant()
    {
        isSelcted = true;
        iconimage.sprite = iconSprite; // immediately show the face
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
