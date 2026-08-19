Shader "Map/DepthStepShader"
{
    Properties
    {
        _BakeColor ("Bake Color", Color) = (1,1,1,1)
        _Steps ("Steps", Float) = 8.0
        _FinalDepthBrightness ("Final Depth Brightness", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "MapBakePass_URP"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float depth : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _BakeColor;
                float _Steps;
                float _FinalDepthBrightness; // 🌟 추가됨
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                float3 positionVS = TransformWorldToView(vertexInput.positionWS);
                output.depth = -positionVS.z / _ProjectionParams.z;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // *요청 반영: Step_1 이하(0=None, 1=Step_1)일 경우 명도 양자화 스킵, 순수 색상 반환
                if (_Steps <= 1.5)
                {
                    return _BakeColor;
                }

                float clampedDepth = saturate(1.0 - input.depth);
                float steps = max(2.0, _Steps);
                float steppedDepth = floor(clampedDepth * steps) / (steps - 1.0);

                // *요청 반영: 0.0이 아닌 finalDepthBrightness 하한선까지 맵핑
                float finalMultiplier = lerp(_FinalDepthBrightness, 1.0, steppedDepth);
                return _BakeColor * finalMultiplier;
            }
            ENDHLSL
        }
    }
}