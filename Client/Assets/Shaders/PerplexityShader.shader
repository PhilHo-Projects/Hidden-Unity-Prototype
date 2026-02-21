Shader "Custom/Fire2D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorStart ("Color Start", Color) = (1, 0.7, 0, 1)
        _ColorEnd ("Color End", Color) = (1, 0, 0, 0)
        _FireHeight ("Fire Height", Range(0, 5)) = 2
        _FireIntensity ("Fire Intensity", Range(0, 5)) = 1.5
        _FlickerSpeed ("Flicker Speed", Range(0, 10)) = 3
        _NoiseScale ("Noise Scale", Range(0, 50)) = 20
        _DistortionAmount ("Distortion", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
        }

        Blend One One // Additive blending
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
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _ColorStart;
            float4 _ColorEnd;
            float _FireHeight;
            float _FireIntensity;
            float _FlickerSpeed;
            float _NoiseScale;
            float _DistortionAmount;

            // Improved noise function
            float2 hash2(float2 p)
            {
                float2 k = float2(0.3183099, 0.3678794);
                p = p * k + k.yx;
                return -1.0 + 2.0 * frac(16.0 * k * frac(p.x * p.y * (p.x + p.y)));
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                
                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(dot(hash2(i + float2(0.0,0.0)), f - float2(0.0,0.0)),
                               dot(hash2(i + float2(1.0,0.0)), f - float2(1.0,0.0)), u.x),
                          lerp(dot(hash2(i + float2(0.0,1.0)), f - float2(0.0,1.0)),
                               dot(hash2(i + float2(1.0,1.0)), f - float2(1.0,1.0)), u.x), u.y);
            }

            // Fractal Brownian Motion
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < 5; i++)
                {
                    value += amplitude * noise(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                return value;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // Base fire shape
                float fireShape = 1.0 - uv.y;
                
                // Add noise-based distortion
                float time = _Time.y * _FlickerSpeed;
                float2 noiseUV = uv * _NoiseScale;
                noiseUV.y -= time * 2.0;
                
                float noise1 = fbm(noiseUV);
                float noise2 = fbm(noiseUV * 1.5 + float2(time * 0.5, 0));
                
                // Combine noises for more interesting movement
                float finalNoise = (noise1 * 0.6 + noise2 * 0.4);
                
                // Apply distortion
                uv.x += finalNoise * _DistortionAmount;
                
                // Create fire gradient with noise
                float gradient = smoothstep(0.0, _FireHeight, fireShape + finalNoise);
                gradient *= _FireIntensity;
                
                // Add flickering
                float flicker = sin(time * 1.5) * 0.1 + 0.9;
                gradient *= flicker;
                
                // Color gradient
                float4 col = lerp(_ColorEnd, _ColorStart, gradient);
                
                // Apply alpha
                col.a = gradient;
                
                // Add some bright spots based on noise
                float brightSpots = pow(noise2, 3.0) * gradient;
                col.rgb += brightSpots * _ColorStart.rgb;
                
                return col * col.a;
            }
            ENDCG
        }
    }
}
