using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("PausePanel")]
    public GameObject pausePanel;

    public static PauseMenu Instance;
    public bool IsPaused => pausePanel.activeSelf;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (ScoreManager.Instance != null && ScoreManager.Instance.Isgameover)
                return;

            //直接取反当前的激活状态
            pausePanel.SetActive(!pausePanel.activeSelf);
        }
    }

    //给返回游戏 按钮用的函数 Back
    public void BackToGame()
    {
        pausePanel.SetActive(false); //关闭面板，IsPaused会自动变成false
    }

    //给“返回主菜单”按钮用的函数 Menu
    public void ReturnToMainMenu()
    {
        //断开联机
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        //切回大厅场景
        SceneManager.LoadScene("MainMenu");
    }

    //给“退出至桌面”按钮用的函数 Quit
    public void QuitGame()
    {
        Debug.Log("正在退出游戏...");

        //打包后，会直接杀掉游戏进程退出
        Application.Quit();

        //编辑器里点这个按钮也能停止运行
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}