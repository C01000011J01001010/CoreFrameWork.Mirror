Shader "Map/OutlineComposite"
{
    Properties
    {
        _MainTex ("Base Map", 2D) = "white" {}
        _MaskTex ("Target Depth Mask", 2D) = "black" {} 
        _GlobalDepthTex ("Global Scene Depth", 2D) = "black" {} 
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Float) = 2.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "MapOutlinePass_URP_Depth"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_MaskTex); SAMPLER(sampler_MaskTex);
            TEXTURE2D(_GlobalDepthTex); SAMPLER(sampler_GlobalDepthTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _PixelSize;
                float4 _OutlineColor;
                float _OutlineThickness;
                float _DepthThreshold; // 🌟 임계값 추가
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // 타겟의 깊이값 (유저님 셰이더 특성상 가까울수록 1, 멀수록 0)
                float targetDepth = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv).r;
                float centerMask = step(0.0001, targetDepth);

                float2 offset = _PixelSize.xy * _OutlineThickness;
                float edge = 0;
                float maxEdgeDepth = 0;

                float2 offsets[8] = {
                    float2(offset.x, 0), float2(-offset.x, 0), float2(0, offset.y), float2(0, -offset.y),
                    float2(offset.x, offset.y), float2(-offset.x, -offset.y), float2(-offset.x, offset.y), float2(offset.x, -offset.y)
                };

                for(int i = 0; i < 8; i++)
                {
                    float neighborDepth = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv + offsets[i]).r;
                    edge += step(0.0001, neighborDepth);
                    maxEdgeDepth = max(maxEdgeDepth, neighborDepth);
                }

                float isOutline = step(0.1, edge) * (1.0 - centerMask);

                // 화면에 찍힌 가장 앞에 있는 물체의 깊이
                float globalDepth = SAMPLE_TEXTURE2D(_GlobalDepthTex, sampler_GlobalDepthTex, input.uv).r;

                // 🌟 완벽한 깊이 비교 연산
                // 타겟의 엣지 깊이에서 임계값을 뺀 값보다, 글로벌 깊이가 작거나 같다(더 뒤에 있다)면 화면에 보임!
                // (가까울수록 1.0, 멀수록 0.0 인 깊이 구조에 맞춘 로직)
                float isVisible = step(globalDepth, maxEdgeDepth + _DepthThreshold);

                return lerp(baseColor, _OutlineColor, isOutline * isVisible * _OutlineColor.a);
            }
            ENDHLSL
        }
    }
}