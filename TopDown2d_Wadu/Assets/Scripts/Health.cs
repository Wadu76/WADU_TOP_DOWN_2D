using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;   //for image
using System.Collections; //for 协程 (Coroutine) 倒计时

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
    //同步玩家的“生死状态”
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int maxHealth = 100;
    public Image healthBarFill; //滑动血条
    //上次受伤时间
    private float lastDamageTime = 0f;
    //初始化：监听血量变化
    public override void OnNetworkSpawn()
    {
        //当currentHealth数值发生变化时，执行OnHealthChanged
        currentHealth.OnValueChanged += OnHealthChanged;
        isDead.OnValueChanged += OnDeathStateChanged; //监听生死变化
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
        //如果不是服务器，不仅不扣血，连判断都不判断，直接退出 且如果4了就也退出
        if (!IsServer || isDead.Value) return;

        //检查撞到的东西是不是子弹
        if (other.CompareTag("Bullet"))
        {
            //在扣血前，立刻关闭子弹的碰撞体
            //这样即使Destroy要等到帧末才执行，这个子弹在本帧内也变成了“实体幻影”，无法再次触发伤害了。 这样就不会一个子弹二次触发了
            //other.enabled = false;依旧不行
            if (Time.time - lastDamageTime < 0.05f) return;
            //如果成功造成了伤害，马上刷新“最后一次受伤的时间”
            lastDamageTime = Time.time;
            //扣血 (修改Value会自动同步给所有客户端)
            currentHealth.Value -= 10;

            //销毁子弹 (必须用 NetworkObject的 Despawn)
            //哪怕子弹在客户端还没飞到，服务器说它没了，它就得没
            /*NetworkObject netObj = other.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Despawn();
            }*/
            // no more 网络组件 直接摧毁！
            Destroy(other.gameObject);
        }
    }

    //当血量变化时执行 (客户端和服务器都会执行这个)
    // previousValue: 变化前的值, newValue: 变化后的值
    private void OnHealthChanged(int previousValue, int newValue)
    {
        UpdateHealthUI(newValue);
        Debug.Log($"玩家 {OwnerClientId} 血量: {newValue}");

        //如果血量归零，且我是服务器，且目前还没死，就执行死亡逻辑
        if (newValue <= 0 && IsServer && !isDead.Value)
        {
            Debug.Log($"玩家 {OwnerClientId} 挂了！");
            //这里以后可以写：播放死亡动画、重生、或者关闭控制
            //简单演示：把人藏起来 (不要 Destroy，否则连接会断)
            //gameObject.SetActive(false); // 暂时不建议直接关，可能会导致网络不同步，先看Log

            StartCoroutine(RespawnRoutine());
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

    //核心死亡表现逻辑（所有客户端都会执行）
    private void OnDeathStateChanged(bool oldState, bool newState)
    {
        // newState 为 true 表示死了，false 表示活着

        //开启/关闭所有可见的图片（本体的圆圈和枪）
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            sr.enabled = !newState;
        }

        //开启/关闭碰撞体（防止鞭尸和挡子弹）
        GetComponent<Collider2D>().enabled = !newState;

        //隐藏/显示血条的父级Canvas
        if (healthBarFill != null)
        {
            healthBarFill.transform.parent.gameObject.SetActive(!newState);
        }
    }

    //只在服务器运行的复活倒计时器
    private IEnumerator RespawnRoutine()
    {
        isDead.Value = true; //宣布死亡，触发上面的隐藏逻辑

        Debug.Log($"玩家 {OwnerClientId} 死亡，3秒后复活...");
        yield return new WaitForSeconds(3f); //挂机等3秒

        //随机找个坐标复活 (X: -5到5, Y: -5到5)
        //transform.position = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);
        Vector3 randomSpawnPos = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { OwnerClientId }
            }
        };
        TeleportPlayerClientRpc(randomSpawnPos, clientRpcParams);

        currentHealth.Value = maxHealth; //满血
        isDead.Value = false; //宣布复活，模型重新显示！
    }

    //专门让客户端自己瞬移的方法
    [ClientRpc]
    private void TeleportPlayerClientRpc(Vector3 newPos, ClientRpcParams clientRpcParams = default)
    {
        // 客户端接到命令，乖乖把自己移过去
        transform.position = newPos;
    }
}

    