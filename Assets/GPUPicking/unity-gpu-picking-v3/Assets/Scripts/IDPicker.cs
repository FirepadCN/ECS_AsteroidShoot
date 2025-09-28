using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class IDPicker : MonoBehaviour
{
    [Header("Scene")]
    public Camera sceneCamera;
    [Range(1000, 5_000_000)]
    public int pointCount = 200_000;
    public float cloudRadius = 15f;
    [Tooltip("Cloud will be placed ~25 units *in front of the camera*.")]
    public float cloudDistance = 25f;

    [Header("Picking RT")]
    public int pickWidth = 0, pickHeight = 0; // 0 = match screen
    public float pointSize = 8f;
    public bool useNonJitteredProjection = true;

    [Header("Materials (auto-created if null)")]
    public Material idEncodeMat;      // Hidden/PointIDEncode_NoGS
    public Material visualizeMat;     // Hidden/PointVisualize_NoGS

    [Header("Debug/Output")]
    public Transform lastPickedTransform;

    // Buffers
    ComputeBuffer pointsBuffer;    // float3 positions
    ComputeBuffer idsBuffer;       // uint ids 0..N-1
    Vector3[] cpuPositions;        // CPU mirror for demo

    RenderTexture idRT;
    uint lastPicked = UInt32.MaxValue;
    Vector3 lastPickedPos;

    void Awake()
    {
        if (!sceneCamera) sceneCamera = Camera.main;

        if (!idEncodeMat)
            idEncodeMat = new Material(Shader.Find("Hidden/PointIDEncode_NoGS"));
        if (!visualizeMat)
            visualizeMat = new Material(Shader.Find("Hidden/PointVisualize_NoGS"));

        GeneratePointsInFrontOfCamera();
        CreateBuffersAndUpload();
        AllocatePickRT();
    }

    void OnDestroy()
    {
        SafeRelease(pointsBuffer);
        SafeRelease(idsBuffer);
        if (idRT) idRT.Release();
    }

    void Update()
    {
        DrawPointsVisual();

        if (Input.GetMouseButtonDown(0))
        {
            DoPick(Input.mousePosition);
        }
    }

    void AllocatePickRT()
    {
        int w = pickWidth > 0 ? pickWidth : Screen.width;
        int h = pickHeight > 0 ? pickHeight : Screen.height;
        if (idRT && (idRT.width != w || idRT.height != h))
        {
            idRT.Release();
            idRT = null;
        }
        if (!idRT)
        {
            idRT = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            idRT.name = "ID_RT";
            idRT.antiAliasing = 1;
            idRT.Create();
        }
    }
    void GeneratePointsInFrontOfCamera()
    {
        if (!sceneCamera) sceneCamera = Camera.main;
        Vector3 center = sceneCamera.transform.position + sceneCamera.transform.forward * cloudDistance;

        cpuPositions = new Vector3[pointCount];
        var rnd = new System.Random(1234);
        for (int i = 0; i < pointCount; i++)
        {
            float u = (float)rnd.NextDouble();
            float v = (float)rnd.NextDouble();
            float theta = 2f * Mathf.PI * u;
            float phi = Mathf.Acos(2f * v - 1f);
            float r = cloudRadius * Mathf.Pow((float)rnd.NextDouble(), 1f/3f);
            Vector3 p = new Vector3(
                r * Mathf.Sin(phi) * Mathf.Cos(theta),
                r * Mathf.Sin(phi) * Mathf.Sin(theta),
                r * Mathf.Cos(phi)
            );
            cpuPositions[i] = center + p;
        }
    }

    void CreateBuffersAndUpload()
    {
        SafeRelease(pointsBuffer);
        SafeRelease(idsBuffer);

        pointsBuffer = new ComputeBuffer(pointCount, sizeof(float) * 3);
        idsBuffer    = new ComputeBuffer(pointCount, sizeof(uint));

        pointsBuffer.SetData(cpuPositions);
        var ids = Enumerable.Range(0, pointCount).Select(i => (uint)i).ToArray();
        idsBuffer.SetData(ids);
    }

    Matrix4x4 GetProjForRT(Camera cam)
    {
        Matrix4x4 proj = useNonJitteredProjection ? cam.nonJitteredProjectionMatrix : cam.projectionMatrix;
        return GL.GetGPUProjectionMatrix(proj, true); // RT: true
    }

    Matrix4x4 GetProjForScreen(Camera cam)
    {
        return GL.GetGPUProjectionMatrix(cam.projectionMatrix, false); // screen: false
    }

    void DrawPointsVisual()
    {
        if (!visualizeMat || pointCount <= 0 || pointsBuffer == null) return;

        Matrix4x4 VP = GetProjForScreen(sceneCamera) * sceneCamera.worldToCameraMatrix;
        visualizeMat.SetMatrix("_VP", VP);
        visualizeMat.SetFloat("_PointSize", pointSize);
        visualizeMat.SetBuffer("_Points", pointsBuffer);

        // Big bounds to ensure visible
        Graphics.DrawProcedural(visualizeMat, new Bounds(sceneCamera.transform.position + sceneCamera.transform.forward * cloudDistance, Vector3.one * (cloudRadius*3f)), MeshTopology.Points, pointCount);
    }

    void DoPick(Vector2 mousePixel)
    {
        if (!idEncodeMat || pointsBuffer == null || pointCount <= 0) { Debug.LogWarning("Pick aborted"); return; }
        AllocatePickRT();

        var cmd = new CommandBuffer { name = "GPU Pick Pass" };
        cmd.SetRenderTarget(idRT);
        cmd.SetViewport(new Rect(0, 0, idRT.width, idRT.height));
        cmd.ClearRenderTarget(true, true, Color.clear);

        Matrix4x4 VP = GetProjForRT(sceneCamera) * sceneCamera.worldToCameraMatrix;

        idEncodeMat.SetMatrix("_VP", VP);
        idEncodeMat.SetFloat("_PointSize", pointSize);
        idEncodeMat.SetBuffer("_Points", pointsBuffer);
        idEncodeMat.SetBuffer("_PointIDs", idsBuffer);

        cmd.DrawProcedural(Matrix4x4.identity, idEncodeMat, 0, MeshTopology.Points, pointCount);
        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Release();

        int x = Mathf.RoundToInt(mousePixel.x * idRT.width  / (float)Screen.width);
        int y = Mathf.RoundToInt(mousePixel.y * idRT.height / (float)Screen.height);
        x = Mathf.Clamp(x, 0, idRT.width - 1);
        y = Mathf.Clamp(y, 0, idRT.height - 1);

        AsyncGPUReadback.Request(idRT, 0, request =>
        {
            if (request.hasError) { Debug.LogError("GPUReadback error"); return; }
            var data = request.GetData<Color32>();
            var c = data[y * idRT.width + x];
            uint packed = (uint)(c.r | (c.g << 8) | (c.b << 16) | (c.a << 24));
            if (packed == 0u) { lastPicked = UInt32.MaxValue; return; }
            uint id = packed - 1u;
            lastPicked = id;

            if (cpuPositions != null && id < (uint)cpuPositions.Length)
                lastPickedPos = cpuPositions[id];
            if (lastPickedTransform) lastPickedTransform.position = lastPickedPos;

            Debug.Log($"Picked id={id}, pos={lastPickedPos}");
        });
    }

    static void SafeRelease(ComputeBuffer buf)
    {
        if (buf != null) buf.Release();
    }
}
