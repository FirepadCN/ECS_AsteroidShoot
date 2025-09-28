Shader "Hidden/PointVisualize_NoGS"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZWrite On
        ZTest LEqual
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma target 4.5
            #pragma vertex VS
            #pragma fragment PS
            #include "UnityCG.cginc"

            StructuredBuffer<float3> _Points;
            float4x4 _VP;
            float    _PointSize;

            struct appdata { uint vid : SV_VertexID; };
            struct v2f { float4 pos : SV_POSITION; float psize : PSIZE; };

            v2f VS(appdata v)
            {
                v2f o;
                float3 p = _Points[v.vid];
                o.pos = mul(_VP, float4(p,1));
                o.psize = _PointSize > 0 ? _PointSize : 8;
                return o;
            }

            float4 PS(v2f i) : SV_Target { return 1; }
            ENDCG
        }
    }
}
