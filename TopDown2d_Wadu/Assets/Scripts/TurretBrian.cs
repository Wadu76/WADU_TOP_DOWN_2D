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

    private Transform currentTarget;
    private float nextFireTime;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //不是服务器就不弄
        if (!IsServer) return;

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
        // 计算炮塔指向玩家的方向向量
        Vector2 direction = currentTarget.position - turretHead.position;

        // 算出 Z 轴应该旋转的角度
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f; // 减 90 度是因为 Unity 2D 默认 Y 轴向上
        Quaternion targetRotation = Quaternion.Euler(new Vector3(0, 0, angle));

        // 平滑旋转（Slerp 差值），让 AI 看起来更真实，而不是瞬间锁头
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
        }
    }
}
