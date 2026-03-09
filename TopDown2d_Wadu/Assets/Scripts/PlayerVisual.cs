using UnityEngine;
using Unity.Netcode;

public class PlayerVisual : NetworkBehaviour
{
    [Header("changing part")]
    public SpriteRenderer bodySprite; // 拖入Body的 SpriteRenderer
    public SpriteRenderer gunSprite;  // 拖入Barrel的 SpriteRenderer

    //必须和 MainMenu 里填的一模一样
    [Header("color choice")]
    public Color[] bodyColors;
    public Color[] gunColors;

    //网络变量 允许 Owner（玩家自己）去修改它！
    //只要玩家一修改，全服所有人的画面都会自动同步变色
    public NetworkVariable<int> colorIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner //允许本地玩家直接写入
    );

    public override void OnNetworkSpawn()
    {
        //监听颜色编号变化：只要网络上这个数字变了，就执行 OnColorChanged
        colorIndex.OnValueChanged += OnColorChanged;

        if (IsOwner)
        {
            //如果是我自己生成的，我就去读取MainMenu 里存的那个本地编号！
            int mySavedColor = PlayerPrefs.GetInt("PlayerColorIndex", 0);

            // 把我的编号写进网络变量，Netcode会自动告诉所有人
            colorIndex.Value = mySavedColor;
        }

        //无论如何，生成时先刷新一次自己的颜色
        ApplyColor(colorIndex.Value);
    }

    //当网络监听到颜色变化时触发
    private void OnColorChanged(int previousValue, int newValue)
    {
        ApplyColor(newValue);
    }

    //实际执行变色的方法
    private void ApplyColor(int index)
    {
        // 防错处理：万一数组越界了就不变色
        if (index < 0 || index >= bodyColors.Length) return;

        if (bodySprite != null) bodySprite.color = bodyColors[index];
        if (gunSprite != null) gunSprite.color = gunColors[index];
    }
}