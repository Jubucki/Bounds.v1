Shader "Custom/WobblyPaintFill"
{
    Properties
    {
        _FillColorA ("Fill Color A", Color) = (0.94,0.6,0.48,1)
        _FillColorB ("Fill Color B", Color) = (0.83,0.33,0.49,1)
        _BorderColor ("Border Color", Color) = (1,1,1,1)
        _BorderThickness ("Border Thickness (units)", Range(0,0.5)) = 0.05
        _WobbleScale ("Wobble Scale (units)", Range(0,0.5)) = 0.09
        _WobbleSpeed ("Wobble Speed", Float) = 0.4
        _NoiseFrequency ("Noise Frequency", Float) = 1.8
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionHCS:SV_POSITION; float2 uv:TEXCOORD0; float2 scale:TEXCOORD1; };

            float4 _FillColorA;
            float4 _FillColorB;
            float4 _BorderColor;
            float _BorderThickness;
            float _WobbleScale;
            float _WobbleSpeed;
            float _NoiseFrequency;

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.scale = float2(
                    length(unity_ObjectToWorld._m00_m10_m20),
                    length(unity_ObjectToWorld._m01_m11_m21)
                );
                return o;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash(i);
                float b = hash(i + float2(1,0));
                float c = hash(i + float2(0,1));
                float d = hash(i + float2(1,1));
                float2 u = f*f*(3.0-2.0*f);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            float fbm(float2 p)
            {
                float sum = 0.0;
                float amp = 0.5;
                float freq = 1.0;

                for (int k = 0; k < 4; k++)
                {
                    sum += noise(p * freq) * amp;
                    freq *= 2.3;
                    amp *= 0.55;
                }

                return sum;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 halfSize = i.scale * 0.5;
                float2 p = (i.uv * 2.0 - 1.0) * halfSize;

                float t = _Time.y * _WobbleSpeed;
                float n = fbm(p * _NoiseFrequency + t);

                float2 q = abs(p) - halfSize;
                float sd = min(max(q.x, q.y), 0.0) + length(max(q, 0.0));
                sd += (n - 0.5) * _WobbleScale;

                float aa = 0.004;
                float outerEdge = smoothstep(-aa, aa, sd);
                float innerEdge = smoothstep(-_BorderThickness - aa, -_BorderThickness + aa, sd);

                float fillMask = 1.0 - innerEdge;
                float borderMask = innerEdge - outerEdge;

                float4 fillColor = lerp(_FillColorA, _FillColorB, n);
                float4 col = fillColor * fillMask + _BorderColor * borderMask;
                col.a = fillMask + borderMask;

                clip(col.a - 0.01);
                return col;
            }
            ENDHLSL
        }
    }
}