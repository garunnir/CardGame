Shader "CardBattle/CardFaceUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _SpriteAspect ("Sprite Aspect", Float) = 1
        _QuadAspect ("Quad Aspect", Float) = 0.72727275
        _SpriteFit ("Sprite Fit", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "Geometry"
        }

        LOD 100
        Cull Back
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "CardFaceUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 meshUv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Cutoff;
                half _SpriteAspect;
                half _QuadAspect;
                half _SpriteFit;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float2 ApplyHeightFillMeshUv(float2 meshUv)
            {
                if (_SpriteFit < 0.5h)
                {
                    return meshUv;
                }

                if (_SpriteAspect >= _QuadAspect)
                {
                    half uFrac = _QuadAspect / _SpriteAspect;
                    meshUv.x = (meshUv.x - 0.5h) * uFrac + 0.5h;
                    return meshUv;
                }

                half uFrac = _SpriteAspect / _QuadAspect;
                half minX = 0.5h - uFrac * 0.5h;
                half maxX = 0.5h + uFrac * 0.5h;
                meshUv.x = (meshUv.x - minX) / uFrac;
                return meshUv;
            }

            bool IsOutsideHeightFillPillarbox(float2 meshUv)
            {
                if (_SpriteFit < 0.5h || _SpriteAspect >= _QuadAspect)
                {
                    return false;
                }

                half uFrac = _SpriteAspect / _QuadAspect;
                half minX = 0.5h - uFrac * 0.5h;
                half maxX = 0.5h + uFrac * 0.5h;
                return meshUv.x < minX || meshUv.x > maxX;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.meshUv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                if (IsOutsideHeightFillPillarbox(input.meshUv))
                {
                    clip(-1);
                }

                float2 fittedUv = ApplyHeightFillMeshUv(input.meshUv);
                float2 atlasUv = fittedUv * _MainTex_ST.xy + _MainTex_ST.zw;
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, atlasUv) * _Color;
                clip(color.a - _Cutoff);
                return half4(color.rgb, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
