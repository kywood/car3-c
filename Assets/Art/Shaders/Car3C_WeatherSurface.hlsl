#ifndef CAR3C_WEATHER_SURFACE_INCLUDED
#define CAR3C_WEATHER_SURFACE_INCLUDED

// 날씨 표면 효과 (Car3C/WeatherLit 전용). URP Lit의 SurfaceData/InputData를 조명 계산 직전에 바꾼다.
// - CAR3C_PUDDLES  : 젖은 노면의 물웅덩이(평평하고 거울 같은 면) + 빗방울 파문
// - CAR3C_WEATHERCOVER: 물체(건물·차량·소품) — 비: 젖어 어둡고 매끈하게 / 눈: 위를 향한 면에 쌓인 눈 + 반짝임
// 모든 값은 전역(EnvironmentView가 설정)이라 머티리얼 CBUFFER는 URP Lit과 같다(SRP Batcher 유지).
// 무늬는 주행 거리 기준 좌표(z + _Car3CRoadFlow)에 붙어 있어 도로·배경과 함께 다가온다.
// _Car3CRoadFlow는 주행 거리 mod CAR3C_FLOW_PERIOD 이므로 모든 무늬가 z 방향으로 그 주기로 반복되어야 이음새가 없다.

#define CAR3C_FLOW_PERIOD 256.0

float _Car3CRainIntensity; // 0 = 안 옴, 이슬비 ~0.35, 폭우 1
float _Car3CPuddleAmount;  // 0~1 노면에서 물웅덩이가 차지하는 정도
float _Car3CSnowCover;     // 0~1 쌓인 눈의 양
float _Car3CRoadFlow;      // 주행 거리 mod 256 (m)
float _Car3CWetness;       // 비 올 때 1

float Car3CHash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

