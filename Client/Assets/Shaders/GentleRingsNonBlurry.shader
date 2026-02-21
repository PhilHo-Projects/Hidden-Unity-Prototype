Shader "Unlit/GentleRingsNotBlurry"
{
    Properties
    {
        _Resolution ("Resolution", Float) = 1.0
        _Sharpness ("Sharpness", Range(1.0, 10.0)) = 3.0
        _ColorIntensity ("Color Intensity", Range(0.5, 2.0)) = 1.0
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

            #define PI 3.141596

            float _Resolution;
            float _Sharpness;
            float _ColorIntensity;

            float3 a = float3(0.5, 0.5, 0.5);
            float3 b = float3(0.5, 0.5, 0.5);
            float3 c = float3(1.0, 1.0, 1.0);
            float3 d = float3(0.00, 0.33, 0.67);

            // Improved color mapper
            float3 colorMap(float t) {
                return saturate((a + b * cos(2.0 * PI * (c * t + d))) * _ColorIntensity);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;
                uv *= _Resolution;

                float r = length(uv);
                float a = atan2(uv.y, uv.x);

                float ring = 1.5 + 0.8 * sin(PI * 0.25 * _Time.y);
                float kr = smoothstep(0.45, 0.55, 0.5 - 0.5 * cos(7.0 * PI * r));
                float3 kq = smoothstep(0.45, 0.55, 0.5 - 0.5 * sin(ring * float3(30., 29.3, 28.6) * r - 6.0 * _Time.y + PI * float3(-0.05, 0.5, 1.0)));

                float3 c = kr * (0.1 + kq * (1. - 0.5 * colorMap(a / PI))) * (0.5 + 0.5 * sin(11. * a + 22.5 * r));

                // Sharpening
                c = pow(c, _Sharpness);

                // Output to screen
                float3 finalColor = lerp(float3(0.0, 0.0, 0.2), c, 0.85);

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}