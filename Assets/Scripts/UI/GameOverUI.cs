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
        GameNotificationManager notificationManager = GameNotificationManager.Instance != null
            ? GameNotificationManager.Instance
            : FindFirstObjectByType<GameNotificationManager>();

        if (notificationManager != null)
        {
            notificationManager.ClearDamageTexts();
        }

        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (winnerText != null)
        {
            winnerText.text = $"{result}";
        }

        Time.timeScale = 0f;
    }

    public void OnBackToMenuClicked()
    {
        Time.timeScale = 1f;

        Unity.Netcode.NetworkManager networkManager = Unity.Netcode.NetworkManager.Singleton;
        if (networkManager != null)
        {
            networkManager.Shutdown();
            Destroy(networkManager.gameObject);
        }

        SceneLoadGuard.TryLoadScene(ProjectSceneNames.Login, resetTimeScale: true);
    }
}