// z 방향 period 칸마다 반복되는 값 노이즈(0~1).
float Car3CNoise(float2 p, float periodZ)
{
    float2 cell = floor(p);
    float2 f = p - cell;
    float2 u = f * f * (3.0 - 2.0 * f);
    float z0 = cell.y - periodZ * floor(cell.y / periodZ);
    float z1 = z0 + 1.0;
    z1 -= periodZ * floor(z1 / periodZ);
    float a = Car3CHash(float2(cell.x, z0));
    float b = Car3CHash(float2(cell.x + 1.0, z0));
    float c = Car3CHash(float2(cell.x, z1));
    float d = Car3CHash(float2(cell.x + 1.0, z1));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float2 Car3CRoadCoord(float3 positionWS)
{
    // 좌우 굴곡으로 옮겨진 x를 되돌려 무늬가 도로에 붙어 있게 한다.
    float x = positionWS.x;
#ifdef CAR3C_BEND_INCLUDED
    x -= Car3CBendOffset(positionWS.z).x;
#endif
    return float2(x, positionWS.z + _Car3CRoadFlow);
}

// 빗방울 파문 한 겹. 칸마다 한 방울이 떨어져 원이 퍼진다. 반환 = 노면 기울기(xz).
float2 Car3CRippleLayer(float2 p, float cellSize, float seed, float density)
{
    float2 cp = p / cellSize;
    float2 cell = floor(cp);
    float2 f = cp - cell;
    float periodZ = CAR3C_FLOW_PERIOD / cellSize;
    cell.y -= periodZ * floor(cell.y / periodZ);

    float h = Car3CHash(cell + seed);
    float time = _Time.y * 1.4 + h * 7.0;
    float cycle = floor(time);
    float t = time - cycle;
    // 매 주기 이 칸에 방울이 떨어질지(비의 세기만큼).
    float drop = step(Car3CHash(cell + seed + cycle * 0.731), density);
    float2 center = 0.3 + 0.4 * float2(Car3CHash(cell + seed + 17.0 + cycle), Car3CHash(cell + seed + 31.0 + cycle));

    float2 delta = f - center;
    float dist = length(delta);
    float radius = t * 0.3;
    float x = clamp((dist - radius) * 30.0, -PI, PI);
    float fade = (1.0 - t) * (1.0 - t);
    float ring = sin(x) * fade * drop;
    return delta / max(dist, 1e-4) * ring;
}

void Car3CApplyPuddles(float3 positionWS, inout SurfaceData surfaceData, inout InputData inputData)
{
    float2 p = Car3CRoadCoord(positionWS);

    // 물웅덩이 모양: 큰 얼룩 + 작은 얼룩. 주기 = 256m (0.125 * 256 = 32칸, 0.5 * 256 = 128칸).
    float shape = Car3CNoise(p * 0.125, 32.0) * 0.7 + Car3CNoise(p * 0.5, 128.0) * 0.3;
    float threshold = lerp(0.82, 0.52, saturate(_Car3CPuddleAmount));
    float puddle = smoothstep(threshold, threshold + 0.06, shape);
    float edge = smoothstep(threshold - 0.08, threshold, shape) - puddle;

    // 물이 고인 곳은 어둡고 매끈하며 평평하다. 가장자리는 젖은 테두리.
    surfaceData.albedo *= lerp(1.0, 0.45, puddle) * lerp(1.0, 0.8, edge);
    surfaceData.smoothness = lerp(surfaceData.smoothness, 0.98, puddle);
    surfaceData.smoothness = max(surfaceData.smoothness, lerp(surfaceData.smoothness, 0.85, edge));
    surfaceData.occlusion = lerp(surfaceData.occlusion, 1.0, puddle);

    float3 normalWS = normalize(lerp(inputData.normalWS, float3(0.0, 1.0, 0.0), puddle));

    // 빗방울 파문: 물웅덩이에서 뚜렷하고, 젖은 노면에서는 약하게.
    float density = saturate(_Car3CRainIntensity);
    float2 slope = Car3CRippleLayer(p, 0.55, 3.0, density) + Car3CRippleLayer(p + 0.27, 0.8, 11.0, density * 0.8);
    float distanceFade = saturate(1.0 - length(GetCameraPositionWS() - positionWS) / 45.0);
    slope *= (0.08 + 0.32 * puddle) * distanceFade;
    normalWS = normalize(normalWS + float3(slope.x, 0.0, slope.y));

    inputData.normalWS = normalWS;
}

// 젖은 물체: 거친(다공성) 면일수록 물을 머금어 어두워지고, 위를 향한 면일수록 물막이 생겨 매끈해진다.
void Car3CApplyWet(float3 geometricNormalWS, inout SurfaceData surfaceData)
{
    float wet = saturate(_Car3CWetness);
    if (wet <= 0.001) return;
    float porosity = saturate(1.0 - surfaceData.smoothness) * (1.0 - surfaceData.metallic);
    float up = saturate(geometricNormalWS.y);
    // 원래 색이 회색으로 바래지 않도록 반사는 적당히(위를 향한 면 위주), 어두워지는 정도도 제한한다.
    surfaceData.albedo *= lerp(1.0, 0.7, wet * porosity);
    surfaceData.smoothness = lerp(surfaceData.smoothness, max(surfaceData.smoothness, 0.7), wet * (0.15 + 0.6 * up));
}

void Car3CApplySnow(float3 positionWS, float3 geometricNormalWS, inout SurfaceData surfaceData, inout InputData inputData)
{
    float amount = saturate(_Car3CSnowCover);
    if (amount <= 0.001) return;

    float2 p = Car3CRoadCoord(positionWS);
    float breakup = Car3CNoise(p * 1.5, 384.0) * 0.6 + Car3CNoise(p * 6.0, 1536.0) * 0.4;
    float up = geometricNormalWS.y;
    // 눈이 많을수록 덜 평평한 면까지 덮는다.
    float start = lerp(0.85, 0.35, amount);
    float cover = smoothstep(start, start + 0.25, up + (breakup - 0.5) * 0.3);
    cover *= smoothstep(0.0, 0.35, amount + breakup * 0.2);

    half3 snowColor = half3(0.82, 0.85, 0.9);
    surfaceData.albedo = lerp(surfaceData.albedo, snowColor, cover);
    surfaceData.metallic = lerp(surfaceData.metallic, 0.0, cover);
    surfaceData.smoothness = lerp(surfaceData.smoothness, 0.35, cover);
    surfaceData.specular = lerp(surfaceData.specular, half3(0.04, 0.04, 0.04), cover);
    surfaceData.emission *= 1.0 - cover;
    inputData.normalWS = normalize(lerp(inputData.normalWS, geometricNormalWS, cover));

    // 눈 반짝임: 드문 점이 보는 각도와 해 방향이 맞을 때만 빛난다.
    float3 grid = floor(float3(p.x, positionWS.y, p.y) * 30.0);
    float glint = step(0.996, Car3CHash(grid.xz + grid.y * 17.13));
    Light mainLight = GetMainLight();
    float3 halfDir = normalize(mainLight.direction + inputData.viewDirectionWS);
    float facing = pow(saturate(dot(geometricNormalWS, halfDir)), 8.0);
    surfaceData.emission += glint * facing * cover * mainLight.color * 2.5;
}

#endif
