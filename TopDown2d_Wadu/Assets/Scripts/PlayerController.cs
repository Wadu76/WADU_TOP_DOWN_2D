using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    public float moveSpeed = 5f; //设为public方便在编辑器里调整

    private Rigidbody2D rb;
    private Camera mainCamera;
    private Vector2 moveInput; //储存移动输入
    private Vector2 mousePos;  //储存鼠标的世界坐标

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
    }

    //Update 专门用来读取输入，保证操作灵敏且视觉流畅
    void Update()
    {
        //如果这个角色不是我（本地玩家）控制的，就不要读取输入
        if (!IsOwner) return;
        //处理键盘移动输入
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized; // 归一化防止斜走变快

        // 2. 处理鼠标位置转换
        // 把鼠标在屏幕上的像素坐标 转为游戏世界坐标
        Vector3 screenPos = Input.mousePosition;
        // 关键点：屏幕坐标是2D的，我们需要告诉摄像机这个物体离镜头有多远
        screenPos.z = Mathf.Abs(mainCamera.transform.position.z);
        mousePos = mainCamera.ScreenToWorldPoint(screenPos);

        //处理旋转 (放在Update里看着更丝滑)
        //向量减法：目标点 - 当前点 = 指向目标的向量
        Vector2 lookDir = mousePos - rb.position;

        //让物体的Y轴(绿色箭头/上方) 对准这个方向
        transform.up = lookDir;

        //Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, -10f);


    }

    //FixedUpdate 专门用来处理物理引擎，防止穿墙和抖动
    void FixedUpdate()
    {
        //同样的若不是本人就不移动
        if (!IsOwner) return;
        //移动刚体
        rb.velocity = moveInput * moveSpeed;
        //Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, -10f);

    }

    private void LateUpdate()
    {
        if (!IsOwner) return;
        //摄像头固定到本角色上 保持z轴为 -10，否则摄像机会钻到地图里去变黑
        Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, -10f);
        
    }
}