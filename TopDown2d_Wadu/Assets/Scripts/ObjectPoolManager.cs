using System.Collections.Generic;
using UnityEngine;

//不需要处理网络同步了,池子相关操作只在Server进行，客户端只生产视觉上的子弹
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance;

    [Header("Pool Setting")]
    public GameObject playerbulletPrefab;
    public GameObject ai_bulletPrefab;//要单独弄ai的子弹池，否则ai调用的也是普通子弹池里的子弹（也会是黄色的）
    public int poolSize = 5;
    

    //用队列维护池子 近也方便 出也方便
    private Queue<GameObject> playerbulletPool = new Queue<GameObject>();
    //第二个队列 AI的子弹队列（AI对手用的子弹和玩家不一样，所以要维护两个池子）
    private Queue<GameObject> aibulletPool = new Queue<GameObject>();

    private void Awake()
    {
        //要看挂的物品存不存在
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        //游戏一开始，直接在本地造好30发子弹备用
        InitializePool();
    }

    private void InitializePool()
    {

        for (int i = 0; i < poolSize; i++)
        {
            // player's bullet
            CreateNewBullet(false);
            // ai's bullet
            CreateNewBullet(true);
        }
    }

    
    //制造新子弹函数
    private void CreateNewBullet(bool isAI)
    {
        //根据isAI bool 值判断是玩家/ai的子弹并创造对应的子弹
        GameObject prefab = isAI ? ai_bulletPrefab : playerbulletPrefab;
        //初始化一个
        GameObject bullet = Instantiate(prefab);
        //先关闭不显示
        bullet.SetActive(false);
        //设置到我们挂在的物体下面，防止hierarchy子弹看着乱
        bullet.transform.SetParent(transform);
        //造好后入队
        if (isAI) aibulletPool.Enqueue(bullet);
        else playerbulletPool.Enqueue(bullet);
    }

    //借子弹
    public GameObject GetBullet(bool isAI, Vector2 position, Quaternion rotation)
    {
        //先选对应池子
        Queue<GameObject> targetPool = isAI ? aibulletPool : playerbulletPool;

        //借之前池子里起码得有子弹吧
        if (targetPool.Count == 0)
        {
            CreateNewBullet(isAI);
        }

        //要用 出队
        GameObject bullet = targetPool.Dequeue();

        //
        bullet.transform.position = position;
        bullet.transform.rotation = rotation;

        //拿出来直接激活就能用！
        bullet.SetActive(true);
        return bullet;
    }

    //还子弹回池子
    public void ReturnBullet(bool isAI , GameObject bullet)
    {
        //先关闭
        bullet.SetActive(false);
        //再入池子
        //bulletPool.Enqueue(bullet);
        if (isAI) aibulletPool.Enqueue(bullet);
        else playerbulletPool.Enqueue(bullet);
    }
}