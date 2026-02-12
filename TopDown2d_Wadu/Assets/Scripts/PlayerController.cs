using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    float movespeed = 5f;
    Rigidbody2D rb;
    // Update is called once per frame
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    void FixedUpdate()
    {
        //水平输入 
        float horizontal_input = Input.GetAxisRaw("Horizontal");
        //垂直输入
        float vertical_input = Input.GetAxisRaw("Vertical");

        //组合成向量
        Vector2 input_movement = new Vector2(horizontal_input, vertical_input);

        //归一化
        Vector2 Normalization_movement = input_movement.normalized;

        rb.velocity = movespeed * Normalization_movement;


    }
}
