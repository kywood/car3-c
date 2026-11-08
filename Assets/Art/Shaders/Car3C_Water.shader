// 항구 바다용 모바일 물 셰이더 (URP, 불투명, 텍스처 없음).
// - 물결: 정수 파수 벡터의 너울(사인) + 주기 노이즈 잔물결. 모든 무늬가 _TileLength(m) 주기로 반복되므로
//   SeaView가 넘기는 _FlowOffset(주행 거리 mod _TileLength)로 끊김 없이 흘러간다.
// - 하늘 반사(Fresnel, 반사 프로브/스카이박스), 태양 반짝임, 해안(_ShoreX) 쪽 얕은 색과 거품, 안개, 그림자 수신.
Shader "Car3C/Water"
{
    Properties
    {
        [Header(Color)]
        _DeepColor ("Deep Color", Color) = (0.012, 0.055, 0.085, 1)
        _ShallowColor ("Shallow Color", Color) = (0.05, 0.22, 0.24, 1)
        _ScatterColor ("Crest Scatter Color", Color) = (0.06, 0.28, 0.26, 1)
        _ShoreX ("Shore World X", Float) = 32
        _ShallowWidth ("Shallow Width (m)", Float) = 14

        [Header(Waves)]
        _TileLength ("Tile Length (m)", Float) = 64
        _SwellStrength ("Swell Slope", Range(0, 0.6)) = 0.16
        _SwellSpeed ("Swell Time Scale", Range(0, 2)) = 0.8
        _RippleStrength ("Ripple Slope", Range(0, 1.5)) = 0.42
        _RippleSpeed ("Ripple Drift (cells/s)", Range(0, 4)) = 0.6
        _DetailDistance ("Ripple Fade Distance (m)", Float) = 220

        [Header(Reflection)]
        _ReflectionStrength ("Reflection Strength", Range(0, 1.5)) = 1
        _Roughness ("Near Roughness", Range(0, 1)) = 0.06
        _FarRoughness ("Far Roughness", Range(0, 1)) = 0.22
        _SunSharpness ("Sun Glint Sharpness", Range(16, 2048)) = 700
        _SunIntensity ("Sun Glint Intensity", Range(0, 20)) = 6

        [Header(Foam)]
        _FoamColor ("Foam Color", Color) = (0.86, 0.9, 0.92, 1)
        _ShoreFoamWidth ("Shore Foam Width (m)", Float) = 2.5
        _CapThreshold ("Whitecap Threshold", Range(0.5, 1)) = 0.86
        _CapAmount ("Whitecap Amount", Range(0, 1)) = 0.35

        [HideInInspector] _FlowOffset ("Flow Offset (m)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #define CAR3C_BEND_WATER_LEVEL  // 바다는 수평 유지(굴곡은 좌우만, 높이는 땅이 낮아질 때만)
        #include "Assets/Art/Shaders/Car3C_Bend.hlsl"  // Car3C: 도로 굴곡

        CBUFFER_START(UnityPerMaterial)
            float4 _DeepColor;
            float4 _ShallowColor;
            float4 _ScatterColor;
            float4 _FoamColor;
            float _ShoreX;
            float _ShallowWidth;
            float _TileLength;
            float _SwellStrength;
            float _SwellSpeed;
            float _RippleStrength;
            float _RippleSpeed;
            float _DetailDistance;
            float _ReflectionStrength;
            float _Roughness;
            float _FarRoughness;
            float _SunSharpness;
            float _SunIntensity;
            float _ShoreFoamWidth;
            float _CapThreshold;
            float _CapAmount;
            float _FlowOffset;
        CBUFFER_END

        // 전역 값: 맑을 때 1, 비·눈일 때 0 (EnvironmentView가 설정). 흐린 날 해 반짝임을 없앤다.
        float _Car3CSunGlintScale;

        struct DepthAttributes
        {
            float4 positionOS : POSITION;
        };

        struct DepthVaryings
        {
            float4 positionCS : SV_POSITION;
        };

        DepthVaryings DepthVert(DepthAttributes input)
        {
            DepthVaryings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            return output;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            float Hash(float2 cell)
            {
                return frac(sin(dot(cell, float2(12.9898, 78.233))) * 43758.5453);
            }

            // period 칸마다 반복되는 값 노이즈. x = 값(0~1), yz = 칸 단위 기울기.
            float3 TiledNoise(float2 p, float period, float seed)
            {
                float2 cell = floor(p);
                float2 f = p - cell;
                float2 u = f * f * (3.0 - 2.0 * f);
                float2 du = 6.0 * f * (1.0 - f);

                float2 c0 = cell - period * floor(cell / period);
                float2 c1 = c0 + 1.0;
                c1 -= period * floor(c1 / period);

                float a = Hash(c0 + seed);
                float b = Hash(float2(c1.x, c0.y) + seed);
                float c = Hash(float2(c0.x, c1.y) + seed);
                float d = Hash(c1 + seed);
                float k = a - b - c + d;

                float value = a + (b - a) * u.x + (c - a) * u.y + k * u.x * u.y;
                float2 gradient = du * float2(b - a + k * u.y, c - a + k * u.x);
                return float3(value, gradient);
            }

            // 정수 파수 (n.x, n.y) / _TileLength 의 너울. x = 높이(-1~1), yz = 미터당 기울기.
            float3 Swell(float2 position, float2 n, float time)
            {
                float2 k = TWO_PI * n / _TileLength;
                float omega = sqrt(9.81 * length(k)) * _SwellSpeed;
                float phase = dot(k, position) - TWO_PI * frac(omega * time / TWO_PI);
                float s, c;
                sincos(phase, s, c);
                return float3(s, c * normalize(k));
            }

            float3 Ripple(float2 position, float cells, float drift, float seed, float time)
            {
                float2 p = position * (cells / _TileLength);
                // 축마다 따로 감아야 frac가 넘어갈 때 정확히 한 주기(cells)만큼 건너뛰어 끊김이 없다.
                p += frac(time * drift * float2(0.8, 0.6) / cells) * cells;
                float3 noise = TiledNoise(p, cells, seed);
                noise.yz *= cells / _TileLength;
                return noise;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float3 positionWS = input.positionWS;
                float2 position = positionWS.xz + float2(0.0, _FlowOffset);
                float time = _Time.y;

                float3 toCamera = _WorldSpaceCameraPos - positionWS;
                float viewDistance = length(toCamera);
                float3 viewDir = toCamera / viewDistance;
                float detail = saturate(1.0 - viewDistance / _DetailDistance);

                float3 s0 = Swell(position, float2(1.0, 2.0), time);
                float3 s1 = Swell(position, float2(-2.0, 3.0), time);
                float3 s2 = Swell(position, float2(3.0, 4.0), time);
                float3 s3 = Swell(position, float2(-4.0, 7.0), time);
                float2 swellSlope = s0.yz * 0.5 + s1.yz * 0.3 + s2.yz * 0.2 + s3.yz * 0.12;
                float swellHeight = s0.x * 0.5 + s1.x * 0.3 + s2.x * 0.2;

                float3 r0 = Ripple(position, 16.0, _RippleSpeed, 3.0, time);
                float3 r1 = Ripple(position, 32.0, -_RippleSpeed * 1.3, 11.0, time);
                float3 r2 = Ripple(position, 64.0, _RippleSpeed * 1.7, 23.0, time);
                float2 rippleSlope = (r0.yz * 2.2 + r1.yz * 1.3 + r2.yz * 0.7) * detail;

                float2 slope = swellSlope * _SwellStrength + rippleSlope * _RippleStrength;
                float3 normalWS = normalize(float3(-slope.x, 1.0, -slope.y));

                float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 lightColor = mainLight.color * mainLight.shadowAttenuation;
                float3 lightDir = mainLight.direction;

                float nDotV = saturate(dot(normalWS, viewDir));
                float nDotL = saturate(dot(normalWS, lightDir));
                float3 ambient = SampleSH(normalWS);

                float shoreDistance = max(positionWS.x - _ShoreX, 0.0);
                float shallow = exp(-shoreDistance / max(_ShallowWidth, 0.01));
                float3 waterColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, shallow);
                float crest = saturate(swellHeight * 0.5 + 0.5);
                float backLight = pow(saturate(dot(viewDir, -lightDir) + 0.3), 3.0);
                float3 body = waterColor * (ambient + lightColor * nDotL * 0.5)
                    + _ScatterColor.rgb * crest * crest * backLight * lightColor * 0.35;

                float3 reflectDir = reflect(-viewDir, normalWS);
                reflectDir.y = abs(reflectDir.y);
                float roughness = lerp(_FarRoughness, _Roughness, detail);
                float3 reflection = GlossyEnvironmentReflection(reflectDir, roughness, 1.0) * _ReflectionStrength;
                float fresnel = 0.02 + 0.98 * pow(1.0 - nDotV, 5.0);

                float3 halfDir = normalize(lightDir + viewDir);
                float nDotH = saturate(dot(normalWS, halfDir));
                float sharpness = lerp(_SunSharpness * 0.25, _SunSharpness, detail);
                float glint = pow(nDotH, sharpness) * _SunIntensity * step(0.0, lightDir.y) * _Car3CSunGlintScale;

                float3 color = lerp(body, reflection, fresnel) + glint * lightColor;

                float shoreBand = saturate(1.0 - shoreDistance / max(_ShoreFoamWidth, 0.01));
                float shoreFoam = saturate((shoreBand * 1.5 - (1.0 - r1.x)) * 3.0);
                float caps = smoothstep(_CapThreshold, 1.0, r0.x * 0.6 + crest * 0.4) * _CapAmount * detail;
                float foam = saturate(shoreFoam + caps);
                float3 foamColor = _FoamColor.rgb * (SampleSH(float3(0.0, 1.0, 0.0)) + lightColor * saturate(lightDir.y));
                color = lerp(color, foamColor, foam);

                color = MixFog(color, input.fogFactor);
                return float4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            half DepthFrag(DepthVaryings input) : SV_Target
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthNormalsFrag

            half4 DepthNormalsFrag(DepthVaryings input) : SV_Target
            {
                return half4(0.0, 1.0, 0.0, 0.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
