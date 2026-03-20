using UnityEngine;
using TMPro; // 引入 UI 命名空间

public class ShowRoomCode : MonoBehaviour
{
    void Start()
    {
        //如果房间码不是空的，就显示出来
        if (!string.IsNullOrEmpty(RelayManager.RoomCode))
        {
            GetComponent<TextMeshProUGUI>().text = "RM: " + RelayManager.RoomCode;
            GetComponent<TextMeshProUGUI>().color = Color.magenta;
        }
        else
        {
            //如果是客户端，可以选择隐藏这个字，或者显示为"客户端"
            GetComponent<TextMeshProUGUI>().text = "已加入房间";
        }
    }
}