using UnityEngine;

public class BulletVisual : MonoBehaviour
{
    public GameObject hitEffectPrefab;

    //加一个开关，Shooter会告诉它是不是逻辑子弹
    [HideInInspector]
    public bool isLogicBullet = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 如果撞到了玩家或墙壁
        if (other.CompareTag("Player") || other.CompareTag("Wall"))
        {
            //视觉子弹才生成特效（防止主机看到双份火花）
            if (!isLogicBullet && hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }

            //无论哪种子弹，撞到墙或人都要把自己销毁！
            Destroy(gameObject);
        }
    }
}