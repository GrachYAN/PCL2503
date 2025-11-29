using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI winnerText;
    //private LogicManager logicManager;

    public void Start()
    {
        //logicManager = FindFirstObjectByType<LogicManager>();
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void ShowGameOver(string result)
    {
        panel.SetActive(true);
        winnerText.text = $"{result}";
        Time.timeScale = 0f;
    }

    public void OnBackToMenuClicked()
    {
        // 1. 恢复时间（
        Time.timeScale = 1f;

        // 2. 如果是联机模式，断开连接
        if (Unity.Netcode.NetworkManager.Singleton != null)
        {
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
            Destroy(Unity.Netcode.NetworkManager.Singleton.gameObject); // 彻底销毁网络管理器
        }

        // 3. 加载登录场景
        SceneManager.LoadScene("LoginScene");
    }

    /*
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
    */
}