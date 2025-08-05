using UnityEngine;

public class MeshDeformerGPU
{
    private ComputeShader computeShader;
    private int kernelIndex;

    public MeshDeformerGPU(ComputeShader shader)
    {
        computeShader = shader;
        kernelIndex = computeShader.FindKernel("CSMain");
    }

    public void Deform(Mesh mesh, Vector3 point, float radius, float strength, float deltaTime)
    {
        Vector3[] vertices = mesh.vertices;
        ComputeBuffer vertexBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
        vertexBuffer.SetData(vertices);

        computeShader.SetBuffer(kernelIndex, "vertices", vertexBuffer);
        computeShader.SetInt("vertexCount", vertices.Length);
        computeShader.SetFloat("radius", radius);
        computeShader.SetFloat("strength", strength);
        computeShader.SetFloat("deltaTime", deltaTime);
        computeShader.SetVector("deformPoint", point);

        int threadGroups = Mathf.CeilToInt(vertices.Length / 64f);
        computeShader.Dispatch(kernelIndex, threadGroups, 1, 1);

        vertexBuffer.GetData(vertices);
        mesh.vertices = vertices;
        mesh.RecalculateNormals();

        vertexBuffer.Dispose();
    }
}
