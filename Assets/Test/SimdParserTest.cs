using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

public class SimdParserTest : MonoBehaviour
{
    private const int LineCount = 1_000_000;
    private string testFilePath;

    void Start()
    {
        testFilePath = Application.dataPath + "/test_points.txt";

        if(!File.Exists(testFilePath))
        // 1. 自动生成测试数据
            GenerateTestFile(testFilePath, LineCount);

        var lines = File.ReadAllLines(testFilePath);
        
        // Warmup
        LoadPointsWithFloatParse(testFilePath);
        SimdTxtParser.SpanSliceParse(lines);
        SimdTxtParser.ParseFile(testFilePath);

        var sw = Stopwatch.StartNew();
        var points1=SimdTxtParser.SpanSliceParse(lines);
        
        sw.Stop();
        UnityEngine.Debug.Log($"Span.Slice+IndexOf: {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        var points2 = SimdTxtParser.ParseFile(testFilePath);
        sw.Stop();
        UnityEngine.Debug.Log($"SIMD Scan: {sw.ElapsedMilliseconds} ms");
        
        sw.Restart();
        var points3 = LoadPointsWithFloatParse(testFilePath);
        sw.Stop();
        UnityEngine.Debug.Log($"Float.Parse: {sw.ElapsedMilliseconds} ms");

        // 4. 随机检查前 3 行，保证结果一致
        for (int i = 0; i < 3; i++)
        {
            Debug.Log($"Line {i}: Parse={points1[i]}, SIMD={points2[i]}, Float.Parse={points3[i]}");
        }
    }

    /// <summary>生成测试用的 txt 文件</summary>
    private void GenerateTestFile(string filePath, int count)
    {
        using (StreamWriter sw = new StreamWriter(filePath))
        {
            System.Random rand = new System.Random();
            for (int i = 0; i < count; i++)
            {
                float x = (float)(rand.NextDouble() * 1000 - 500);
                float y = (float)(rand.NextDouble() * 1000 - 500);
                float z = (float)(rand.NextDouble() * 1000 - 500);
                float r = (float)(rand.NextDouble() * 255);
                float g = (float)(rand.NextDouble() * 255);
                float b = (float)(rand.NextDouble() * 255);
                float a = (float)rand.NextDouble();

                sw.WriteLine($"{x:F2} {y:F2} {z:F2} {r:F2} {g:F2} {b:F2} {a:F2}");
            }
        }

        Debug.Log($"测试文件已生成: {filePath}");
    }

    /// <summary>标准 float.Parse 方式</summary>
    private Point[] LoadPointsWithFloatParse(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);
        Point[] points = new Point[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            string[] parts = lines[i].Split(' ');
            float x = float.Parse(parts[0]);
            float y = float.Parse(parts[1]);
            float z = float.Parse(parts[2]);
            float r = float.Parse(parts[3]);
            float g = float.Parse(parts[4]);
            float b = float.Parse(parts[5]);
            float a = float.Parse(parts[6]);

            points[i].Position = new Vector3(x, y, z);
            points[i].Color = new Vector4(r, g, b, a);
        }

        return points;
    }
}
