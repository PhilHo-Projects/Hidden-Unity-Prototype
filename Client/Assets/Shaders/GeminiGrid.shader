Shader "Custom/GeminiGrid"
{
    Properties
    {
        _MainTex ("Texture (Not Actively Used)", 2D) = "white" {} 

        _AnimationSpeed ("Animation Speed", Range(0.01, 2.0)) = 0.5
        _OverallScale ("Overall Scale", Range(0.5, 20.0)) = 4.0
        _EdgeSmoothness ("Edge Smoothness", Range(0.001, 0.5)) = 0.05 
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}
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

            // Properties that are still used
            sampler2D _MainTex;
            float _AnimationSpeed;
            float _OverallScale;
            float _EdgeSmoothness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // 1. Calculate 'px' (normalized coordinates)
                float2 uv_centered = (i.uv - 0.5) * 2.0;
                float aspectRatio = _ScreenParams.x / _ScreenParams.y;
                
                float2 px;
                px.x = _OverallScale * uv_centered.x * aspectRatio;
                px.y = _OverallScale * uv_centered.y;

                // 2. Calculate 'id' (tile identifier/value)
                float currentTime = _Time.y * _AnimationSpeed;
                
                float id_noise_arg = dot(floor(px + 0.5), float2(113.1, 17.81));
                float id = 0.5 + 0.5 * cos(currentTime + sin(id_noise_arg) * 43758.545);

                // 3. Calculate 'co' (tile color)
                // Color phases are now hardcoded to original Shadertoy values
                float3 colorPhases = float3(0.0, 1.0, 2.0); 
                float3 co = 0.5 + 0.5 * cos(currentTime + 2.0 * id + colorPhases);

                // 4. Calculate 'pa' (pattern alpha/shape)
                // Sub-pattern frequency is now hardcoded to 2*PI (original value 6.2831)
                float subPatternHardcodedFreq = UNITY_PI * 2.0f; 
                float2 pattern_input_val = id * (0.5 + 0.5 * cos(subPatternHardcodedFreq * px));
                float2 pa = smoothstep(0.0, _EdgeSmoothness, pattern_input_val);
                
                // 5. Final color
                return float4(co * pa.x * pa.y, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback "Diffuse"
}