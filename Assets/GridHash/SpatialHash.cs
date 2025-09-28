using System.Collections.Generic;
using UnityEngine;

public class SpatialHash
{
    public float cellSize = 2f;
    public readonly Dictionary<Vector3Int, List<Transform>> grid = new();

    private static readonly List<Vector3Int> neighborOffsets = new();

    public SpatialHash(float cellSize = 2f)
    {
        this.cellSize = Mathf.Max(0.0001f, cellSize);
        BuildNeighborOffsets(1); // 默认 1 环（半径≈cellSize）
    }

    // 若半径变化很大，可在外部根据 radius 调整邻域层数
    public void BuildNeighborOffsets(int range)
    {
        neighborOffsets.Clear();
        for (int x = -range; x <= range; x++)
        for (int y = -range; y <= range; y++)
        for (int z = -range; z <= range; z++)
            neighborOffsets.Add(new Vector3Int(x,y,z));
    }
    
    private Vector3Int Hash(Vector3 pos)
    {
        return new Vector3Int(
            Mathf.FloorToInt(pos.x / cellSize),
            Mathf.FloorToInt(pos.y / cellSize),
            Mathf.FloorToInt(pos.z / cellSize));
    }

    public void Clear() => grid.Clear();

    public void Register(Transform t)
    {
        var key = Hash(t.position);
        if (!grid.TryGetValue(key, out var list))
        {
            list = new List<Transform>(16);
            grid[key] = list;
        }
        list.Add(t);
    }
    
    public void QueryNearbyNonAlloc(Vector3 pos, float radius, List<Transform> outCandidates)
    {
        outCandidates.Clear();
        int range = Mathf.CeilToInt(radius / cellSize);
        // 预构建过的 offsets 如果层数不够，可临时补充
        if (neighborOffsets.Count < (2*range+1)*(2*range+1)*(2*range+1))
            BuildNeighborOffsets(range);

        var baseCell = Hash(pos);
        for (int i = 0; i < neighborOffsets.Count; i++)
        {
            var key = baseCell + neighborOffsets[i];
            if (grid.TryGetValue(key, out var list))
                outCandidates.AddRange(list);
        }
    }

}