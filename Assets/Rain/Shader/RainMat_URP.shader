Shader "URP/RainScreenURP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Speed("Speed",float)=0.1
        _TileNum("TileNum",float)=5
        _AspectRatio("AspectRatio",int)=4
        _TailTileNum("_TailTileNum",int)=3
        _Period("_Period",float)=5
        _Angle("_Angle",float)=15
        _Transparency("Transparency", Range(0,1)) = 1   // 👈 新增
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_ST;
            float _Speed;
            int _TileNum;
            int _AspectRatio;
            int _TailTileNum;
            float _Period;
            float _Angle;
            float _Transparency;   // 👈 新增

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            float random(float2 st) {
                return frac(sin(dot(st.xy,float2(12.9898,78.233)))*43758.5453123);
            }

            float2x2 rotate2d(float angle){
                return float2x2(cos(angle),-sin(angle),sin(angle),cos(angle));
            }

            float2 Rain(float2 uv){
                uv = mul(rotate2d(radians(_Angle)), uv);
                float t = _Time.y * 6.283185 / _Period;
                uv.y += _Time.y * _Speed;
                uv *= float2(_TileNum*_AspectRatio, _TileNum);

                float idRand = random(floor(uv));
                t += idRand * 6.283185;
                uv.x += (idRand - 0.5) * 0.6;

                uv = frac(uv) - 0.5;
                uv.y += sin(t+sin(t+sin(t)*0.55))*0.45;
                uv.y *= _AspectRatio;
                float r = smoothstep(0.2,0.1,length(uv));

                float2 tailUV = uv * float2(1.0,_TailTileNum);
                tailUV.y = frac(tailUV.y)-0.5;
                tailUV.x *= _TailTileNum;
                float rtail=length(tailUV);
                rtail*=uv.y;
                rtail=smoothstep(0.2,0.1,rtail);
                rtail*=smoothstep(0.3,0.4,uv.y);

                return rtail*tailUV + r*uv;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float2 finalUV = Rain(uv*2) + Rain(uv*4);
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + finalUV);

                // 👇 应用透明度
                col.a *= _Transparency;

                return col;
            }
            ENDHLSL
        }
    }
}