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

    //虚拟子弹生成
    void SpawnVisualBullet()
    {
        if (bulletPrefeb == null) return;

        //实例化一个真子弹 后面我们给设置成假的
        GameObject bullet = Instantiate(bulletPrefeb, firePoint.position, firePoint.rotation);

        //关闭预测子弹的碰撞体，防止客户端自己触发伤害逻辑
        /*Collider2D col = bullet.GetComponent<Collider2D>();
        if(col != null)
        {
            col.enabled = false;
        }*/

        //这样它能正常撞墙爆火花，但Health脚本看到它不是"Bullet就不扣血了
        bullet.tag = "Untagged";
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if(rb != null)
        {
            rb.velocity = firePoint.up * bulletSpeed;
        }

        //2s后直接destroy 因为不是网络组件了可以直接销毁
        Destroy(bullet, 2f);
    }
    //开火函数，每一发子弹都通过该函数产生与删除
    void Fire()
    {
        if (bulletPrefeb == null)
        {
            Debug.LogError("Prefeb未赋值");
        }

        //实例化子弹
        GameObject bullet = Instantiate(
            bulletPrefeb,
            firePoint.position,
            firePoint.rotation      //朝向和发射点一致
            );

        Rigidbody2D bullet2D = bullet.GetComponent<Rigidbody2D>();

        //子弹速度 向量*子弹速度
        bullet2D.velocity = firePoint.up * bulletSpeed;

        //我们要删除的是子弹本身，而不是他的rb组件
        Destroy(bullet, 2f);

    }

    /*/目前客户端看到的子弹是粘滞在原地的
    [ServerRpc]
    void RequestFirstServerRpc()
    {
        if (bulletPrefeb == null) return;

        //在server实例化子弹
        GameObject bullet = Instantiate(
            bulletPrefeb,
            firePoint.position,
            firePoint.rotation
            );
        
        //给速度
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if( rb != null )
        {
            rb.velocity = firePoint.up * bulletSpeed;
        }

        //获取网络组件，让服务器生成的子弹在其他地方也spawn
        bullet.GetComponent<NetworkObject>().Spawn();


        //摧毁
        Destroy(bullet, 2f);
    }*/


    [ServerRpc]
    void RequestFireServerRpc(ServerRpcParams rpcParams = default)
    {
        //服务器生成“逻辑”子弹（负责OnTriggerEnter2D算伤害）
        GameObject logicBullet = Instantiate(bulletPrefeb, firePoint.position, firePoint.rotation);

        //服务器不需要看子弹画面，关闭图片渲染，让它隐形
        SpriteRenderer sr = logicBullet.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        BulletVisual bv = logicBullet.GetComponent<BulletVisual>();
        if (bv != null)
        {
            bv.isLogicBullet = true;

            bv.shooterId = rpcParams.Receive.SenderClientId;
        } 
            
        //bv.enabled = false; 不需要再直接关闭BulletVisaul脚本，直接标记为逻辑子弹：不爆火花，撞墙自动摧毁

        


        //关掉逻辑子弹上的视觉脚本，只让它算伤害，不让它爆火花 防止与BulletVisual的冲突
        Rigidbody2D rb = logicBullet.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = firePoint.up * bulletSpeed;

        Destroy(logicBullet, 2f);

        //通知所有客户端：有人开枪了！
        //把开火者的ClientId 传过去
        BroadcastFireClientRpc(rpcParams.Receive.SenderClientId);
    }

    
    //广播给所有客户端执行
    [ClientRpc]
    void BroadcastFireClientRpc(ulong senderId)
    {
        //防重复 如果是开火者本人，直接跳过！
        //因为Update里已经生成过0延迟的预测子弹了，如果不跳过，他会看到射出两发子弹。
        if (NetworkManager.Singleton.LocalClientId == senderId) return;

        //其他玩家看到发射者开枪的视觉表现
        SpawnVisualBullet();
    }
}
