using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;

public class UIController : MonoBehaviour
{
    public GameObject endGamePanel;
    public TMP_Text countText;
    public TMP_InputField userNameField;

    private int movesCount;

    [Header("Panels")]
    public GameObject panel;
    public GameObject endgamepanel;
    public GameObject mainmenupanel;

    void Start()
    {
        if (endGamePanel != null) endGamePanel.SetActive(false);
        if (countText != null) countText.text = "0";
    }

    public void SavePlayerName()
    {
        string name = string.IsNullOrEmpty(userNameField.text) ? "Player" : userNameField.text;
        PlayerPrefs.SetString("PlayerName", name);
        Debug.Log($"💾 Saved Player Name: {name}");
    }

    public void ActivateEndPanel()
    {
        if (endGamePanel != null) endGamePanel.SetActive(true);
    }

    public void ChangeMovesCount(int movesCount)
    {
        this.movesCount = movesCount;
        if (countText != null) countText.text = movesCount.ToString();
    }

    public void SaveHighScore()
    {
        // 🔹 Get CardManager reference
        CardManager cm = FindObjectOfType<CardManager>();

        Difficulty savedDifficulty = Difficulty.Medium; // fallback

        if (cm != null && cm.levels.Count > cm.currentLevelIndex)
        {
            savedDifficulty = cm.levels[cm.currentLevelIndex].difficulty;
        }
        else
        {
            savedDifficulty = (Difficulty)PlayerPrefs.GetInt("Difficulty", (int)Difficulty.Medium);
        }

        string userName = string.IsNullOrEmpty(userNameField.text) ? "Player" : userNameField.text;

        float gameTime = cm != null ? cm.GetGameTime() : 0f;
        int score = HighScoreHelper.CalculateHighScore(Mathf.FloorToInt(gameTime), movesCount, savedDifficulty);

        HighScores hs = HighScoreHelper.LoadHighScores(savedDifficulty);
        ScoreEntry newHighScore = new ScoreEntry(userName, score);

        HighScoreHelper.AddHighScore(hs, newHighScore);
        HighScoreHelper.SaveHighScore(hs, savedDifficulty);
        HighScoreHelper.SaveHighScore(hs, savedDifficulty);

        // 🔹 Reload scores for MenuController if it exists
        MenuController menu = FindObjectOfType<MenuController>();
        if (menu != null)
        {
            menu.SwitchHighScoreTab((int)savedDifficulty);
        }


        Debug.Log($"✅ High Score Saved! " +
                  $"Difficulty: {savedDifficulty}, " +
                  $"Player: {userName}, " +
                  $"Time: {Mathf.FloorToInt(gameTime)}s, " +
                  $"Moves: {movesCount}, " +
                  $"Score: {score}");

        if (panel != null) panel.SetActive(false);
        if (endgamepanel != null) endgamepanel.SetActive(false);
        if (mainmenupanel != null) mainmenupanel.SetActive(true);
    }


    public void QuitToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
