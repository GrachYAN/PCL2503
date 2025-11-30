using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMenuController : MonoBehaviour
{
    [Header("UI 组件")]
    public GameObject pauseMenuPanel;

    // 👇 新增：引用你的 GameOverUI 脚本
    public GameOverUI gameOverUIScript;

    [Header("游戏状态")]
    public bool isPaused = false;

    void Start()
    {
        // 强制在游戏开始时解除暂停。这修复了从其他场景加载可能带来的 TimeScale = 0 的问题。
        Time.timeScale = 1f;
        isPaused = false;
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    // 🏳️ 投降逻辑 - 修改版
    public void OnSurrenderClicked()
    {
        Debug.Log("玩家选择投降");

        // 1. 必须先恢复时间，否则 GameOver 动画可能播不出来，或者 Restart 点击无效
       // Time.timeScale = 1f;

        // 2. 关闭暂停菜单（否则两个菜单叠在一起很难看）
        pauseMenuPanel.SetActive(false);
        isPaused = false;

        // 3. 调用 GameOverUI 的方法
        if (gameOverUIScript != null)
        {
            gameOverUIScript.ShowGameOver("You Surrendered");
        }
    }
}
