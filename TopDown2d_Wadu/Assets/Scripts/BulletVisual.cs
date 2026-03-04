using UnityEngine;

public class BulletVisual : MonoBehaviour
{
    //火花特效预制体
    public GameObject hitEffectPrefab;

    private void OnTriggerEnter2D(Collider2D other)
    {
        //如果撞到了玩家或墙壁（可以根据 Tag 来判断 后面补wall） if (other.CompareTag("Player") || other.CompareTag("Wall"))
        if (other.CompareTag("Player"))
        {
            //如果有特效预制体，就在当前位置生成特效
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }

            //销毁这个视觉子弹自己
            Destroy(gameObject);
        }
    }
}