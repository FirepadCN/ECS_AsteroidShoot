using Unity.Entities;
using Unity.Mathematics;

//----------------小行星----------------
public struct MoveSpeed : IComponentData
{
    public float Value;
}

public struct Heading : IComponentData
{
    public float3 Value; // 归一化方向
}

public struct Lifetime : IComponentData
{
    public float Seconds;
}

public struct AsteroidTag : IComponentData {} // 小块标记


// 单例配置（场景内 1 份）
public struct SpawnSettings : IComponentData
{
    public Entity Prefab;
    public int Count;
    public float3 AreaSize; // 生成范围盒
}

//------------子弹-----------------
public struct BulletPool : IComponentData
{
    public Entity Prefab;
    public int Size;
}


public struct BulletTag : IComponentData {}
public struct BulletVelocity : IComponentData
{
    public float3 Value;
}
public struct ActiveBullet : IComponentData {} // 有这个组件表示正在飞行

//------------UI-------------------
public struct Score : IComponentData
{
    public int Value;
}