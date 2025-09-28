using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(AudioSource))]
public class SplineSoundFollower : MonoBehaviour
{
    public SplineContainer spline;   // 区域轨迹（必须是封闭的）
    public Transform player;         // 玩家
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.loop = true;
            audioSource.playOnAwake = true;
            audioSource.spatialBlend = 1f; // 3D 音效
            audioSource.Play();
        }
    }

    void Update()
    {
        if (spline == null || player == null) return;

        Vector3 targetPos;

        // 判断玩家是否在区域内部
        if (IsPointInsideSpline(player.position, spline))
        {
            targetPos = player.position; // 直接在玩家身上
        }
        else
        {
            // 否则投影到轨迹最近点
            SplineUtility.GetNearestPoint(
                spline.Spline,
                player.position,
                out var nearest,
                out float t
            );
            targetPos = nearest;
        }

        transform.position = targetPos;
    }

    /// <summary>
    /// 判断点是否在 Spline 形成的区域内（适合2D或XZ平面投影）。
    /// </summary>
    private bool IsPointInsideSpline(Vector3 point, SplineContainer splineContainer)
    {
        // 投影到 XZ 平面
        Vector2 p = new Vector2(point.x, point.z);

        // 简单射线法判断
        int intersections = 0;
        var nativeSpline = splineContainer.Spline;
        int knotCount = nativeSpline.Count;

        Vector3 prev = nativeSpline[0].Position;
        for (int i = 1; i < knotCount; i++)
        {
            Vector3 curr = nativeSpline[i].Position;

            Vector2 a = new Vector2(prev.x, prev.z);
            Vector2 b = new Vector2(curr.x, curr.z);

            if (((a.y > p.y) != (b.y > p.y)) &&
                (p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x))
            {
                intersections++;
            }

            prev = curr;
        }

        // 奇数 → 在内部；偶数 → 在外部
        return (intersections % 2) == 1;
    }
}