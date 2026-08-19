Shader "Custom/URP_LaserCylinder"
{
    Properties
    {
        [HDR] _BaseColor ("Laser Color (HDR)", Color) = (1, 0.2, 0.2, 1)
        _StartRadius ("Start Radius (0~1)", Range(0.0, 1.0)) = 0.3
        _EndAlpha ("End Alpha (0~1)", Range(0.0, 1.0)) = 0.0
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode (0:Off, 1:Front, 2:Back)", Float) = 2
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
        }
        LOD 100

        // 알파 블렌딩 및 깊이 설정
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull [_CullMode]

        Pass
        {
            Name "LaserPass"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS  : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _StartRadius;
                float _EndAlpha;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // 1. 오브젝트 공간(Object Space)에서의 카메라 위치 및 시선 벡터 계산
                float3 cameraPosOS = TransformWorldToObject(_WorldSpaceCameraPos);
                float3 viewDirOS = normalize(cameraPosOS - input.positionOS);

                // 2. 시선 방향의 XZ 평면 성분 크기 (옆면 관찰 vs 위/아래 관찰 각도 비율)
                float sinAngle = length(viewDirOS.xz);

                // 3. [옆면 시점] 시선에 수직인 레이저 중심축(Y축)과의 직교 거리 연산
                float d_side = abs(input.positionOS.x * viewDirOS.z - input.positionOS.z * viewDirOS.x) / max(sinAngle, 0.0001);

                // 4. [위/아래 시점] 중심축(Y축)으로부터의 2D 원형 거리 연산
                float d_top = length(input.positionOS.xz);

                // 5. 관찰 각도(sinAngle)에 따라 두 거리값을 부드럽게 보간하여 통합 거리(projDist) 산출
                float projDist = lerp(d_top, d_side, saturate(sinAngle));

                // 6. 반지름 정규화 (기본 Cylinder 반지름 0.5 -> 1.0 범위로 확장)
                float normalizedDist = saturate(projDist * 2.0);

                // 7. 유저 지정 구간(startRadius ~ 1.0) 보간 계수 t 연산
                float t = saturate((normalizedDist - _StartRadius) / max(1.0 - _StartRadius, 0.0001));

                // 8. 알파값 lerp 적용 (0 ~ startRadius 구간은 Alpha = 1.0, 이후 endAlpha로 보간)
                float finalAlpha = lerp(1.0, _EndAlpha, t);

                half4 finalColor = _BaseColor;
                finalColor.a *= finalAlpha;

                return finalColor;
            }
            ENDHLSL
        }
    }
}