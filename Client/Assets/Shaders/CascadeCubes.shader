Shader "Custom/GeometricPattern"
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
            float4 _MainTex_ST;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            // Unity-specific global variables to replace the original ones
            #define _iResolution float3(_ScreenParams.x, _ScreenParams.y, 0)
            #define _iTime _Time.y
            
            // Include the core shader functions from your code
            float2 mod_emu(float2 x, float2 y)
            {
                return x - y * floor(x / y);
            }

            float tanh_emu(float x)
            {
                return (abs(x) > 15.0) ? sign(x) : tanh(x);
            }

            float mod_emu(float x, float y)
            {
                return x - y * floor(x / y);
            }
            
            float2 vec2_ctor_int_int(int x0, int x1)
            {
                return float2(x0, x1);
            }
            
            float2x2 mat2_ctor_float4(float4 x0)
            {
                return float2x2(x0);
            }
            
            float3 vec3_ctor(float x0, float x1, float x2)
            {
                return float3(x0, x1, x2);
            }
            
            float4 vec4_ctor(float3 x0, float x1)
            {
                return float4(x0, x1);
            }
            
            int int_ctor(float2 x0)
            {
                return int(x0.x);
            }

            float f_segment(in float2 _p, in float2 _a, in float2 _b)
            {
                (_p -= _a);
                (_b -= _a);
                return length((_p - (_b * clamp((dot(_p, _b) / dot(_b, _b)), 0.0, 1.0))));
            }
            
            static float _t = 0;
            
            float2 f_T(in float3 _p)
            {
                (_p.xy = mul(_p.xy, transpose(mat2_ctor_float4(cos(((-_t) + float4(0.0, 1.57000005, -1.57000005, 0.0)))))));
                (_p.xz = mul(_p.xz, transpose(float2x2(0.707388222, -0.706261635, 0.707388222, 0.707388222))));
                (_p.yz = mul(_p.yz, transpose(float2x2(0.810963094, 0.585742831, -0.584451437, 0.810963094))));
                return _p.xy;
            }
            
            void f_mainImage_float4(inout float4 _O, in float2 _u)
            {
                float2 _R3049 = _iResolution.xy;
                float2 _X3050 = {0, 0};
                float2 _U3051 = ((10.0 * _u) / _R3049.y);
                float2 _M3052 = {2.0, 2.29999995};
                float2 _I3053 = (floor((_U3051 / _M3052)) * _M3052);
                float2 _J3054 = {0, 0};
                (_U3051 = mod_emu(_U3051, _M3052));
                (_O *= 0.0);
                for(int _k3055 = 0; (_k3055 < 4); (_k3055++))
                {
                    (_X3050 = (vec2_ctor_int_int((_k3055 % 2), (_k3055 / 2)) * _M3052));
                    (_J3054 = (_I3053 + _X3050));
                    if (((int_ctor((_J3054 / _M3052)) % 2) > 0))
                    {
                        (_X3050.y += 1.14999998);
                    }
                    (_t = (tanh_emu((((-0.200000003 * (_J3054.x + _J3054.y)) + mod_emu((2.0 * _iTime), 10.0)) - 1.60000002)) * 0.785000026));
                    for(float _a3056 = 0; (_a3056 < 6.0); (_a3056 += (1.57000005 + 0.0)))
                    {
                        float3 _A3057 = vec3_ctor(cos(_a3056), sin(_a3056), 0.699999988);
                        float3 _B3058 = vec3_ctor((-_A3057.y), _A3057.x, 0.699999988);
                        (_O += (smoothstep((15.0 / _R3049.y), 0.0, f_segment((_U3051 - _X3050), f_T(_A3057), f_T(_B3058))) + 0.0));
                        (_O += (smoothstep((15.0 / _R3049.y), 0.0, f_segment((_U3051 - _X3050), f_T(_A3057), f_T((_A3057 * float3(1.0, 1.0, -1.0))))) + 0.0));
                        (_A3057.z = ((-_A3057.z) + 0.0));
                        (_B3058.z = ((-_B3058.z) + 0.0));
                        (_O += (smoothstep((15.0 / _R3049.y), 0.0, f_segment((_U3051 - _X3050), f_T(_A3057), f_T(_B3058))) + 0.0));
                    }
                }
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = float4(1,1,1,1);
                f_mainImage_float4(col, i.vertex.xy);
                
                // Apply any needed color corrections as in the original shader
                if (col.x < 0.0) col = float4(1.0, 0.0, 0.0, 1.0);
                if (col.y < 0.0) col = float4(0.0, 1.0, 0.0, 1.0);
                if (col.z < 0.0) col = float4(0.0, 0.0, 1.0, 1.0);
                if (col.w < 0.0) col = float4(1.0, 1.0, 0.0, 1.0);
                
                return float4(col.xyz, 1.0);
            }
            ENDCG
        }
    }
}
