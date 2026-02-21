Shader "Custom/RetroSun"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float tri(float t, float scale, float shift)
            {
                return (abs(t * 2.0 - 1.0) - shift) * scale;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = (i.uv - 0.5) + 0.5;
                
                // sun
                float dist = length(uv - float2(0.5, 0.5));
                float divisions = 6.0;
                float divisionsShift = 0.5;
                
                float pattern = tri(frac((uv.y + 0.5) * 20.0), 2.0/divisions, divisionsShift) - (-uv.y + 0.26) * 0.85;
                float sunOutline = smoothstep(0.0, -0.015, max(dist - 0.315, -pattern));
                
                float3 c = sunOutline * lerp(float3(4.0, 0.0, 0.2), float3(1.0, 1.1, 0.0), uv.y);
                
                // glow
                float glow = max(0.0, 1.0 - dist * 2);
                glow = min(glow * glow * glow, 0.325);
                c += glow * float3(1.5, 0.3, (sin(_Time.y) + 1.0)) * 1.1;
                
                return float4(c, 1.0);
            }
            ENDHLSL
        }
    }
}
