#ifndef CAR3C_BEND_INCLUDED
#define CAR3C_BEND_INCLUDED

// 도로 굴곡(상하·좌우) 화면 효과. 게임 규칙(차선·충돌)은 곧은 도로 그대로이고 그릴 때만 앞쪽 세상을 휜다.
// 굴곡은 도로에 고정된 높이 함수 H(s) = Σ A_i sin(k_i s + φ0_i)를 차 위치에서 평평해지도록(값·기울기 0) 뺀 것:
//   offset(z) = H(d + z) - H(d) - z · H'(d)
// CPU(RoadBendView)가 주행 거리 d에서의 위상 φ_i = k_i d + φ0_i (mod 2π)를 넘긴다. 진폭이 0이면 아무 일도 하지 않는다.
// 사용법: Core.hlsl(또는 ...Input.hlsl) 다음, ...Pass.hlsl 앞에서 include → 이후 코드의 위치 변환이 휜 위치를 쓴다.

float4 _Car3CBendPhase;   // 상하 φ_i (rad)
float4 _Car3CBendK;       // 상하 k_i (rad/m)
float4 _Car3CBendAmp;     // 상하 A_i (m)
float4 _Car3CBendPhaseX;  // 좌우
float4 _Car3CBendKX;
float4 _Car3CBendAmpX;

float Car3CBendWave(float z, float4 phase, float4 k, float4 amp)
{
    float4 s0, c0;
    sincos(phase, s0, c0);
    return dot(amp, sin(phase + k * z)) - dot(amp, s0) - z * dot(amp * k, c0);
}

// 월드 위치 z(차 기준 앞쪽 거리)에서의 굴곡 (x = 좌우, y = 상하).
float2 Car3CBendOffset(float z)
{
    return float2(Car3CBendWave(z, _Car3CBendPhaseX, _Car3CBendKX, _Car3CBendAmpX),
                  Car3CBendWave(z, _Car3CBendPhase, _Car3CBendK, _Car3CBendAmp));
}

// 굴곡이 전혀 없는 평평한 세상을 차 기준으로 본 높이 변화(물처럼 수평을 유지해야 하는 면).
float Car3CBendFlatY(float z)
{
    float4 s0, c0;
    sincos(_Car3CBendPhase, s0, c0);
    return -dot(_Car3CBendAmp, s0) - z * dot(_Car3CBendAmp * _Car3CBendK, c0);
}

float3 Car3CBend(float3 positionWS)
{
    float2 o = Car3CBendOffset(positionWS.z);
#ifdef CAR3C_BEND_WATER_LEVEL
    // 바다: 수평(해수면)을 유지하고, 땅이 더 낮아지는 곳에서만 함께 내려가 땅을 덮지 않는다.
    o.y = min(Car3CBendFlatY(positionWS.z), o.y);
#endif
    return float3(positionWS.x + o.x, positionWS.y + o.y, positionWS.z);
}

float3 Car3CTransformObjectToWorldBent(float3 positionOS)
{
    return Car3CBend(TransformObjectToWorld(positionOS));
}

float4 Car3CTransformObjectToHClipBent(float3 positionOS)
{
    return TransformWorldToHClip(Car3CTransformObjectToWorldBent(positionOS));
}

VertexPositionInputs Car3CGetVertexPositionInputsBent(float3 positionOS)
{
    VertexPositionInputs input;
    input.positionWS = Car3CTransformObjectToWorldBent(positionOS);
    input.positionVS = TransformWorldToView(input.positionWS);
    input.positionCS = TransformWorldToHClip(input.positionWS);
    float4 ndc = input.positionCS * 0.5f;
    input.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
    input.positionNDC.zw = input.positionCS.zw;
    return input;
}

#ifdef CAR3C_BEND_PARTICLES
// URP 파티클 셰이더는 위치를 GetParticleVertexPositionInputs로 구하므로 그 결과도 휜다(바닥 빛·부스터 발판 등).
VertexPositionInputs Car3CGetParticleVertexPositionInputsBent(float3 positionOS)
{
    VertexPositionInputs input = GetParticleVertexPositionInputs(positionOS);
    input.positionWS = Car3CBend(input.positionWS);
    input.positionVS = TransformWorldToView(input.positionWS);
    input.positionCS = TransformWorldToHClip(input.positionWS);
    float4 ndc = input.positionCS * 0.5f;
    input.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
    input.positionNDC.zw = input.positionCS.zw;
    return input;
}
#define GetParticleVertexPositionInputs Car3CGetParticleVertexPositionInputsBent
#endif

#define TransformObjectToWorld Car3CTransformObjectToWorldBent
#define TransformObjectToHClip Car3CTransformObjectToHClipBent
#define GetVertexPositionInputs Car3CGetVertexPositionInputsBent

#endif
