using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//该脚本主要用来识别子弹是谁发出的。用于击杀播报和kd计算

public class BulletLogic : MonoBehaviour
{
    [HideInInspector]
    public ulong shooterId; // 记录开枪者的网络ID
    public int damage = 10; // 子弹伤害值
 
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 只有服务器有权处理伤害逻辑
        if (!Unity.Netcode.NetworkManager.Singleton.IsServer) return;

        if (other.CompareTag("Player"))
        {
            Health health = other.GetComponent<Health>();
            if (health != null)
            {
                // 把伤害和“凶手”的ID一起传给Health
                health.TakeDamage(damage, shooterId);
            }
        }
    }
}
