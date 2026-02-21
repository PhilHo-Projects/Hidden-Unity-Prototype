Shader "UI/SmoothBorderTrail"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        
        _TrailColor ("Trail Color", Color) = (1,1,1,1)
        _TrailSpeed ("Trail Speed", Range(0.1, 5.0)) = 1.0
        _TrailIntensity ("Trail Intensity", Range(0, 10)) = 0
        _TrailLength ("Trail Length", Range(0.1, 1.0)) = 0.3
        _TrailWidth ("Trail Width", Range(0.01, 0.1)) = 0.03
        _TrailSharpness ("Trail Sharpness", Range(1, 10)) = 3
        _TrailEnabled ("Trail Enabled", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _TrailColor;
            float _TrailSpeed;
            float _TrailIntensity;
            float _TrailLength;
            float _TrailWidth;
            float _TrailSharpness;
            float _TrailEnabled;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color;
                return OUT;
            }

            // Convert UV to border distance (0-1 around perimeter)
            float GetBorderDistance(float2 uv)
            {
                float2 centered = abs(uv - 0.5) * 2.0; // Convert to -1 to 1, then abs
                float2 edge = max(centered.x, centered.y); // Distance to closest edge
                
                // Calculate which edge we're closest to and position along perimeter
                if (centered.x > centered.y) {
                    // Left or right edge
                    if (uv.x > 0.5) {
                        // Right edge: 0.25 to 0.5
                        return 0.25 + (1.0 - uv.y) * 0.25;
                    } else {
                        // Left edge: 0.75 to 1.0
                        return 0.75 + uv.y * 0.25;
                    }
                } else {
                    // Top or bottom edge
                    if (uv.y > 0.5) {
                        // Top edge: 0 to 0.25
                        return uv.x * 0.25;
                    } else {
                        // Bottom edge: 0.5 to 0.75
                        return 0.5 + (1.0 - uv.x) * 0.25;
                    }
                }
            }

            // Distance from point to border
            float DistanceToBorder(float2 uv)
            {
                float2 d = abs(uv - 0.5) - 0.5;
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // Early exit if trail is disabled or intensity is 0
                if (_TrailEnabled == 0 || _TrailIntensity <= 0) {
                    color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                    return color;
                }
                
                // Get distance to border
                float borderDist = abs(DistanceToBorder(IN.texcoord));
                
                // Only apply effect near the border
                if (borderDist > _TrailWidth) {
                    color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                    return color;
                }
                
                // Get current position along border perimeter
                float borderPos = GetBorderDistance(IN.texcoord);
                
                // Calculate trail head position
                float currentTime = _Time.y * _TrailSpeed;
                float trailHead = fmod(currentTime, 1.0);
                
                // Calculate distance from this pixel to the trail
                float trailDist = abs(borderPos - trailHead);
                // Handle wrap-around
                trailDist = min(trailDist, 1.0 - trailDist);
                
                // Create trail mask
                float trailMask = 1.0 - smoothstep(0.0, _TrailLength, trailDist);
                
                // Apply distance-based fade for border width
                float borderMask = 1.0 - smoothstep(0.0, _TrailWidth, borderDist);
                
                // Combine masks with sharpness
                float finalMask = pow(trailMask * borderMask, 1.0 / _TrailSharpness);
                
                // Apply trail effect
                color.rgb += _TrailColor.rgb * finalMask * _TrailIntensity;
                color.a = max(color.a, finalMask * _TrailColor.a);
                
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                
                return color;
            }
            ENDCG
        }
    }
}
