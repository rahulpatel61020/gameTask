using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

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

    [Header("FX")]
    public ParticleSystem matchEffectPrefab;
    public TMP_Text scoreText;
    public TMP_Text timerText;      // UI text for timer
    public TMP_Text moveText;       // UI text for move counter

    private CardGridLayout cardGridLayout;
    private List<Sprite> spritePairs;
    private List<card> openCards = new List<card>();
    private List<card> allCards = new List<card>();

    private int score;
    private int matchedPairs;
    private int totalPairs;
    private bool isChecking = false;

    private float levelTimer;
    private bool timerRunning;
    private int moveCounter; // counts PAIRS flipped

    [System.Serializable]
    public class LevelConfig
    {
        public string levelName;
        public int rows;
        public int cols;
    }

    private void Start()
    {
        cardGridLayout = gridTransform.GetComponent<CardGridLayout>();
        if (cardGridLayout == null)
            cardGridLayout = gridTransform.gameObject.AddComponent<CardGridLayout>();

        LoadLevel(currentLevelIndex);
    }

    private void Update()
    {
        // --- Timer ---
        if (timerRunning)
        {
            levelTimer += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    public void LoadLevel(int levelIndex)
    {
        foreach (Transform child in gridTransform)
            Destroy(child.gameObject);

        openCards.Clear();
        allCards.Clear();
        score = 0;
        matchedPairs = 0;
        moveCounter = 0;
        levelTimer = 0f;
        timerRunning = true;

        UpdateScoreUI();
        UpdateTimerUI();
        UpdateMoveUI();

        currentLevelIndex = Mathf.Clamp(levelIndex, 0, levels.Count - 1);
        LevelConfig config = levels[currentLevelIndex];

        int totalCards = config.rows * config.cols;
        if (totalCards % 2 != 0)
        {
            Debug.LogError($"Grid {config.rows}x{config.cols} is not even! Needs an even number of cards.");
            return;
        }

        totalPairs = totalCards / 2;

        cardGridLayout.rows = config.rows;
        cardGridLayout.colums = config.cols;
        cardGridLayout.spacing = new Vector2(15, 15);
        cardGridLayout.preferredTopPadding = 20;

        PrepareSprites(totalPairs);
        CreateCards(totalCards);

        Debug.Log($"Loaded level: {config.levelName} ({config.rows}x{config.cols})");

        StartCoroutine(RevealAllCards());
    }

    IEnumerator RevealAllCards()
    {
        foreach (var c in allCards) c.ShowInstant();
        yield return new WaitForSeconds(1.2f);
        foreach (var c in allCards) c.Hide();
    }

    public void SetSelected(card c)
    {
        if (!c.isSelcted && !isChecking)
        {
            PlaySfx(flipClip);
            c.Show();
            openCards.Add(c);

            // Don't count move here, wait until 2nd card
            CheckOpenCards();
        }
    }

    private void CheckOpenCards()
    {
        if (openCards.Count < 2) return;

        // ✅ A pair was attempted → count as 1 move
        moveCounter++;
        UpdateMoveUI();

        var a = openCards[openCards.Count - 2];
        var b = openCards[openCards.Count - 1];

        if (!a.isSelcted) a.Show();
        if (!b.isSelcted) b.Show();

        if (a.iconSprite == b.iconSprite)
        {
            PlaySfx(matchClip);
            matchedPairs++;
            score++;
            UpdateScoreUI();
            StartCoroutine(DestroyPair(a, b));

            if (matchedPairs >= totalPairs)
            {
                GameOver();
            }
        }
        else
        {
            StartCoroutine(HidePair(a, b));
        }
    }

    IEnumerator HidePair(card a, card b)
    {
        isChecking = true;
        yield return new WaitForSeconds(0.5f);
        a.Hide();
        b.Hide();
        PlaySfx(mismatchClip);
        openCards.Clear();
        isChecking = false;
    }

    IEnumerator DestroyPair(card a, card b)
    {
        isChecking = true;

        float popDuration = 0.2f;
        float shrinkDuration = 0.3f;

        LeanTween.scale(a.gameObject, Vector3.one * 1.2f, popDuration).setEaseOutBack();
        LeanTween.scale(b.gameObject, Vector3.one * 1.2f, popDuration).setEaseOutBack();
        yield return new WaitForSeconds(popDuration + 0.05f);

        if (matchEffectPrefab && gridTransform != null)
        {
            Canvas canvas = gridTransform.GetComponentInParent<Canvas>();
            SpawnFXOnCanvas(canvas, a.transform.position);
            SpawnFXOnCanvas(canvas, b.transform.position);
        }

        yield return new WaitForSeconds(0.2f);

        LeanTween.scale(a.gameObject, Vector3.zero, shrinkDuration).setEaseInBack();
        LeanTween.scale(b.gameObject, Vector3.zero, shrinkDuration).setEaseInBack();
        yield return new WaitForSeconds(shrinkDuration);

        a.GetComponent<Button>().interactable = false;
        b.GetComponent<Button>().interactable = false;

        if (a.TryGetComponent<Image>(out var ai)) ai.enabled = false;
        if (b.TryGetComponent<Image>(out var bi)) bi.enabled = false;

        openCards.Clear();
        isChecking = false;
    }

    private void SpawnFXOnCanvas(Canvas canvas, Vector3 worldPos)
    {
        Vector3 canvasPos = WorldToCanvasPosition(canvas, worldPos);
        ParticleSystem fx = Instantiate(matchEffectPrefab, canvas.transform);
        fx.transform.localPosition = canvasPos;

        float baseCardSize = 200f;
        float scaleFactor = cardGridLayout.cardSize.x / baseCardSize;
        fx.transform.localScale = Vector3.one * scaleFactor * 80;

        fx.Play();
        Destroy(fx.gameObject, 2f);
    }

    private Vector3 WorldToCanvasPosition(Canvas canvas, Vector3 worldPosition)
    {
        Vector2 viewportPos = Camera.main.WorldToViewportPoint(worldPosition);
        Vector2 canvasSize = canvas.GetComponent<RectTransform>().sizeDelta;

        return new Vector3(
            (viewportPos.x - 0.5f) * canvasSize.x,
            (viewportPos.y - 0.5f) * canvasSize.y,
            0f
        );
    }

    // --- Utility ---
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
            allCards.Add(c);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(gridTransform as RectTransform);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource && clip) sfxSource.PlayOneShot(clip);
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(levelTimer / 60f);
            int seconds = Mathf.FloorToInt(levelTimer % 60f);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }

    private void UpdateMoveUI()
    {
        if (moveText != null)
            moveText.text = $"Moves: {moveCounter}";
    }

    // --- Game Control ---
    private void GameOver()
    {
        timerRunning = false; // stop timer

        if (currentLevelIndex + 1 < levels.Count)
            Debug.Log($"✅ LEVEL {levels[currentLevelIndex].levelName} COMPLETE!");
        else
            Debug.Log("🏆 ALL LEVELS COMPLETE! GAME OVER!");

        PlaySfx(gameOverClip);
    }

    public void NextLevel()
    {
        if (currentLevelIndex + 1 < levels.Count)
            LoadLevel(currentLevelIndex + 1);
        else
            GameOver();
    }

    public void RestartGame()
    {
        LoadLevel(currentLevelIndex);
    }

    public void QuitGame()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
