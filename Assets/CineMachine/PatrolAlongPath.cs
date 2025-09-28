using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class PatrolAlongPath : MonoBehaviour
{
    public SplineContainer spline;
    public float speed = 2f;

    private float currentT = 0f;  // 当前参数位置
    private float traveled = 0f;  // 已行进距离

    void Update()
    {
        if (spline == null) return;

        // 移动距离累加
        traveled += speed * Time.deltaTime;

        // 基于当前 t 和距离，计算新的点
        float newT;
        float3 pos = SplineUtility.GetPointAtLinearDistance(
            spline.Spline,
            currentT,
            speed * Time.deltaTime,
            out newT
        );

        // 更新状态
        currentT = newT;
        transform.position = pos;
    }
}