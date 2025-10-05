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
    public GameObject endgamepanel;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip flipClip, matchClip, mismatchClip, gameOverClip;

    [Header("FX")]
    public ParticleSystem matchEffectPrefab;
    public TMP_Text scoreText;
    public TMP_Text timerText;
    public TMP_Text moveText;

    [Header("UI Panels")]
    public GameObject panel;
    public GameObject mainmenupanel;

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
    private int moveCounter;

    private string saveKey = "CardGameSave";

    [System.Serializable]
    public class LevelConfig
    {
        public string levelName;
        public int rows;
        public int cols;
        public Difficulty difficulty;
    }

    [System.Serializable]
    public class SaveData
    {
        public int currentLevelIndex;
        public float levelTimer;
        public int moveCounter;
        public int score;
        public int matchedPairs;
        public List<int> matchedCardIndexes = new List<int>();
        public List<int> spriteIndexes = new List<int>();
        public bool isGameOver;
    }

    private void Start()
    {
        cardGridLayout = gridTransform.GetComponent<CardGridLayout>();
        if (cardGridLayout == null)
            cardGridLayout = gridTransform.gameObject.AddComponent<CardGridLayout>();

        if (PlayerPrefs.HasKey(saveKey))
            LoadProgress();
        else
            LoadLevel(currentLevelIndex);
    }

    private void Update()
    {
        if (timerRunning)
        {
            levelTimer += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    // ============================================================
    // SAVE / LOAD SYSTEM
    // ============================================================

    public void SaveProgress()
    {
        SaveData data = new SaveData();
        data.currentLevelIndex = currentLevelIndex;
        data.levelTimer = levelTimer;
        data.moveCounter = moveCounter;
        data.score = score;
        data.matchedPairs = matchedPairs;
        data.isGameOver = false;

        data.matchedCardIndexes = new List<int>();
        data.spriteIndexes = new List<int>();

        for (int i = 0; i < allCards.Count; i++)
        {
            int spriteIndex = System.Array.IndexOf(sprites, allCards[i].iconSprite);
            data.spriteIndexes.Add(spriteIndex);

            if (!allCards[i].GetComponent<Button>().interactable)
                data.matchedCardIndexes.Add(i);
        }

        string json = JsonUtility.ToJson(data);
        string key = GetSaveKey(currentLevelIndex);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();

        Debug.Log($"💾 Progress Saved for Level {currentLevelIndex}");
    }

    public void LoadProgress()
    {
        if (!PlayerPrefs.HasKey(saveKey))
        {
            Debug.Log("⚠ No global save found!");
            LoadLevel(currentLevelIndex);
            return;
        }

        string globalJson = PlayerPrefs.GetString(saveKey);
        SaveData globalData = JsonUtility.FromJson<SaveData>(globalJson);

        currentLevelIndex = globalData.currentLevelIndex;

        // Try per-level save
        string levelKey = GetSaveKey(currentLevelIndex);
        if (PlayerPrefs.HasKey(levelKey))
        {
            string json = PlayerPrefs.GetString(levelKey);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // ✅ Only resume if game is NOT over
            if (!data.isGameOver)
            {
                Debug.Log("📂 Resuming unfinished game");
                LoadLevel(currentLevelIndex, data);
                return;
            }
            else
            {
                // ✅ Level was completed - clear it and start fresh
                Debug.Log("🔄 Level was completed previously, starting fresh");
                PlayerPrefs.DeleteKey(levelKey);
            }
        }

        Debug.Log("▶ Starting fresh");
        LoadLevel(currentLevelIndex);
    }

    public void ClearProgress()
    {
        PlayerPrefs.DeleteKey(saveKey);

        // ✅ Clear all level-specific saves
        for (int i = 0; i < levels.Count; i++)
        {
            string key = GetSaveKey(i);
            if (PlayerPrefs.HasKey(key))
                PlayerPrefs.DeleteKey(key);
        }

        PlayerPrefs.Save();
        Debug.Log("🗑 All Progress Cleared!");
    }

    public void ContinueGame(int levelIndex)
    {
        string key = GetSaveKey(levelIndex);

        if (PlayerPrefs.HasKey(key))
        {
            string json = PlayerPrefs.GetString(key);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // ✅ Only resume if NOT completed
            if (!data.isGameOver && data.currentLevelIndex == levelIndex)
            {
                Debug.Log($"📂 Resuming saved game for Level {levelIndex}");
                LoadLevel(levelIndex, data);
                return;
            }
            else if (data.isGameOver)
            {
                // ✅ Level was completed - clear it and start fresh
                Debug.Log($"🔄 Level {levelIndex} was completed, starting fresh");
                PlayerPrefs.DeleteKey(key);
            }
        }

        Debug.Log($"▶ Starting fresh for Level {levelIndex}");
        LoadLevel(levelIndex);
    }

    private string GetSaveKey(int levelIndex)
    {
        return $"CardGameSave_Level_{levelIndex}";
    }

    // ============================================================
    // LEVEL LOADING & GAMEPLAY
    // ============================================================

    public void LoadLevel(int levelIndex, SaveData saveData = null)
    {
        // ✅ Stop timer immediately
        timerRunning = false;

        // ✅ Cancel all LeanTween animations on the grid
        LeanTween.cancel(gridTransform.gameObject);

        // Clear existing cards completely
        foreach (Transform child in gridTransform)
        {
            // Cancel any ongoing animations on cards
            LeanTween.cancel(child.gameObject);
            Destroy(child.gameObject);
        }

        openCards.Clear();
        allCards.Clear();

        // ✅ Reset checking state
        isChecking = false;

        // Initialize or restore game state
        if (saveData == null)
        {
            // Fresh start
            score = 0;
            matchedPairs = 0;
            moveCounter = 0;
            levelTimer = 0f;

            // ✅ Clear any existing save for this level
            string key = GetSaveKey(levelIndex);
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                Debug.Log($"🗑 Cleared old save for Level {levelIndex}");
            }
        }
        else
        {
            // ✅ Restore saved state
            score = saveData.score;
            matchedPairs = saveData.matchedPairs;
            moveCounter = saveData.moveCounter;
            levelTimer = saveData.levelTimer;
            Debug.Log($"📂 Restored: Score={score}, Pairs={matchedPairs}, Moves={moveCounter}, Time={levelTimer:F1}s");
        }

        // Update UI
        UpdateScoreUI();
        UpdateTimerUI();
        UpdateMoveUI();

        // Load level config
        currentLevelIndex = Mathf.Clamp(levelIndex, 0, levels.Count - 1);
        LevelConfig config = levels[currentLevelIndex];

        int totalCards = config.rows * config.cols;
        if (totalCards % 2 != 0)
        {
            Debug.LogError($"❌ Grid {config.rows}x{config.cols} is not even! Needs an even number of cards.");
            return;
        }

        totalPairs = totalCards / 2;

        // Setup grid layout
        cardGridLayout.rows = config.rows;
        cardGridLayout.colums = config.cols;
        cardGridLayout.spacing = new Vector2(15, 15);
        cardGridLayout.preferredTopPadding = 20;

        // Prepare sprites
        PrepareSprites(totalPairs);

        // Create cards
        for (int i = 0; i < totalCards; i++)
        {
            card c = Instantiate(cardPrefab, gridTransform);

            // Assign sprite
            if (saveData != null && i < saveData.spriteIndexes.Count)
                c.SetIconSprite(sprites[saveData.spriteIndexes[i]]);
            else
                c.SetIconSprite(spritePairs[i]);

            c.controller = this;
            allCards.Add(c);

            // ✅ Reset scale to normal
            c.transform.localScale = Vector3.one;

            // Apply saved state (matched or unmatched)
            if (saveData != null && saveData.matchedCardIndexes.Contains(i))
            {
                // Matched card - disable and hide
                c.GetComponent<Button>().interactable = false;
                foreach (var img in c.GetComponentsInChildren<Image>())
                    img.enabled = false;
            }
            else
            {
                // Unmatched card - enable and show
                c.GetComponent<Button>().interactable = true;
                foreach (var img in c.GetComponentsInChildren<Image>())
                    img.enabled = true;
            }
        }

        Debug.Log($"✅ Loaded level: {config.levelName} ({config.rows}x{config.cols})");

        // ✅ Start timer after setup
        timerRunning = true;

        // Show reveal animation only for fresh games
        if (saveData == null)
            StartCoroutine(DelayedReveal(config));
    }

    private IEnumerator DelayedReveal(LevelConfig config)
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(gridTransform as RectTransform);

        switch (config.difficulty)
        {
            case Difficulty.Easy:
                StartCoroutine(RevealAllCards(2f));
                break;
            case Difficulty.Medium:
                StartCoroutine(RevealAllCards(1.2f));
                break;
            case Difficulty.Hard:
                StartCoroutine(RevealAllCards(0.7f));
                break;
        }
    }

    IEnumerator RevealAllCards(float revealDuration = 1.2f)
    {
        foreach (var c in allCards)
            c.ShowInstant();

        yield return new WaitForSeconds(revealDuration);

        foreach (var c in allCards)
            c.Hide();
    }

    // ============================================================
    // CARD INTERACTION & MATCHING
    // ============================================================

    public void SetSelected(card c)
    {
        if (!c.isSelcted && !isChecking)
        {
            PlaySfx(flipClip);
            c.Show(() =>
            {
                openCards.Add(c);
                CheckOpenCards();
            });
        }
    }

    private void CheckOpenCards()
    {
        if (openCards.Count < 2)
            return;

        moveCounter++;
        UpdateMoveUI();

        var a = openCards[openCards.Count - 2];
        var b = openCards[openCards.Count - 1];

        StartCoroutine(WaitForFlipThenCheck(a, b));
    }

    IEnumerator WaitForFlipThenCheck(card a, card b)
    {
        yield return new WaitUntil(() => !a.IsAnimating && !b.IsAnimating);
        yield return new WaitForSeconds(0.1f);

        if (a.iconSprite == b.iconSprite)
        {
            // Match found!
            PlaySfx(matchClip);
            matchedPairs++;
            score++;
            UpdateScoreUI();
            StartCoroutine(DestroyPair(a, b));

            if (matchedPairs >= totalPairs)
                GameOver();
        }
        else
        {
            // No match
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

        // Pop animation
        LeanTween.scale(a.gameObject, Vector3.one * 1.2f, popDuration).setEaseOutBack();
        LeanTween.scale(b.gameObject, Vector3.one * 1.2f, popDuration).setEaseOutBack();
        yield return new WaitForSeconds(popDuration + 0.05f);

        // Spawn particle effects
        if (matchEffectPrefab && gridTransform != null)
        {
            Canvas canvas = gridTransform.GetComponentInParent<Canvas>();
            SpawnFXOnCanvas(canvas, a.transform.position);
            SpawnFXOnCanvas(canvas, b.transform.position);
        }

        yield return new WaitForSeconds(0.2f);

        // Shrink animation
        LeanTween.scale(a.gameObject, Vector3.zero, shrinkDuration).setEaseInBack();
        LeanTween.scale(b.gameObject, Vector3.zero, shrinkDuration).setEaseInBack();
        yield return new WaitForSeconds(shrinkDuration);

        // Disable cards
        a.GetComponent<Button>().interactable = false;
        b.GetComponent<Button>().interactable = false;

        foreach (var img in a.GetComponentsInChildren<Image>())
            img.enabled = false;
        foreach (var img in b.GetComponentsInChildren<Image>())
            img.enabled = false;

        openCards.Clear();
        isChecking = false;

        // Check if this was the last pair before saving
        bool wasLastPair = (matchedPairs >= totalPairs);

        // Save progress after each match (only if game not over)
        if (!wasLastPair)
        {
            SaveProgress();
        }
    }

    // ============================================================
    // VISUAL EFFECTS
    // ============================================================

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

    // ============================================================
    // SPRITE MANAGEMENT
    // ============================================================

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

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(gridTransform as RectTransform);
    }

    // ============================================================
    // AUDIO & UI UPDATES
    // ============================================================

    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource && clip)
            sfxSource.PlayOneShot(clip);
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

    // ============================================================
    // GAME CONTROL
    // ============================================================

    private void GameOver()
    {
        timerRunning = false;
        PlaySfx(gameOverClip);

        // ✅ CRITICAL: Delete level-specific save FIRST
        string levelKey = GetSaveKey(currentLevelIndex);
        if (PlayerPrefs.HasKey(levelKey))
        {
            PlayerPrefs.DeleteKey(levelKey);
            Debug.Log($"🗑 Cleared level {currentLevelIndex} save on completion");
        }

        // Show end game panel
        if (endgamepanel != null)
        {
            endgamepanel.SetActive(true);
            RectTransform rt = endgamepanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(-1200f, 0f);
                LeanTween.moveX(rt, 0f, 0.6f).setEaseOutExpo();
            }
        }

        // Save completion to global save
        SaveData data = new SaveData();
        data.currentLevelIndex = currentLevelIndex;
        data.levelTimer = levelTimer;
        data.moveCounter = moveCounter;
        data.score = score;
        data.matchedPairs = matchedPairs;
        data.isGameOver = true;

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();

        Debug.Log($"🎉 Game Over - Level {currentLevelIndex} completed!");
    }

    public void NextLevel()
    {
        if (currentLevelIndex + 1 < levels.Count)
            LoadLevel(currentLevelIndex + 1);
        else
            Debug.Log("🏆 All levels completed!");
    }

    public void RestartGame()
    {
        // ✅ Pass null to force fresh start with new shuffled cards
        LoadLevel(currentLevelIndex, null);
    }

    public void QuitGame()
    {
        if (panel != null)
            panel.SetActive(false);

        if (mainmenupanel != null)
            mainmenupanel.SetActive(true);
    }

    public void ExitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ============================================================
    // PUBLIC GETTERS
    // ============================================================

    public float GetGameTime()
    {
        return levelTimer;
    }
}

public enum Difficulty
{
    Easy,
    Medium,
    Hard
}