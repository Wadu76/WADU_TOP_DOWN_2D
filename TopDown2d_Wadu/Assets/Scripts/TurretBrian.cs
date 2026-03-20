using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TurretBrian : NetworkBehaviour
{
    [Header("炮塔组件")]
    public Transform turretHead; //TurretHead
    public Transform firePoint;  //枪口

    [Header("参数")]
    public float attackRange = 8f;     //索敌半径
    public float rotationSpeed = 5f;   //炮头旋转速度
    public float fireRate = 1.5f;      //开火间隔

    [Header("子弹")]
    public GameObject bulletPrefab;

    [Header("AI_Bullet_Speed")]
    public float bulletSpeed = 10f; // 必须和AIBullet实际速度一致 和玩家的一样（shooter脚本里

    //目前要瞄准的target（玩家
    private Transform currentTarget;
    private float nextFireTime;

    

  

    // Update is called once per frame
    void Update()
    {
        //不是服务器就不弄
        if (!IsServer) return;
        //死了不能开枪
        //if (GetComponent<Health>().isDead.Value) return;
        //游戏结束就不开火了
        if ((ScoreManager.Instance != null && ScoreManager.Instance.Isgameover)) return;

        //是服务器就开始索敌，这些全由服务器负责
        FindNearestPlayer();

        if (currentTarget != null)
        {
            //瞄准玩家
            AimAtTarget();

            //如果冷却好了，就开火
            if (Time.time >= nextFireTime)
            {
                Fire();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    //索敌函数
    private void FindNearestPlayer()
    {
        // 找到场上所有带有 "Player" 标签的物体
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float shortestDistance = Mathf.Infinity;
        GameObject nearestPlayer = null;

        foreach (GameObject player in players)
        {
            // 如果玩家死了，就不管他（假设你的 Health 脚本里有 isDead 状态）
            Health health = player.GetComponent<Health>();
            if (health != null && health.isDead.Value) continue;

            // 计算距离
            float distance = Vector2.Distance(transform.position, player.transform.position);

            // 找到距离最近，且在攻击范围内的玩家
            if (distance < shortestDistance && distance <= attackRange)
            {
                shortestDistance = distance;
                nearestPlayer = player;
            }
        }

        // 更新当前目标
        if (nearestPlayer != null)
        {
            currentTarget = nearestPlayer.transform;
        }
        else
        {
            currentTarget = null;
        }
    }

    //瞄准
    private void AimAtTarget()
    {
        // 获取玩家的刚体（为了拿到玩家现在的移动速度）
        Rigidbody2D targetRb = currentTarget.GetComponent<Rigidbody2D>();

        Vector2 targetPos; // 最终要瞄准的位置


        if (targetRb != null)
        {
            //开启预判
            //算距离
            float distance = Vector2.Distance(firePoint.position, currentTarget.position);
            //算子弹飞过去要多久
            float timeToHit = distance / bulletSpeed;
            //算玩家这段时间会走到哪里
            targetPos = (Vector2)currentTarget.position + (targetRb.velocity * timeToHit);
        }
        else
        {
            //如果玩家没有刚体（或者没在动），就瞄准当前位置
            targetPos = currentTarget.position;
        }

        //指向计算出的预判位置
        Vector2 direction = targetPos - (Vector2)turretHead.position;

        //算出角度并平滑旋转
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRotation = Quaternion.Euler(new Vector3(0, 0, angle));
        turretHead.rotation = Quaternion.Slerp(turretHead.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    //开火逻辑
    private void Fire()
    {
        // 1. 服务器生成逻辑子弹
        SpawnBullet(true);

        // 2. 呼叫所有客户端生成视觉子弹（带特效）
        SpawnVisualBulletClientRpc();
    }

    [ClientRpc]
    private void SpawnVisualBulletClientRpc()
    {
        // 如果是服务器，刚才已经生成过逻辑子弹了，不用再生成视觉子弹
        if (IsServer) return;

        // 客户端生成只看不管的视觉子弹
        SpawnBullet(false);
    }

    private void SpawnBullet(bool isLogic)
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, turretHead.rotation);

        // 给子弹赋予速度 (假设子弹有 Rigidbody2D)
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // 给子弹一个向前的速度 (注意这里是 turretHead.up，假设你的炮口朝上)
            rb.velocity = turretHead.up * 10f;
        }

        // 设置子弹属性 (这里假设 ID 给 999 代表是 AI 打的)
        BulletVisual bv = bullet.GetComponent<BulletVisual>();
        if (bv != null)
        {
            bv.isLogicBullet = isLogic;
            bv.shooterId = 999;
            //bv.victimId = 999;
        }
    }
}
