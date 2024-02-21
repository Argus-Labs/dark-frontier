// HLSL Volumetric Noise
// Values inspired by: https://www.shadertoy.com/view/ssj3Wc

float3 randomNoise(float3 p)
{
    // Replace these magic numbers to your liking.
    p = float3(dot(p, float3(127.1, 311.7, 69.5)),
        dot(p, float3(269.5, 183.3, 132.7)),
        dot(p, float3(247.3, 108.5, 96.5)));

    // 43758.5453123 is a common noise scalar of unknown origin.
    return -1.0 + 2.0 * frac(cos(p) * 43758.5453123);
}

float perlin(float3 p)
{
    float3 t = frac(p);
    p = floor(p);

    float a = dot(randomNoise(p), t);
    float b = dot(randomNoise(p + float3(1, 0, 0)), t - float3(1, 0, 0));
    float c = dot(randomNoise(p + float3(0, 1, 0)), t - float3(0, 1, 0));
    float d = dot(randomNoise(p + float3(0, 0, 1)), t - float3(0, 0, 1));
    float e = dot(randomNoise(p + float3(1, 1, 0)), t - float3(1, 1, 0));
    float f = dot(randomNoise(p + float3(1, 0, 1)), t - float3(1, 0, 1));
    float g = dot(randomNoise(p + float3(0, 1, 1)), t - float3(0, 1, 1));
    float h = dot(randomNoise(p + float3(1, 1, 1)), t - float3(1, 1, 1));

    // This is important because it smooths out some otherwise blocky features.
    t = smoothstep(0.0, 1.0, t);

    return lerp(lerp(lerp(a, b, t.x), lerp(c, e, t.x), t.y), lerp(lerp(d, f, t.x), lerp(g, h, t.x), t.y), t.z);
}

void Turbulence3_Octaves4_float(float3 p, float lacunarity, float persistence, out float Out)
{
    const int numOctaves = 4;
    float scale = 1.0;
    float scaleAcc = 0.0;
    float t = 0.0;
    
    for (int i = 0; i < numOctaves; ++i)
    {
        scaleAcc += scale;
        t += scale * abs(perlin(p));
        p *= lacunarity;
        scale *= persistence;
    }

    Out = saturate(t / scaleAcc);
}

void RippledTurbulence3_Octaves3_float(float3 p, float lacunarity, float persistence, float foldiness, out float Out)
{
    const int numOctaves = 3;
    float scale = 1.0;
    float scaleAcc = 0.0;
    float t = 0.0;
    
    for (int i = 0; i < numOctaves; ++i)
    {
        scaleAcc += scale;
        t += scale * sin(foldiness * (perlin(p)));
        p *= lacunarity;
        scale *= persistence;
    }

    Out = saturate(t / scaleAcc);
}
