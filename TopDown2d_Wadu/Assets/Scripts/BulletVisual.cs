using UnityEngine;
using Unity.Netcode;

public class BulletVisual : MonoBehaviour
{
    public GameObject hitEffectPrefab;

    //加一个开关，Shooter会告诉它是不是逻辑子弹
    [HideInInspector]
    public bool isLogicBullet = false;

    [HideInInspector]
    public ulong shooterId; //as the name it is
    public ulong victimId;
    public int damage = 10;

    private bool hasBeenReturned = false;
    bool isAIBullet;

    /*
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //第一优先级：如果是果冻墙什么都不做
        // 直接return，让BouncyMaterial接管反弹工作
        if (collision.gameObject.CompareTag("JellyWalls"))
        {
            return;
        }

        //如果撞到了玩家或普通墙壁
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Wall"))
        {
            //视觉子弹才生成特效（防止主机看到双份火花）
            if (!isLogicBullet && hitEffectPrefab != null)
            {
                //在碰撞点生成火花（可选优化：collision.contacts[0].point 可以获取精确碰撞点）
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }

            //逻辑子弹打到人就扣血
            if (isLogicBullet && collision.gameObject.CompareTag("Player") && NetworkManager.Singleton.IsServer)
            {
                Health health = collision.gameObject.GetComponent<Health>();
                if (health != null)
                {
                    health.TakeDamage(10, shooterId);
                }
            }

            //无论哪种子弹，撞到普通墙或人都要把自己销毁
            Destroy(gameObject);
        }*/

    private void OnTriggerEnter2D(Collider2D other)
        {
            //拦截 如果这颗子弹已经触发过回收了，直接退出，防止一弹双回（比如同时打到墙&人
            if (hasBeenReturned) return;
        //如果撞到了玩家或墙壁
        if (other.CompareTag("Player") || other.CompareTag("Wall") || other.CompareTag("AI"))
            {

                
                //视觉子弹才生成特效（防止主机看到双份火花）
                if (!isLogicBullet && hitEffectPrefab != null)
                {
                    //火花特效
                    Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                }

                //1.逻辑子弹打到 2.人 就扣血
                if(isLogicBullet && (other.CompareTag("Player") || other.CompareTag("AI")) && NetworkManager.Singleton.IsServer)
                {
                    //Health health = GetComponent<Health>(); 这是为何只能默认获取id0 不加other就只能获取自身的组件了
                    Health health = other.GetComponent<Health>();
                    if (health != null)
                    {
                        health.TakeDamage(damage, shooterId);
                    }
                }

            //无论哪种子弹，撞到墙或人都要把自己销毁
            //Destroy(gameObject);
            //优化，撞到墙/玩家后，直接回收，不再destroy
            //上锁，并回收
            hasBeenReturned = true;
            if (shooterId == 999) isAIBullet = true;
            else isAIBullet = false;

                ObjectPoolManager.Instance.ReturnBullet(isAIBullet, gameObject);
        }
        }

    //Setactive false时，协程也会被关闭
    //每次从对象池中被拿出来并激活 (SetActive(true)) 时，就会自动执行OnEnable
    private void OnEnable()
    {
        //每次从池子里借出来时，重置这把锁
        hasBeenReturned = false;
        //开启2秒后自动回收的协程
        StartCoroutine(AutoReturn());
    }

    private System.Collections.IEnumerator AutoReturn()
    {
        yield return new WaitForSeconds(2f);

        //只有当2秒过去后，子弹还没被别的逻辑（比如撞墙）回收时，才执行自动回收

        //jiage !
        if (hasBeenReturned)
        {
            if (shooterId == 999) isAIBullet = true;
            else isAIBullet = false;
            //不需要做任何非空判断了，因为如果它已经被提前回收（撞墙），这个协程早就被系统掐断了
            ObjectPoolManager.Instance.ReturnBullet(isAIBullet, gameObject);
        }
        
    }
}
