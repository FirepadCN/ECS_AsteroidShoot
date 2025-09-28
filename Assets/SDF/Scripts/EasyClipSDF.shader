Shader "Unlit/EasyClipSDF"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainColor ("Main Color", Color) = (1, 1, 1, 1)
        _PlaneTex ("Plane Texture", 2D) = "white" {}
        _PlaneCol ("Plane Color", Color) = (1, 1, 1, 1)
        _CircleCol ("Circle Color", Color) = (1, 1, 1, 1)
        _CircleRad ("Circle Radius", Range(0.0, 0.5)) = 0.45
        _PlaneDist ("Plane Dist", Range(-0.5, 0.5)) = 0.0
        _PlaneNormal ("Plane Normal", Vector) = (0,1,0,0)
    }
    SubShader
    {
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 hitPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainColor;
            sampler2D _PlaneTex;
            float4 _PlaneCol;

            float4 _CircleCol;
            float _CircleRad;
            float _PlaneDist;
            float4 _PlaneNormal; // xyz = 法线

            #define MAX_MARCHING_STEPS 50
            #define MAX_DISTANCE 20
            #define SURFACE_DISTANCE 0.001

            // --- SDF: 任意平面 ---
            float planeSDF(float3 ray_position)
            {
                float3 n = normalize(_PlaneNormal.xyz);
                return dot(n, ray_position) - _PlaneDist;
            }

            float sphereCasting(float3 ray_origin, float3 ray_direction)
            {
                float distance_origin = 0;
                for (int i = 0; i < MAX_MARCHING_STEPS; i++)
                {
                    float3 ray_position = ray_origin + ray_direction * distance_origin;
                    float distance_scene = planeSDF(ray_position);
                    distance_origin += distance_scene;

                    if (distance_scene < SURFACE_DISTANCE || distance_origin > MAX_MARCHING_STEPS)
                        break;
                }
                return distance_origin;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.hitPos = v.vertex;
                return o;
            }

            fixed4 frag(v2f i, bool face : SV_IsFrontFace) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col *= _MainColor;

                float3 ray_origin = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1));
                float3 ray_direction = normalize(i.hitPos - ray_origin);

                float t = sphereCasting(ray_origin, ray_direction);

                float4 planeCol = 0;

                if (t < MAX_DISTANCE)
                {
                    // --- 平面交点 ---
                    float3 plane_pos = ray_origin + ray_direction * t;

                    // --- 平面坐标系构造 ---
                    float3 n = normalize(_PlaneNormal.xyz);
                    float3 refVec = abs(n.y) < 0.99 ? float3(0,1,0) : float3(1,0,0);
                    float3 u = normalize(cross(refVec, n));
                    float3 v = normalize(cross(n, u));

                    //黑洞
                    ////float uvScale= sqrt(1-pow((i.hitPos.y-0.05)*2,2));
                    //利用平面参数调整uv缩放
                    float uvScale= sqrt(1-pow((_PlaneDist*2),2));
                    
                    // --- UV 投影 ---
                    float2 uv_plane;
                    uv_plane.x = dot(plane_pos, u);
                    uv_plane.y = dot(plane_pos, v);
                    uv_plane = uv_plane.xy / uvScale + 0.5; // 简单映射到 [0,1]

                    // --- 圆形边缘计算 ---
                    float2 circleUV = uv_plane - 0.5;
                    float c = length(circleUV);
                    float circleMask = smoothstep(_CircleRad, _CircleRad - 0.01, c);

                    // --- 平面纹理 + 圆形边缘 ---
                    planeCol = tex2D(_PlaneTex, uv_plane) * circleMask * _PlaneCol;
                    planeCol += (1 - circleMask) * _CircleCol;
                }

                // --- 剔除裁剪平面一侧 ---
                float3 n_discard = normalize(_PlaneNormal.xyz);
                if (dot(n_discard, i.hitPos) > _PlaneDist)
                    discard;

                return face ? col : planeCol;
            }
            ENDCG
        }
    }
}
