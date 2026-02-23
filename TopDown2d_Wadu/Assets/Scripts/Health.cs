using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;   //for image

public class Health : NetworkBehaviour
{
    //定义网络变量
    //ReadPermission.Everyone: 所有人都能看到血量（为以后做血条UI准备）
    //WritePermission.Server: 只有服务器有权修改血量（防作弊）
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public int maxHealth = 100;
    public Image healthBarFill; //滑动血条
    //初始化：监听血量变化
    public override void OnNetworkSpawn()
    {
        //当currentHealth数值发生变化时，执行OnHealthChanged
        currentHealth.OnValueChanged += OnHealthChanged;

        UpdateHealthUI(currentHealth.Value);
    }

    //清理：取消监听
    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    //只有服务器能触发碰撞逻辑
    //因为子弹开了IsTrigger，所以这里用OnTriggerEnter2D
    private void OnTriggerEnter2D(Collider2D other)
    {
        //如果不是服务器，不仅不扣血，连判断都不判断，直接退出
        if (!IsServer) return;

        //检查撞到的东西是不是子弹
        if (other.CompareTag("Bullet"))
        {
            //扣血 (修改Value会自动同步给所有客户端)
            currentHealth.Value -= 10;

            //销毁子弹 (必须用 NetworkObject的 Despawn)
            //哪怕子弹在客户端还没飞到，服务器说它没了，它就得没
            NetworkObject netObj = other.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Despawn();
            }
        }
    }

    //当血量变化时执行 (客户端和服务器都会执行这个)
    // previousValue: 变化前的值, newValue: 变化后的值
    private void OnHealthChanged(int previousValue, int newValue)
    {
        UpdateHealthUI(newValue);
        Debug.Log($"玩家 {OwnerClientId} 血量: {newValue}");

        if (newValue <= 0)
        {
            Debug.Log($"玩家 {OwnerClientId} 挂了！");
            //这里以后可以写：播放死亡动画、重生、或者关闭控制
            //简单演示：把人藏起来 (不要 Destroy，否则连接会断)
            //gameObject.SetActive(false); // 暂时不建议直接关，可能会导致网络不同步，先看Log
        }
    }
    //这是一个专门用来更新血条UI的新方法，扣血/出生时候用
    private void UpdateHealthUI(int health)
    {
        if (healthBarFill != null)
        {
            // fillAmount 必须是 0 到 1 之间的小数
            // 把 int 转成 float 再相除，就能得到比如 80/100 = 0.8
            healthBarFill.fillAmount = (float)health / maxHealth;
        }
    }
}

    