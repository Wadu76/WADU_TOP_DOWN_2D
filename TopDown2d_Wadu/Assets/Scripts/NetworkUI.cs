using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI; //如果用的是旧版Button
using UnityEngine.UIElements; //通常建议用Button组件的 OnClick
using UnityEngine.SceneManagement;//场景管理

public class NetworkUI : MonoBehaviour
{
    //两个按钮Host & Client
    public UnityEngine.UI.Button hostBtn;
    public UnityEngine.UI.Button clientBtn;

    void Start()
    {
       /*hostBtn.onClick.AddListener(() => {
            //作为房主启动
            NetworkManager.Singleton.StartHost();
            //房主带着所有人到GameScene
            //联机加载场景必须用 NetworkManager.SceneManager
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);

            //不再需要隐藏了！直接跳出三界之外
            //HideUI();
        });

        clientBtn.onClick.AddListener(() => {
            //客户端启动，同理但只需要启动跟着房主change scene就行了
            NetworkManager.Singleton.StartClient();
            //HideUI();
        });
       */
    }

    void HideUI()
    {
        //隐藏挂载这个脚本的整个物体
        //或者可以显式地隐藏按钮： hostBtn.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }
}