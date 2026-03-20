using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;


//可移动ai脚本
public class TurretBrianMega : NetworkBehaviour
{
    //AI的三个状态
    public enum AIState
    {
        Patrol, //巡逻
        Chase,  //追击
        Attack  //攻击
    }

    [Header("炮塔组件")]
    public Transform turretHead; //TurretHead
    public Transform firePoint;  //枪口

    [Header("参数")]
    public float detectRange = 10f;     //索敌半径
    public float attackRange = 8f;     //攻击半径 （可移动的大点 让ai远处就跑过来
    public float patrolRadius = 10f;   //巡逻半径 (以出生点为中心)
    public float rotationSpeed = 5f;   //炮头旋转速度
    public float fireRate = 1.5f;      //开火间隔

    [Header("寻路组件")]
    public NavMeshAgent agent; //拖入挂在身上的NavMesh Agent

    [Header("子弹")]
    public GameObject bulletPrefab;


    [Header("AI_Bullet_Speed")]
    public float bulletSpeed = 10f; //必须和AIBullet实际速度一致 和玩家的一样（shooter脚本里


    //核心变量
    private AIState currentState;
    //目前要瞄准的target（玩家
    private Transform currentTarget;
    private float nextFireTime;

    //巡逻相关
    private Vector2 startPosition; //记录AI的出生点
    public bool isPatrolling = false; //是否正在前往巡逻点的路上

    //巡逻计时器
    private float patrolTimer = 0f;
    public float patrolInterval = 4f; // 每4秒换个地方溜达

    public override void OnNetworkSpawn()
    {
        //强制关闭3D旋转，适配2D
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        //只有服务器需要初始化大脑
        if (IsServer)
        {
            //记录出生点，以后就在这附近巡逻
            startPosition = transform.position;
            //默认进入巡逻状态
            currentState = AIState.Patrol;
            isPatrolling = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //不是服务器就不弄
        if (!IsServer) return;
        //死了不能开枪
        if (GetComponent<Health>().isDead.Value) return;    
        //游戏结束就不开火了
        if ((ScoreManager.Instance != null && ScoreManager.Instance.Isgameover)) return;

        //每帧先感知周围环境，更新当前状态
        UpdateAIState();

        //是服务器就开始索敌，这些全由服务器负责
        FindNearestPlayer();

        //根据当前状态，执行对应的大脑逻辑
        switch (currentState)
        {
            case AIState.Patrol:
                PatrolLogic();
                isPatrolling = true;
                break;
            case AIState.Chase:
                ChaseLogic();
                isPatrolling = false;
                break;
            case AIState.Attack:
                AttackLogic();
                isPatrolling = false;
                break;
        }

       
    }

    private void UpdateAIState()
    {
        FindNearestPlayer();    //先找最近的玩家
        
        if(currentTarget == null)
        {
            //没看到人，继续patrol
            currentState = AIState.Patrol;
            return;
        }

        //找到人了
        float distance = Vector2.Distance(transform.position, currentTarget.position);

        if(distance > detectRange)
        {
            //立刻清空玩家残影，准备重新规划巡逻路线
            if (currentState != AIState.Patrol)
            {
                agent.ResetPath();
            }
            //玩家跑出了视野外，放弃追击，回去巡逻了
            currentState = AIState.Patrol;
            currentTarget = null;
        }
        else if(distance <= detectRange && distance > attackRange)
        {
            //玩家在视野内，但还没进射程，追
            currentState = AIState.Chase;
        }
        else if(distance <= attackRange)
        {
            //距离足够了，开炮
            currentState = AIState.Attack;
        }
    }

    private void PatrolLogic()
    {
        //倒计时
        patrolTimer -= Time.deltaTime;

        if (patrolTimer <= 0f)
        {
            //时间到了，随便找个新点
            Vector2 randomDirection = UnityEngine.Random.insideUnitCircle * patrolRadius;
            Vector3 randomTarget = new Vector3(startPosition.x + randomDirection.x, startPosition.y + randomDirection.y, 0f);

            agent.SetDestination(randomTarget);

            //重置计时器
            patrolTimer = patrolInterval;
        }
    }

    private void ChaseLogic()
    {
        if (currentTarget != null)
        {
            //瞄准并全速前进
            agent.SetDestination(currentTarget.position);
            AimAtTarget(); //边跑边瞄准，给玩家压迫感
        }
    }

    private void AttackLogic()
    {
        if (currentTarget != null)
        {
            //停下脚步专心开火（或也可以不停止，边走边打）
            //agent.ResetPath();

            AimAtTarget();

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
        //找到场上所有带有 "Player" 标签的物体
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float shortestDistance = Mathf.Infinity;
        GameObject nearestPlayer = null;

        foreach (GameObject player in players)
        {
            //如果玩家死了，就不管他 Health 脚本里isDead状态）
            Health health = player.GetComponent<Health>();
            if (health != null && health.isDead.Value) continue;

            //计算距离
            float distance = Vector2.Distance(transform.position, player.transform.position);

            //找到距离最近，且在攻击范围内的玩家
            if (distance < shortestDistance && distance <= detectRange)
            {
                shortestDistance = distance;
                nearestPlayer = player;
            }
        }

        //更新当前目标
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
        //获取玩家的刚体（为了拿到玩家现在的移动速度）
        Rigidbody2D targetRb = currentTarget.GetComponent<Rigidbody2D>();

        Vector2 targetPos; //最终要瞄准的位置


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
        //服务器生成逻辑子弹
        SpawnBullet(true);

        //服务器给自己也生成一颗视觉子弹（负责看特效） 下面函数Isserver 就return了
        //所以此处若是不生成，那主机看ai的子弹就不带特效了
        SpawnBullet(false);

        //呼叫所有客户端生成视觉子弹（带特效）
        SpawnVisualBulletClientRpc();
    }

    [ClientRpc]
    private void SpawnVisualBulletClientRpc()
    {
        // 如果是服务器，刚才已经生成过逻辑子弹了，不用再生成视觉子弹，
        if (IsServer) return;

        //客户端生成只看不管的视觉子弹
        SpawnBullet(false);
    }

    private void SpawnBullet(bool isLogic)
    {
        //GameObject bullet = Instantiate(bulletPrefab, firePoint.position, turretHead.rotation); 不在这里实例化
        //添加第一个参数 isAI 这里都是ai子弹，直接传入true就行
        GameObject bullet = ObjectPoolManager.Instance.GetBullet(true, firePoint.position, firePoint.rotation);

        //给子弹赋予速度 (假设子弹有 Rigidbody2D)
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // 给子弹一个向前的速度 (注意这里是 turretHead.up，假设你的炮口朝上)
            rb.velocity = turretHead.up * 10f;
        }

        //设置子弹属性 (这里假设 ID 给 999 代表是 AI 打的)
        BulletVisual bv = bullet.GetComponent<BulletVisual>();
        if (bv != null)
        {
            bv.isLogicBullet = isLogic;
            bv.shooterId = 999;
            //bv.victimId = 999;
        }
    }


}
