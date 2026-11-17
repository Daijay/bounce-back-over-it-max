Shader "BounceBack/IceShine"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Float) = 0.75
        _Tint ("Tint", Color) = (0.85,0.96,1,1)
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
                float band = frac(dot(p, float2(0.7, 0.7)) * 0.16 - t * 0.22);
                float shine = smoothstep(0.0, 0.04, band) * smoothstep(0.13, 0.05, band);
                float glint = Noise(p * 6.0 + t * 0.3);
                float lum = dot(tex.rgb, float3(0.3, 0.59, 0.11));
                float a = tex.a * input.color.a * (shine * (0.5 + lum) + glint * glint * 0.08) * _Intensity;
                return half4(_Tint.rgb, a);
            }
            ENDHLSL
        }
    }
}
