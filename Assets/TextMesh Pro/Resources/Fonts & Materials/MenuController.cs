using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject playMenuPanel;
    public GameObject highScoreMenuPanel;

    [Header("High Score UI")]
    public GameObject highScoreUIPrefab;   // prefab row with TMP_Text children "Name" and "Score"
    public Transform highScoreList;        // parent container (Vertical/Horizontal Layout Group)

    [Header("Tabs")]
    public GameObject easyTab;
    public GameObject normalTab;
    public GameObject hardTab;

    [Header("Colors")]
    public Color activeColor = Color.white;
    public Color inactiveColor = Color.gray;

    void Awake()
    {
        playMenuPanel.SetActive(false);
        highScoreMenuPanel.SetActive(false);
    }

    void Start()
    {
        int savedDiff = PlayerPrefs.GetInt("Difficulty", (int)Difficulty.Medium);
        SwitchHighScoreTab(savedDiff);
    }

    public void SwitchHighScoreTab(int difficulty)
    {
        // ✅ Always reload fresh from PlayerPrefs
        HighScores highScores = HighScoreHelper.LoadHighScores((Difficulty)difficulty);

        ChangeTabLabel((Difficulty)difficulty);
        ChangeHighScoreList(highScores);
    }

    public void PlayGame(int difficulty)
    {
        PlayerPrefs.SetInt("Difficulty", difficulty);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    private void ForceOpaque(Image img, Color c)
    {
        c.a = 1f;              // force alpha to 1
        img.color = c;
    }

    private void ChangeTabLabel(Difficulty difficulty)
    {
        ForceOpaque(easyTab.GetComponent<Image>(), inactiveColor);
        ForceOpaque(normalTab.GetComponent<Image>(), inactiveColor);
        ForceOpaque(hardTab.GetComponent<Image>(), inactiveColor);

        switch (difficulty)
        {
            case Difficulty.Easy: ForceOpaque(easyTab.GetComponent<Image>(), activeColor); break;
            case Difficulty.Medium: ForceOpaque(normalTab.GetComponent<Image>(), activeColor); break;
            case Difficulty.Hard: ForceOpaque(hardTab.GetComponent<Image>(), activeColor); break;
        }
    }


    private void ChangeHighScoreList(HighScores highScores)
    {
        // Clear old children
        foreach (Transform child in highScoreList)
        {
            Destroy(child.gameObject);
        }

        if (highScores == null || highScores.entryList == null || highScores.entryList.Count == 0)
        {
            Debug.LogWarning("⚠ No high scores found for this difficulty yet.");
            return;
        }

        // ✅ Sort high scores (highest → lowest)
        highScores.entryList.Sort((a, b) => b.score.CompareTo(a.score));

        foreach (ScoreEntry highScore in highScores.entryList)
        {
            GameObject hs = Instantiate(highScoreUIPrefab, highScoreList);
            TMP_Text[] childs = hs.GetComponentsInChildren<TMP_Text>();

            foreach (TMP_Text text in childs)
            {
                if (text.gameObject.name == "Name")
                    text.text = highScore.userName;
                else if (text.gameObject.name == "Score")
                    text.text = highScore.score.ToString();
            }
        }
    }
}
