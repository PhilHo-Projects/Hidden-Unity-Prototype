Shader "Custom/SmoothVoronoiContours"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Time ("Time", Float) = 0.0
        _Glossy ("Glossy Mode", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                float4 screenPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Glossy;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            // Standard 2x2 hash algorithm
            float2 hash22(float2 p) 
            {
                float n = sin(dot(p, float2(41.0, 289.0)));
                p = frac(float2(2097152.0, 262144.0) * n);
                return cos(p * 6.283 + _Time) * 0.5;
            }

            // Smooth Voronoi implementation
            float smoothVoronoi(float2 p, float falloff) 
            {
                float2 ip = floor(p);
                p -= ip;
                
                float d = 1.0;
                float res = 0.0;
                
                for(int i = -1; i <= 2; i++) {
                    for(int j = -1; j <= 2; j++) {
                        float2 b = float2(i, j);
                        float2 v = b - p + hash22(ip + b);
                        d = max(dot(v, v), 1e-4);
                        res += 1.0 / pow(d, falloff);
                    }
                }
                
                return pow(1.0 / res, 0.5 / falloff);
            }

            // 2D function for contour generation
            float func2D(float2 p)
            {
                float d = smoothVoronoi(p * 2.0, 4.0) * 0.66 + smoothVoronoi(p * 6.0, 4.0) * 0.34;
                return sqrt(d);
            }

            // Smooth fract function for glossy mode
            float smoothFract(float x, float sf)
            {
                x = frac(x);
                return min(x, x * (1.0 - x) * sf);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Get screen coordinates similar to Shadertoy
                float2 fragCoord = i.screenPos.xy / i.screenPos.w * _ScreenParams.xy;
                float3 iResolution = float3(_ScreenParams.xy, 1.0);
                float iTime = _Time;
                
                // Screen coordinates
                float2 uv = (fragCoord.xy - iResolution.xy * 0.5) / iResolution.y;
                
                // Standard epsilon for numerical gradient
                float2 e = float2(0.001, 0.0);
                
                // Function value
                float f = func2D(uv);
                
                // Numerical gradient calculation
                float g = length(float2(f - func2D(uv - e.xy), f - func2D(uv - e.yx))) / e.x;
                g = 1.0 / max(g, 0.001);
                
                // Contour generation
                float freq = 12.0;
                float smoothFactor = iResolution.y * 0.0125;
                
                float c;
                if(_Glossy > 0.5) {
                    // Glossy mode
                    c = smoothFract(f * freq, g * iResolution.y / 16.0);
                } else {
                    // Regular mode
                    c = clamp(cos(f * freq * 3.14159 * 2.0) * g * smoothFactor, 0.0, 1.0);
                }
                
                // Coloring
                float3 col = float3(c, c, c);
                float3 col2 = float3(c * 0.64, c, c * c * 0.1);
                
                float mixFactor;
                if(_Glossy > 0.5) {
                    mixFactor = -uv.y + clamp(frac(f * freq * 0.5) * 2.0 - 1.0, 0.0, 1.0);
                } else {
                    mixFactor = -uv.y + clamp(cos(f * freq * 3.14159) * 2.0, 0.0, 1.0);
                }
                
                col = lerp(col, col2, mixFactor);
                
                // Special contour coloring
                f = f * freq;
                if(_Glossy > 0.5) {
                    if(f > 8.0 && f < 9.0) col *= float3(1.0, 0.0, 0.1);
                } else {
                    if(f > 8.5 && f < 9.5) col *= float3(1.0, 0.0, 0.1);
                }
                
                // Glossy highlighting
                if(_Glossy > 0.5) {
                    col += g * g * g * float3(0.3, 0.5, 1.0) * 0.25 * 0.25 * 0.25 * 0.1;
                }
                
                return float4(sqrt(clamp(col, 0.0, 1.0)), 1.0);
            }
            ENDCG
        }
    }
}
