using UnityEngine;
using UnityEngine.UI;
//该脚本只能在mainmenu上改角色颜色，实际上还要另一个脚本改真正的角色颜色
public class CharacterSelectUI : MonoBehaviour
{
    [Header("UI Images")]
    public Image previewBody; //拖入本体的Image
    public Image previewGun;  //拖入炮筒的Image

    [Header("Colors)")]
    public Color[] bodyColors; //比如：红、蓝、绿
    public Color[] barrelColors;  //比如：深红、深蓝、深绿

    private int currentIndex = 0;

    void Start()
    {
        //游戏启动时，读取玩家上次选的颜色（默认是0）
        currentIndex = PlayerPrefs.GetInt("PlayerColorIndex", 0);
        UpdatePreview();
    }

    //绑定给 > 按钮
    public void NextColor()
    {
        currentIndex++;
        if (currentIndex >= bodyColors.Length) currentIndex = 0; //循环
        UpdatePreview();
    }

    //绑定给 < 按钮
    public void PrevColor()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = bodyColors.Length - 1; //循环
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        //更新UI预览的颜色
        previewBody.color = bodyColors[currentIndex];
        previewGun.color = barrelColors[currentIndex];

        //把玩家选的编号存在电脑本地 这样就能继承上一次的颜色
        PlayerPrefs.SetInt("PlayerColorIndex", currentIndex);
        PlayerPrefs.Save();
    }
}