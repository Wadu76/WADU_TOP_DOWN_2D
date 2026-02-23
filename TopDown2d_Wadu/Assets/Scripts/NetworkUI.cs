using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI; //如果用的是旧版Button
using UnityEngine.UIElements; //通常建议用Button组件的 OnClick

public class NetworkUI : MonoBehaviour
{
    // 在编辑器里把两个按钮拖进去
    public UnityEngine.UI.Button hostBtn;
    public UnityEngine.UI.Button clientBtn;

    void Start()
    {
        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            HideUI();
        });

        clientBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            HideUI();
        });
    }

    void HideUI()
    {
        //隐藏挂载这个脚本的整个物体
        //或者可以显式地隐藏按钮： hostBtn.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }
}