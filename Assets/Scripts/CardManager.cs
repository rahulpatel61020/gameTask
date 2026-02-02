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
    public Button continueButton;

    private CardGridLayout cardGridLayout;
    private List<Sprite> spritePairs;
    private List<card> openCards = new List<card>();
    private List<card> allCards = new List<card>();

    private int score;
    private int matchedPairs;
    private int totalPairs;
    private bool isChecking = false;
    private bool isProcessingMatch = false; // New flag to prevent race conditions

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

        RefreshContinueButton();

        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForEndOfFrame();
        yield return null;

        Canvas.ForceUpdateCanvases();

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
    // MOBILE LIFECYCLE (Auto-Save)
    // ============================================================

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            if (timerRunning && matchedPairs < totalPairs)
            {
                SaveProgress();
                Debug.Log("💾 Auto-saved on app pause");
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (timerRunning && matchedPairs < totalPairs)
        {
            SaveProgress();
            Debug.Log("💾 Auto-saved on app quit");
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            if (timerRunning && matchedPairs < totalPairs)
            {
                SaveProgress();
                Debug.Log("💾 Auto-saved on focus loss");
            }
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

        string levelKey = GetSaveKey(currentLevelIndex);
        PlayerPrefs.SetString(levelKey, json);
        PlayerPrefs.SetString(saveKey, json);
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

        if (globalData.isGameOver)
        {
            Debug.Log($"🔄 Level {currentLevelIndex} was completed, restarting it");
            LoadLevel(currentLevelIndex);
            return;
        }

        string levelKey = GetSaveKey(currentLevelIndex);
        if (PlayerPrefs.HasKey(levelKey))
        {
            string json = PlayerPrefs.GetString(levelKey);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            if (!data.isGameOver)
            {
                Debug.Log("📂 Resuming unfinished game");
                LoadLevel(currentLevelIndex, data);
                return;
            }
            else
            {
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

        for (int i = 0; i < levels.Count; i++)
        {
            string key = GetSaveKey(i);
            if (PlayerPrefs.HasKey(key))
                PlayerPrefs.DeleteKey(key);
        }

        PlayerPrefs.Save();
        Debug.Log("🗑 All Progress Cleared!");

        RefreshContinueButton();
    }

    public void ContinueGame()
    {
        if (!PlayerPrefs.HasKey(saveKey))
        {
            Debug.Log("⚠ No save found to continue!");
            return;
        }

        string globalJson = PlayerPrefs.GetString(saveKey);
        SaveData globalData = JsonUtility.FromJson<SaveData>(globalJson);

        int levelIndex = globalData.currentLevelIndex;

        if (globalData.isGameOver)
        {
            Debug.Log($"🔄 Level {levelIndex} was completed, restarting it");
            LoadLevel(levelIndex);
            return;
        }

        string key = GetSaveKey(levelIndex);
        if (PlayerPrefs.HasKey(key))
        {
            string json = PlayerPrefs.GetString(key);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            if (!data.isGameOver)
            {
                Debug.Log($"📂 Resuming saved game for Level {levelIndex}");
                LoadLevel(levelIndex, data);
                return;
            }
            else
            {
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

    public void RefreshContinueButton()
    {
        if (continueButton != null)
        {
            bool hasSave = PlayerPrefs.HasKey(saveKey);
            continueButton.gameObject.SetActive(hasSave);
        }
    }

    // ============================================================
    // LEVEL LOADING & GAMEPLAY
    // ============================================================

    public void LoadLevel(int levelIndex, SaveData saveData = null)
    {
        timerRunning = false;

        // Stop all coroutines to prevent race conditions
        StopAllCoroutines();

        LeanTween.cancel(gridTransform.gameObject);

        foreach (Transform child in gridTransform)
        {
            LeanTween.cancel(child.gameObject);
            Destroy(child.gameObject);
        }

        openCards.Clear();
        allCards.Clear();
        isChecking = false;
        isProcessingMatch = false;

        if (levelIndex != currentLevelIndex)
        {
            string oldLevelKey = GetSaveKey(currentLevelIndex);
            if (PlayerPrefs.HasKey(oldLevelKey))
            {
                PlayerPrefs.DeleteKey(oldLevelKey);
                Debug.Log($"🗑 Cleared in-progress save for Level {currentLevelIndex} (switching to Level {levelIndex})");
            }
        }

        currentLevelIndex = Mathf.Clamp(levelIndex, 0, levels.Count - 1);

        StartCoroutine(DelayedLoadLevel(saveData));
    }

    private IEnumerator DelayedLoadLevel(SaveData saveData)
    {
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();

        yield return null;

        if (saveData == null)
        {
            score = 0;
            matchedPairs = 0;
            moveCounter = 0;
            levelTimer = 0f;

            string key = GetSaveKey(currentLevelIndex);
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                Debug.Log($"🗑 Cleared old save for Level {currentLevelIndex}");
            }

            SaveData globalData = new SaveData();
            globalData.currentLevelIndex = currentLevelIndex;
            globalData.isGameOver = false;
            string globalJson = JsonUtility.ToJson(globalData);
            PlayerPrefs.SetString(saveKey, globalJson);
            PlayerPrefs.Save();
        }
        else
        {
            score = saveData.score;
            matchedPairs = saveData.matchedPairs;
            moveCounter = saveData.moveCounter;
            levelTimer = saveData.levelTimer;
            Debug.Log($"📂 Restored: Score={score}, Pairs={matchedPairs}, Moves={moveCounter}, Time={levelTimer:F1}s");
        }

        UpdateScoreUI();
        UpdateTimerUI();
        UpdateMoveUI();

        LevelConfig config = levels[currentLevelIndex];

        int totalCards = config.rows * config.cols;
        if (totalCards % 2 != 0)
        {
            Debug.LogError($"❌ Grid {config.rows}x{config.cols} is not even! Needs an even number of cards.");
            yield break;
        }

        totalPairs = totalCards / 2;

        cardGridLayout.rows = config.rows;
        cardGridLayout.colums = config.cols;
        cardGridLayout.spacing = new Vector2(15, 15);
        cardGridLayout.preferredTopPadding = 20;

        LayoutRebuilder.ForceRebuildLayoutImmediate(gridTransform as RectTransform);

        yield return null;

        PrepareSprites(totalPairs);

        for (int i = 0; i < totalCards; i++)
        {
            card c = Instantiate(cardPrefab, gridTransform);

            if (saveData != null && i < saveData.spriteIndexes.Count)
                c.SetIconSprite(sprites[saveData.spriteIndexes[i]]);
            else
                c.SetIconSprite(spritePairs[i]);

            c.controller = this;
            allCards.Add(c);

            c.transform.localScale = Vector3.one;

            if (saveData != null && saveData.matchedCardIndexes.Contains(i))
            {
                c.GetComponent<Button>().interactable = false;
                foreach (var img in c.GetComponentsInChildren<Image>())
                    img.enabled = false;
            }
            else
            {
                c.GetComponent<Button>().interactable = true;
                foreach (var img in c.GetComponentsInChildren<Image>())
                    img.enabled = true;
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(gridTransform as RectTransform);

        yield return null;

        Debug.Log($"✅ Loaded level: {config.levelName} ({config.rows}x{config.cols})");

        timerRunning = true;

        RefreshContinueButton();

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
        // Block input during reveal
        isChecking = true;

        foreach (var c in allCards)
            c.ShowInstant();

        yield return new WaitForSeconds(revealDuration);

        foreach (var c in allCards)
            c.Hide();

        // Wait for hide animation to complete
        yield return new WaitForSeconds(0.3f);

        // Allow input after reveal
        isChecking = false;
    }

    // ============================================================
    // CARD INTERACTION & MATCHING
    // ============================================================

    public void SetSelected(card c)
    {
        // Don't allow selection if:
        // - Currently checking/processing
        // - Card is already selected/flipped
        // - Card is already matched (not interactable)
        if (isChecking || isProcessingMatch)
            return;

        if (c.isSelcted || !c.GetComponent<Button>().interactable)
            return;

        // If we already have 2 cards open, don't allow more
        if (openCards.Count >= 2)
            return;

        // Check if this card is already in openCards (prevent double-tap)
        if (openCards.Contains(c))
            return;

        PlaySfx(flipClip);

        // Add to openCards BEFORE showing to prevent race condition
        openCards.Add(c);

        c.Show(() =>
        {
            // Check if we need to evaluate the pair
            // Only check if this card is still in openCards (wasn't cleared by a reset)
            if (openCards.Count == 2 && openCards.Contains(c))
            {
                CheckOpenCards();
            }
        });
    }

    private void CheckOpenCards()
    {
        if (openCards.Count != 2)
            return;

        // Prevent multiple checks
        if (isProcessingMatch)
            return;

        isProcessingMatch = true;

        moveCounter++;
        UpdateMoveUI();

        var a = openCards[0];
        var b = openCards[1];

        StartCoroutine(WaitForFlipThenCheck(a, b));
    }

    IEnumerator WaitForFlipThenCheck(card a, card b)
    {
        // Wait for both cards to finish flipping with timeout
        float timeout = 2f;
        float elapsed = 0f;

        while ((a.IsAnimating || b.IsAnimating) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        if (a.iconSprite == b.iconSprite)
        {
            // Match found!
            PlaySfx(matchClip);
            matchedPairs++;
            score++;
            UpdateScoreUI();

            // Mark cards as matched IMMEDIATELY
            a.GetComponent<Button>().interactable = false;
            b.GetComponent<Button>().interactable = false;

            // Clear openCards so player can flip next cards
            openCards.Clear();

            // Allow new card selections
            isProcessingMatch = false;

            // Check for game over
            bool isGameOver = (matchedPairs >= totalPairs);

            // Start animation (doesn't block new card flips)
            StartCoroutine(DestroyPair(a, b, isGameOver));
        }
        else
        {
            // No match - need to wait before allowing new flips
            StartCoroutine(HidePair(a, b));
        }
    }

    IEnumerator HidePair(card a, card b)
    {
        // Block new selections while showing mismatch
        isChecking = true;

        yield return new WaitForSeconds(0.5f);

        // Hide both cards
        a.Hide();
        b.Hide();

        PlaySfx(mismatchClip);

        // Wait for hide animation to complete
        yield return new WaitForSeconds(0.3f);

        // Clear and allow new selections
        openCards.Clear();
        isProcessingMatch = false;
        isChecking = false;
    }

    IEnumerator DestroyPair(card a, card b, bool isGameOver)
    {
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

        // Hide card images
        foreach (var img in a.GetComponentsInChildren<Image>())
            img.enabled = false;
        foreach (var img in b.GetComponentsInChildren<Image>())
            img.enabled = false;

        // Handle game over or save progress
        if (isGameOver)
        {
            GameOver();
        }
        else
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

        string levelKey = GetSaveKey(currentLevelIndex);
        if (PlayerPrefs.HasKey(levelKey))
        {
            PlayerPrefs.DeleteKey(levelKey);
            Debug.Log($"🗑 Cleared level {currentLevelIndex} save on completion");
        }

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
        if (endgamepanel != null)
            endgamepanel.SetActive(false);

        if (currentLevelIndex + 1 < levels.Count)
        {
            LoadLevel(currentLevelIndex + 1);
        }
        else
        {
            Debug.Log("🏆 All levels completed!");
        }
    }

    public void RestartGame()
    {
        if (endgamepanel != null)
            endgamepanel.SetActive(false);

        LoadLevel(currentLevelIndex, null);
    }

    public void QuitGame()
    {
        if (matchedPairs < totalPairs)
        {
            SaveProgress();
        }

        if (endgamepanel != null)
            endgamepanel.SetActive(false);

        if (panel != null)
            panel.SetActive(false);

        if (mainmenupanel != null)
            mainmenupanel.SetActive(true);

        RefreshContinueButton();
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
    // LEVEL SELECTION (Start Fresh - Ignores Saves)
    // ============================================================

    public void StartLevel(int levelIndex)
    {
        string oldLevelKey = GetSaveKey(currentLevelIndex);
        if (PlayerPrefs.HasKey(oldLevelKey))
        {
            PlayerPrefs.DeleteKey(oldLevelKey);
        }

        string newLevelKey = GetSaveKey(levelIndex);
        if (PlayerPrefs.HasKey(newLevelKey))
        {
            PlayerPrefs.DeleteKey(newLevelKey);
        }

        if (mainmenupanel != null)
            mainmenupanel.SetActive(false);

        if (panel != null)
            panel.SetActive(true);

        if (endgamepanel != null)
            endgamepanel.SetActive(false);

        LoadLevel(levelIndex, null);

        Debug.Log($"▶ Started Level {levelIndex} fresh");
    }

    public void StartEasyLevel()
    {
        StartLevel(0);
    }

    public void StartMediumLevel()
    {
        StartLevel(1);
    }

    public void StartHardLevel()
    {
        StartLevel(2);
    }

    // ============================================================
    // PUBLIC GETTERS
    // ============================================================

    public float GetGameTime()
    {
        return levelTimer;
    }

    public bool HasSaveData()
    {
        return PlayerPrefs.HasKey(saveKey);
    }
}

public enum Difficulty
{
    Easy,
    Medium,
    Hard
}