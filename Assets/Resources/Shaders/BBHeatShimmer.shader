Shader "BounceBack/HeatShimmer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Float) = 0.28
        _Tint ("Tint", Color) = (1,0.55,0.18,1)
        [Enum(UnityEngine.Rendering.BlendMode)] _Dst ("Destination blend", Float) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Blend SrcAlpha [_Dst]
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Intensity;
                float4 _Tint;
                float _Dst;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                o.positionWS = world;
                o.positionCS = TransformWorldToHClip(world);
                o.color = input.color;
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float t = _Time.y;
                float2 p = input.positionWS.xy;
                float w = sin(p.x * 1.7 + t * 2.3 + sin(p.y * 1.3 - t * 1.9) * 1.5);
                float w2 = sin(p.x * 3.1 - t * 1.7 + p.y * 2.0);
                float band = 0.5 + 0.5 * sin(p.y * 2.4 - t * 3.2 + w * 1.4);
                float rise = pow(saturate(1.0 - input.uv.y), 1.6) * smoothstep(0.0, 0.06, input.uv.y);
                float edge = smoothstep(0.0, 0.1, input.uv.x) * smoothstep(0.0, 0.1, 1.0 - input.uv.x);
                float a = band * (0.6 + 0.4 * w2) * rise * edge * _Intensity;
                float3 col = lerp(_Tint.rgb, float3(1.0, 0.85, 0.45), band);
                return half4(col, a * tex.a);
            }
            ENDHLSL
        }
    }
}
