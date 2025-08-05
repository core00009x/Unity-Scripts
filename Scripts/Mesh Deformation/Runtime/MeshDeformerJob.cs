using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class MeshDeformerJobDispatcher
{
    public static void ScheduleDeformation(
        Vector3[] originalVertices,
        ref Vector3[] displacedVertices,
        Vector3 point,
        float radius,
        float strength,
        float deltaTime,
        Texture2D heightMap = null)
    {
        // Create NativeArrays for the original and deformed vertices
        NativeArray<float3> original = new NativeArray<float3>(originalVertices.Length, Allocator.TempJob);
        NativeArray<float3> deformed = new NativeArray<float3>(originalVertices.Length, Allocator.TempJob);

        // Convert Vector3[] to NativeArray<float3>
        for (int i = 0; i < originalVertices.Length; i++)
        {
            original[i] = (float3)originalVertices[i]; // Convert Vector3 to float3
        }

        NativeArray<float> heightValues = default;
        int heightMapWidth = 0;
        int heightMapHeight = 0;

        if (heightMap != null)
        {
            // Convert heightmap pixels to grayscale values and store in a NativeArray
            Color[] pixels = heightMap.GetPixels();
            heightMapWidth = heightMap.width;
            heightMapHeight = heightMap.height;
            heightValues = new NativeArray<float>(pixels.Length, Allocator.TempJob);
            for (int i = 0; i < pixels.Length; i++)
            {
                heightValues[i] = pixels[i].grayscale;
            }
        }

        // Create the job with required parameters
        DeformJob job = new DeformJob
        {
            vertices = original,
            deformed = deformed,
            point = point,
            radius = radius,
            strength = strength,
            deltaTime = deltaTime,
            useHeightMap = heightMap != null,
            heightValues = heightValues,
            width = heightMapWidth,
            height = heightMapHeight
        };

        // Schedule the job
        JobHandle handle = job.Schedule(originalVertices.Length, 64);
        handle.Complete();

        // Copy the deformed vertices back to the displacedVertices array
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            displacedVertices[i] = deformed[i];
        }

        // Dispose of NativeArrays
        original.Dispose();
        deformed.Dispose();
        if (heightValues.IsCreated)
        {
            heightValues.Dispose();
        }
    }

    [BurstCompile]
    private struct DeformJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> vertices;
        [WriteOnly] public NativeArray<float3> deformed;

        public float3 point;
        public float radius;
        public float strength;
        public float deltaTime;

        [ReadOnly] public bool useHeightMap;
        [ReadOnly] public NativeArray<float> heightValues;
        public int width;
        public int height;

        public void Execute(int index)
        {
            // Get the vertex and calculate the distance to the point
            float3 vertex = vertices[index];
            float3 toVertex = vertex - point;
            float dist = math.length(toVertex);

            // If the vertex is outside the radius, leave it unchanged
            if (dist > radius)
            {
                deformed[index] = vertex;
                return;
            }

            // Calculate the falloff based on distance from the point
            float falloff = math.smoothstep(radius, 0, dist);
            float3 dir = math.normalize(toVertex);
            float intensity = strength * falloff * deltaTime;

            // Apply heightmap-based deformation if necessary
            if (useHeightMap && width > 0 && height > 0)
            {
                // Simple UV mapping assuming vertex.x and vertex.z are in [-0.5, 0.5]
                int u = (int)math.clamp((vertex.x + 0.5f) * width, 0, width - 1);
                int v = (int)math.clamp((vertex.z + 0.5f) * height, 0, height - 1);
                int indexMap = v * width + u;

                if (indexMap < heightValues.Length)
                {
                    float heightValue = heightValues[indexMap];
                    intensity *= heightValue;
                }
            }

            // Deform the vertex
            deformed[index] = vertex + dir * intensity;
        }
    }
}
