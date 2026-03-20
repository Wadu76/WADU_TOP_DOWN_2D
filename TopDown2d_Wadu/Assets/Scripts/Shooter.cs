using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Unity.Netcode;

public class Shooter : NetworkBehaviour
{
    public GameObject bulletPrefeb;
    //单独设置个muzzle，直接绑定到抢上的话发射点会在原本锚点那儿
    public Transform firePoint;     //发射点
    public float bulletSpeed = 10f;     //子弹速度
    public float fireRate = 0.2f;       //发射速度限制
    private float nexttime = 0f;         //下次发射时间，用于计算子弹射出cool down时间


    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        //死了不能开枪
        if (GetComponent<Health>().isDead.Value) return;
        //加上游戏结束不能开枪
        if (ScoreManager.Instance != null && ScoreManager.Instance.Isgameover) return;
        //先看有没有ScoreManager 不然空指了

        //再看有没有打开esc界面 同样看有没有pausemenu
        if (PauseMenu.Instance != null && PauseMenu.Instance.IsPaused) return;
        //if (GetComponent<Health>().isDead.Value || (ScoreManager.Instance != null && ScoreManager.Instance.Isgameover || 
        //  (PauseMenu.Instance != null && !(PauseMenu.Instance.isActive)))) return;

        if (Input.GetMouseButtonDown(0) && nexttime <= Time.time)
        {
            //Fire(); 这是没网络同步的发射
            //本地立刻生成“假”子弹，延迟补偿
            SpawnVisualBullet();
            //服务器算伤害
            RequestFireServerRpc();
            //will add firerate later
            //nexttime += fireRate; 这样会屯时间 实现连发
            nexttime = Time.time + fireRate;
        }
    }

    //客户端本地生成视觉子弹（带特效，没有伤害
    //现在改成从子弹池子里借 不再单独生成
    void SpawnVisualBullet()
    {
        if (bulletPrefeb == null) return;

        //添加第一个参数 isAI，此处都是玩家子弹，所以直接设置为false
        GameObject bullet = ObjectPoolManager.Instance.GetBullet(false, firePoint.position, firePoint.rotation);

        //关闭预测子弹的碰撞体，防止客户端自己触发伤害逻辑
        /*Collider2D col = bullet.GetComponent<Collider2D>();
        if(col != null)
        {
            col.enabled = false;
        }*/

        //这样它能正常撞墙爆火花，但Health脚本看到它不是"Bullet就不扣血了
        //bullet.tag = "Untagged";完全多余，该逻辑已经改成islogicbullet的布尔值判断了
        //如果上面这样改tag就会污染池子里的子弹
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if(rb != null)
        {
            rb.velocity = firePoint.up * bulletSpeed;
        }


        //设置为视觉子弹
        BulletVisual bv = bullet.GetComponent<BulletVisual>();
        if (bv != null)
        {
            bv.isLogicBullet = false;
        }

        //2s后直接destroy 因为不是网络组件了可以直接销毁
        //Destroy(bullet, 2f);
        //现在子弹回收都在bulletvisual里面了，所以这里就不回收了
        //玩家只用管扣扳机，子弹要考虑的就很多了（自己回收自己）
    }
    


    [ServerRpc]
    void RequestFireServerRpc(ServerRpcParams rpcParams = default)
    {
        //都是玩家子弹
        GameObject bullet = ObjectPoolManager.Instance.GetBullet(false, firePoint.position, firePoint.rotation);

        //服务器不需要看子弹画面，关闭图片渲染，让它隐形
        //SpriteRenderer sr = logicBullet.GetComponent<SpriteRenderer>();
        //if (sr != null) sr.enabled = false;

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.velocity = firePoint.up * bulletSpeed;

        BulletVisual bv = bullet.GetComponent<BulletVisual>();
        if (bv != null)
        {
            bv.isLogicBullet = true;

            bv.shooterId = rpcParams.Receive.SenderClientId;
        }


        //bv.enabled = false; 不需要再直接关闭BulletVisaul脚本，直接标记为逻辑子弹：不爆火花，撞墙自动摧毁


        //关掉逻辑子弹上的视觉脚本，只让它算伤害，不让它爆火花 防止与BulletVisual的冲突
        //Rigidbody2D rb = logicBullet.GetComponent<Rigidbody2D>();
        //if (rb != null) rb.velocity = firePoint.up * bulletSpeed;

        //不能destroy了，已经从池子借了此处destroy就没意义了，最后会导致池子空
        //Destroy(bullet, 2f);

        //启动2s后回收子弹的协程
        //StartCoroutine(ReturnBulletDelay(bullet, 2f));
        //通知所有客户端：有人开枪了！
        //把开火者的ClientId 传过去
        //没有这个客户端就看不到主机端的子弹了
        BroadcastFireClientRpc(rpcParams.Receive.SenderClientId);
    }


    //关闭此处协程，我们直接在子弹射出的时候就给其计时2f，后面收回
    //子弹2s后还给池子
    //System.Collections.IEnumerator协程的强制返回类型
    /*
    private System.Collections.IEnumerator ReturnBulletDelay(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        //如果2秒后这颗子弹还在激活状态（没撞墙），就把它还给池子
        //目前撞墙依旧是destroy 打到墙上回因为子弹没了此处直接报错
        if (bullet.activeInHierarchy)
        {
            ObjectPoolManager.Instance.ReturnBullet(bullet);
        }
    }
    */
        //广播给所有客户端执行
        [ClientRpc]
    void BroadcastFireClientRpc(ulong senderId)
    {
        //防重复 如果是开火者本人，直接跳过
        //因为Update里已经生成过0延迟的预测子弹了，如果不跳过，他会看到射出两发子弹。
        if (NetworkManager.Singleton.LocalClientId == senderId) return;

        //其他玩家看到发射者开枪的视觉表现
        SpawnVisualBullet();
    }
}
