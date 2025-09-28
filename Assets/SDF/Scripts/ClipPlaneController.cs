using System.Runtime.InteropServices;
using UnityEngine;

namespace EasyClipSDF
{
    [ExecuteAlways]
    public class ClipPlaneController : MonoBehaviour
    {
        public Material targetMaterial;

        [Header("Circle Settings")]
        public float circleRadius = 0.5f;

        private void Update()
        {
            if (targetMaterial == null) return;

            // 平面法线和距离
            Vector3 normal = transform.up; // 使用物体的本地Y轴作为法线
            float d = Vector3.Dot(normal, transform.position);

            targetMaterial.SetVector("_PlaneNormal", new Vector4(normal.x, normal.y, normal.z, 0));
            targetMaterial.SetFloat("_PlaneDist", d);
            targetMaterial.SetFloat("_CircleRadius", circleRadius);
        }
    
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0, 1, 1, 0.4f);
            Vector3 normal = transform.up;

            // 在场景里画出圆形切面
            int segments = 64;
            Vector3 prevPoint = Vector3.zero;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 dir = (transform.right * Mathf.Cos(angle) + transform.forward * Mathf.Sin(angle)) * circleRadius;
                Vector3 point = transform.position + dir;
                if (i > 0)
                    Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }

            // 画法线箭头
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + normal);
        }
        
        
        [DllImport("algorithm", EntryPoint = "GetPixelPos", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetPixelPos(
            double px, double py, double pz,
            double ox, double oy, double oz,
            double[] view, double[] proj,
            int imageWidth, int imageHeight,
            double[] result);

        [ContextMenu("Test AOT")]
        private void TestAOT()
        {
            
            
            // ✅ 输入点
            double px = 622631.1258981819;
            double py = 3556893.680344157;
            double pz = 82.83827486718162;

            // ✅ 偏移（可以按你需要调整）
            double ox = 622631.8129543234;
            double oy = 3557128.7418348496;
            double oz = 47.50213640155178;

            // ✅ ViewMatrix（行主序展开）
            double[] view = {
                -0.9985571402554467, 0, 0, -6.915798206231557,
                0.0035361208886891342, 0.997942395249853, -0.06401930655705879, 34.13889682502486,
                -0.053589097256817056, 0.06411689140145582, 0.9964235434897448, -245.88092139549553,
                0, 0, 0, 1
            };



            // ✅ ProjectionMatrix（行主序展开）
            double[] proj = {
                1.9616706246605113, 0, 0, 0,
                0, 3.4874144438409087, 0, 0,
                0, 0, -1.0000486269763977, -0.4862815868918704,
                0, 0, -1, 0
            };
            


            double[] result = new double[2];

            GetPixelPos(622631.1, 3556893.6, 82.8,
                622631.8, 3557128.7, 47.5,
                view, proj,
                4000, 3000,
                result);

            Debug.Log($"Pixel: {result[0]}, {result[1]}");
        }
        
    }     
}
