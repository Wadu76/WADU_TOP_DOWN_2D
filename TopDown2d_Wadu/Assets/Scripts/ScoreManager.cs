using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance;
    [Header("UI")]
    //public Text scoreboardText;
    public GameObject scoreboardPanel;
    public TextMeshProUGUI scoreboardText;

    [Header("GameOver Settings")]
    public int targetKills = 10;    //杀死10个人结束游戏
    public GameObject gameoverPanel;
    public TextMeshProUGUI winnerText;

    public bool Isgameover = false; //当为true时就可以展示游戏结束画面了

    //记录每个玩家ID对应的击杀数和死亡数
    private Dictionary<ulong, int> killsDict = new Dictionary<ulong, int>();
    private Dictionary<ulong, int> deathsDict = new Dictionary<ulong, int>();
    
    private void Awake()
    {
        //方便其他脚本随时调用 ScoreManager.Instance
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
       //检测是否按住tab，按住就显示score board
       //
       if(scoreboardText != null)
        {
            //按着tab setactive参数就为 1=true 反之为false
            //scoreboardText.gameObject.SetActive(Input.GetKey(KeyCode.Tab));
            scoreboardPanel.SetActive(Input.GetKey(KeyCode.Tab));
        }
    }

    //只有服务器有权调用这个方法来登记击杀 击杀时我们才调用
    public void RegisterKill(ulong killerId, ulong victimId)
    {
        //是否是服务器 || 游戏是否正在进行
        if (!IsServer || Isgameover) return;

        //击杀者+1 kill
        //若字典里没有该Id，就先创建键值对
        if (!killsDict.ContainsKey(killerId)) killsDict[killerId] = 0;
        //调用了就说明该Id玩家击杀数+1
        killsDict[killerId]++;

        //被击杀者+1 death 具体同上
        if (!deathsDict.ContainsKey(victimId)) deathsDict[victimId] = 0;
        deathsDict[victimId]++;

        //广播给所有客户端：触发右上角的击杀提示UI
        UpdateKillFeedClientRpc(killerId, victimId);

        //数据有变 计分板刷新
        RefreshScoreboard();


        //游戏结束 胜利判断
        if (killsDict[killerId] >= targetKills)
        {
            //人头数够多 游戏结束
            Isgameover = true;
            //宣布该玩家是赢家
            ShowGameOverClientRpc(killerId);
        }
    }

    //服务器通知所有客户端 有人被杀了
    [ClientRpc]
    private void UpdateKillFeedClientRpc(ulong killerId, ulong victimId)
    {
        if (killerId == victimId)
        {
            Debug.Log($"<color=yellow>[击杀播报]</color> 玩家 {victimId} 蠢死了（自杀）");
        }
        else
        {
            Debug.Log($"<color=red>[击杀播报]</color> 玩家 {killerId} 击杀了 玩家 {victimId} !!");
        }
    }


    private void RefreshScoreboard()
    {
        string boardText = "<color=#FFD700>\tPlayer   \t\tKills   \t\tDeaths</color>\n";
        //boardText += "--------------------------------------------------\n";

        //收集所有参与过击杀或死亡的玩家ID
        List<ulong> allPlayers = new List<ulong>();
        foreach (var id in killsDict.Keys) if (!allPlayers.Contains(id)) allPlayers.Add(id);
        foreach (var id in deathsDict.Keys) if (!allPlayers.Contains(id)) allPlayers.Add(id);
        //两个字典都弄 防止有人一个没杀一直在死 （or 一直没死一直在杀）

        //
        foreach(var id in allPlayers)
        {
            //一样的 若没有该玩家的索引 就创建一个value设为0
            int k = killsDict.ContainsKey(id) ? killsDict[id] : 0;
            //死亡表同理
            int d = deathsDict.ContainsKey(id) ? deathsDict[id] : 0;

            //给击杀数标红，给死亡数标灰
            boardText += $"\tPlayer {id}\t\t<color=red>{k}</color>\t\t<color=grey>{d}</color>\n";
        }

        //把拼好的文字广播给所有客户端
        UpdateScoreboardUIClientRpc(boardText);
    }


    //客户端接收面板信息并更新
    [ClientRpc]
    private void UpdateScoreboardUIClientRpc(string boardText)
    {
        if(scoreboardText != null)
        {
            scoreboardText.text = boardText;
        }
    }

    //客户端接收游戏结束的命令
    [ClientRpc]
    private void ShowGameOverClientRpc(ulong winnerId)
    {
        Isgameover = true; //客户端也锁死状态

        if (gameoverPanel != null)
        {
            gameoverPanel.SetActive(true); //弹出结束面板！

            if (winnerText != null)
            {
                //获胜方看到的
                if (winnerId == NetworkManager.Singleton.LocalClientId)
                {
                    winnerText.text = "          Chicken Dinner! \n";
                    winnerText.color = Color.yellow; 
                }
                else//没获胜方看到的
                {
                    winnerText.text = $"          GameOver!{winnerId} Won";
                    winnerText.color = Color.red;
                }
            }
        }
    }

    //return to menu按钮点击方法
    public void ReturnToMainMenu()
    {
        //先断开联机网络
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        //然后本地重新加载主菜单场景
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
