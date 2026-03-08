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
                    health.TakeDamage(10, shooterId);
                }
            }

            //无论哪种子弹，撞到墙或人都要把自己销毁
            Destroy(gameObject);
        }
    }
}