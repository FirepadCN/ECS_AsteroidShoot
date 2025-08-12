using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class BulletPoolAuthoring : MonoBehaviour
{
    public GameObject BulletPrefab;
    public int PoolSize = 100;
}


public class BulletPoolBaker : Baker<BulletPoolAuthoring>
{
    public override void Bake(BulletPoolAuthoring authoring)
    {
        var poolEntity = GetEntity(TransformUsageFlags.None);
        var prefabEntity = GetEntity(authoring.BulletPrefab, TransformUsageFlags.Dynamic);
        AddComponent(poolEntity, new BulletPool
        {
            Prefab = prefabEntity,
            Size = authoring.PoolSize
        });
    }
}



