Shader "Custom/ParticleEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float t = _Time.y + 5.0;
                float z = 6.0;
                static const int n = 100;
                
                float3 startColor = float3(0, 0.64, 0.2);
                float3 endColor = float3(0.06, 0.35, 0.85);
                
                float startRadius = 0.84;
                float endRadius = 1.6;
                
                float power = 0.51;
                float duration = 4.0;
                
                float2 s = _ScreenParams.xy;
                float2 v = z * (2.0 * i.vertex.xy - s) / s.y;
                
                float3 col = float3(0, 0, 0);
                float2 pm = v.yx * 2.8;
                float dMax = duration;
                
                float evo = (sin(_Time.y * 0.01 + 400.0) * 0.5 + 0.5) * 99.0 + 1.0;
                
                float mb = 0.0;
                float mbRadius = 0.0;
                float sum = 0.0;
                
                for(int i = 0; i < n; i++)
                {
                    float d = frac(t * power + 48934.4238 * sin(float(i/int(evo)) * 692.7398));
                    float tt = 0.0;
                    float a = 6.28 * float(i)/float(n);
                    float x = d * cos(a) * duration;
                    float y = d * sin(a) * duration;
                    float distRatio = d/dMax;
                    
                    mbRadius = lerp(startRadius, endRadius, distRatio);
                    float2 p = v - float2(x,y);
                    mb = mbRadius/dot(p,p);
                    sum += mb;
                    
                    col = lerp(col, lerp(startColor, endColor, distRatio), mb/sum);
                }
                
                sum /= float(n);
                col = normalize(col) * sum;
                sum = clamp(sum, 0.0, 0.4);
                float3 tex = float3(1, 1, 1);
                col *= smoothstep(tex, float3(0,0,0), float3(sum,sum,sum));
                
                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
