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
    public bool IsAnimating => isAnimating;

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
        // Don't show if already showing or animating
        if (isSelcted || isAnimating) return;

        isSelcted = true;
        isAnimating = true;

        // Cancel any existing animations on this card
        LeanTween.cancel(gameObject);

        // Flip animation (scale X to 0 → swap sprite → scale X back)
        LeanTween.scaleX(gameObject, 0, 0.15f).setOnComplete(() =>
        {
            if (iconimage != null)
                iconimage.sprite = iconSprite;

            if (iconimage != null)
                iconimage.enabled = true;

            LeanTween.scaleX(gameObject, 1, 0.15f).setOnComplete(() =>
            {
                isAnimating = false;
                onComplete?.Invoke();
            });
        });
    }

    public void Hide()
    {
        // Only hide if card is selected (showing face)
        if (!isSelcted) return;

        // If already animating, force stop and reset
        if (isAnimating)
        {
            LeanTween.cancel(gameObject);
            isAnimating = false;
        }

        isSelcted = false;
        Flip(hiddeniconSprite);
    }

    /// <summary>
    /// Force hide the card without animation (for cleanup)
    /// </summary>
    public void ForceHide()
    {
        LeanTween.cancel(gameObject);
        isSelcted = false;
        isAnimating = false;

        if (iconimage != null)
            iconimage.sprite = hiddeniconSprite;

        transform.localScale = Vector3.one;
    }

    public void ResetVisuals()
    {
        // Cancel any animations
        LeanTween.cancel(gameObject);

        // Always start hidden & interactable
        isSelcted = false;
        isAnimating = false;

        var btn = GetComponent<Button>();
        if (btn != null)
            btn.interactable = true;

        var images = GetComponentsInChildren<Image>(true);
        foreach (var img in images)
            img.enabled = true;

        if (iconimage != null)
            iconimage.sprite = hiddeniconSprite;

        transform.localScale = Vector3.one;
    }

    public void ShowInstant()
    {
        // Cancel any existing animations
        LeanTween.cancel(gameObject);

        isSelcted = true;
        isAnimating = false;

        if (iconimage != null)
            iconimage.sprite = iconSprite;

        transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Hide instantly without animation
    /// </summary>
    public void HideInstant()
    {
        LeanTween.cancel(gameObject);

        isSelcted = false;
        isAnimating = false;

        if (iconimage != null)
            iconimage.sprite = hiddeniconSprite;

        transform.localScale = Vector3.one;
    }

    private void Flip(Sprite newSprite)
    {
        isAnimating = true;

        // Cancel any existing animations first
        LeanTween.cancel(gameObject);

        // Make sure scale X starts at 1
        Vector3 currentScale = transform.localScale;
        currentScale.x = 1f;
        transform.localScale = currentScale;

        LeanTween.scaleX(gameObject, 0f, 0.15f).setOnComplete(() =>
        {
            if (iconimage != null)
                iconimage.sprite = newSprite;

            LeanTween.scaleX(gameObject, 1f, 0.15f).setOnComplete(() =>
            {
                isAnimating = false;
            });
        });
    }

    private void OnDisable()
    {
        // Clean up when card is disabled
        LeanTween.cancel(gameObject);
        isAnimating = false;
    }
}