using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CardManager : MonoBehaviour
{
    [SerializeField] Sprite[] sprites;
    [SerializeField] card cardPrefab;
    [SerializeField] Transform gridTransform;

    [Header("Levels")]
    public List<LevelConfig> levels;
    public int currentLevelIndex = 0;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip flipClip, matchClip, mismatchClip, gameOverClip;

    private CardGridLayout cardGridLayout;   // custom layout now
    private List<Sprite> spritePairs;
    private List<card> openCards = new List<card>();
    private int score;

    [System.Serializable]
    public class LevelConfig
    {
        public string levelName;
        public int rows;
        public int cols;
    }

    private void Start()
    {
        // make sure we have our custom layout
        cardGridLayout = gridTransform.GetComponent<CardGridLayout>();
        if (cardGridLayout == null)
            cardGridLayout = gridTransform.gameObject.AddComponent<CardGridLayout>();

        LoadLevel(currentLevelIndex);
    }

    public void LoadLevel(int levelIndex)
    {
        // clear old cards
        foreach (Transform child in gridTransform)
            Destroy(child.gameObject);

        currentLevelIndex = Mathf.Clamp(levelIndex, 0, levels.Count - 1);
        LevelConfig config = levels[currentLevelIndex];

        int totalCards = config.rows * config.cols;
        if (totalCards % 2 != 0)
        {
            Debug.LogError($"Grid {config.rows}x{config.cols} is not even! Needs an even number of cards.");
            return;
        }

        // set layout info
        cardGridLayout.rows = config.rows;
        cardGridLayout.colums = config.cols;
        cardGridLayout.spacing = new Vector2(15, 15); // tweak for aesthetics
        cardGridLayout.preferredTopPadding = 20;       // tweak for margins

        PrepareSprites(totalCards / 2);
        CreateCards(totalCards);

        score = 0;

        LayoutRebuilder.ForceRebuildLayoutImmediate(gridTransform as RectTransform);
    }

    public void SetSelected(card c)
    {
        if (!c.isSelcted)
        {
            PlaySfx(flipClip);
            c.Show();
            openCards.Add(c);
            CheckOpenCards();
        }
    }

    private void CheckOpenCards()
    {
        if (openCards.Count < 2) return;

        var a = openCards[openCards.Count - 2];
        var b = openCards[openCards.Count - 1];

        if (a.iconSprite == b.iconSprite)
        {
            PlaySfx(matchClip);
            score += 100;
            openCards.Clear();
        }
        else
        {
            StartCoroutine(HidePair(a, b));
        }
    }

    IEnumerator HidePair(card a, card b)
    {
        yield return new WaitForSeconds(0.5f);
        a.Hide();
        b.Hide();
        score = Mathf.Max(0, score - 10);
        PlaySfx(mismatchClip);
        openCards.Clear();
    }

    public void PrepareSprites(int pairCount)
    {
        spritePairs = new List<Sprite>();
        for (int i = 0; i < pairCount; i++)
        {
            spritePairs.Add(sprites[i % sprites.Length]);
            spritePairs.Add(sprites[i % sprites.Length]);
        }
        Shuffle(spritePairs);
    }

    public void Shuffle(List<Sprite> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }

    void CreateCards(int totalCards)
    {
        for (int i = 0; i < totalCards; i++)
        {
            card c = Instantiate(cardPrefab, gridTransform);
            c.SetIconSprite(spritePairs[i]);
            c.controller = this;
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridTransform as RectTransform);

        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource && clip) sfxSource.PlayOneShot(clip);
    }
}
