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
        if (Input.GetMouseButtonDown(0) && nexttime <= Time.time)
        {
            //Fire(); 这是没网络同步的发射
            RequestFirstServerRpc();
            //will add firerate later
            //nexttime += fireRate; 这样会屯时间 实现连发
            nexttime = Time.time + fireRate;
        }
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

    //目前客户端看到的子弹是粘滞在原地的
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
    }
}
