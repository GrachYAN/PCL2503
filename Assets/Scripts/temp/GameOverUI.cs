using UnityEngine;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI winnerText;
    private LogicManager logicManager;

    public void Start()
    {
        logicManager = FindFirstObjectByType<LogicManager>();
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void ShowGameOver(string result)
    {
        panel.SetActive(true);
        winnerText.text = $"{result}";
    }

    public void HideGameOver()
    {
        panel.SetActive(false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1;
        UnityEngine.SceneManagement.SceneManager.LoadScene("ChessScene");
        logicManager.Initialize();
    }
}