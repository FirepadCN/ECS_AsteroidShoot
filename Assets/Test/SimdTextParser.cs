using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Burst.Intrinsics;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

public struct Point
{
    public Vector3 Position;
    public Vector4 Color;
}

public static class SimdTxtParser
{
    // 主入口：解析 txt 文件
    public static Point[] LoadPoints(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);
        Point[] points = new Point[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            var span = lines[i];
            float[] values = ParseLine(span);
            points[i].Position = new Vector3(values[0], values[1], values[2]);
            points[i].Color = new Vector4(values[3], values[4], values[5], values[6]);
        }

        return points;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float[] ParseLine(string line, int expectedCount = 7)
    {
        ReadOnlySpan<char> span = line.AsSpan();
        float[] values = new float[expectedCount];

        int count = 0;
        int start = 0;
        int i = 0;
        int step = Vector<byte>.Count; // 16/32

        // 将 char -> byte (仅限 ASCII 范围，PLY 数据安全)
        byte[] buffer = System.Text.Encoding.ASCII.GetBytes(span.ToArray());

        // SIMD 扫描空格位置，加速分词
        while (i + step <= buffer.Length)
        {
            var block = new Vector<byte>(buffer, i);
            var cmp = Vector.Equals(block, new Vector<byte>((byte)' '));
            for (int j = 0; j < step; j++)
            {
                if (cmp[j] != 0)
                {
                    // 提取 token
                    int len = i + j - start;
                    if (len > 0)
                    {
                        values[count++] = ParseFloatAscii(buffer.AsSpan(start, len));
                        if (count == expectedCount) return values;
                    }
                    start = i + j + 1;
                }
            }
            i += step;
        }

        // 处理剩余部分
        for (; i < buffer.Length; i++)
        {
            if (buffer[i] == ' ')
            {
                int len = i - start;
                if (len > 0)
                {
                    values[count++] = ParseFloatAscii(buffer.AsSpan(start, len));
                    if (count == expectedCount) break;
                }
                start = i + 1;
            }
        }

        // 末尾 token
        if (start < buffer.Length)
        {
            values[count++] = ParseFloatAscii(buffer.AsSpan(start));
        }

        return values;
    }

    // 标量解析 (比 float.Parse 快)
    public static float ParseFloatAscii(ReadOnlySpan<byte> span)
    {
        bool neg = false;
        int i = 0;
        if (span[0] == '-') { neg = true; i++; }

        int intPart = 0;
        float fracPart = 0;
        float div = 1;

        for (; i < span.Length; i++)
        {
            byte c = span[i];
            if (c == '.') { i++; break; }
            intPart = intPart * 10 + (c - (byte)'0');
        }

        for (; i < span.Length; i++)
        {
            byte c = span[i];
            div *= 10f;
            fracPart += (c - (byte)'0') / div;
        }

        float res = intPart + fracPart;
        return neg ? -res : res;
    }
    
    
    // =============== 方法一：Span.Slice + IndexOf ===================
    public static float[] SpanSliceParse(string[] lines)
    {
        float[] values = new float[7];

        foreach (var line in lines)
        {
            var span = line.AsSpan();

            int start = 0;
            for (int k = 0; k < 6; k++) // 找6个空格
            {
                int spaceIdx = span.Slice(start).IndexOf(' ');
                values[k] = ParseFloatAscii(span.Slice(start, spaceIdx));
                start += spaceIdx + 1;
            }
            values[6] = ParseFloatAscii(span.Slice(start));
        }
        return values;
    }

    // =============== 方法二：SIMD 扫描 ===================
    // ================= 方法二：SIMD(Vector<byte>) =================
    static void SimdParse(string[] lines)
    {
        foreach (var line in lines)
        {
            var bytes = Encoding.ASCII.GetBytes(line);
            float[] values = new float[7];
            var positions = new List<int>(8);

            ScanDelimitersVector(bytes, (byte)' ', (byte)'\n', positions);

            int start = 0;
            int vi = 0;
            foreach (int pos in positions)
            {
                int len = pos - start;
                if (len > 0)
                {
                    values[vi++] = ParseFloatAscii(bytes.AsSpan(start, len));
                }
                start = pos + 1;
            }

            if (start < bytes.Length && vi < 7)
                values[vi] = ParseFloatAscii(bytes.AsSpan(start));
        }
    }
    
    public static List<float[]> ParseFile(string path)
    {
        // 1. 读入整个文件（假设是ASCII）
        byte[] data = File.ReadAllBytes(path);
        var span = new ReadOnlySpan<byte>(data);

        // 2. 存结果（每行7个float）
        var results = new List<float[]>(capacity: 10000);
        float[] currentLine = new float[7];
        int valueIndex = 0;

        // 3. 扫描分隔符
        var positions = new List<int>(span.Length / 5); // 预估分隔符数量
        ScanDelimitersVector(span, (byte)' ', (byte)'\n', positions);

        int start = 0;
        foreach (int pos in positions)
        {
            int len = pos - start;
            if (len > 0)
            {
                var token = span.Slice(start, len);
                float v = ParseFloatAscii(token);
                currentLine[valueIndex++] = v;
            }

            byte delim = span[pos];
            if (delim == (byte)'\n')
            {
                // 行结束
                results.Add(currentLine);
                currentLine = new float[7];
                valueIndex = 0;
            }

            start = pos + 1;
        }

        return results;
    }

    // ================= SIMD 分隔符扫描 =================
    public static void ScanDelimitersVector(ReadOnlySpan<byte> span, byte space, byte newline, List<int> outPositions)
    {
        int len = span.Length;
        int step = Vector<byte>.Count; // 一次并行 16 或 32 个字节
        int i = 0;

        var vSpace = new Vector<byte>(space);
        var vNewline = new Vector<byte>(newline);

        for (; i + step <= len; i += step)
        {
            var chunk = new Vector<byte>(span.Slice(i, step).ToArray()); // 注意：ToArray 有分配，后面可以优化成 stackalloc
            var cmp1 = Vector.Equals(chunk, vSpace);
            var cmp2 = Vector.Equals(chunk, vNewline);
            var cmp = Vector.BitwiseOr(cmp1, cmp2);

            for (int j = 0; j < step; j++)
            {
                if (cmp[j] != 0)
                    outPositions.Add(i + j);
            }
        }

        // 处理剩余
        for (; i < len; i++)
        {
            if (span[i] == space || span[i] == newline)
                outPositions.Add(i);
        }
    }
    // =============== 标量解析 float ===================
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ParseFloatAscii(ReadOnlySpan<char> s)
    {
        if (s.Length == 0) return 0f;
        int i = 0; bool neg = false;
        if (s[0] == '-') { neg = true; i++; }
        int intPart = 0;
        while (i < s.Length && s[i] != '.')
        {
            intPart = intPart * 10 + (s[i] - '0');
            i++;
        }
        float frac = 0f, div = 1f;
        if (i < s.Length && s[i] == '.') i++;
        while (i < s.Length)
        {
            div *= 10f;
            frac += (s[i] - '0') / div;
            i++;
        }
        return neg ? -(intPart + frac) : (intPart + frac);
    }

}
