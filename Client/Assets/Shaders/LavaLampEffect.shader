Shader "Custom/EnhancedLavaLampEffect"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        [Header(Color Settings)]
        _Color1 ("Base Color", Color) = (1,0,0,1)
        _Color2 ("Secondary Color", Color) = (1,1,0,1)
        
        [Header(Animation)]
        _Speed ("Animation Speed", Range(0, 5)) = 1.0
        _Scale ("Noise Scale", Range(0, 100)) = 5.0
        
        [Header(Advanced Settings)]
        _NoiseIntensity ("Noise Intensity", Range(0, 2)) = 1.0
        _Smoothness ("Smoothness", Range(0.51, 1)) = 0.51
        _DistortionStrength ("Distortion", Range(0, 1)) = 0.1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "PreviewType" = "Sphere"
        }
        
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0
            
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            // Properties
            float4 _Color1;
            float4 _Color2;
            float _Speed;
            float _Scale;
            float _NoiseIntensity;
            float _Smoothness;
            float _DistortionStrength;
            static const float4 D = float4(0.0, 0.5, 1.0, 2.0);

            // Optimized noise functions
            float3 mod289(float3 x) 
            {
                return x - floor(x / 289.0) * 289.0;
            }

            float4 mod289(float4 x) 
            {
                return x - floor(x / 289.0) * 289.0;
            }

            float4 permute(float4 x) 
            {
                return mod289((x * 34.0 + 1.0) * x);
            }

            float4 taylorInvSqrt(float4 r) 
            {
                return 1.79284291400159 - r * 0.85373472095314;
            }

            // Improved Simplex noise
            float snoise(float3 v)
            {
                const float2 C = float2(1.0/6.0, 1.0/3.0);
                
                // First corner
                float3 i  = floor(v + dot(v, C.yyy));
                float3 x0 = v   - i + dot(i, C.xxx);
                
                // Other corners
                float3 g = step(x0.yzx, x0.xyz);
                float3 l = 1.0 - g;
                float3 i1 = min(g.xyz, l.zxy);
                float3 i2 = max(g.xyz, l.zxy);
                
                float3 x1 = x0 - i1 + C.xxx;
                float3 x2 = x0 - i2 + C.yyy;
                float3 x3 = x0 - 0.5;
                
                // Permutations
                i = mod289(i);
                float4 p = permute(permute(permute(
                    i.z + float4(0.0, i1.z, i2.z, 1.0))
                    + i.y + float4(0.0, i1.y, i2.y, 1.0))
                    + i.x + float4(0.0, i1.x, i2.x, 1.0));
                    
                // Gradients
                float n_ = 0.142857142857;
                float3 ns = n_ * D.wyz - D.xzx;
                
                float4 j = p - 49.0 * floor(p * ns.z * ns.z);
                
                float4 x_ = floor(j * ns.z);
                float4 y_ = floor(j - 7.0 * x_);
                
                float4 x = x_ * ns.x + ns.yyyy;
                float4 y = y_ * ns.x + ns.yyyy;
                float4 h = 1.0 - abs(x) - abs(y);
                
                float4 b0 = float4(x.xy, y.xy);
                float4 b1 = float4(x.zw, y.zw);
                
                float4 s0 = floor(b0) * 2.0 + 1.0;
                float4 s1 = floor(b1) * 2.0 + 1.0;
                float4 sh = -step(h, 0.0);
                
                float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
                float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
                
                float3 p0 = float3(a0.xy, h.x);
                float3 p1 = float3(a0.zw, h.y);
                float3 p2 = float3(a1.xy, h.z);
                float3 p3 = float3(a1.zw, h.w);
                
                // Normalise gradients
                float4 norm = taylorInvSqrt(float4(
                    dot(p0, p0),
                    dot(p1, p1),
                    dot(p2, p2),
                    dot(p3, p3)
                ));
                p0 *= norm.x;
                p1 *= norm.y;
                p2 *= norm.z;
                p3 *= norm.w;
                
                // Mix final noise value
                float4 m = max(0.6 - float4(
                    dot(x0, x0),
                    dot(x1, x1),
                    dot(x2, x2),
                    dot(x3, x3)
                ), 0.0);
                m = m * m;
                return 42.0 * dot(m * m, float4(
                    dot(p0, x0),
                    dot(p1, x1),
                    dot(p2, x2),
                    dot(p3, x3)
                ));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                UNITY_TRANSFER_FOG(output, output.positionCS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Time-based animation
                float time = _Time.y * _Speed;
                
                // Add screen position-based distortion
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float2 distortedUV = input.uv + screenUV * _DistortionStrength;
                
                // Layer multiple noise samples for more interesting effect
                float noise1 = snoise(float3(distortedUV * _Scale, time));
                float noise2 = snoise(float3(distortedUV * _Scale * 0.5, time * 0.7));
                
                // Combine noise layers
                float finalNoise = (noise1 + noise2 * 0.5) * _NoiseIntensity;
                
                // Smooth the transition
                finalNoise = smoothstep(_Smoothness, 1 - _Smoothness, finalNoise * 0.5 + 0.5);
                
                // Color lerp with improved blending
                float4 finalColor = lerp(_Color1, _Color2, finalNoise);
                
                // Apply fog
                UNITY_APPLY_FOG(input.fogCoord, finalColor);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Diffuse"
}
