using System.Collections.Generic;
using UnityEngine;

//不需要处理网络同步了,池子相关操作只在Server进行，客户端只生产视觉上的子弹
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance;

    [Header("Pool Setting")]
    public GameObject bulletPrefab;
    public int poolSize = 30;

    //用队列维护池子 近也方便 出也方便
    private Queue<GameObject> bulletPool = new Queue<GameObject>();

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
            CreateNewBullet();
        }
    }

    
    //制造新子弹函数
    private void CreateNewBullet()
    {
        //初始化一个
        GameObject bullet = Instantiate(bulletPrefab);
        //先关闭不显示
        bullet.SetActive(false);
        //设置到我们挂在的物体下面，防止hierarchy子弹看着乱
        bullet.transform.SetParent(transform);
        //造好后入队
        bulletPool.Enqueue(bullet);
    }

    //借子弹
    public GameObject GetBullet(Vector2 position, Quaternion rotation)
    {
        //借之前池子里起码得有子弹吧
        if (bulletPool.Count == 0)
        {
            CreateNewBullet();
        }

        //要用 出队
        GameObject bullet = bulletPool.Dequeue();

        //
        bullet.transform.position = position;
        bullet.transform.rotation = rotation;

        //拿出来直接激活就能用！
        bullet.SetActive(true);
        return bullet;
    }

    //还子弹回池子
    public void ReturnBullet(GameObject bullet)
    {
        //先关闭
        bullet.SetActive(false);
        //再入池子
        bulletPool.Enqueue(bullet);
    }
}