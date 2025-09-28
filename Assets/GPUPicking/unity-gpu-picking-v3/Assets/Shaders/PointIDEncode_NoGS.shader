Shader "Hidden/PointIDEncode_NoGS"
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
            StructuredBuffer<uint>   _PointIDs;

            float4x4 _VP;
            float    _PointSize; // kept for API symmetry

            struct appdata { uint vid : SV_VertexID; };
            struct v2f { float4 pos : SV_POSITION; float psize : PSIZE; uint id : TEXCOORD0; };

            v2f VS(appdata v)
            {
                v2f o;
                float3 p = _Points[v.vid];
                o.pos = mul(_VP, float4(p,1));
                o.psize = _PointSize;
                o.id  = _PointIDs[v.vid] + 1u;
                return o;
            }

            float4 PS(v2f i) : SV_Target
            {
                uint pid = i.id;
                i.psize= _PointSize;
                return float4((pid & 255u)/255.0, ((pid>>8) & 255u)/255.0, ((pid>>16) & 255u)/255.0, ((pid>>24) & 255u)/255.0);
            }
            ENDCG
        }
    }
}
