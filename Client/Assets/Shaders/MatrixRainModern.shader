Shader "Custom/MatrixRain"
{
    Properties
    {
        _MainTex ("Character Texture", 2D) = "white" {}
        _Columns ("Number of Columns in Texture", Float) = 16
        _Rows ("Number of Rows in Texture", Float) = 16
        _Speed ("Fall Speed", Range(0.1, 10.0)) = 1
        _Density ("Character Density", Range(0.1, 10.0)) = 1
        _GlowIntensity ("Glow Intensity", Range(0.1, 3.0)) = 1.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

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
            };

            sampler2D _MainTex;
            float _Speed;
            float _Density;
            float _GlowIntensity;
            float _Columns;
            float _Rows;

            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898,78.233))) * 43758.5453123);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // Create columns
                float2 cell = float2(floor(uv.x * _Density * 30), floor(uv.y * _Density * 30));
                
                // Animate downward
                float time = _Time.y * _Speed;
                float offset = random(float2(cell.x, 0)) * 6.28318;
                float speed = random(cell) * 0.5 + 0.5;
                float y = frac(uv.y - time * speed);
                
                // Generate characters from texture
                float charIndex = random(cell + floor(time * speed));
                float2 charPos = float2(
                    fmod(charIndex * 256, _Columns),
                    floor(charIndex * 256 / _Columns)
                );
                float2 charUV = (charPos + frac(uv * _Density * 30)) / float2(_Columns, _Rows);
                float char = tex2D(_MainTex, charUV).r;
                
                // Create glow effect
                float glow = pow(1.0 - y, 2.0) * _GlowIntensity;
                
                // Improved fade top and bottom
                float fadeTop = smoothstep(0.0, 0.4, y);
                float fadeBottom = smoothstep(0.0, 1.0, 1.0 - y);
                float fade = fadeTop * fadeBottom;
                
                // Final color
                float brightness = char * fade * glow;
                fixed4 col = fixed4(0, brightness, 0, brightness * 0.8);
                
                return col;
            }
            ENDCG
        }
    }
}
