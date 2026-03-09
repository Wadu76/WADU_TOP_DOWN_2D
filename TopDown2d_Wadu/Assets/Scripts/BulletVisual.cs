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

    public int damage = 10;


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
            //如果撞到了玩家或墙壁
            if (other.CompareTag("Player") || other.CompareTag("Wall"))
            {

                
                //视觉子弹才生成特效（防止主机看到双份火花）
                if (!isLogicBullet && hitEffectPrefab != null)
                {
                    Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                }

                //1.逻辑子弹打到 2.人 就扣血
                if(isLogicBullet && other.CompareTag("Player") && NetworkManager.Singleton.IsServer)
                {
                    //Health health = GetComponent<Health>(); 这是为何只能默认获取id0 不加other就只能获取自身的组件了
                    Health health = other.GetComponent<Health>();
                    if (health != null)
                    {
                        health.TakeDamage(damage, shooterId);
                    }
                }

                //无论哪种子弹，撞到墙或人都要把自己销毁
                Destroy(gameObject);
            }
        }
    }
