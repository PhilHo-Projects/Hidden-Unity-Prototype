Shader "Custom/FlagstoneHLSL"
{
    Properties
    {
        _MainTex ("Texture (iChannel0)", 2D) = "white" {}
        _AnimationSpeed ("Animation Speed", Range(0.0, 5.0)) = 1.0
        _GlowIntensity ("Glow Intensity", Range(0.0, 3.0)) = 1.0
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
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            Texture2D _MainTex;
            SamplerState sampler_MainTex;

            float _AnimationSpeed;
            float _GlowIntensity;

            float mod_emu(float x, float y)
            {
                return x - y * floor(x / y);
            }

            float f_smin(float _a, float _b, float _k)
            {
                float _h = max((_k - abs(_a - _b)), 0.0f) / _k;
                return min(_a, _b) - (((_h * _h * _h) * _k) * 0.166666672f);
            }

            float3 f_palette(float _t)
            {
                return (0.519999981f + (0.479999989f * cos((6.28318548f * ((float3(0.899999976f, 0.800000012f, 0.5f) * _t) + float3(0.100000001f, 0.0500000007f, 0.100000001f))))));
            }

            float f_hash12(float2 _p)
            {
                _p = _p * 1.12129998f;
                float3 _p3 = frac(float3(_p.x, _p.y, _p.x) * 0.103100002f);
                _p3 += dot(_p3, _p3.yzx + 33.3300018f);
                return frac((_p3.x + _p3.y) * _p3.z);
            }

            float f_randSpan(float2 _p, float _timeVal)
            {
                return ((((sin(((_timeVal * 1.60000002f) + (f_hash12(_p) * 6.28318548f))) * 0.5f) + 0.5f) * 0.600000024f) + 0.200000003f);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 currentResolution = float3(_ScreenParams.x, _ScreenParams.y, _ScreenParams.z);
                float currentTime = _Time.y * _AnimationSpeed;

                float2 fragCoordXY = (i.screenPos.xy / i.screenPos.w) * currentResolution.xy;

                float2 _uv = (((2.0f * fragCoordXY) - currentResolution.xy) / currentResolution.y);
                _uv *= 4.0f;
                _uv += (float2(0.699999988f, 0.5f) * currentTime);
                
                float2 _fl = floor(_uv);
                float2 _fr = frac(_uv);
                bool _ch = (mod_emu((_fl.x + _fl.y), 2.0f) > 0.5f);
                float _r1 = f_randSpan(_fl, currentTime);
                
                float2 sc17;
                if (_ch) { sc17 = _fr.xy; } else { sc17 = _fr.yx; }
                float2 _ax = sc17;

                float _a1 = (_ax.x - _r1);
                float _si = sign(_a1);

                float2 sc18;
                if (_ch) { sc18 = float2(_si, 0.0f); } else { sc18 = float2(0.0f, _si); }
                float2 _o1 = sc18;
                
                float _r2 = f_randSpan((_fl + _o1), currentTime);
                float _a2 = (_ax.y - _r2);
                float2 _st = step(float2(0.0f, 0.0f), float2(_a1, _a2));
                
                float2 sc19;
                if (_ch) { sc19 = _st.xy; } else { sc19 = _st.yx; }
                float2 _of = sc19;
                
                float2 _id = ((_fl + _of) - 1.0f);
                bool _ch2 = (mod_emu((_id.x + _id.y), 2.0f) > 0.5f);
                float _r00 = f_randSpan((_id + float2(0.0f, 0.0f)), currentTime);
                float _r10 = f_randSpan((_id + float2(1.0f, 0.0f)), currentTime);
                float _r01 = f_randSpan((_id + float2(0.0f, 1.0f)), currentTime);
                float _r11 = f_randSpan((_id + float2(1.0f, 1.0f)), currentTime);
                
                float2 sc1a;
                if (_ch2) { sc1a = float2(_r00, _r10); } else { sc1a = float2(_r01, _r00); }
                float2 _s0 = sc1a;

                float2 sc1b;
                if (_ch2) { sc1b = float2(_r11, _r01); } else { sc1b = float2(_r10, _r11); }
                float2 _s1 = sc1b;
                
                float2 _s = ((1.0f - _s0) + _s1);
                float2 _puv = (((_uv - _id) - _s0) / _s);
                float2 _b = ((0.5f - abs(_puv - 0.5f)) * _s);
                float _d = f_smin(_b.x, _b.y, 0.150000006f);
                float _l = smoothstep(0.0199999996f, 0.0599999987f, _d);
                float2 _hp = ((1.0f - _puv) * _s);
                float _h = smoothstep(0.0799999982f, 0.0f, max(f_smin(_hp.x, _hp.y, 0.150000006f), 0.0f));
                float2 _sp = (_puv * _s);
                float _sh = smoothstep(0.0500000007f, 0.119999997f, max(f_smin(_sp.x, _sp.y, 0.150000006f), 0.0f));
                
                float3 _tex = pow(_MainTex.Sample(sampler_MainTex, _puv).xyz, float3(2.20000005f, 2.20000005f, 2.20000005f));
                
                float3 _col = f_palette(f_hash12(_id));
                _col *= _tex;
                _col *= ((float3(_puv.x, _puv.y, 0.0f) * 0.600000024f) + 0.400000006f);
                _col *= ((_sh * 0.800000012f) + 0.200000003f);
                _col += (_h * float3(0.899999976f, 0.699999988f, 0.5f));
                _col *= (_l * 5.0f);
                
                float2 normScreenUV = fragCoordXY / currentResolution.xy;
                float2 gv_coord = float2((1.10000002f - normScreenUV.x) * (currentResolution.x / currentResolution.y), 
                                         (1.10000002f - normScreenUV.y));
                float glowDistanceFactor = length(gv_coord);

                float glowBase = pow((0.119999997f / max(0.0001f, glowDistanceFactor)), 1.5f);
                float3 glowColor = float3(1.0f, 0.800000012f, 0.400000006f);
                float glowModulation = (_l * 0.300000012f) + 0.699999988f;
                _col += _GlowIntensity * glowBase * glowColor * glowModulation;
                
                _col = max(_col, float3(0.0f, 0.0f, 0.0f));
                _col = _col / (1.0f + _col); 
                _col = pow(_col, float3(0.454545438f, 0.454545438f, 0.454545438f));

                return float4(_col, 1.0f);
            }
            ENDHLSL
        }
    }
    Fallback "Diffuse"
}