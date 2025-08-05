#pragma kernel CSMain

RWStructuredBuffer<float3> vertices;
float3 brushPos;
float radius;
float strength;
float deltaTime;

[numthreads(64,1,1)]
void CSMain(uint id : SV_DispatchThreadID)
{
    float3 v = vertices[id];
    float3 toVertex = v - brushPos;
    float dist = length(toVertex);

    if (dist < radius)
    {
        float falloff = smoothstep(radius, 0, dist);
        float3 displacement = normalize(toVertex) * strength * falloff * deltaTime;
        vertices[id] = v + displacement;
    }
}
