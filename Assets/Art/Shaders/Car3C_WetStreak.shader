// 젖은 노면에 길게 비친 불빛(가로등·후미등). 바닥에 눕힌 쿼드에 쓰고, 밤에만 켜지는 조명 오브젝트 아래에 둔다.
// 세기는 전역 _Car3CWetness(비 올 때 1, 아니면 0)를 곱하므로 비가 그치면 저절로 사라진다.
// uv.x = 폭, uv.y = 길이. _Symmetric = 1이면 가운데가 가장 밝고 양쪽으로, 0이면 uv.y = 1(빛 쪽)에서 0으로 흐려진다.
Shader "Car3C/WetStreak"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 0.7, 0.35, 1)
        _Symmetric ("Symmetric (0/1)", Float) = 1
        _Width ("Core Width", Range(0.05, 0.5)) = 0.22
        _Shimmer ("Shimmer", Range(0, 1)) = 0.55
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-20" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Streak"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Art/Shaders/Car3C_Bend.hlsl"  // Car3C: 도로 굴곡

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Symmetric;
                float _Width;
                float _Shimmer;
            CBUFFER_END

            float _Car3CWetness;
            float _Car3CRoadFlow;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float roadZ : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.uv = input.uv;
                output.roadZ = positions.positionWS.z + _Car3CRoadFlow;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            float Hash(float x)
            {
                return frac(sin(x * 127.1) * 43758.5453);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                if (_Car3CWetness <= 0.001) return half4(0, 0, 0, 0);

                float across = abs(input.uv.x - 0.5) * 2.0;
                float core = exp(-across * across / (_Width * _Width));

                float along = _Symmetric > 0.5 ? 1.0 - abs(input.uv.y - 0.5) * 2.0 : input.uv.y;
                along = saturate(along);
                along = along * along * (1.5 - 0.5 * along);

                // 노면 잔물결에 흔들리며 끊기는 반사: 도로 좌표에 붙은 띠 + 시간에 따른 흔들림.
                float band = floor(input.roadZ * 3.0 + _Time.y * 2.0);
                float flicker = lerp(1.0, 0.35 + 0.65 * Hash(band), _Shimmer);

                float intensity = core * along * flicker * _Car3CWetness;
                half3 color = _Color.rgb * intensity;
                color = MixFogColor(color, half3(0, 0, 0), input.fogFactor);
                return half4(color, 0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
