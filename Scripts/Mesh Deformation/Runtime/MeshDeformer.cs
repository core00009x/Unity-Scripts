using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(Collider))]
public class MeshDeformer : MonoBehaviour
{
    [Header("Deformation Settings")]
    public float radius = 1f;
    public float strength = 1f;
    public Texture2D heightMap;
    public bool useGPU = false;

    [Header("Collision Settings")]
    public LayerMask collisionLayer;

    public ComputeShader deformationShader;
    private MeshDeformerGPU gpuDeformer;

    public int maxUndoSteps = 10;
    private Stack<Vector3[]> undoStack = new();

    private MeshFilter meshFilter;
    private Mesh deformingMesh;
    private Vector3[] originalVertices;
    private Vector3[] displacedVertices;

    private Collider meshCollider;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        deformingMesh = meshFilter.mesh;
        deformingMesh.MarkDynamic();

        originalVertices = deformingMesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];
        System.Array.Copy(originalVertices, displacedVertices, originalVertices.Length);

        meshCollider = GetComponent<Collider>();
        gpuDeformer = new MeshDeformerGPU(deformationShader);
        PushUndo();
    }

    void Update()
    {
        DetectAndDeformCollisions();
        deformingMesh.vertices = displacedVertices;
        deformingMesh.RecalculateNormals();
    }

    void DetectAndDeformCollisions()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity, collisionLayer))
        {
            Vector3 hitPoint = hit.point;
            Vector3 localHitPoint = transform.InverseTransformPoint(hitPoint);
            ApplyDeformation(localHitPoint, hit.collider.gameObject);
        }
    }

    void ApplyDeformation(Vector3 point, GameObject collisionObject)
    {
        float impactForce = CalculateImpactForce(collisionObject);
        strength = Mathf.Clamp(impactForce, 0f, 10f);

        if (useGPU)
        {
            GPUDeform(point, collisionObject);
        }
        else
        {
            CPUDeform(point, collisionObject);
        }
    }

    float CalculateImpactForce(GameObject collisionObject)
    {
        Rigidbody rb = collisionObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            return rb.linearVelocity.magnitude * rb.mass;
        }
        return 1f;
    }

    void CPUDeform(Vector3 point, GameObject collisionObject)
    {
        MeshDeformerJobDispatcher.ScheduleDeformation(
            originalVertices,
            ref displacedVertices,
            point,
            radius,
            strength,
            Time.deltaTime,
            heightMap);

        PushUndo();
    }

    void GPUDeform(Vector3 point, GameObject collisionObject)
    {
        Vector3 localPoint = transform.InverseTransformPoint(point);
        gpuDeformer.Deform(deformingMesh, localPoint, radius, strength, Time.deltaTime);
    }

    public void Undo()
    {
        if (undoStack.Count > 1)
        {
            undoStack.Pop();
            displacedVertices = undoStack.Peek();
            deformingMesh.vertices = displacedVertices;
            deformingMesh.RecalculateNormals();
        }
    }

    void PushUndo()
    {
        if (undoStack.Count >= maxUndoSteps) undoStack.Clear();
        Vector3[] copy = new Vector3[displacedVertices.Length];
        System.Array.Copy(displacedVertices, copy, displacedVertices.Length);
        undoStack.Push(copy);
    }
}
