using UnityEngine;

public class KeepStraight : MonoBehaviour
{
    //这个是在头顶的高度偏移量，你可以根据需要调整
    public Vector3 offset = new Vector3(0, 1.5f, 0);
    //每一帧的最后执行（在玩家旋转完之后）

    void LateUpdate()
    {
        //强行把当前物体的旋转角度锁死为 0（绝对正立） both 背景白&血条绿。
        //好吧并给不了这俩，所以直接把这个脚本托给canvas
        //HealthBar_Background.transform.rotation = Quaternion.identity;
        //HealthBar_Fill.transform.rotation = Quaternion.identity;
        transform.rotation = Quaternion.identity;

        //锁死位置 永远只在父物体（Player）的正上方offset的位置
        if (transform.parent != null)
        {
            transform.position = transform.parent.position + offset;
        }
    }
}