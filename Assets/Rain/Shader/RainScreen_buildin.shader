Shader "ShaderCookbook/RainScreen"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "0,0,0,0"
		_Transparent("Transparent",Float)=1
		
		_Speed("Speed",float)=0.1
		_TileNum("TileNum",float)=5
		_AspectRatio("AspectRatio",int)=4
		_TailTileNum("_TailTileNum",int)=3
		_Period("_Period",float)=5
			_Angle("_Angle",float)=15
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }

		Pass
		{
			ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag			
			#include "UnityCG.cginc"

            #define PI2 6.28

            sampler2D _MainTex;
			half _Transparent;
			half4 _MainTex_ST;

			float _Speed;
			int _TileNum;
			int _AspectRatio;
			int _TailTileNum;
			float _Period;
			float _Angle;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			//获取随机值
            float random (float2 st) {
                return frac(sin(dot(st.xy,float2(12.9898,78.233)))*43758.5453123);
            }

			 //旋转
            float2x2 rotate2d(float angle){
                return float2x2(cos(angle),-sin(angle),sin(angle),cos(angle));
            }

			float2 Rain(float2 uv){
                //旋转模拟风
				uv=mul(rotate2d(_Angle*3.14/180),uv);
				
				//栅格化
				float t=_Time.y*PI2/_Period;

				//雨滴流动
				uv.y+= _Time.y*_Speed;

				uv*=fixed2(_TileNum*_AspectRatio,_TileNum);

                fixed2 idRand=random(floor(uv));
				t+=idRand.x*PI2;
				uv.x+=(idRand.x-0.5)*0.6;

                 

				uv=frac(uv);
				uv-=0.5;

				uv.y+=sin(t+sin(t+sin(t)*0.55))*0.45;
				//绘制雨滴
				uv.y*=_AspectRatio;//矫正
				float r=length(uv);
				r=smoothstep(0.2,0.1,r);

				//添加尾迹
				float2 tailUV=uv*float2(1.0,_TailTileNum);
				tailUV.y=frac(tailUV.y)-0.5;
				tailUV.x*=_TailTileNum;

                

				float rtail=length(tailUV);
				rtail*=uv.y;
				rtail=smoothstep(0.2,0.1,rtail);
				rtail*=smoothstep(0.3,0.4,uv.y);
                
				//融合
				float2 allUV=float2(rtail*tailUV+r*uv);
				return allUV;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv=i.uv;
              
			    float2 finalUV=Rain(uv*2)+Rain(uv*4);
				fixed4 rainCol=tex2D(_MainTex,uv+finalUV);
				rainCol.a*= _Transparent;
				
				return rainCol;
			}
			ENDCG
		}
	}
}
